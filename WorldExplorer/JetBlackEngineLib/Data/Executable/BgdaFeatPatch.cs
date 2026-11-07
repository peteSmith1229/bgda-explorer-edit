// ═══════════════════════════════════════════════════════════════════════════════
// GENERATED FILE: WorldExplorer/JetBlackEngineLib/Data/Executable/BgdaFeatPatch.cs
// by make_worldexplorer_feats6.py from featpatch.py version 6 (and featpool.py, featpatch_v5.py, featpatch_v4.py,
// featpatch_v3.py, gtext_feats5.py and the icons in ICONS.LMP). Don't edit by hand: run the script again.
//
// The feats patch as data: four spells borrowed from the game's monsters (Xantam's Fire for Adrianna; Blade Barrier,
// Antimagic Field and Righteous Might for Kromlech) and Power Attack for Kromlech (the game's own feat, which it never
// offers), each on or off by itself, in game code nothing runs (the pool), so they need no three-player build and
// WorldExplorer can turn three players on or off underneath them.
// ═══════════════════════════════════════════════════════════════════════════════
namespace JetBlackEngineLib.Data.Executable;

/// <summary>One spell of the patch: its bit in <see cref="FeatPatch.SpellsAt"/>'s mask, hero class, feat bit, cast type,
/// cast code, ability name, icon, and its level-up list entry ({class, 0, entry}) and ability record with the default settings
/// (Power Attack: none, and no icon: the game's own record and icon are the ones it uses).</summary>
internal sealed record SpellPatchData(string Key, int Mask, int HeroClass, int Bit, int CastType, uint Cast, string Name,
                                      string IconName, string EntryBase64, string AbilityBase64, string IconBase64)
{
    /// <summary>The spell has its own ability record in the data block (every one but Power Attack, whose record is the game's).</summary>
    public bool OwnRecord => AbilityBase64.Length > 0;
}

/// <summary>featpatch.py version 6 as data (see BgdaExecutable.BorrowedSpells.cs).</summary>
internal static class FeatPatch
{
    public const int Version = 6;

    /// <summary>
    /// The pool: 61 stretches of the game's own code that nothing runs (32,724 bytes), the same addresses and bytes
    /// in the stock executable and every three-player build (featpool.py says how they were found).
    /// </summary>
    public static readonly (int Start, int End)[] Pool =
    {
        (0x00102910, 0x00102B78), (0x00102D80, 0x00103000), (0x0010C338, 0x0010C6D0), (0x0010CD50, 0x0010CF50),
        (0x00118978, 0x00118D70), (0x00129318, 0x00129538), (0x00129C48, 0x00129D78), (0x0012A710, 0x0012A8A0),
        (0x0012BAE8, 0x0012BF90), (0x0012CD30, 0x0012D080), (0x0012F790, 0x0012FA08), (0x00130540, 0x00130708),
        (0x00131170, 0x001312C0), (0x00132018, 0x00132170), (0x00132D90, 0x001333F0), (0x00134CE0, 0x00134E18),
        (0x00134F18, 0x001350D8), (0x0013CDF8, 0x0013D000), (0x0013D218, 0x0013D430), (0x00166E28, 0x00166FC0),
        (0x0016ACB8, 0x0016AE88), (0x00191A20, 0x00191D20), (0x00196AB8, 0x00196C18), (0x00198968, 0x00198AC0),
        (0x00199A60, 0x00199B98), (0x001A28B0, 0x001A2A08), (0x001A9A38, 0x001A9B60), (0x001AE930, 0x001AEA30),
        (0x001B8DA0, 0x001B8F18), (0x001B9570, 0x001B9688), (0x001B9E68, 0x001B9F68), (0x001BAFF8, 0x001BB100),
        (0x001C36E8, 0x001C3870), (0x001CBA38, 0x001CBB48), (0x001D32D8, 0x001D3420), (0x001E8558, 0x001E8790),
        (0x001FF340, 0x001FF4E0), (0x00204DE0, 0x00204EE0), (0x00206460, 0x00206800), (0x002068C8, 0x00206AC0),
        (0x002071C0, 0x00207400), (0x00207488, 0x002075C0), (0x00207A08, 0x00207C00), (0x00208160, 0x00208320),
        (0x002089C0, 0x00208B10), (0x00208CB0, 0x00208E48), (0x0020AC28, 0x0020AD30), (0x0020B890, 0x0020BCB8),
        (0x0020C080, 0x0020C280), (0x0020C4C8, 0x0020C61C), (0x0020DA88, 0x0020DBC8), (0x0020E6E8, 0x0020EB58),
        (0x0020F6A8, 0x0020F9C8), (0x0020FCE0, 0x0020FFC0), (0x00210058, 0x002102A0), (0x00210A00, 0x00210C08),
        (0x00210CA0, 0x00210E60), (0x0021BEE8, 0x0021C000), (0x0021C990, 0x0021CA98), (0x00221B88, 0x00222320),
        (0x00224B68, 0x00224CB8)
    };

    public const int PoolBytes = 32724;

    /// <summary>The game's bytes in the pool, one stretch after another: zlib, then base64.</summary>
    public const string PoolOriginalBase64 =
        "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"
        + "ibiGi4OflGht9CCSI1YqRXeYQr6MQylrpIJjPDDfJSbqIo6c/7wk7Yx2F+WiehT9fWsT+U7VSvSd6mgzj72ZNQx7ggqYk/NKOPYQfOJeV24LHewr"
        + "WSFIt7DJv1Ad1hs5fmSizFkjvebEXmjVm/UK6ZuAjfkZ3c+2sdOubf/PamuX5uz3ZA0prHM4SkPHD3H8xh5iu3cN2x0SLauW6Fi1lYBdoQ+IMstQ"
        + "JhwaVCxjOeDb12FVriBeo5LoSdiLPrTVi3L5iSLkRdhi+p/SG4CB2+A9/Oh+Q/LalUveo4mExPD+DZcFbCLPhfVnqbgG8kyOX1Ocg82zxzfrX6DS"
        + "f16ciSLaSJS0f5v0T25e2m0zmihK/O6MDVrUDt3Ovz/qli3SYG1I/2/ueGzHeHxAjCtud06PGZS+k/2OIkkWnay19qjOCtuDGtutCnwGieENh/Zh"
        + "/LZIXH8lyucw3hhOxt3aAc3Mtpy+X+AX1nMouw+t7Jjw6nAbScwDEsamI3/Za6qIKTohee3wczh0TOJ6jL8Hq1fvr916rCs26530R3hn/KKGoWwk"
        + "S+VnxoHxZjxsu92ELSgx3tw2t8dte7i3YP6oQj2i5DXGl+Hx5My2nzW5jbD+hICJcSJ6dILlln0k3s/dgfr/bB9CtUevcXlO/zu8MxwVQnZnpGho"
        + "hnhMlI4HzJ0ZwAK9yvMc6yymRZPJPP0OeMjyH9a/QSxPYYxz2O/Xos2WHM2irf2GLJrRrdRKKt7uyRbDzbogHPqE4K1lQHoh72G9VdBT8MV4ljbr"
        + "1yXul9eTv21jXOqsP9mPPjbPBzw78qdLrwmaN14L69vkKy25KW6rLuLMj6DUTtteZrKuiGZmpPkx7dIXPNvkjmEKWanoLvim8GWjEeBh5OVoKC/2"
        + "QcsFPV5xbAJ37zWIOZ91AtvvZWJLIj+Xt6IkDzb5pbl5WOO1ZHnOZuAybUiLZkjiZ5U24Y/94T6T6c7jd1romyYTc+kqhtMyljnyH2J5fJUcHcpy"
        + "/Cvu2HldSuqSmGOYfsw7L3aJ62KiXxsNAU8deCZYLTzqzmMJMY95dXl+elPwOolx2SvoAN9yy4m3fxpjuFHE20UPzshCx+J3s/6G9DtvQ9dAN2NO"
        + "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"
        + "/wDZbDuJ";

    public const string PoolOriginalSha256 = "61d9c74b8a8f6a052479a3505c8458611f8b9ff67bfbd76018a2e7b8a48b36ce";

    /// <summary>How much of the pool the patch takes: its data block, its code and its four vtable copies.</summary>
    public const int PoolUsedBytes = 7200;

    /// <summary>The code (5,232 bytes in 10 pieces over the pool's stretches): the same whichever spells are on.</summary>
    public static readonly (int Va, string Base64)[] Code =
    {
        (0x00102910, "PACIjAcAABUAAAAAOACJjAgAKoUMACuNISCKAAgAYAEDAAUkMP+9J6AAsP+oALH/sACy/7gAs//AALT/yAC//yGAgAAMABGNAQAYJFoAIBIhIAAC"
            + "EAApjhAACa4UACmOFAAJrhgAKY4YAAmuEwASPJAtUiYcACDGvANBxgAAAUYcAADmOrUEDCEgAAIhQAAAIVAAAkgASZX9/wslAwBrLQIAYBHoAykl"
            + "MPgpJUgASaUBAAglCQALKfb/YBUMAEolEwASPJAtUibIpIiPtANJjjUAIBEAAAAAGwAJARBQAAAxAEAVAAAAABwAAMa4A0HGAAABRhAAAcaBCABG"
            + "AACi54AIAEYQAKLnFAABxoEIAEYEAKLngAgARhQAoucYAAHGgQgARggAoueACABGGACi5yEgoAMQAKUnIACmJ+IiBAwgAAcktAASkrUAE5IgALQn"
            + "AACEjhQAgBAEAJQm/P+REAAAAAAMAIiMNAAIMRAACST3/wkVAAAAACEoIAIhMEACIThgAgEACDyghgg1IUgAACFQAAACAAs8Dn4EDAgAazXr/wAQ"
            + "AAAAAEAACI7IpImPKsAJASEgAAKgALDfqACx37AAst+4ALPfwAC038gAv9+O/wAX0AC9JwgA4AMAAAAA"),
        (0x00102D80, "oP+9JxMAFjyQLdYmZK8EDJAChI44AHKQQABzkEgAaJBQAGmQHAqqjyYASKUoAEmlIbgAAAQAYBIAAAAA72IEDAAAAAAhuEAAGACUxgQAYBIAAAAA"
            + "AKiARAsAABAwANbGyKSIjwMACDEDAAktCkAJAIBACAArAAk80PEpJSFIKAEAADXFEAA2xSGAAAAqQBICQQAAEQAAAABASBAA//9KJiOYKgFABgsk"
            + "GFhrAjQAjJYhYIsBAGQMAANkDABIAKyvzMMEDCEggAEoAMHGAgABRhAAgsYAEABGUACg56bDBAxIAKSPKADBxgIAAUYUAILGABAARlQAoOdYALTn"
            + "BLsEDGAABCQiAEAQTACirwAQk0SgEIBGgD4BPAAIgUSCEAFGIADMxkITFUaAPwE8AHCBRCQAz8YCEBZGJAAARgAACUQATAkAA0wJACEgQABQAKUn"
            + "SACmjyE44AIhQIAClMQGDCFQAABMAKKPEwAIPCCTCCU4AEisXABJjAUAIBEAAAAADAApjQIAIBGAAAglOAAorb7/ABABABAmYAC9Jz2KBQgCAAIk"
            + "0P+9JwAAv/8IAKT/EACl/xgAsP8gALH/EwAQPJAtECYhiAAAQEERACFACAIIAKTflusEDAAEBSUKAEAUgEARACFACAI0BQWNlusEDAgApN8EAEAU"
            + "AAAAAAgApN+W6wQMEACl3wIAQBAhSAAAOABJjIBAEQAhQAgCKAUJrQEAMSYDACgq5/8AFQAAAAAcAAiOAQAIMQoAABEIAKTflusEDGAABSYIAEAU"
            + "CACk3zEABTyW6wQM8KmlJAMAQBQIAKTflusEDBAApd8AAL/fGACw3yAAsd8IAOADMAC9Jw=="),
        (0x0010C338, "wP+9JzAAv/8QALD/GACx/yAAsv8hgIAAIYigACGQwAAQACiOAACorxQAKI4EAKivGAAojggAqK8hIAACISigAyEwAABWLAQMITgAABMACDwQvQgl"
            + "OAAIrjwAAK4MAAiOAQAINQwACK4TABk8kC05JxwAIMa8AyHHAAABRhwAAObIpIiPIUASAUAACK7//wgkRAAIrjwABCYhKCACIGwFDCEwIAKyJQQM"
            + "ISAAAiFAAAAhSAAAIVAAAkgASaU0Qgw8AwALKQYAYBUAAAAA8EEMPAYACykCAGAVAAAAAHBBDDxMAEytnKSLjxkADTwNZq01GFhtAW48DTxf8601"
            + "IVhtAZyki68CbAsAUABNpVVVKSUBAAglCQALKef/YBUMAEolMAAEPHB5hCSXrwQMISgAAjAAv98QALDfGACx3yAAst8IAOADQAC9J8ikiI8KAAkk"
            + "GgAJARBAAABKAAAVUACKjEgAQBEAAAAADABLjUUAYBEAAAAAYP+9J5AAv/9gALD/aACx/3AAsv94ALP/gAC0/yGAgAAhiGABZK8EDJACJI5IAHKQ"
            + "UABzkBMACDyQLQglHAAAxhAAAcaBCABGAACi54AIAEYQAKLnFAABxoEIAEYEAKLngAgARhQAouc0AADFGAABxoEIAEYIAKLngAgARhgAouchIKAD"
            + "EAClJyAApifiIgQMEAAHJCAAtCcAAISOFACAEAQAlCb8/5EQDACIjDQACDEQAAkk+P8JFQBACTyAAomsISggAiEwQAIhOGACAQAIPKCGCDUhSAAA"
            + "IVAAAAMACzwOfgQMYABrNev/ABAAAAAAISAAApAAv99gALDfaACx33AAst94ALPfgAC036AAvSfmwwYIAAAAAND/vScAAL//CACw/xAAsf8YALL/"
            + "IACz/96KBQwAAAAAFgAIPHwrCY2UKwqNAlwJAAM8DCQsAGwVAlwKAGokDCQpAGwVAEwJAABUCgADVAoAIZgqARMAEDyQLRAmDAARjoAAEiYgACAS"
            + "AAAAAAAASI4EAAktGQAgESFQYAIIFgskGFgLASFQSwFEAAw8RU6MNSFwAAA/AA0kAABPjQYA7BEAAAAAAQDOJfv/zRVYAEolCgAAEAAAAAAIAFgm"
            + "FgAZJAAAD4///zknAABPrQQAGCf7/yAXBABKJQAATK3//zEm4P8AEGAAUiYAAL/fCACw3xAAsd8YALLfIACz3wgA4AMwAL0n"),
        (0x0010CD50, "oP+9JyAAsP8oALH/MACy/zgAtOc8ALXnQAC250gAv/8hgIAAPAAIjjsAABEAAAAADAARjTgAIBIhIKAAl68EDCEoAALMwwQMNAAkhiBBATwACIFE"
            + "AgUBRqbDBAw0ACSGIEEBPAAIgURCBQFGKAASJJykiI8ZAAk8DWYpNRhACQFuPAk8X/MpNSFACQGcpIivAyQIAMzDBAwcAKSvHAABxgIAAUYQAAHG"
            + "AAABRoAFFEamwwQMHACkjxwAAcYCAAFGFAABxgAAAUYAABVGAAC25wQAoOcYAADG8EIBPAAIgUQAAAFGCACg5xAAoK8UAKCvGACgrysABDyI7oQk"
            + "ISigAxAApifSPwUMAGCARP//UibX/0AWAAAAACAAsN8oALHfMACy3zgAtMc8ALXHQAC2x0gAv98IAOADYAC9J+D/vScAALD/CACx/xAAv/8hgIAA"
            + "IYigABAACDzwKgglOAAIriEgAAIxAAU8VDMEDNAQpSQ8AAiODAAAEQAAAAAMAAmNBwAgEUQACo4FAEAVPAArjf3/DDz//4w1JFhsATwAK61mbAUM"
            + "PAAEJjwABCaMIAQMAgAFJCEgAAI2LQQMISgAAAEAKDIhIAACAACw3wgAsd8QAL/fAwAAESAAvScauQQIAAAAAAgA4AMAAAAA"),
        (0x00118978, "CQCDECAAASQJAIEQIQABJBIAgRAiAAEkKgCBEAAAAABV+QUIAAAAAE35BQgAAAAAISDAAgAIBTRzrwQMPCgFABMACDyQLQglIUACAaADEpGoAxGR"
            + "1/kFCDEABDzw/70nISDAAgAQBTRzrwQMPCgFABMACDyQLQglQEgCACFICQHAAymVGQApJTIACiQbACoBEkgAAAAAqa8TAAQ8UuwEDPAxhCQhKEAA"
            + "AACpjwBgiUQgY4BGAGiARBAAvSfR+QUIISCgA/D/vSchIMACACAFNHOvBAw8KAUAEwAIPJAtCCUhQAIB0AMJkQAAqa/YAwmRBACprxMABDxS7AQM"
            + "EDKEJCEoQAAAAKmPAGCJRCBjgEYEAKmPAGiJRGBrgEYQAL0n0fkFCCEgoAMhKEACkAJMjgQAjS1BAKARgGAMABMADTyQLa0lIXCsAZAFz40lAOAR"
            + "yKSYj6AF2Y0jyDgD//85J/ADuI0ryDgDHQAgEwwAmIwQABgzGwAAE///7yWQBc+tQDAGAEA4BwDA/70nAAC//wgApP8QAKb/GACn/yAAqP8oAKn/"
            + "MACq/zEABDz4lYQkl68EDCEoQAIAAL/fCACk3xAApt8YAKffIACo3ygAqd8wAKrfQAC9JwIAABAhKEACkAXArZACTI6AYAwAEwANPJAtrSUhcKwB"
            + "YAXPjQ8A4BFwBdiNCAAZLwwAIBMhwLgB0AMYk2QAGCdkABkkGFjYADIAayUaAHkBEjAAABhY+AAyAGslGgB5ARI4AAAIAOADAAAAAED/vSewAL//"
            + "LADAxhAAgcaBCABGAACi54AIAEYQAKLnFACBxoEIAEYEAKLngAgARhQAoucYAIHGgQgARggAoueACABGGACi5yEgoAMQAKUnIACmJ+IiBAwgAAck"
            + "AACkxgQApcYsAMbGgjEGRoAxBkYhEAAAIAC5JwAAKI8eAAARBAA5J/z/FBEAAAAADAAJjTQAKTEQAAok9/8qFQAAAAAQAADFEACBxgEAAUYUAALF"
            + "FACBxoEQAUbCAARGQhAFRsAYAUYACIBENhgBRur/AUUAAAAAAgAARoIQAkYAAAJGNAAGRuT/AEUAAAAAhgEARuH/ABAhEAABsAC/3wgA4APAAL0n"
            + "EwAWPJAt1iaQAoSOABAFNHOvBAw8KAUAQEACACFAyALAAxKVkAKIjgQACS0dACARgEAIACG4yAJQBemODQAgEQAAAAAMADCNCgAAEgAAAADIpIiP"
            + "IUASAUAACK4hIAACMAAFPFQzBAw4e6UkDQAAEAAAAAAEuwQMUAAEJAkAQBAhgEAAISBAACEogAISpwQMITBAAlAF5CYhKAACIGwFDCEwAAI9igUI"
            + "AgACJA=="),
        (0x00129420, "EwAIPJAtCCUQAAmNYAIKJQYAIBEAAAAASABL3QUAZBH//ykl+v8AEFAASiWO7QUIAAAAAOA4DCQYYKwAKgANPICJrSUhaKwBMAGu3SR4xAEiAOAV"
            + "uDO4JQ8AGSQgAAKH//8DJAUAQxD//zkn+/8gF1AAGCcIAOADAAAAACEQAAAhGEIBAABjjCEIAgMAACOsBABCJCgAQyj5/2AUAAAAACgAQ4UoAAOn"
            + "LAACJCEYQgEAAGOMIQgCAwAAI6wEAEIkRABDKPn/YBQAAAAASABD3UgAA/8lcMQBMAGu/QgA4AMAAAAA"),
        (0x00129C48, "wP+9JzAAv/8QALD/GACx/yAAsv8hgIAAIYigACGQwAAQACiOAACorxQAKI4EAKivGAAojggAqK8hIAACISigAyEwAABWLAQMITgAAAwACI4BAAg1"
            + "DAAIrsikiI8hQBIBQAAIrhAACDzwKgglOAAIrjwAAK48ACiOAgAJPCRQCQFEAAquJUAJATwAKK4cACDGIEEBPAAIgUQAAAFGHAAA5jwABCYhKCAC"
            + "IGwFDCEwIAKyJQQMISAAAiEgAAIwAAU8VDMEDDh7pSQwAL/fEACw3xgAsd8gALLfCADgA0AAvScqAAE82F0hNCMIQQAMACEsAwAgFAAAAAAIAOAD"
            + "AABEjCoAATzYXSE0IwhBABMABDy4MoQkISCBAAgA4AMAAISM"),
        (0x0012A710, "kAKMjAQAjS0xAKARgGAMABMADTyQLa0lIXCsAWAFz40PAOARDQD4MA0AABMBALgoCwAAF3AF2I0IABkvCAAgEyHAuAHYAxiTZAAZJCPAOAMYwLgA"
            + "MgAYJxoAGQMSKAAAUAXPjRoA4BGgBfgwGAAAE8ikmI+ABdmNI8gZAwoAOS8TACAXDADvjREA4BGABdit0P+9JwAAv/8IAKT/EACl/xgApv8gAKf/"
            + "ISDgATAABTxUMwQMMDulJAAAv98IAKTfEACl3xgApt8gAKffMAC9J/6YBQgAAAAA4P+9JwAAsP8IALH/EAC//yGAgAAhiKAAEwAIPBC9CCU4AAiu"
            + "ISAAAjEABDyAXoQkl68EDCEoAAI8AAiOAwAAEQAAAABmbAUMPAAEJjwABCaMIAQMAgAFJCEgAAI2LQQMISgAAAEAKDIhIAACAACw3wgAsd8QAL/f"
            + "AwAAESAAvScauQQIAAAAAAgA4AMAAAAA"),
        (0x0012BAE8, "EwAWPJAt1iaQAoSOAAgFNHOvBAw8KAUAIUDCAqADEpGoAxORvAmqjyYAUqUoAFOlkAKIjgQACS0iACARgEAIACG4yAJABemOEAAgEQAAAAAMADCN"
            + "DQAAEgAAAADIpIiPsAPJjiFACQFAAAiutAASorUAE6IwAAQ8cHmEJJevBAwhKAACDwAAEAAAAAAEuwQMwAAEJAsAQBAhgEAAISBAACEogALOMAQM"
            + "sAPGjrQAEqK1ABOiQAXkJiEoAAIgbAUMITAAAj2KBQgCAAIkEwAWPJAt1iaQAoSOACAFNHOvBAw8KAUAIZBAAJACiI4EAAktJwAgEYBACAAhuMgC"
            + "cAXyruAD045gBemOEQAgEQAAAAAMADCNDgAAEgAAAADIpIiPIUgTAUQACa5IAADGSEIBPAAIgUQCAAFGJAAARgAACkQjUAoBQAAKrg0AABAAAAAA"
            + "BLsEDFAABCQNAEAQIYBAACEgQAAhKIACKoMHDCEwYAJgBeQmISgAAiBsBQwhMAACMAAEPCh5hCSXrwQMISiAAj2KBQgCAAIkkAKMjAQAjS0fAKAR"
            + "gGAMABMADTyQLa0lIXCsAWAFz40ZAOARAAAAAAwA740WAOARAAAAAEgA4MXkA6HFAgABRoA/ATwACIFEAAABRuD/vScAAL//CACw/xAAtOchgIAA"
            + "aAEUxgIAFEa8cAUMaAEA5mgBFOYAAL/fCACw3xAAtMcIAOADIAC9J7xwBQgAAAAA"),
        (0x0012BD90, "8P+9JwAAv/9SqgUMAAQFPAEAQygLEGMABgBDKAUAASQKECMAEwADPJAtYyQhGGIAAAC/3wgA4AMQAL0n8P+9JwAAv/9SqgUMAAAAAAEAQygLEGMA"
            + "BgBDKAUAASQKECMAAAC/3wgA4AMQAL0nkAKEjgQABTRzrwQMPCgFAJACiI4EAAktDwAgEYBACAATAA08kC2tJSFwqAEhSKIB6AMpkZAFya3IpIqP"
            + "8AOrjSFQSwGgBcqtMAAEPFh5hCSXrwQMISiAAj2KBQgCAAIk4P+9JwAAv/8IAKX/ISiAADIABDwS6gQMGEqEJCEgQAAIAKXfgD8BPABggUTQyQcM"
            + "AAAAAAAAv98IAOADIAC9Jw==")
    };

    /// <summary>
    /// The four vtable copies (0x80 bytes each): the game's vtable at GameVa as the file has it, with these words
    /// replaced (FireTrailMaker; FireTrail: her trail's update, no touch; BladeBarrierSpell: its destructor and update;
    /// AntimagicFieldSpell: its destructor).
    /// </summary>
    public static readonly (int Va, int GameVa, (int At, uint Word)[] Words)[] VTables =
    {
        (0x00129320, 0x00316718, new (int, uint)[] {  }),
        (0x001293A0, 0x00316798, new (int, uint)[] { (0x24, 0x0010C480), (0x3C, 0x0012E418) }),
        (0x0012BD10, 0x0031B650, new (int, uint)[] { (0x0C, 0x0012A7E8), (0x24, 0x00102910) }),
        (0x00102AF0, 0x0031B5D0, new (int, uint)[] { (0x0C, 0x0010CE8C) })
    };

    public const int VTableSize = 0x80;

    /// <summary>The data block.</summary>
    public const int DataVa = 0x00132D90, DataSize = 0x5B0, VersionAt = 4, CountAt = 0xC, AbilityCountAt = 0x10,
                     SpellsAt = 0x1C, EntriesAt = 0x80, EntryStride = 0x60, AbilitiesAt = 0x260,
                     AbilityStride = 0x50, EntryPriceAt = 0x4A, AbilityManaAt = 0x2C, AbilityDamageAt = 0x26,
                     XantamDamageMinAt = 0x48, XantamDamageMaxAt = 0x50, BarrierDamageMinAt = 0x3A0,
                     BarrierDamageMaxAt = 0x3A8, FieldFramesAt = 0x3C0, MightDamageAt = 0x3D0,
                     MightDefenceAt = 0x3D8, PowerHitsAt = 0x3E8, PowerFramesAt = 0x3F0,
                     CastTableAt = 0x4A0, CastTypes = 34, GameCastTypes = 23;

    /// <summary>Power Attack's time: its doubled hits must come within this many frames of the cast (10 s).</summary>
    public const int PowerFrames = 500;

    /// <summary>The game's Power Attack record (the one its grant copies): its name, cast type and feat bit are checked.</summary>
    public const int PowerRecordVa = 0x0030E2E0;
    public const string PowerRecordName = "powerattack";

    /// <summary>The game's own cast table (its first 23 entries go into the data block's) and its default case.</summary>
    public const int GameCastTableVa = 0x00307BC0;
    public const uint CastDefault = 0x001628F0;

    /// <summary>
    /// The data block without what depends on the spells that are on (their count and mask, their list entries and ability
    /// records, the cast table): every spell's own settings at their defaults, the names of the icons, the HUD lines' keys.
    /// </summary>
    public const string DataCommonBase64 =
        "RkVBVAYAAAAAAAAAAAAAAAAAAACwBQAAxMUQAAAAAACamZk/AAAAQAAAwEEAALRDAIA7RQAAAEIAAwMFBQUAAAAAAAAAAQAAAAEBAgIDAAAAAgME"
        + "BQYAAAAAAAAAAAAAaWNvbl94YW50YW1maXJlLnRleAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAEBggKDAAAAAgKDRAUAAD0AQAAMgAAAAAAoEEAACBBAADIAPoALAFeAZABAAAAAAAKFB4oMgAAAAoUHigyAAD0AQAAmpmZPgABAgMEBQAA"
        + "9AEAAAAAAAAAAAAAAAAAAGljb25fYmxhZGViYXJyaWVyLnRleAAAAAAAAAAAAAAAaWNvbl9hbnRpbWFnaWNmaWVsZC50ZXgAAAAAAAAAAABpY29u"
        + "X3JpZ2h0ZW91c21pZ2h0LnRleAAAAAAAAAAAAGFudGltYWdpY2ZpZWxkX2h1ZAAAAAAAAAAAAAAAAAAAcmlnaHRlb3VzbWlnaHRfaHVkAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAIKowAPCpMAB4qjAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA==";

    /// <summary>The spells, in the order the data block lists the ones that are on (the order of BorrowedSpell).</summary>
    public static readonly SpellPatchData[] Spells =
    {
        new("xantam", 0x1, 1, 26, 10, 0x00102D80, "xantamfire", "icon_xantamfire.tex",
            "AQAAAAAAAAB4YW50YW1maXJlX25hbWUAAAAAAAAAAAAAAAAAAAAAAHhhbnRhbWZpcmVfZGVzYwAAAAAAAAAAAAAAAAAAAAAAAAUQAAAAAAAAAAAE"
            + "AAAAAAAAAAAAAAAA",
            "eGFudGFtZmlyZQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAKAGQAAwABAAIAAAAAAHBB/wBaAB4AAAD+AXgAAACg/wAAAAAAAAAAAAAABAAAAAA=",
            "LAAsABEA1wBEAAAAwmQWAIAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAEgAAAAAAAEA4AAAAAAAAAAAAAAAAAAQBQAAAAAAAAAAAAAAAAAAAA"
            + "UQAAAAAAAAAQAAAAEAAAAFIAAAAAAAAAAAAAAAAAAABTAAAAAAAAAECAAAAAAAAIAAAAAAAAAAAAAAAAqSwIOeFyGX/reB4D63geN4gdBgjreB4H"
            + "eSUzf81bFhTreB4x63geCd5tGwPTXhR163geM/rLVX+tPw8F63geJ+RyHA7reB4B6XYeKmwiLn/HThd/63geAlgcJn/reB4K63geAb0/DF7reB44"
            + "22UUfuuLIn/reB4lkiIHCrePVH/reB4B63geMet4HgbreB4FwUgQPsRVFQSjTB9Oz1wWH89/KH+zNQpK2mYWfFUaJH/reB4CYCEkdY4fBg/reB4T"
            + "63geAd+/bn/FSQ5gykoNecRLHH/0rzp//eCCf5kjBxL3u0V/eSUzf+p4HjnzpzR/63geBet4HgHreB4C63geLOt4HjTreB4FtDQKWOt4HgLukiV/"
            + "12ogQOt4HgW+QAxWpkAWf+t4HgO5OgtTm21Bf/jCS3/reB4a/Nhvf5sjByuTIAYX63geKNRZEX3reB44gBoFAY4fBgf60F5/63geGogdBgS5Qg8m"
            + "tTQKYN1qGhrebxl954Eff9RdE3j702R/63geIptzRn/reB4WiR8GAo8nCQHAQgxk63geD+t4HgpwJC9+33Eaful2Hh371mt//d5/f+d0HQHVYRgy"
            + "86s3f+t4HhjreB4I/eCDf8dIDXSqhE9/ZiUjcKorCE/reB4GgxsFArAzCj1aIgx/pC8KGut4HgfreB4E/Npzf1EZIn/reB4C+cZQf+t4HjDreB4C"
            + "53QdB++XKX/reB4K63geBNRuIn/KTg9v63geOddhFHzGRw1w63geCoVHOX/reB4gxUcOafe9R3/reB429rlGf/OqOH+LHgYG63geA24pL3/reB4X"
            + "3GoXfet4HhvreB4B6nceBZ4lBzPVg0V/o0wfTut4HjPAShEx63geOfe7RH/fbBsp63geAfa1QH/6zVp/wlIUDh4JBX/RrmZ/63geMI4fBhLreB4g"
            + "2F4SfvGiMX/reB4U63geFOt4HgN9KDV/63geKut4Hg33vEZ/3moWf/WyPX/OVhFu648nf3YlMn/reB4fqS0JM+t4HhCPTyp/yl0kf+t4HgKUIQcb"
            + "63geMut4HgaWIwcP+s5cf9hmGRLAQw1QpEMuXOXGdH/reB4vgy0xc/a5Q3+eLAoN979Mf4g2KGPreB4BnEArW7NRJk7html/5Hscf+t4Hh+/PQtx"
            + "63geA+mII3/reB4L63geMZw8MWOLVz9/58h1f++bLH+gJwgn/eKHf92iW3+aIwcX6XYdBZ8lBzD70WJ/8Z8tf342IF7GoGB/7ZMof4c+Hn/ESA9Y"
            + "vlojSOt4HgOpLAhC63geKOt4HgX93Xt/kT0mXHomNH/71GZ/nSUHHut4HgFeHyh/hBsFA+t4HgNiIyp/0FYRdfe6RH/reB4SAYAAAAAAABAOAAAA"
            + "AAAAAAAAAAAAAAEAUAAAAAAAAAADgAAAAAAAEA4AAAAAAAAAGAAAABcAAABSAAAAAAAAAAAAAAAAAAAAUQAAAAAAAAAAAAAAAAAAAFMAAAAAAAAA"
            + "ioAAAAAAAAgAAAAAAAAAAAAO2UoA+0fUAEdqQQBq20wAAGeunAAoKPgAmVjCnJthtqzpTGGsHkEerGHU6ay2Sq1Gm6yt0pnVrdUo0q2sZ0bbagAA"
            + "akcAAEerAADZhAAAhGEAIfhYAACcKAAAAK4AAAADfaEAQttMAGpn9gCuY9WcAJuqKQBhm9mmCB49nEBAtKwb1UGsi/aLrEFMG6y0oaMHQO2TOgg6"
            + "kzphOqPtmwdjrgAAZ2oAANtCAAB9AwAAR0AAIfceAKYpmwAAnKoAAH4AO2F9CsutrjW8of+PvJMAxVbJABhhmBm/QLOBaUEqwN+8MfTBvMH0wcvB"
            + "wDE735cxVCpUsmKzYrJUmFQxl8n/kwD3rqEANQatABl+YQAAQWnCAEC/nABhGAAAVsUAAEcAzLYSnHGtZ5yXxGP7baMAI8/sKYfp/JxnIvKPmRsq"
            + "n9NtkbKflw2yDXGfkZHM07MxwSrNsi3yLbLN/J8xs+xjowCrZ8QAnBKtAJxHtgAAG5mPACJnIQDphykAz3gAAAAOc9QhkB5BnH3Uk8Jai/bqAEZd"
            + "agA2Xa6c+XeqKWIIKoAt9tOA45PjgNNBLYAq1A1uYoB7gPmAe4A2gA2ARm6LWkQA1H0KAB6QIQBzgQAAqggAKa4lAJxqzgAA6nIAAAAKomEAP6Lp"
            + "+BML1IH1tKHZAASGeABGchgAbiW/nA9zD/Ftofzx3tTe8fxKbfEPYfIEDyezO25Gs0ZGO/KgBAS00YQACxP4ACU/AACinAAAv3MAIRglAAB4uAAA"
            + "2R0AABlOMMbKAFgUJgDPp/UAHjwAAIaSAAAdfAAAt4UAAGZqxIceZxWuz64VrliuxNAwh+m6EWpAhtSF1JRAfBFa6f1Q/gAAJq8AAMq6AACmHwD6"
            + "GgAAABwAAAAdAAAAHQAAAAAy0TgABQlfKU2/uRcAqqcAAHodAACabAAAaOEAAPMTd/uqJol+v0L16gmQYbHRq5mJzxNW8HPhc9pWbL1Qmc4XLgAA"
            + "KacAjgBfAOgAxgBs8wAAAKcAAABeAAAAMgAAALiUZMdsQ76wW54CPEg3VRz65E8AAP2NTQD+knoApMbaPrt8HCGCIWghaJzuF/0+dDIyOOS7jpIA"
            + "OQBoWV16guj++lkAXr4yADJLHQBkXjwAALsAHQB6APAATQCoAAAAX/DgAKfwHQBg8M4BVwJsXsjoeefh+lNP+gCGXuEApzKUTs7kpACCAFcATwBX"
            + "74aOp1+aMlOnvl4FivpPvjO+5zNeb1sAAZSaAACnpAAAguAAAF8AqHnhADNJBQBQ6MMAOC4AX0IWAMhqxwDi22gAdWcAKf2pOJwBWImBw89e954e"
            + "2krrEcdA4tRX1MhAjRGUSo3EiR6VoZ7PSKFsWEPEjanOZwAAqNsAABZqAACKQgAA5vf9AOGBxgBbnAAASykAAFMAS3CGAIxndADiqmAK4psACqhh"
            + "AA4cSgCQLNROxdihyARrzOI7dUZXRi47u8wsBILS2KFe3djU2N1eSkvSgmEWmwCcgqoAAIpnAAC7cAAAb8VTACyQWQDzDgAAiiEAAABHv0YAapnV"
            + "pq7PcZypCN4OANRzfpxBHgYpk9TbgfaL1VGs3qx7rHGse6zVrFHVRqxi9lGsbZNUrFRBbaxR1GIIqpwAz66cAJlqAAC/RwAA24sAgX3UAJx+HgAh"
            + "DnMAAAB4mewAGLYMnGdKID+ZgyqrAASIIxlGrRgp1cS/q7yjvJ/eKqyylyCXsqwM3jG87FTTvJFin9UNYg1Gn1SfBNODmT8A6WecALYYAACZxQAA"
            + "v6MAqxjEAJx4rQAZUogAACMAyZsYAJgevxmzQGnCKkEAkGE7pgat7DWuoQ/Z/5P5Md4qzbKzs9Oy05izMc3J3t+fMfnBwcEPwcHB7DGf3ztpQQA/"
            + "v0AAGRgeAAAjcwAAk//7AKGugQCtfaYAYX4AAEcAzGYSAPmJZ6bepWOcKkAAA8+h+OpKTBlq3PaPEBs2kTQqNLI03jSyNPk0kTTMNNMPwTbN+S32"
            + "LfnNTMEP06FjQAAKZ3cAphL9AABHiQAAGxDZANxqGQBKPSkAz1IAAAAA86IA+F4oABOimaa+YZtEAOld9wDUhuUAQUUlAEwzRguAm/F3Npk2d/Eo"
            + "gK1GooC2TEqAYUEegB7UYYDp6bZh4aYAJXYAAF74AABeAAAAejMAAJ3gAACP4AAARJQAAAAAhisAAAKxAAAa2wAhoq7KAJvXpgBhuiYACLkrAEA8"
            + "g9EbrkG4i9uL/UGxGzCDK6O/QGOT/wiqk6lh/6Njm7+iKQAAZgAAAAIAAABFAAAAKy4AACanAAAZugAAIdcAAABFSwAAzuhOABwXGgCJQqjv5OTu"
            + "AAAzggAAOTMAADzhuhck7/AAFyEz+OgAvh9LFyNmEuF9hmqaatZ9grFdeE8A4ADvABoAkgAfAC4AAACouQAAAK8AAAAUeR8AxgXmAAAUpwAAu84A"
            + "AKQyegCoF9qViU7unsNm50kF4PNZTVWVUO8XxqhlUwC5AM5l/b48wzVe14k1gsLzRLk1TyaSNWAA2gBLAAEA2gAAAC4AAADO/gAAAGg37wCSQ/0A"
            + "HyxeAFnOBSwryE2naH8B4v6Czi9eAILOSwDH2Ng4VW/oU4bolY2+a0l/TXVNdEkueuCVldrW82aClIJvim90HHKVsK9eaGgAAaQ8AE0WHQD6uVAA"
            + "SUgAkmZvAB8dtwAAuUUAAE2+Q7h6zrWMzue14mhXGuIuANr+pwDHhi4A4s7OAHS3u4JmL0PiSes3V0O5AfO7SwJg7iyC8OJLV0t0qI24808aV3IA"
            + "SYK+AEjzZABDkgAALNgA79oCAACCOQAAgnQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=="),
        new("powerattack", 0x10, 2, 34, 18, 0x0012BDFC, "powerattack", "icon_powerattack.tex",
            "AgAAAAAAAABwb3dlcmF0dGFja19uYW1lAAAAAAAAAAAAAAAAAAAAAHBvd2VyYXR0YWNrX2Rlc2MAAAAAAAAAAAAAAAAAAAAAAAUFAAAAAAAAAAAA"
            + "BAAAAAAAAAAAAAAA",
            "",
            ""),
        new("bladebarrier", 0x2, 2, 43, 32, 0x0012BAE8, "bladebarrier", "icon_bladebarrier.tex",
            "AgAAAAAAAABibGFkZWJhcnJpZXJfbmFtZQAAAAAAAAAAAAAAAAAAAGJsYWRlYmFycmllcl9kZXNjAAAAAAAAAAAAAAAAAAAAAAUKAAAAAAAAAAAA"
            + "AAgAAAAAAAAAAAAA",
            "YmxhZGViYXJyaWVyAAAAAAAAAAAAAAAAAAAAAAAAAAAgAGQAAQAEAAgAAAAAAHBByADNAOYAAABKAUoBfAGg/wAAAAAAAAAAAAAAAAAIAAA=",
            "LAAsABEA1wBEAAAAwmQWAIAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAEgAAAAAAAEA4AAAAAAAAAAAAAAAAAAQBQAAAAAAAAAAAAAAAAAAAA"
            + "UQAAAAAAAAAQAAAAEAAAAFIAAAAAAAAAAAAAAAAAAABTAAAAAAAAAECAAAAAAAAIAAAAAAAAAAAAAAAAlazlB4Wa1BCou+onrcP2FpKp4gJ9ks0e"
            + "jKHYKpKp4gSEmdQCmLDoFHF2gH+ZrNhciZzLS5at5glWXG15fJHMB1djhUySqeICcICoPW1zfX+OZEB9iqHaD6vA8Qpma3Z+i6HbBlxpi0g7P014"
            + "iZ7PM5Gm2i+WreYKQkdacXheSnuSqeIJUFp2bqO46kWPpd8Hf4WRf5WdrX3J2f4cj6XfCY2h1i2ctOwilazkCWRKOHt8kMses8f0TZOq4wOKoNoM"
            + "OUFZYJau5hCZsekiNDhKaYOXxjWasegelq7mDJmw6CiGm9Qme4GNfkhOY3KTqeIQm6/iG4ac1gaaseoXmrLqGpmx6QmXr+cZl67nDpCl2yqiqLV/"
            + "wdP7EJOq4weTq+MFhZvVI5iw6ARJU3FXgJXPK0FKZGWQp+AGlKzkAZyz6ySqvu84j6bfHsTV/RmDiZV/lJuof620wX6Kn9kyXmRye4ec1w9xUjp8"
            + "S1Z0YaC37hKVrOUbZnWdVXuNvEeCl9Eik6nhJ3xzcX6WaD98orrwIlpmhWWKkZ5/gZbRBaS88hyitugthZrUL5at5gtQU2F5kqjiAYab1gpVYoRV"
            + "f2hXfnyMsmGJntcSzNv/Kouh2wd+ks0px9f9F46k3S2JntcnkaffMYGW0Byiuu8Oe4/KJ5aq3TCFmtQegpfSCMjY/RaSqeIFPzU2dmlufHuVreUC"
            + "lq7mAZ217R2Tp9whgpfSE5Kp4gLd5fJ/oLfqKJCm4ASjtucfNzpMbYug2iSWZzx+O0BSdJKp4gE9RFtqkajhAzIyP3aVrOUIjKLcAaq/7i6Yr+cL"
            + "U0E3e4OZ0wSUq+QDhJjRKpSr5AaOpNwUTDw2eMjY/Razx/M8i100f5uz6g2IntgHf5TOIJes4DSAlM8NQElkXKe/9BiftOY6iqDaA5627Ra1yfVc"
            + "h53XAo2ToH+OpN4PeHh+f4uh2xqcsOMje426OpOq4wSOpd4BRDc2ekRIV3mbsugreH6Jfn2SzRybs+sfk6rjFX+UzwGQpNowgZfRF4KIl3yInNQq"
            + "OD9XZZev5wKYr+gGmLDoFcfO23+ht+wK1d3qf4qf0z+DmdMNb3WDfJOq4wuFWDF/lavjMWFmb3+WruYSz9bjf4NdPXx0eYR/YnGWRn2RzQN9kc0K"
            + "lqziQuLq93+MotQyjaTdBNHY5X99WTt8WVpgfZWt5Q2RqOEBQDk+d5Wt5Q+Ijpt/jKHZIJCm4AKSqeIBkWI4f8ra/iCasegmrcP1EDc8UHBcYGp+"
            + "mqGvf5Oq4S2VrOUFjaTdC5Cn4AGGmtMsvM/6FJCWpH+ZsekRNDVEboaNmX+0yPYMvMLOf5Cm3jl7j8sdnLPpJ5Gn4QKYnqx/AYAAAAAAABAOAAAA"
            + "AAAAAAAAAAAAAAEAUAAAAAAAAAADgAAAAAAAEA4AAAAAAAAAGAAAABcAAABSAAAAAAAAAAAAAAAAAAAAUQAAAAAAAAAAAAAAAAAAAFMAAAAAAAAA"
            + "ioAAAAAAAAgAAAAAAAAAAADqAC4Aj+otALfhUwARBTsAAAqbAAA3pQAArukAAJ48AcsesY7LM3kWy0ddc92BqXPkgZJz0JTdc9rmTXPSr3SPzwAA"
            + "t78AALe/AAAAAAAAAHYAAAB/AAAAZwAAAM8AAACct7YAgY8UACSUHwBsR8IAAB52AMVD1ACF7p+3hD+iLv+6XzL/SPf9TS5hHU1CrNtmyuTbTeNM"
            + "2/bWOvf2JPS0zwAAt88AABG/AAC/EQAAvz0AtwCwAI8A2AAAAAgAAEGvRIjulDbwxyG2o0LKFNfFSDKjxbE28MXeoojGiJcN5A1P7/jewK06IAdE"
            + "uxOmURvJzWBmyfQHOt/TMfig+3ywbQCGf7MApD6zAMYZeQBSvPvcAKxl5gADU4oAai1XAIYRFDatPtnZ73b7X6AO2V5XLhtlhKLExMbIW++ks2ni"
            + "zYJtNAygfAk6320rGwwdqzbJX8EiGMC81Wgxpgxj632YeQDbvnIAP77ZAJnjaQDop5oeAHjwxgAGiMUAcYLFAM/lecHPplSf2H2nPNTrfS+YZ1F8"
            + "h3/lB3iwSmCzPV16QvFwtWRrtb1EayqtMfGpgjHRAoAd+ZBGLUZ6+WShUXuzIy4AFbY8AI1EMABjSdwA14gANt7wAJ+LmgDpNncAbM9hfDgIeMMt"
            + "2MBErT3rBmSsbMFt/OmQw32fWTmnrCiXM2v5sdFrDxsPa9E0+Wsho+yy6Sfs85/s7OxKBOwneWTEI5gA3rbUANfZfwBjFZsA4mMAaRT0ADhvFADH"
            + "NvsAM/RQLKhbc2P4X8eIJfu6grshH/sTytpPGCtFb/V3TcgYbhOD7id034Ynsd8JbmTNoCnu7FqTcwTvBCGTxOzWLNW7DADbZiUAP01mAC/aTQCe"
            + "g/ZYAPX2HQBY2j8AWBdzAEU3G/b6R99NZkMTqORA3/hHLdXN41gXJS6SuRsJ9rmC84gNavmIYqv5ggxK+S3JYYNd82/vn3Bv/VkJ+7ICoPRNwgBA"
            + "9joAQ/rtAAHQ/wCczfaxADqSMgBMGO4AZq2OAAVQTlWujvbIAUP/s0M/+DZAAOQ4HOcbUx/nFIOSNxTQ4jS8RPBeBjlj2aZR76c596e2w6eXIuun"
            + "fg1Pp7zXX325x0MA+OABAKhHrgBOEJEA0l8ABR+xAHXbrQB1QCsAAOcKqJgKN5I2N1BFtp6O7aIvABdDxwAUx0gAI71Tivu6XvDAhzSCw6zX0209"
            + "zG98nyJxfFEj++sGs+/AvKdj+36LR54AThA3AJKMBQAX4XUAzp8A4TK+AAASPAAAc9YAAAAA6Y+KACivCgDU/pEAAhAAAK5QAACeRwAAMzMAANZz"
            + "Y0PYEpru2MppEtjjST9/4J8/A9aTP5UhFT+fVrjHsNyvtAAAr7cAAOoRAAAAEQAAPgAAABkAAADcAAAA3AAAAAAAtLcAAJu0AACbJIoApdYAAHXK"
            + "ALf+QAAR5jIAEdz3Ms7PO4axzyk/i88aPBe/2zCD6Ujjg3ZAx4NnyoYXz+mPpQAAAJsAAAC/AAAAvwAAEb8AABG/AAC3AAAAjwAAAADuZ7YAhna2"
            + "hfcOI1f7vvsFhZg2r8UiI7RS7Xk+QZKz+iWoVE46/+v/E/rA/7vQffZeufT/dNMV/yXyT027n3LUnxEA2FkAAAjpAADPMAAAz1EAgc/8AJy/rACE"
            + "v5gAhQAyDprFKT3ixQ029MXwNiLGBbqIFq55Y9YWFcyp7rla5Bglw0wY1Z2oWPR87VsVfO0i+/vtWwdvqBtgeEzNfjmsSbQAlUmPAAIr6gDYQAAA"
            + "CKcA7mezAEGbRABSt5cAxV3PzFQjCGNUVdiIp5af8GC0A4l6GbziwTBEmmo8szSp3+N68/VcBntoXESZo7LA6fAEzCijBFNqaARhqxgE5URV0wBk"
            + "WokADswNAKXv1wA+bQ04AHL0tQAj00EAIzZSAAPPyHxxCMgxeNjIObM9yLy/hyPBZzVPAn99+yjUMSMwG0GQRhsPwfmN+XoPxEY5pB1uU+OtJzi+"
            + "jSdC8htun3eINACio6MA1FqIAHZbuAA+s2n9APu2EgBeeDMAuKKBAD5OfFV27cPMArlg3i70fmLlIUpbYCpdXh29QF+dU1zTe0YnE/lGRt9GRvP1"
            + "J0ayyWu5RPlrGhqya3AsC2vzDY3XTDYAWmZdACJFPAD72iEAIxgAH7YMAP3IzQASSToAHjDVchM8y+sYXWZ49bpM99+nngk6eeCCu/tAoLtPrbHN"
            + "ZHtL17LR7Frs0bK5S9ELzfH1G1xruQxkaxqqxPFwaGO4/3QAZfbbAGnQPwBeqHMAZRMAzhclAC6J5ABDg+0AAVN1u5JONxhF+lC79v8WaPgQ7k06"
            + "M73tVe7O5Psy1UzZEh93eKlj0wcoo18xKJobw9dfqW3eeV1UsXg4TzI5ztn29AAW+qgAUNBFAJEpkgDnJXRIACULEgCovRYA/+5QAEiKu1Ox5xio"
            + "+gplktCuW04Fjv+JUEP2th4/TbYSOOS2YRQ5fKviw3zBiPttwYh3696NwcDX08HA3k9+b1NUUXnQUwCuTk0ABbF0AHVIzgDng0gSADrHHgDtL+gA"
            + "TUc3AAAAPxkAAEg+iufOf5Hn9HY3AGkKUAA2N44AYRBDAFGUp8SzCGkJpwiaOETPo11gZw2p/A47vnrUNkiH2CMa/dg4r+EA8q/hAC4AAADKAAAA"
            + "4JsAAEe3AACBtAAAjI8AAAAAAbcAAC8RAADuEQAAx7TnAL51CgCf6jcAla9QAKKPNu5xzzbucc9lL5jP3iEDv9eUPWxajmo+XpmwZ7Ok1M8oAAAA"
            + "6QAAADAAAABWAAAAlL8AAOYRAADqtwAA6gAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=="),
        new("antimagic", 0x4, 2, 44, 33, 0x00118CC0, "antimagicfield", "icon_antimagicfield.tex",
            "AgAAAAAAAABhbnRpbWFnaWNmaWVsZF9uYW1lAAAAAAAAAAAAAAAAAGFudGltYWdpY2ZpZWxkX2Rlc2MAAAAAAAAAAAAAAAAAAAUIAAAAAAAAAAAA"
            + "ABAAAAAAAAAAAAAA",
            "YW50aW1hZ2ljZmllbGQAAAAAAAAAAAAAAAAAAAAAAAAhAGQAAQAAAAAAAAAAACBB3ACWAP8AAACkAQQB/gGg/wAAAAAAAAAAAAAAAAAQAAA=",
            "LAAsABEA1wBEAAAAwmQWAIAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAEgAAAAAAAEA4AAAAAAAAAAAAAAAAAAQBQAAAAAAAAAAAAAAAAAAAA"
            + "UQAAAAAAAAAQAAAAEAAAAFIAAAAAAAAAAAAAAAAAAABTAAAAAAAAAECAAAAAAAAIAAAAAAAAAAAAAAAAe0GkVnNAmE3cpsI/llXhA5hX4gOIT6tU"
            + "2ajMIrh34HaYWOIBllXhA+/P+X+VUsFlnVfIZo1MuGGPXbVYllXhEOrH+X2LWbBYm1zjCqxz0WXEmecHpVzFAZlX2x6ZWeI0czucU61q2HTunj1u"
            + "l1XgBGAwhkdeLYRHllXhAvC2iXqWVeEG/emvfct6kheWVeEEjVK3X5BOu2OYV+EHn2fGY6hf0GaISbNerHTnNbaB6kqjZ8RguYbiB5hY4gWWVeEK"
            + "llbhAqNby2ReLYVIpG/LZX5DpVS8jM5kl1bhDXU8nFGWZLpa2Gg0JPLj/AKYVMRmmFjiBpZV4QiaW+IOmFfhCrJ73gKWVeEDp2zmK4tXsFXMn/Bn"
            + "oFnLaN/F8gOcVshpr2XYaZlZ4SOndM5n8NH6ddaq8n7ox/lyllXhA8WQ6n2YVsJmmV+9XJpVw2PdjYICnV/jIqZs5iGha8hkdz+dUHE5mVGATqdU"
            + "l1PBYtik8HqERqtVv4TleoRTq1bIlu1/llXhFOW22m6+ZYwEoV3RAeW/9n6LUrNal1bhBXtHolLKket1ZzONS5ZV4QKXV+Erl1bhAaduzWNtOJNM"
            + "0KDgcffGZXaDTKlTlFG9Xm84mFLgtfV+m2TCYW03llHLfL1wjle2Xcyc7n6WVuEWiUm0YJxqvltrN5BKq2TWcNq09XWeYeMMl1bhEde17guYWOIP"
            + "l1fhKJdW4QPxxsx+gk6oUpla4gTYr/R4nV/jKpZV4QuWVeEhtWKhA8N/5nP76sl/iku0XphY4QStYrgDllbhDNKk8X+WVuEgomblFM2NzG+XVuAC"
            + "mVXAX5lZ4gKWVuEOsHTLZJhX4QSmXdBqbzqXUI5Nt163fuAe+ejpf5ZV4QHctfZmkl+3WKNcz26kXM5npnLNZ5xd4xqsYdRmnF3fApxe4x7Mn+9W"
            + "m1vhB59Zy2p8QaNSeD6fUYpOsVieW9cShUutWNSp72FjMIpK9tj6fnY9n1Xfu/R1llXhBKx05z/Sr/AEl1fhL5lZ4h64cbknnFrCX4FHqViGR7Be"
            + "mFjhApdW4RmYWeIDmFjiCZRYvF+ZWeIW13BSBp5b2AycWtoL33I+D5pa4RSbXOMR47HWebJ602eXVuEdy2pxBaBj5BH735l+rnTkDqBk5BvSp98V"
            + "ol7EYJZW4QSWVeEHunfGObyF5lqdWNMbqmXQZJdX4QGZWuIM5HoyVKtx50aqYdNqZTOLSNev9G+WVeEBk1C+YrVt3nGlaeIFl1bhJd+Of0y5b99r"
            + "nzggMpZV4QWXVuECmVriAXo/olSRTrpfrHbSZ8lRHFigZOQ7kV63W5dW4QefWMdhllXhAd2bwHijZ+Mx0aXxeMGH5WxhL4dJAYAAAAAAABAOAAAA"
            + "AAAAAAAAAAAAAAEAUAAAAAAAAAADgAAAAAAAEA4AAAAAAAAAGAAAABcAAABSAAAAAAAAAAAAAAAAAAAAUQAAAAAAAAAAAAAAAAAAAFMAAAAAAAAA"
            + "ioAAAAAAAAgAAAAAAAAAAABMAEEA0gCDANIAgwDStDYAO0HLADu9YwA7R5YAR0eLDZQfKPKMHz7ELx8+cy8ft3O3Hy8fPh8vHz7njB8o+pSkxgAA"
            + "dZsAAHWbAACk6QAAdbcA6QA3AOkAigAAAMYAADv2liM79ovYO9UO2ADVZKIAAEHVAEe90gDSR8uW9r1j8rX3xYq/93scjvfWJI73j/esu473YbuO"
            + "93tXyvfFhsoFPQAAxJ4AAOjGAADgmwAA4D8AxBFAAODpxwDg6ScA4PaLIxvtlgPN4mPAiO3LwIgAy+2I0tU6INI6OofV4jrItnE0+aVoGPlWaDP5"
            + "Vv7zKjPfVjG8KlYxvPmlszT5tlGUXgCUx2UA27e6APc34wBCjvyGAMVNKQBheykAnAikANVkIBs6DmLN7YvdIhuLroiWlmmjyyNpyNIjaZjS9GnI"
            + "l2hxdvXZbQz1mhPJ9dkJDLgM9cG4yfVrZQz1mnF28fnRKgCCzOQAjOHxAD4+bQD3trohABi8uwCFhVcAjsUfAL3O3Ay9oi6ag0OHyYO8yKvYtHhW"
            + "2J7IbcA9aP7AgcloNQJ9cMl3qRdKci0KXeUtLl0uXeUtpu/lqX3scn1E0HJrFb8AS+TWALB6jABRgD4AXsIAGG3sAJU0oQB7dPAAnL3XrpoNohN2"
            + "NkNcSvI0aC3TBLoTrCSdXN4hwfX8KMnsNX6Qrzl+RAIG5TkXqR1EWUSqOB05pqodRFjq/5B3Zv/sW0MAoUbFAFsQggAVVZQAn7kA4155AJNVMADP"
            + "46EA19cpmrqynuxVMz2rUbbHLTESnBNLu3uXa/iVUewLGCqt5S6v/x0Kcv8dF+//HXCgr1hyHnA5rx4Kfv8eX3L/Ky7wAQDuFVMAUhrDAHuNFgCc"
            + "GRQ0AMKohQAsSMUAiYAIAMwpmjSyhnZlhWdKcTT4LeRXQBMq2teXWz3PUesLkyrsr2avcP/qoF8eOArvHllvX6pfHm8X7x5sWF8rGXJw/63wsQCF"
            + "UMMAxRoWANeNFACBGVDjAHkQ8wAwesUAieMIAPqedOMfNzSSV8dlVfcLSF4+51HXjPpGe2EpW47K6Ot00PA5JuzrbBR5FmxQ6hYZqLmfuRYZRmYW"
            + "WRVTFjkUJSYydMoAFk2CABTPjABQzD4AEBoA4WBIAMfjcQBndOMAQufEz44fxpOFH5HuvNo9gN1nAFXHx6TxjOF1n4KC4BV7S38wEOunLEiJ5FNg"
            + "0KhPSE/9JahTVcOoLEh8pzAQJn8Ue4IAqIKMABqMPgB6PvcAcd0AIe4YAJ7zkwAFxY4A6Ok7nACbAI8OmwCyZAUAjmQAAGdBAAAhveAAPkcRAJS9"
            + "3TbjV0W07lfmmb5XcQRDV/MEcRJ0Ev4fGBL+H+MS3SmU6AAAPuAAACF1AAD3dQAATeBXALLgHwDWER8AnACkAOk7t2PpOwuW6TuUi8RHnA4AAOhB"
            + "AMtCvQDLu4MAy2cNtfLPPk5nez6OIWE+jtsI2wjbjtthPo4hez6/98U+tTdnkQDp2sYAAEIFAAAfxAAAnMQfAJTp+gDH6foAJ+n6ADvV0vQ77csD"
            + "9uJjwPYbltzVY4sb1cvYG9XS2OLS1c7i1LhhEy8JYVacbWGuCG2C7oI0CG3WVpxt1lThZXu4gRMLCOkAx+ERAD0LEQA3IQAAir4A98bzAFebygAf"
            + "m3sAHwDSOs1M1dUg0uLSyDobyxPtliNp7WMHIu3LoiI60qIiovUYUQ9RGPVSs74QQ7N0XL5thTG+cVIxvv5Osxj+z7N7TegAgs/oAC+CdQBAjHUA"
            + "PgkAPj3mACE3vAC7BXQAV2NHo7bLg4di1YP7ZeINmGgONhtoi6LNaJaiItmW3KOdwf7BRJpK+WrBXSqqwS0xqmipwepoqZotyWprq9lEmuw00ABN"
            + "dGsA0coVAJxhsAA+UX+cABCNKABtRfgAuhjaANhB+6UH8sgTB/KHlwPOmGhBzvRoNtb02TYPIHYNM4c1SkoMfq1EDH5dRGt3Sn1rX9lESqbBd104"
            + "DH5dLQx+7+9tbgD8RTAAyvzrAGFNSwCMS7DRACp/hADkVUAAl0X4AJnXE5ocTlwMIZP+ZkA0/i3MEvkTotrZl7YndlE04V0qqf//r2od/3KmHq/v"
            + "Ah6vbwJYch5+OQoe5XdwHuVyOCvq8LwAMBVDANAasgDrjYQASxkANLCtAIVeLADFbYkACAQ/E5oke5fB2/P1T0Du3y2PKZoJstoMcTM9ZlGlCy0q"
            + "WK8drzn/Hgp3Kx5wciv/bOWqryvlcHIr5Qrv/x1yoP8ZSTQArZ+FACwQxQCJjYQA8LkA7kZuAPPx0AB7jRYAnOHn+VLWH2szv1cwRb67bm3o+K4Q"
            + "BUBl5DeE8RXH0eRbWusX73Ilb7mvLLnqrywKZu9mr08tAa9POLlyMqbvWjI8JgB7pxQAnHpQACjdGgDb0Hr8AG7+TgAyvo8AJpU/AD7nsI+c50u/"
            + "0R9rdE2G0OMpnjOSQie6jbtAbfEhP3/kWUZsJVlLGYlw8MLDWhZTw+yJWnyrMnAW6olv8BklbxSnUACcSH8AKP5IANsY/QC7JTSVADL8ewAmtZwA"
            + "PHvHAOfp4eH66YKBdZt7gujEjmEFAHSGigC82j0A5iEL6WU+SHQUQxozFFKfvjzKRhg8xRXFUBg8yqgYPJUavkmFXnRgPgsA/iE9AOO7ngB0V4YA"
            + "jtEAH3vXAB+CPwD64UAA+gDpIZF16T494OmM2xERCD6bAHvomwC1KcYAQ0I3AL6e7o9e193WXpz9YUgvjWFglI2UYGF6L4BhYJzdYV6E7ta+2vcA"
            + "80JCAMopKQB7HykACD4A+ozbAPo+IQD6IfcAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=="),
        new("righteousmight", 0x8, 2, 45, 34, 0x0012BBB4, "righteousmight", "icon_righteousmight.tex",
            "AgAAAAAAAAByaWdodGVvdXNtaWdodF9uYW1lAAAAAAAAAAAAAAAAAHJpZ2h0ZW91c21pZ2h0X2Rlc2MAAAAAAAAAAAAAAAAAAAUMAAAAAAAAAAAA"
            + "ACAAAAAAAAAAAAAA",
            "cmlnaHRlb3VzbWlnaHQAAAAAAAAAAAAAAAAAAAAAAAAiAGQAAQAAAAAAAAAAAHBBWgD6AFoAAAB4AKQBeACg/wAAAAAAAAAAAAAAAAAgAAA=",
            "LAAsABEA1wBEAAAAwmQWAIAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAEgAAAAAAAEA4AAAAAAAAAAAAAAAAAAQBQAAAAAAAAAAAAAAAAAAAA"
            + "UQAAAAAAAAAQAAAAEAAAAFIAAAAAAAAAAAAAAAAAAABTAAAAAAAAAECAAAAAAAAIAAAAAAAAAAAAAAAAPsE9MFTqUCtIcUB9RuNEA213dH9G0zwK"
            + "YPJbXlDVTDpH4EQERuZGGEbjRCNY7lRSRuZGEju/Ojxe4VhmXuNYSEfjRTxH5kYxRuZGA1rvVl19joJ/TedLTFbyUhFy+mluQ75AVEfmRhBCpjYs"
            + "c3Rrf3liP383IRJ/Z3lqfztBMX9J4kUbOLo3Pzq9OS9G5UYsRuVGNF3vWFlG5UYCOrw5LlLrTSA2mjReRW0/fUbWPgRP7U0CNC4qfzInIH9G5kYd"
            + "UuRNM56tgH+60I1/TeBFJ0fXPwZf8FpaNS8sf1sxGH8/aDp9TuZJEVnvVVY4ujguRuZGG0bbQQxR609LgnE5f2LsXFVG4UMKS+hKP1XxUgx4USl/"
            + "StpBFkbbQQlS5U5RLHYqX0bcQSZYf1F+N7k3PHA8HX9r9WNkR+VGDju/OjRDzEFHiqZ0foyTln9G4EMqa/ZkZlNzTX9G0UNLTeZJDGfyYF54QB9/"
            + "h4hIf1/xWltS8FAERuZGN1zmVlQ1Lyt/SOFECDm8OEU6ZjJ2RuVGIDxTLn5I20EcbPRkYIBDIX9a4FVabG88f0bjRQJNbUF+bHJ2f0fmRgFL5EcY"
            + "Or05Pj6vPGRG1D0IbH1NfjcxLX84uzhBlp6gfy1sKm9i811hXoRYfkfmRjhJ2EATafFiW0GyPkSBbDZ/nppkf1n0VBhI50c7PZo2REbhRBhH3kIS"
            + "TOlKKju/OUg7qDlWWPNTHDUvLH9SLxd+RuZGCEPKQTY1Lit/QJI3cFHsThg9wjtKQ8ZBTmpvdH9H4EMVRtxBBkRJSH89LBh+Rt5CAj7EPUNG5kYF"
            + "SN1EL0bmRgxG4kQbMnEqVDY2NH81LSh/bPNkXnGTYH5G1D0NO8A7RFRlPX9G5UYmVPFRCEvnSkViamp/PcM7P0fYPxU3YzF7RuZGIjk6OX9G40Uk"
            + "NS8rf0blRi5G1D0LQiUTf0rpSQJn7mBUP3AqLUzUST5H1EU/RtlAF1pfYX9G5UUfQ8pBQDe6NzZG5UYJRuZGFmbzX2Q4uzg4T+xMBlrXUllQvUQz"
            + "Ni8sf0joSDg1Lyt/ZYhffkblRjlp9mJqSudIBUx1QX1S609NODQufzq+OjE7pzhbLWApdDm8ODs9wjw2RuVGBlfUUWQ5OC9/S9NIRmLpXE94mW1+"
            + "W/dXHUfmRz1G5kYKVM9PaTQxLn+FkI1/O785OE5UVH81Lyx/VtxRQkOyOz5aRCd/hnk+fyx3KmhvdXl/Z21wf0fWPhJH1j4PRuVGNzi6ODFG3EEg"
            + "XPBYV0bbQAVxeHt/Rt5CD2c3G39hc2B/ub/Ef231ZWJL50ggSioVf3aEaX+hqKx/PsQ9Oj6yPFhX8VIhNTQzf0bmRilG5kYkAYAAAAAAABAOAAAA"
            + "AAAAAAAAAAAAAAEAUAAAAAAAAAADgAAAAAAAEA4AAAAAAAAAGAAAABcAAABSAAAAAAAAAAAAAAAAAAAAUQAAAAAAAAAAAAAAAAAAAFMAAAAAAAAA"
            + "ioAAAAAAAAgAAAAAAAAAAELQm8tXTZO9Ek2ZGqiBJ75cPnO0FOmPFf6L95u+tPeTR9SZvj4UmRo+FJmbRz8LPkITJ0PLqCcly9Bz/sunjyj32qgA"
            + "R00SAD5NmwCE0JMAnOkAuqiLABUUnAC9B6UAmUL+FRpX1Nu+vnzL//5nmXoURwvUGD6ZqNqEy7q0nEK+6fQVXov0Gv+D9Jucgzzbg4Nckzy+Z5kQ"
            + "vq4RoBVIjC5HZ/4APny+AIQ/mwCDpcsApboAvkO2AJsYCgCTFNQAmbpzJW+ocwqh1BE7lqBhrNH36RB8R4tW4unuBzyLKUO8bWisSSKjO5giiPOj"
            + "VInQaPSJVMnAT7zfIk/04m1JPFmg0QDS1JYAC/7HAI8SbwA0pwJXAAeXkwBWWJkArpMnAIsAEIJkduJ5CnaUodR29FGP0rM7jGFvQUc6r0jpMZT6"
            + "Yrv0A237lMhtyOL7bQOuSbzHbZgi2G2xYthUQYbHvDs/UQCPhccANBJTAC1XggBqlICTAK9EmQBvwQsAs9JzAAB2/BAAq1FRADWhY6s1KqGrNNNd"
            + "NY+WpsGPzA8Pc8yQsxxLodgcDWNuHNid+Ryzmvnr3BxuHMwcSxyWHEsc0+tK8WoAofGpAJ2iBgDZagYARJAANMEPADQROgA0LXsAjwD3REQAEZCA"
            + "AAS7nQALY2s1BkqrNWoqNV1qKl1dLSqm+FtLa25rS53sa96A7Fv4OuxbSltuWypbS1sqW0tbSltjR6IA+owGAIARagBEBDQAWEYAJz64ACeisgB2"
            + "ovIAdri+pla4/w9csiXZFIQUw0PyeOkl8VZCJPHtk6iyVsFPKpLDVaGqAu+h6iw4Kupmqp3qK6/kcB3/HTjkpStVnQq9QwAUvRQA0JNcAP6TTQCc"
            + "hFaTAEJWywC9TZkAvb8LAGSlZq+2P4uvTHy+r0w8G65C9EzPGkhM0b76TDuc+lI/75YbVaSqd5KkWho4quoalqrqHlBVcDgMkjg40R5Vql+6zgDf"
            + "uk8AfChPAFA9SQCoFUm+AL6YFQASsVcAKBbbAP54eecUQcd1z+bHddZTx3X6m1MfSb45BTL/yOezxdfnGR5y8GXqcvBy72XwcqoZ8OXwh59y4Mj9"
            + "QJ454OWfTrB5U9YAx+bPAMdBFABTeCQAs/AAxTLwAP9J8AC++usAmz0Q6LCoyuiwFLvo8Hz76LCUFehliboFTjleH+AfVh+5ecbvsHSOZbBlHnSw"
            + "75V5uVqw68Y44LD96v3w4FXh8Lnw+3wA8LsUAPDKqADwED0A67kAVjngAF6JTgC6lGUAFUhCbBq7V92T+xLdmbMk3Qs9LmUnCr/NBHgK1REQPWxH"
            + "tz7d27fp3b236d2Tt+mRy5FXt5lgm7cLt5u3BLfbkfez9wAk+0cAErvpAJtIiwCTxLoQAGwleADNvwoAZS49APo+4RJP6S9Xb763k3T/t5lkp82Z"
            + "2hjVy2c7bL0jJMRXt4O3tLe6tyi3E7e+t6VgFbf+t5u3/re9t/63Ybelt0d0PgD/b+kAFU+LANv6ugDLL6gjAOE7ZwBsGNoAzadkAD6cCr/ppbpc"
            + "i4EVVrp4mwEK99txeEcazqA+vs/2hP9WJWLtAdRtv02/bT8HVm0Kv6z0tCIIyRJUCGIVvKxi6fSLeD0AnMVXABPukwAlFZkA9k0A26BBAMt4zgAL"
            + "CnEAJ0eEVtY+IdT0hIUl9IND/7y2jyhj0I+0002MXhfiPniuz4a8wEij9MDAozDfyaPPysDJeKPAbXptvIalbbyItskkQxUAejHbANQ6mQBN2wsA"
            + "4q4Ay1YXAAteAwAnumMAj2EADA9XAO0pdwAjyAKr30p2NVEqdjVKoXNdY9MRpg6z5qGxS29L30tv7E0N5uwMobHs+7NJbk8DT0tJoWlLmEr8SgCp"
            + "D8gAqcECAAbSDwBqDqYLAGNhjwDYLTQAUak0AKsAaabSAFkPWACU/JcAr30AACnIADVjoXY1KiqrXdMq3hxJS95uu0ve+VneDflpHIf5DSqzS6FK"
            + "2EuzoQ1Lh8gP4wAGppAAasEPADQRWAA00/ItAEryagBj8moAAqIGADRmNVg09V1ENPWmD4/1RK1zPoD1dukCZquE5ouruFvp5R1+gkBrfixAa353"
            + "QB1AV0CqWx1+6lHqfpLZ734dD6o6FY8AixWPALIacwCyV3MA8psAm6mbANstVwCTNBoAy6IKmZry89Ka8trSmvLaweM+GqbQPr4PJUdkgiVh/rOa"
            + "Y+9/G3LkWz1b5HK+f+9jPX+qyB7YqoIe2O8xkn8eZh5m0JkA8YELAD5eCwBhCicAy7QA/pn+ALSZqAASmV4AFRD/Bx8BQ0MFI00KBQGU0AW6+hAf"
            + "JU/iHwezrMesx00zOMin5HDICXI4pA5l6uR4yJJFWjPv5JJ5WkXq61VFjuv6sACU+rAATfqwANrO6wC00cd8AC7IeABQTwkAzvpMAN8VMgWxZBno"
            + "SSW/5xkuv+f+fObn9s5v5wizb+f0x+Z1WrBZjlWNWdU4zYmNcB4yucw4OPCO6jjwVZLv8DjqwvAZ8AAuT/AAJZjwAGQWsAAV5utIAG+zCABvziYA"
            + "5nyoAFfKecQSSR/E/ubw4ad0dcZWunUgrnp11UlNdWzIAYpsirfht7m3tbe1t7lgxLfwLze38LeSt/C3qrfwtze38LfwdKcAsOb+AOtJEgB5ylcA"
            + "yOEAAUnEAE2u1QB6ViAAur76dGD+TyC31G/Nt6yktbfWZI3N+tqNbE+g4MRvI+Dhjbdgt563kbeRt563YLeNYLe3jbc2t+C3n7eNt7e3jbe1pKwA"
            + "zW/UACBP/gB0+r4Aby8AI0/hAKD6bADa1s0AZAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA==")
    };

    /// <summary>The hooks: where, the game's word, the patch's word, and the spells (mask) any one of which needs it.</summary>
    public static readonly (int Va, uint Game, uint Patched, int Spells)[] Hooks =
    {
        (0x00161EF8, 0x2C830017, 0x2C830022, 0x1F),
        (0x00161F00, 0x3C020030, 0x3C020013, 0x1F),
        (0x00161F08, 0x24427BC0, 0x24423230, 0x1F),
        (0x00167AFC, 0x0240282D, 0x0C0462A8, 0x18),
        (0x0016DD44, 0x0C04EB96, 0x0C040BC9, 0x1F),
        (0x0017A4BC, 0x0C05ED8E, 0x0C04A508, 0x1F),
        (0x0017E52C, 0x14830009, 0x0804625E, 0xE),
        (0x0017EA44, 0x8C440000, 0x0C04A74A, 0xE),
        (0x0017EAB4, 0x8C440000, 0x0C04A74A, 0xE),
        (0x0018331C, 0x0C058ADE, 0x0C043171, 0x1F),
        (0x001B177C, 0x3C030031, 0x8E230038, 0x1),
        (0x001B1784, 0x24636798, 0x24630080, 0x1),
        (0x0030A104, 0x001663F8, 0x0012A710, 0xC),
        (0x0030A11C, 0x0015C2F0, 0x0012BC80, 0x8)
    };

    /// <summary>The game's words the code relies on (checked before it goes in, never changed).</summary>
    public static readonly (int Va, uint Word)[] Checks =
    {
        (0x00161EE8, 0x94820020),
        (0x00161EEC, 0x2442FFFF),
        (0x00161EF0, 0x00021400),
        (0x00161EF4, 0x00022403),
        (0x00161EFC, 0x1060027C),
        (0x00161F04, 0x00041880),
        (0x00161F0C, 0x00621821),
        (0x00161F10, 0x8C640000),
        (0x00161F14, 0x00800008),
        (0x001628F0, 0x24020002),
        (0x001628F4, 0xA282032A),
        (0x00167AF4, 0x016F3821),
        (0x00167AF8, 0x0280202D),
        (0x00167B00, 0x018F3021),
        (0x00167B08, 0x0260582D),
        (0x0016DD38, 0x3C050031),
        (0x0016DD3C, 0x8F84C0D0),
        (0x0016DD40, 0x24A5A9C0),
        (0x0016DD48, 0xAE030024),
        (0x0016DD4C, 0x8C430038),
        (0x0016DD60, 0xAE030028),
        (0x0017A4C0, 0xDC440048),
        (0x0017E520, 0x00051400),
        (0x0017E524, 0x2403000E),
        (0x0017E528, 0x00022403),
        (0x0017E530, 0x24020008),
        (0x0017E534, 0x3C040031),
        (0x0017E538, 0x0C04EC52),
        (0x0017E554, 0x14820009),
        (0x0017E558, 0x24020012),
        (0x0017E57C, 0x14820009),
        (0x0017E584, 0x3C040031),
        (0x0017E58C, 0x2484E770),
        (0x0017E744, 0x44807000),
        (0x0017E748, 0x44807800),
        (0x0017E74C, 0x0C04D762),
        (0x0017E750, 0x00000000),
        (0x0017E754, 0x1000000C),
        (0x0017E758, 0x00000000),
        (0x0017E75C, 0x0C04EC52),
        (0x0017E760, 0x2484E7D8),
        (0x0017E76C, 0x44926000),
        (0x0017E778, 0x44916800),
        (0x0017EA24, 0x846233D8),
        (0x0017EA2C, 0x3C03002A),
        (0x0017EA30, 0x00021080),
        (0x0017EA34, 0x24635D58),
        (0x0017EA38, 0x00431021),
        (0x0017EA48, 0x00B32821),
        (0x0017EA94, 0x3C04002A),
        (0x0017EA98, 0x844233D8),
        (0x0017EA9C, 0x24845D58),
        (0x0017EAA4, 0x00021080),
        (0x0017EAAC, 0x00441021),
        (0x0017EAB8, 0x0220482D),
        (0x00183314, 0x0C05F21C),
        (0x00183320, 0x00000000),
        (0x001B1450, 0x24636798),
        (0x001B156C, 0x0080882D),
        (0x001B1790, 0xAE030038),
        (0x001E0CA8, 0x27BDFFB0),
        (0x00307C04, 0x001628F0),
        (0x0030A0F4, 0x0016AC68),
        (0x003167BC, 0x001B0F98),
        (0x003167D4, 0x001B0EB8),
        (0x0031B5DC, 0x001DF620),
        (0x0031B5F4, 0x001E0EC8),
        (0x0031B5FC, 0x001DF6D0),
        (0x0031B65C, 0x001DF120),
        (0x0031B674, 0x001DF3B0),
        (0x0031B67C, 0x001DF1F0)
    };

    /// <summary>The hero's melee hit: the game's call of the hit function, or Corrosive's (a jal either way).</summary>
    public const int MeleeCallVa = 0x00167B04;

    /// <summary>The game's strings the code uses (sounds, fallback icons, the HUD's damage line's key).</summary>
    public static readonly (int Va, string Text)[] Strings =
    {
        (0x00303B30, "spel_dsrptn_afteraffct.anm"),
        (0x00307928, "spel_cas_clangadin.anm"),
        (0x00307958, "spel_cas_bullstart.anm"),
        (0x00307970, "spel_cas_whirlwind.anm"),
        (0x00307B38, "spel_cas_copstart.anm"),
        (0x003095F8, "spel_cas_bullend.anm"),
        (0x0030A9F0, "icon_protection.tex"),
        (0x0030AA20, "icon_whirlwind.tex"),
        (0x0030AA78, "icon_powerattack.tex"),
        (0x0030E770, "PowerSpellDamageWeapon"),
        (0x0030E7D8, "SpellDamage"),
        (0x003110D0, "spel_cas_copend.anm"),
        (0x00315E80, "spel_end_mord.anm"),
        (0x00324A18, "fx.lmp")
    };

    /// <summary>FUN_00162B78's 'lui v1, hi' and 'addiu t2, v1, lo' (where the class lists are built: LISTS reads them).</summary>
    public const int ListsLuiVa = 0x00162B7C, ListsAddiuVa = 0x00162B94;

    /// <summary>The ability table and its END.</summary>
    public const int AbilityTableVa = 0x0030D750, AbilityTableEndVa = 0x0030E420;

    /// <summary>The test files' tripwire: 'j TripBase + stretch x 0x1000' and 'nop' in turn, ending with the nop.</summary>
    public const uint TripBase = 0x0A000000;

    /// <summary>
    /// Version 5 (kromlech-spells-test1: Xantam's Fire and Kromlech's three, without Power Attack): its hooks (and which of its
    /// spells need each, the same masks as version 6's), its code's pieces with their SHA-256 and its vtable copies, and where
    /// its data block keeps the settings (its mask, Xantam's Fire's damage and the entries' prices where version 6 has them).
    /// </summary>
    public const int V5Version = 5, V5EntriesAt = 0x80, V5AbilitiesAt = 0x200,
                     V5BarrierDamageMinAt = 0x340, V5BarrierDamageMaxAt = 0x348, V5FieldFramesAt = 0x360,
                     V5MightDamageAt = 0x370, V5MightDefenceAt = 0x378;

    public static readonly (int Va, uint Game, uint Patched, int Spells)[] V5Hooks =
    {
        (0x00161EF8, 0x2C830017, 0x2C830022, 0xF),
        (0x00161F00, 0x3C020030, 0x3C020013, 0xF),
        (0x00161F08, 0x24427BC0, 0x244231C0, 0xF),
        (0x00167AFC, 0x0240282D, 0x0C04AF4F, 0x8),
        (0x0016DD44, 0x0C04EB96, 0x0C040BC9, 0xF),
        (0x0017A4BC, 0x0C05ED8E, 0x0C04A4C6, 0xF),
        (0x0017E52C, 0x14830009, 0x0804625E, 0xE),
        (0x0017EA44, 0x8C440000, 0x0C04A537, 0xE),
        (0x0017EAB4, 0x8C440000, 0x0C04A537, 0xE),
        (0x0018331C, 0x0C058ADE, 0x0C043171, 0xF),
        (0x001B177C, 0x3C030031, 0x8E230038, 0x1),
        (0x001B1784, 0x24636798, 0x24630080, 0x1),
        (0x0030A104, 0x001663F8, 0x00129C48, 0xC),
        (0x0030A11C, 0x0015C2F0, 0x0012BBB4, 0x8)
    };

    public static readonly (int Va, int Length)[] V5Code =
    {
        (0x00102910, 0x1E0), (0x00102D80, 0x280), (0x0010C338, 0x390), (0x0010CD50, 0x1EC),
        (0x00118978, 0x22C), (0x00118CB0, 0xAC), (0x00129318, 0x204), (0x00129C48, 0x118),
        (0x0012A710, 0x15C), (0x0012BAE8, 0x15C), (0x0012BCD0, 0xD8)
    };

    public const string V5CodeSha256 = "20ecb1fae454e254a6b791189b926be405c636f08412114ae0a9e4b450ebd07f";

    public static readonly (int Va, int GameVa, (int At, uint Word)[] Words)[] V5VTables =
    {
        (0x00118BB0, 0x00316718, new (int, uint)[] {  }),
        (0x00118C30, 0x00316798, new (int, uint)[] { (0x24, 0x0010C480), (0x3C, 0x0012E418) }),
        (0x0012BC50, 0x0031B650, new (int, uint)[] { (0x0C, 0x0012A7DC), (0x24, 0x00102910) }),
        (0x00102AF0, 0x0031B5D0, new (int, uint)[] { (0x0C, 0x0010CE8C) })
    };

    /// <summary>Version 4 (Xantam's Fire alone, test4): its data block's settings, its code and two vtable copies, its six hooks.</summary>
    public const int V4CodeVa = 0x00221B88, V4CodeEndVa = 0x002222F0, V4PriceAt = 0xCA, V4ManaAt = 0x3AC,
                     V4DamageMinAt = 0x48, V4DamageMaxAt = 0x50;

    public const string V4CodeBase64 =
        "0P+9JwAAv/8IALD/EACx/xgAsv8gALP/3ooFDAAAAAAWAAg8fCsJjZQrCo0CXAkAAzwMJCwAbBUCXAoAaiQMJCkAbBUATAkAAFQKAANUCgAhmCoB"
        + "EwAQPJAtECYMABGOgAASJiAAIBIAAAAAAABIjgQACS0ZACARIVBgAggWCyQYWAsBIVBLAUQADDxFTow1IXAAAD8ADSQAAE+NBgDsEQAAAAABAM4l"
        + "+//NFVgASiUKAAAQAAAAAAgAWCYWABkkAAAPj///OScAAE+tBAAYJ/v/IBcEAEolAABMrf//MSbg/wAQYABSJgAAv98IALDfEACx3xgAst8gALPf"
        + "CADgAzAAvScTAAg8kC0IJRAACY2AAwolBgAgEQAAAABIAEvdBQBkEf//KSX6/wAQUABKJY7tBQgAAAAA4DgMJBhgrAAqAA08gImtJSForAEwAa7d"
        + "JHjEASIA4BW4M7glDwAZJCAAAof//wMkBQBDEP//OSf7/yAXUAAYJwgA4AMAAAAAIRAAACEYQgEAAGOMIQgCAwAAI6wEAEIkKABDKPn/YBQAAAAA"
        + "KABDhSgAA6csAAIkIRhCAQAAY4whCAIDAAAjrAQAQiREAEMo+f9gFAAAAABIAEPdSAAD/yVwxAEwAa79CADgAwAAAADw/70nAAC//1KqBQwABAU8"
        + "AQBDKAsQYwAGAEMoBQABJAoQIwATAAM8kC1jJCEYYgAAAL/fCADgAxAAvSeg/70nEwAWPJAt1iZchwgMkAKEjjgAcpBAAHOQSABokFAAaZAcCqqP"
        + "JgBIpSgASaUhuAAABABgEgAAAADUhwgMAAAAACG4QAAYAJTGBABgEgAAAAAAqIBECwAAEDAA1sbIpIiPAwAIMQMACS0KQAkAgEAIACsACTzQ8Skl"
        + "IUgoAQAANcUQADbFIYAAACpAEgJBAAARAAAAAEBIEAD//0omI5gqAUAGCyQYWGsCNACMliFgiwEAZAwAA2QMAEgArK/MwwQMISCAASgAwcYCAAFG"
        + "EACCxgAQAEZQAKDnpsMEDEgApI8oAMHGAgABRhQAgsYAEABGVACg51gAtOcEuwQMYAAEJCIAQBBMAKKvABCTRKAQgEaAPgE8AAiBRIIQAUYgAMzG"
        + "QhMVRoA/ATwAcIFEJADPxgIQFkYkAABGAAAJRABMCQADTAkAISBAAFAApSdIAKaPITjgAiFAgAKUxAYMIVAAAEwAoo8iAAg88CEIJTgASKxcAEmM"
        + "BQAgEQAAAAAMACmNAgAgEYAACCU4ACitvv8AEAEAECZgAL0nPYoFCAIAAiRA/70nsAC//ywAwMYQAIHGgQgARgAAoueACABGEACi5xQAgcaBCABG"
        + "BACi54AIAEYUAKLnGACBxoEIAEYIAKLngAgARhgAouchIKADEAClJyAApifiIgQMIAAHJAAApMYEAKXGLADGxoIxBkaAMQZGIRAAACAAuScAACiP"
        + "HgAAEQQAOSf8/xQRAAAAAAwACY00ACkxEAAKJPf/KhUAAAAAEAAAxRAAgcYBAAFGFAACxRQAgcaBEAFGwgAERkIQBUbAGAFGAAiARDYYAUbq/wFF"
        + "AAAAAAIAAEaCEAJGAAACRjQABkbk/wBFAAAAAIYBAEbh/wAQIRAAAbAAv98IAOADwAC9J8ikiI8KAAkkGgAJARBAAABKAAAVUACKjEgAQBEAAAAA"
        + "DABLjUUAYBEAAAAAYP+9J5AAv/9gALD/aACx/3AAsv94ALP/gAC0/yGAgAAhiGABXIcIDJACJI5IAHKQUABzkBMACDyQLQglHAAAxhAAAcaBCABG"
        + "AACi54AIAEYQAKLnFAABxoEIAEYEAKLngAgARhQAouc0AADFGAABxoEIAEYIAKLngAgARhgAouchIKADEAClJyAApifiIgQMEAAHJCAAtCcAAISO"
        + "FACAEAQAlCb8/5EQDACIjDQACDEQAAkk+P8JFQBACTyAAomsISggAiEwQAIhOGACAQAIPKCGCDUhSAAAIVAAAAMACzwOfgQMYABrNev/ABAAAAAA"
        + "ISAAApAAv99gALDfaACx33AAst94ALPfgAC036AAvSfmwwYIAAAAAOD/vScAAL//CACk/xAApf8TAAU8lusEDPAtpSQIAEAUCACk3zEABTyW6wQM"
        + "8KmlJAMAQBQIAKTflusEDBAApd8AAL/fCADgAyAAvScAAAAAAAAAAAAAAAAAAAAAcKgbAAAAAADAqBsAAAAAAGDkEgAAAAAAWOQSAAAAAABgFRsA"
        + "AAAAAEjkEgAAAAAAIOQSAAAAAAAY5BIAAAAAABDkEgAAAAAACOQSAAAAAADg4xIAAAAAANjjEgAAAAAA0OMSAAAAAADI4xIAAAAAAMDjEgAAAAAA"
        + "uOMSAAAAAAA4qRsAAAAAAIipGwAAAAAAYOQSAAAAAABY5BIAAAAAAFQgIgAAAAAASOQSAAAAAAAg5BIAAAAAABjkEgAAAAAAEOQSAAAAAAAI5BIA"
        + "AAAAAODjEgAAAAAA2OMSAAAAAADQ4xIAAAAAAMjjEgAAAAAAwOMSAAAAAAC44xIA";

    public static readonly (int Va, uint Game, uint Patched)[] V4Hooks =
    {
        (0x0016DD44, 0x0C04EB96, 0x0C088866),
        (0x0017A4BC, 0x0C05ED8E, 0x0C088723),
        (0x0018331C, 0x0C058ADE, 0x0C0886E2),
        (0x001B177C, 0x3C030031, 0x8E230038),
        (0x001B1784, 0x24636798, 0x24630080),
        (0x00307BE4, 0x001628F0, 0x00221DAC)
    };

    /// <summary>Version 3 (test3): the spell area's bytes in the file (its settings where version 4 has them) and its seven hooks.</summary>
    public const int V3AreaFile = 0x227C00, V3AreaSize = 0x3000;

    public static readonly (int Va, uint Game, uint Patched)[] V3Hooks =
    {
        (0x00100010, 0x24425E00, 0x080C9C80),
        (0x0018331C, 0x0C058ADE, 0x0C03588E),
        (0x0017A4BC, 0x0C05ED8E, 0x0C0358C0),
        (0x00307BE4, 0x001628F0, 0x000D6420),
        (0x001B177C, 0x3C030031, 0x8E230038),
        (0x001B1784, 0x24636798, 0x24630080),
        (0x0016DD44, 0x0C04EB96, 0x0C035A03)
    };

    /// <summary>The spells' names, descriptions and HUD lines (0 English, 1 French, 2 German, 3 Italian, 4 Spanish), as gtext_feats5.py writes them.</summary>
    public static readonly (string Spell, int Language, string Key, string Text)[] Texts =
    {
        ("xantam", 0, "xantamfire_name", "Xantam's Fire"),
        ("xantam", 0, "xantamfire_desc", "Like the beholder Xantam, the caster sends trails of fire across the ground, burning all foes in their path. More trails at level 3, homing at level 5."),
        ("xantam", 1, "xantamfire_name", "Feu de Xantam"),
        ("xantam", 1, "xantamfire_desc", "Comme le tyrannoeil Xantam, le lanceur projette au sol des traînées de feu qui brûlent les adversaires. Plus de traînées au niveau 3, guidées au niveau 5."),
        ("xantam", 2, "xantamfire_name", "Xantams Feuer"),
        ("xantam", 2, "xantamfire_desc", "Wie der Betrachter Xantam schickt der Zaubernde Feuerspuren über den Boden, die alle Gegner verbrennen. Mehr Spuren ab Stufe 3, gelenkt ab Stufe 5."),
        ("xantam", 3, "xantamfire_name", "Fuoco di Xantam"),
        ("xantam", 3, "xantamfire_desc", "Come il beholder Xantam, l'incantatore scaglia scie di fuoco sul terreno che bruciano tutti i nemici. Più scie al livello 3, a ricerca al livello 5."),
        ("xantam", 4, "xantamfire_name", "Fuego de Xantam"),
        ("xantam", 4, "xantamfire_desc", "Como el contemplador Xantam, el lanzador envía estelas de fuego por el suelo que queman a los enemigos. Más estelas en el nivel 3, buscadoras en el 5."),
        ("bladebarrier", 0, "bladebarrier_name", "Blade Barrier"),
        ("bladebarrier", 0, "bladebarrier_desc", "Whirling axes circle the caster for 10 seconds, striking every nearby foe once a second. More damage with each level."),
        ("bladebarrier", 1, "bladebarrier_name", "Barrière de lames"),
        ("bladebarrier", 1, "bladebarrier_desc", "Des haches tournoient autour du lanceur pendant 10 secondes et frappent chaque seconde les adversaires proches. Plus de dégâts à chaque niveau."),
        ("bladebarrier", 2, "bladebarrier_name", "Klingenbarriere"),
        ("bladebarrier", 2, "bladebarrier_desc", "Wirbelnde Äxte umkreisen den Zaubernden 10 Sekunden lang und treffen jede Sekunde alle Gegner in der Nähe. Mehr Schaden mit jeder Stufe."),
        ("bladebarrier", 3, "bladebarrier_name", "Barriera di Lame"),
        ("bladebarrier", 3, "bladebarrier_desc", "Asce rotanti circondano l'incantatore per 10 secondi e colpiscono ogni secondo i nemici vicini. Più danni a ogni livello."),
        ("bladebarrier", 4, "bladebarrier_name", "Barrera de cuchillas"),
        ("bladebarrier", 4, "bladebarrier_desc", "Hachas giratorias rodean al lanzador durante 10 segundos y golpean cada segundo a los enemigos cercanos. Más daño en cada nivel."),
        ("antimagic", 0, "antimagicfield_name", "Antimagic Field"),
        ("antimagic", 0, "antimagicfield_desc", "A field of antimagic surrounds the caster: fire, cold, lightning and magic cannot harm the caster while it lasts. It lasts longer with each level."),
        ("antimagic", 0, "antimagicfield_hud", "Duration: %d seconds"),
        ("antimagic", 1, "antimagicfield_name", "Champ antimagie"),
        ("antimagic", 1, "antimagicfield_desc", "Un champ antimagie entoure le lanceur : le feu, le froid, la foudre et la magie ne peuvent pas le blesser tant qu'il dure. Dure plus longtemps à chaque niveau."),
        ("antimagic", 1, "antimagicfield_hud", "Durée : %d secondes"),
        ("antimagic", 2, "antimagicfield_name", "Antimagisches Feld"),
        ("antimagic", 2, "antimagicfield_desc", "Ein antimagisches Feld umgibt den Zaubernden: Feuer, Kälte, Blitze und Magie können ihm nichts anhaben, solange es besteht. Hält mit jeder Stufe länger."),
        ("antimagic", 2, "antimagicfield_hud", "Dauer: %d Sekunden"),
        ("antimagic", 3, "antimagicfield_name", "Campo Anti-magia"),
        ("antimagic", 3, "antimagicfield_desc", "Un campo anti-magia circonda l'incantatore: fuoco, freddo, fulmini e magia non possono ferirlo finché dura. Dura di più a ogni livello."),
        ("antimagic", 3, "antimagicfield_hud", "Durata: %d secondi"),
        ("antimagic", 4, "antimagicfield_name", "Campo antimagia"),
        ("antimagic", 4, "antimagicfield_desc", "Un campo antimagia rodea al lanzador: el fuego, el frío, el rayo y la magia no pueden dañarle mientras dure. Dura más en cada nivel."),
        ("antimagic", 4, "antimagicfield_hud", "Duración: %d segundos"),
        ("righteousmight", 0, "righteousmight_name", "Righteous Might"),
        ("righteousmight", 0, "righteousmight_desc", "For 10 seconds the caster grows mighty: melee attacks do more damage and blows against the caster do less. Stronger with each level."),
        ("righteousmight", 0, "righteousmight_hud", "+%d%% damage, -%d%% damage taken"),
        ("righteousmight", 1, "righteousmight_name", "Force du colosse"),
        ("righteousmight", 1, "righteousmight_desc", "Pendant 10 secondes, le lanceur grandit : ses attaques de mêlée infligent plus de dégâts et les coups qu'il reçoit moins. Plus puissant à chaque niveau."),
        ("righteousmight", 1, "righteousmight_hud", "+%d%% de dégâts, -%d%% de dégâts subis"),
        ("righteousmight", 2, "righteousmight_name", "Rechtschaffene Macht"),
        ("righteousmight", 2, "righteousmight_desc", "Der Zaubernde wächst 10 Sekunden lang: Seine Nahkampfangriffe richten mehr Schaden an, Treffer gegen ihn weniger. Stärker mit jeder Stufe."),
        ("righteousmight", 2, "righteousmight_hud", "+%d%% Schaden, -%d%% erlittener Schaden"),
        ("righteousmight", 3, "righteousmight_name", "Potenza del Giusto"),
        ("righteousmight", 3, "righteousmight_desc", "Per 10 secondi l'incantatore cresce: i suoi attacchi in mischia infliggono più danni e i colpi che subisce meno. Più potente a ogni livello."),
        ("righteousmight", 3, "righteousmight_hud", "+%d%% danni, -%d%% danni subiti"),
        ("righteousmight", 4, "righteousmight_name", "Poder justiciero"),
        ("righteousmight", 4, "righteousmight_desc", "Durante 10 segundos el lanzador crece: sus ataques cuerpo a cuerpo causan más daño y los golpes que recibe, menos. Más poderoso en cada nivel."),
        ("righteousmight", 4, "righteousmight_hud", "+%d%% de daño, -%d%% de daño recibido")
    };
}
