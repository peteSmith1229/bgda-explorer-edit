// ═══════════════════════════════════════════════════════════════════════════════
// NEW FILE: WorldExplorer/JetBlackEngineLib/Data/World/BgdaLevelBudget.cs
//
// How much of Baldur's Gate: Dark Alliance's fixed memory a level (GOB) uses, for
// the PAL SLES_506.72 with three players (ADRIANNA21). Worked out from the game
// code and checked against save states of Town, Crypt1, Mine2, towert and burnfrst.
//
// OBJECT POOLS. Every object in objects.ob that the object factory knows
// (BgdaObjectClasses) takes one slot of a fixed pool when the level starts:
//   256-byte pool, 512 slots - props, doors, triggers, lights, treasure
//   768-byte pool, 384 slots - monsters, townsfolk, story characters, a few large props
//   1 KB pool,      32 slots - Beholder, Eldrith, Wisp, and the three player characters
// A full pool spills into the next larger one; when all are full the object is
// simply not created (the game checks for this everywhere). While playing, spells,
// missiles and effects take more slots (7 of the 768-byte pool and 30 of the
// 256-byte pool in a Meteor Swarm fight), so some must stay free.
//
// MEMORY. The heap (26,869,564 bytes) holds, besides fixed data, every archive in
// the level's GOB (loaded when the level starts; sound data goes to sound memory
// and only a small stub stays), any archive an object or a world element needs
// that is not in the GOB (loaded from the disc as a separate file, e.g. burnfrst's
// trees and tents), the world buffer the .world file asks for (header field at
// 0x60 x 128 KB) and the level's work buffer (level table entries x 4,784 bytes).
// What is left is free; the game needs part of it while playing (dialogue voice
// buffers, items, archives loaded later). The estimate of the free memory after
// loading was within 2 KB of the game in all five save states.
// ═══════════════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBlackEngineLib.Data.DataContainers;

namespace JetBlackEngineLib.Data.World;

/// <summary>The three object pools (FUN_0012EC10).</summary>
public enum BgdaPool
{
    /// <summary>256-byte slots, 512 of them.</summary>
    Small = 0,
    /// <summary>768-byte slots, 384 of them: monsters and characters.</summary>
    Medium = 1,
    /// <summary>1 KB slots, 32 of them.</summary>
    Large = 2
}

public enum BgdaBudgetStatus
{
    /// <summary>Comfortably within the limit.</summary>
    Ok,
    /// <summary>Close to the limit: test it in the game.</summary>
    Tight,
    /// <summary>Over the limit.</summary>
    Over
}

/// <summary>Where the game gets an archive from.</summary>
public enum BgdaArchiveSource
{
    /// <summary>In the level's GOB: loaded when the level starts, used or not.</summary>
    Gob,
    /// <summary>Not in the GOB: loaded from the disc as a separate file; the file was found next to the GOB.</summary>
    DiscFileFound,
    /// <summary>Not in the GOB: loaded from the disc as a separate file (size from the disc directory).</summary>
    Disc,
    /// <summary>Neither in the GOB nor on the disc.</summary>
    Missing
}

/// <summary>One object pool.</summary>
public sealed class BgdaPoolUse
{
    internal BgdaPoolUse(BgdaPool pool, int slotBytes, int slots, int players, int spare)
    {
        Pool = pool;
        SlotBytes = slotBytes;
        Slots = slots;
        Players = players;
        Spare = spare;
    }

    public BgdaPool Pool { get; }
    public int SlotBytes { get; }
    public int Slots { get; }

    /// <summary>Slots the player characters take (1 KB pool only).</summary>
    public int Players { get; }

    /// <summary>Level objects in this pool when the level starts (spilled ones included).</summary>
    public int Objects { get; internal set; }

    /// <summary>Of <see cref="Objects"/>, how many came from the smaller pool because it was full.</summary>
    public int SpilledIn { get; internal set; }

    /// <summary>Slots worth keeping free for what the game makes while playing.</summary>
    public int Spare { get; }

    public int Used => Players + Objects;
    public int Free => Slots - Used;
}

/// <summary>An archive the level loads.</summary>
public sealed class BgdaArchiveUse
{
    internal BgdaArchiveUse(string name, BgdaArchiveSource source)
    {
        Name = name;
        Source = source;
    }

    /// <summary>Archive name, e.g. "gnoll.lmp".</summary>
    public string Name { get; }

    public BgdaArchiveSource Source { get; }

    /// <summary>Size of the archive file (0 when missing).</summary>
    public long FileBytes { get; internal set; }

    /// <summary>Sound data (.vag) in it; it goes to sound memory, not the heap.</summary>
    public long SoundBytes { get; internal set; }

    /// <summary>Heap memory it takes (estimated from its contents).</summary>
    public long RamBytes { get; internal set; }

    /// <summary>True when only the file size was known (the sound data could not be subtracted).</summary>
    public bool RamIsUpperEstimate { get; internal set; }

    /// <summary>The archive holding objects.ob and the .world file.</summary>
    public bool IsLevelArchive { get; internal set; }

    /// <summary>Object classes in objects.ob whose model comes from it.</summary>
    public List<string> UsedBy { get; } = new();
}

/// <summary>Objects of one class in objects.ob.</summary>
public sealed class BgdaClassUse
{
    internal BgdaClassUse(string name, BgdaObjectClass? cls)
    {
        Name = name;
        Class = cls;
    }

    public string Name { get; }

    /// <summary>What the factory makes for this name, or null if it doesn't know it.</summary>
    public BgdaObjectClass? Class { get; }

    public int Count { get; internal set; }

    /// <summary>The pool the class belongs in (by object size); null when not created by the factory.</summary>
    public BgdaPool? Pool => Class == null ? null : BgdaLevelBudget.PoolFor(Class.Size);

    /// <summary>How many landed in each pool (index = <see cref="BgdaPool"/>).</summary>
    public int[] Placed { get; } = new int[3];

    /// <summary>Objects that found no free slot and would not be created.</summary>
    public int NotCreated { get; internal set; }

    /// <summary>The archive its model comes from (null when the class has none).</summary>
    public BgdaArchiveUse? Archive { get; internal set; }
}

/// <summary>One line of the memory breakdown.</summary>
public sealed record BgdaMemoryPart(string Label, long Bytes, string Detail, bool IsLevel);

/// <summary>Object-pool and memory budget of a Dark Alliance level.</summary>
public sealed class BgdaLevelBudget
{
    // ─── game facts (SLES_506.72) ───
    public static readonly int[] PoolSlotBytes = { 256, 768, 1024 };
    public static readonly int[] PoolSlots = { 512, 384, 32 };

    /// <summary>768-byte slots to keep free for spells, missiles and effects (7 seen in a Meteor Swarm fight).</summary>
    public const int SpellSlots = 10;

    /// <summary>256-byte slots spells, effects, gold and items take while playing (30 seen in a Meteor Swarm fight).</summary>
    public const int SmallRuntimeSlots = 32;

    /// <summary>Bytes a player character takes in the 1 KB pool.</summary>
    public const int PlayerObjectBytes = 992;

    /// <summary>The heap, block headers included: 0x0064E960-0x01FEE89C.</summary>
    public const long HeapBytes = 26_869_564;

    /// <summary>
    /// What every heap block costs besides its contents: a 24-byte header, and about 192 bytes
    /// the game leaves free behind each archive when it shrinks it (measured: 184-194 on average).
    /// </summary>
    public const int PerBlockBytes = 24 + 192;

    /// <summary>
    /// globsnd (its 544-byte stub), fx, mouth, gtext, inventry, icons, hud, equipmnt, mapicons and
    /// loading.lmp, loaded at start-up: 2,803,248 bytes in 10 blocks.
    /// </summary>
    public const long StartupArchivesBytes = 2_803_248 + 10 * PerBlockBytes;

    /// <summary>The texture cache: 4,456,896 bytes in the three-player builds (5,243,328 in the original game).</summary>
    public const long TextureCacheBytes = 4_456_896 + PerBlockBytes;

    /// <summary>human.lmp 553,944 + dwarf.lmp 545,720 + elf.lmp 828,112: the most three players load.</summary>
    public const long HeroArchivesBytes = 1_927_776 + 3 * PerBlockBytes;

    public const long FixedBytes = StartupArchivesBytes + TextureCacheBytes + HeroArchivesBytes;

    /// <summary>The world buffer is the .world header's field at 0x60 times this (plus 272 bytes of rounding).</summary>
    public const int WorldBufferUnitBytes = 128 * 1024;

    /// <summary>The level work buffer is the level table's entry count times this (plus about 200 bytes).</summary>
    public const int WorkBufferEntryBytes = 4_784;

    /// <summary>Rounding in an archive's heap block (measured: exactly 256 for archives without sound).</summary>
    public const int ArchiveOverheadBytes = 256;

    /// <summary>What stays on the heap of each sound (.vag) once it is in sound memory (128 or 256 bytes; 176 on average).</summary>
    public const int SoundStubBytes = 176;

    /// <summary>Below this much free memory after loading the level is tight.</summary>
    public const long TightBelowBytes = 1024 * 1024;

    /// <summary>Below this much free memory after loading there is less room than in any original level.</summary>
    public const long OverBelowBytes = 400 * 1024;

    public static BgdaPool PoolFor(int size) => size <= 256 ? BgdaPool.Small : size <= 768 ? BgdaPool.Medium : BgdaPool.Large;

    /// <summary>
    /// Names in objects.ob that take no pool slot although the game uses them: the level's lights
    /// (every level has these six) and Eldrith's lightning points (the factory records the position
    /// and creates no object).
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> SpecialNames = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Ambient_Light"] = "level light", ["Ambient_Light2"] = "level light",
        ["Directional_Light"] = "level light", ["Directional_Light2"] = "level light",
        ["Directional_LightD"] = "level light", ["Directional_LightD2"] = "level light",
        ["Zap"] = "lightning point, no object"
    };

    // ─── results ───

    /// <summary>The level name the game uses (the GOB name without extension, lower case).</summary>
    public string LevelName { get; private set; } = "";

    public int Players { get; private set; }

    /// <summary>The archive that holds objects.ob (null when the GOB has none).</summary>
    public string? LevelArchive { get; private set; }

    /// <summary>Objects in objects.ob.</summary>
    public int ObjectCount { get; private set; }

    public IReadOnlyList<BgdaPoolUse> Pools { get; private set; } = Array.Empty<BgdaPoolUse>();

    /// <summary>Classes in objects.ob, most numerous first.</summary>
    public IReadOnlyList<BgdaClassUse> Classes { get; private set; } = Array.Empty<BgdaClassUse>();

    /// <summary>Every archive the level loads: the GOB's own, then those loaded from the disc.</summary>
    public IReadOnlyList<BgdaArchiveUse> Archives { get; private set; } = Array.Empty<BgdaArchiveUse>();

    public string? WorldFile { get; private set; }

    /// <summary>Archives the .world's elements load by name (models such as trees and tents).</summary>
    public List<string> WorldArchiveNames { get; } = new();

    public int WorldBufferUnits { get; private set; }

    /// <summary>The world buffer's heap block (0 when the level has none).</summary>
    public long WorldBufferBytes => WorldBufferUnits == 0 ? 0 : (long)WorldBufferUnits * WorldBufferUnitBytes + 272;

    public int WorkBufferEntries { get; private set; }

    /// <summary>The work buffer's heap block.</summary>
    public long WorkBufferBytes => (long)WorkBufferEntries * WorkBufferEntryBytes + 200;

    /// <summary>Fixed parts, then this level's parts.</summary>
    public IReadOnlyList<BgdaMemoryPart> MemoryParts { get; private set; } = Array.Empty<BgdaMemoryPart>();

    /// <summary>Heap taken by the GOB's archives (blocks, headers and gaps).</summary>
    public long GobArchivesBytes { get; private set; }

    /// <summary>Heap taken by archives loaded from the disc for the objects and the world's elements.</summary>
    public long DiscArchivesBytes { get; private set; }

    /// <summary>Everything this level adds to the heap (headers and gaps included).</summary>
    public long LevelBytes => GobArchivesBytes + DiscArchivesBytes +
                              (WorldBufferUnits == 0 ? 0 : WorldBufferBytes + PerBlockBytes) + WorkBufferBytes + PerBlockBytes;

    /// <summary>Estimated free heap right after the level has loaded (one free block: its header is not free).</summary>
    public long FreeAfterLoadBytes => HeapBytes - FixedBytes - LevelBytes - 24;

    public BgdaBudgetStatus MemoryStatus =>
        FreeAfterLoadBytes < OverBelowBytes ? BgdaBudgetStatus.Over
        : FreeAfterLoadBytes < TightBelowBytes ? BgdaBudgetStatus.Tight
        : BgdaBudgetStatus.Ok;

    public BgdaPoolUse Small => Pools[0];
    public BgdaPoolUse Medium => Pools[1];
    public BgdaPoolUse Large => Pools[2];

    /// <summary>Objects that would not be created because every pool they fit was full.</summary>
    public int NotCreated => Classes.Sum(c => c.NotCreated);

    /// <summary>Objects whose name the factory doesn't create (the level lights and lightning points included).</summary>
    public int UnknownObjects => Classes.Where(c => c.Class == null).Sum(c => c.Count);

    /// <summary>
    /// Small objects the game makes while playing that won't fit in the 256-byte pool and so
    /// take 768-byte slots.
    /// </summary>
    public int ExpectedSmallSpill => Math.Max(0, Small.Used + SmallRuntimeSlots - Small.Slots);

    /// <summary>
    /// More monsters that fit in the 768-byte pool, keeping <see cref="SpellSlots"/> free for
    /// spells and room for the small objects that spill over while playing.
    /// </summary>
    public int MonsterRoom => Math.Max(0, Medium.Free - SpellSlots - ExpectedSmallSpill);

    public BgdaBudgetStatus SlotStatus =>
        NotCreated > 0 || Medium.Free < 0 ? BgdaBudgetStatus.Over
        : Medium.Free - ExpectedSmallSpill < SpellSlots || Large.Free < 2 ? BgdaBudgetStatus.Tight
        : BgdaBudgetStatus.Ok;

    /// <summary>Things worth knowing, worst first.</summary>
    public IReadOnlyList<string> Warnings { get; private set; } = Array.Empty<string>();

    // ─── analysis ───

    /// <summary>
    /// Works out the budget of <paramref name="gob"/> as it would be saved (pending edits
    /// included). <paramref name="folder"/> is where to look for archives the GOB lacks (the
    /// game loads those from the disc; the folder a GOB was opened from usually has them).
    /// <paramref name="players"/> is how many player characters take 1 KB slots; the memory
    /// estimate always allows for three different heroes (the most the game loads).
    /// </summary>
    public static BgdaLevelBudget Analyze(GobFile gob, string? folder = null, int players = 3)
    {
        if (players is < 1 or > 3) throw new ArgumentOutOfRangeException(nameof(players));
        var b = new BgdaLevelBudget
        {
            Players = players,
            LevelName = Path.GetFileNameWithoutExtension(gob.Name).ToLowerInvariant()
        };
        var warnings = new List<string>();

        // ---- the GOB's archives ----
        var archives = new List<BgdaArchiveUse>();
        var byName = new Dictionary<string, BgdaArchiveUse>(StringComparer.OrdinalIgnoreCase);
        LmpFile? levelLmp = null;
        foreach (var (name, lmp) in gob.Directory)
        {
            if (lmp.Directory.Count == 0) lmp.ReadDirectory();
            var a = new BgdaArchiveUse(name, BgdaArchiveSource.Gob);
            var entries = EffectiveEntries(lmp);
            a.FileBytes = lmp.IsDirty ? PackedLength(entries) : lmp.DataLength;
            (a.SoundBytes, a.RamBytes) = RamEstimate(a.FileBytes, entries);
            archives.Add(a);
            byName.TryAdd(name, a);
            if (entries.Any(e => e.Name.Equals("objects.ob", StringComparison.OrdinalIgnoreCase)) &&
                (levelLmp == null || IsNamedAfterLevel(name, b.LevelName)))
            {
                levelLmp = lmp;
            }
        }

        // ---- objects.ob ----
        var classes = new Dictionary<string, BgdaClassUse>(StringComparer.Ordinal);
        var order = new List<BgdaClassUse>();
        b.Pools = new[]
        {
            new BgdaPoolUse(BgdaPool.Small, 256, PoolSlots[0], 0, SmallRuntimeSlots),
            new BgdaPoolUse(BgdaPool.Medium, 768, PoolSlots[1], 0, SpellSlots),
            new BgdaPoolUse(BgdaPool.Large, 1024, PoolSlots[2], players, 0)
        };
        if (levelLmp != null)
        {
            b.LevelArchive = levelLmp.Name;
            byName[levelLmp.Name].IsLevelArchive = true;
            var ob = ReadEntry(levelLmp, "objects.ob");
            List<ObjectData> objects;
            try
            {
                objects = ob == null ? new List<ObjectData>() : ObDecoder.Decode(ob, 0, ob.Length);
            }
            catch (Exception ex) when (ex is ArgumentException or IndexOutOfRangeException or ArgumentOutOfRangeException)
            {
                objects = new List<ObjectData>();
                warnings.Add($"objects.ob in {levelLmp.Name} could not be read ({ex.Message}), so its objects are not counted.");
            }

            b.ObjectCount = objects.Count;
            var used = new int[3];
            used[2] = players;
            foreach (var o in objects)
            {
                if (!classes.TryGetValue(o.Name, out var cu))
                {
                    cu = new BgdaClassUse(o.Name, BgdaObjectClasses.Find(o.Name));
                    classes[o.Name] = cu;
                    order.Add(cu);
                }

                cu.Count++;
                if (cu.Class == null) continue;
                var p = Place(used, cu.Class.Size);
                if (p < 0)
                {
                    cu.NotCreated++;
                    continue;
                }

                cu.Placed[p]++;
                b.Pools[p].Objects++;
                if (p != (int)PoolFor(cu.Class.Size)) b.Pools[p].SpilledIn++;
            }

            // ---- the .world file: the world buffer ----
            var world = EffectiveEntries(levelLmp).FirstOrDefault(e => e.Name.EndsWith(".world", StringComparison.OrdinalIgnoreCase));
            if (world.Name != null && world.Length >= WorldFileHeader.Size)
            {
                // only the header is needed: read it in place unless the .world has been edited
                var units = world.Offset >= 0
                    ? BitConverter.ToInt32(levelLmp.FileData, world.Offset + 0x60)
                    : BitConverter.ToInt32(ReadEntry(levelLmp, world.Name)!, 0x60);
                b.WorldFile = world.Name;
                b.WorldBufferUnits = Math.Max(0, units);
            }
        }
        else
        {
            warnings.Add("This GOB has no objects.ob, so there are no level objects to count.");
        }

        if (b.WorldFile == null && levelLmp != null)
            warnings.Add("No .world file was found next to objects.ob, so the world buffer is not counted.");

        // ---- archives the objects and the world need that are not in the GOB: loaded from the disc ----
        Dictionary<string, string>? folderFiles = null;
        BgdaArchiveUse Resolve(string archive)
        {
            if (byName.TryGetValue(archive, out var a)) return a;
            folderFiles ??= ListFolder(folder);
            if (folderFiles.TryGetValue(archive, out var path) && TryReadLooseArchive(path, out var fileBytes, out var entries))
            {
                a = new BgdaArchiveUse(archive, BgdaArchiveSource.DiscFileFound) { FileBytes = fileBytes };
                (a.SoundBytes, a.RamBytes) = RamEstimate(fileBytes, entries);
            }
            else if (BgdaObjectClasses.DiscArchives.TryGetValue(archive, out var discBytes))
            {
                a = new BgdaArchiveUse(archive, BgdaArchiveSource.Disc)
                {
                    FileBytes = discBytes, RamBytes = discBytes + ArchiveOverheadBytes, RamIsUpperEstimate = true
                };
            }
            else
            {
                a = new BgdaArchiveUse(archive, BgdaArchiveSource.Missing);
            }

            archives.Add(a);
            byName[archive] = a;
            return a;
        }

        foreach (var cu in order)
        {
            var archive = cu.Class?.Archive;
            if (archive == null) continue;
            var a = Resolve(archive);
            cu.Archive = a;
            a.UsedBy.Add(cu.Name);
        }

        // world elements with flag 0x0800 are models from a separate archive, loaded by name ("%s.lmp")
        // when the level starts (FUN_00146490 -> find-or-load FUN_0013A3D0) - e.g. burnfrst's trees and tents
        if (levelLmp != null && b.WorldFile != null)
        {
            foreach (var (name, elements) in WorldArchives(levelLmp, b.WorldFile))
            {
                var a = Resolve(name);
                a.UsedBy.Add($"world ({elements:N0} element{(elements == 1 ? "" : "s")})");
                b.WorldArchiveNames.Add(name);
            }
        }

        b.Archives = archives;
        b.Classes = order.OrderByDescending(c => c.Count).ThenBy(c => c.Name, StringComparer.Ordinal).ToList();
        b.GobArchivesBytes = archives.Where(a => a.Source == BgdaArchiveSource.Gob).Sum(a => a.RamBytes + PerBlockBytes);
        b.DiscArchivesBytes = archives.Where(a => a.Source is BgdaArchiveSource.Disc or BgdaArchiveSource.DiscFileFound)
            .Sum(a => a.RamBytes + PerBlockBytes);
        b.WorkBufferEntries = BgdaObjectClasses.LevelEntries(b.LevelName);

        var discCount = archives.Count(a => a.Source != BgdaArchiveSource.Gob && a.Source != BgdaArchiveSource.Missing);
        var discWorld = archives.Count(a => a.Source != BgdaArchiveSource.Gob && a.Source != BgdaArchiveSource.Missing &&
                                            a.UsedBy.Any(u => u.StartsWith("world (")));
        b.MemoryParts = new[]
        {
            new BgdaMemoryPart("Start-up archives", StartupArchivesBytes,
                "fx, hud, inventory, equipment and the other archives loaded when the game starts", false),
            new BgdaMemoryPart("Texture cache", TextureCacheBytes, "4.25 MB in the three-player builds (5 MB in the original game)", false),
            new BgdaMemoryPart("Heroes", HeroArchivesBytes,
                "human, dwarf and elf archives - the most three players load (less when two pick the same hero)", false),
            new BgdaMemoryPart("Archives in this GOB", b.GobArchivesBytes,
                $"{archives.Count(a => a.Source == BgdaArchiveSource.Gob)} archives, loaded when the level starts (sounds go to sound memory)", true),
            new BgdaMemoryPart("Archives from the disc", b.DiscArchivesBytes,
                discCount == 0 ? "none - everything the objects and the world need is in this GOB"
                    : $"{discCount} archive{(discCount == 1 ? "" : "s")} the objects{(discWorld > 0 ? " or the world" : "")} need that " +
                      $"{(discCount == 1 ? "is" : "are")} not in this GOB", true),
            new BgdaMemoryPart("World buffer", b.WorldBufferUnits == 0 ? 0 : b.WorldBufferBytes + PerBlockBytes,
                b.WorldFile == null ? "no .world file" : $"{b.WorldBufferUnits} x 128 KB, as {b.WorldFile} asks", true),
            new BgdaMemoryPart("Level work buffer", b.WorkBufferBytes + PerBlockBytes,
                $"{b.WorkBufferEntries} x 4,784 bytes (the game's level table)", true)
        };

        // ---- warnings, worst first ----
        var free = b.FreeAfterLoadBytes;
        if (free < 0)
            warnings.Insert(0, $"Memory: the level needs {Mb(-free)} more than the game has, so it can't load.");
        else if (b.MemoryStatus == BgdaBudgetStatus.Over)
            warnings.Insert(0, $"Memory: only {Mb(free)} free after loading - less than any original level. The game " +
                               "may run out while playing, for example when a conversation starts.");

        foreach (var cu in b.Classes.Where(c => c.NotCreated > 0))
            warnings.Add($"{cu.NotCreated} of the {cu.Count} {cu.Name} objects find no free pool slot and would not be created.");

        if (b.Medium.SpilledIn > 0)
            warnings.Add($"The 256-byte pool is full: {b.Medium.SpilledIn} small objects take 768-byte slots instead.");

        foreach (var a in archives.Where(a => a.Source == BgdaArchiveSource.Missing))
            warnings.Add($"{string.Join(", ", a.UsedBy)} need{(a.UsedBy.Count == 1 ? "s" : "")} {a.Name}, which is neither in " +
                         "this GOB nor on the game disc: the game has no model to load and will probably crash.");

        if (b.SlotStatus == BgdaBudgetStatus.Tight && b.NotCreated == 0)
            warnings.Add(b.Large.Free < 2
                ? $"The 1 KB pool has {b.Large.Free} free slot{(b.Large.Free == 1 ? "" : "s")} left."
                : $"Fewer than {SpellSlots} monster slots are left for spells and missiles: in big fights they will " +
                  "spill into the 1 KB pool, then fail.");

        foreach (var a in archives.Where(a => a.Source is BgdaArchiveSource.Disc or BgdaArchiveSource.DiscFileFound))
        {
            var users = a.UsedBy.Where(u => !u.StartsWith("world (")).ToList();
            if (users.Count == 0) continue;     // the original levels' own world models: nothing to warn about
            warnings.Add($"{string.Join(", ", users)} need{(users.Count == 1 ? "s" : "")} {a.Name}, which is not in this " +
                         $"GOB. The game loads it from the disc as a separate file when the level starts ({Mb(a.RamBytes)} " +
                         $"{(a.RamIsUpperEstimate ? "at most, counted" : "counted")}); copy it into the GOB to keep the level self-contained.");
        }

        var unknown = b.Classes.Where(c => c.Class == null && !SpecialNames.ContainsKey(c.Name)).ToList();
        if (unknown.Count > 0)
        {
            var count = unknown.Sum(c => c.Count);
            warnings.Add($"{count} object{(count == 1 ? "" : "s")} ({string.Join(", ", unknown.Select(c => $"{c.Count} {c.Name}"))}) " +
                         $"use{(count == 1 ? "s" : "")} a name the object factory doesn't create, so the game makes nothing for " +
                         $"{(count == 1 ? "it" : "them")} (no pool slot either). If one should be a monster or prop, check the " +
                         "spelling: names must match exactly, capitals included.");
        }

        if (b.MemoryStatus == BgdaBudgetStatus.Tight)
            warnings.Add($"Memory is tight: {Mb(free)} free after loading, about as full as the fullest original levels. " +
                         "Dialogue, items and archives loaded later use up to about 0.4 MB more while playing.");

        b.Warnings = warnings;
        return b;
    }

    /// <summary>
    /// FUN_0012EC10: takes a slot in the smallest pool <paramref name="size"/> fits, else in the
    /// next larger one with a free slot. Returns the pool, or -1 when every one is full.
    /// </summary>
    private static int Place(int[] used, int size)
    {
        for (var p = (int)PoolFor(size); p < 3; p++)
        {
            if (used[p] >= PoolSlots[p]) continue;
            used[p]++;
            return p;
        }

        return -1;
    }

    /// <summary>
    /// The pool slots objects of these classes take when the level starts (the players' characters
    /// in the 1 KB pool included), and how many would not be created. Names the factory doesn't
    /// create are skipped. Quick enough to run after every edit.
    /// </summary>
    public static (int Small, int Medium, int Large, int NotCreated) CountSlots(IEnumerable<string> classNames, int players = 3)
    {
        var used = new[] { 0, 0, players };
        var notCreated = 0;
        foreach (var name in classNames)
        {
            var cls = BgdaObjectClasses.Find(name);
            if (cls != null && Place(used, cls.Size) < 0) notCreated++;
        }

        return (used[0], used[1], used[2], notCreated);
    }

    private static bool IsNamedAfterLevel(string archive, string level) =>
        Path.GetFileNameWithoutExtension(archive).Equals(level, StringComparison.OrdinalIgnoreCase);

    private static string Mb(long bytes) =>
        bytes >= 1024 * 1024 ? $"{bytes / (1024.0 * 1024.0):0.##} MB" : $"{bytes / 1024.0:0.#} KB";

    // ─── archives ───

    internal readonly record struct Entry(string Name, int Offset, int Length);

    /// <summary>Entries as they would be saved: edits, deletions and additions applied; shared data counted once.</summary>
    internal static List<Entry> EffectiveEntries(LmpFile lmp)
    {
        var list = new List<Entry>();
        foreach (var (name, info) in lmp.Directory)
        {
            if (lmp.PendingDeletions.Contains(name)) continue;
            list.Add(lmp.PendingEdits.TryGetValue(name, out var edit)
                ? new Entry(name, -1, edit.Length)
                : new Entry(name, info.StartOffset, info.Length));
        }

        foreach (var (name, data) in lmp.PendingAdditions) list.Add(new Entry(name, -1, data.Length));
        return list;
    }

    private static int Align(long x, int a) => (int)((x + a - 1) / a * a);

    /// <summary>Length of the archive written with these entries (header and entries 128-byte aligned).</summary>
    internal static long PackedLength(List<Entry> entries)
    {
        long len = Align(4 + 64L * entries.Count, 128);
        var seen = new HashSet<int>();
        foreach (var e in entries)
        {
            if (e.Offset >= 0 && !seen.Add(e.Offset)) continue;   // shared data is stored once
            len = Align(len + e.Length, 128);
        }

        return len;
    }

    /// <summary>
    /// Heap block of an archive: the file minus its sound data (sent to sound memory; a stub of
    /// about 176 bytes stays per sound), plus the block header and rounding.
    /// </summary>
    internal static (long Sound, long Ram) RamEstimate(long fileBytes, IEnumerable<Entry> entries)
    {
        long sound = 0;
        var sounds = 0;
        var seen = new HashSet<(int, int)>();
        foreach (var e in entries)
        {
            if (!e.Name.EndsWith(".vag", StringComparison.OrdinalIgnoreCase)) continue;
            if (e.Offset >= 0 && !seen.Add((e.Offset, e.Length))) continue;
            sound += Align(e.Length, 128);
            sounds++;
        }

        return (sound, Math.Max(0, fileBytes - sound) + (long)sounds * SoundStubBytes + ArchiveOverheadBytes);
    }

    /// <summary>
    /// Archives the world's elements load: each element (56 bytes, from the header's element array
    /// at 0x24, count at 0x00) whose flags (+0x30) have 0x0800 names an archive at its first offset.
    /// Returns name -> number of elements.
    /// </summary>
    internal static List<(string Name, int Elements)> WorldArchives(LmpFile lmp, string worldName)
    {
        var info = lmp.FindFile(worldName);
        if (info == null) return new();
        byte[] data;
        int start, length;
        var edited = ReadEntryIfEdited(lmp, worldName);
        if (edited != null)
        {
            (data, start, length) = (edited, 0, edited.Length);
        }
        else
        {
            (data, start, length) = (lmp.FileData, info.StartOffset, info.Length);
        }

        var result = new List<(string, int)>();
        if (length < WorldFileHeader.Size) return result;
        var count = BitConverter.ToInt32(data, start);
        var array = BitConverter.ToInt32(data, start + 0x24);
        if (count <= 0 || array <= 0 || (long)array + 56L * count > length) return result;
        var seen = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < count; i++)
        {
            var e = start + array + 56 * i;
            if ((BitConverter.ToUInt16(data, e + 0x30) & 0x0800) == 0) continue;
            var at = BitConverter.ToInt32(data, e);
            if (at <= 0 || at >= length) continue;
            var end = start + at;
            while (end < start + length && data[end] != 0 && end - (start + at) < 64) end++;
            if (end == start + at) continue;
            var name = System.Text.Encoding.ASCII.GetString(data, start + at, end - (start + at)) + ".lmp";
            seen[name] = seen.TryGetValue(name, out var c) ? c + 1 : 1;
        }

        foreach (var (name, c) in seen) result.Add((name.ToLowerInvariant(), c));
        return result;
    }

    /// <summary>The entry's pending bytes when it has been edited or added, else null.</summary>
    private static byte[]? ReadEntryIfEdited(LmpFile lmp, string name)
    {
        foreach (var (n, data) in lmp.PendingAdditions)
        {
            if (n.Equals(name, StringComparison.OrdinalIgnoreCase)) return data;
        }

        return lmp.PendingEdits.TryGetValue(name, out var edit) ? edit : null;
    }

    /// <summary>The entry's bytes as they would be saved (null when absent or deleted).</summary>
    internal static byte[]? ReadEntry(LmpFile lmp, string name)
    {
        foreach (var (n, data) in lmp.PendingAdditions)
        {
            if (n.Equals(name, StringComparison.OrdinalIgnoreCase)) return data;
        }

        var info = lmp.FindFile(name);
        if (info == null) return null;
        var key = lmp.Directory.First(kv => ReferenceEquals(kv.Value, info)).Key;
        if (lmp.PendingDeletions.Contains(key)) return null;
        if (lmp.PendingEdits.TryGetValue(key, out var edit)) return edit;
        var bytes = new byte[info.Length];
        Array.Copy(lmp.FileData, info.StartOffset, bytes, 0, info.Length);
        return bytes;
    }

    /// <summary>Files in <paramref name="folder"/> by lower-case name (empty when there is no folder).</summary>
    private static Dictionary<string, string> ListFolder(string? folder)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(folder) || !System.IO.Directory.Exists(folder)) return map;
        try
        {
            foreach (var path in System.IO.Directory.EnumerateFiles(folder, "*.lmp", new EnumerationOptions
                     {
                         MatchCasing = MatchCasing.CaseInsensitive, RecurseSubdirectories = false
                     }))
            {
                map.TryAdd(Path.GetFileName(path), path);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }

        return map;
    }

    /// <summary>Reads only the directory of a separate archive file.</summary>
    private static bool TryReadLooseArchive(string path, out long fileBytes, out List<Entry> entries)
    {
        entries = new List<Entry>();
        fileBytes = 0;
        try
        {
            using var f = File.OpenRead(path);
            fileBytes = f.Length;
            var head = new byte[4];
            if (f.Read(head, 0, 4) != 4) return false;
            var n = BitConverter.ToInt32(head, 0);
            if (n < 0 || n > 10000 || 4 + 64L * n > fileBytes) return false;
            var dir = new byte[64 * n];
            if (f.Read(dir, 0, dir.Length) != dir.Length) return false;
            for (var i = 0; i < n; i++)
            {
                var name = DataUtil.GetString(dir, i * 64);
                entries.Add(new Entry(name, BitConverter.ToInt32(dir, i * 64 + 56), BitConverter.ToInt32(dir, i * 64 + 60)));
            }

            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
