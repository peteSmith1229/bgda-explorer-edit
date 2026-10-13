using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

namespace JetBlackEngineLib.Data.CutScenes;

/// <summary>Text settings in a cutscene's header.</summary>
public enum CutsceneText
{
    /// <summary>The subtitle text the scene shows, e.g. "introcut_text1".</summary>
    Subtitles,

    /// <summary>The narration sound file, e.g. "nar_bushwacked.vag".</summary>
    Narration,

    /// <summary>The music file, e.g. "mx_cs_mugging_01_l.vag".</summary>
    Music
}

/// <summary>
/// A Baldur's Gate: Dark Alliance cutscene (.cut) that can be changed and
/// written back: the header's sound and subtitle names and its position, the
/// cast (name, start position, facing; copy or remove a member) and the
/// keyframes (time and values; copy or delete them).
/// <para>
/// Layout: a header, the cast (0x2C bytes each), the keyframes (0x14 bytes
/// each, in time order) and, in every scene shipped with the game, room for
/// eight more keyframes holding a copy of the first eight. The game uses that
/// room for six camera keyframes it adds when header flag 2 or 4 is set (start
/// from, or return to, the gameplay camera). Keyframes are kept in time order
/// and that copy is kept in step; everything else is written back as it was read.
/// </para>
/// <para>
/// Keyframes refer to cast members by position (their actor number, and the
/// target of a "Face" keyframe), so a copied member is added at the end of the
/// cast, and removing one renumbers the keyframes of the members after it.
/// </para>
/// </summary>
public sealed class EditableCutscene
{
    /// <summary>Longest sound or subtitle name (the field is 0x28 bytes, with its terminator).</summary>
    public const int MaxTextLength = 0x27;

    /// <summary>Longest cast member name.</summary>
    public const int MaxNameLength = CutsceneActor.NameSize - 1;

    /// <summary>
    /// Most cast members a scene can have here. The game has no fixed limit, but each member the scene
    /// creates takes a slot in the game's object pools while it plays:
    /// a player stand-in takes one of the 32 1 KB slots, which the players' own characters use too.
    /// </summary>
    public const int MaxCast = 32;

    private const int KeyframeOffsetField = 0x04;
    private const int KeyframeCountField = 0x08;
    private const int CastOffsetField = 0x0C;
    private const int CastCountField = 0x10;
    private const int PositionField = 0x14;
    private const int HeaderSize = 0x9C;

    private static readonly Dictionary<CutsceneText, int> TextFields = new()
    {
        [CutsceneText.Subtitles] = 0x20,
        [CutsceneText.Narration] = 0x48,
        [CutsceneText.Music] = 0x74
    };

    private const int TextFieldSize = 0x28;

    /// <summary>Everything before the keyframes: header, padding and cast (edited in place; replaced when the cast grows or shrinks).</summary>
    private byte[] _prefix;

    private readonly List<CutsceneKeyframe> _keyframes;
    private readonly List<CutsceneActor> _cast = new();

    /// <summary>The bytes after the keyframes.</summary>
    private readonly byte[] _trailing;

    /// <summary>The trailing bytes are a copy of the first keyframes, kept in step when they change.</summary>
    private readonly bool _trailingMirrorsKeyframes;

    private EditableCutscene(byte[] cut)
    {
        if (cut.Length < HeaderSize) throw Invalid("The cutscene is too short to have a header.");

        int Field(int offset) => BitConverter.ToInt32(cut, offset);
        var keyframeOffset = Field(KeyframeOffsetField);
        var keyframeCount = Field(KeyframeCountField);
        var castOffset = Field(CastOffsetField);
        var castCount = Field(CastCountField);

        if (castCount < 0 || castCount > 255 || castOffset < HeaderSize ||
            castOffset + (long)castCount * CutsceneActor.Size > keyframeOffset)
        {
            throw Invalid("The cast isn't where the header says.");
        }

        if (keyframeCount < 1 || keyframeCount > 0xFFFF ||
            keyframeOffset + (long)keyframeCount * CutsceneKeyframe.Size > cut.Length)
        {
            throw Invalid("The keyframes aren't where the header says.");
        }

        _prefix = cut.AsSpan(0, keyframeOffset).ToArray();
        ReadCast();

        _keyframes = new List<CutsceneKeyframe>(keyframeCount);
        for (var i = 0; i < keyframeCount; i++)
        {
            var keyframe = CutsceneKeyframe.Read(cut, keyframeOffset + i * CutsceneKeyframe.Size);
            if (!float.IsFinite(keyframe.Time) || keyframe.Time < 0)
            {
                throw Invalid($"Keyframe {i + 1} has no valid time.");
            }

            if (keyframe.ActorKind == CutsceneActorKind.Cast && (keyframe.Actor < 0 || keyframe.Actor >= castCount))
            {
                throw Invalid($"Keyframe {i + 1} is for cast member {keyframe.Actor}, but the cast has {castCount}.");
            }

            _keyframes.Add(keyframe);
        }

        var keyframesEnd = keyframeOffset + keyframeCount * CutsceneKeyframe.Size;
        _trailing = cut.AsSpan(keyframesEnd).ToArray();
        _trailingMirrorsKeyframes = _trailing.Length > 0 && _trailing.Length % CutsceneKeyframe.Size == 0 &&
                                    MirrorsKeyframes(cut, keyframeOffset, keyframesEnd, _trailing.Length);
    }

    /// <summary>Reads a cutscene; throws <see cref="InvalidDataException"/> when its layout isn't understood.</summary>
    public static EditableCutscene Load(byte[] cut) => new(cut);

    /// <summary>Reads a cutscene, or explains why it can't be edited.</summary>
    public static bool TryLoad(byte[] cut, [NotNullWhen(true)] out EditableCutscene? cutscene, out string error)
    {
        try
        {
            cutscene = new EditableCutscene(cut);
            error = "";
            return true;
        }
        catch (InvalidDataException ex)
        {
            cutscene = null;
            error = ex.Message;
            return false;
        }
    }

    public IReadOnlyList<CutsceneActor> Cast => _cast;

    /// <summary>The keyframes, in time order.</summary>
    public IReadOnlyList<CutsceneKeyframe> Keyframes => _keyframes;

    /// <summary>Where <paramref name="keyframe"/> is in time order, or -1 if it isn't in this cutscene.</summary>
    public int IndexOf(CutsceneKeyframe keyframe) => _keyframes.IndexOf(keyframe);

    /// <summary>The time of the last keyframe, in seconds.</summary>
    public float Duration => _keyframes.Count == 0 ? 0 : _keyframes[^1].Time;

    /// <summary>Header flags (meaning not known).</summary>
    public int Flags => BitConverter.ToInt32(_prefix, 0);

    /// <summary>The number stored before the music name (meaning not known; 10 or 17 in scenes with music).</summary>
    public int AudioSetting => BitConverter.ToInt32(_prefix, 0x70);

    /// <summary>A point in the level stored in the header (x, y, z).</summary>
    public float PositionX => BitConverter.ToSingle(_prefix, PositionField);

    public float PositionY => BitConverter.ToSingle(_prefix, PositionField + 4);
    public float PositionZ => BitConverter.ToSingle(_prefix, PositionField + 8);

    /// <summary>A copy of the cutscene's current bytes.</summary>
    public byte[] ToArray()
    {
        var keyframeOffset = _prefix.Length;
        var data = new byte[keyframeOffset + _keyframes.Count * CutsceneKeyframe.Size + _trailing.Length];
        _prefix.CopyTo(data, 0);
        BitConverter.GetBytes(_keyframes.Count).CopyTo(data, KeyframeCountField);

        for (var i = 0; i < _keyframes.Count; i++)
        {
            _keyframes[i].Write(data, keyframeOffset + i * CutsceneKeyframe.Size);
        }

        var trailingStart = keyframeOffset + _keyframes.Count * CutsceneKeyframe.Size;
        _trailing.CopyTo(data, trailingStart);
        if (_trailingMirrorsKeyframes)
        {
            var copies = Math.Min(_trailing.Length / CutsceneKeyframe.Size, _keyframes.Count);
            for (var i = 0; i < copies; i++)
            {
                _keyframes[i].Write(data, trailingStart + i * CutsceneKeyframe.Size);
            }
        }

        return data;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Header
    // ─────────────────────────────────────────────────────────────────────────

    public string GetText(CutsceneText text)
    {
        var start = TextFields[text];
        var end = start;
        while (end < start + TextFieldSize && _prefix[end] != 0) end++;
        return Encoding.ASCII.GetString(_prefix, start, end - start);
    }

    /// <summary>Why <paramref name="value"/> can't be used as a sound or subtitle name, or null when it can (empty means none).</summary>
    public static string? CheckText(string value) => CheckAscii(value, MaxTextLength, allowEmpty: true);

    public void SetText(CutsceneText text, string value)
    {
        Require(CheckText(value));
        var bytes = Encoding.ASCII.GetBytes(value);
        var start = TextFields[text];
        for (var i = 0; i < TextFieldSize; i++) _prefix[start + i] = i < bytes.Length ? bytes[i] : (byte)0;
    }

    /// <summary>Sets one coordinate (0 = x, 1 = y, 2 = z) of the header's position.</summary>
    public void SetPosition(int axis, float value)
    {
        if (axis is < 0 or > 2) throw new ArgumentOutOfRangeException(nameof(axis));
        Require(CheckDecimal(value));
        BitConverter.GetBytes(value).CopyTo(_prefix, PositionField + axis * 4);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Cast
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Why <paramref name="name"/> can't be a cast member's name, or null when it can.</summary>
    public static string? CheckName(string name) => CheckAscii(name, MaxNameLength, allowEmpty: false);

    public void SetName(CutsceneActor actor, string name)
    {
        CheckOwner(actor);
        Require(CheckName(name));
        actor.Name = name;
    }

    /// <summary>Sets one coordinate (0 = x, 1 = y, 2 = z) of where a cast member starts.</summary>
    public void SetPosition(CutsceneActor actor, int axis, float value)
    {
        CheckOwner(actor);
        Require(CheckDecimal(value));
        switch (axis)
        {
            case 0: actor.X = value; break;
            case 1: actor.Y = value; break;
            case 2: actor.Z = value; break;
            default: throw new ArgumentOutOfRangeException(nameof(axis));
        }
    }

    public void SetFacing(CutsceneActor actor, double degrees)
    {
        CheckOwner(actor);
        Require(CheckDecimal(degrees));
        actor.Facing = degrees;
    }

    /// <summary>
    /// A name for a copy of <paramref name="actor"/> that isn't in the cast yet: the name with the next
    /// number, e.g. Player → Player2 → Player3, Thief1 → Thief2.
    /// </summary>
    public string SuggestCopyName(CutsceneActor actor)
    {
        CheckOwner(actor);
        var name = actor.Name;
        var digits = name.Length;
        while (digits > 0 && char.IsAsciiDigit(name[digits - 1])) digits--;
        var stem = name[..digits];
        var number = digits < name.Length && int.TryParse(name[digits..], out var n) ? n : 1;
        var taken = _cast.Select(a => a.Name).ToHashSet(StringComparer.Ordinal);
        string candidate;
        do
        {
            number++;
            var suffix = number.ToString(System.Globalization.CultureInfo.InvariantCulture);
            candidate = stem[..Math.Min(stem.Length, MaxNameLength - suffix.Length)] + suffix;
        } while (taken.Contains(candidate));

        return candidate;
    }

    /// <summary>Why <paramref name="actor"/> can't be copied, or null when it can.</summary>
    public string? CheckCopy(CutsceneActor actor, bool withKeyframes)
    {
        CheckOwner(actor);
        if (_cast.Count >= MaxCast) return $"The cast already has {MaxCast} members.";
        var added = withKeyframes ? _keyframes.Count(k => k.Actor == actor.Index) : 0;
        return _keyframes.Count + added > 0xFFFF ? "The cutscene can't have more keyframes." : null;
    }

    /// <summary>
    /// Adds a copy of <paramref name="actor"/> at the end of the cast (every member keeps its position, so
    /// no keyframe changes), named <paramref name="name"/> and starting (<paramref name="dx"/>,
    /// <paramref name="dy"/>, <paramref name="dz"/>) from it. With <paramref name="withKeyframes"/>, each of
    /// its keyframes is copied for the new member at the same time (after the keyframes already at that
    /// time), with its Move to points moved by the same offset.
    /// </summary>
    public CutsceneActor CopyActor(CutsceneActor actor, string name, float dx, float dy, float dz, bool withKeyframes)
    {
        Require(CheckName(name));
        Require(CheckDecimal(dx));
        Require(CheckDecimal(dy));
        Require(CheckDecimal(dz));
        var problem = CheckCopy(actor, withKeyframes);
        if (problem != null) throw new InvalidOperationException(problem);

        var source = actor.Index;
        var index = (short)_cast.Count;
        var castOffset = BitConverter.ToInt32(_prefix, CastOffsetField);
        var at = castOffset + _cast.Count * CutsceneActor.Size;   // straight after the last member

        var entry = _prefix.AsSpan(castOffset + source * CutsceneActor.Size, CutsceneActor.Size).ToArray();
        Array.Clear(entry, 0, CutsceneActor.RuntimeSize);
        var prefix = new byte[_prefix.Length + CutsceneActor.Size];
        _prefix.AsSpan(0, at).CopyTo(prefix);
        entry.CopyTo(prefix, at);
        _prefix.AsSpan(at).CopyTo(prefix.AsSpan(at + CutsceneActor.Size));
        SetPrefix(prefix, _cast.Count + 1);

        var copy = _cast[index];
        copy.Name = name;
        copy.X += dx;
        copy.Y += dy;
        copy.Z += dz;

        if (withKeyframes)
        {
            var offset = new[] { (int)Math.Round(dx), (int)Math.Round(dy), (int)Math.Round(dz) };
            foreach (var keyframe in _keyframes.Where(k => k.Actor == source).ToList())
            {
                var added = keyframe.Clone();
                added.Actor = index;
                if (added.Action == 0)
                {
                    for (var i = 0; i < 3; i++) added.SetParameter(i, unchecked(added.Parameters[i] + offset[i]));
                }

                var after = _keyframes.FindLastIndex(k => k.Time <= added.Time);
                _keyframes.Insert(after + 1, added);
            }
        }

        return copy;
    }

    /// <summary>Why <paramref name="actor"/> can't be removed, or null when it can.</summary>
    public string? CheckRemove(CutsceneActor actor)
    {
        CheckOwner(actor);
        return _keyframes.All(k => k.Actor == actor.Index)
            ? "This member has every keyframe of the cutscene, and a cutscene needs at least one."
            : null;
    }

    /// <summary>
    /// Removes a cast member and its keyframes. The members after it move up one place, and the keyframes
    /// are renumbered to match: their actor number and the target of "Face" keyframes. A "Face" keyframe
    /// that turned towards the removed member now stops facing (-1).
    /// </summary>
    public void RemoveActor(CutsceneActor actor)
    {
        var problem = CheckRemove(actor);
        if (problem != null) throw new InvalidOperationException(problem);

        var index = actor.Index;
        var castOffset = BitConverter.ToInt32(_prefix, CastOffsetField);
        var at = castOffset + index * CutsceneActor.Size;
        var prefix = new byte[_prefix.Length - CutsceneActor.Size];
        _prefix.AsSpan(0, at).CopyTo(prefix);
        _prefix.AsSpan(at + CutsceneActor.Size).CopyTo(prefix.AsSpan(at));

        _keyframes.RemoveAll(k => k.Actor == index);
        foreach (var keyframe in _keyframes)
        {
            if (keyframe.FaceTarget is { } target && target >= 0)
            {
                keyframe.SetParameter(0, target == index ? -1 : target > index ? target - 1 : target);
            }

            if (keyframe.ActorKind == CutsceneActorKind.Cast && keyframe.Actor > index) keyframe.Actor--;
        }

        SetPrefix(prefix, _cast.Count - 1);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Keyframes
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Why <paramref name="seconds"/> can't be a keyframe time, or null when it can.</summary>
    public static string? CheckTime(double seconds) =>
        double.IsFinite(seconds) && seconds is >= 0 and <= 3600 ? null : "Enter a time from 0 to 3600 seconds.";

    /// <summary>Moves a keyframe in time; the keyframes stay in time order.</summary>
    public void SetTime(CutsceneKeyframe keyframe, double seconds)
    {
        var index = RequireIndex(keyframe);
        Require(CheckTime(seconds));
        keyframe.Time = (float)seconds;

        // Stable re-sort: the keyframe goes after any others at the same time.
        _keyframes.RemoveAt(index);
        var at = _keyframes.FindIndex(k => k.Time > keyframe.Time);
        _keyframes.Insert(at < 0 ? _keyframes.Count : at, keyframe);
    }

    /// <summary>Why <paramref name="value"/> can't be stored in <paramref name="field"/>, or null when it can.</summary>
    public static string? CheckValue(CutsceneField field, double value) => field.Kind switch
    {
        CutsceneValueKind.Integer => double.IsFinite(value) && Math.Round(value) is >= int.MinValue and <= int.MaxValue
            ? null
            : "Enter a whole number.",
        _ => CheckDecimal(value)
    };

    public void SetValue(CutsceneKeyframe keyframe, CutsceneField field, double value)
    {
        RequireIndex(keyframe);
        if (!keyframe.Fields.Contains(field)) throw new ArgumentException("The keyframe has no such value.", nameof(field));
        Require(CheckValue(field, value));
        keyframe.SetValue(field, value);
    }

    /// <summary>Adds a copy of <paramref name="keyframe"/> straight after it (same time) and returns it.</summary>
    public CutsceneKeyframe Duplicate(CutsceneKeyframe keyframe)
    {
        var index = RequireIndex(keyframe);
        if (_keyframes.Count >= 0xFFFF) throw new InvalidOperationException("The cutscene can't have more keyframes.");
        var copy = keyframe.Clone();
        _keyframes.Insert(index + 1, copy);
        return copy;
    }

    public void Remove(CutsceneKeyframe keyframe)
    {
        var index = RequireIndex(keyframe);
        if (_keyframes.Count == 1) throw new InvalidOperationException("A cutscene needs at least one keyframe.");
        _keyframes.RemoveAt(index);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Reads the cast members from <see cref="_prefix"/>.</summary>
    private void ReadCast()
    {
        var castOffset = BitConverter.ToInt32(_prefix, CastOffsetField);
        var castCount = BitConverter.ToInt32(_prefix, CastCountField);
        _cast.Clear();
        for (var i = 0; i < castCount; i++)
        {
            _cast.Add(new CutsceneActor(_prefix, castOffset + i * CutsceneActor.Size, i));
        }
    }

    /// <summary>Puts in a new header-and-cast block with <paramref name="castCount"/> members (the keyframes follow it).</summary>
    private void SetPrefix(byte[] prefix, int castCount)
    {
        BitConverter.GetBytes(prefix.Length).CopyTo(prefix, KeyframeOffsetField);
        BitConverter.GetBytes(castCount).CopyTo(prefix, CastCountField);
        _prefix = prefix;
        ReadCast();
    }

    private static bool MirrorsKeyframes(byte[] cut, int keyframeOffset, int keyframesEnd, int trailingLength)
    {
        var length = Math.Min(trailingLength, keyframesEnd - keyframeOffset);
        return cut.AsSpan(keyframesEnd, length).SequenceEqual(cut.AsSpan(keyframeOffset, length));
    }

    private int RequireIndex(CutsceneKeyframe keyframe)
    {
        var index = _keyframes.IndexOf(keyframe);
        if (index < 0) throw new ArgumentException("The keyframe belongs to another cutscene.", nameof(keyframe));
        return index;
    }

    private void CheckOwner(CutsceneActor actor)
    {
        if (actor.Index >= _cast.Count || !ReferenceEquals(_cast[actor.Index], actor))
        {
            throw new ArgumentException("The cast member belongs to another cutscene.", nameof(actor));
        }
    }

    /// <summary>Why <paramref name="value"/> can't be stored as a number here, or null when it can.</summary>
    public static string? CheckDecimal(double value) =>
        double.IsFinite(value) && Math.Abs(value) <= 1e7 ? null : "Enter a number between -10,000,000 and 10,000,000.";

    private static string? CheckAscii(string value, int maxLength, bool allowEmpty)
    {
        if (value.Length == 0) return allowEmpty ? null : "Enter a name.";
        if (value.Length > maxLength) return $"Use {maxLength} characters or fewer.";
        foreach (var c in value)
        {
            if (c < 0x20 || c > 0x7E) return "Use plain ASCII letters, digits and punctuation.";
        }

        return null;
    }

    private static void Require(string? problem)
    {
        if (problem != null) throw new ArgumentException(problem);
    }

    private static InvalidDataException Invalid(string message) => new(message);
}
