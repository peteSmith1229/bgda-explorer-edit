using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using static JetBlackEngineLib.Data.Scripting.ScrDecoder;

namespace JetBlackEngineLib.Data.Scripting;

/// <summary>
/// A compiled Baldur's Gate: Dark Alliance script (.scr) whose call arguments
/// can be changed and written back.
/// <para>
/// Code never moves. Numbers are rewritten in place. Text is rewritten in its
/// own slot of the data segment when it fits and nothing else points into that
/// slot; otherwise it is added at the end of the data segment (the work area
/// after it moves up) and the call is pointed at the new copy. Jumps, labels,
/// switch tables and variables keep their addresses, the same append-only rule
/// <see cref="ScriptDetourPatcher"/> follows.
/// </para>
/// </summary>
public sealed class EditableScript
{
    /// <summary>Longest text an argument may be given.</summary>
    public const int MaxTextLength = 255;

    /// <summary>The script header follows 0x60 bytes of padding.</summary>
    private const int HeaderSize = 0x60;

    private const int HeaderFieldsSize = 0x30;
    private const int TableEntrySize = 0x18;

    // Header fields, relative to HeaderSize. Offsets in the header are relative to it too.
    private const int WorkAreaField = 0x00;
    private const int InstructionsField = 0x0C;
    private const int DataField = 0x10;
    private const int DataEndField = 0x14;
    private const int WorkAreaEndField = 0x18;
    private const int Offset5Field = 0x1C;
    private const int InternalCountField = 0x20;
    private const int InternalsField = 0x24;
    private const int ExternalCountField = 0x28;
    private const int ExternalsField = 0x2C;

    /// <summary>Header fields that can point at or past the end of the data segment.</summary>
    private static readonly int[] FieldsMovedWithData =
        { WorkAreaField, DataEndField, WorkAreaEndField, Offset5Field, InternalsField, ExternalsField };

    private const int OpPushA = 0x24;
    private const int OpPushS3 = 0x25;
    private const int OpPushImmediate = 0x27;
    private const int OpPushVariable = 0x28;
    private const int OpPushLocal = 0x29;
    private const int OpPop = 0x2C;
    private const int OpCallExternal = 0x7B;
    private const int OpDebugLine = 0x7D;

    private readonly record struct Op(int Address, int Code, int Size);

    private byte[] _data;
    private readonly int _codeStart;
    private readonly int _dataStart;
    private int _dataLength;
    private readonly List<Op> _code = new();
    private readonly List<ScriptCall> _calls = new();

    /// <summary>File offsets of operands known to be plain numbers (argument counts, number arguments).</summary>
    private readonly HashSet<int> _numberOperands = new();

    /// <summary>Data-segment entries: start → slot size (the text, its terminator and padding).</summary>
    private readonly SortedDictionary<int, int> _slots = new();

    /// <summary>
    /// Room each text was given (start → bytes). Shortening a text zeroes the
    /// rest of its slot, which then reads as empty entries; the room is still its own.
    /// </summary>
    private readonly Dictionary<int, int> _room = new();

    /// <summary>Operand values that may point into the data segment.</summary>
    private readonly List<int> _dataReferences = new();

    private EditableScript(byte[] scr)
    {
        _data = (byte[])scr.Clone();
        if (_data.Length < HeaderSize + HeaderFieldsSize)
        {
            throw Invalid("The script is too short to have a header.");
        }

        var bodyLength = _data.Length - HeaderSize;
        var instructionsOffset = Field(InstructionsField);
        var dataOffset = Field(DataField);
        var dataEnd = Field(DataEndField);
        if (instructionsOffset < HeaderFieldsSize || dataOffset < instructionsOffset || dataEnd < dataOffset ||
            dataEnd > bodyLength)
        {
            throw Invalid("The header's offsets are out of range.");
        }

        if ((dataOffset - instructionsOffset) % 4 != 0 || (dataEnd - dataOffset) % 4 != 0)
        {
            throw Invalid("The code or data isn't a whole number of words.");
        }

        _codeStart = HeaderSize + instructionsOffset;
        _dataStart = HeaderSize + dataOffset;
        _dataLength = dataEnd - dataOffset;

        var internals = ReadTable(InternalCountField, InternalsField, "function")
            .OrderBy(e => e.Address).ThenBy(e => e.Name, StringComparer.Ordinal).ToList();
        var externals = ReadTable(ExternalCountField, ExternalsField, "engine function").Select(e => e.Name).ToArray();
        Functions = internals.Select(e => e.Name).ToArray();
        Externals = externals;

        ReadCode(dataOffset - instructionsOffset);
        var entryPoints = CheckControlFlow(internals);
        ParseSlots();
        foreach (var (start, size) in _slots) _room[start] = size;
        ReadCalls(internals, externals, entryPoints);
        CollectDataReferences();
    }

    /// <summary>Reads a script; throws <see cref="InvalidDataException"/> when its layout isn't understood.</summary>
    public static EditableScript Load(byte[] scr) => new(scr);

    /// <summary>Reads a script, or explains why it can't be edited.</summary>
    public static bool TryLoad(byte[] scr, [NotNullWhen(true)] out EditableScript? script, out string error)
    {
        try
        {
            script = new EditableScript(scr);
            error = "";
            return true;
        }
        catch (InvalidDataException ex)
        {
            script = null;
            error = ex.Message;
            return false;
        }
    }

    /// <summary>Script functions (internal labels), in address order.</summary>
    public IReadOnlyList<string> Functions { get; }

    /// <summary>Engine functions the script imports, by index.</summary>
    public IReadOnlyList<string> Externals { get; }

    /// <summary>Every call to an engine function, in code order.</summary>
    public IReadOnlyList<ScriptCall> Calls => _calls;

    public int InstructionCount => _code.Count;

    /// <summary>Size of the data segment (string literals and global variables), in bytes.</summary>
    public int DataLength => _dataLength;

    /// <summary>Size of the script, in bytes.</summary>
    public int Length => _data.Length;

    /// <summary>A copy of the script's current bytes.</summary>
    public byte[] ToArray() => (byte[])_data.Clone();

    /// <summary>Why <paramref name="text"/> can't be used as an argument, or null when it can.</summary>
    public static string? CheckText(string? text)
    {
        if (string.IsNullOrEmpty(text)) return "Enter some text.";
        if (text.Length > MaxTextLength) return $"Text can't be longer than {MaxTextLength} characters.";
        foreach (var c in text)
        {
            if (c < 0x20 || c > 0x7E) return "Use plain ASCII letters, digits and punctuation.";
        }

        return null;
    }

    /// <summary>
    /// How many characters a text argument can take without the script
    /// growing: the room its text has, or 0 when other code may point into
    /// that room (a change is then stored as new text).
    /// </summary>
    public int InPlaceLength(ScriptArgument argument) =>
        argument.Kind == ScriptValueKind.Text && IsPrivate(argument) ? Room(argument.Value) - 1 : 0;

    /// <summary>True when <paramref name="text"/> can replace the argument's text without the script growing.</summary>
    public bool FitsInPlace(ScriptArgument argument, string text) => text.Length <= InPlaceLength(argument);

    public void SetNumber(ScriptArgument argument, int value)
    {
        CheckEditable(argument, ScriptValueKind.Number);
        WriteInt32(argument.ImmediateOffset, value);
        argument.Value = value;
    }

    public void SetText(ScriptArgument argument, string text)
    {
        CheckEditable(argument, ScriptValueKind.Text);
        var problem = CheckText(text);
        if (problem != null) throw new ArgumentException(problem, nameof(text));
        if (text == argument.Text) return;

        var bytes = Encoding.ASCII.GetBytes(text);
        var needed = (bytes.Length + 4) & ~3; // the text and its terminator, padded to a word
        var start = argument.Value;
        var room = Room(start);
        var isPrivate = IsPrivate(argument);

        if (isPrivate && bytes.Length + 1 <= room)
        {
            WriteText(start, room, bytes);
        }
        else if (isPrivate && start + room == _dataLength)
        {
            // Already the last entry (typically text added by an earlier edit): make it bigger.
            GrowData(needed - room);
            WriteText(start, needed, bytes);
            _room[start] = needed;
        }
        else
        {
            var added = _dataLength;
            GrowData(needed);
            WriteText(added, needed, bytes);
            _room[added] = needed;
            WriteInt32(argument.ImmediateOffset, added);
            argument.Value = added;
        }

        argument.Text = text;
        ParseSlots();
        CollectDataReferences();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Reading
    // ─────────────────────────────────────────────────────────────────────────

    private List<(string Name, int Address)> ReadTable(int countField, int offsetField, string what)
    {
        var count = Field(countField);
        var offset = Field(offsetField);
        if (count < 0 || count > 4096 ||
            (count > 0 && (offset < HeaderFieldsSize ||
                           HeaderSize + offset + (long)count * TableEntrySize > _data.Length)))
        {
            throw Invalid($"The {what} table is out of range.");
        }

        var entries = new List<(string, int)>(count);
        for (var i = 0; i < count; i++)
        {
            var at = HeaderSize + offset + i * TableEntrySize;
            var end = at + 4;
            while (end < at + TableEntrySize && _data[end] != 0) end++;
            entries.Add((Encoding.ASCII.GetString(_data, at + 4, end - at - 4), I32(at)));
        }

        return entries;
    }

    /// <summary>Splits the code into instructions; the last must end exactly where the data begins.</summary>
    private void ReadCode(int codeLength)
    {
        var address = 0;
        while (address < codeLength)
        {
            var code = I32(_codeStart + address);
            if (code < 0 || code >= bgdaOpCodeArgs.Length)
            {
                throw Invalid($"Unknown instruction 0x{code:X} at 0x{address:X4}.");
            }

            var left = codeLength - address;
            var size = bgdaOpCodeArgs[code] switch
            {
                ARGS_TYPE.NO_ARGS => 4,
                ARGS_TYPE.ONE_ARG or ARGS_TYPE.ONE_ARG_INSTR => 8,
                ARGS_TYPE.TWO_ARGS => 12,
                // The count includes the count word itself.
                ARGS_TYPE.VAR_ARGS => left < 8 ? -1 : CountedSize(I32(_codeStart + address + 4), 1, 4, 4),
                // Switch table: count, (address, case value) pairs, default address.
                _ => left < 8 ? -1 : CountedSize(I32(_codeStart + address + 4), 0, 12, 8)
            };
            if (size < 0 || size > left)
            {
                throw Invalid($"The instruction at 0x{address:X4} runs past the end of the code.");
            }

            _code.Add(new Op(address, code, size));
            address += size;
        }
    }

    private static int CountedSize(int count, int minCount, int fixedSize, int itemSize) =>
        count < minCount || count > 0x10000 ? -1 : fixedSize + count * itemSize;

    /// <summary>
    /// Checks that functions, jumps and switch tables all land on instructions,
    /// and returns those landing points.
    /// </summary>
    private HashSet<int> CheckControlFlow(List<(string Name, int Address)> internals)
    {
        var starts = new HashSet<int>(_code.Select(op => op.Address));
        var entryPoints = new HashSet<int>();
        foreach (var (name, address) in internals)
        {
            if (!starts.Contains(address)) throw Invalid($"Function '{name}' starts inside an instruction.");
            entryPoints.Add(address);
        }

        foreach (var op in _code)
        {
            foreach (var target in CodeTargets(op))
            {
                if (!starts.Contains(target))
                {
                    throw Invalid($"The jump at 0x{op.Address:X4} lands inside an instruction.");
                }

                entryPoints.Add(target);
            }
        }

        return entryPoints;
    }

    private IEnumerable<int> CodeTargets(Op op)
    {
        switch (bgdaOpCodeArgs[op.Code])
        {
            case ARGS_TYPE.ONE_ARG_INSTR:
                yield return Operand(op, 0);
                break;
            case ARGS_TYPE.ARGS_130:
                var cases = Operand(op, 0);
                for (var i = 0; i < cases; i++) yield return Operand(op, 1 + 2 * i);
                yield return Operand(op, 1 + 2 * cases);
                break;
        }
    }

    /// <summary>
    /// Splits the data segment the way <see cref="ScrDecoder"/> reads the string
    /// table: each entry starts on a word and ends with the word holding its terminator.
    /// </summary>
    private void ParseSlots()
    {
        _slots.Clear();
        var start = -1;
        for (var word = 0; word < _dataLength; word += 4)
        {
            if (start < 0) start = word;
            var at = _dataStart + word;
            if (_data[at] == 0 || _data[at + 1] == 0 || _data[at + 2] == 0 || _data[at + 3] == 0)
            {
                _slots[start] = word + 4 - start;
                start = -1;
            }
        }

        if (start >= 0) _slots[start] = _dataLength - start;
    }

    private void ReadCalls(List<(string Name, int Address)> internals, string[] externals, HashSet<int> entryPoints)
    {
        var function = -1;
        for (var k = 0; k < _code.Count; k++)
        {
            var op = _code[k];
            while (function + 1 < internals.Count && internals[function + 1].Address <= op.Address) function++;
            if (op.Code != OpCallExternal) continue;

            var index = Operand(op, 0);
            if (index < 0 || index >= externals.Length)
            {
                throw Invalid($"The call at 0x{op.Address:X4} names engine function {index}, " +
                              $"but the script imports {externals.Length}.");
            }

            var call = new ScriptCall(function >= 0 ? internals[function].Name : "", externals[index], op.Address);
            ReadArguments(call, k, entryPoints);
            _calls.Add(call);
        }
    }

    /// <summary>
    /// Arguments are pushed last-first, then their size in bytes, then the
    /// call is made, so argument i is the push i + 2 instructions before it.
    /// </summary>
    private void ReadArguments(ScriptCall call, int callIndex, HashSet<int> entryPoints)
    {
        const string unidentified = "The call's arguments couldn't be identified.";
        var countPush = callIndex > 0 ? _code[callIndex - 1] : default;
        var byteCount = callIndex > 0 && countPush.Code == OpPushImmediate ? Operand(countPush, 0) : -1;
        var count = byteCount / 4;
        if (byteCount < 0 || byteCount % 4 != 0 || count > 16 || callIndex - 1 - count < 0)
        {
            call.Note = unidentified;
            return;
        }

        var pushes = new Op[count];
        for (var i = 0; i < count; i++)
        {
            pushes[i] = _code[callIndex - 2 - i];
            if (pushes[i].Code is not (OpPushA or OpPushS3 or OpPushImmediate or OpPushVariable or OpPushLocal))
            {
                call.Note = unidentified;
                return;
            }
        }

        _numberOperands.Add(OperandOffset(countPush, 0));

        var parameters = ScriptFunctions.ParametersOf(call.Name);
        string? note = null;
        if (parameters == null)
        {
            note = "WorldExplorer doesn't know this function's arguments yet, so they can't be edited.";
        }
        else if (parameters.Count != count)
        {
            note = $"{call.Name} takes {Arguments(parameters.Count)}, but this call passes {count}.";
        }
        else
        {
            // Code that jumps in part-way through the pushes could supply other values.
            if (count > 0 && Enumerable.Range(callIndex - count, count + 1)
                    .Any(j => entryPoints.Contains(_code[j].Address)))
            {
                note = "The arguments are set on more than one path, so they can't be edited here.";
            }

            for (var i = 0; i < count && note == null; i++)
            {
                if (parameters[i].Kind == ScriptValueKind.Text && pushes[i].Code == OpPushImmediate &&
                    !_slots.ContainsKey(Operand(pushes[i], 0)))
                {
                    note = $"The {parameters[i].Name} argument doesn't point at text.";
                }
            }
        }

        var known = note == null;
        var arguments = new ScriptArgument[count];
        for (var i = 0; i < count; i++)
        {
            var push = pushes[i];
            var name = known ? parameters![i].Name : $"argument {i + 1}";
            if (push.Code != OpPushImmediate)
            {
                var source = push.Code switch
                {
                    OpPushVariable => "variable",
                    OpPushLocal => "local variable",
                    _ => "computed"
                };
                arguments[i] = new ScriptArgument(this, call, i, name, ScriptValueKind.Computed, -1, 0, null, source);
                continue;
            }

            var operand = OperandOffset(push, 0);
            var value = I32(operand);
            var kind = known ? parameters![i].Kind : ScriptValueKind.Number;
            if (kind == ScriptValueKind.Number && known) _numberOperands.Add(operand);
            arguments[i] = new ScriptArgument(this, call, i, name, kind, operand, value,
                kind == ScriptValueKind.Text ? ReadText(value) : null, null);
        }

        call.Arguments = arguments;
        call.IsKnown = known;
        call.Note = note;
    }

    private static string Arguments(int count) => count == 1 ? "1 argument" : $"{count} arguments";

    /// <summary>
    /// Every operand that could hold a data-segment address: all of them except
    /// code addresses, switch case values, debug line numbers, stack sizes,
    /// engine function indexes and arguments known to be numbers. Text
    /// arguments are included, so each counts its own reference.
    /// </summary>
    private void CollectDataReferences()
    {
        _dataReferences.Clear();
        foreach (var op in _code)
        {
            switch (bgdaOpCodeArgs[op.Code])
            {
                case ARGS_TYPE.ONE_ARG when op.Code is not (OpPop or OpCallExternal):
                    if (!_numberOperands.Contains(OperandOffset(op, 0))) _dataReferences.Add(Operand(op, 0));
                    break;
                case ARGS_TYPE.TWO_ARGS when op.Code != OpDebugLine:
                    _dataReferences.Add(Operand(op, 0));
                    _dataReferences.Add(Operand(op, 1));
                    break;
                case ARGS_TYPE.VAR_ARGS:
                    for (var i = 1; i < Operand(op, 0); i++) _dataReferences.Add(Operand(op, i));
                    break;
            }
        }
    }

    /// <summary>True when only this argument points into its text's room, so the room can be rewritten.</summary>
    private bool IsPrivate(ScriptArgument argument)
    {
        if (!_slots.ContainsKey(argument.Value)) return false;
        var end = argument.Value + Room(argument.Value);
        var inside = 0;
        foreach (var value in _dataReferences)
        {
            if (value >= argument.Value && value < end && ++inside > 1) return false;
        }

        return inside == 1;
    }

    private int Room(int start) =>
        Math.Max(_room.TryGetValue(start, out var room) ? room : 0, _slots.TryGetValue(start, out var slot) ? slot : 0);

    private string ReadText(int offset)
    {
        var text = new StringBuilder();
        for (var i = offset; i < _dataLength; i++)
        {
            var b = _data[_dataStart + (i & ~3) + 3 - (i & 3)]; // bytes are reversed within each word
            if (b == 0) break;
            text.Append((char)b);
        }

        return text.ToString();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Writing
    // ─────────────────────────────────────────────────────────────────────────

    private void CheckEditable(ScriptArgument argument, ScriptValueKind kind)
    {
        if (!ReferenceEquals(argument.Owner, this))
        {
            throw new ArgumentException("The argument belongs to another script.", nameof(argument));
        }

        if (!argument.IsEditable || argument.Kind != kind)
        {
            throw new InvalidOperationException($"The {argument.Name} argument of {argument.Call.Name} can't be " +
                                                $"set to {(kind == ScriptValueKind.Text ? "text" : "a number")}.");
        }
    }

    /// <summary>Writes <paramref name="bytes"/>, a terminator and padding over a slot, word-reversed.</summary>
    private void WriteText(int start, int slotSize, byte[] bytes)
    {
        for (var i = 0; i < slotSize; i++)
        {
            _data[_dataStart + start + (i & ~3) + 3 - (i & 3)] = i < bytes.Length ? bytes[i] : (byte)0;
        }
    }

    /// <summary>Adds <paramref name="count"/> zero bytes at the end of the data segment.</summary>
    private void GrowData(int count)
    {
        var insertAt = _dataStart + _dataLength;
        var oldEnd = Field(DataEndField);
        var grown = new byte[_data.Length + count];
        Buffer.BlockCopy(_data, 0, grown, 0, insertAt);
        Buffer.BlockCopy(_data, insertAt, grown, insertAt + count, _data.Length - insertAt);
        _data = grown;

        foreach (var field in FieldsMovedWithData)
        {
            var value = Field(field);
            if (value != -1 && value >= oldEnd) WriteInt32(HeaderSize + field, value + count);
        }

        _dataLength += count;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private int Field(int field) => I32(HeaderSize + field);

    private int OperandOffset(Op op, int index) => _codeStart + op.Address + 4 + 4 * index;

    private int Operand(Op op, int index) => I32(OperandOffset(op, index));

    private int I32(int offset)
    {
        if (offset < 0 || offset + 4 > _data.Length) throw Invalid("The script ends unexpectedly.");
        return BitConverter.ToInt32(_data, offset);
    }

    private void WriteInt32(int offset, int value) => BitConverter.GetBytes(value).CopyTo(_data, offset);

    private static InvalidDataException Invalid(string message) => new(message);
}
