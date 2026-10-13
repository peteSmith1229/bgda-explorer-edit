using System.Text;

namespace JetBlackEngineLib.Data.CutScenes;

/// <summary>Who a keyframe is for.</summary>
public enum CutsceneActorKind
{
    Camera,

    /// <summary>Scene-wide events: subtitles, sound and the like.</summary>
    Scene,

    /// <summary>A member of the cast.</summary>
    Cast
}

/// <summary>How a keyframe value is stored and shown.</summary>
public enum CutsceneValueKind
{
    /// <summary>A whole number (positions are in world units).</summary>
    Integer,

    /// <summary>A floating-point number.</summary>
    Decimal,

    /// <summary>A 16-bit angle (65536 units per turn), shown in degrees.</summary>
    Angle
}

/// <summary>An editable value of a keyframe: one of its three parameters, and how to read it.</summary>
public sealed record CutsceneField(string Name, int Slot, CutsceneValueKind Kind);

/// <summary>
/// What the keyframe actions of Baldur's Gate: Dark Alliance cutscenes mean,
/// as far as they are understood. Actions not described here keep their
/// values but are shown by number and can only be moved in time, copied or
/// deleted.
/// </summary>
public static class CutsceneActions
{
    public const short CameraActor = -100;
    public const short SceneActor = -99;

    private static readonly CutsceneField[] None = Array.Empty<CutsceneField>();

    private static readonly CutsceneField[] Position =
    {
        new("X", 0, CutsceneValueKind.Integer),
        new("Y", 1, CutsceneValueKind.Integer),
        new("Z", 2, CutsceneValueKind.Integer)
    };

    private static readonly CutsceneField[] Angle = { new("angle", 0, CutsceneValueKind.Angle) };
    private static readonly CutsceneField[] Value = { new("value", 0, CutsceneValueKind.Decimal) };
    private static readonly CutsceneField[] Distance = { new("distance", 0, CutsceneValueKind.Integer) };

    public static CutsceneActorKind KindOf(short actor) => actor switch
    {
        CameraActor => CutsceneActorKind.Camera,
        SceneActor => CutsceneActorKind.Scene,
        _ => CutsceneActorKind.Cast
    };

    /// <summary>What the action does, e.g. "Position" (camera) or "Move to" (cast).</summary>
    public static string Describe(CutsceneActorKind kind, short action) => (kind, action) switch
    {
        (CutsceneActorKind.Camera, 0) => "Position",
        (CutsceneActorKind.Camera, 1) => "Turn",
        (CutsceneActorKind.Camera, 2) => "Tilt",
        (CutsceneActorKind.Camera, 6) => "View distance",
        (CutsceneActorKind.Scene, 0) => "Next subtitle",
        (CutsceneActorKind.Scene, 3) => "Hide subtitle",
        (CutsceneActorKind.Cast, 0) => "Move to",
        (CutsceneActorKind.Cast, 1) => "Face",
        (CutsceneActorKind.Cast, 2) => "Play animation",
        _ => $"Event {action}"
    };

    /// <summary>True when the action's meaning is known (unknown ones are shown by number).</summary>
    public static bool IsKnown(CutsceneActorKind kind, short action) =>
        !Describe(kind, action).StartsWith("Event ", StringComparison.Ordinal);

    /// <summary>The values of a keyframe that can be edited.</summary>
    public static IReadOnlyList<CutsceneField> FieldsOf(CutsceneActorKind kind, short action) => (kind, action) switch
    {
        (CutsceneActorKind.Camera, 0) or (CutsceneActorKind.Cast, 0) => Position,
        (CutsceneActorKind.Camera, 1) or (CutsceneActorKind.Camera, 2) => Angle,
        (CutsceneActorKind.Camera, 4) or (CutsceneActorKind.Camera, 5) => Value,
        (CutsceneActorKind.Camera, 6) => Distance,
        _ => None
    };

    /// <summary>Degrees for a 16-bit angle (65536 units per turn).</summary>
    public static double ToDegrees(int units) => units * 360.0 / 65536.0;

    /// <summary>The 16-bit angle for <paramref name="degrees"/>, between -32768 and 32767.</summary>
    public static int FromDegrees(double degrees)
    {
        var units = (long)Math.Round(degrees * 65536.0 / 360.0) % 65536;
        if (units >= 32768) units -= 65536;
        if (units < -32768) units += 65536;
        return (int)units;
    }
}

/// <summary>A cutscene keyframe: at <see cref="Time"/>, <see cref="Actor"/> does <see cref="Action"/>.</summary>
public sealed class CutsceneKeyframe
{
    internal const int Size = 0x14;

    private readonly int[] _parameters;

    internal CutsceneKeyframe(float time, short actor, short action, int[] parameters)
    {
        Time = time;
        Actor = actor;
        Action = action;
        _parameters = parameters;
    }

    /// <summary>Seconds from the start of the scene.</summary>
    public float Time { get; internal set; }

    /// <summary>-100 for the camera, -99 for scene events, otherwise the cast member's index.</summary>
    public short Actor { get; internal set; }

    public short Action { get; }

    public CutsceneActorKind ActorKind => CutsceneActions.KindOf(Actor);

    public string Description => CutsceneActions.Describe(ActorKind, Action);

    public IReadOnlyList<CutsceneField> Fields => CutsceneActions.FieldsOf(ActorKind, Action);

    /// <summary>The three raw parameters.</summary>
    public IReadOnlyList<int> Parameters => _parameters;

    /// <summary>
    /// For a cast member's "Face" keyframe: the index of the cast member it turns towards
    /// (-1 = stops facing anyone); null for every other keyframe. Like <see cref="Actor"/>,
    /// it refers to the cast by position.
    /// </summary>
    public int? FaceTarget => ActorKind == CutsceneActorKind.Cast && Action == 1 ? _parameters[0] : null;

    /// <summary>The parameters of an action that can't be edited, as stored, or "" when there is nothing to show.</summary>
    public string RawText => (ActorKind, Action) switch
    {
        (CutsceneActorKind.Scene, _) => "", // what is stored here is left over from the authoring tool
        (CutsceneActorKind.Cast, 1) => _parameters[0] < 0 ? "no one" : $"cast member {_parameters[0] + 1}",
        (CutsceneActorKind.Cast, 2) => $"animation 0x{_parameters[0]:X8}, {_parameters[1]}",
        _ when Fields.Count > 0 => "",
        _ => string.Join(", ", _parameters.Select(p => $"0x{p:X8}"))
    };

    public double GetValue(CutsceneField field)
    {
        var raw = _parameters[field.Slot];
        return field.Kind switch
        {
            CutsceneValueKind.Decimal => BitConverter.Int32BitsToSingle(raw),
            CutsceneValueKind.Angle => CutsceneActions.ToDegrees((short)raw),
            _ => raw
        };
    }

    /// <summary>True when storing <paramref name="value"/> in <paramref name="field"/> would change the keyframe.</summary>
    public bool WouldChange(CutsceneField field, double value) => ToRaw(field, value) != _parameters[field.Slot];

    internal void SetValue(CutsceneField field, double value) => _parameters[field.Slot] = ToRaw(field, value);

    internal void SetParameter(int slot, int value) => _parameters[slot] = value;

    private static int ToRaw(CutsceneField field, double value) => field.Kind switch
    {
        CutsceneValueKind.Decimal => BitConverter.SingleToInt32Bits((float)value),
        CutsceneValueKind.Angle => CutsceneActions.FromDegrees(value),
        _ => checked((int)Math.Round(value))
    };

    internal CutsceneKeyframe Clone() => new(Time, Actor, Action, (int[])_parameters.Clone());

    internal void Write(byte[] data, int offset)
    {
        BitConverter.GetBytes(Time).CopyTo(data, offset);
        BitConverter.GetBytes(Actor).CopyTo(data, offset + 4);
        BitConverter.GetBytes(Action).CopyTo(data, offset + 6);
        for (var i = 0; i < 3; i++) BitConverter.GetBytes(_parameters[i]).CopyTo(data, offset + 8 + 4 * i);
    }

    internal static CutsceneKeyframe Read(byte[] data, int offset) => new(
        BitConverter.ToSingle(data, offset),
        BitConverter.ToInt16(data, offset + 4),
        BitConverter.ToInt16(data, offset + 6),
        new[]
        {
            BitConverter.ToInt32(data, offset + 8), BitConverter.ToInt32(data, offset + 12),
            BitConverter.ToInt32(data, offset + 16)
        });

    public override string ToString() => $"{Time:0.###} s: {ActorKind} {Description}";
}

/// <summary>A member of a cutscene's cast: who, where they start and which way they face.</summary>
public sealed class CutsceneActor
{
    internal const int Size = 0x2C;
    internal const int RuntimeSize = 0x08;     // +0x00 / +0x04: filled in by the game when the scene starts
    internal const int NameOffset = 0x08;
    internal const int NameSize = 0x14;
    private const int PositionOffset = 0x1C;
    private const int FacingOffset = 0x28;
    private const int CreatedOffset = 0x2B;

    private readonly byte[] _data;
    private readonly int _offset;

    internal CutsceneActor(byte[] data, int offset, int index)
    {
        _data = data;
        _offset = offset;
        Index = index;
    }

    /// <summary>Position in the cast; keyframes refer to cast members by it.</summary>
    public int Index { get; }

    /// <summary>
    /// True when the scene creates the character when it starts ("Player" and "Player2" are stand-ins
    /// for players 1 and 2, "Player3" for player 3 with ADRIANNA23; other names are object classes such
    /// as Karne). False when the scene takes an object already in the level near the start position
    /// (any character within 12 units, or an object with this name).
    /// </summary>
    public bool IsCreated => _data[_offset + CreatedOffset] != 0;

    /// <summary>The character the cast member is, e.g. "Player" or "Karne".</summary>
    public string Name
    {
        get
        {
            var start = _offset + NameOffset;
            var end = start;
            while (end < start + NameSize && _data[end] != 0) end++;
            return Encoding.ASCII.GetString(_data, start, end - start);
        }
        internal set
        {
            var bytes = Encoding.ASCII.GetBytes(value);
            for (var i = 0; i < NameSize; i++) _data[_offset + NameOffset + i] = i < bytes.Length ? bytes[i] : (byte)0;
        }
    }

    public float X
    {
        get => BitConverter.ToSingle(_data, _offset + PositionOffset);
        internal set => BitConverter.GetBytes(value).CopyTo(_data, _offset + PositionOffset);
    }

    public float Y
    {
        get => BitConverter.ToSingle(_data, _offset + PositionOffset + 4);
        internal set => BitConverter.GetBytes(value).CopyTo(_data, _offset + PositionOffset + 4);
    }

    public float Z
    {
        get => BitConverter.ToSingle(_data, _offset + PositionOffset + 8);
        internal set => BitConverter.GetBytes(value).CopyTo(_data, _offset + PositionOffset + 8);
    }

    /// <summary>Which way the cast member faces at the start, in degrees (0 to 360).</summary>
    public double Facing
    {
        get => FacingUnits * 360.0 / 65536.0;
        internal set => BitConverter.GetBytes(ToFacingUnits(value)).CopyTo(_data, _offset + FacingOffset);
    }

    /// <summary>The facing as stored: a 16-bit angle (65536 units per turn).</summary>
    public ushort FacingUnits => BitConverter.ToUInt16(_data, _offset + FacingOffset);

    /// <summary>The stored facing for <paramref name="degrees"/>.</summary>
    public static ushort ToFacingUnits(double degrees) => unchecked((ushort)CutsceneActions.FromDegrees(degrees));

    public override string ToString() => $"{Index}: {Name}";
}
