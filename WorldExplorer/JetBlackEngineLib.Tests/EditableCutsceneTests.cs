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

    // ─────────────────────────────────────────────────────────────────────────
    // Cast: copy and remove
    // ─────────────────────────────────────────────────────────────────────────

    private const int CastCountField = 0x10;

    /// <summary>The keyframes as (time, actor, action, parameters) for comparing scenes.</summary>
    private static (float, short, short, int, int, int)[] KeyframeData(EditableCutscene cutscene) =>
        cutscene.Keyframes.Select(k => (k.Time, k.Actor, k.Action, k.Parameters[0], k.Parameters[1], k.Parameters[2])).ToArray();

    [Test]
    public void CopyNames_NumberThePlayersOnly()
    {
        var intro = EditableCutscene.Load(_intro);

        Assert.That(intro.SuggestCopyName(intro.Cast[0]), Is.EqualTo("Player2"));
        Assert.That(intro.SuggestCopyName(intro.Cast[1]), Is.EqualTo("Thief1"), "another Thief1: Thief2 and Thief3 are other characters");
        Assert.That(intro.SuggestCopyName(intro.Cast[3]), Is.EqualTo("Karne"));
        intro.CopyActor(intro.Cast[0], "Player2", 0, 0, 0, withKeyframes: false);
        Assert.That(intro.SuggestCopyName(intro.Cast[0]), Is.EqualTo("Player3"));
        Assert.That(intro.SuggestCopyName(intro.Cast[6]), Is.EqualTo("Player3"));
        intro.CopyActor(intro.Cast[0], "Player3", 0, 0, 0, withKeyframes: false);
        Assert.That(intro.SuggestCopyName(intro.Cast[7]), Is.EqualTo("Player3"), "with all three players, a copy keeps its name");
        intro.RemoveActor(intro.Cast[6]);
        Assert.That(intro.SuggestCopyName(intro.Cast[0]), Is.EqualTo("Player2"), "a free player name is used again");
        intro.SetName(intro.Cast[3], "player");
        Assert.That(intro.SuggestCopyName(intro.Cast[3]), Is.EqualTo("player"), "the game compares player names exactly");
    }

    [Test]
    public void CopiedPlayers_GoAtTheEnd_WithTheirKeyframes()
    {
        var intro = EditableCutscene.Load(_intro);
        var before = KeyframeData(intro);
        var player = intro.Keyframes.Where(k => k.Actor == 0).ToList();

        var player2 = intro.CopyActor(intro.Cast[0], "Player2", -32, 0, 0, withKeyframes: true);
        var player3 = intro.CopyActor(intro.Cast[0], "Player3", 32, 0, 0, withKeyframes: true);
        var edited = intro.ToArray();

        Assert.That(intro.Cast.Select(a => a.Name),
            Is.EqualTo(new[] { "Player", "Thief1", "Thief2", "Karne", "NPCGuard1", "NPCGuard2", "Player2", "Player3" }));
        Assert.That((player2.Index, player3.Index), Is.EqualTo((6, 7)));
        Assert.That((player2.X, player2.Y, player2.Z), Is.EqualTo((intro.Cast[0].X - 32, intro.Cast[0].Y, intro.Cast[0].Z)));
        Assert.That(player3.X, Is.EqualTo(intro.Cast[0].X + 32));
        Assert.That((player2.Facing, player3.Facing), Is.EqualTo((intro.Cast[0].Facing, intro.Cast[0].Facing)));
        Assert.That(player2.IsCreated && player3.IsCreated, Is.True, "created by the scene, like the player");

        // Header: two more cast members, the keyframes 2 x 0x2C further on, 2 x 9 more keyframes.
        Assert.That(Field(edited, CastCountField), Is.EqualTo(8));
        Assert.That(Field(edited, KeyframeOffsetField), Is.EqualTo(Field(_intro, KeyframeOffsetField) + 2 * CastMemberSize));
        Assert.That(Field(edited, KeyframeCountField), Is.EqualTo(184 + 2 * player.Count));
        Assert.That(EndsWithCopyOfFirstKeyframes(edited), Is.True);

        // Everything before the new members is as it was, apart from the two header numbers.
        var castEnd = Field(_intro, CastOffsetField) + 6 * CastMemberSize;
        Assert.That(Differences(_intro.AsSpan(0, castEnd).ToArray(), edited.AsSpan(0, castEnd).ToArray()).All(i =>
            Within(i, KeyframeOffsetField, 4) || Within(i, KeyframeCountField, 4) || Within(i, CastCountField, 4)), Is.True);
        Assert.That(edited.AsSpan(castEnd, 8).ToArray(), Has.All.EqualTo((byte)0), "the game fills in +0 and +4 itself");

        // The old keyframes keep their order; each of the player's is followed by its two copies.
        Assert.That(KeyframeData(intro).Where(k => k.Item2 < 6), Is.EqualTo(before));
        foreach (var keyframe in player)
        {
            var at = intro.IndexOf(keyframe);
            var copies = new[] { intro.Keyframes[at + 1], intro.Keyframes[at + 2] };
            Assert.That(copies.Select(k => k.Actor), Is.EqualTo(new short[] { 6, 7 }));
            Assert.That(copies.All(k => k.Time == keyframe.Time && k.Action == keyframe.Action), Is.True);
            var dx = keyframe.Description == "Move to" ? 32 : 0;
            Assert.That(copies[0].Parameters, Is.EqualTo(new[] { keyframe.Parameters[0] - dx, keyframe.Parameters[1], keyframe.Parameters[2] }));
            Assert.That(copies[1].Parameters, Is.EqualTo(new[] { keyframe.Parameters[0] + dx, keyframe.Parameters[1], keyframe.Parameters[2] }));
        }

        var reread = EditableCutscene.Load(edited);
        Assert.That(reread.ToArray(), Is.EqualTo(edited));
        Assert.That(CutDecoder.Decode(edited, 0, edited.Length).Cast, Has.Count.EqualTo(8));
    }

    [Test]
    public void CopyThenRemove_GivesBackTheScene()
    {
        var intro = EditableCutscene.Load(_intro);
        var karne = intro.Cast[3];
        var (x, y, z) = (karne.X, karne.Y, karne.Z);
        var copy = intro.CopyActor(karne, intro.SuggestCopyName(karne), 10, -20, 2, withKeyframes: true);
        Assert.That(copy.Name, Is.EqualTo("Karne"));
        Assert.That((copy.X, copy.Y, copy.Z), Is.EqualTo((x + 10, y - 20, z + 2)));
        Assert.That(intro.Keyframes.Count(k => k.Actor == 6), Is.EqualTo(intro.Keyframes.Count(k => k.Actor == 3)));
        var moves = intro.Keyframes.Where(k => k.Actor == 3 && k.Description == "Move to").ToList();
        Assert.That(moves, Has.Count.EqualTo(3));
        Assert.That(intro.Keyframes.Where(k => k.Actor == 6 && k.Description == "Move to").SelectMany(k => k.Parameters),
            Is.EqualTo(moves.SelectMany(k => new[] { k.Parameters[0] + 10, k.Parameters[1] - 20, k.Parameters[2] + 2 })));

        intro.RemoveActor(copy);
        Assert.That(intro.ToArray(), Is.EqualTo(_intro));
    }

    /// <summary>
    /// What removing cast member <paramref name="index"/> should leave: its keyframes gone, the members
    /// after it one place up, and "Face" keyframes that turned towards it turning towards no one.
    /// (Only "Face" and the camera's "Look at" name another cast member, and no shipped scene has a "Look at":
    /// the game's other actions take positions, animations or numbers.)
    /// </summary>
    private static (float, short, short, int, int, int)[] AfterRemoving((float, short, short, int, int, int)[] keyframes, int index) =>
        keyframes.Where(k => k.Item2 != index).Select(k =>
        {
            if (k.Item2 < 0) return k;
            var actor = (short)(k.Item2 > index ? k.Item2 - 1 : k.Item2);
            var target = k.Item3 != 1 || k.Item4 < 0 ? k.Item4 : k.Item4 == index ? -1 : k.Item4 > index ? k.Item4 - 1 : k.Item4;
            return (k.Item1, actor, k.Item3, target, k.Item5, k.Item6);
        }).ToArray();

    private static int?[] FaceTargetsOf(EditableCutscene cutscene, int actor) =>
        cutscene.Keyframes.Where(k => k.Actor == actor && k.FaceTarget != null).Select(k => k.FaceTarget).ToArray();

    [Test]
    public void RemovingACastMember_RenumbersTheOthers()
    {
        var stock = Stock("intro2p.cut");
        var original = EditableCutscene.Load(stock);
        var before = KeyframeData(original);
        var names = original.Cast.Select(a => a.Name).ToList();
        Assert.That(names, Is.EqualTo(new[] { "Thief", "Thief", "Thief", "Player", "Karne", "Guard", "Guard", "Player2" }));
        // The first two thieves turn towards the player (3); then the first turns towards player 2 (7).
        Assert.That(FaceTargetsOf(original, 0), Is.EqualTo(new int?[] { 3, 7, -1, -1 }));
        Assert.That(FaceTargetsOf(original, 1), Is.EqualTo(new int?[] { 3, -1, -1 }));

        for (var index = 0; index < names.Count; index++)
        {
            var scene = EditableCutscene.Load(stock);
            scene.RemoveActor(scene.Cast[index]);
            var edited = scene.ToArray();

            Assert.That(scene.Cast.Select(a => a.Name), Is.EqualTo(names.Where((_, i) => i != index)), $"removing {index}");
            Assert.That(scene.Cast.Select(a => a.Index), Is.EqualTo(Enumerable.Range(0, names.Count - 1)));
            Assert.That(KeyframeData(scene), Is.EqualTo(AfterRemoving(before, index)), $"removing {index}");
            Assert.That(Field(edited, CastCountField), Is.EqualTo(names.Count - 1));
            Assert.That(Field(edited, KeyframeOffsetField), Is.EqualTo(Field(stock, KeyframeOffsetField) - CastMemberSize));
            Assert.That(Field(edited, KeyframeCountField), Is.EqualTo(before.Length - before.Count(k => k.Item2 == index)));
            Assert.That(EndsWithCopyOfFirstKeyframes(edited), Is.True, $"removing {index}");
            Assert.That(EditableCutscene.Load(edited).ToArray(), Is.EqualTo(edited));

            switch (names[index], index)
            {
                case ("Thief", 0):
                    Assert.That(FaceTargetsOf(scene, 0), Is.EqualTo(new int?[] { 2, -1, -1 }), "the second thief is first now");
                    break;
                case ("Player", _):
                    Assert.That(FaceTargetsOf(scene, 0), Is.EqualTo(new int?[] { -1, 6, -1, -1 }), "no player to turn to; player 2 is 6");
                    Assert.That(FaceTargetsOf(scene, 1), Is.EqualTo(new int?[] { -1, -1, -1 }));
                    break;
                case ("Player2", _):
                    Assert.That(FaceTargetsOf(scene, 0), Is.EqualTo(new int?[] { 3, -1, -1, -1 }), "no player 2 to turn to");
                    break;
            }
        }
    }

    [Test]
    public void FaceOrLookAt_AtAPlayerWhoIsNotInEveryGame_IsWarnedAbout()
    {
        var stock = Stock("intro2p.cut");
        var scene = EditableCutscene.Load(stock);
        // intro2p.cut (which the game never plays): the first thief turns towards Player (3), then Player2 (7)
        var warned = scene.Keyframes.Where(k => scene.TargetWarning(k) != null).ToList();
        Assert.That(warned.Select(k => k.FaceTarget), Is.EqualTo(new int?[] { 7 }));
        Assert.That(scene.TargetWarning(warned[0]), Does.Contain("Player2 is there only in games with two or three players"));
        Assert.That(scene.TargetWarning(warned[0]), Does.Contain("address 0"));

        scene.SetName(scene.Cast[7], "Player3");
        Assert.That(scene.TargetWarning(warned[0]), Does.Contain("only in three-player games"));
        scene.SetName(scene.Cast[7], "Karne");
        Assert.That(scene.TargetWarning(warned[0]), Is.Null, "Karne is made in every game");

        // a camera "Look at" aimed at Player2, made from the first camera keyframe (and its copy at the end)
        var cut = (byte[])stock.Clone();
        var count = Field(cut, KeyframeCountField);
        var camera = Enumerable.Range(0, count).First(i => BitConverter.ToInt16(cut, KeyframeAt(cut, i) + 4) == -100);
        foreach (var at in camera < CopiedKeyframes
                     ? new[] { KeyframeAt(cut, camera), KeyframeAt(cut, count + camera) }
                     : new[] { KeyframeAt(cut, camera) })
        {
            BitConverter.GetBytes((short)3).CopyTo(cut, at + 6);
            BitConverter.GetBytes(7).CopyTo(cut, at + 8);
        }

        var looking = EditableCutscene.Load(cut);
        var look = looking.Keyframes.Single(k => k.LookAtTarget != null);
        Assert.That(looking.TargetWarning(look), Does.Contain("this Look at follows no one"));
        Assert.That(looking.TargetWarning(look), Does.Contain("turn and tilt"));

        // a member taken from the level (not made by the scene) isn't a player stand-in
        cut[Field(cut, CastOffsetField) + 7 * CastMemberSize + 0x2B] = 0;
        var fromLevel = EditableCutscene.Load(cut);
        Assert.That(fromLevel.TargetWarning(fromLevel.Keyframes.Single(k => k.LookAtTarget != null)), Is.Null);
    }

    [Test]
    public void CastLimits_AreChecked()
    {
        var intro = EditableCutscene.Load(_intro);
        Assert.Throws<ArgumentException>(() => intro.CopyActor(intro.Cast[0], "", 0, 0, 0, true));
        Assert.Throws<ArgumentException>(() => intro.CopyActor(intro.Cast[0], new string('P', EditableCutscene.MaxNameLength + 1), 0, 0, 0, true));
        Assert.Throws<ArgumentException>(() => intro.CopyActor(intro.Cast[0], "Player2", float.NaN, 0, 0, true));
        var other = EditableCutscene.Load(Stock("intro2p.cut"));
        Assert.Throws<ArgumentException>(() => intro.CopyActor(other.Cast[0], "Thief", 0, 0, 0, true));
        Assert.Throws<ArgumentException>(() => intro.RemoveActor(other.Cast[0]));
        Assert.That(intro.ToArray(), Is.EqualTo(_intro), "nothing changed");

        while (intro.Cast.Count < EditableCutscene.MaxCast) intro.CopyActor(intro.Cast[1], "Thief3", 1, 1, 0, false);
        Assert.That(intro.CheckCopy(intro.Cast[0], true), Is.Not.Null);
        Assert.Throws<InvalidOperationException>(() => intro.CopyActor(intro.Cast[0], "Player2", 0, 0, 0, true));

        // A member that has every keyframe can't be removed: a cutscene needs at least one.
        var flyby = EditableCutscene.Load(Stock("flyby.cut"));
        Assert.That(flyby.Cast, Is.Empty);
        var lonely = EditableCutscene.Load(_intro);
        foreach (var keyframe in lonely.Keyframes.Where(k => k.Actor != 3).ToList()) lonely.Remove(keyframe);
        Assert.That(lonely.CheckRemove(lonely.Cast[3]), Is.Not.Null);
        Assert.Throws<InvalidOperationException>(() => lonely.RemoveActor(lonely.Cast[3]));
        Assert.That(lonely.CheckRemove(lonely.Cast[0]), Is.Null, "a member without keyframes can go");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Animations
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Every "Play animation" keyframe of the stock intro, in time order: who, the animation file, the mode.</summary>
    private static readonly (string Who, string File, int Mode)[] IntroAnimations =
    {
        ("Thief1", "thv1_headcrak.anm", 3), ("Player", "nw_deatha.anm", 1), ("Thief1", "tf1_01a.anm", 4),
        ("Karne", "karn04a.anm", 4), ("Thief1", "tf1_01b.anm", 4), ("Thief2", "tf2_01.anm", 4),
        ("Karne", "karn04b.anm", 4), ("NPCGuard1", "cw1_02.anm", 4), ("NPCGuard2", "cw2_01.anm", 4),
        ("Player", "nw_idleb.anm", 2), ("Player", "nw_getup.anm", 0), ("NPCGuard1", "cw1_03.anm", 4),
        ("NPCGuard2", "cw2_02.anm", 4), ("NPCGuard1", "cw1_04.anm", 4), ("NPCGuard2", "cw2_03.anm", 4),
        ("NPCGuard1", "cw1_05.anm", 4), ("NPCGuard2", "cw2_04.anm", 4)
    };

    [Test]
    public void IntroAnimations_AreNamedByTheirHash()
    {
        var intro = EditableCutscene.Load(_intro);
        var plays = intro.Keyframes.Where(k => k.IsPlayAnimation).ToList();

        Assert.That(plays.Select(k => (intro.Cast[k.Actor].Name, k.AnimationHash, k.AnimationMode)),
            Is.EqualTo(IntroAnimations.Select(a => (a.Who, (int?)CutsceneAnimations.Hash(a.File), (int?)a.Mode))));
        Assert.That(plays.All(k => k.AnimationPosition == null && k.Description == "Play animation"), Is.True);
        Assert.That(intro.Keyframes.Where(k => !k.IsPlayAnimation)
            .All(k => k.AnimationHash == null && k.AnimationPosition == null && k.AnimationMode == null), Is.True);
        Assert.That(plays[1].RawText, Is.EqualTo("animation 0x603A8D34, mode 1"));

        // intro2p.cut (which the game never plays) picks both players' fall by position: the first in their list.
        var twoPlayers = EditableCutscene.Load(Stock("intro2p.cut"));
        var falls = twoPlayers.Keyframes.Where(k => k.IsPlayAnimation).ToList();
        Assert.That(falls.Select(k => (twoPlayers.Cast[k.Actor].Name, k.AnimationPosition, k.AnimationMode, k.AnimationHash)),
            Is.EqualTo(new[] { ("Player", (int?)0, (int?)1, (int?)null), ("Player2", (int?)0, (int?)1, (int?)null) }));
        Assert.That(falls[0].RawText, Is.EqualTo("animation #0 in the list, mode 1"));
    }

    [Test]
    public void AnimationEdits_ChangeOnlyThatKeyframe()
    {
        var intro = EditableCutscene.Load(_intro);
        var fall = intro.Keyframes.First(k => k.IsPlayAnimation && intro.Cast[k.Actor].Name == "Player");
        var index = intro.IndexOf(fall);
        var values = KeyframeAt(_intro, index) + 8;

        intro.SetAnimation(fall, "nw_idleb");
        Assert.That(fall.Parameters, Is.EqualTo(new[] { CutsceneAnimations.Hash("nw_idleb.anm"), 1, CutsceneAnimations.ByName }),
            "the name's hash, the mode kept");
        intro.SetAnimationMode(fall, 2);
        Assert.That(fall.AnimationMode, Is.EqualTo(2));
        var edited = intro.ToArray();

        Assert.That(edited, Has.Length.EqualTo(_intro.Length));
        Assert.That(Differences(_intro, edited).All(i => Within(i, values, 8)), Is.True, "only its first two values changed");
        Assert.That(EditableCutscene.Load(edited).Keyframes[index].AnimationHash, Is.EqualTo(CutsceneAnimations.Hash("nw_idleb.anm")));

        intro.SetAnimation(fall, "nw_deatha.anm");
        intro.SetAnimationMode(fall, 1);
        Assert.That(intro.ToArray(), Is.EqualTo(_intro), "back to the stock fall");

        intro.SetAnimationPosition(fall, 5);
        Assert.That((fall.AnimationPosition, fall.AnimationHash, fall.Parameters[2]), Is.EqualTo(((int?)5, (int?)null, 0)));
        intro.SetAnimationHash(fall, CutsceneAnimations.Hash("nw_deatha.anm"));
        Assert.That(intro.ToArray(), Is.EqualTo(_intro), "named by hash again");

        // A keyframe that picks by position keeps its third value when it gets another position.
        var twoPlayers = EditableCutscene.Load(Stock("intro2p.cut"));
        var first = twoPlayers.Keyframes.First(k => k.IsPlayAnimation);
        var third = first.Parameters[2];
        twoPlayers.SetAnimationPosition(first, 3);
        Assert.That(first.Parameters, Is.EqualTo(new[] { 3, 1, third }));
    }

    [Test]
    public void InvalidAnimationEdits_AreRejected()
    {
        var intro = EditableCutscene.Load(_intro);
        var other = EditableCutscene.Load(_intro);
        var fall = intro.Keyframes.First(k => k.IsPlayAnimation);
        var move = intro.Keyframes.First(k => k.Description == "Move to");
        var longest = new string('a', CutsceneAnimations.MaxFileNameLength - 4);

        Assert.That(EditableCutscene.CheckAnimationName("nw_deatha"), Is.Null);
        Assert.That(EditableCutscene.CheckAnimationName(longest), Is.Null);
        Assert.That(EditableCutscene.CheckAnimationName(longest + ".anm"), Is.Null);
        Assert.That(EditableCutscene.CheckAnimationName(longest + "a"), Is.Not.Null);
        Assert.That(EditableCutscene.CheckAnimationMode(5), Is.Null);
        Assert.Throws<ArgumentException>(() => intro.SetAnimation(fall, ""));
        Assert.Throws<ArgumentException>(() => intro.SetAnimation(fall, "nw death"));
        Assert.Throws<ArgumentException>(() => intro.SetAnimation(fall, "café"));
        Assert.Throws<ArgumentException>(() => intro.SetAnimation(fall, longest + "a"));
        Assert.Throws<ArgumentException>(() => intro.SetAnimation(move, "nw_deatha"));
        Assert.Throws<ArgumentException>(() => intro.SetAnimationHash(move, 1));
        Assert.Throws<ArgumentException>(() => intro.SetAnimationPosition(move, 1));
        Assert.Throws<ArgumentException>(() => intro.SetAnimationMode(move, 1));
        Assert.Throws<ArgumentException>(() => intro.SetAnimation(other.Keyframes.First(k => k.IsPlayAnimation), "nw_idleb"));
        Assert.Throws<ArgumentException>(() => intro.SetAnimationMode(fall, 6));
        Assert.Throws<ArgumentException>(() => intro.SetAnimationMode(fall, -1));
        Assert.Throws<ArgumentException>(() => intro.SetAnimationPosition(fall, -1));

        Assert.That(intro.ToArray(), Is.EqualTo(_intro), "nothing changed");
    }

    [Test]
    public void SceneAndCameraActions_AreNamedForWhatTheGameDoes()
    {
        var intro = EditableCutscene.Load(_intro);
        int Count(string description) => intro.Keyframes.Count(k => k.Description == description);

        // Fourteen lines, each shown, its voice started (by a gesture or the scene) and waited for.
        Assert.That(Count("Next subtitle"), Is.EqualTo(14));
        Assert.That(Count("Wait for the voice line"), Is.EqualTo(14));
        Assert.That(Count("Hide subtitle"), Is.EqualTo(1));
        Assert.That(Count("Next voice line"), Is.EqualTo(2), "the narration, and the guard's Halt! (no gesture)");
        Assert.That(intro.Keyframes.Count(k => k.IsPlayAnimation && k.AnimationMode == 4), Is.EqualTo(13), "gestures: the other 13 lines");
        Assert.That(Count("Next music part"), Is.EqualTo(3));
        Assert.That(Count("Cut"), Is.EqualTo(13));
        Assert.That(Count("Smooth stop"), Is.EqualTo(9));
        Assert.That(intro.Keyframes.All(k => CutsceneActions.IsKnown(k.ActorKind, k.Action)), Is.True,
            "every action the intro uses is named");
        Assert.That(CutsceneActions.Describe(CutsceneActorKind.Camera, 3), Is.EqualTo("Look at"));
        Assert.That(CutsceneActions.IsKnown(CutsceneActorKind.Scene, 4), Is.False);
        Assert.That(CutsceneActions.IsKnown(CutsceneActorKind.Cast, 7), Is.False);
    }

    [Test]
    public void LookAtTargets_AreRenumberedLikeFaceTargets()
    {
        // No shipped scene has a camera "Look at" (action 3), so make one from the intro's first camera keyframe:
        // look at cast member 3 (Karne).
        var bytes = (byte[])_intro.Clone();
        var at = KeyframeAt(bytes, 0);
        var copy = KeyframeAt(bytes, Field(bytes, KeyframeCountField));    // and its copy at the end of the file
        Assert.That(BitConverter.ToInt16(bytes, at + 4), Is.EqualTo(CutsceneActions.CameraActor));
        foreach (var offset in new[] { at, copy })
        {
            BitConverter.GetBytes((short)3).CopyTo(bytes, offset + 6);
            BitConverter.GetBytes(3).CopyTo(bytes, offset + 8);
        }
        var intro = EditableCutscene.Load(bytes);
        var look = intro.Keyframes[0];
        Assert.That(look.Description, Is.EqualTo("Look at"));
        Assert.That(look.LookAtTarget, Is.EqualTo(3));
        Assert.That(look.CastTarget, Is.EqualTo(3));
        Assert.That(look.FaceTarget, Is.Null);
        Assert.That(look.RawText, Is.EqualTo("cast member 4"));

        intro.RemoveActor(intro.Cast[1]);                      // Thief1: Karne moves up to 2
        Assert.That(intro.Keyframes[0].LookAtTarget, Is.EqualTo(2));
        intro.RemoveActor(intro.Cast[2]);                      // Karne himself: the camera follows no one
        Assert.That(intro.Keyframes[0].LookAtTarget, Is.EqualTo(-1));
        Assert.That(intro.Keyframes[0].RawText, Is.EqualTo("no one"));
        var saved = intro.ToArray();
        Assert.That(BitConverter.ToInt32(saved, KeyframeAt(saved, 0) + 8), Is.EqualTo(-1));
        Assert.That(EndsWithCopyOfFirstKeyframes(saved), Is.True, "the copy of the first keyframes follows");
    }
}
