using JetBlackEngineLib.Data.CutScenes;
using JetBlackEngineLib.Data.DataContainers;
using System;
using System.IO;
using System.Linq;

namespace JetBlackEngineLib.Tests;

/// <summary>
/// Checks cutscene editing against the New Game cutscenes in
/// Test_files/CUTTOWN.GOB (skipped when that file is not present).
/// </summary>
[TestFixture]
public class EditableCutsceneTests
{
    private const string ArchiveName = "cuttown.lmp";
    private const string IntroEntry = "intro.cut";
    private const int KeyframeOffsetField = 0x04;
    private const int KeyframeCountField = 0x08;
    private const int CastOffsetField = 0x0C;
    private const int KeyframeSize = 0x14;
    private const int CastMemberSize = 0x2C;
    private const int CopiedKeyframes = 8;

    private string _gobPath = "";
    private byte[] _intro = Array.Empty<byte>();

    [OneTimeSetUp]
    public void ReadIntro()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "Test_files", "CUTTOWN.GOB")))
        {
            dir = dir.Parent;
        }

        if (dir == null)
        {
            Assert.Ignore("Test_files/CUTTOWN.GOB was not found above the test directory.");
        }

        _gobPath = Path.Combine(dir!.FullName, "Test_files", "CUTTOWN.GOB");
        _intro = Entry(OpenCutTown(new GobFile(EngineVersion.DarkAlliance, _gobPath)), IntroEntry);
    }

    private static LmpFile OpenCutTown(GobFile gob)
    {
        var lmp = gob.Directory[ArchiveName];
        lmp.ReadDirectory();
        return lmp;
    }

    private static byte[] Entry(LmpFile lmp, string name)
    {
        var entry = lmp.Directory[name];
        return lmp.FileData.AsSpan(entry.StartOffset, entry.Length).ToArray();
    }

    private byte[] Stock(string name) => Entry(OpenCutTown(new GobFile(EngineVersion.DarkAlliance, _gobPath)), name);

    private static int Field(byte[] cut, int offset) => BitConverter.ToInt32(cut, offset);

    private static int KeyframeAt(byte[] cut, int index) => Field(cut, KeyframeOffsetField) + index * KeyframeSize;

    private static int[] Differences(byte[] a, byte[] b) =>
        Enumerable.Range(0, Math.Min(a.Length, b.Length)).Where(i => a[i] != b[i]).ToArray();

    private static bool Within(int index, int start, int length) => index >= start && index < start + length;

    /// <summary>True when the file ends with a copy of its first eight keyframes.</summary>
    private static bool EndsWithCopyOfFirstKeyframes(byte[] cut)
    {
        var copy = KeyframeAt(cut, Field(cut, KeyframeCountField));
        return cut.Length - copy == CopiedKeyframes * KeyframeSize &&
               cut.AsSpan(copy).SequenceEqual(cut.AsSpan(KeyframeAt(cut, 0), CopiedKeyframes * KeyframeSize));
    }

    private static bool InTimeOrder(EditableCutscene cutscene) =>
        cutscene.Keyframes.Zip(cutscene.Keyframes.Skip(1)).All(p => p.First.Time <= p.Second.Time);

    [TestCase("flyby.cut", 31, 0)]
    [TestCase("intro.cut", 184, 6)]
    [TestCase("intro2p.cut", 134, 8)]
    public void NewGameCutscenes_AreWrittenBackUnchanged(string name, int keyframes, int cast)
    {
        var stock = Stock(name);
        var cutscene = EditableCutscene.Load(stock);

        Assert.That(cutscene.Keyframes, Has.Count.EqualTo(keyframes));
        Assert.That(cutscene.Cast, Has.Count.EqualTo(cast));
        Assert.That(InTimeOrder(cutscene), Is.True);
        Assert.That(EndsWithCopyOfFirstKeyframes(stock), Is.True);
        Assert.That(cutscene.ToArray(), Is.EqualTo(stock));
    }

    [Test]
    public void Intro_IsUnderstood()
    {
        var intro = EditableCutscene.Load(_intro);

        Assert.That(intro.GetText(CutsceneText.Subtitles), Is.EqualTo("introcut_text1"));
        Assert.That(intro.GetText(CutsceneText.Narration), Is.EqualTo("nar_bushwacked.vag"));
        Assert.That(intro.GetText(CutsceneText.Music), Is.EqualTo("mx_cs_mugging_01_l.vag"));
        Assert.That(intro.Cast.Select(a => a.Name),
            Is.EqualTo(new[] { "Player", "Thief1", "Thief2", "Karne", "NPCGuard1", "NPCGuard2" }));
        Assert.That(intro.Cast[0].Facing, Is.EqualTo(286.875));
        Assert.That(intro.Duration, Is.EqualTo(66.53f).Within(0.01f));
        Assert.That(intro.Keyframes.Count(k => k.Description == "Next subtitle"), Is.EqualTo(14));
        Assert.That(intro.Keyframes.Count(k => k.Description == "Move to"), Is.EqualTo(29));
        Assert.That(intro.Keyframes.Where(k => k.Description is "Turn" or "Tilt")
                .All(k => k.Parameters[0] is >= short.MinValue and <= short.MaxValue), Is.True,
            "camera angles are stored as 16-bit numbers");
    }

    [Test]
    public void ValueEdit_ChangesTheKeyframeAndItsCopy()
    {
        var intro = EditableCutscene.Load(_intro);
        var camera = intro.Keyframes[2];
        Assert.That(camera.Description, Is.EqualTo("Position"));
        var x = camera.Fields[0];

        intro.SetValue(camera, x, 200);
        var edited = intro.ToArray();

        // X is the keyframe's first parameter, 8 bytes in; keyframe 2 is also in the copy at the end.
        var inKeyframe = KeyframeAt(_intro, 2) + 8;
        var inCopy = KeyframeAt(_intro, intro.Keyframes.Count + 2) + 8;
        Assert.That(edited, Has.Length.EqualTo(_intro.Length));
        Assert.That(Differences(_intro, edited), Is.Not.Empty);
        Assert.That(Differences(_intro, edited).All(i => Within(i, inKeyframe, 4) || Within(i, inCopy, 4)), Is.True,
            "only X and its copy changed");
        Assert.That(EndsWithCopyOfFirstKeyframes(edited), Is.True);
        Assert.That(CutDecoder.Decode(edited, 0, edited.Length).keyframes[2].i8, Is.EqualTo(200));
        Assert.That(EditableCutscene.Load(edited).Keyframes[2].GetValue(x), Is.EqualTo(200));
    }

    [Test]
    public void AngleEdit_IsStoredTheWayTheGameStoresAngles()
    {
        var intro = EditableCutscene.Load(_intro);
        var turn = intro.Keyframes.First(k => k.Description == "Turn");
        var angle = turn.Fields.Single();
        var others = turn.Parameters.Skip(1).ToArray();

        intro.SetValue(turn, angle, -90);
        Assert.That(turn.Parameters[0], Is.EqualTo(-16384));
        Assert.That(turn.GetValue(angle), Is.EqualTo(-90));

        Assert.That(turn.WouldChange(angle, 270), Is.False, "270° faces the same way as -90°");
        intro.SetValue(turn, angle, 45.5);
        Assert.That(turn.GetValue(angle), Is.EqualTo(45.5).Within(360.0 / 65536));
        Assert.That(turn.Parameters.Skip(1), Is.EqualTo(others), "the other two numbers are kept");
    }

    [TestCase(0, 0)]
    [TestCase(90, 16384)]
    [TestCase(-90, -16384)]
    [TestCase(270, -16384)]
    [TestCase(-270, 16384)]
    [TestCase(180, -32768)]
    [TestCase(-180, -32768)]
    [TestCase(359.99, -2)]
    [TestCase(720, 0)]
    public void Angles_AreSixteenBitNumbers(double degrees, int units)
    {
        Assert.That(CutsceneActions.FromDegrees(degrees), Is.EqualTo(units));
        Assert.That(CutsceneActor.ToFacingUnits(degrees), Is.EqualTo(unchecked((ushort)units)));
    }

    [Test]
    public void DecimalEdit_IsStoredAsAFloat()
    {
        var flyby = EditableCutscene.Load(Stock("flyby.cut"));
        var keyframe = flyby.Keyframes.First(k => k.ActorKind == CutsceneActorKind.Camera && k.Action == 5);
        var value = keyframe.Fields.Single();
        Assert.That(value.Kind, Is.EqualTo(CutsceneValueKind.Decimal));

        flyby.SetValue(keyframe, value, 12.5);
        var edited = flyby.ToArray();

        Assert.That(keyframe.Parameters[0], Is.EqualTo(BitConverter.SingleToInt32Bits(12.5f)));
        Assert.That(CutDecoder.Decode(edited, 0, edited.Length).keyframes[flyby.IndexOf(keyframe)].f8, Is.EqualTo(12.5f));
    }

    [Test]
    public void TimeEdit_KeepsTheKeyframesInTimeOrder()
    {
        var intro = EditableCutscene.Load(_intro);
        var first = intro.Keyframes[0];
        var move = intro.Keyframes.Last(k => k.Description == "Move to");

        intro.SetTime(move, 0);
        Assert.That(intro.IndexOf(move), Is.EqualTo(intro.Keyframes.Count(k => k.Time == 0) - 1),
            "a moved keyframe goes after the others at the same time");

        intro.SetTime(first, 30.5);
        var edited = intro.ToArray();

        Assert.That(InTimeOrder(intro), Is.True);
        Assert.That(intro.Keyframes[intro.IndexOf(first) - 1].Time, Is.LessThanOrEqualTo(30.5f));
        Assert.That(intro.Keyframes[intro.IndexOf(first) + 1].Time, Is.GreaterThan(30.5f));
        Assert.That(edited, Has.Length.EqualTo(_intro.Length));
        Assert.That(EndsWithCopyOfFirstKeyframes(edited), Is.True, "the copy follows the new first keyframes");
        Assert.That(EditableCutscene.Load(edited).Keyframes.Select(k => k.ToString()),
            Is.EqualTo(intro.Keyframes.Select(k => k.ToString())));
    }

    [Test]
    public void CopyAndDelete_ResizeTheCutscene()
    {
        var intro = EditableCutscene.Load(_intro);
        var count = intro.Keyframes.Count;
        var move = intro.Keyframes.First(k => k.Description == "Move to");
        var x = move.Fields[0];

        var copy = intro.Duplicate(move);
        var grown = intro.ToArray();

        Assert.That(intro.Keyframes[intro.IndexOf(move) + 1], Is.SameAs(copy));
        Assert.That(copy.ToString(), Is.EqualTo(move.ToString()));
        Assert.That(copy.Parameters, Is.EqualTo(move.Parameters));
        Assert.That(grown, Has.Length.EqualTo(_intro.Length + KeyframeSize));
        Assert.That(Field(grown, KeyframeCountField), Is.EqualTo(count + 1));
        Assert.That(EndsWithCopyOfFirstKeyframes(grown), Is.True);
        var decoded = CutDecoder.Decode(grown, 0, grown.Length);
        Assert.That(decoded.keyframes, Has.Count.EqualTo(count + 1));
        Assert.That(decoded.Cast, Has.Count.EqualTo(6));

        // The copy is a keyframe of its own.
        intro.SetValue(copy, x, move.GetValue(x) + 100);
        intro.SetTime(copy, move.Time + 1);
        Assert.That(copy.GetValue(x), Is.EqualTo(move.GetValue(x) + 100));

        intro.Remove(copy);
        Assert.That(intro.ToArray(), Is.EqualTo(_intro), "deleting the copy gives back the scene as it was");

        intro.Remove(intro.Keyframes[0]);
        var shrunk = intro.ToArray();

        Assert.That(shrunk, Has.Length.EqualTo(_intro.Length - KeyframeSize));
        Assert.That(Field(shrunk, KeyframeCountField), Is.EqualTo(count - 1));
        Assert.That(EndsWithCopyOfFirstKeyframes(shrunk), Is.True);
        Assert.That(EditableCutscene.Load(shrunk).Keyframes, Has.Count.EqualTo(count - 1));
    }

    [Test]
    public void TheLastKeyframe_IsKept()
    {
        var flyby = EditableCutscene.Load(Stock("flyby.cut"));
        while (flyby.Keyframes.Count > 1)
        {
            flyby.Remove(flyby.Keyframes[flyby.Keyframes.Count - 1]);
        }

        Assert.Throws<InvalidOperationException>(() => flyby.Remove(flyby.Keyframes[0]));
        Assert.That(EditableCutscene.Load(flyby.ToArray()).Keyframes, Has.Count.EqualTo(1));
    }

    [Test]
    public void HeaderAndCastEdits_ChangeOnlyThoseValues()
    {
        var intro = EditableCutscene.Load(_intro);
        var player = intro.Cast[0];

        intro.SetText(CutsceneText.Music, "mx_env_elfsong_l.vag");
        intro.SetText(CutsceneText.Narration, "");
        intro.SetPosition(0, -1000.5f);
        intro.SetName(intro.Cast[3], "Karne2");
        intro.SetPosition(player, 1, 1500.25f);
        intro.SetFacing(player, 90);
        var edited = intro.ToArray();

        var decoded = CutDecoder.Decode(edited, 0, edited.Length);
        Assert.That(decoded.subtitles, Is.EqualTo("introcut_text1"));
        Assert.That(decoded.string48, Is.Empty);
        Assert.That(decoded.string74, Is.EqualTo("mx_env_elfsong_l.vag"));
        Assert.That(decoded.Cast[3].name, Is.EqualTo("Karne2"));
        Assert.That(decoded.Cast[0].y, Is.EqualTo(1500.25f));
        Assert.That((ushort)decoded.Cast[0].s28, Is.EqualTo(0x4000));

        var reread = EditableCutscene.Load(edited);
        Assert.That(reread.PositionX, Is.EqualTo(-1000.5f));
        Assert.That(reread.Cast[0].Facing, Is.EqualTo(90));
        Assert.That(reread.Cast[3].Name, Is.EqualTo("Karne2"));

        // The rest of a shortened name is cleared.
        Assert.That(edited.AsSpan(0x74 + 20, 0x28 - 20).ToArray(), Has.All.EqualTo((byte)0));

        // The keyframes, the other cast members and the rest of the player are untouched.
        var keyframes = Field(_intro, KeyframeOffsetField);
        Assert.That(edited[keyframes..], Is.EqualTo(_intro[keyframes..]));
        var cast = Field(_intro, CastOffsetField);
        foreach (var other in new[] { 1, 2, 4, 5 })
        {
            var start = cast + other * CastMemberSize;
            Assert.That(edited[start..(start + CastMemberSize)], Is.EqualTo(_intro[start..(start + CastMemberSize)]));
        }

        Assert.That(Differences(_intro, edited).Where(i => Within(i, cast, CastMemberSize)).All(i =>
            Within(i, cast + 0x20, 4) || Within(i, cast + 0x28, 2)), Is.True, "only the player's Y and facing changed");
    }

    [Test]
    public void InvalidEdits_AreRejectedWithoutChangingTheCutscene()
    {
        var intro = EditableCutscene.Load(_intro);
        var other = EditableCutscene.Load(_intro);
        var camera = intro.Keyframes[2];
        var subtitle = intro.Keyframes.First(k => k.Description == "Next subtitle");
        var tooLong = new string('a', EditableCutscene.MaxTextLength + 1);

        Assert.Throws<ArgumentException>(() => intro.SetText(CutsceneText.Music, tooLong));
        Assert.Throws<ArgumentException>(() => intro.SetText(CutsceneText.Subtitles, "café"));
        Assert.Throws<ArgumentOutOfRangeException>(() => intro.SetPosition(3, 0));
        Assert.Throws<ArgumentException>(() => intro.SetPosition(0, float.PositiveInfinity));
        Assert.Throws<ArgumentException>(() => intro.SetName(intro.Cast[0], ""));
        Assert.Throws<ArgumentException>(() =>
            intro.SetName(intro.Cast[0], new string('a', EditableCutscene.MaxNameLength + 1)));
        Assert.Throws<ArgumentException>(() => intro.SetName(other.Cast[0], "Player"));
        Assert.Throws<ArgumentException>(() => intro.SetFacing(intro.Cast[0], double.NaN));
        Assert.Throws<ArgumentException>(() => intro.SetTime(camera, -1));
        Assert.Throws<ArgumentException>(() => intro.SetTime(camera, double.NaN));
        Assert.Throws<ArgumentException>(() => intro.SetTime(camera, 3601));
        Assert.Throws<ArgumentException>(() => intro.SetTime(other.Keyframes[0], 1));
        Assert.Throws<ArgumentException>(() => intro.SetValue(camera, camera.Fields[0], 1e12));
        Assert.Throws<ArgumentException>(() => intro.SetValue(subtitle, camera.Fields[0], 1));
        Assert.Throws<ArgumentException>(() => intro.Duplicate(other.Keyframes[0]));
        Assert.Throws<ArgumentException>(() => intro.Remove(other.Keyframes[0]));

        Assert.That(intro.ToArray(), Is.EqualTo(_intro));
    }

    [Test]
    public void UnreadableCutscenes_AreRefused()
    {
        Assert.That(EditableCutscene.TryLoad(Array.Empty<byte>(), out _, out _), Is.False);
        Assert.That(EditableCutscene.TryLoad(_intro[..0x200], out _, out var truncated), Is.False);
        Assert.That(truncated, Does.Contain("keyframes"));

        var missingActor = (byte[])_intro.Clone();
        BitConverter.GetBytes((short)9).CopyTo(missingActor, KeyframeAt(_intro, 0) + 4);
        Assert.That(EditableCutscene.TryLoad(missingActor, out _, out var actor), Is.False);
        Assert.That(actor, Does.Contain("cast member 9"));

        var noTime = (byte[])_intro.Clone();
        BitConverter.GetBytes(float.NaN).CopyTo(noTime, KeyframeAt(_intro, 0));
        Assert.That(EditableCutscene.TryLoad(noTime, out _, out var time), Is.False);
        Assert.That(time, Does.Contain("time"));

        var castOverKeyframes = (byte[])_intro.Clone();
        BitConverter.GetBytes(200).CopyTo(castOverKeyframes, 0x10);
        Assert.That(EditableCutscene.TryLoad(castOverKeyframes, out _, out var cast), Is.False);
        Assert.That(cast, Does.Contain("cast"));
    }

    [Test]
    public void EditedCutscene_IsSavedIntoTheGob()
    {
        var gob = new GobFile(EngineVersion.DarkAlliance, _gobPath);
        var lmp = OpenCutTown(gob);
        var intro = EditableCutscene.Load(Entry(lmp, IntroEntry));
        intro.SetText(CutsceneText.Music, "mx_env_elfsong_l.vag");
        intro.SetFacing(intro.Cast[0], 90);
        intro.Remove(intro.Keyframes.First(k => k.Description == "Next subtitle"));
        var edited = intro.ToArray();

        lmp.ReplaceEntry(IntroEntry, edited);
        var packed = GobWriter.TryPatchInPlace(gob) ?? GobWriter.Pack(gob);

        var saved = new GobFile(EngineVersion.DarkAlliance, "saved.gob", packed);
        var savedLmp = OpenCutTown(saved);
        Assert.That(Entry(savedLmp, IntroEntry), Is.EqualTo(edited));
        foreach (var name in lmp.Directory.Keys.Where(n => n != IntroEntry))
        {
            Assert.That(Entry(savedLmp, name), Is.EqualTo(Entry(lmp, name)), $"{name} is unchanged");
        }

        var reread = EditableCutscene.Load(Entry(savedLmp, IntroEntry));
        Assert.That(reread.GetText(CutsceneText.Music), Is.EqualTo("mx_env_elfsong_l.vag"));
        Assert.That(reread.Cast[0].Facing, Is.EqualTo(90));
        Assert.That(reread.Keyframes, Has.Count.EqualTo(183));
    }
}
