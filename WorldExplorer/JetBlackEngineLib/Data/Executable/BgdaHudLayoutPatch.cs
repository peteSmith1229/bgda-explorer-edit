// ═══════════════════════════════════════════════════════════════════════════════
// GENERATED FILE — do not edit by hand. Made by make_hud_patch.py from hudlayout.py (HUD layout tools).
// Drop into WorldExplorer/JetBlackEngineLib/Data/Executable/BgdaHudLayoutPatch.cs
//
// HudLayoutPatch: the three-player HUD layout block (0x800 bytes at file 0x227000, the
//   stock executable's empty DVP overlay sections; a three-player build loads it at 0x00326000 and
//   HUDBOOT copies it home to 0x000DB800 before crt0 clears BSS), 294 words of code at
//   0x000DBA00, and the 16 places in the game's code that reach it.
// ═══════════════════════════════════════════════════════════════════════════════
namespace JetBlackEngineLib.Data.Executable;

internal static class HudLayoutPatch
{
    /// <summary>Where the block is in the file, and its size.</summary>
    public const int FileOffset = 0x227000, Size = 0x800;

    /// <summary>Where it lives while the game runs (and where the file's copy is loaded).</summary>
    public const int RuntimeAddress = 0x000DB800, LoadAddress = 0x00326000;

    /// <summary>The table: one 16-byte entry per player and part (player * 8 + part), from this
    /// offset in the block: s16 anchor x, anchor y, new x, new y, size (256 = 100 %).</summary>
    public const int TableOffset = 0x10, EntrySize = 16, PartsPerPlayer = 8;

    /// <summary>Part numbers in the table. Effects (the status icons) follow Icons' placement.</summary>
    public const int Panel = 0, Icons = 1, Gold = 2, Skills = 3, Effects = 4;

    /// <summary>SHA-256 of <see cref="BlockBase64"/>, checked before the data is trusted.</summary>
    public const string BlockSha256 = "bc7cc2e57f13b7857300d6d27f2fb02279d69ce4adb901b637c9d0a9d4586576";

    /// <summary>The block with the game's own layout in its table (player 3's icons brought on screen).</summary>
    public const string BlockBase64 =
        "SFVETAEAAAD/////AAAAAAAAAAAAAAAAAAEAAAAAAAAAAAAAAAAAAAABAAAAAAAAAAAAAAAAAAAAAQAAAAAAAAAAwAEAAMABAAEAAAAAAAAAAAAA" +
        "AAAAAAABAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAgAIAAIACAAAAAQAAAAAAAIACAACAAgAA" +
        "AAEAAAAAAACAAgAAgAIAAAABAAAAAAAAgALAAYACwAEAAQAAAAAAAIACAACAAgAAAAEAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
        "AAAAAAAAAAAAAAAAAAAAAAAAAABAAQAAQAEAAAABAAAAAAAATwQAAEABAAAAAQAAAAAAAEABAABAAQAAAAEAAAAAAABAAcABQAHAAQABAAAAAAAA" +
        "qwMAAEABAAAAAQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
        "AAAAAAAAAABEqoKPAwABJDkAQRQOAAE8CLgijDYAQAQAEQIAIRBBABC4TIQSuE6EFLhNhBa4T4QYuFiEJsiNASYIzwElyCEDAP8BJyXIIQMpACAT" +
        "0KSZgxEAYBQAeQ8ABgAgEwBxDgBAYQwAQGkNAABYjCUFAAAQAFitJQBhDAAAaQ0AAGyMJQBsrSUAcM4lAHDvJf//GTQIAOADAQACJAN5DwABAAEk" +
        "CgBhFAAAAAADACATAAAAAEBgDABAaA0ALXCAAS14oAH/Bxk0CADgAwEAAiTgvIGPQAgBACNgwQEjaOEBLXCAAS14oAH/Bxk0CADgAwEAAiQIAOAD" +
        "LRAAAP//IjAjEEwAGABYABIQAAADEgIAIRBNAAEAQgQtEAAAKxgiAwsQIwMCDAEA//8hMCMILgAYADgAEggAAAMKAQAhCC8AAQAiBC0IAAArGCED" +
        "CwgjAwAMAQAIAOADJRBBAA4AATwIuCGMQAAgBAAAAADQ/70nAAC//wgAsP8QALH/gG4DDC0YAAA0AEAQAACRlBgAgYwOAAMkMAAjFBQAgYwAEAM8" +
        "LQAjFBAAg5T/f2Mw//8xJioIcQALiGEAAIkRACAAkCQhiDACKwgRAiMAIBAAAAAACAACjv8AQjBAAAEkCQBBEPYAQzAEAAEkBABhFAAAAAC+bgMM" +
        "AAABjgAAAq7x/wAQEAAQJgAAAY4EAAOO/wchMP8HYzAlCCMA+P8gEAAAAACAbgMMAQADJL5uAwwAAAGOAAACroBuAwwCAAMkvm4DDAQAAY4EAAKu" +
        "gG4DDC0YAADq/wAQAAAAAAAAv98IALDfEACx3zAAvSde1AQIAAAAAA4AATwIuCGMAwAhBAAAAABLDwUIgP+9J9D/vScAAL//CACw/xAAsf9FABA8" +
        "Km8DDNBYEI4UAAAQAAAAAEsPBQiA/70nDgABPAi4IYwDACEEAAAAAMsQBQhw/70n0P+9JwAAv/8IALD/EACx/0UAEDw7bwMM0FgQjgMAABAAAAAA" +
        "yxAFCHD/vSdFABE80FgxjoBuAwwtGAAADABAEAAAAAArCBECCQAgEAAAAAC+bgMMCAABjggAAq6+bgMMGAABjhgAAq72/wAQIAAQJgAAv98IALDf" +
        "EACx3wgA4AMwAL0nQAgCAA4AAzwIuGGs0PIFCDilgyfACBMAAQAhNA4AAzwIuGGswvQFCAAAhI7ACBMABAAhNA4AAzwIuGGsSvUFCJQAoo/ACBMA" +
        "DgADPAi4YaxY9QUIKgACPPD/vScAAL//wAgEAAMAITQOAAM8jPcFDAi4Yaz//wEkDgADPAi4YawAAL/fCADgAxAAvSeAYAMAIWCCAQAAmY3Ar4GP" +
        "RKqMjwMADSQUAI0VwGADAAIAjCUAYQwADgANPCFgjQEQuI2FFLiOhRi4j4UjyC0DGAAvAxLIAAADyhkAIcguAxK4jYUWuI6FIwgtABgALwASCAAA" +
        "AwoBACEILgAAEIFEsfsFCAAAAABACBQAAgAhNA4AGTwIuCGvMvwFCAAWAgACAAEkIwgyAMAIAQACACE0DgAZPBLqBAgIuCGv//8BJA4AGTwIuCGv" +
        "cPwFCBABv98AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAMgAIPABgCCUOIAk8" +
        "ALgpJTIACjyAZ0olAAALjQQACCUAACut/P8KFQQAKSUGAAQIAF5CJAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";

    public sealed record Hook(int Va, uint Stock, uint Patched, string What);

    /// <summary>The words the patch changes in the game's code: the stock word and the patch's.</summary>
    public static readonly Hook[] Hooks =
    {
        new(0x00100010, 0x24425E00u, 0x080C99E0u, "crt0: copy the block home"),
        new(0x0017CB3C, 0x2783A538u, 0x08036F53u, "per-player HUD: panel"),
        new(0x0017D304, 0x8E840000u, 0x08036F58u, "icon loop: potion / ammo slot"),
        new(0x0017D524, 0x8FA20094u, 0x08036F5Eu, "icon loop: effect icon"),
        new(0x0017D55C, 0x3C02002Au, 0x08036F64u, "icon loop: level-up icon"),
        new(0x0017D75C, 0x0C05F78Cu, 0x0C036F69u, "skill bar"),
        new(0x0017EEC0, 0xC782AFC0u, 0x08036F76u, "flying gold number: target"),
        new(0x0017EEDC, 0xC4600000u, 0x44990000u, "flying gold number: x (mtc1 t9, f0)"),
        new(0x0017F0C4, 0x00021600u, 0x08036F94u, "gold readout"),
        new(0x0017F17C, 0x0C04EA12u, 0x0C036F9Au, "gold coin"),
        new(0x0017F1BC, 0xDFBF0110u, 0x08036FA1u, "end of the gold function"),
        new(0x001411E4, 0x0C04D45Eu, 0x0C036ED6u, "sprite writer 0x140EB0: submit"),
        new(0x00141668, 0x0C04D45Eu, 0x0C036ED6u, "clipped sprite writer 0x141210: submit"),
        new(0x00141878, 0x0C04D45Eu, 0x0C036ED6u, "rectangle writer 0x1416A0: submit"),
        new(0x00143D28, 0x27BDFF80u, 0x08036F1Bu, "text writer 0x143D28"),
        new(0x00144328, 0x27BDFF70u, 0x08036F2Cu, "text writer 0x144328")
    };
}
