using System.IO;
using System.Text;

namespace JetBlackEngineLib.Data.CutScenes;

/// <summary>One subtitle: the key a cutscene names it by and the text the game shows.</summary>
public sealed record CutsceneSubtitle(string Key, string Text);

/// <summary>
/// A level's cutscene subtitles, e.g. cuttown_cut.uni in cuttown.lmp.
/// <para>
/// Layout: the subtitles one after another, each an ASCII key and a NUL, a NUL more when needed to reach an even
/// offset, then the text in UTF-16LE and a 16-bit NUL. Four zero bytes end the file.
/// </para>
/// <para>
/// How the game uses them (FUN_00158848 sets up a scene, FUN_00155528 runs its keyframes): the scene's header
/// names its first subtitle ("introcut_text1"). Its first "Next subtitle" keyframe shows that one and each later
/// one shows the subtitle after it in the file, whatever its key; "Hide subtitle" hides it. At the end of the file
/// (the zero bytes) "Next subtitle" leaves the last one. The game looks in the level's archive for the first .uni
/// file with "_cut" in its name, and keeps only the files of its language when it loads the archive: no suffix
/// for English, then _fr, _ge, _it and _sp (<see cref="LanguageOf"/>).
/// </para>
/// </summary>
public sealed class CutsceneSubtitles
{
    /// <summary>The language suffixes of subtitle files, after the English file (no suffix).</summary>
    public static readonly IReadOnlyList<(string Suffix, string Language)> Languages = new[]
    {
        ("", "English"), ("_fr", "French"), ("_ge", "German"), ("_it", "Italian"), ("_sp", "Spanish")
    };

    private const int EndLength = 4;

    private CutsceneSubtitles(List<CutsceneSubtitle> subtitles) => Subtitles = subtitles;

    /// <summary>The subtitles in file order (the order the game shows them in).</summary>
    public List<CutsceneSubtitle> Subtitles { get; }

    /// <summary>An empty file to add subtitles to.</summary>
    public static CutsceneSubtitles Create() => new(new List<CutsceneSubtitle>());

    /// <summary>Reads a subtitle file; throws <see cref="InvalidDataException"/> when its layout isn't understood.</summary>
    public static CutsceneSubtitles Read(byte[] data)
    {
        var subtitles = new List<CutsceneSubtitle>();
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
            subtitles.Add(new CutsceneSubtitle(key, Encoding.Unicode.GetString(data, text, end - text)));
            at = end + 2;
        }

        return new CutsceneSubtitles(subtitles);
    }

    /// <summary>The file's bytes, laid out as the game's own files are.</summary>
    public byte[] ToArray()
    {
        using var stream = new MemoryStream();
        foreach (var (key, text) in Subtitles)
        {
            Require(CheckKey(key));
            Require(CheckText(text));
            stream.Write(Encoding.ASCII.GetBytes(key));
            stream.WriteByte(0);
            if ((stream.Length & 1) != 0) stream.WriteByte(0);
            stream.Write(Encoding.Unicode.GetBytes(text));
            stream.Write(new byte[2]);
        }

        stream.Write(new byte[EndLength]);
        return stream.ToArray();
    }

    /// <summary>The index of the subtitle with <paramref name="key"/>, or -1.</summary>
    public int IndexOf(string key) => Subtitles.FindIndex(s => s.Key == key);

    /// <summary>
    /// The subtitles a scene shows when its header names <paramref name="firstKey"/> and it has
    /// <paramref name="count"/> "Next subtitle" keyframes (fewer when the file ends first).
    /// </summary>
    public IEnumerable<CutsceneSubtitle> ShownBy(string firstKey, int count)
    {
        var first = IndexOf(firstKey);
        return first < 0 ? Enumerable.Empty<CutsceneSubtitle>() : Subtitles.Skip(first).Take(count);
    }

    /// <summary>Why <paramref name="key"/> can't be a subtitle key, or null when it can.</summary>
    public static string? CheckKey(string key) =>
        key.Length == 0 ? "Enter a key, such as introcut_text1." :
        key.Length > EditableCutscene.MaxTextLength ? $"Use {EditableCutscene.MaxTextLength} characters or fewer." :
        key.Any(c => c <= 0x20 || c > 0x7E) ? "Use plain ASCII letters, digits and punctuation, without spaces." : null;

    /// <summary>Why <paramref name="text"/> can't be a subtitle, or null when it can.</summary>
    public static string? CheckText(string text) =>
        text.Length == 0 ? "Enter the text to show." :
        text.Contains('\0') ? "The text can't contain a NUL character." : null;

    /// <summary>"French" for "cuttown_cut_fr.uni", "English" for "cuttown_cut.uni" (the game's rule).</summary>
    public static string LanguageOf(string fileName)
    {
        // The game checks _sp., _it., _ge. and _fr. in that order and the last match wins (FUN_001390E8).
        var language = "English";
        foreach (var suffix in new[] { "_sp.", "_it.", "_ge.", "_fr." })
        {
            if (fileName.Contains(suffix, StringComparison.Ordinal))
            {
                language = Languages.First(l => l.Suffix == suffix[..^1]).Language;
            }
        }

        return language;
    }

    private static void Require(string? problem)
    {
        if (problem != null) throw new ArgumentException(problem);
    }

    private static InvalidDataException Invalid(string message) => new($"Not a cutscene subtitle file: {message}");
}
