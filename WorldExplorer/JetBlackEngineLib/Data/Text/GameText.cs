// ═══════════════════════════════════════════════════════════════════════════════
// NEW FILE: WorldExplorer/JetBlackEngineLib/Data/Text/GameText.cs
//
// The game's text files (.uni) and GTEXT.LMP, which holds the interface text in five
// languages: gtext.uni (English), gtext_fr.uni, gtext_ge.uni, gtext_it.uni, gtext_sp.uni.
// The game keeps only its own language's file (see CutsceneSubtitles.LanguageOf).
//
// .uni layout: entries one after another, each an ASCII key and a NUL, a NUL more when
// needed to reach an even offset, then the text in UTF-16LE and a 16-bit NUL; four zero
// bytes end the file. The game finds a text by key (FUN_0013B148, a hash of the key); a key
// it can't find shows as "NF:" and the first 16 characters of the key. GTEXT has keys with
// spaces and keys twice, so entries are kept exactly as they are and an unchanged file is
// written back byte for byte.
//
// An item's name is the text of "<item name>_name" ("longsword_name" = "Long Sword"); the
// modifiers are "it_..." keys ("it_flaming" = "Flaming"). In French, German, Italian and
// Spanish an item name ends with a gender mark ("Epée longue_F"): the naming code
// (FUN_001719E0) strips any "_x" ending and, for _f/_F and _m/_M, looks up the modifier
// words of that gender first ("it_flaming_F", then "it_flaming"; FUN_00171818). Some
// modifiers exist only in gendered forms, so a name without a mark would show
// "NF:it_flaming" in French: names added to those languages get a masculine mark.
//
// The game hashes every key into a table of 4096 (FUN_0013B3C8) when it loads GTEXT.LMP
// with the level and cutscene texts; GTEXT has fewer than 900 keys per language, so a few
// new names fit easily.
// ═══════════════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using JetBlackEngineLib.Data.DataContainers;

namespace JetBlackEngineLib.Data.Text;

/// <summary>One .uni text file, entries kept in order exactly as they are.</summary>
public sealed class UniTextFile
{
    private const int EndLength = 4;

    public UniTextFile(List<(string Key, string Text)> entries) => Entries = entries;

    /// <summary>The entries in file order (a key can appear twice; the game finds the first).</summary>
    public List<(string Key, string Text)> Entries { get; }

    /// <summary>Reads a .uni file; throws <see cref="InvalidDataException"/> when its layout isn't understood.</summary>
    public static UniTextFile Read(byte[] data)
    {
        var entries = new List<(string, string)>();
        var at = 0;
        while (at < data.Length && data[at] != 0)
        {
            var keyEnd = Array.IndexOf(data, (byte)0, at);
            if (keyEnd < 0) throw Invalid($"The key at 0x{at:X} has no end.");
            var key = Encoding.ASCII.GetString(data, at, keyEnd - at);
            var text = keyEnd + 1;
            if ((text & 1) != 0) text++;
            var end = text;
            while (end + 1 < data.Length && (data[end] != 0 || data[end + 1] != 0)) end += 2;
            if (end + 1 >= data.Length) throw Invalid($"The text of {key} has no end.");
            entries.Add((key, Encoding.Unicode.GetString(data, text, end - text)));
            at = end + 2;
        }

        return new UniTextFile(entries);
    }

    /// <summary>The file's bytes, laid out as the game's own files are.</summary>
    public byte[] ToArray()
    {
        using var stream = new MemoryStream();
        foreach (var (key, text) in Entries)
        {
            if (key.Length == 0 || key.Any(c => c == 0 || c > 0x7E)) throw new ArgumentException($"\"{key}\" isn't a key the game can read.");
            if (text.Contains('\0')) throw new ArgumentException($"The text of {key} can't contain a NUL character.");
            stream.Write(Encoding.ASCII.GetBytes(key));
            stream.WriteByte(0);
            if ((stream.Length & 1) != 0) stream.WriteByte(0);
            stream.Write(Encoding.Unicode.GetBytes(text));
            stream.Write(new byte[2]);
        }

        stream.Write(new byte[EndLength]);
        return stream.ToArray();
    }

    /// <summary>The text of the first entry with <paramref name="key"/>, or null.</summary>
    public string? Get(string key)
    {
        foreach (var (k, t) in Entries)
        {
            if (k == key) return t;
        }

        return null;
    }

    /// <summary>Changes the first entry with <paramref name="key"/>, or adds one at the end.</summary>
    public void Set(string key, string text)
    {
        var i = Entries.FindIndex(e => e.Key == key);
        if (i >= 0) Entries[i] = (key, text);
        else Entries.Add((key, text));
    }

    private static InvalidDataException Invalid(string message) => new($"Not a .uni text file: {message}");
}

/// <summary>GTEXT.LMP: the interface text (item names among it) in the game's five languages.</summary>
public sealed class GameText
{
    /// <summary>The file of each language in GTEXT.LMP, English first.</summary>
    public static readonly IReadOnlyList<(string File, string Language)> Files = new[]
    {
        ("gtext.uni", "English"), ("gtext_fr.uni", "French"), ("gtext_ge.uni", "German"),
        ("gtext_it.uni", "Italian"), ("gtext_sp.uni", "Spanish")
    };

    private readonly byte[] _original;
    private readonly LmpFile _lmp;
    private readonly UniTextFile[] _languages;
    private readonly bool[] _changed;

    private GameText(byte[] data, LmpFile lmp, UniTextFile[] languages)
    {
        _original = data;
        _lmp = lmp;
        _languages = languages;
        _changed = new bool[languages.Length];
    }

    /// <summary>Reads GTEXT.LMP; throws <see cref="InvalidDataException"/> when one of the five files is missing or unreadable.</summary>
    public static GameText Read(byte[] data)
    {
        var lmp = new LmpFile(EngineVersion.DarkAlliance, "gtext.lmp", data, 0, data.Length);
        try
        {
            lmp.ReadDirectory();
            var languages = new UniTextFile[Files.Count];
            for (var i = 0; i < Files.Count; i++)
            {
                var entry = lmp.FindFile(Files[i].File) ?? throw new InvalidDataException($"GTEXT.LMP has no {Files[i].File}.");
                languages[i] = UniTextFile.Read(data.AsSpan(entry.StartOffset, entry.Length).ToArray());
            }

            return new GameText(data, lmp, languages);
        }
        catch (Exception e) when (e is ArgumentException or IndexOutOfRangeException or OverflowException)
        {
            throw new InvalidDataException("Not GTEXT.LMP: its directory can't be read.", e);
        }
    }

    public bool Changed => _changed.Any(c => c);

    /// <summary>A text in a language (0 English ... 4 Spanish), or null when the key isn't there.</summary>
    public string? Get(string key, int language = 0) => _languages[language].Get(key);

    /// <summary>Every entry of a language (0 English ... 4 Spanish), in the order of its file.</summary>
    public IReadOnlyList<(string Key, string Text)> Entries(int language) => _languages[language].Entries;

    /// <summary>The key of an item's name: "longsword" gives "longsword_name".</summary>
    public static string NameKey(string itemName) => itemName + "_name";

    /// <summary>The longest item name the game itself has ("Twinkle (+5 Defending Scimitar)").</summary>
    public const int MaxItemNameLength = 31;

    /// <summary>
    /// The gender mark given to an item name added to each language: none in English, masculine in the
    /// others ("_M" as French and German write it, "_m" as Italian and Spanish do).
    /// </summary>
    public static readonly IReadOnlyList<string> NewNameMarks = new[] { "", "_M", "_M", "_m", "_m" };

    /// <summary>
    /// Item names the game's text has in all five languages but no item of the stock game uses: an unused
    /// record renamed to one of these is named in every language without adding text.
    /// </summary>
    public static readonly IReadOnlyList<string> ReadyMadeItemNames = new[]
    {
        "lthammer", "rapier", "admbattleaxe", "admdagger", "flametongue", "frostbrand", "puncturerapier",
        "necklace", "acidflask", "fireflask", "holywater", "recall"
    };

    /// <summary>A text as the naming code shows it: any "_x" ending (a gender mark) removed.</summary>
    public static string? WithoutGender(string? text) =>
        text is { Length: >= 2 } && text[^2] == '_' ? text[..^2] : text;

    /// <summary>An item's name as a language shows it (gender mark removed), or null when it has none.</summary>
    public string? GetItemName(string itemName, int language = 0) => WithoutGender(Get(NameKey(itemName), language));

    /// <summary>Whether every language has <paramref name="key"/>.</summary>
    public bool InAllLanguages(string key) => _languages.All(l => l.Get(key) != null);

    /// <summary>Every character the texts of GTEXT.LMP use: those the game's font is known to draw.</summary>
    public const string TextCharacters =
        " !\"%&'()+,-./0123456789:;<>?ABCDEFGHIJKLMNOPQRSTUVWXYZ[]_abcdefghijklmnopqrstuvwxyz{|}~" +
        "\u00A1\u00B0\u00BF\u00C1\u00C4\u00C8\u00C9\u00D3\u00D6\u00DF\u00E0\u00E1\u00E2\u00E4\u00E7\u00E8\u00E9\u00EA" +
        "\u00EC\u00ED\u00EE\u00EF\u00F1\u00F2\u00F3\u00F4\u00F6\u00F9\u00FA\u00FB\u00FC\u0153\u2019\u2026";

    /// <summary>Why <paramref name="name"/> can't be an item's English name, or null when it can.</summary>
    public static string? CheckItemName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "Enter the name the game shows.";
        if (name.Length > MaxItemNameLength) return $"Names can be up to {MaxItemNameLength} characters (the game's longest).";
        if (name.Any(c => !TextCharacters.Contains(c)))
            return "Use letters, digits, spaces and the punctuation and accented letters of the game's own texts.";
        if (name != name.Trim()) return "Names can't start or end with a space.";
        if (name.Length >= 2 && name[^2] == '_') return "The game would read the \"_\" and last letter as a gender mark and hide them.";
        return null;
    }

    /// <summary>
    /// Sets the English name of an item, and adds it with a masculine mark to each other language that has
    /// no name for the item (so no language shows "NF:"). Other languages' own names stay.
    /// </summary>
    public void SetItemName(string itemName, string englishName)
    {
        if (CheckItemName(englishName) is { } problem) throw new ArgumentException($"{itemName}: {problem}");
        SetText(NameKey(itemName), englishName, NewNameMarks);
    }

    /// <summary>
    /// Sets the English text of <paramref name="key"/> and adds it, with the same text, to each other
    /// language that doesn't have the key (so no language shows "NF:"). Other languages' own texts stay.
    /// </summary>
    public void SetEnglish(string key, string text)
    {
        if (string.IsNullOrEmpty(text) || text.Contains('\0')) throw new ArgumentException($"Enter the text of {key}.");
        SetText(key, text, null);
    }

    /// <summary>
    /// Sets the text of <paramref name="key"/> in one language (0 English ... 4 Spanish): the first entry with the key
    /// changes, or one is added at the end of that language's file. Other languages stay as they are.
    /// </summary>
    public void SetLanguageText(int language, string key, string text)
    {
        if (language < 0 || language >= _languages.Length) throw new ArgumentOutOfRangeException(nameof(language));
        if (string.IsNullOrEmpty(key) || key.Any(c => c <= ' ' || c > '~'))
            throw new ArgumentException($"\"{key}\" isn't a key: use letters, digits and punctuation, without spaces.");
        if (string.IsNullOrEmpty(text) || text.Contains('\0')) throw new ArgumentException($"Enter the text of {key}.");
        if (_languages[language].Get(key) == text) return;
        _languages[language].Set(key, text);
        _changed[language] = true;
    }

    /// <summary>Takes every entry with <paramref name="key"/> out of one language; true when there was one.</summary>
    public bool RemoveLanguageText(int language, string key)
    {
        if (language < 0 || language >= _languages.Length) throw new ArgumentOutOfRangeException(nameof(language));
        if (_languages[language].Entries.RemoveAll(e => e.Key == key) == 0) return false;
        _changed[language] = true;
        return true;
    }

    private void SetText(string key, string text, IReadOnlyList<string>? marks)
    {
        if (string.IsNullOrEmpty(key) || key.Any(c => c <= ' ' || c > '~'))
            throw new ArgumentException($"\"{key}\" isn't a key: use letters, digits and punctuation, without spaces.");
        if (_languages[0].Get(key) != text)
        {
            _languages[0].Set(key, text);
            _changed[0] = true;
        }

        for (var i = 1; i < _languages.Length; i++)
        {
            if (_languages[i].Get(key) != null) continue;
            _languages[i].Set(key, text + (marks?[i] ?? ""));
            _changed[i] = true;
        }
    }

    /// <summary>GTEXT.LMP's bytes: the original ones when nothing changed.</summary>
    public byte[] ToArray()
    {
        if (!Changed) return _original;
        for (var i = 0; i < Files.Count; i++)
        {
            if (_changed[i]) _lmp.ReplaceEntry(Files[i].File, _languages[i].ToArray());
        }

        return LmpWriter.Pack(_lmp);
    }
}
