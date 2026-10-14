using JetBlackEngineLib.Data.DataContainers;
using JetBlackEngineLib.Data.World;
using System.IO;
using System.Text;

namespace JetBlackEngineLib.Data.CutScenes;

/// <summary>A way to play an animation: the second value of a "Play animation" keyframe.</summary>
/// <param name="Number">The value stored in the keyframe.</param>
/// <param name="Name">A short name, e.g. "Play and hold".</param>
/// <param name="Description">What the game does with it, and where the New Game intro uses it.</param>
public sealed record CutsceneAnimationMode(int Number, string Name, string Description)
{
    /// <summary>"1 · Play and hold".</summary>
    public string Text => $"{Number} · {Name}";

    public override string ToString() => Text;
}

/// <summary>
/// A cast member's "Play animation" keyframe (action 2), as the game handles it (FUN_00155528):
/// <list type="bullet">
/// <item>When the third value is <see cref="ByName"/> (0x0BADFEED), the first value names the animation: it is
/// the <see cref="Hash"/> of its file name, e.g. "nw_deatha.anm". The game looks for that file among the .anm
/// files of the character's archive: for a player stand-in, the archive of the character that player chose
/// (human.lmp, dwarf.lmp or elf.lmp); for anyone else, the archive of its class (thief1.lmp for Thief1).</item>
/// <item>Otherwise the first value is a position in the character's own list of animations, counting from 0.
/// Only intro2p.cut, which the game never plays, does this.</item>
/// <item>The second value is the play mode (<see cref="Modes"/>).</item>
/// </list>
/// </summary>
public static class CutsceneAnimations
{
    /// <summary>The cast action that plays an animation.</summary>
    public const short Action = 2;

    /// <summary>The third value of a keyframe that names its animation by <see cref="Hash"/>.</summary>
    public const int ByName = 0x0BADFEED;

    /// <summary>The extension of animation files.</summary>
    public const string Extension = ".anm";

    /// <summary>Longest animation file name: archive entry names take 56 bytes with their terminator.</summary>
    public const int MaxFileNameLength = 55;

    /// <summary>
    /// The number the game finds an animation by: Bob Jenkins' one-at-a-time hash of its file name, with
    /// ".anm", computed the way the game does it (signed bytes, arithmetic shifts).
    /// </summary>
    public static int Hash(string fileName)
    {
        unchecked
        {
            var hash = 0;
            foreach (var c in fileName)
            {
                hash += (sbyte)(byte)c;
                hash += hash << 10;
                hash ^= hash >> 6;
            }

            hash += hash << 3;
            hash ^= hash >> 11;
            hash += hash << 15;
            return hash;
        }
    }

    /// <summary>"nw_deatha.anm" for "nw_deatha" (a name that already ends in .anm stays as it is).</summary>
    public static string FileName(string name) =>
        name.EndsWith(Extension, StringComparison.OrdinalIgnoreCase) ? name : name + Extension;

    /// <summary>"nw_deatha" for "nw_deatha.anm".</summary>
    public static string ShortName(string fileName) =>
        fileName.EndsWith(Extension, StringComparison.OrdinalIgnoreCase) ? fileName[..^Extension.Length] : fileName;

    /// <summary>
    /// The play modes the game knows, by number. A keyframe with another number looks its animation up and
    /// plays nothing.
    /// <para>
    /// Characters play animations in layers. A new one goes on top and blends in. One that plays once is
    /// removed at its end, which shows the one below again. "Hold" stays on its last frame, "loop" repeats,
    /// and "replace" removes the layers below once it has blended in.
    /// </para>
    /// </summary>
    public static IReadOnlyList<CutsceneAnimationMode> Modes { get; } = new CutsceneAnimationMode[]
    {
        new(0, "Play once",
            "Blends in over 0.2 s, plays once and blends out at the end, back to what was playing. " +
            "In the intro: the players getting up (60.4 s)."),
        new(1, "Play and hold",
            "Blends in over 0.2 s, plays once and stays on its last frame until another animation goes over it. " +
            "In the intro: the players falling (34.4 s)."),
        new(2, "Loop",
            "Blends in over 0.2 s and repeats until another animation goes over it. " +
            "In the intro: the players' idle before they get up (60.3 s)."),
        new(3, "Play once, no blend",
            "Like Play once, but it starts and ends at once, without blending. " +
            "In the intro: Thief1's head-crack (34.2 s)."),
        new(4, "Gesture",
            "Blends in over 0.3 s, replaces what is playing and plays once; then the character's main animation " +
            "repeats again. It also plays the scene's next voice line (the clock waits until the line has started) " +
            "and takes the character as the one talking. " +
            "In the intro: the talking gestures of Thief1, Thief2, Karne and the guards."),
        new(5, "Switch to, loop",
            "Blends in over 0.2 s, replaces what is playing and repeats.")
    };

    /// <summary>The mode with this number, or null when the game doesn't know it.</summary>
    public static CutsceneAnimationMode? ModeOf(int number) => number >= 0 && number < Modes.Count ? Modes[number] : null;
}

/// <summary>An animation a cast member can play, for a list to pick from.</summary>
/// <param name="Name">The animation's name without ".anm", e.g. "nw_deatha".</param>
/// <param name="Archives">Which of the cast member's archives have it.</param>
/// <param name="InAll">True when all of them have it: for a player stand-in, whichever character that player chose.</param>
public sealed record CutsceneAnimationChoice(string Name, IReadOnlyList<string> Archives, bool InAll);

/// <summary>The animations a cast member can play, and the archives they come from.</summary>
public sealed class CutsceneAnimationChoices
{
    internal CutsceneAnimationChoices(IReadOnlyList<string> archives, IReadOnlyList<CutsceneAnimationChoice> animations,
        string source, bool isCertain)
    {
        Archives = archives;
        Animations = animations;
        Source = source;
        IsCertain = isCertain;
    }

    /// <summary>Nothing is known about the cast member's animations.</summary>
    public static CutsceneAnimationChoices None { get; } =
        new(Array.Empty<string>(), Array.Empty<CutsceneAnimationChoice>(), "", false);

    /// <summary>
    /// True when the game is known to use these archives: a player's characters, or the archive of a
    /// townsperson's class. False when they were worked out from what the keyframes play, or from a name.
    /// </summary>
    public bool IsCertain { get; }

    /// <summary>The archives the cast member's animations come from, e.g. thief1.lmp (lower case).</summary>
    public IReadOnlyList<string> Archives { get; }

    /// <summary>The animations: those every archive has first, then the others, each group in name order.</summary>
    public IReadOnlyList<CutsceneAnimationChoice> Animations { get; }

    /// <summary>Why those archives, e.g. "the archive of the Thief1 class" (empty when none was found).</summary>
    public string Source { get; }

    /// <summary>The animation called <paramref name="name"/> (with or without ".anm", any case), or null.</summary>
    public CutsceneAnimationChoice? Find(string name)
    {
        var shortName = CutsceneAnimations.ShortName(name);
        return Animations.FirstOrDefault(a => a.Name.Equals(shortName, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// The animation (.anm) files of the archives a cutscene can use: the archives in the open GOB and the separate
/// archive files (.lmp) of the game's data folder, which hold the characters (human.lmp, thief1.lmp, …). It
/// names the animations of "Play animation" keyframes and lists the ones each cast member can play.
/// </summary>
public sealed class CutsceneAnimationCatalog
{
    /// <summary>
    /// The player characters' archives. A player stand-in plays the animations of whichever character that
    /// player chose (the first archive of the player's resource list, which FUN_00155528 searches).
    /// </summary>
    public static readonly IReadOnlyList<string> PlayerArchives = new[] { "human.lmp", "dwarf.lmp", "elf.lmp" };

    private readonly Dictionary<string, string[]> _archives = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<int, List<string>> _byHash = new();
    private readonly Dictionary<string, string> _byFileName = new(StringComparer.OrdinalIgnoreCase);

    /// <param name="archives">Each archive's name and the names of its entries. When an archive is named twice,
    /// the first one counts.</param>
    public CutsceneAnimationCatalog(IEnumerable<(string Archive, IEnumerable<string> Entries)> archives)
    {
        foreach (var (archive, entries) in archives)
        {
            var name = archive.ToLowerInvariant();
            if (_archives.ContainsKey(name)) continue;
            var animations = entries.Where(e => e.EndsWith(CutsceneAnimations.Extension, StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.Ordinal).ToArray();
            _archives[name] = animations;
            foreach (var animation in animations)
            {
                var hash = CutsceneAnimations.Hash(animation);
                if (!_byHash.TryGetValue(hash, out var names)) _byHash[hash] = names = new List<string>();
                if (!names.Contains(animation)) names.Add(animation);
                _byFileName.TryAdd(animation, animation);
            }
        }
    }

    /// <summary>A catalog that knows no archives.</summary>
    public static CutsceneAnimationCatalog Empty { get; } = new(Array.Empty<(string, IEnumerable<string>)>());

    /// <summary>The archives it knows (lower case).</summary>
    public IReadOnlyCollection<string> Archives => _archives.Keys;

    /// <summary>The animation file names in an archive, in the archive's order, or null when it isn't known.</summary>
    public IReadOnlyList<string>? AnimationsIn(string archive) => _archives.TryGetValue(archive, out var a) ? a : null;

    /// <summary>The animation file names with this hash, in any archive (usually one).</summary>
    public IReadOnlyList<string> FileNamesOf(int hash) =>
        _byHash.TryGetValue(hash, out var names) ? names : Array.Empty<string>();

    /// <summary>
    /// The short name of an animation any archive has, spelled as the archive spells it, for a name in any case,
    /// with or without ".anm"; null when no archive has it.
    /// </summary>
    public string? FindName(string name) =>
        _byFileName.TryGetValue(CutsceneAnimations.FileName(name), out var file) ? CutsceneAnimations.ShortName(file) : null;

    /// <summary>
    /// What a cast member called <paramref name="castName"/> can play, as the game picks the archive it searches:
    /// <list type="bullet">
    /// <item>A player stand-in can be any of the three characters.</item>
    /// <item>A townsperson (an NPC class such as Thief1 or NPCGuard1) uses the archive of its class.</item>
    /// <item>For anyone else (Karne is a monster class, and the intro's Karne plays tkarne.lmp, not karne.lmp),
    /// the archive that has most of the animations its keyframes already play (<paramref name="hashesUsed"/>) is
    /// taken; on a tie, the archive of its class comes first, then one named after it. With no keyframes to go
    /// by, the archive of its class is taken, else one named after it.</item>
    /// </list>
    /// </summary>
    public CutsceneAnimationChoices ChoicesFor(string castName, IEnumerable<int> hashesUsed)
    {
        if (CutsceneActor.IsPlayerName(castName))
        {
            var players = PlayerArchives.Where(_archives.ContainsKey).ToList();
            if (players.Count > 0)
            {
                return Choices(players, players.Count == PlayerArchives.Count
                    ? "the player's character: human.lmp, dwarf.lmp or elf.lmp"
                    : $"the player's character ({string.Join(", ", players)}; the others weren't found)", true);
            }
        }

        var cls = BgdaObjectClasses.Find(castName);
        var classArchive = cls?.Archive?.ToLowerInvariant();
        if (classArchive != null && !_archives.ContainsKey(classArchive)) classArchive = null;
        if (classArchive != null && cls!.Kind == BgdaObjectKind.Npc)
        {
            return Choices(new[] { classArchive }, $"the archive of the {castName} class", true);
        }

        var used = hashesUsed.Distinct().ToList();
        if (used.Count > 0)
        {
            var scores = _archives.ToDictionary(a => a.Key,
                a => used.Count(h => _byHash.TryGetValue(h, out var names) && names.Any(n => a.Value.Contains(n))));
            var best = scores.Count == 0 ? 0 : scores.Values.Max();
            if (best > 0)
            {
                var candidates = scores.Where(s => s.Value == best).Select(s => s.Key)
                    .OrderBy(a => a, StringComparer.Ordinal).ToList();
                var own = castName.ToLowerInvariant();
                var letters = own.TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
                var pick = candidates.FirstOrDefault(a => a == classArchive)
                           ?? candidates.FirstOrDefault(a => a == own + ".lmp")
                           ?? (letters.Any(char.IsLetter)
                               ? candidates.FirstOrDefault(a => a.Contains(letters, StringComparison.Ordinal))
                               : null)
                           ?? (candidates.Count == 1 ? candidates[0] : null);
                if (pick != null)
                {
                    var why = pick == classArchive
                        ? $"the archive of the {castName} class"
                        : best == used.Count
                            ? "the archive with the animations its keyframes play"
                            : $"the archive with {best} of the {used.Count} animations its keyframes play";
                    return Choices(new[] { pick }, why, false);
                }
            }
        }

        if (classArchive != null) return Choices(new[] { classArchive }, $"the archive of the {castName} class", false);
        var named = castName.ToLowerInvariant() + ".lmp";
        return _archives.ContainsKey(named)
            ? Choices(new[] { named }, "the archive named after it", false)
            : CutsceneAnimationChoices.None;
    }

    /// <summary>
    /// The short name of the animation with this hash: one in <paramref name="choices"/>' archives if there is one,
    /// else one in any archive; null when no archive has it.
    /// </summary>
    public string? NameOf(int hash, CutsceneAnimationChoices choices)
    {
        var names = FileNamesOf(hash);
        if (names.Count == 0) return null;
        var own = names.FirstOrDefault(n =>
            choices.Archives.Any(a => _archives.TryGetValue(a, out var files) && files.Contains(n)));
        return CutsceneAnimations.ShortName(own ?? names[0]);
    }

    private CutsceneAnimationChoices Choices(IReadOnlyList<string> archives, string source, bool isCertain)
    {
        var where = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var archive in archives)
        {
            foreach (var file in _archives[archive])
            {
                var name = CutsceneAnimations.ShortName(file);
                if (!where.TryGetValue(name, out var list)) where[name] = list = new List<string>();
                if (!list.Contains(archive)) list.Add(archive);
            }
        }

        var animations = where
            .Select(w => new CutsceneAnimationChoice(w.Key, w.Value, w.Value.Count == archives.Count))
            .OrderBy(c => c.InAll ? 0 : 1).ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return new CutsceneAnimationChoices(archives, animations, source, isCertain);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Reading the archives
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Reads the animation names of the archives in <paramref name="gob"/> (with their unsaved additions and
    /// deletions) and of the separate archive files (*.lmp) in <paramref name="folders"/>. Only the archives'
    /// directories are read. An archive in the GOB comes before a file of the same name, and earlier folders
    /// before later ones.
    /// </summary>
    public static CutsceneAnimationCatalog Read(GobFile? gob, IEnumerable<string?> folders)
    {
        var archives = new List<(string, IEnumerable<string>)>();
        if (gob != null)
        {
            foreach (var (name, lmp) in gob.Directory)
            {
                var entries = EntryNames(lmp);
                if (entries != null) archives.Add((name, entries));
            }
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var folder in folders)
        {
            List<string> files;
            try
            {
                if (string.IsNullOrWhiteSpace(folder) ||
                    !seen.Add(Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder))) ||
                    !System.IO.Directory.Exists(folder))
                {
                    continue;
                }

                files = System.IO.Directory.EnumerateFiles(folder, "*.lmp", new EnumerationOptions
                {
                    MatchCasing = MatchCasing.CaseInsensitive, RecurseSubdirectories = false, IgnoreInaccessible = true
                }).OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToList();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException
                                           or NotSupportedException)
            {
                continue;
            }

            foreach (var path in files)
            {
                var names = ReadDirectoryNames(path);
                if (names != null) archives.Add((Path.GetFileName(path), names));
            }
        }

        return new CutsceneAnimationCatalog(archives);
    }

    /// <summary>An archive's entry names as they would be saved, or null when its directory can't be read.</summary>
    private static IEnumerable<string>? EntryNames(LmpFile lmp)
    {
        try
        {
            if (lmp.Directory.Count == 0) lmp.ReadDirectory();
        }
        catch (Exception ex) when (ex is ArgumentException or IndexOutOfRangeException or InvalidOperationException)
        {
            return null;
        }

        return lmp.Directory.Keys.Where(n => !lmp.PendingDeletions.Contains(n))
            .Concat(lmp.PendingAdditions.Select(a => a.Name)).ToList();
    }

    /// <summary>
    /// The entry names of an archive file: a count, then 64-byte entries that start with the name (up to 56
    /// bytes). Null when the file isn't such an archive or can't be read.
    /// </summary>
    internal static string[]? ReadDirectoryNames(string path)
    {
        try
        {
            using var file = File.OpenRead(path);
            var head = new byte[4];
            file.ReadExactly(head);
            var count = BitConverter.ToInt32(head, 0);
            if (count < 0 || count > 10000 || 4 + 64L * count > file.Length) return null;
            var directory = new byte[64 * count];
            file.ReadExactly(directory);
            var names = new string[count];
            for (var i = 0; i < count; i++)
            {
                var start = i * 64;
                var end = Array.IndexOf(directory, (byte)0, start, 56);
                names[i] = Encoding.ASCII.GetString(directory, start, (end < 0 ? start + 56 : end) - start);
            }

            return names;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}
