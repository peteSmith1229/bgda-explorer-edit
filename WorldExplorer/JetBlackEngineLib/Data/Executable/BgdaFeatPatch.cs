// ═══════════════════════════════════════════════════════════════════════════════
// GENERATED FILE: WorldExplorer/JetBlackEngineLib/Data/Executable/BgdaFeatPatch.cs
// by make_worldexplorer_feats26.py from featpatch.py version 26 (and featpool.py, featpatch_v25.py, featpatch_v24.py,
// featpatch_v23.py, featpatch_v22.py, featpatch_v21.py, featpatch_v20.py,
// featpatch_v19.py, featpatch_v18.py, featpatch_v17.py, featpatch_v16.py, featpatch_v15.py,
// featpatch_v14.py, featpatch_v13.py, featpatch_v12.py, featpatch_v11.py, featpatch_v10.py, featpatch_v9.py, featpatch_v8.py,
// featpatch_v7.py, featpatch_v6.py, featpatch_v5.py, featpatch_v4.py, featpatch_v3.py, gtext_feats17.py and the icons in
// ICONS.LMP). Don't edit by hand: run the script again.
//
// The feats patch as data: eight spells borrowed from the game's monsters (Xantam's Fire, Xantam's Orbs and Ilivarra's
// Dimension Door for Adrianna; Blade Barrier, Antimagic Field and Righteous Might for Kromlech; the kobold shaman's Insect
// Plague and the monsters' poison gas, as Poison Gas Arrow, for Vahn), the game's own unused vines (Entangle for Vahn),
// three feats the game has but never offers (Inferno Arrow for Vahn, Sorcerer's Blood for Adrianna, Power Attack for
// Kromlech) and three new ones (Expert Focus for Vahn: Power Attack for his bow, as a timed boost; Phase Axe for Kromlech: a
// magic axe thrown through every monster in its path; Shatter for Adrianna: a ring of frost that freezes the monsters round
// her), each on or off by itself, and Improved Enemy AI (smarter targeting, surrounding the hero, raising the alarm, the
// casters casting more, the brutes knocking the heroes back, the drow blinking, the wounded regrouping, the archers
// shooting Vahn's arrows and the bosses fighting harder), in game code nothing runs (the pool), so they need no
// three-player build and WorldExplorer can turn three players on or off
// underneath them.
// ═══════════════════════════════════════════════════════════════════════════════
namespace JetBlackEngineLib.Data.Executable;

/// <summary>One spell of the patch: its bit in <see cref="FeatPatch.SpellsAt"/>'s mask, hero class, feat bit, cast type,
/// cast code (0: none: Sorcerer's Blood, a passive, and Inferno Arrow and Poison Gas Arrow, whose types are the game's
/// arrow case), ability name, icon, and its level-up list entry ({class, 0, entry}) and ability record with the default
/// settings (Power Attack,
/// Sorcerer's Blood and Inferno Arrow: none, the game's own record is the one they use; Power Attack and Sorcerer's Blood
/// bring no icon: Power Attack's is the game's).</summary>
internal sealed record SpellPatchData(string Key, int Mask, int HeroClass, int Bit, int CastType, uint Cast, string Name,
                                      string IconName, string EntryBase64, string AbilityBase64, string IconBase64)
{
    /// <summary>The spell has its own ability record in the data block (every one but Power Attack, Sorcerer's Blood and Inferno Arrow, whose records are the game's).</summary>
    public bool OwnRecord => AbilityBase64.Length > 0;

    /// <summary>The spell has a cast case in the patch's cast table (every one but Sorcerer's Blood, a passive, and Inferno Arrow and Poison Gas Arrow, arrows).</summary>
    public bool HasCast => Cast != 0;
}

/// <summary>featpatch.py version 26 as data (see BgdaExecutable.BorrowedSpells.cs).</summary>
internal static class FeatPatch
{
    public const int Version = 26;

    /// <summary>
    /// The pool: 99 stretches of the game's own code that nothing runs (40,632 bytes), the same addresses and
    /// bytes in the stock executable and every three-player build (featpool.py says how they were found: its first pool's
    /// 61, 32,724 bytes, and since version 26 its second's 38, 7,908 bytes), by address.
    /// </summary>
    public static readonly (int Start, int End)[] Pool =
    {
        (0x00102910, 0x00102B78), (0x00102D80, 0x00103000), (0x0010C338, 0x0010C6D0), (0x0010CD50, 0x0010CF50),
        (0x00118978, 0x00118D70), (0x00129318, 0x00129538), (0x00129C48, 0x00129D78), (0x0012A710, 0x0012A8A0),
        (0x0012A960, 0x0012AA30), (0x0012B1A0, 0x0012B240), (0x0012B458, 0x0012B518), (0x0012BAE8, 0x0012BF90),
        (0x0012CD30, 0x0012D080), (0x0012E9AC, 0x0012EA68), (0x0012F790, 0x0012FA08), (0x00130540, 0x00130708),
        (0x00130888, 0x00130968), (0x00130A44, 0x00130B18), (0x00131170, 0x001312C0), (0x00131560, 0x00131640),
        (0x00132018, 0x00132170), (0x00132374, 0x001324F8), (0x00132D90, 0x001333F0), (0x00134CE0, 0x00134E18),
        (0x00134F18, 0x001350D8), (0x0013CDF8, 0x0013D000), (0x0013D218, 0x0013D430), (0x0013FD10, 0x0013FDC0),
        (0x00149C60, 0x00149D58), (0x00166E28, 0x00166FC0), (0x0016ACB8, 0x0016AE88), (0x0017C9D8, 0x0017CA90),
        (0x001819A4, 0x00181A88), (0x00191A20, 0x00191D20), (0x00196758, 0x00196818), (0x00196AB8, 0x00196C18),
        (0x00197DC8, 0x00197EB8), (0x00198968, 0x00198AC0), (0x00199A60, 0x00199B98), (0x0019AD68, 0x0019AE10),
        (0x0019E210, 0x0019E2B8), (0x001A28B0, 0x001A2A08), (0x001A9A38, 0x001A9B60), (0x001AE930, 0x001AEA30),
        (0x001B8DA0, 0x001B8F18), (0x001B9570, 0x001B9688), (0x001B9E68, 0x001B9F68), (0x001BAFF8, 0x001BB100),
        (0x001C36E8, 0x001C3870), (0x001C6B38, 0x001C6BE0), (0x001CBA38, 0x001CBB48), (0x001D32D8, 0x001D3420),
        (0x001E0ACC, 0x001E0BC0), (0x001E1328, 0x001E13D0), (0x001E8558, 0x001E8790), (0x001EE678, 0x001EE720),
        (0x001F3D24, 0x001F3DC8), (0x001FF158, 0x001FF248), (0x001FF340, 0x001FF4E0), (0x00204DE0, 0x00204EE0),
        (0x00206460, 0x00206800), (0x002068C8, 0x00206AC0), (0x002070A4, 0x00207158), (0x002071C0, 0x00207400),
        (0x00207488, 0x002075C0), (0x00207A08, 0x00207C00), (0x00207CD0, 0x00207D80), (0x00208160, 0x00208320),
        (0x002089C0, 0x00208B10), (0x00208CB0, 0x00208E48), (0x00209038, 0x00209118), (0x002097E8, 0x002098B0),
        (0x0020A534, 0x0020A608), (0x0020AC28, 0x0020AD30), (0x0020B4FC, 0x0020B59C), (0x0020B890, 0x0020BCB8),
        (0x0020C080, 0x0020C280), (0x0020C4C8, 0x0020C61C), (0x0020D70C, 0x0020D7E8), (0x0020DA88, 0x0020DBC8),
        (0x0020E6E8, 0x0020EB58), (0x0020ECC4, 0x0020EDA0), (0x0020EF40, 0x0020F068), (0x0020F6A8, 0x0020F9C8),
        (0x0020FCE0, 0x0020FFC0), (0x00210058, 0x002102A0), (0x00210A00, 0x00210C08), (0x00210CA0, 0x00210E60),
        (0x002110D8, 0x002111C0), (0x002119F0, 0x00211AC0), (0x00211C24, 0x00211CD0), (0x00218884, 0x00218970),
        (0x00218A54, 0x00218B48), (0x0021BEE8, 0x0021C000), (0x0021C990, 0x0021CA98), (0x00221950, 0x00221A10),
        (0x00221B88, 0x00222320), (0x00224B68, 0x00224CB8), (0x00225E78, 0x00225F30)
    };

    public const int PoolBytes = 40632;

    /// <summary>The game's bytes in the pool, one stretch after another: zlib, then base64.</summary>
    public const string PoolOriginalBase64 =
        "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";

    public const string PoolOriginalSha256 = "ae51287635dc47d8cb814d005ffd6afa6af6b6509f0cc19682d1168effe3f8f5";

    /// <summary>How much of the pool the patch takes: its eleven data blocks, its code, its vtable copies and its cast table.</summary>
    public const int PoolUsedBytes = 35832;

    /// <summary>The code (27,556 bytes in 76 pieces over the pool's stretches): the same whichever spells are on.</summary>
    public static readonly (int Va, string Base64)[] Code =
    {
        (0x00102910, "QP+9JyEAFjyQuNYmkAKEjgEABTyiGggMPCgFACG4QAAhQMIC4AASkfAACZH4AAqRfAqrjyYAaaUoAGqlMAAEPPA0hCSyNggMISiAAgABwMYQAIHG"
            + "gQgARgAAoueACABGEACi5xQAgcaBCABGBACi54AIAEYUAKLnGACBxoEIAEYIAKLngAgARhgAouchIKADEAClJyAApifiIgQMIAAHJGcAQBohIIAC"
            + "mucGDCAApSdjAEAQAAAAAAAAUYwAAFSsFAAsxhQAgMYBYwBGEAAtxhAAgMZBawBGsACt57QArOeSwwQMAAAAACGYQAAEuwQMsAIEJFIAQBAhgEAA"
            + "sAChx7QAoscCCAFGwhACRgAAA0aAPwE8ABiBRDQAA0YJAAFFAAAAAAQAAEbAQQE8ABiBRMMYAEZCCANGghADRgMAABAAAAAAAAiARAAQgEQQAIDG"
            + "AAABRqAAoOcUAIDGAAACRqQAoOcYAIiOqACoryEgAAKgAKUnITBgAiE4AAAQJwgkvDQEDCFIAAAEAcDGUAEA5lCcgMdUAQDmHAAgxiBBATwACIFE"
            + "AAABRhwAAOYaAAg8sCgIJTgACK4MAAiOBQAINQwACK6QAgCuyKSJjyFQ1wLoAEqRgFgKACFQagEhUCoBmAIKrggBy46c/yklIUgrAZQCCa7//wgk"
            + "nAIIrpACiI6gAgiioQIXoiFQ1wLwAEiRogIIovgASJGjAgiipAIArpACBCYhKCACIGwFDCEwIAKyJQQMISAAApn/ABD//1ImwAC9Jz2KBQgCAAIk"
            + "ZEMICAAADCRkQwgICAAMJGRDCAgYAAwk"),
        (0x00102D80, "MP+9J6AAv/+oALD/sACx/7gAsv8hgIAAIQASPJC4UibIpIiPmAIJjipQKAGIAEAVAAAAAJACCI4JAAARAAAAAAwAEY0GACASAAAAAFoBKYYDACAZ"
            + "AAAAACsAABAAAAAAAAFAxhAAAcaBCABGAACi54AIAEYQAKLnFAABxoEIAEYEAKLngAgARhQAoucYAAHGgQgARggAoueACABGGACi5yEgoAMQAKUn"
            + "IACmJ+IiBAwgAAckISAAAprnBgwgAKUnYwBAEAAAAAAAAFGMkAIIjgMAABEAAAAAZmwFDJACBCaQAgCukAIEJiEoIAIgbAUMITAgAhwAIMYgQQE8"
            + "AAiBRAAAAUYcAADmyKSIj5QCCY4jQAkBZAAIKQkAABUAAAAAISAAAiEoIAL6QwE8AGCBRIA/ATwAaIFEWnAEDABwgETIpIiPpAIJjipQCQE6AEAV"
            + "AAAAABAAIMYQAAHGAQABRgIAAEYUACLGFAABxoEQAUaCEAJGAAACRhgAIsYYAAHGgRABRoIQAkYAAAJGHAABxkIIAUY0AAFGJwAARQAAAAAMAUmO"
            + "IUgJAaQCCa6gAgqS4DgLJBhQSwEqAAs86LxrJSFQSwEAAEWNISAgAqICBpKjAgeSAQAIPKCGCDUhSAAAIVAAAAIACzwOfgQMRABrNTwAKI4IAAk8"
            + "JFAJAQ4AQBUEAAk8JFAJARABQMYCAEARgD8BPAAAgUSMAiHGNAgARgIAAEUAAAAAjAIg5iEgIALsTAcMoAIFkqAAv9+oALDfsACx37gAst8IAOAD"
            + "0AC9JyEgAAKgAL/fqACw37AAsd+4ALLf0AC9JzgAiYwIACqFDAArjSEgigAIAGABAwAFJA=="),
        (0x0010C338, "gP+9JyEAFjyo9tYmkAKEjiAABTyiGggMPCgFACGIwgIAASiSgEgIACFACQFIA4imnKSIjxkACTwNZik1GEAJAW48CTxf8yk1IUAJAZykiK8CNAgA"
            + "AwAJJBsAyQAQMAAAAQDGJDEABTz4EaUkMJgIDFAApCdQAKQnsjYIDCEogALwACiSAKCIRCClgEZgAcQmKACIjBUABBEAAAAAOAHMxjwBzcYAcIBE"
            + "QAHFkkEBxpJCAceSQwHIkkQByZJFAcqSRgHLkkcBzJIAAKyvSAHMjggArK9MAcyOEACsr1ABzI4YAKyvxE8FDCAAoK8kAdbGQaUWRgAAgERorQBG"
            + "LAHAxkKtAEYwAcDGaa0ARjQB0o4iAEAaIYAAAMzDBAwhIAACgACg56bDBAwhIAACgAChx4IIFkYQAIPGgBADRnAAoueCABZGFACDxoAQA0Z0AKLn"
            + "GACCxigBw8aAEANGeACi50IIFUaAAKHnAgAVRoQAoOeIAKCvYAHEJnAApSeAAKYn0j8FDABggEQhgBICAQAIPCpACALg/wAVAAAAABAAgMZBABRG"
            + "MACh50AAFEZAAKHnFACAxkEAFEY0AKHnQAAURkQAoecYAIDGQQAURjgAoedAABRGSACh5zAApCdAAKUnkACmJ+IiBAxAAAckkACwJwAAEo4+AEAS"
            + "BAAQJvz/VBIMAEiOJAAJMfn/IBUYAAkx9/8gERAACTEEACARAAAAAFoBSYby/yAZAAAAABAAQMYQAIHGAQABRgIAAEYUAELGFACBxoEQAUaCEAJG"
            + "AAACRgQAAEYcAEHGAQABRjQAFEbj/wBFAAAAAAwASI4QAAgxEgAAETwASY5CAAo8AAFKNSRQKgENAEAV+AArkoAoCwAhKKsAAQAKPIAASjUkUCoB"
            + "AgBAEQAAAABCKAUAAwCgGAAAAADwswQMISBAAiEgQAIhKIAC4AAmkugAJ5IBAAg8oIYINSFIAAAhUAAAAwALPA5+BAxABWs1wf8AEAAAAACAAL0n"
            + "PYoFCAIAAiTQ/70nAACw/xAAsf8gAL//IYiAACGAoAAxAAg8cHQIJTgAKK5QACiOCQAAEQAAAAAMAAmNBgAgEQAAAABIAiqRWQArkgIASxUAAAAA"
            + "SAIgoVAAJCaMIAQMAgAFJCEgIAI2LQQMISgAAAEAEDIGAAASIAC/3yEgIAIQALHfAACw3xq5BAgwAL0nEACx3wAAsN8IAOADMAC9Jw=="),
        (0x0010CD50, "PACIjAcAABUAAAAAOACJjAgAKoUMACuNISCKAAgAYAEDAAUkMP+9J6AAsP+oALH/sACy/7gAs//AALT/yAC//yGAgAAMABGNAQAYJFoAIBIhIAAC"
            + "EAApjhAACa4UACmOFAAJrhgAKY4YAAmuIgASPIgbUiYcACDG3AVBxgAAAUYcAADmOrUEDCEgAAIhQAAAIVAAAkgASZX9/wslAwBrLQIAYBHoAykl"
            + "MPgpJUgASaUBAAglCQALKfb/YBUMAEolIgASPIgbUibIpIiP1AVJjjUAIBEAAAAAGwAJARBQAAAxAEAVAAAAABwAAMbYBUHGAAABRhAAAcaBCABG"
            + "AACi54AIAEYQAKLnFAABxoEIAEYEAKLngAgARhQAoucYAAHGgQgARggAoueACABGGACi5yEgoAMQAKUnIACmJ+IiBAwgAAcktAASkrUAE5IgALQn"
            + "AACEjhQAgBAEAJQm/P+REAAAAAAMAIiMNAAIMRAACST3/wkVAAAAACEoIAIhMEACIThgAgEACDyghgg1IUgAACFQAAACAAs8Dn4EDAgAazXr/wAQ"
            + "AAAAAEAACI7IpImPKsAJASEgAAKgALDfqACx37AAst+4ALPfwAC038gAv9+O/wAX0AC9JwgA4AMAAAAAWgFChiAAATxsZCGMAAEhMMIBQigIAOAD"
            + "JRBBAA=="),
        (0x00118978, "IgCDECAAASQiAIEQIQABJGEAgRAiAAEkeQCBECMAASQnAIEQCQABJBEAgRAkAAEkOACBECUAASRBAIEQJgABJEoAgRAnAAEkhgCBECgAASSaAIEQ"
            + "KQABJLkAgRAqAAEkwgCBEAAAAABV+QUIAAAAACIAATykGyGMgAAhMBsAIBQAAAAAVfkFCAAAAABN+QUIAAAAACEgwAIACAU0ohoIDDwoBQAiAAg8"
            + "iBsIJSFAAgHABRKRyAURkdf5BQgxAAQ8ISDAAgBABTSiGggMPCgFACIACDyIGwglIUACASAGEpEoBhGR1/kFCDEABDwhIMACohoIDAABBSQiAAg8"
            + "iBsIJSFAAgGIBhKRkAYRkdf5BQgxAAQ8ISDAAgCABTSiGggMPCgFACIACDxoSwglIUACAbAAEpG4ABGR1/kFCDEABDwhIMACAQAFPKIaCAw8KAUA"
            + "IQAIPJC4CCUhQAIB8AASkfgAEZHX+QUIMQAEPCEgwAICAAU8ohoIDDwoBQAhAAg8cLoIJSFAAgHgABKR6AARkdf5BQgxAAQ88P+9JyEgwAIAEAU0"
            + "ohoIDDwoBQAiAAg8iBsIJUBIAgAhSAkB4AUplRkAKSUyAAokGwAqARJIAAAAAKmvIgAEPFLsBAzIIYQkIShAAAAAqY8AYIlEIGOARgBogEQQAL0n"
            + "0fkFCCEgoAPw/70nISDAAgAgBTSiGggMPCgFACIACDyIGwglIUACAfAFCZEAAKmv+AUJkQQAqa8iAAQ8UuwEDNwhhCQhKEAAAACpjwBgiUQgY4BG"
            + "BACpjwBoiURga4BGEAC9J9H5BQghIKAD8P+9JyEgwAIEAAU8ohoIDDwoBQAhAAg8kLsIJUBIAgAhSAkB4AAplQAAqa8hAAQ8UuwEDIC8hCQhKEAA"
            + "AACpjwBgiUQgY4BGAGiARBAAvSfR+QUIISCgA/D/vSchIMACCAAFPKIaCAw8KAUAIQAIPOjmCCUhQAIB4AAJkQAAqa/oAAmRCgAKJBsAKgESSAAA"
            + "BACprxBIAAAIAKmvIQAEPFLsBAzY54QkIShAAAAAqY8AYIlEIGOARgQAqY8AaIlEYGuARggAqY8AcIlEoHOARhAAvSfS+QUIISCgAyEgwAIQAAU8"
            + "ohoIDDwoBQAhAAg8EOgIJSFAAgHgABKR6AARkdf5BQgxAAQ88P+9JyEgwAIgAAU8ohoIDDwoBQAhAAg8qPYIJSFAAgHwAAmRAACpr/gACZEKAAok"
            + "GwAqARJIAAAEAKmvEEgAAAgAqa8hAAQ8UuwEDLD3hCQhKEAAAACpjwBgiUQgY4BGBACpjwBoiURga4BGCACpjwBwiUSgc4BGEAC9J9L5BQghIKAD"),
        (0x00129318, "gP+9JzAAv/84ALD/QACx/0gAsv9QALP/WAC0/2AAtf9oALb/cAC053QAted4ALbnLYCAAC2IQAEtoIABLaggAy2wAAELAIAWIZAAADQAM4YAgHMm"
            + "HAAUxhwAIMYApQBGoEEBPAAAgUQApQBGCwAAEAAAAAAUAAzGFAAgxgFjAEYQAA3GEAAgxkFrAEaSwwQMAAAAACGYQAAkAdTGAQBIJkJACAABAEky"
            + "AgAgFQAAAAAjQAgAQEMIACFAaAIgAKivzMMEDCEgAAFGBQBGpsMEDCAApI+GBQBGAqgURgAAoOcCsBRGBACg5wgAoK8tSCACC0gUAiQAqa8cAAzG"
            + "1KSEjxAAJSUtMKADAQAHJAgjBQwhQAAAgD8BPAAIgUQ0AAFGDAAARYagAEYpAIASAAAAAIKgAEYAQAE8AAiBRIEQAUbIQgE8AAiBRDQQAUYgAAFF"
            + "AAAAACQAqY8CqAJGEAAhxQAAAUYQAKDnArACRhQAIcUAAAFGFACg5xgAKo0YAKqvHAAMxtSkhI8AHgUMEAClJyQAqY8YACHFQEEBPAAQgUSACAJG"
            + "NBAARgkAAUUAAAAAAEIBPAAQgUSBCAJGNAACRgMAAUUAAAAArh4ICBgAoOcBAFImBQBILrD/ABUAAAAALSAAAi3IoAIwAL/fOACw30AAsd9IALLf"
            + "UACz31gAtN9gALXfaAC233AAtMd0ALXHeAC2xwgAIAOAAL0n"),
        (0x00129C48, "IShAApACTI4EAI0tQQCgEYBgDAAiAA08iButJSFwrAEYB8+NJQDgEcikmI8oB9mNI8g4A///OScQBriNK8g4Ax0AIBMMAJiMEAAYMxsAABP//+8l"
            + "GAfPrUAwBgBAOAcAwP+9JwAAv/8IAKT/EACm/xgAp/8gAKj/KACp/zAAqv8xAAQ8+JWEJLI2CAwhKEACAAC/3wgApN8QAKbfGACn3yAAqN8oAKnf"
            + "MACq30AAvScCAAAQIShAAhgHwK2QAkyOgGAMACIADTyIG60lIXCsAegGz40PAOAR+AbYjQgAGS8MACATIcC4AfAFGJNkABgnZAAZJBhY2AAyAGsl"
            + "GgB5ARIwAAAYWPgAMgBrJRoAeQESOAAACADgAwAAAAA="),
        (0x0012A710, "yKSIjwoACSQaAAkBEEAAAF0AABVQAIqMWwBAEQAAAAAMAEuNWABgEQAAAABg/70nYAC//2gAsP9wALH/eACy/4AAs/+IALT/IYCAACGIYAGQAiSO"
            + "ohoIDAABBSQiAAg8iBsIJSFAAgGwBhKRuAYTkSIACDyIGwglHAAAxhAAAcaBCABGAACi54AIAEYQAKLnFAABxoEIAEYEAKLngAgARhQAouc0AADF"
            + "GAABxoEIAEYIAKLngAgARhgAouchIKADEAClJyAApifiIgQMEAAHJCAAtCcAAISOIwCAEAQAlCb8/5EQDACIjDQACDEQAAkk+P8JFQAAAAAQAIDE"
            + "EAABxgEAAUYCAABGFACCxBQAAcaBEAFGghACRgAAAkYEAABGHACBxAEAAUYcAAHGNAABRuj/AEUAAAAAISggAiEwQAIhOGACAQAIPKCGCDUhSAAA"
            + "IVAAAAMACzwOfgQMYABrNdz/ABAAAAAAISAAAmAAv99oALDfcACx33gAst+AALPfiAC036AAvSfmwwYIAAAAAA=="),
        (0x0012A960, "RKqSjyoAEzzovHMmKABAGgAAAAAAAGSOIgCAEAAAAAAMAIiMBAAIMR4AABUAAAAAEACAxBAAAcYBAAFGAgAARhQAgcQUAALGQQgCRkIIAUYAAAFG"
            + "BAAARhwAgcQBAAFGNAAURg8AAEUAAAAAISgAAoQAJo6IACeOAQAIPKCGCDUhSAAAIVAAAA5+BAyMACuOAABojoQCAMWsACHGKAABRoQCAOXgOHMm"
            + "2P8AEP//UiaQAL/fgACy34gAs99wALTHdAC1x3gAtscIAOADoAC9Jw=="),
        (0x0012B1A0, "gP+9J3AAv/9gALL/aAC052wAtechkIAAEAAAxkAAoOcUAADGRACg5xgAAMZcACHGAAABRkgAoOcgAEDGAD8BPAAIgUQCAAFGGABBxgAAAUZIAKHH"
            + "AQUBRhAATMZAAKDHAWMARhQATcZEAKDHQWsARgJgDEZCaA1GAAABRkQFAEagQQE8AAiBRGitAUaSwwQMAAAAAIR4BggAAAAA"),
        (0x0012B458, "HAAWxkGlFkYAAIBEaK0ARiwBYMZCrQBGMAFgxmmtAEY0AXKOJABAGmQAoK/MwwQMZACkj2gAoOemwwQMZACkj2gAoceCCBZGEAADxoAQA0YoAKLn"
            + "ggAWRhQAA8aAEANGLACi52AAoscoAWPGgBADRjAAoudCCBVGNACh5wIAFUY4AKDnPACgr2ABZCYoAKUnNACmJ9I/BQwAYIBEZACojyFAEgFkAKiv"
            + "AQAJPCpICQHe/yAVAAAAAFiqBAgAAAAA"),
        (0x0012BAE8, "IAAIPGBkCCUMAAmNAgApMeEAIBEAAAAA3wCgEAAAAAAMAKmMIAApMdsAIBEMAImMIAApMdgAIBU4AImMkAAKJQAAS43UAGARAAAAAPz/aRUEAEol"
            + "HACAxBwAocQAAAFGJAABxUAAAUY0CA1GygABRQAAAAAQAILEEACjxIEQA0YUAIPEFACkxMEYBEYCEQJGQhkDRgAhBUYgAAXFQikFRjQoBEa8AAFF"
            + "AAAAAESqiY8qAAo86LxKJSFYAAC2ACAZAAAAAAAATI0EAIUR//8pJeA4SiX5/wAQAQBrJQMAbC2tAIARQGELAIBZCwAhWGwBIVhoAXD/vSeAAL//"
            + "QACw/1AAsf9gALL/cACz/0gAtP8AAKznBACt5wgArucMAKDnEACi5xQAo+ctgIAALYigAHABciXIpImPOAAKjS1YQAIhmAAAAABsjQUAkBUEAGyN"
            + "I2AsAStgigFHAIAVAAAAAAEAcyYIAGwq9v+AFQwAayUhoAAAEACixxQAo8cA/w08ACCNRP//EyQhcAAALVhAAiAADzywZO8lyKSJjyAACDxgZAgl"
            + "OAAKjQZg1AEBAIwxEwCAFQAAbI0HAIARAAAAAAUAkBEEAGyNI2AsAStgigELAIAVAAAAAAAA5cVCKQJGBADmxYIxA0ZAKQZGNCAFRgMAAEUAAAAA"
            + "BikARi2YwAEBAM4lDABrJQgAzCnm/4AVCADvJVQAYAYBAAwkBGBsAiWgjAKjrwQMAAAAAAoAQBQAAAAA/wCMMv//jSUkYI0B//+NJSRgjQHK/4AR"
            + "AAAAAEUAABAAAAAAwFgTAIBgEwAhWGwBIVhyAQAAcK0IAGCtDQAAEBwAq68cAKuvcQEMgiZgLAEHAIwxBwCAFQAAAACjrwQMAAAAAAMAQBQcAKuP"
            + "MQAAEAAAYK0cAKuPyKSJjwQAaa0gAAg8YGQIJcBgEwAhYIgBDACgxygAAcVACABGUACCxYIQAUYQACPGgBADRlQAg8XCGAFGFAAkxsAYBEYQAATG"
            + "ASECRgIhBEYUAAXGQSkDRkIpBUYAIQVGCABtjQgAoBUwAAXFLAAFxUIpBUY0IAVGCAAARQEADSQOAAAQCABtrUIpBUY0IAVGCgABRQAAAAAIAGCt"
            + "GAAkxjABBSUMAKCsEACi5BQAo+QYAKTkAwAAEDQADcUtKCACBACtxy0gAAIAAKzHCACux4AAv99AALDfUACx32AAst9wALPfSAC035AAvScg/70n"
            + "XHAECHAAsv/w/70nAAC//yAACDxgZAglwGATACFgiAEcAKDHHAABxgAAAUYoAAHFAAABRlAAgcVCCABGQACh51QAgcVCCABGRACh50gAoK8QACHG"
            + "MACh5xQAIcY0AKHnGAAhxlBBDTwAEI1EQAgCRjgAoefUpISPMAClJ0AApie4gozHojMFDAMAByQACIBENAABRgcAAUUBAAIkgD8NPAAIjUQ0CABG"
            + "AgABRQAAAAAhEAAAAAC/3wgA4AMQAL0nCQABJAcAYRQiAAE8pBshjIAAITALACAQBAABJAkAABAvA4GiJgABJAYAYRQiAAE8pBshjAAEITACACAQ"
            + "BwABJC8DgaJXeQUIAAAAAA=="),
        (0x0012CD30, "oP+9J0AAtP8QALH/AQAUJFAAv/8tiIAAMACz/yAAsv8AALD/pAEnjpcA4BA8AiiOkwAAFQAAAAAgABk8YGQ5JwwAOI8FAAgzjQAAEQAAAADIpISP"
            + "cQEogiZAiAAPAAgxhAAAFQAAAAAEAAgzDgAAEXQBKIYDAAkkCwAJFQwA6IwMAAiNIAAIMQcAABEAAAAAluUGDC0gIAIDAEAQAAAAAGMxCAwtICAC"
            + "IAAZPGBkOScMADiPAQAIMwYAABWkASeOLRjgAMikhI8tSIAAi2EECHEBKJJsAOAQAAAAAAwA8IwMAAiOIAAIMWIAABEMAiiOPgAAEQAAAAAMABKN"
            + "OwBAEgAAAAA5AFASDABJjiAAKjE2AEARBAAqMTQAQBXIpImPRAIqjiNIKgEQACqPK0gqAS4AIBEAAAAAEAACxhAAJsaBEAZGghACRhQABsYUACfG"
            + "gTEHRoIxBkaAEAZGGAAGxhgAJ8aBMQdGgjEGRoAQBkYQAEPGEAAmxsEYBkbCGANGFABGxhQAJ8aBMQdGgjEGRsAYBkYYAEbGGAAnxoExB0aCMQZG"
            + "wBgGRjQYAkYqAAFFAAAAABwAJMYcAAXGACEFRhwAJccAIQVGAiEERjQgAkYGAABFAAAAABQAJcdCKQJGNBgFRhwAAUUAAAAABsgEDC0gIAIfAEAQ"
            + "LZBAAB0AUBIAAAAAEAACxhAAJsaBEAZGghACRhQABsYUACfGgTEHRoIxBkaAEAZGGAAGxhgAJ8aBMQdGgjEGRoAQBkYgABk8YGQ5JxgAJcdCKQJG"
            + "NAAFRggAAEUAAAAApAEzJmZsBQwtIGACLSBgAi0oQAIgbAUMLTBAAsikhI8TYgQIpAEnjoRhBAgtGOAAVGIECCGgAAAhAAg8qPYIJcikiY8hUCUB"
            + "HAEYjVQBC40jYGkB//+MJStgmAEDAIARI2hqAQIAoQUAAAAAVAEKrbABCyWwAgwlIWgAACFwYAH/fw88AAB5jREAJBMEAHmNI8gpA///OScrCDgD"
            + "AwAgFCoILwMDAAAQCmhtAQtwYQELeCEDCABrJfL/bBUAAAAACmjNAQAApK0IAOADBACqrSNgKQP//4wlK2CYAQMAgBEjYCoDAgCBBQAAAAAEAGqt"
            + "CADgAwAAAAA="),
        (0x0012E9AC, "wP+9JzAAv/8gALD/KACx/xAApK8UAKznIYCAACAAATxsZCGMAAEhMCAAIBBaAQiGHgAAGXQBCIYcAAARCQAJJBoACREOAAkkGAAJEaQBCI4WAAAR"
            + "AAAAAAwACI0TAAARAAAAACEAETxA7zEmAAEkJrOCBwyQACUmCgBAEAABJCa7ggcMkAAlJgS7BAxQAAQkBABAECEgQAAhKAAC/usGDJgAJo7WWQYI"
            + "AAAAAO9ZBggAAAAA"),
        (0x0012F790, "0P+9JyEAFjyQu9YmkAKEjgQABTyiGggMPCgFAEBAAgAhQMgC4AAIlQCgiEQgpYBGMAAEPCg0hCSyNggMISiAAszDBAw0AISGRgUARqbDBAw0AISG"
            + "hgUARhQNBgwQAIQmKwAQPPi8ECYrABE8KL0xJgQAEiQAAADGBAABxoIAFUbCCBZGgBADRgAYgEQ0EANGEQAARQAAAAAQAIPGAAAkxsEYBEbCGABG"
            + "FACExgQAJcYBIQVGAiEBRsAYBEYAIIBEgSACRsMYAkY0GBRGAgAARQAAAAAGHQBG//9SJgwAECbk/0AWDAAxJgQBwMY0oABGQAABRQAAAAACqBRG"
            + "AACg5wKwFEYEAKDnCACgrxwAjMbUpISPEACFJiEwoAMBAAckCCMFDCFAAACAPwE8AAiBRDQAAUYEAABFAAAAAAKlAEYUAcHGAaUBRgQBwMY0oABG"
            + "KAABRQAAAAACqBRGEACBxgAAAUYQAKDnArAURhQAgcYAAAFGFACg5xgAiI4YAKivHACMxtSkhI8AHgUMEAClJxgAgcYMAcLGgAgCRjQQAEYGAAFF"
            + "AAAAABABwsaBCAJGNAACRgQAAEUAAAAACAHAxuH/ABABpQBGGACg5zjTBAwhIIACEACojxAAiK4UAKiPFACIrhgAqI8YAIiuOrUEDCEggAI40wQM"
            + "ISCAAjAAvSc9igUIAgACJFwAiIwYAAAVDACpjCQAKjEVAEAVGAAtMRMAoBE8AIuMAgBgESFgAAAMAGyNAQAIJFwAiKz0AQgkRACIrFQAhoxYAIeM"
            + "ISCgACEogAEBAAg8oIYINSFIAAAhUAAAAwALPA5+BAhAAWs1CADgAwAAAAA="),
        (0x00130540, "0P+9JwAAv/8IAKT/EACl/xgAsP8gALH/IgAQPGhLECYhiAAABQAqKgoAQBEAAAAAAEERAMBIEQAhQAkBIUAIAggACCWAUBEAIVAKAi8AABCAAEol"
            + "BQAKJAcAKhYAAAAAIQAIPFC5CCUhAAo8aLlKJSYAABAAAAAABwAKJA0AKhIIAAokEQAqEgkACiQVACoSCgAKJBkAKhIAAAAAIQAIPDC7CCUhAAo8"
            + "SLtKJRcAABAAAAAAIQAIPFC8CCUhAAo8aLxKJREAABAAAAAAIQAIPKjnCCUhAAo8wOdKJQsAABAAAAAAIQAIPNDoCCUhAAo86OhKJQUAABAAAAAA"
            + "IQAIPGj3CCUhAAo8gPdKJSgAqq8IAKTflusEDCEoAAEJAEAUKACqjwAARY2W6wQMCACk3wQAQBQAAAAACACk35brBAwQAKXfAgBAECFIAAA4AEmM"
            + "gEARACFACALQAAmtAQAxJgsAKCqv/wAVAAAAACIAEDyIGxAmHAAIjgEACDEKAAARCACk35brBAxgAAUmCABAFAgApN8xAAU8lusEDPCppSQDAEAU"
            + "CACk35brBAwQAKXfAAC/3xgAsN8gALHfCADgAzAAvSc="),
        (0x00130888, "BwAAEQEACSQTAAkRAgAJJB8ACREAAAAAz4IHCDAAoK8EuwQMmAAEJCcAQBAhIEAAQAClJ1AApichOAACIUAAACFIAABgACqOzrAEDCFYAAAdAAAQ"
            + "AAAAAAS7BAycAAQkGQBAECEgQABAAKUnUACmJyE4AAIhQAAAIUgAAGQAKo50sQQMIVgAAA8AABAAAAAABLsEDKQABCQLAEAQISBAAGwAKI4AAKiv"
            + "QAClJ1AApidcAKePIUAAAiFIAAAhUAAAWlsGDGgAK45wAL/fYACy32gAtMdsALXHCADgA4AAvSc="),
        (0x00130A44, "AgAAEAEADCQhYAAAIAABPGxkIYwAASEwAwAgFAAAAACMPAQIAAAAACEAGTxA7zknsAA/r7QAJq+4ACevvAAor8AAKa/EACqvyAArr9QALK8QAC6P"
            + "FgDAESFoAACcpI+PGQACPA1mQjQYeOIBbjwCPF/zQjQheOIBnKSPrwJ8DwAbAO4BEBAAABJ4AAAIAEAUAwACJBsA4gEQEAAAgBACACEQIgMgAEWQ"
            + "IgBNlDgAra/MACWvjDwEDNAALa8hABk8QO85J/ZZBgwhIEAAWMUECAAAAAA="),
        (0x00131170, "wP+9JzAAv/8QALD/GACx/yAAsv8hgIAAIYigACGQwAAQACiOAACorxQAKI4EAKivGAAojggAqK8hIAACISigAyEwAABWLAQMITgAACEACDywjAgl"
            + "OAAIrjwAAK4MAAiOAQAINQwACK4iABk8iBs5JxwAIMbcBSHHAAABRhwAAObIpIiPIUASAUAACK7//wgkRAAIrjwABCYhKCACIGwFDCEwIAKyJQQM"
            + "ISAAAiFAAAAhSAAAIVAAAkgASaU0Qgw8AwALKQYAYBUAAAAA8EEMPAYACykCAGAVAAAAAHBBDDxMAEytnKSLjxkADTwNZq01GFhtAW48DTxf8601"
            + "IVhtAZyki68CbAsAUABNpVVVKSUBAAglCQALKef/YBUMAEolMAAEPHB5hCSyNggMISgAAjAAv98QALDfGACx3yAAst8IAOADQAC9Jw=="),
        (0x00131560, "1AAsjy8AgBEBAAIk2AAirwS7BAw4AQQkIQAZPEDvOScoAEAQISBAALQAJo+4ACePvAAoj8AAKY/EACqPyAArj9gAI48AACDFBAAhxRgAIsccACPH"
            + "ACCDRCAhgEbCGARGQgECRoIJA0ZBKQZGggEDRsIJAkaAMQdG3AAl5+AAJucIACKN5AAir9wAKScUACKPGBBDACFQQgEjGAMA2AAjr4w8BAzMACWP"
            + "IQAZPEDvOSf2WQYMISBAANgAI4/U/2AEAAAAALAAP48QAP8nCADgAwAAAAA="),
        (0x00132018, "RKqIjyoACTzovCklIRAAAAB/CjwAAIpEyKSKj0QCi4xw/kolKsBLARAAgcQUAILEGACDxCwChMQwAoXENAKGxMEJBEbCOQdGARIFRgJCCEbAOQhG"
            + "ARoGRgJCCEbAOQhGOAKIxAJCCEY4AAAZAAAAAAAALI0yAIARAAAAAAwAjY0EAK0xLgCgFQAAAAAQAInFQUoBRkJKCUYUAIrFgVICRoJSCkZASgpG"
            + "GACKxYFSA0aCUgpGQEoKRhwAABcAAAAAEACKxYFSBEaCUgpGFACLxcFaBUbCWgtGgFILRhgAi8XBWgZGwloLRoBSC0Y0UAhGDgABRQAAAAA0UAdG"
            + "CwABRQAAAAAcAIrFHACLxIBSC0ZIQg08AFiNRIBSC0bESglGNFgKRgYAAEUAAAAANEgARgMAAEUAAAAABkgARiEQgAHgOCklyP8AEP//CCUIAOAD"
            + "AAAAAA=="),
        (0x00132374, "YP+9J5AAv/+AALL/iACz/3AAtOd0ALXneAC25xgAgMRgAKDnnKSGjxkACTwNZik1GDDJAG48CTxf8yk1ITDJAJykhq8CNAYAAwAJJBsAyQAQMAAA"
            + "AQDGJDEABTz4EaUkMJgIDEAApCdAAKQnsjYIDCEoAAIcABTGgAAgxgClAEYhABM8qPZzJmABZCYoAIiMAwAEFQAAAAAWrQQIAAAAADgBbMY8AW3G"
            + "AHCAREABZZJBAWaSQgFnkkMBaJJEAWmSRQFqkkYBa5JHAWySAACsr0gBbI4IAKyvTAFsjhAArK9QAWyOGACsr8RPBQwgAKCvFq0ECAAAAABEqoiP"
            + "KgAJPOi8KSUcAAAZAAAAAAAAKo0WAEARAAAAAAwAS40EAGsxEgBgFQAAAAAQAEDFEACBxAEAAUYCAABGFABBxRQAgsRBCAJGQggBRgAAAUYEAABG"
            + "HABBxQEAAUY0AAxGAwAARQAAAAAIAOADIRBAAeA4KSXk/wAQ//8IJQgA4AMhEAAA"),
        (0x00132D90, "oP69JwABv/8IAbD/EAGx/xgBsv8gAbP/KAG0/zABtf84AbTnPAG150ABtuchgIAAPAAIjtcAABEAAAAADAARjdQAIBIAAAAAkAIkjqIaCAwAAQUk"
            + "IgASPIgbUiYhmEICEAAIjiAAqK8UAAiOJACorxgACI4oAKivTEQGDCAApCegBmiSAKCIRCClgEYgAKDHQQAURgAAoedAABRGEACh5yQAoMdBABRG"
            + "BACh50AAFEYUAKHnKACgx0EAFEYIAKHnQAAURhgAoechIKADEAClJ0AApifiIgQMIAAHJMgACI4AqIhEYK2ARsA/ATwAsIFEg7UURkAAtCcAAISO"
            + "PwCAEAQAlCb8/5EQDACIjCQACTH5/yAVGAAJMff/IBEAAAAAEACAxCAAoccBAAFGAgAARhQAgsQkAKHHgRABRoIQAkYAAAJGBAAARhwAgcQBAAFG"
            + "AAiARCgAAUY0ABRG5v8ARQAAAAABoABGAgAVRgIAFkYpABVGJAAARgAABkQCAMAUAAAAAAEABiQhOMAAwACkryEoIAIBAAg8oIYINSFIAAAhUAAA"
            + "AwALPA5+BAxgAGs1wACkjwwAiIwQAAgxzv8AEQAAAACAAoDEAAiARDQIAEbJ/wBFAAAAAJgGaJIACIhEYAiARqBBATwAEIFEQwgCRgIAAUbA/wAQ"
            + "gAKA5CAAqI8wAKivJACojzQAqK8oAKiPOACor9SkhI8gAKUnAB4FDIQGTMYoAKHHNAgARgcAAUUAAAAAgMcBPAAQgUQ0AAJGAgABRQAAAAA4AKDn"
            + "BLsEDFQABCQqAEAQIaBAACEggAIwAKUnITAAAFYsBAwhOAAAIQAIPDCsCCU4AIiuDACIjgAECTwOAyk1JUAJAQwAiK5QAICuUACEJiEoIAIgbAUM"
            + "ITAgAkBBCDwgAIiuOrUEDCEggAKoBmiSAACIRCAAgEYcAIDmMACojzwAiK40AKiPQACIrjgAqI9EAIiuTACAopgGaJKASAgAIUAJAcikiY8hQAkB"
            + "SACIrrIlBAwhIIACqAZokgCgiEQgpYBGxAZAxgKlAEaYBmiSAKiIRGCtgEYgQQE8AACBREOtAEbABlWOIaAAACpAlQIWAAARAAAAAAS7BAxgAAQk"
            + "EgBAECEgQAAARBQAGwAVARIwAAAANAYAAzQGADAApSchOAAAIUAgAiFIAAAhUAAABqMARgBogEQAcIBElMQGDMarAEbp/wAQAQCUJgABv98IAbDf"
            + "EAGx3xgBst8gAbPfKAG03zABtd84AbTHPAG1x0ABtscIAOADYAG9J8D/vSciABY8iBvWJpAChI4AQAU0ohoIDDwoBQAhQMICGAYSkSAGCZEoBgqR"
            + "IACpryQAqq/8CauPJgBppSgAaqUwAAQ8+DOEJLI2CAwhKIACmGYGDAAAAAALAEAQAAAAABAAQMQUAEHEGABCxCAAQ8QAPwE8ACCBRMIYBEaAEANG"
            + "CAAAEAAAAACAAKDHAACjxgAAA0aEAKHHBACjxkAIA0aIAKLHgACjxwEAA0aEAKPHQQgDRogAo8eBEANGEACg5xQAoecYAKLnwgAARgIJAUbAGARG"
            + "AhECRgAYBEaAPwE8ABiBRDQAA0YJAABFAAAAAAAAoMYEAKHGABCARBAAoOcUAKHnGACi52FHATwAAIFEBAAARjAG1MbDoABGEACgxwIAA0YQAKDn"
            + "FAChx0IIA0YUAKHnGACix4IQA0YYAKLnPAbDxoMdFEY0BtTGOAbVxv//UCYKAAAaAAAAAMIAFEYCCRVGwBgERgIJFEZCARVGQSAFRgYYAEb2/wAQ"
            + "//8QJigAoOcsAKHnIYAAACpAEgI6AAARAAAAACgAoMcsAKHHMACg5zQAoecYAKLHOACi5xAAoseBAAJGghAWRoAAo8eAEANGAACi5xQAoseBCAJG"
            + "ghAWRoQAo8eAEANGBACi54gAoscIAKLnBLsEDGAABCQPAEAQPACiryEgQAAhKKADITCAAsigBwwwAKcnPACijyEACDwAwQglOABIrCAAqY9UAEms"
            + "JACpj1gASaxcAECsKACgxywAocfCABRGAgkVRsEYBEYCARVGQgkURkAgBUYGGABGwgAURgIJFUbBGARGAgEVRkIJFEZAIAVGKACj5ywAoefF/wAQ"
            + "AQAQJkAAvSc9igUIAgACJA=="),
        (0x00134CE0, "kP+9JzAAv/84ALD/QACx/0gAsv9QALP/WAC1/yGAgAAkAAiOHAARjZb+BAwhICACIQAIPJC7CCUYARWNLgCgEgAAAAAbAFUAEpAAACoAQBIhmAAA"
            + "lv4EDCEgIAIhQHICGwACARCYAAAQAAiOEACorxQACI4UAKivGAAIjhgAqK+APwE8AGCBRCEgIAJAAAUmEACmJzQAB4YhQGACIUigA+L9BAwgAKon"
            + "nKSLjxkADTwNZq01GFhtAW48DTxf8601IVhtAZyki68QAKCvFACgrxgAoK8AYIBEXgAEPLBshCQhKKAD0j8FDBAApif//7Um2P+gFgAAAAAwAL/f"
            + "OACw30AAsd9IALLfUACz31gAtd8IAOADcAC9Jw=="),
        (0x00134F18, "QP+9JyIAFjxoS9YmkAKEjgCABTSiGggMPCgFACG4QAAhQMICmAASkbAACZG4AAqRfAqrjyYAaaUoAGqlMQAEPHARhCSyNggMISiAAsAAwMYQAIHG"
            + "gQgARgAAoueACABGEACi5xQAgcaBCABGBACi54AIAEYUAKLnGACBxoEIAEYIAKLngAgARhgAouchIKADEAClJyAApifiIgQMIAAHJMAA1MYCpRRG"
            + "PgBAGiGAAABGpQBGIAC5JwAAKI8ZAAARAAAAABUAFBEMAAmNNAApMRAACiQRACoVWgEJhQ8AIBkAAAAAEAAAxRAAgcYBAAFGAgAARhQAAsUUAIHG"
            + "gRABRoIQAkYAAAJGNAAVRgMAAEUAAAAARgUARiGAIAPm/wAQBAA5Jx8AABIAAAAAAAARjgAAFK4UACzGFACAxgFjAEYQAC3GEACAxpLDBAxBawBG"
            + "IZhAAAS7BAxYAAQkEQBAECGAQAAQAIiOoACorxQAiI6kAKivGACIjqgAqK8hIAACoAClJyEwYAKk/QYMITggApACiI5QAAiiUQAXosL/ABD//1Im"
            + "wAC9Jz2KBQgCAAIk"),
        (0x0013CDF8, "gP+9JyIAFjyIG9YmIQASPBDoUiaQAoSOEAAFPKIaCAw8KAUAIUBCAuAACZHoAAqRaACpr2wAqq88CquPJgBppSgAaqUxAAQ8IF6EJLI2CAwhKIAC"
            + "CAFIjggACY1dACARDAAJjVsAIBEAAAAAmGYGDAAAAAAZAEAQAAAAABAAQMQUAEHEGABCxCAAQ8QAPwE8ACCBRMIYBEaAEANGwACjx0EBA0bEAKPH"
            + "gQkDRsgAo8fBEQNGAigFRsIwBkYAAANGwjgHRgAAA0aAPwE8ABiBRDQAA0YHAABFAAAAAAAApcYEAKbGADiARAIoBUbCMAZGAAADRgQAAEbwAEPG"
            + "wxgARkIpA0ZQAKXngjEDRlQApufCOQNGWACn58AAqI9AAKivxACoj0QAqK/IAKiPSACorwS7BAxgAQQkJgBAEGAAoq8AAKCvCACgr2gAqI8QAKiv"
            + "bACojxgAqK8gAKCvKACgrzAAoK84AKCvISBAACEoAAAIAUiOCAAGjQwAB41AAKgnUACpJzQAioaMPAQMIViAAmAAoo8hAAg8UIoIJTgASKz4AEiO"
            + "HABIrMikiI/0AEmOIUAJAdwASKw4AUCsPAFIJFwBSSQAAACtBAAIJf3/CRUAAAAAgAC9Jz2KBQgCAAIkIwEokgYAABEAAAAAwAAgxgAIiERgCIBG"
            + "AAABRsAAIOZSqgUIAAAAAA=="),
        (0x0013D218, "IAAIPGBkCCUMAAmNBAApMWwAIBEMAOmMEAAqMWkAQBEkACoxZwBAFXIB6oBlAEAVWgHqhGMAQBl0AeqEAgBKLWAAQBE8AuqMXgBAFQAAAAAAAMDE"
            + "EADhxAEAAUYCAABGBADBxBQA4sRBCAJGQggBRgAAAUYIAMHEGADixEEIAkZCCAFGAAABRjwAAcVCCAFGNAgARksAAUUAAAAAwP+9JzAAv/8gALD/"
            + "EACx/wAAsv8tgOAALYjAAJblBgwtIAACOgBAEAAAAABEqoiPKgAJPOi8KSUhkAAAIAAKPGBkSiVIAEPFwhgDRh4AABkAAAAAAAAsjRgAgBEAAAAA"
            + "DACNjQQArTEUAKAVAAAAABAAgMUAACHGAQABRgIAAEYUAIHFBAAixkEIAkZCCAFGAAABRhgAgcUIACLGQQgCRkIIAUYAAAFGNAADRgMAAEUAAAAA"
            + "xgAARi2QgAHgOCkl4v8AEP//CCURAEASAAAAAFggCAwtIAACDQBAFKQBESZmbAUMLSAgAi0gIAItKEACIGwFDC0wQAI4AAKOkABEhJQAWYwhIAQC"
            + "CfggAwMABSQwAL/fIACw3xAAsd8AALLfCADgA0AAvScIAOADAAAAAHIBiIAOAAAVPAKIjAkAABUAAAAAIAAJPGBkKSUMACmNAQApMQMAIBEAAAAA"
            + "BsgECAAAAACA/r0nsmAECFABsP8IAOADIRAAAA=="),
        (0x0013FD10, "wP+9JzAAv/8gALD/KACx/xAApK8hgIAAIAABPGxkIYwAASEwGQAgEHQBCIYXAAARCQAJJBUACRFaAQiGEwAAGQAAAAAhABE8QO8xJvgAJCazggcM"
            + "eAAlJgwAQBAcAAzGgAAgxgBjAEYbyQQMISAAAgYAQBAUAKKv+AAkJruCBwx4ACUm3cgEDBQApI8QAKSPMAC/3yAAsN8oALHfjEgHCEAAvSc="),
        (0x00149C60, "oP+9J1AAv/9AALD/SACx/zAApK80AKznIYCAACAAATxsZCGMAAEhMCoAIBByAQiCKAAAFVoBCIYmAAAZpAEIjiQAABEAAAAADAAIjSEAABF0AQmG"
            + "PACorzgAqa8hABE8QO8xJgMACiQFACoRBgAKJAMAKhELAAokFgAqFQAAAADwACQms4IHDEgAJSYFAEAQ8AAkJruCBwxIACUmzhoHDDwApI84AKmP"
            + "AwAKJAkAKhXoACQms4IHDEAAJSYFAEAQ6AAkJruCBwxAACUmaKwEDDwApI8wAKSPNACsx1AAv99AALDfSACx33B0BAhgAL0n"),
        (0x00166E28, "IABhhtr/ISQ0ACAQIABhhvf/ISQvACAUIgABPKQbIYyAACEwKwAgEAAAAACA/70nAAC//wgAov8QAKP/GACk/yAApf8oAKb/MACn/zgAqP9AAKn/"
            + "SACq/xJAAABQAKj/EEAAAFgAqP+QAoSOohoIDAABBSQiAAg8iBsIJSFIAgGIBiqRJgBqppAGKpEoAGqmgAYKjSwAaq5QAKjfEwAAAVgAqN8RAAAB"
            + "AAC/3wgAot8QAKPfGACk3yAApd8oAKbfMACn3zgAqN9AAKnfSACq34AAvScIAOADLABhxiIAATykGyGMAAQhMPr/IBAAAAAAgP+9JwAAv/8IAKL/"
            + "EACj/xgApP8gAKX/KACm/zAAp/84AKj/QACp/0gAqv8SQAAAUACo/xBAAABYAKj/kAKEjgIABTyiGggMPCgFACEACDxwugglIUgCAeAAKpEmAGqm"
            + "6AAqkSgAaqZQAKjfEwAAAVgAqN8RAAABAAC/3wgAot8QAKPfGACk3yAApd8oAKbfMACn3zgAqN9AAKnfSACq39D/ABCAAL0n"),
        (0x0016ACB8, "yKSIj0gAiYwqUAkBawBAEVgAi4wqYAsBYQCAFQAAAACg/r0nMAG//zgBsP9AAbH/SAGy/yGAgAAhAAk8gLspJQAAKY0hSAkBWAAJrlQACpLgOAsk"
            + "GFBLASoACzzovGslIVBLAQAAUY1GACASAAAAAEwACI4AAIhEIACARhAAAcaBCABGAACi54AIAEYQAKLnFAABxoEIAEYEAKLngAgARhQAoucYAAHG"
            + "gQgARggAoueACABGGACi5yEgoAMQAKUnIACmJ+IiBAxAAAckIACyJwAARI4rAIAQBABSJvz/kRAMAIiMNAAIMRAACST4/wkVWgGIhPb/ABkAAAAA"
            + "EACAxBAAAcYBAAFGAgAARhQAgsQUAAHGgRABRoIQAkYAAAJGBAAARhwAgcQBAAFGTAAIjgAIiERgCIBGNAABRuT/AEUAAAAAIAGkryEoIAJWAAaS"
            + "VwAHkgEACDyghgg1IUgAACFQAAAEAAs8Dn4EDEAAazUgAaSP7EwHDFQABZLU/wAQAAAAACEgAAIwAb/fOAGw30ABsd9IAbLfYAG9J8ikiI8yAAkk"
            + "GgAJARBAAAADAAAVAAAAAAgA4AMAAAAA2oYGCAAAAAA="),
        (0x0017C9D8, "0P+9JyAAv/8AAKWvBACmrwgAp69ykgcMAAAAACAAATxsZCGMAAEhMBcAIBAAAAAAIQAZPEDvOSc0ACiPEgAAEQEAKTICACAVAAAAACNACAAEAKmP"
            + "IUAoAQBECAADRAgADACorwS7BAwAAQQkBgBAECEgQAAAAKWPDACmjwgAp49ykgcMAQAIJCAAv98IAOADMAC9Jw=="),
        (0x001819A4, "sP+9J0AAv/8AAKWvBACnrwgAqa9+PwYMAAAAACAAATxsZCGMAAEhMCcAIBAAAAAAIQAZPEDvOScwACiPIgAAEQEAKTICACAVAAAAACNACAAEAKmP"
            + "IUAoAQBECAADRAgADACor8zDBAwhIAABcEMBPAAIgUQCAAFGEACg56bDBAwMAKSPcEMBPAAIgUQCAAFGFACg5xgAoK8EuwQMeAAEJAkAQBAhIEAA"
            + "AACljxAApicMAKePIUAAAAgAqY8hUAAAfj8GDCFYAABAAL/fCADgA1AAvSc="),
        (0x00191A20, "oP+9JyIAFjyIG9YmkxoIDJAChI44AHKQQABzkEgAaJBQAGmQHAqqjyYASKUoAEmlIbgAAAQAYBIAAAAAmGYGDAAAAAAhuEAAGACUxgQAYBIAAAAA"
            + "AKiARAsAABAwANbGyKSIjwMACDEDAAktCkAJAIBACAArAAk80PEpJSFIKAEAADXFEAA2xSGAAAAqQBICQQAAEQAAAABASBAA//9KJiOYKgFABgsk"
            + "GFhrAjQAjJYhYIsBAGQMAANkDABIAKyvzMMEDCEggAEoAMHGAgABRhAAgsYAEABGUACg56bDBAxIAKSPKADBxgIAAUYUAILGABAARlQAoOdYALTn"
            + "BLsEDGAABCQiAEAQTACirwAQk0SgEIBGgD4BPAAIgUSCEAFGIADMxkITFUaAPwE8AHCBRCQAz8YCEBZGJAAARgAACUQATAkAA0wJACEgQABQAKUn"
            + "SACmjyE44AIhQIAClMQGDCFQAABMAKKPGwAIPECaCCU4AEisXABJjAUAIBEAAAAADAApjQIAIBGAAAglOAAorb7/ABABABAmYAC9Jz2KBQgCAAIk"
            + "oP+9J0AAsP9IALH/UAC//yGAgAAhABE8EOgxJjQACJb8ACmOIUAJATQACKYAAKCvBACgrwgAoK8AYIBEKwAEPIjuhCQQAAUm0j8FDCEwoAMcAQiO"
            + "AACoryABCI4EAKivJAEIjggAqK8hIAACgDgEDCEooAMBAEgwHgAAEQAAAAAQAAiOEACorxQACI4UAKivGAAIjhgAqK8CAAQk2DAGDBAApSecpIiP"
            + "GQAJPA1mKTUYQAkBbjwJPF/zKTUhQAkBnKSIrwI0CAADAMYwAQDGJDAABTwoNaUkMJgIDCAApCcgAKQnsjYIDCEoAAIGAAAQAAAAANwACI7IpImP"
            + "KlAoAQkAQBUAAAAAji4HDCEgAAIhIAACQACw30gAsd9QAL/fBgAAEGAAvSdAALDfSACx31AAv98IAOADYAC9JzgAiYwIACqFDAArjSEgigAIAGAB"
            + "AwAFJA=="),
        (0x00196758, "CAEkJrOCBwycACUmFQBAEBwADMaoACDGAGMARhvJBAwhIAACDwBAEAgBJCa7ggcMnAAlJgS7BAzAAAQkCQBAECEgQAAYAKKvISgAAlzEBAykACaO"
            + "GACojxkACTzQfSklOAAJrRAApI8UAKzHMAC/3yAAsN8oALHfcHQECEAAvSfQAC2PBgCgFQQADiTYAI6sMgAOPOicziUIAOADOACOrBoADjxwrc4l"
            + "CADgAzgAjqw0AYKUkOhCJHI9BAg0AYKk"),
        (0x00196AB8, "yKSIjwoACSQaAAkBEEAAAEoAABVQAIqMSABAEQAAAAAMAEuNRQBgEQAAAABg/70nkAC//2AAsP9oALH/cACy/3gAs/+AALT/IYCAACGIYAGTGggM"
            + "kAIkjkgAcpBQAHOQIgAIPIgbCCUcAADGEAABxoEIAEYAAKLngAgARhAAoucUAAHGgQgARgQAoueACABGFACi5zQAAMUYAAHGgQgARggAoueACABG"
            + "GACi5yEgoAMQAKUnIACmJ+IiBAwQAAckIAC0JwAAhI4UAIAQBACUJvz/kRAMAIiMNAAIMRAACST4/wkVAEAJPIACiawhKCACITBAAiE4YAIBAAg8"
            + "oIYINSFIAAAhUAAAAwALPA5+BAxgAGs16/8AEAAAAAAhIAACkAC/32AAsN9oALHfcACy33gAs9+AALTfoAC9J+bDBggAAAAA"),
        (0x00197E50, "DAGjjyAAATxsZCGMAAEhMAMAIBADAAIkIQAZPHjvIo8IAOADKhBiAA=="),
        (0x00198968, "oP+9JyAAsP8oALH/MACy/zgAtOc8ALXnQAC250gAv/8hgIAAPAAIjjsAABEAAAAADAARjTgAIBIhIKAAsjYIDCEoAALMwwQMNAAkhiBBATwACIFE"
            + "AgUBRqbDBAw0ACSGIEEBPAAIgURCBQFGKAASJJykiI8ZAAk8DWYpNRhACQFuPAk8X/MpNSFACQGcpIivAyQIAMzDBAwcAKSvHAABxgIAAUYQAAHG"
            + "AAABRoAFFEamwwQMHACkjxwAAcYCAAFGFAABxgAAAUYAABVGAAC25wQAoOcYAADG8EIBPAAIgUQAAAFGCACg5xAAoK8UAKCvGACgrysABDyI7oQk"
            + "ISigAxAApifSPwUMAGCARP//UibX/0AWAAAAACAAsN8oALHfMACy3zgAtMc8ALXHQAC2x0gAv98IAOADYAC9Jw=="),
        (0x00199A60, "QP+9J7AAv/8sAMDGEACBxoEIAEYAAKLngAgARhAAoucUAIHGgQgARgQAoueACABGFACi5xgAgcaBCABGCACi54AIAEYYAKLnISCgAxAApScgAKYn"
            + "4iIEDCAAByQAAKTGBAClxiwAxsaCMQZGgDEGRiEQAAAgALknAAAojx4AABEEADkn/P8UEQAAAAAMAAmNNAApMRAACiT3/yoVAAAAABAAAMUQAIHG"
            + "AQABRhQAAsUUAIHGgRABRsIABEZCEAVGwBgBRgAIgEQ2GAFG6v8BRQAAAAACAABGghACRgAAAkY0AAZG5P8ARQAAAACGAQBG4f8AECEQAAGwAL/f"
            + "CADgA8AAvScGAAEkCQBhEAQAASQFAGEUAAAAACAAAiRkAAMk2AAirtQAI651QgQIAAAAAHJCBAgAAAAA"),
        (0x0019E210, "AEAIJCNAAgEARAgAA0QIAFwAqK/MwwQMISAAAVgAIcYCAAFGUACg56bDBAxcAKSPWAAhxgIAAUZUAKDnA6AVRgIAAUZYAKDnnKSIjxkACTwNZik1"
            + "GEAJAW48CTxf8yk1IUAJAZykiK8CRAgABAAJJBsACQEiwgQIEEAAAA=="),
        (0x001A29B0, "8P+9JwAAv/80AQiOAQAIMQwAABFoMwmOCgAgEQEABTSQAiSNUqoFDDwoBQAjAQiSIUACAQABCS3/AAokCkBJASMBCKIAAL/f5JwFCBAAvSc="),
        (0x001AE930, "sP+9JyAAv/8oALD/MACx/zgAsv8hgIAAPAAIji8AABEAAAAADAARjSwAIBIAAAAAkAIkjgIABTyiGggMPCgFABAAoq8hABI8cLpSJiGQQgIQAAiO"
            + "AACorxQACI4EAKivGAAIjggAqK8EuwQMYAAEJBoAQBAhIEAAISigA/gARpKAQAYAITDIAPAAR5KYiAYMIUAAACGAQAAhAAg8gMAIJTgACK6QAiiO"
            + "VAAIohAAqI9VAAiiAAFIklYACKIIAUiSVwAIosikiI9YAAiuMQAEPIgRhCSyNggMISgAAiAAv98oALDfMACx3zgAst8IAOADUAC9Jw=="),
        (0x001B8DA0, "HgAZPAMAABCwnzknHgAZPJC1OScgAAg8YGQIJQwACY0gACkxUAAgEcikiY9xAYqAJlAqAR8ASjFLAEAVkAMKjSNQKgEcAQuNK1BLAUYAQBVyAYqQ"
            + "RABAFTwCioxCAEAVWgGKhEAAQBlIAoqQMgBKLT0AQBGkAYqMOwBAEQAAAAAMAEqNOABAEQAAAAAMAEuNJABrMSAAazkzAGAVdAGLhJACjIwrYAwA"
            + "/f9tJQUAoBH5/20lLACAEQAAAAAqAKAVAAAAABgAQMUYAIHEAQABRgUAAEaAQgE8AAiBRDQIAEYhAAFFAAAAABAAQsUQAIHEgRABRhQAQ8UUAIHE"
            + "wRgBRoIQAkbCGANGgBADRgkAgBEAAAAAyEIBPAAIgURCCAFGNBABRhAAAEUAAAAADAAAEAAAAAAWQwE8AAiBREIIAUY0EAFGCAABRQAAAAAgAQHF"
            + "QggBRjQIAkYDAAFFAAAAAMakBAgAAAAACAAgAwAAAAA="),
        (0x001B9570, "IAAAFgQAASQiAAE8pBshjAAQITAEACAQAAAAAHAcCAwhICACwAAjjgQAASQJAGEUAAAAACIAATykGyGMgAAhMAIAIBAAAAAAAQADJJo/BAgAAAAA"
            + "BwABJPz/YRQAAAAAIgABPKQbIYwABCEw9/8gEAAAAACiNggMISAgAvP/ABDAACOODABhFAAAAAAiAAE8pBshjIAAITAFACAQAAAAAGTLBAwhICAC"
            + "AQADJMAAI66UPQQIAAAAAAcAAST8/2EUAAAAACIAATykGyGMAAQhMPf/IBAAAAAATLoGDCEgIALz/wAQwAAjjjgAiIwgAAk88GQpJQAAKo0EAEAR"
            + "IRAAAPz/SBUEACklAQACJAgA4AMAAAAA"),
        (0x001B9E68, "IQABPJC5JMQCIQRGQCEERkApBUaAIQVGIRAAACHIoAAaABg8sCgYJwAAKI8rAAARAAAAACcABBEMAAmNNAApMRAACiQjACoVWgEJhSEAIBkAAAAA"
            + "EAAAxRAAgcQBAAFGAgAARhQAAsUUAIHEgRABRoIQAkYAAAJGNAAERhUAAEUhWKAAAABsjQ0AgBEAAAAAOACNjQgAuBWQAo2NBgCgEQAAAAAMAK2N"
            + "AwCoFQAAAAADAAAQAAAFRvL/ABAEAGslNAAGRgMAAEUAAAAAhgEARiEQIAPU/wAQBAA5JwgA4AMAAAAA"),
        (0x001BAFF8, "wP+9JzAAv/8QALD/GACx/yAAsv8hgIAAIYigACGQwAAQACiOAACorxQAKI4EAKivGAAojggAqK8hIAACISigAyEwAABWLAQMITgAAAwACI4BAAg1"
            + "DAAIrsikiI8hQBIBQAAIriAACDxAdQglOAAIrjwAAK48ACiOAgAJPCRQCQFEAAquJUAJATwAKK4cACDGIEEBPAAIgUQAAAFGHAAA5jwABCYhKCAC"
            + "IGwFDCEwIAKyJQQMISAAAiEgAAIwAAU8WmIGDDh7pSQwAL/fEACw3xgAsd8gALLfCADgA0AAvScjEEgA9AFCJCAAATxsZCGMAAEhMAIKAQAGECIA"
            + "AkMHCCEQAgE="),
        (0x001C36E8, "0P+9JwAAv/8IALD/EACx/xgAsv8gALP/3ooFDAAAAAAWAAg8fCsJjZQrCo0CXAkAAzwMJE0AbBUCXAoAaiQMJEoAbBUATAkAAFQKAANUCgAhmCoB"
            + "IgAQPIgbECYMABGOgAASJiAAIBIAAAAAAABIjgQACS0ZACARIVBgAggWCyQYWAsBIVBLAUQADDxFTow1IXAAAD8ADSQAAE+NBgDsEQAAAAABAM4l"
            + "+//NFVgASiUKAAAQAAAAAAgAWCYWABkkAAAPj///OScAAE+tBAAYJ/v/IBcEAEolAABMrf//MSbg/wAQYABSJiEACDyo9gglHgAIEgAAAAAhAAk8"
            + "EOgpJRYACRIAAAAAIUAgASEACTzo5iklEQAJEgAAAAAhQCABIQAJPJC7KSUMAAkSAAAAACFAIAEhAAk8cLopJQcACRIAAAAAIUAgASEACTyQuCkl"
            + "AgAJEgAAAAAhQCABIYAAAQgAEY6//wAQEAASJgAAv98IALDfEACx3xgAst8gALPfCADgAzAAvSc="),
        (0x001C6B38, "0P+9JyAAv/8QAIDEAACg5xQAgMQEAKDnIACAxAA/ATwACIFEAgABRhgAgcQAAAFGCACg5wS7BAxUAAQkCgBAECEgQAAhKKADUAAmjlQAJ46YiAYM"
            + "AgAIJDEABDyIEYQksjYIDCEoQAAgAL/fCADgAzAAvSc="),
        (0x001CBA38, "sP+9JyAAsP8oALH/MAC05zgAv/8hgIAAMQAEPIBehCSyNggMISgAAiEACDwQ6AglEAERjQwBFMUjACAaAAAAAJykiI8ZAAk8DWYpNRhACQFuPAk8"
            + "X/MpNSFACQGcpIivAyQIAMzDBAwcAKSvAgAURhAAAcYAAAFGAACg56bDBAwcAKSPAgAURhQAAcYAAAFGBACg5xgACI4IAKivEACgrxQAoK8YAKCv"
            + "KwAEPIjuhCQhKKADEACmJ9I/BQwAYIBE3f8AEP//MSYgALDfKACx3zAAtMc4AL/fCADgA1AAvScgAGGG2v8hJAYAIBAgAGGG7P8hJAMAIBAAAAAA"
            + "iHkFCAAAAAAReQUIAAAAAA=="),
        (0x001D32D8, "kAKMjAQAjS0xAKARgGAMACIADTyIG60lIXCsAegGz40PAOARDQD4MA0AABMBALgoCwAAF/gG2I0IABkvCAAgEyHAuAH4BRiTZAAZJCPAOAMYwLgA"
            + "MgAYJxoAGQMSKAAA2AbPjRoA4BGgBfgwGAAAE8ikmI8IB9mNI8gZAwoAOS8TACAXDADvjREA4BEIB9it0P+9JwAAv/8IAKT/EACl/xgApv8gAKf/"
            + "ISDgATAABTxaYgYMMDulJAAAv98IAKTfEACl3xgApt8gAKffMAC9J/6YBQgAAAAAIQAIPLC5CCXIpImPIQAZPKS5OScAADmPIcg5ASFQAAH/fw48"
            + "///ONRAACyQAAAyNCgCEEQgAD40qwO4BAwAAEwAAAAAhcOABIVAAAf//ayX2/2AVDAAIJSFAQAEAAAStBAAFrQgA4AMIABmt"),
        (0x001E0ACC, "BACJjAYAIBEAAIiMAQAIJQAAiKwqEAkBCADgAwEAQjgAAICsnKSKjxkACzwNZms1GFBLAW48Czxf82s1IVBLAZykiq8CVAoABACrjAMAYBEhYAAA"
            + "GwBLARBgAAAAAKuMIWCLAQQAjKwIAOADIRAAAAS7BAx4AAQkEABAECEgQABAAKUnUACmJzAAqI9ASAgAIUgpAnAAKYVcAKePITjpAAA8BwADPAcA"
            + "AQAILSFIAAIhUAAAfj8GDCFYAAAwAKiPAQAIJTAAqK8DAAkp6P8gFQAAAABUwgQIAAAAAA=="),
        (0x001E8558, "IgAIPIgbCCUQAAmN4AMKJQYAIBEAAAAASABL3SYAZBH//ykl+v8AEFAASiUhAAs8qPZrJR4ACxEAAAAAIQAMPBDojCUWAAwRAAAAACFYgAEhAAw8"
            + "6OaMJREADBEAAAAAIViAASEADDyQu4wlDAAMEQAAAAAhWIABIQAMPHC6jCUHAAwRAAAAACFYgAEhAAw8kLiMJQIADBEAAAAAIViAASFAYAEMAAmN"
            + "2f8AEHAACiWO7QUIAAAAAOA4DCQYYKwAKgANPICJrSUhaKwBMAGu3SR4xAEiAOAVuDO4JQ8AGSQgAAKH//8DJAUAQxD//zkn+/8gF1AAGCcIAOAD"
            + "AAAAACEQAAAhGEIBAABjjCEIAgMAACOsBABCJCgAQyj5/2AUAAAAACgAQ4UoAAOnLAACJCEYQgEAAGOMIQgCAwAAI6wEAEIkRABDKPn/YBQAAAAA"
            + "SABD3UgAA/8lcMQBMAGu/QgA4AMAAAAAIgAWPIgb1iaQAoSOAAgFNKIaCAw8KAUAIUDCAsAFEpHIBRORvAmqjyYAUqUoAFOlkAKIjgQACS0iACAR"
            + "gEAIACG4yALIBumOEAAgEQAAAAAMADCNDQAAEgAAAADIpIiP0AXJjiFACQFAAAiutAASorUAE6IwAAQ8cHmEJLI2CAwhKAACDwAAEAAAAAAEuwQM"
            + "wAAEJAsAQBAhgEAAISBAACEogAJcxAQM0AXGjrQAEqK1ABOiyAbkJiEoAAIgbAUMITAAAj2KBQgCAAIk"),
        (0x001FF340, "IgAWPIgb1iaQAoSOACAFNKIaCAw8KAUAIZBAAJACiI4EAAktJwAgEYBACAAhuMgC+AbyrgAG047oBumOEQAgEQAAAAAMADCNDgAAEgAAAADIpIiP"
            + "IUgTAUQACa5IAADGSEIBPAAIgUQCAAFGJAAARgAACkQjUAoBQAAKrg0AABAAAAAABLsEDFAABCQNAEAQIYBAACEgQAAhKIACKoMHDCEwYALoBuQm"
            + "ISgAAiBsBQwhMAACMAAEPCh5hCSyNggMISiAAj2KBQgCAAIkUACIjC4AABEAAAAADAAJjSsAIBEAAAAAWgEqhSgAQBkAAAAASAIqkVkAi5ArYEsB"
            + "AgCAEQAAAABIAiuhyKSKj1wAi4wqYEsBHgCAFQAAAAAiAA08MEysjSFgTAFcAIys4P+9JwAAv/8IAKT/WACKkOA4CyQYUEsBKgALPOi8ayUhUEsB"
            + "AABFjVoAhpBbAIeQISAgAQEACDyghgg1IUgAACFQAAACAAs8Dn4EDEQAazUAAL/fCACk3wIAABAgAL0nTACArDT8BggAAAAA"),
        (0x00204DE0, "IxBDACAAGTxgZDknDAA4jwgAGDMFAAATMgBjJCgBOI8YAHgAEhgAAMIZAwBPhAYIIRBDACMQQwAgABk8YGQ5JwwAOI8IABgzBQAAE5YAYyQoATiP"
            + "GAB4ABIYAADCGQMAAfMGCCEQQwAjEEMAIAAZPGBkOScMADiPCAAYMwUAABNkAGMkKAE4jxgAeAASGAAAwhkDAPpvBwghEEMAIAACPGBkQiQMAEKM"
            + "CABCMAQAQBAUAAIkIAACPGBkQiQsAUKMG2kGCAAAAACUAoOAfAGKjIBIAwASRAgkBkAoAQ8ACDEkEEgBAwBAFAEAQiwIAOADAAAAALCICAgAAAAA"),
        (0x002068C8, "DACpjCQAKjEtAEAVGAAqMSsAQBE8AYgkXAGKJAAAC40nAGURBAAIJfz/ChUAAAAAOAGLjAcAbDGAYAwAIWCMADwBha0BAGslOAGLrCQBgKzg/70n"
            + "AAC//wgApP8QAKX/ISiAADEABDxoXoQksjYIDAAAAAAIAKTfEACl3wAAv98gAL0nPACLjAIAYBEhYAAADABsjcQAhozIAIeMISCgACEogAEBAAg8"
            + "oIYINSFIAAAhUAAAAwALPA5+BAhAAWs1CADgAwAAAACw/70nQACw/0gAv/8hgIAAEAAIjjAAqK8UAAiONACorxgACI44AKivISCgAzAApSeCvgQM"
            + "NAAGhiEACDwQ6AglAAEExQQBBcUAAKDHAgAERgwAocdCCAVGAAABRiQAocdBCABGJACh5wQAoMcCAARGEAChx0IIBUYAAAFGKAChx0EIAEYoAKHn"
            + "LAEEjjABBY4DAAYkMACnJyFAoAMhSAAA//8KJIb5BAwhWAAAQACw30gAv98IAOADUAC9J/D/vScAAL//UqoFDAAEBTwBAEMoCxBjAAYAQygFAAEk"
            + "ChAjACIAAzyIG2MkIRhiAAAAv98IAOADEAC9J/D/vScAAL//UqoFDAAAAAABAEMoCxBjAAYAQygFAAEkChAjAAAAv98IAOADEAC9Jw=="),
        (0x002071C0, "PACIjCkAABEAAAAADAAIjSYAABEAAAAADAAJjSAAKTEiACARAAAAAJACCY0EACotHgBAEYBICQAhAAo86OZKJSFISQEEAUuNCAEqjcikiY8jUEkB"
            + "FQBAGStYagETAGAVAAAAAJACCY2ASAkAIQAKPOjmSiUhSEkB4P+9JxAAv/8AAKCvBACgrwgAoK8AYIBEEACFJF4ABDywbIQk0j8FDCEwoAMQAL/f"
            + "IAC9JwgA4AMAAAAA4P+9JwAAsP8IALH/EAC//yGAgAAhiKAAIAAIPEB1CCU4AAiuISAAAjEABTxaYgYM0BClJDwACI4MAAARAAAAAAwACY0HACAR"
            + "RAAKjgUAQBU8ACuN/f8MPP//jDUkWGwBPAArrWZsBQw8AAQmPAAEJowgBAwCAAUkISAAAjYtBAwhKAAAAQAoMiEgAAIAALDfCACx3xAAv98DAAAR"
            + "IAC9Jxq5BAgAAAAACADgAwAAAAAiABY8iBvWJpAChI4AEAU0ohoIDDwoBQBAQAIAIUDIAuAFEpWQAoiOBAAJLR0AIBGAQAgAIbjIAtgG6Y4NACAR"
            + "AAAAAAwAMI0KAAASAAAAAMikiI8hQBIBQAAIriEgAAIwAAU8WmIGDDh7pSQNAAAQAAAAAAS7BAxQAAQkCQBAECGAQAAhIEAAISiAAv7rBgwhMEAC"
            + "2AbkJiEoAAIgbAUMITAAAj2KBQgCAAIkIxBDAJYAYyQgAAE8bGQhjAABITACCgEABhgjAA5CBwghEEMA"),
        (0x00207488, "0P+9JwAAv/8IALH/EACy/xgAs/8hiOAAjALyjGj9BgxIAvOQjAIyriEACDwwjQglOABIrFAACZJYAEmgUQAKkiIADzxoS+8lyAD4jSFY6gGgAGyR"
            + "WQBMoCtobAIKYG0CSAIsorAAbJFaAEyguABskVsATKCoAGyRgGgMACFgrAHIpI6PIWDMAUwATKwhwNgBXABYrAAAv98IALHfEACy3xgAs98IAOAD"
            + "MAC9Jw=="),
        (0x00207AB8, "yKSJj5ADya4tIAACensHDCEoAAAQAKiPEAAIrhQAqI8UAAiuGACojxgACK4UACzGFAAAxgFjAEYQAC3GEAAAxkFrAEaSwwQMAAAAADQAAqY6tQQM"
            + "LSAAAi0gAAJ6ewcMISgAADAABDwoNIQksjYIDC0oAAIJAIASdAEIhvn/CCUGAAAVOAACjpAARISUAFmMISAEAgn4IAMDAAUkPqUECAAAAAAhAAg8"
            + "qPYIJcikiY8cAQ2NVAEKjSNQSQH//0olK1BNARkAQBFaASqGFwBAGQwAKo4gAEoxFABAFbABCyWwAgwlAABqjQYAUREEAGqNCABrJfv/bBUAAAAA"
            + "CwAAEAAAAAAjUEkB//9KJStQTQEGAEARMgAKJEgCKqKEAiDGIAEBxSgAAUaEAiDmMACx31AAtMcIAOADYAC9Jw=="),
        (0x00208160, "IAAIPGBkCCXIpImPRAAKjZACCyWQAwwlIWgAACFwYAEheAAAAAB4jRIABBMEAHiNI8A4ASvICgMDACAXAAAAAAQAABAKaG0BK8j4AQtweQELeBkD"
            + "CABrJfL/bBUAAAAACmjNAQAApK0EAKmtCADgAyEQAAAjwDgBK8gKAwMAIBcBAAIkBABprSEQAAAIAOADAAAAAJACbI0EAI0tIACgEYBgDAAhAA08"
            + "6OatJSFgrAEEAa6NCAGNjcikjI8jaKwBFwCgGStwzQEVAMAVAAAAAJACbI2AYAwAIQANPOjmrSUhYKwBGAGNjWQArSVkAA4kEACijxgQTQAyAEIk"
            + "GwBOABIQAAAQAKKvGACjjxgYbQAyAGMkGwBuABIYAAAYAKOvjDwECAAAAADg/70nAACw/wgAsf8QAL//IYCAACGIoAAhAAg8sIwIJTgACK4hIAAC"
            + "MQAEPIBehCSyNggMISgAAjwACI4DAAARAAAAAGZsBQw8AAQmPAAEJowgBAwCAAUkISAAAjYtBAwhKAAAAQAoMiEgAAIAALDfCACx3xAAv98DAAAR"
            + "IAC9Jxq5BAgAAAAACADgAwAAAAA="),
        (0x002089C0, "kAKMjAQAjS0fAKARgGAMACIADTyIG60lIXCsAegGz40ZAOARAAAAAAwA740WAOARAAAAAEgA4MUEBqHFAgABRoA/ATwACIFEAAABRuD/vScAAL//"
            + "CACw/xAAtOchgIAAaAEUxgIAFEa8cAUMaAEA5mgBFOYAAL/fCACw3xAAtMcIAOADIAC9J7xwBQgAAAAA"),
        (0x00208AD0, "8P+9JwAAv//MwwQMAAAAAAUAABAAAAAA8P+9JwAAv/+mwwQMAAAAACIAATwsTCHEAgABRgAAv98IAOADEAC9Jw=="),
        (0x00208DB0, "BgABJBoAYRAEAAEkBQBhEAcAASQKAGEQAAAAADc9BAgAAAAAIgABPKQbIYyAACEw+v8gEAAAAAAPAAAQAAAAACIAATykGyGMAAQhMPP/IBAAAAAA"
            + "MgAEPDEABTwYSoQkEuoEDHARpSQxPQQIAAAAAC09BAgAAAAAGz0ECAAAAAA="),
        (0x0020ACB0, "0P+9JwAAsP8QALH/IAC//yGIgAAhgKAAMgAIPDC1CCU4ACiukAIojgMAABEAAAAAZmwFDJACJCaQAiQmjCAEDAIABSQhICACzLgEDCEoAAABABAy"
            + "BgAAEiAAv98hICACEACx3wAAsN8auQQIMAC9JxAAsd8AALDfCADgAzAAvSc="),
        (0x0020C180, "IAAZPGBkOScMADiPEAAYMxMAABMMAJiMIAAYMxAAABMNAG81DQDvOQ0A4BU4AK+MkAAuJxQBLY8AAMyNCACAEQQAziUDAI8RAQCsMfr/ABBCaA0A"
            + "AgCAERgBOI8lWHgBDn4ECAAAAACQAoSOCAAFPKIaCAw8KAUAkAKIjgQACS0RACARgEAIACEADTzo5q0lIXCoASFIogHoACqRgFgKACFQagHIpIuP"
            + "IVhqAQgBy63gACqRGAHKrTEABDxQEYQksjYIDCEogAI9igUIAgACJCMQRABkAIQkIAABPGxkIYwAASEwAgoBAAYgJADqdwcIIRBEAA=="),
        (0x0020C4C8, "IQAJPLC5KSXIpIqPEAALJAAALI0OAIYVCAAtjSpwqgELAMAVBAAtjeA4DiQYaK4BKgAOPOi8ziUhaK4BAACtjQYAoBEAAAAACAAAASEwoAH//2sl"
            + "7v9gFQwAKSUIAAABAAAAAJAChI4EAAU0ohoIDDwoBQCQAoiOBAAJLQ8AIBGAQAgAIgANPIgbrSUhcKgBIUiiAQgGKZEYB8mtyKSKjxAGq40hUEsB"
            + "KAfKrTAABDxYeYQksjYIDCEogAI9igUIAgACJOD/vScQAL//AACw/1ggCAwtgIAACABAFAAAAAAgAAg8YGQIJUAADMUhIAAAEAAFJoilBAwtMAAC"
            + "EAC/3wAAsN8IAOADIAC9JyoAATzYXSE0IwhBACwAISwDACAUAAAAAAgA4AMAAESMKgABPNhdITQjCEEAIgAEPDhMhCQhIIEACADgAwAAhIw="),
        (0x0020DA88, "4P+9JxAAv/8AAKCvBACgr0BAATwAAIFECACg5wBggEQQAIUkXgAEPJBihCTSPwUMITCgAxAAv98IAOADIAC9J+D/vScAAL//CACl/yEogAAyAAQ8"
            + "EuoEDBhKhCQhIEAACACl34A/ATwAYIFE0MkHDAAAAAAAAL/fCADgAyAAvSfg/70nAAC//wgApP8QAKX/IgAFPPAhpSSW6wQMAAAAAAMAQBQIAKTf"
            + "lusEDBAApd8AAL/fCADgAyAAvSchAAI84PxCJCAAQYzIpIOPQAJIJEAAQiQAAEmMBQAkFQgASYwjSGkAK0ghAQUAIBUAAAAAIABCJPf/SBQAAAAA"
            + "IRAAAAgA4AMAAAAAIAABPGxkIYwAASEwBwAgECEAATyQ/yKMAQBCOAMAQBCQ/yKsIngHCAAAAABgeAcI4AIijg=="),
        (0x0020E928, "AP+9J6AAv/+oALD/sACx/7gAsv/AALP/yAC0/9AAtf/YALb/4AC05+QAtectgIAALYigAC2QwAAhABU84Py1JiAAFjxgZNYmIZgAABQAoMaAQgE8"
            + "AAiBRBAAAsbBEABGAACj58AQAEYQAKPnFAACxsEQAEYEAKPnwBAARhQAo+cYAALGwRABRggAo+fAEAFGGACj5wAApCcQAKUnIACmJ+IiBAwRAAck"
            + "IAC0JxwAFcYYAKDGQK0ARgAAhI5SAIAQBACUJvz/kBAMAImMNAApMRAAKTn4/yAVWgGJhPb/IBlyAYmA9P8gFTwCiYzy/yAVOACJjJAAyiYAAEuN"
            + "7v9gEQQASiX8/2kVAAAAANE2CAwAAAAABABAEAYASZD//ykl5f8gEQAAAAAQAIDEEAAhxoEAAUYUAIDEFAAhxsEAAUYCEQJGQhkDRgAgBUYEAABG"
            + "gD8BPAAIgUQ0AAFG1v8BRQAAAAAcAIHEQAgVRkMIAEYCEQFGEACFxAAhBUZCGQFGFACGxEApBkYQAAbGgSEGRhQAB8bBKQdGEAAoxhAACcYBQglG"
            + "FAApxhQACsZBSgpGAjIIRkI6CUYAQglGAEiARDRACUa8/wBFAAAAAIIxBkbCOQdGgDEHRgQAYBIAAAAANDAURrT/AEUAAAAABjUARi2YgACQAKTn"
            + "lACl5xgAiYyt/wAQmACpr8JCCAgAAAAA"),
        (0x0020F958, "QP+9J5AAs/9wALH/YACw/yGIgACwAL//MACwJ6AAtP+AALL/EAAojjAAqK8UACiONACorxgAKI44AKivISCgAyEoAAKCvgQMNAAmhiEgoAMhKAAA"
            + "vrwEDDQBJoaZQQQIAAAAAA=="),
        (0x0020FF94, "9rQEDAAAAAAgAAE8bGQhjAABITCCCQEAIxBBACB4BwgAAAAA"),
        (0x00210058, "oP+9JyAAv/8wALD/OACx/0AAsv9IALP/LYCAAC2IAAMtkEAALZggA3EBDoImcE4BBwDOMQ0AwBUAAAAALSAAAi0oIAJKOggMLTBAAgYASJIGAAAV"
            + "BwBIkiwAABHIpImPAQAIJAYASKIMAEmuBwBIkiYAABEAAAAAEABAxhAAAcYBAAFGBACg5xQAQsYUAAHGgRABRgAAoucCAABGghACRgAAAkYcAGHG"
            + "QggBRjQIAEYWAABFAAAAABQALMYUAADGAWMARhAALcYQAADGQWsARpLDBAwAAAAANAAIhiNAAgEARAgAA0QIAAIAAQUAAAAAI0AIAABACCkEAAAR"
            + "AACsx5LDBAwEAK3HNAACpi0gAAIgAL/fMACw3zgAsd9AALLfSACz30yzBAhgAL0nIAAZPGBkOScMADiPQAAYMz4AABMAAAAAPACgEAwAmIwgABgz"
            + "OQAAFwwAuIwgABgzNgAAEy1Y4APRNggMAAAAAC34YAExAEAQBgBYkP//GCcuAAAXBwBYkCwAABMQAEDEEACBxEEAAUZCCAFGFABCxBQAg8TBEANG"
            + "whgDRkAIA0YhAA884PzvJRwA48XCGANGNBgBRgsAAEUwAS4nDADArRAAwOUUAMLlGABNjBgAza0tKMABNAAtxyD/vSdccAQIcACy/+D/vScQAL//"
            + "EACtjAAAra8UAK2MBACtrxgArYwIAK2vOACYjIgADoeMABmPISCOAC0woAMJ+CADISgAABAAv98IAOADIAC9J7quBAgAAAAA"),
        (0x00210A00, "IAAZPGBkOScMADiPQAAYMzoAABNyAZiAOAAAFzgAmIyQAC8nIXAAAAAA7Y0zAKARBADvJfz/uBUBAM4lIQAZPOD8OScMAC2P///OJQZozQEBAK0x"
            + "KQCgEVoBjIQjaIUBAGwNAANsDQAtWOAD0TYIDMikio8t+GABBQBAEAAAAAAeAKAdCABKrBwAABAAAECsGgCgGSAALo8hAAI8IP1CJAACSCQheEAA"
            + "IcAAAAAASYwNACARCABJjCNISQEraC4BCQCgEStoCQMDAKARAAAAACF4QAAhwCABIABCJPP/SBQAAAAAIRDgAQAARKwEAEykBgBApAgASqwMAECs"
            + "sP+9J1KlBAgwALP/MABgEgAAAAAQAGDGcACg55AAoceBCABGFABgxnQAoOeUAKHHwQgARhgAYMZQQQE8AAiBRAAAAUZ4AKDnHABgxgAAFUYcAAHG"
            + "QAgARkMIAEaCEAFGgACi58IYAUaEAKPniACgr9SkhI9wAKUngACmJ7iCjMeiMwUMAwAHJAAIgEQ0AAFGBgABRQAAAACAPwE8AAiBRDQIAEYKAABF"
            + "AAAAAJAAqI8QAEiulACojxQASK6YAKiPGABIrgEAAiQDAAAQBwBCoiEQAAAHAECioAC/36gAsN+wALHfuACy38AAs9/IALTf0AC139gAtt/gALTH"
            + "5AC1xwgA4AMAAb0n"),
        (0x00210CA0, "IAAZPGBkOScMADiPQAAYMzUAABMtWOAD0TYIDMikio8t+GABMABAEHQBmIQIAEqs/f8YJywAABekAZiMKgAAEwAAAAAMABiPJwAAEwAAAAAMAA+P"
            + "JADvMSAA7zkiAOAVBgBPkP7/7iUfAMARAAAAACEAGTzg/DknDADgFVoBjoQZAMAZBABNhAgALI8YaKwBZAAMJBhwzAEqcK4BEgDAFQAAAAAOAAAQ"
            + "AAAAAAwATowjcE4BRAKNjCNoTQEraK4BBACgFRAALY8rcM0BBADAFQAAAAACAA4kAwAAEAYATqQWQAgIAAAAAEyzBAgAAAAAIAAZPGBkOScMADiP"
            + "gAAYMyQAABMhABk84Pw5JyHALANEAg2TQAIujx4AoBGcpI+PHADAERkAAjwNZkI0GHjiAW48Ajxf80I0IXjiAZykj68CfA8AGwDuARAQAAASeAAA"
            + "EABAFAAAAAAbAO0BEBAAACHAAgNFAgKTgBACACEQIgNkAkWQZgJDlAIAYBADAAIkOACjrwMAohQAAAAAum8ICAAAAACMPAQIAAAAACMQQwDIAGMk"
            + "IAABPGxkIYwAASEwAgoBAAYYIwAjnwcIIRBDAA=="),
        (0x0021BEE8, "hAI/r4gCJq+MAievkAIor5QCKa+YAiqvnAIrrwEAAiSMPAQMoAIirwS7BAw0AQQkIQAZPOD8OScmAEAQISBAAIgCJo+MAiePkAIoj5QCKY+YAiqP"
            + "nAIrj6ACI48AACDFBAAhxXgCIsd8AiPHACCDRCAhgEbCGARGQgECRoIJA0ZBKQZGggEDRsIJAkaAMQdGpAIl56gCJucIACKNrAIir6QCKSeAAiKP"
            + "GBBDACFQQgEjGAMAoAIjr4w8BAwDAAUkIQAZPOD8OSegAiOP1v9gBAAAAACEAj+PCADgAwAAAAA="),
        (0x0021CA10, "AwAgFgAAAABkQwgIEAAMJIw8BAgAAAAAwAABjiIAGTyQyTknCxghA3r+Bgg4AAOuNAGChCQBgMR57EMoAwBgFIyVgcc4/0IkNAGCpAEAAUZyPQQI"
            + "JAGA5A=="),
        (0x002222C0, "IAABPGxkIYwAASEwEAAgEMACi4wBAGwk/wBtMQkArBVAAo6MAAFtMQkAoBW8Ao+MKmjPAQYAoBUAAAAAGpMICAABizX6AM4lvAKOrMACjKwIAOAD"
            + "AAAAAA=="),
        (0x00224C68, "wAKLrCZQSAF8AYqsJACJjMBAAwAhSCgB4P+9JxAAv/8AAKT/DAAkjaj/BAwhKEABAACk3xAAv99IAYL8AQACJAgA4AMgAL0n")
    };

    /// <summary>
    /// The vtable copies, in pieces of 0x80 bytes: the game's vtable at GameVa as the file has it, with these words
    /// replaced (FireTrailMaker; FireTrail: her trail's update, no touch; BladeBarrierSpell: its destructor and update;
    /// AntimagicFieldSpell: its destructor; DodgeBall: its touch, Xantam's Orbs'; FireTrail again: Inferno Arrow's flames'
    /// update, no touch; VineSpell: Entangle's vines' destructor and update; InsectPlagueSpell: Insect Plague's swarms'
    /// destructor and update, and its second piece: a swarm is a creature, whose vtable is 0xA0 bytes long, and the
    /// game's walk calls through +0x8C; poisonGasSpell: Poison Gas Arrow's clouds' update, the game's cloud in all else; the
    /// thieves' knives: Phase Axe's update, draw and touch, the knives' destructor; the game arrow: the lizardmen's
    /// spears of Vahn's update and draw, and (version 26) Karne's special knives' update and draw; BladeBarrierSpell again:
    /// Eldrith's barrier's destructor, Ilivarra's update).
    /// </summary>
    public static readonly (int Va, int GameVa, (int At, uint Word)[] Words)[] VTables =
    {
        (0x001A9A40, 0x00316718, new (int, uint)[] {  }),
        (0x001A9AC0, 0x00316798, new (int, uint)[] { (0x24, 0x00196AB8), (0x3C, 0x0012E418) }),
        (0x00208CB0, 0x0031B650, new (int, uint)[] { (0x0C, 0x00208288), (0x24, 0x0010CD50) }),
        (0x00207540, 0x0031B5D0, new (int, uint)[] { (0x0C, 0x00207274) }),
        (0x0020C100, 0x0031C3C8, new (int, uint)[] { (0x3C, 0x0012F998) }),
        (0x0020AC30, 0x00316798, new (int, uint)[] { (0x24, 0x0012A710), (0x3C, 0x0012E418) }),
        (0x00208D30, 0x00317470, new (int, uint)[] { (0x0C, 0x0010C634), (0x24, 0x001FF40C) }),
        (0x001A28B0, 0x0031B530, new (int, uint)[] { (0x0C, 0x0020ACB0), (0x24, 0x00102D80) }),
        (0x001A2930, 0x0031B5B0, new (int, uint)[] {  }),
        (0x0020C080, 0x00313140, new (int, uint)[] { (0x24, 0x0016ACB8) }),
        (0x00208A50, 0x00319CE8, new (int, uint)[] { (0x24, 0x00191BC4), (0x2C, 0x00206990), (0x3C, 0x002068C8) }),
        (0x0021C990, 0x00305888, new (int, uint)[] { (0x24, 0x0021CA40), (0x2C, 0x0020F958) }),
        (0x0019AD70, 0x00305888, new (int, uint)[] { (0x24, 0x00196808), (0x2C, 0x0020F958) }),
        (0x00197DD0, 0x0031B650, new (int, uint)[] { (0x0C, 0x00208288) })
    };

    public const int VTableSize = 0x80;

    /// <summary>The data block (in the pool's stretch 59 since version 10; every earlier version's is at <see cref="LegacyDataVa"/>; the icons of types 32-42 and Entangle's settings are in the second block, <see cref="Data2Va"/>, since version 11; Insect Plague's list entry, ability record and settings in the third, <see cref="Data3Va"/>, since version 12, Poison Gas Arrow's in the fourth, <see cref="Data4Va"/>, since version 13, Dimension Door's in the fifth, <see cref="Data5Va"/>, since version 14, Expert Focus's in the sixth, <see cref="Data6Va"/>, since version 15, Phase Axe's in the seventh, <see cref="Data7Va"/>, since version 16, Shatter's in the eighth, <see cref="Data8Va"/>, since version 17: the mask at <see cref="SpellsAt"/> has their bits, but the counts and entries here are the other nine's).</summary>
    public const int DataVa = 0x00221B88, LegacyDataVa = 0x00132D90, DataSize = 0x738, VersionAt = 4, CountAt = 0xC, AbilityCountAt = 0x10,
                     SpellsAt = 0x1C, EntriesAt = 0x80, EntryStride = 0x60, AbilitiesAt = 0x3E0,
                     AbilityStride = 0x50, EntryPriceAt = 0x4A, EntryRanksAt = 0x49, AbilityManaAt = 0x2C, AbilityDamageAt = 0x26,
                     XantamDamageMinAt = 0x48, XantamDamageMaxAt = 0x50, BarrierDamageMinAt = 0x5C0,
                     BarrierDamageMaxAt = 0x5C8, FieldFramesAt = 0x5E0, MightDamageAt = 0x5F0,
                     MightDefenceAt = 0x5F8, PowerHitsAt = 0x608, PowerFramesAt = 0x610,
                     OrbsCountAt = 0x618, OrbsDamageMinAt = 0x620, OrbsDamageMaxAt = 0x628,
                     OrbsSpeedAt = 0x630, OrbsFanAt = 0x634, OrbsApexAt = 0x63C,
                     InfernoIconAt = 0x668, InfernoManaAt = 0x680, InfernoDamageMinAt = 0x688,
                     InfernoDamageMaxAt = 0x690, InfernoBurnAt = 0x698, InfernoBlastAt = 0x6A0,
                     InfernoSpreadAt = 0x6A8, InfernoFlameMinAt = 0x6B0, InfernoFlameMaxAt = 0x6B8,
                     CastTypes = 42, GameCastTypes = 23;

    /// <summary>
    /// The second data block (version 11: the start of the pool's stretch 60, marked "FEA2" and the version): the names
    /// of the icons of types 32-36 and their fallbacks, Entangle's settings by rank (its vines, how much slower a wrapped monster
    /// walks: the game's slow byte, half the percentage; how long a vine holds it, in tenths of a second; its thorns' least and
    /// most damage), its fixed values, then (run time) the icons' textures. The same whichever spells are on, but for
    /// Entangle's settings.
    /// </summary>
    public const int Data2Va = 0x00224B68, Data2Size = 0x100, EntangleVinesAt = 0x98, EntangleSlowAt = 0xA0,
                     EntangleTimeAt = 0xA8, EntangleThornMinAt = 0xB0, EntangleThornMaxAt = 0xB8;

    public const string Data2Mark = "FEA2";

    /// <summary>
    /// Entangle's fixed values (the second block has them, from the template): its vines' monsters within 240 of him, the
    /// vines crawling 200 a second (the game's: 100), a thorn every 50 frames (a second).
    /// </summary>
    public const float EntangleRange = 240f, EntangleSpeed = 200f;
    public const int EntanglePeriod = 50;

    /// <summary>The second data block with Entangle's defaults (its run-time textures zero).</summary>
    public const string Data2CommonBase64 =
        "RkVBMhoAAABpY29uX2JsYWRlYmFycmllci50ZXgAAABpY29uX2FudGltYWdpY2ZpZWxkLnRleABpY29uX3JpZ2h0ZW91c21pZ2h0LnRleABpY29u"
        + "X3hhbnRhbW9yYnMudGV4AAAAAABpY29uX2VudGFuZ2xlLnRleAAAAAAAAAAgqjAA8KkwAHiqMADwqDAAQKkwAAAAAAAAAQIDBAUAAAAPERQWGQAA"
        + "ACgtMjc8AAAAAgMEBQYAAAAEBQYHCAAAAABwQwAAAEAyAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAA==";

    /// <summary>
    /// The third data block (version 12: the start of the pool's stretch 47, marked "FEA3" and the version): how many
    /// list entries and ability records it has (Insect Plague's: 1 and 1 when it is on, else 0), its list entry ({class, 0, entry})
    /// and ability record, the names of its icon and its fallback, its settings by rank (how many swarms; how long a swarm lasts, in
    /// tenths of a second; a sting's least and most damage), its fixed values, then (run time) 16 poison records. The levelling-up
    /// lists and the grant read its entry and record after the main block's.
    /// </summary>
    public const int Data3Va = 0x0020B890, Data3Size = 0x1E0, Data3CountAt = 0x8, Data3AbilityCountAt = 0xC,
                     Data3EntryAt = 0x10, Data3AbilityAt = 0x70, InsectSwarmsAt = 0xE0,
                     InsectTimeAt = 0xE8, InsectStingMinAt = 0xF0, InsectStingMaxAt = 0xF8;

    public const string Data3Mark = "FEA3";

    /// <summary>
    /// Insect Plague's fixed values (the third block has them, from the template): its swarms' monsters within 240 of him (and
    /// of a swarm whose monster has died), a swarm following at 150 (the shaman's: 100) after 25 frames of
    /// gathering, a sting every 50 frames (a second), 5 seconds of poison a sting (1 for a monster that resists
    /// it), a poison record kept 260 frames.
    /// </summary>
    public const float InsectRange = 240f, InsectSpeed = 150f, InsectPoison = 5f;
    public const int InsectGather = 25, InsectPeriod = 50, InsectKeep = 260;

    /// <summary>The third data block with Insect Plague off (no entry or record) and its defaults (its poison records zero).</summary>
    public const string Data3CommonBase64 =
        "RkVBMxoAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAaWNvbl9pbnNlY3RwbGFndWUudGV4AAAAKKkwAAAAAAAAAQIDBAUAAABQUFBQUAAAAAEBAQEBAAAAAwMD"
        + "AwMAAAAAcEMAABZDGQAAADIAAAAAAKBABAEAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

    /// <summary>
    /// The fourth data block (version 13: in the pool's stretch 47, after the third, marked "FEA4" and the version),
    /// laid out as the third: how many list entries and ability records it has (Poison Gas Arrow's: 1 and 1 when it is on, else 0),
    /// its list entry ({class, 0, entry}) and ability record, the names of its icon and its fallback, its settings by rank (the
    /// arrow's least and most damage; the cloud's reach; how long it lasts, in tenths of a second; its least and most damage once
    /// a second), then the frames between the cloud's hits. The levelling-up lists and the grant read its entry and record after
    /// the third block's.
    /// </summary>
    public const int Data4Va = 0x0020BA70, Data4Size = 0x120, GasArrowMinAt = 0xE0, GasArrowMaxAt = 0xE8,
                     GasReachAt = 0xF0, GasTimeAt = 0xF8, GasMinAt = 0x100, GasMaxAt = 0x108,
                     GasPeriodAt = 0x110;

    public const string Data4Mark = "FEA4";

    /// <summary>
    /// Poison Gas Arrow's fixed values: the cloud hits every 50 frames (a second; the fourth block has it, from the
    /// template), with the game's own cloud's kind of hit (0x40040: the hit function poisons, as the monsters' gas does);
    /// its reach is at least 10 (the game's constructor's least).
    /// </summary>
    public const int GasPeriod = 50, GasKind = 0x40040;

    /// <summary>The fourth data block with Poison Gas Arrow off (no entry or record) and its defaults.</summary>
    public const string Data4CommonBase64 =
        "RkVBNBoAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAaWNvbl9wb2lzb25nYXNhcnJvdy50ZXgAQKkwAAAAAAAACAkKCwwAAAAMDg8REgAAACgtMjc8AAAAKC0y"
        + "NzwAAAACAgMDBAAAAAMEBAUGAAAyAAAAAAAAAAAAAAAAAAAA";

    /// <summary>
    /// The fifth data block (version 14: in the pool's stretch 47, after the fourth, marked "FEA5" and the version),
    /// laid out as the third: how many list entries and ability records it has (Dimension Door's: 1 and 1 when it is on, else 0),
    /// its list entry ({class, 0, entry}) and ability record, the names of its icon and its fallback, its distances by rank (u16s
    /// at <see cref="DoorDistanceAt"/> + 2 x rank; its entry offers 3 ranks, and ranks 4 and 5 hold rank 3's), its HUD line's key,
    /// then its fixed values (floats: the least way worth going, how much nearer each try is, how far above and below her feet
    /// the floor may be, how far short of a wall she stops; then the sparks of each burst). The levelling-up lists and the grant
    /// read its entry and record after the fourth block's.
    /// </summary>
    public const int Data5Va = 0x0020BB90, Data5Size = 0x120, DoorDistanceAt = 0xE0, DoorHudKeyAt = 0xF0,
                     DoorMinAt = 0x104, DoorSparksAt = 0x118;

    public const string Data5Mark = "FEA5";

    /// <summary>
    /// Dimension Door's fixed values (the fifth block has them, from the template): she goes nowhere when less than 16 of the
    /// way is left; a floor more than 12 above her feet or 32 below brings her 8 nearer a try; she stops
    /// 2 short of a wall; 50 of Ilivarra's sparks a burst. Its entry offers 3 ranks;
    /// a distance is 20 to 400 (featpatch.py's settings).
    /// </summary>
    public const float DoorMin = 16f, DoorStep = 8f, DoorUp = 12f, DoorDown = 32f, DoorBack = 2f;
    public const int DoorSparks = 50, DoorRanks = 3, DoorDistanceMin = 20, DoorDistanceMax = 400;

    /// <summary>The fifth data block with Dimension Door off (no entry or record) and its defaults.</summary>
    public const string Data5CommonBase64 =
        "RkVBNRoAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAaWNvbl9kaW1lbnNpb25kb29yLnRleAAA0KowAAAAAAAAAGQAlgDIAMgAyAAAAAAAZGltZW5zaW9uZG9v"
        + "cl9odWQAAAAAAIBBAAAAQQAAQEEAAABCAAAAQDIAAAAAAAAA";

    /// <summary>
    /// The sixth data block (version 15: the start of the pool's stretch 51, marked "FEA6" and the version), laid out
    /// as the third: how many list entries and ability records it has (Expert Focus's: 1 and 1 when it is on, else 0), its list
    /// entry ({class, 0, entry}) and ability record, the names of its icon and its fallback, its settings by rank (bytes: the
    /// percentage more damage; how long the focus lasts, in tenths of a second), its HUD line's key, the longest focus in frames
    /// (a focus ending further off than that is from before a frame-count reset), then (run time) for each of four players the
    /// frame his focus ends and its percentage. The levelling-up lists and the grant read its entry and record after the fifth
    /// block's.
    /// </summary>
    public const int Data6Va = 0x0020E6E8, Data6Size = 0x128, FocusPercentAt = 0xE0, FocusTimeAt = 0xE8,
                     FocusHudKeyAt = 0xF0, FocusLongestAt = 0x104, FocusUntilAt = 0x108,
                     FocusNowAt = 0x118;

    public const string Data6Mark = "FEA6";

    /// <summary>
    /// Expert Focus's fixed values: the longest focus the settings allow (1275 frames: 25.5 s; the sixth block has it, from
    /// the template); a percentage is 1 to 200; while a focus lasts each arrow's least and most damage are raised by it, rounded
    /// ((x x (100 + p) + 50) / 100), at the shot's three calls of the arrow constructor.
    /// </summary>
    public const int FocusLongest = 1275, FocusPercentMin = 1, FocusPercentMax = 200;

    /// <summary>The sixth data block with Expert Focus off (no entry or record) and its defaults (its run-time fields zero).</summary>
    public const string Data6CommonBase64 =
        "RkVBNhoAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAaWNvbl9leHBlcnRmb2N1cy50ZXgAAAAAeKowAAAAAAAAMjIyMjIAAAAyS2R9lgAAZXhwZXJ0Zm9jdXNf"
        + "aHVkAAAAAAD7BAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";

    /// <summary>
    /// The seventh data block (version 16: in the pool's stretch 51, after the sixth, marked "FEA7" and the version),
    /// laid out as the third: how many list entries and ability records it has (Phase Axe's: 1 and 1 when it is on, else 0), its
    /// list entry ({class, 0, entry}) and ability record (its damage, at <see cref="AbilityDamageAt"/>, the first rank's: the HUD's
    /// line until the first cast), the names of its icon and its fallback, its least and most damage by rank (bytes at
    /// <see cref="AxeDamageMinAt"/> and <see cref="AxeDamageMaxAt"/> + rank), then the axe's fixed values (its speed, how many
    /// frames it flies, its radius, its spin a frame, the point of the model it turns about, its weapon-table entry, the ring of
    /// sparks it vanishes in and how many). The levelling-up lists and the grant read its entry and record after the sixth block's.
    /// </summary>
    public const int Data7Va = 0x0020E810, Data7Size = 0x118, AxeDamageMinAt = 0xE0, AxeDamageMaxAt = 0xE8,
                     AxeSpeedAt = 0xF0, AxeLifeAt = 0xF4, AxeModelAt = 0x108;

    public const string Data7Mark = "FEA7";

    /// <summary>
    /// Phase Axe's fixed values (the seventh block has them, from the template): 300 a second for 67 frames (400
    /// units), a circle of 16, the game's axe4 (the weapon table's entry at 0x002A7A28: its model and texture, loaded
    /// with every level), a ring of 24 sparks when it vanishes; a damage is 0 to 255, each hit's least no more than its most.
    /// </summary>
    public const float AxeSpeed = 300f, AxeRange = 400f;
    public const int AxeLife = 67, AxeWeaponVa = 0x002A7A28, AxeSparks = 24;

    /// <summary>The seventh data block with Phase Axe off (no entry or record) and its defaults.</summary>
    public const string Data7CommonBase64 =
        "RkVBNxoAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAaWNvbl9waGFzZWF4ZS50ZXgAAAAAAAAAkKowAAAAAAAACAoMDhAAAAAMDxIVGAAAAACWQ0MAAAAAAIBB"
        + "wBIAAAAAAAAAABhBKHoqAAAAgEEYAAAAAAAAAA==";

    /// <summary>
    /// The eighth data block (version 17: the start of the pool's stretch 52, marked "FEA8" and the version), laid out
    /// as the third: how many list entries and ability records it has (Shatter's: 1 and 1 when it is on, else 0), its list entry
    /// ({class, 0, entry}) and ability record (its damage, at <see cref="AbilityDamageAt"/>, the first rank's), the names of its
    /// icon and its fallback, its least and most damage, reach, freeze and cooldown by rank (bytes at <see cref="ShatterDamageMinAt"/>,
    /// <see cref="ShatterDamageMaxAt"/>, <see cref="ShatterReachAt"/>, <see cref="ShatterFreezeAt"/> and <see cref="ShatterCooldownAt"/>
    /// + rank; the freeze and the cooldown in tenths of a second), its HUD line's key, then the ring's fixed values (the longest
    /// hold, a frozen monster's chill, where the ring starts and how high, its speed and its most, the angle between particles,
    /// the particle type's sizes, colours, life, blending and picture), then (run time, zero in the file) the frame the last hold
    /// ends, the particle type and the hold records from <see cref="ShatterRunTimeAt"/>. The levelling-up lists and the grant read
    /// its entry and record after the seventh block's.
    /// </summary>
    public const int Data8Va = 0x0020F6A8, Data8Size = 0x2B0, ShatterDamageMinAt = 0xE0,
                     ShatterDamageMaxAt = 0xE8, ShatterReachAt = 0xF0, ShatterFreezeAt = 0xF8,
                     ShatterCooldownAt = 0x100, ShatterRunTimeAt = 0x154;

    public const string Data8Mark = "FEA8";

    /// <summary>
    /// Shatter's limits: a reach of 10 to 255 (its ring's look goes out to 214.75 at most), a freeze of 0.1 to 25.5
    /// seconds, a cooldown of 0 to 25.5 seconds (in tenths); a damage is 0 to 255, the least no more than the most. A monster
    /// that resists cold or magic is frozen half as long; one immune to cold, to magic or to all damage isn't frozen.
    /// </summary>
    public const int ShatterReachMin = 10, ShatterReachMax = 255, ShatterFreezeMin = 1, ShatterFreezeMax = 255, ShatterCooldownMin = 0,
                     ShatterCooldownMax = 255, ShatterHolds = 32;

    /// <summary>The eighth data block with Shatter off (no entry or record) and its defaults (its run-time fields zero).</summary>
    public const string Data8CommonBase64 =
        "RkVBOBoAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAaWNvbl9zaGF0dGVyLnRleAAAAAAAAAAA2KkwAAAAAAAAAQIDBAUAAAADBAUGCAAAADxGUFpkAAAACg8U"
        + "GR4AAAAUGR4jKAAAc2hhdHRlcl9odWQAAAAAAAAAAAD7BAAAAAAAQAAAgEEAAMBBiYgIPwAA1EIAAgAAAADAQQAAQEKMvvCgoNL/AAEAAAABAAAA"
        + "CQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAA==";

    /// <summary>
    /// The ninth data block (since version 18, the whole of the pool's stretch 38 since version 21, marked
    /// "FEA9" and the version): Improved Enemy AI's. At <see cref="EnemyAiOnAt"/> 1 when it is on, at
    /// <see cref="EnemyAiPartsAt"/> its parts (<see cref="EnemyAiTargeting"/>, <see cref="EnemyAiSurround"/>,
    /// <see cref="EnemyAiAlarm"/>, <see cref="EnemyAiCasters"/>, <see cref="EnemyAiBrutes"/>, <see cref="EnemyAiDrow"/>,
    /// <see cref="EnemyAiRegroup"/>, <see cref="EnemyAiArchers"/>, <see cref="EnemyAiBosses"/>: all nine,
    /// <see cref="EnemyAiParts"/>; WorldExplorer's three cards put in four, four and one, featpatch.py can put any);
    /// then its numbers, the eight directions round a hero, the vtables of the 32 kinds of monster it changes,
    /// the brutes, the drow's blink and the casters' waits, the same whether it is on or off; then (run time, zero in the file)
    /// from <see cref="EnemyAiRunTimeAt"/>. Its hooks are in <see cref="Hooks"/> with the mask <see cref="EnemyAiMask"/>.
    /// </summary>
    public const int Data9Va = 0x00206460, Data9Size = 0x3A0, EnemyAiOnAt = 0x8,
                     EnemyAiPartsAt = 0xC, EnemyAiRunTimeAt = 0x130;

    public const string Data9Mark = "FEA9";

    public const int EnemyAiTargeting = 0x1, EnemyAiSurround = 0x2, EnemyAiAlarm = 0x4,
                     EnemyAiCasters = 0x8, EnemyAiBrutes = 0x10, EnemyAiDrow = 0x20,
                     EnemyAiRegroup = 0x40, EnemyAiArchers = 0x80, EnemyAiBosses = 0x100,
                     EnemyAiParts = 0x1FF, EnemyAiMask = 0x8000;

    /// <summary>The ninth data block with Improved Enemy AI off (its run-time fields zero).</summary>
    public const string Data9CommonBase64 =
        "RkVBORoAAAAAAAAAAAAAAGQAAAAAABBASOH6PgAA8EEAAEhDAADAQQAAQEEAAKBBAABAQgAAAEAyAAAAAADIQwAAyEP0AQAAAAB6RAAAAAAAAIA/"
        + "AAAAAPQENT/0BDU/AAAAAAAAgD/0BDW/9AQ1PwAAgL8AAAAA9AQ1v/QENb8AAAAAAACAv/QENT/0BDW/gIMxABCGMQB4iDEA2JAxAIB9MQAIvTEA"
        + "cLcxAECuMQBQujEAwCIxAIAlMQBgkzEACEkxAHgrMQBgvzEA4DIxAEAyMQCQdjEAKEExAIA+MQDoijEAkDcxADA7MQD4SzEAqEwxAGiNMQBonTEA"
        + "iHkxALh/MQAIyDEAIEUxAPiXMQAAAAAAFwQcSgAQIADIAAAAAADhQwAANENAAAAACgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAA==";

    /// <summary>
    /// The tenth data block (since version 22, at the start of the pool's stretch 53, marked "FEAA" and the
    /// version): the wounded monsters' numbers (40% of their health, the kinds, 200 frames back, a friend within
    /// 400, 24 behind it, 16 from their place, a record stale after 1500 frames), the same
    /// whether Improved Enemy AI is on or off, with (run time, zero in the file) their 16 records; then (since version 23)
    /// the archers' numbers: one shot in 4 special, each shooter's list, the arrows' kinds and Hail's turn, then (run
    /// time) Hail's keeping; then (since version 25, run time) the bosses' word: 1 while Ilivarra raises her second skeleton.
    /// The block is 0x2B4 bytes. <see cref="Data10RunTime"/>: the run-time stretches (start, end).
    /// </summary>
    public const int Data10Va = 0x0020FCE0, Data10Size = 0x2B4;

    /// <summary>The tenth block's run-time stretches (start, end): zero in the file.</summary>
    public static readonly (int Start, int End)[] Data10RunTime = { (0x40, 0x240), (0x284, 0x2B0), (0x2B0, 0x2B4) };

    public const string Data10Mark = "FEAA";

    /// <summary>The tenth data block as the patch writes it.</summary>
    public const string Data10Base64 =
        "RkVBQRoAAAAoAAAACMALJMgAAAAAAMhDAADAQQAAgEHcBQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAABAAAAAEAAAAAAAAA"
        + "AwABAwAAAAAEAAECBAAAAAIAAQAAAAAAAQAgAAUAAAQGAIAAAwAAAAIAAAC+FHs/wsVHPgAIAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAA=";

    /// <summary>
    /// The eleventh data block (since version 26, at the start of the second pool's stretch 91, marked
    /// "FEAB" and the version): the bosses' numbers (Karne's special knives and his fan, Xantam's volleys,
    /// Sessth's spells and clouds, the dragon's ring of frost, Eldrith's field and barrier), the same whether Improved Enemy
    /// AI is on or off, then (run time, zero in the file) Karne's throw's keeping and the bosses' waits. The block is
    /// 0x110 bytes. <see cref="Data11RunTime"/>: the run-time stretches (start, end).
    /// </summary>
    public const int Data11Va = 0x0020EF40, Data11Size = 0x110;

    /// <summary>The eleventh block's run-time stretches (start, end): zero in the file.</summary>
    public static readonly (int Start, int End)[] Data11RunTime = { (0xB0, 0x110) };

    public const string Data11Mark = "FEAB";

    /// <summary>The eleventh data block as the patch writes it.</summary>
    public const string Data11Base64 =
        "RkVBQhoAAAAAAAAAAAAAAAIAAAAACAAAvhR7P8LFRz4BACAABQAABAYAgAAAAAAAABAAAAAQAAAFAAAAAAAAAJYAAACWAAAA+gAAAPoAAAD6AAAA"
        + "KAAAAAAAcEMAAPBBDwAAABkAAAAIAAAAEgAAAAAAYPCgDwAALAEAACwBAAAAANxCAwAAAAYAAABABAAA7gIAAPQBAAAsAQAAWAIAAJABAAD0AQAA"
        + "AABwQgAAoEEAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAA=";

    /// <summary>
    /// The orbs' fan (the data block has them, from the template): 200 a second, 12 degrees between neighbouring
    /// orbs (OrbsFanAt holds the cos and sin of half of it), the fan's point 32 behind her hand.
    /// </summary>
    public const float OrbsSpeed = 200f, OrbsFanDegrees = 12f, OrbsApex = 32f;

    /// <summary>The cast table (42 types: the game's 23 cases, then the default but for the spells that are on): a piece of the pool of its own since version 8.</summary>
    public const int CastTableVa = 0x00207A10;

    /// <summary>Power Attack's time: its doubled hits must come within this many frames of the cast (10 s).</summary>
    public const int PowerFrames = 500;

    /// <summary>
    /// The game's Inferno Arrow record (the one its grant copies: type 9, bit 8): its name, cast type and feat bit are checked,
    /// and that no other ability has its type or bit. The patch puts the mana and the rank's damage into its slot at every
    /// shot (its record's 1 mana and 1-6 damage are never used).
    /// </summary>
    public const int InfernoRecordVa = 0x0030DA20;
    public const string InfernoRecordName = "infernoarrow";

    /// <summary>Inferno Arrow's flames: how many lines of them spread out, all round, and the floor's search round the impact.</summary>
    public const int InfernoMakers = 8;

    /// <summary>The game's Power Attack record (the one its grant copies): its name, cast type and feat bit are checked.</summary>
    public const int PowerRecordVa = 0x0030E2E0;
    public const string PowerRecordName = "powerattack";

    /// <summary>
    /// The game's Sorcerer's Blood record (type 24: its grant only sets the bit): its name, cast type and feat bit are checked, and
    /// that no other ability has its bit. Each level she gains with the feat adds her rank to a total in her record, at
    /// <see cref="SorcerersTotalAt"/> (its last rank byte), and her maximum mana is the game's plus that total.
    /// </summary>
    public const int SorcerersRecordVa = 0x0030E3D0, SorcerersTotalAt = 0x123;
    public const string SorcerersRecordName = "SorcerersBlood";

    /// <summary>The game's own cast table (its first 23 entries go into the patch's) and its default case.</summary>
    public const int GameCastTableVa = 0x00307BC0;
    public const uint CastDefault = 0x001628F0;

    /// <summary>
    /// The data block without what depends on the spells that are on (their count and mask, their list entries and ability
    /// records): every spell's own settings at their defaults, the names of the icons, the HUD lines' keys.
    /// </summary>
    public const string DataCommonBase64 =
        "RkVBVBoAAAAAAAAAAAAAAAAAAAA4BwAA6DYcAAAAAACamZk/AAAAQAAAwEEAALRDAIA7RQAAAEIAAwMFBQUAAAAAAAAAAQAAAAEBAgIDAAAAAgME"
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
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAABAYICgwAAAAICg0QFAAA9AEAADIAAAAAAKBBAAAgQQAAyAD6ACwB"
        + "XgGQAQAAAAAAChQeKDIAAAAKFB4oMgAA9AEAAJqZmT4AAQIDBAUAAPQBAAAAAAAAAAECAwQFAAAACAgICAgAAAAMDAwMDAAAAABIQ/2Yfj8FE9Y9"
        + "AAAAQmFudGltYWdpY2ZpZWxkX2h1ZAAAcmlnaHRlb3VzbWlnaHRfaHVkAABpY29uX2luZmVybm9hcnJvdy50ZXgAAAAAAEBBAADAQQAKDA4QEgAA"
        + "AA8SFRgbAAAAFBkeIygAAABAREhMUAAAADA2PEJIAAAAAQECAgMAAAACAwMEBQAACAAAAA50WjsAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

    /// <summary>The spells, in the order the data block lists the ones that are on (the order of BorrowedSpell).</summary>
    public static readonly SpellPatchData[] Spells =
    {
        new("xantam", 0x1, 1, 26, 10, 0x00191A20, "xantamfire", "icon_xantamfire.tex",
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
        new("powerattack", 0x10, 2, 34, 18, 0x0020C52C, "powerattack", "icon_powerattack.tex",
            "AgAAAAAAAABwb3dlcmF0dGFja19uYW1lAAAAAAAAAAAAAAAAAAAAAHBvd2VyYXR0YWNrX2Rlc2MAAAAAAAAAAAAAAAAAAAAAAAUFAAAAAAAAAAAA"
            + "BAAAAAAAAAAAAAAA",
            "",
            ""),
        new("bladebarrier", 0x2, 2, 43, 32, 0x001E86C0, "bladebarrier", "icon_bladebarrier.tex",
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
        new("antimagic", 0x4, 2, 44, 33, 0x00207324, "antimagicfield", "icon_antimagicfield.tex",
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
        new("righteousmight", 0x8, 2, 45, 34, 0x001FF340, "righteousmight", "icon_righteousmight.tex",
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
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=="),
        new("sorcerersblood", 0x20, 1, 32, 24, 0x00000000, "SorcerersBlood", "",
            "AQAAAAAAAABTb3JjZXJlcnNCbG9vZAAAAAAAAAAAAAAAAAAAAAAAAFNvcmNlcmVyc0Jsb29kRGVzYwAAAAAAAAAAAAAAAAAAAAEDAAAAAAAAAAAA"
            + "AQAAAAAAAAAAAAAA",
            "",
            ""),
        new("xantamorbs", 0x40, 1, 46, 35, 0x00133154, "xantamorbs", "icon_xantamorbs.tex",
            "AQAAAAAAAAB4YW50YW1vcmJzX25hbWUAAAAAAAAAAAAAAAAAAAAAAHhhbnRhbW9yYnNfZGVzYwAAAAAAAAAAAAAAAAAAAAAAAAUMAAAAAAAAAAAA"
            + "AEAAAAAAAAAAAAAA",
            "eGFudGFtb3JicwAAAAAAAAAAAAAAAAAAAAAAAAAAAAAjAAAAAwAIAAwAAAAAACBB/wB0ADgAAAD+AegAcACg/wAAAAAAAAAAAAAAAABAAAA=",
            "LAAsABEA1wBEAAAAwmQWAIAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAEgAAAAAAAEA4AAAAAAAAAAAAAAAAAAQBQAAAAAAAAAAAAAAAAAAAA"
            + "UQAAAAAAAAAQAAAAEAAAAFIAAAAAAAAAAAAAAAAAAABTAAAAAAAAAECAAAAAAAAIAAAAAAAAAAAAAAAA/30qZv9uHgj+0JV//28eSf90Iyr/bh4J"
            + "/8l/I/9vHkH/cB8N/24eAv9vH03/diMK/J1Qf/94JUf/kTpI/24eAf9uHlX/ijRc/24eCv9uHgP+p0N//+Gif/91I2fsYxp//3YkBO1xLX//bh5G"
            + "/28fOf9zIgT/cSEW/28eJP9uHhT0bB1//24eN//YnX//bh4C+4Aof/95JjX+bh5Z/5hFLP+EMw7/eykn/+izf/NoGn//cSAe/9B+f/+CLlf/bx4B"
            + "+Xcjf/9vHz7/bx4u/9eOf/9yIBP/fSle/24eBP9uHg3/dSIC/3EgFP9uHhf/bx8H/28fIP99LQL/bx4b/3AfCP/MeX//bh4C/28eL/9yIAf/8Mh/"
            + "+Y9If/+PQQX/bx9F/24eP/9uHgj/hS8u/7NnBf2rYX//bh4K/3ssBv9vHir/xHgS/24eCuBhJH//bx88/24eBf+6U3//jzom/6FUHf9uHgn/bh4G"
            + "2lccdNpSFXb/bh4W/28fAv96JhX/dSQB4FcXdP9uHgz8hyx//28fBf/21X//wHU+/3AgBP9vHxL/bx8Jz0oTfv9uHgX/bh4F/3QhFf97KiPyaB9x"
            + "/24eCd9XGG//dyUH/JdCf/+CLTj/hTYC9nswf/+UPTT/cB8+/24eC/9xIQH7dCR0/38sEv9uHgnZURV16V4abP5wIGb/w3gH3lYWf/9uHgn/bh4G"
            + "/24eE/97KC73aR1X/3MhD/+DLz/8kDN//3UjWP9uHgH/bh4K/sNvf/9vHw//cyJA/3cmEP+NN1L/cSEM+WodW/91Iw31cSB//3EgR/9vHgz/bh4E"
            + "/28eMv9uHgr/cB8H/3EgGf96JkD/bx9d/8Fhf/6qS3/9nTl//3MjHP9xIWH/bh4E/24eD/9xIAL/bh4K/34qTf97KGz/iz0M/24eD/9yITH/bh4Y"
            + "2FAVc/9xIAr/bh4G/24eEf9wIRz/cCAD/28fBv9uHgz/kjo+/3AfU/+FMCDxZBtl/28eNf+aTQP/bh4H/28fAf+kVwX/rWEN/24ePv9wIAL/kj0d"
            + "/3IhCf9uHhX/ijUr/24eBvdzIH/4ah1g/3YkFv/aln//bh4D/28fA9VOFHz/cB8B/28eIf9uHgf/bh4B/4IuF+pfGH//gzQl/24eBf9yIgH/+d5/"
            + "/3QiIv9vHgT/bx8C/siHf/9vHhDZUhR//24eAv97KBn/bx9E004WePxzI2/5ikF//24eCv/RhX//cSAS/3IhBdhXHX73bhx//28eUP9uHg7/bx86"
            + "/28fKP91JgP/cyIm/24eEP9uHhL/jzoV000Ufv9uHjD/bx8M/28fGdxVGHnYUhdz/24eHf9vHyz/bh4C1lIYfP9vHib/eSYeAYAAAAAAABAOAAAA"
            + "AAAAAAAAAAAAAAEAUAAAAAAAAAADgAAAAAAAEA4AAAAAAAAAGAAAABcAAABSAAAAAAAAAAAAAAAAAAAAUQAAAAAAAAAAAAAAAAAAAFMAAAAAAAAA"
            + "ioAAAAAAAAgAAAAAAAAAAADKeqUAKNelAD6+WgBtCL0AACRtAADfzwAAJELXAAzU1OpZyKXFgz10tpU1dD+Vo1pZlfq9eYM9AvSDyFKmUsgC61QA"
            + "vRS/AISzPgDOB74AzuAAV6XLAKXV6wDUR1gA/ADKYM4A18+9ALzbAgC/EnkAAN8+AADU1XrKpdRgyi+lcz3pTL0fqvUCSOq7Bu1w7Xk4cCJiquf1"
            + "9F3nTLQ/kMGQIEkAi3w+AHlmEgBoZvwAAkgAB4QfAElk+gDAwLYAv/zKAjHC/FYxgdRifL8M6Ym8GZktvEmlXT6czu++xL09u/7iohDtaaEbKrqg"
            + "uPO4liC6ukHvcmlByDTilkgl4hX6bgDpuf0AYli6AAb0BAC9GxxWAPUFUgBDNWoA/l1HAEn8YiAH1KY7By/qP/DwXfpts63IvDHJH1Q4Wf6B6bBI"
            + "E5pjTtH1JUHiBI4O3p/xRT/iNGUfzCllQyUlRUyiYwMFVQDqzRoAOODiAFbquAC94nj0AIz7VgAbyAIAIraEAKU9/qFN7SuhPFC7axS69W6JWsHs"
            + "LeVRIe/0wWM9xVGiELJOJQR7QSm4/Q4hpIJFgtbxRTTMd0UpJQ8zJaKy3CUVhuoAGnY4AOLvSgC4hb0ATOIA9PuMAErIGwAC7yIAhHT2EzPJTNFF"
            + "eSe3RZfW1txdvezQPXnWovuqfk4q77osnwGi4iekDWGfpE66gICgn3dGlnfs8aBhNONV4iUBa9YaMOkA7O9iALqFfQAEpr0AIrgAeQWIAAI1mgCE"
            + "XfYAziuoMw/hLUV3n/8z+OyG3OzJkdDsmIqi1oUKTlw1Liy6E7jz8wQQE7qPuwTWBPsQGOT2E47xSBPQgJrzGJNM8/1mugCEWBsAc1gmANWz6ABe"
            + "9e9XAJrqagD2YvAAZn1nALVfDbEWuQ0WLm+Sj3evKY+cr+xQEZEhEB6eY/XZFqK7HCsJabv+CRi7PfOSHPobTv09UTa6/lAj87sTIxATuFU7GgDH"
            + "qmkA2jjhAKyo+wBeTPpqAJqFVwAFOFcATwb3ADDw2c5vZzXOxqw1hDLa+loy6/qZU+s/L1MU73QyarbOXdlcXMVsjlwgF6JpXYqWjD+pMzf+QEWT"
            + "HItF0bh1A2nk3WQAW7AZABu99wD7ZMIA+lAAVOpDAL9iyAAoAjsAyuAoqPzgCHnUuZR5DMPfrmfLGa78iTmu/I3PYvyL13n8eW/j7a5v3kM4U477"
            + "qgWS/p3wDQX2jQ1ITOBr+7jGbkP9MVcAh8BHACJJPgAfB74AOz0AB6bvAGBKqgDPx40AegAACM/KAF56ygAdvsoAFBIAAMrfAADK3wC+ytQAz8ov"
            + "Xy+qronOqjiQhOk4i720OHwCERFPaPQRMLDd7rl5qnmllQCB1FkAvPcCAM9JvQCUroRgAOXObQBSZ4EAx7+BAAAAOT4AAGrPAAAx2wAAwyQAAADU"
            + "ACgApQBgAHQAPtdaHcm9XayDvTvVm4S22mKE7+v0me+J6U2dFOpaHhkghNIkXwDA93wA1VSJANRUiwD8zuV6AKUCAADUnAAA1EkAAAA+bVYA98Ji"
            + "AEkMtMoZpYXKVKXOvlSEhG2Bvb28VGgCeRD2zPTzSIKq0ftyxbcrtzu2+3I/PUiCPfb2zB+aHxg9H7MAo09kAODDZwDD3awAWIwAYgcEAAZm9QC9"
            + "ZisAZAAd95d6B6XqCAelxcpJzu/Kvr3Awl4Cs1ReC41UrDiL6ScEocV+ExU/3rhryPIJbkjIuCXtSBOSIiIbolEbUaEitRQAKzK/AP6jwAD6IMAA"
            + "tvEApjr5AHkHuAACMVEAhLOl77vAxz8QlXE9E2KLHwn85/SM1O+qJ9QfOrhzKx7zYRMzDYLzM6ApJyM2koCW00hhDQ4igqAj4Snm5ieSDlX15ACq"
            + "+0sAOD1pAH078wC9RvWqAEv7OABpPQYA87a9ADFa+3+JUivs6pjtGF1wmtDUPz8ppfbIKakc/hhq8/b91n/YJRik2JIlpEWiog8jDRzjTg0Q+OZr"
            + "84IzkqTyZSkq8QCmK/kAecgJAALvUQCEVbXqABoyOADiq0oACSC9AL21GEHlUGM26Z8Nlrb4oBUfPDYYIu4sY/M63A1+PVWgGuHsE/h4ggSyTMwb"
            + "slEYUbIazBCy+IJQ45/sBPgTXBPROzgAE+d9AEwxvQBDs84AxiIAs/9DAG1d/gBXqjAAGb0mKSl5saLQcC6gGD+yDuz7QGV3EJDY7NGdZRjs6CPQ"
            + "FZonXPFDn1zjmp9ppBykjJN/n1C4uCfzuBC40QkiE2kWtuUAkapSAG84xwBmBi8AZgQAyV0cAGSqhgAdOHYAHRdP0S2KuZ/nsf8JcLG5E3CNT5Pq"
            + "0rku6jLgpKqvdaTpH+kNYfa0oRia6RWS4YVuThDv0UGa/tYs9vWO5h+4klU/GgDw6mkAPjgQAD59SAB6/T9aAIeqmQBMedUA9r3bAIZ8+60mw0jJ"
            + "bOv+vWx8HwK5FCYCb8S1Am9EtQJvrUMC+gIzAf6DZXci7mXW8+cjGD01XI4/BY6O756iGO83lv36ugC8hRAAv2L7APcC+gCURoWpAFs4ZwAEAh0A"
            + "K6nCADEA1NdqANQ5GQDUah3K1HynAKXK2wClykIApcokAJkIZBSeecREiHmQRFAL/5wEaFPDEzyvwxNSF8sbWZGNTJua1EkAH0I+AJ2/ygCJgcoA"
            + "MQIAzzG9AMrwTQAAZy8AAGAAymrPAMrr1wDKwwgA/DGUAPwAygASAMoAXgDKAB05FF6dpV9CnaVPEj+lbyQ/pcYU+tR2GfrUozk/pZ2ntqUgvgcA"
            + "3W1JAJhUgQBZv4EAqdQAAGTUAACs/AAAB98AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=="),
        new("infernoarrow", 0x80, 0, 8, 9, 0x00000000, "infernoarrow", "icon_infernoarrow.tex",
            "AAAAAAAAAABJbmZlcm5vQXJyb3cAAAAAAAAAAAAAAAAAAAAAAAAAAEluZmVybm9BcnJvd0Rlc2MAAAAAAAAAAAAAAAAAAAAAAAUMAAAAAAAAAQAA"
            + "AAAAAAAAAAAAAAAA",
            "",
            "LAAsABEA1wBEAAAAwmQWAIAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAEgAAAAAAAEA4AAAAAAAAAAAAAAAAAAQBQAAAAAAAAAAAAAAAAAAAA"
            + "UQAAAAAAAAAQAAAAEAAAAFIAAAAAAAAAAAAAAAAAAABTAAAAAAAAAECAAAAAAAAIAAAAAAAAAAAAAAAA+MFKf5AfBhD/7KB/kIJhf/95IB//eB4B"
            + "jB4GBc1lGXr/eB4D/3geA4kdBgL/eyIj/+mZf/94Hg6flYZ/niUIIqwvCUD8125/++CIf4t1PFbxpjh/6Igkf/7Rd392bmN/8tB5f//soH+SeTtj"
            + "+stZf//soH/ZwYN/99N5f3xqMnuGHAYE6IQhf74+C2mYTRtqkoV0f//soH/JSg12+cZQf6UpCCrERg1r7dmSf/62Yn+9PwxT2q9af/94HgbWXRgY"
            + "/3siI8JCDWT923t//+uef7I0Cjn8121/lCEHGsNHDV7CrIN/fGoyc6grCEe2NQph75osf6QuChD813B//+uff/99JCz/eR8VdmQuHv7olX//7KB/"
            + "/+qcf4BHPGDCuax//3geAfKqOn/bZxV+nCQHGpkiBxT/eB4K/nceEo4fBhL/7KB/pSgIPP/rnX/71Wl//+ygf/jBTn//eB4G+cdUf4VuTX//7KB/"
            + "r552f/rLVn+8Ogpv/tqDf9PCqn/+6Jd//6xdY6QpCDT0rjt/5X4ff4EaBQGyMwlN+nUdAvKmM3//eB4Bf2w0f3ZjLxT/p1dd33QbfYaBeX//7KB/"
            + "/3geCu+dMX//eB4G/+qaf//soH+IHQYK/NFuf//soH/piyZ//Omef//soH92Yy0r/+ygf89TEHt/bDR9/eKJf+uQKX92ZC4M+MJSf/94HhF6aDFz"
            + "zHEkEO6WKX/PVxFy/3geCv3dfH/94YR//d+Bf/94Hgb+44t/fRkFAf99JCz/7KB/4GYgav/soH+ARjw48qs9f/3jin//eB4C/6xdY/vVb39/SjpF"
            + "/+ygf/+0Z2qLeDxr/uaSf4weBgZpUy1//+ygf/i7T398ajJ9/uSNf5UhBxN8ajJ5/eeUf//rnn/3vkh//6dXXfvTbn//fSQsgUw8fP/rnX//7KB/"
            + "/30kLP/soH9/ZCwErXQtf8JBDHXxozJ//3geCrc3ClutkWJ/9rpFf/94HgbxoC9/vDwLY+6SJ39/Sjtu/+ygf3xqMgyDGwUCghsFA/3ih3/+5Y5/"
            + "f0s6fIQbBQL/7KB/3W4Zfe6bL3/PxLV/gEY8DfnIWn//eB4Rj2gyaYseBgn/7KB//3geBY15PGKDGwUBjx8GB7ExCVaQHwYNtDYKQopvPW2SIAcO"
            + "x0cNdP3qn3/sjiN/9bRAf/+0Z2r/7KB/+s1Yf//soH+aIwcW/3geBd5tF3/GqFx/4nIYf/94Hg7/7KB/jB4GAvrOX3/83Hl/zMW6f89VEXWAGgUC"
            + "5GMZDf+CKjj82XR//4IqONJTD37/eB4C675Tf9WAJX/+6Zh//uWQf//soH/jdxt//+ygf9dhFHz/651//+ygf/rRaX+poJJ/AYAAAAAAABAOAAAA"
            + "AAAAAAAAAAAAAAEAUAAAAAAAAAADgAAAAAAAEA4AAAAAAAAAGAAAABcAAABSAAAAAAAAAAAAAAAAAAAAUQAAAAAAAAAAAAAAAAAAAFMAAAAAAAAA"
            + "ioAAAAAAAAgAAAAAAAAAAADrAAAAEQAAABEAAADrAAAAAAAGAAAAAAAAAAAABgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA0gAA"
            + "AL0AAADEAAAADAAAAAAAAAAAAAAAAAAAAAAAAAAFAAAA9wAAAOkAAAAFAAAAAACQAAAAZwDrAAAAkAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAvQAAALQAAABHAAAAiwAAAAAA0tIAAADSAAAAAHMAAOlKAGSbVQBM3QAArOkAAGYAAChlAADMABEAzADUCMsAAAAAAAAAAAAAIQAA"
            + "ANYAAAAAAAAAAABDAAAApQAAAJy/gQAAAAAAAAAAAAAAAAAAAAAAAHMAAACEAAAAnAAAAJC8ACKD+wBM1AgATvbAAPsAADhUAACsAAYAgAASMS0A"
            + "AMoAAAAvAAAAMgAAAPtzQwBiAKUAAAAbAAAAhQAAAFYA4gAAAHEAAAAAAAAAAAAAOgAAAJwAAACBAAAAAAAAAAGCx2Rj+T9d+6vDqzzqUu0ILTW4"
            + "xWQhtQChAOcID82YrvUAAPt7Qwa6fqUGYwzJBjjyhanK77cAAGmPAADOYQC3AAAAaAAAAAYAAAAAAAAAAAYAAAAAAAAAAAAAAAAAAA5eeFiwWJO8"
            + "Aay1qnCGghj7KPROO4kva0g0SANmNCq1aX6iAPGqDAABq6kAdV0GADEe4mYA70KeAD23AHsktwBCAAAA4gAAAAYAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAEDo7nenXORvRDAYp/IKdjNdRJNZq4hvJYAmGv8reqjR49EAIWPzAAC48wAAhhcAABhbfQDkNlQAclMAAKoxAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAKNMHkBslRjC/UZ2pgOgduS6RYg2+deaHiJ3dzm4+Ppb7DkAuy55AMvQiAAAZmAAAP8cEwAXvAAAWeoAAKrqAAAAAAA3"
            + "AAAAjgAAAAAAAAAAAAAAAAAAAAAAjgAAAE8AALygk6YwqEadknoz+OdAM3f9h3l/eg6NTXyCSXeNnfgafAnHAP0h4QDnAAAAkgAAAN/9AADcLAAA"
            + "uvEAAHB1AAAAAHQAAAApAAAANwAAAAAAAAAAALkAAAApAAAAdAAAALDctZ3o/rZAwqN3piwsmpiKAGxGeqSNwviu55WVdfjCfGXwAB0AAABvAAAA"
            + "kQAAAGOMAAB1xwAAOK4AADHhAAAAACkAAAAWAAAAjgAAAAAAAAAAAI4AAAAWAAAAFAAAAC8AUwBKAH0AuwAjAH0AOwAAAOMAAABkAAAAZAAhAPkA"
            + "AAAAAAAAAAAAAAAAAAAAAC8AAAAhAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAASAAAAG0AwQDBAG0AAAAAAEgA"
            + "AAAvAAAALwAAAC8AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAANIAAABHAAAAmQAAAIEAAAAFEgAA6QYA"
            + "BukAABIFAACQAAAt1AAA+4PrAPuQkAAIAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAIsAAAAAAAAAAAAAAAAAi3MAAMSEAAC0nAAA"
            + "vb8AAADrVwAAET4AERHYAIM+AADpIQAQ3coAAd0AAND3BgAtAAAAAAAAAAAAAABzAAAAOgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAHMAAACEAAAA"
            + "z5wAAHOBAAAAAAAAAAAAAADcnnUAsACYAIYA7jFTALB9PIAyZqQ/1gf1UsAAMSpi2PtDewCspSAAuMnJALqpqQA1AFbFZQCzMQAAjzsAAGEAQQAA"
            + "AK0AAAAGAAAAAAAABgAAAAAAAAAAAAAAAAAAACGHLQ0A3ABEAEwApwjxZdPcPWT7eNnoSq476EovIw77NdMbDGWjKKkA3JcAADizAC8HAHGuAADI"
            + "+XtDrT2ipa0AyAAAAFEAAAAAAAAAAAAAQgAAAOIAAAAGAAAAAAAAACgVTuCJoEsENKCgdjSnlXZuU3AscOr7bOMiU7bl8Z8dC3ACAH/xAACAWAAA"
            + "CXUAAHsYAC1+nwDw1aoAAAj5AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAGJ/8QQqiEYlvFq+JQ9aiP9gWJEak3ABd/GsWfiwAXZ8"
            + "BCUnAH9ZVQCqEAAAcjEAAKrzAADj8wAADvMAAOVQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAOdwrxXGXDnaevJfb3z8X2yRqPig"
            + "kXfvlZPgk28Vk6/48nwAzmMdABN93gAAzMMAAFBMCAAXsGUAW4YAADPHAAAAAACxAAAAFAAAAGoAAAAAAAAAAABqAAAAFAAAAHQAANe4GZ36Ch4m"
            + "fMIefECn+PpjbKOn6CbDf0Z8M/hsYB/4TnoAAD3GAABk7wAA29wAAHz0+wAaMVUA3gcAAMIAAAAAAABPjgAAjuYAAAApAAAAAAApAAAAFgAAjo4A"
            + "AJQAAKT1jTuubWtiZC++L/Fib21gAMJm5wBGrr4Adzz6AKDM7gAAAP4AAAB4AAAAuwAAANhlAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAsvUAAJGW2ACRAAmW6AAyAP714wChwYYAacGhAMfBMgAAAEoAAADNAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=="),
        new("entangle", 0x100, 0, 47, 36, 0x00134F18, "entangle", "icon_entangle.tex",
            "AAAAAAAAAABlbnRhbmdsZV9uYW1lAAAAAAAAAAAAAAAAAAAAAAAAAGVudGFuZ2xlX2Rlc2MAAAAAAAAAAAAAAAAAAAAAAAAAAAUMAAAAAAAAAAAA"
            + "AIAAAAAAAAAAAAAA",
            "ZW50YW5nbGUAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAkAGQAAQACAAQAAAAAAEBBRgDcADwAAACMAJABZACg/wAAAAAAAAAAAAAAAACAAAA=",
            "LAAsABEA1wBEAAAAwmQWAIAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAEgAAAAAAAEA4AAAAAAAAAAAAAAAAAAQBQAAAAAAAAAAAAAAAAAAAA"
            + "UQAAAAAAAAAQAAAAEAAAAFIAAAAAAAAAAAAAAAAAAABTAAAAAAAAAECAAAAAAAAIAAAAAAAAAAAAAAAAduxYCWF5Pn9WljgKnaBlXof7aQRu5lAS"
            + "iZBWSDZ1KkRu5lABbuZQC3vxXQZu5lAHNm8lf0AuHn+L/m0DbuZQBnPrVQ1u5lAEmPh7BHLpVAFu5lACS4kyf33LTn9u5lABLF8hf4z/bgYuZCVN"
            + "KVkff3m1TX8uYiJ/buZQCCVSHmIzVCNqbuZQFSJKG3lu5lAQbuZQAnLDRn/E/Kg/c6RLfzh5LDxNOSR/Zbw6f4n9awZZqjR/b+dRHW7mUAVu5lAH"
            + "cOhSCHDoUgRBhzA3b+ZRBG7mUAImUR5nQC4efytbHzqJiFd8d+dWKW7mUAZKkC1/buZQB4fxaiBu5lAcQoEtfyNPHWxu5lAObuZQCFGcMHxv51ED"
            + "detXEXrwXAJIjSxybuZQIG7mUBRv51ECTIEzf6eea3Zu5lAbVUEpf0AvHnpZlTp/aNtMHm7mUAcrWyJdbuZQETNqJH87dCh/uLB4ennIS382cSZ/"
            + "buZQB3TnVhhblzt/sv2WJEaGLGBu5lAMbuZQF27mUBpu5lANQC4ef27mUAEuVyJHbuZQBDllJ34nUx57Yr5EGG7mUAlgaTl9Wkcrf0AuHn9QhzV/"
            + "buZQBG/nURQmUx9bcelTCl2bPX9Eei5/fstPf4z/bgEzWSJ/balGf1WSOH9u5lADcOhSBXh0SGZglz5/du1YAkKLMSxz6lUVO3oof27mUANu5lAI"
            + "buZQB2abQn9x6FMPb+dREWekQn8oWCFPXn46N2RKL38jRBp+b+ZRDmHLRyZu5lACbuZQAnSwS39u5lACbuZQBW7mUAFu5lAWWo87f27mUAhu5lAK"
            + "jPFvAW7mUA1u5lAGbuZQAW7mUAFu5lAEg/hlCSlaIVNbmTx/buZQD4P3ZQFu5lAKbuZQG27mUBmL/W0OLF8iTm7mUAFaoj0wbuZQBFmQOBwmUx50"
            + "REQkUmJRL39OjTN/b+dREzl2J39v5lEFXcNEI2XHRgxnTTF/buZQCYn8awF2eUdUculUA0JcKndu5lABQTAfbm7mUAw/bCt/TV0qGD59Kn9wrEh/"
            + "buZQAW7mUAFALx5/VIs4f1BWK0RTrj0lbuZQA27mUA4zMxx/TKE5LbO/fV6BzVJ/buZQF27mUAJv51EXbuZQAUAuHn8jQhttbuZQGG7mUAdu5lAY"
            + "KFIdf0IxH39u5lACMWomRG7mUAFv51EN1MmMf2nTShQpWiFQTGoyf3/0YQOF+WcBfaVRf2ZMMH83cyZ/buZQA779oQhv51EEffNfCnZ/SnxGhS9/"
            + "buZQHX7zYBNv51EBcOdSCUKDKXhALh5/WKA3a2uUSH8kUR5VbuZQApOvYH9LdS4nQzsif1KRNX9EXScvYZ4/f3+7UX84TyJ/AYAAAAAAABAOAAAA"
            + "AAAAAAAAAAAAAAEAUAAAAAAAAAADgAAAAAAAEA4AAAAAAAAAGAAAABcAAABSAAAAAAAAAAAAAAAAAAAAUQAAAAAAAAAAAAAAAAAAAFMAAAAAAAAA"
            + "ioAAAAAAAAgAAAAAAAAAAADJAAgAewBoAGgA0gAIAHsAAADPAAAASwDFAEsAxeMo4zql2EcEuAa735hCCrwIowgIjHApEnsg903J1WthxVHFpQAA"
            + "xZgAAMWMAADFewAAxU0ADQAkALIAowAAAGMAAADPxPwApbJjAGPbpQBNjSgAAL0GEcVlUhHJGCKdaBi1RbanUXxdzqcBGSI/92pNUT6lY2onTbhY"
            + "k6eMhqdRjCCMBwAAjPwAAIxgAABoKAAAe9UAaIitANvLUQC9EbMAAELJBDlSaFYEVpgiBFGjo9/FyCvlyVGhwowgoe64flhWfgZRyOIioXJ99lS6"
            + "fS79B2ORIS5SHPT9P9UODhvOQUErEABbVk8ALFJOAFNgtgDoqa2FAHo/gwCRUiUAAmBlACLFdy72e0EuGwhRK8cSyCHJBu3DaGJ4589RHuFjEP1i"
            + "keCxJAJfkRKzWVRNlzxVciR3GwQiGaH49tXVekzOUdku9gAqUfYAu8j2AAyjVgAA1o64AARNCgC8YO8AeoQYAMSdpyOIy6dse1P2XQjztvSlucMB"
            + "QgDnYwYAoVJhAKdK+cdNXYAOYxVETAZq1z9WpyYrLo4Vp4+tK2JEQafcNf0140cA7At/APmwAABguQAApaoAAD0JAACs8wAAf6AAAAAaCT+emqog"
            + "xAtigsmwoYp7b0k3jG9suBQacULpGlVSB8AG4gYCBgRWtqcHapOhk/31Sc2rcV1t4StEHWIrNUBfC+QAgCcAAJknAACMsAAAe8EAGokDAG/jgwAA"
            + "NIkAAAAATcXxAM7EJwCAiV5vI2cAF7BbAG8TEgAAKXIAAL/QlbpKwt8jUnF0QKNVDh2lOUCRHV+Kwg5Qek7nRBlwLl8aOAAAGkUAAAB/AAAARwAA"
            + "CEcAAHsAbwDbAG8ADABvAG8Aa8mwAN4D5AAG6gBv2FkAuRhZAAB/MwAAEzMAxAFEHG0kDpH+Eh2Kes/19YJoM9EcWte8bS7XjxyTM8BOukQX+QAA"
            + "AFsAAADyAAAAUwAAi+gAAGUAAAB/AAAAuQAAAAAAAx0AjZj9ANJjggAIKyMAAEQSAAAzcr0AM1XBACblbZLc31THce1YbFqK9B5PSVhy+yv4TU/Q"
            + "eiRxBNnOgFhSXgAA014AAHOwAAA3BQAAJecA5EfuAAAFUgAApGMAAAC9wf4Ai1VAAHsHTgDP7mYAAOdjZQAdUtsA/bZoAIIOHM3VtTH9yBwieoD9"
            + "zhy2w5L5wrdU/B3Qj9D9/BzQ5/xqAQAAXJ8AAEbbAABrDAAArIIApLC2ALlHjgAAAEYAAN0AeY3EAHe9ewDins8AV2UAAAMAAAC3AAAAlQAAAEB/"
            + "/0f6JJET/yQO84ZC2QGiEq8S5pivRq8Ir1yUe69NdcmEyQAAn8kAAIjJAADFyQAAgsUAAJXFAAC3ywAAtQAAAAAA2nsRADJ7ewAyaIzFV3sAxb7S"
            + "AMXMiwCIzNsAe8zbdZ/MUXWYzFF1YHb2Vwbrzlc+VwZ11jJjdQQyuHU62rgDpQDE0qUAAMW4AAAAmAAAFggAAGRoAABkewAAvi0AAAAGYFXFp2PR"
            + "xUpjYslMYAZoxa5UCHuY/ZgICP2lEozlmGxU1UL4fsAiWHGRNiHV7bPT9m05UiJtMPZScQ5qyDGnatIAp/bdAMiOjQBSQgAAJDkAH2D/ALSYbQAl"
            + "jA4AZcXIP3nJUfYeey72MQh+B2KlyQffo2hSfVKYo3TOoxLZEpGzIAYc38NW1QJAK/a2ancGUVGrIvZRavYQ9lEutvbAoQgAwPaDAFWOvQDfQgAA"
            + "ujoAQ8hfAGckxwDvpYIAGCIA5ZD2xMJpMclqP3BoVi7JCMd5aLh09ZhjsyBjo1izeX3fBhxS5aPVK/uOzkRx0Qb+TR6ATqN5ENy1bkFiBA/2bgCk"
            + "yJEAfyT8AACZzwAAl4ycABCfRQAQu+gAl+MAAEIAiglSAE6QYmXc2FHEpzHLyVEeyXtRioxnUQK4AbbfNZPuUhwihsgrVqmAp+HZHHJ+TYpf9WBA"
            + "D2pNHFk/p9UkjgAAYIcAAENrAAB88gAArdu7AI6efwBSNAwABhoAAG8AAd5vsBIGbwsG2OSapysAANwF3QAeoH8Aw/OwAKkBtnitjtXfLiRWMMel"
            + "3B2Kz1Qjwg5wwljnIcNVQfb9BND5GgAA6W8AAAEAAACgAAAAsGgAACfbAAAn6AAA8aQAAAAX8HwAGjvpbwCllhcAJLWwAMgAXgCtZScAtlPxAPUq"
            + "HXlAQg6KHaVxQA7PsRUVaNkEapVY32KAOeIGuuJOY9aYuRoAjAAaAMkAbwCNAG8A5IkAALDoAADxRwAAb+QAAG8APNC5AFC1AABfTQAA3NEAAMW2"
            + "ABgD9QCNsalvA+oVgqsz1R70M4DZVVm2bFUmgnE5HcN69Kl5/noCgkkcNbYM0QAAAKoAAAC/AAAAFAAAX7QAACzjAAC7BQAADAAAAAAAM7gAAG4H"
            + "ZQDX350AJvQAAHtJAL0IHQCL/MIAjDVaOZIz1TCOM3KpBkT5lc6At23hGQIOd0AOgmyG/ngcWUDvHAAADIAAAOSBAAC59wAAo14AAJleAACcDAAA"
            + "8AAAAAAAYKgAnVKvAHshrwAIx6+eAP6+3QCpzHsA59TPAO51UnXVdWN0Itok4kLMp/9y+uGvk69sr+KvXa+Rrx6vea+CAxcAINK5AI7FAACHxQAA"
            + "9/8AAPLUAABHxgAABb4AAAAAY8wAESLMAIi2zABoDsyNAP4DDQD1t2gATkjPANTMMszazDLM2swyzMwvMszM65XM1MyRzBnM+8x5zBzMecyCtAAA"
            + "tg0AAMrFAABGAAAA6RYAAJ9IAACItwAA3cEAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=="),
        new("insectplague", 0x200, 0, 48, 37, 0x00102910, "insectplague", "icon_insectplague.tex",
            "AAAAAAAAAABpbnNlY3RwbGFndWVfbmFtZQAAAAAAAAAAAAAAAAAAAGluc2VjdHBsYWd1ZV9kZXNjAAAAAAAAAAAAAAAAAAAAAAUMAAAAAAAAAAAA"
            + "AAABAAAAAAAAAAAA",
            "aW5zZWN0cGxhZ3VlAAAAAAAAAAAAAAAAAAAAAAAAAAAlAGQAAQABAAMAAAAAAHBBqgCMACgAAABUARgBUACg/wAAAAAAAAAAAAAAAAAAAQA=",
            "LAAsABEA1wBEAAAAwmQWAIAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAEgAAAAAAAEA4AAAAAAAAAAAAAAAAAAQBQAAAAAAAAAAAAAAAAAAAA"
            + "UQAAAAAAAAAQAAAAEAAAAFIAAAAAAAAAAAAAAAAAAABTAAAAAAAAAECAAAAAAAAIAAAAAAAAAAAAAAAA3LMif7DOQAoPCwh/6LwifygoEnEgGg9/"
            + "sM5ABtft+kewzkATru9MDrHPQgawzkAJ5fT9X9bs+kYYEw1/sM5AHLDOQB2wzkAKsM5AB7DOQA282HxAsM5ANzgtEX+wzkAXsM5AO7DOQCSryD48"
            + "sM5AE7DOQAmwzkAPy++SBb7ZhEGwzkASobs7Osjht0SwzkAPZHInEBoXDjJVRRd/sM5AAbDQQAqwzkAzFhIMbNbs+kawzkAzsM5AAbDOQDiwzkA2"
            + "sM5AMtbs+kbR6OM0QDQXfLDOQAUaFQ1/sM5AHJGnQ0WwzkABsM5ANLDOQAawzkAZsM5AH9T5ky6wzkAKsM5ANZixOEFmbGtnT1E1aLDOQAQlIyJ/"
            + "sM5AMbDOQAGwzkA0IiERCa7XQguwzkAsODwYZ7DOQDCwzkAWrOVEAhURDDywzkAh1uz6RuDw9lmwzkAQsNBAArDOQA6wzkAgGxgTfculIH+rxz46"
            + "sM5ANbDOQCKwzkAcsM5AI7DOQAKwzkA2NTc1fbDOQAFfaiZWuNRnPraTHX/n9f1isM5ALLDOQC+wzkANGhQMf7DOQAGwzkAnsM5AKX5nGn8pKRM+"
            + "sM5AE6rwRgJPPyR+sM5AObDOQAGwzkA4FBAMf7DOQCewzkAwsM5AMLDWRALB3JRDsM5AK7DOQAExJBN/sM5AKMXZ5UywzkAHMTEVbbDOQAdndStR"
            + "oLo7E6/TQR2wzkABQ0kbX7DOQAqwzkAH6dVof2BiI2DQ59tKsM5AIrDOQBewzkAn1ez3TLDOQBSwzkABtdJbO7bFzlewz0ACsM5AA7DOQBWwzkAB"
            + "LScQeYSYMUZ1hi1IsM5AHLDOQCiwzkAbs9FQOzQmFH8yJhV/sM5AKLDOQCSwzkAEIR0UearwRgSwzkAxsM5AC7DOQASwzkAUsM5AG7DOQAqwzkAN"
            + "sc9EOZGoNUI0NS5xFhIMcuzNS3+wzkAMsM5AN7DOQAiwzkADsM5AF7DOQAPqwCl/0unlSLDOQAawzkAEq+1FCBoYDnmwzkAFsM5ABdbs+kywzkAE"
            + "sM5AC6vHPjmr7EUEsM5AGrDOQA5KPBN/r9FBDLDPQAWwzkA0sM5AAbDOQBEUEAxGsM5ALrDOQCbF36hFsM5AA6rvRgGwzkALsM5ADRYRDH9pVyl+"
            + "sM5AJbDOQAWwzkATsM5AAbDOQA+wzkALsM5ALbDOQBuwzkA1zOTLSNXs90awzkAxsM5AJVtTIG/P59pFyeLBRhYSDVBISkdrsM5AJ7DOQDPW7PpG"
            + "3fD7T6/UQQXU6vBLwNqMQywiD3+vzEAWh5lHTLDOQBuwzkAFnIEkf7DOQAHc7dtRnrc+QFVfIlgoHxF/s9BOPLDOQCqBjJBcAYAAAAAAABAOAAAA"
            + "AAAAAAAAAAAAAAEAUAAAAAAAAAADgAAAAAAAEA4AAAAAAAAAGAAAABcAAABSAAAAAAAAAAAAAAAAAAAAUQAAAAAAAAAAAAAAAAAAAFMAAAAAAAAA"
            + "ioAAAAAAAAgAAAAAAAAAAAC8AKUAtgCZANYAUQBMADMAAAAkAAAAigAAABwAXwDEAAAAAAAAAH0AAABfAAAAwwBRAAAAAAAAAAAAAAAAAAAA0wAA"
            + "APcAAAC+zAAAEswAAAsAPgAUAD4AHQAAAK4AAAC0MNYAKTC8AJyZrACbANYAAAAOAAAA6dVfAIxiFACeAAAAJgAAACYAAAAhAAAAXgAkAAAArgAA"
            + "ALkAAAD4AAAAUwAAAP6nAAA9pwBXTAAAzHkAEsxgAHoADgDMADIAp8kAHBpgOVZelLnIPCcUm6wAivK2OQnCT4Eaew6KGt9UAG1fPACwFHUAdAiw"
            + "AMdbyMWsABpJpQCYrLkAJLk/AOVuIgAkv5AAy2tjAABeGwAAI+LSAMKJXwDCjAAA/8kAAA4AtuXwAD3+wgBteY0AVDIAmVgpX7kFqD8UYJo9lwl2"
            + "YnalMgBhpSMAYbYWOZuWFlv+ufAIl8C3FCS8ybm0vBH/IQAlwhgANmSYAAB/CQAAxXweAAXfywD8SQAAD0wAAAAAnM0AAGM8AABByVFsDjJRAGts"
            + "pQCCbNYAdmz+AFm5BKEOFSehu0oDofY0z/wQNIL7FgX76iryIqEN/1Oh30tWMlEAC/0AAF84AACPVAAALWQAmy2eAP5ElwCYX6QArgAAmGwAAJil"
            + "AAAktVEATMdRAOUAiwAiANoAqQC2AGEAA6EZ0fyh+Syh/Gb/atFmNNh5wrrYhEqTogY0k4J+DdjfwooAVoneAAvypQDBpvgAFCsAjbZAAMqKKQAp"
            + "l3sAtAAwrG8ApeBcAE5nhQA9OiUAyAAhAHAAngBcNqAAXLnnQRaTBHYWqwEEFgYPBBAXcm32dtFr9nIBa0LvARknk9EOcgC5VM8AuSebAF/mfAAw"
            + "/5d7ABk/iQDJFKAAKZJeACVfCfy8FGBjpQnnfMso9dYAlcvuAOjL3lE4Xzzx5hR0ZjrYqUJkkxd28JPqcha6WRYWcgRLFu92ZhYEcmaAZXIOZQAm"
            + "MnYASGCbAADfdAAA73dPAEglGgAAiwkAigDWADnHkMCuSRm8x0mwijJeT3TwAE5e/5mlPVilX1ZuDMCsxEwfWxyBjIwJX6jo5aWQwlMyM6wysM+0"
            + "DnXqU1MJ2G2CPAAAbiQAAFTAAABbYgAAT/AAAE5bAACllgAAc8cAAAC+wl+5epVRTl9ktMm5fDKoABy5vwDAX6kAXzmAesBigWCeBoGWbdHAtiSh"
            + "pbkAbsDKrLmBwrckTo1kdLSEYJt8FAAAU18AAG0AAACEAAAAG24AAF6mAACsDgAAuQkAAB8A7gAAACUAAADIAAAA+wBpAAAAvgAAABIAAAA+ADYA"
            + "bQCwAJYAGQC2AJUAuQDCACwAUQAnACUA8gBWAEIAdAClAAAA1QAAzAAAAKcAAADM8AAAAFsAAABPAAAAJAAAAAAAX6WnAALApwA8wGkAnrkAOcyZ"
            + "ALkAAL6lAACnpQBRfABJAIoAYAC8AGQAiwBtAHAAUQBkAFEA6AC5AGQArEcARwAAAI4AEgBNAMwAegAA/lcAADzGAAA/xgAAucwAAAAOGkwAyt+0"
            + "OfB8OsDytFQkAD9uPV+5Qv4/uaCePSXJpQAAOV8AALz4AACKAAAAdABaJgAASSY5AKyDOQC5JDmXbj4ACak+AAm3EgDHXqcAaGAAztb6AK+s8gAA"
            + "E1gAAAB17Kw5YIlPuYzKa8SMyFRPAIr6azm2IiyBCXxYinWY5aUATJhiAK6B1QAkX6UAdAAyXz8ACBQcANYIlwAUOph7u1IAlcJfALsNAADw8wAA"
            + "hJwA3ZwuAMvkdgAAhuYAALwACMjEAHDItgCb5U5IBU8ASCF5ALmKkGKsl0W5CTxFtNG3bcQXdPm86phmIWqXSwVqdfWQoqAqQQXK58j6e21kSQDu"
            + "ILYAufSvAIsJhwAAnl8CALCPXwBwwQAA3wcAAHMAkLylAIK8TAB2pckABK4AACKXAACEfACLkCIApZCQDr8Wm3B+I/kaoeFmT6FPZllqDktF2LtK"
            + "RSwWNIIGFufbrQB8sz0A7dCsAMQl1gC5ti8zAAteUQA7fAAArLAAAABRAJcA3ADfADYAZAAAbPAAALkAAAAUAABRITYA8cmDqEtZk6bYq5OhBO92"
            + "oe8GdqG7QnKhwoAGoUtKZQZLNKt7OkMAyGO3ANmC5QC3qEwAyiwAW4kiAP3ybAAybrkACQCYAJ8AVgA6AE4AIwC23PUAAKUkAPistgC8mCYATp13"
            + "5WYEcimN79ibWJOrVHKTAaYWq3bqEAEEdksPWQFm/+oriIkAsLToAGAJtwD9VpYADQ8AtmOqALYGhQDEamAAvA05CsIgwDzoUCFnDcX+/d9iOJyK"
            + "km7F2jxuXvh4/04wFrkBDha8BA4WPycp8k5ypSMcZQDjPNElFk9ymxZ5D0F3bQAAJZwAAF8FAACLhAAABnn4AHY8AACcFAAAdF8AALsAvKDCX6Wt"
            + "Q5EU39jHIQlfcM+sFGubpQlro66toCEhIDzvPCM8csjnIXI648TvTAnAFmD+X0J7W6XRwqDEWfActwAAvHUAAPheAAAAeQAAWXlIAOoJ1wD6xAAA"
            + "nl8AAAAStsMAvj/WbGmBnr0AuahSAF8A0gAUALEfCABxPluHyluldMqdTmRkFEnCKV9blV5jY0gc7IJIgcJDFF/i/yGpXwAA+wAAADIAAAC0AAAA"
            + "P4wAAKV1AABfHAAAALwAAKcAAEenAACyAAAAAqfMi9a+AFEAAMyqAACnnAAAAMUAkF25HKnetpamuZZ0bgDFSZw9lAB01OYAJGtDi7l4/5LyAAAA"
            + "nAAAALAAAAA8AAAArlYAANU/AAAApQAAANUAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=="),
        new("gasarrow", 0x400, 0, 49, 38, 0x00000000, "poisongasarrow", "icon_poisongasarrow.tex",
            "AAAAAAAAAABwb2lzb25nYXNhcnJvd19uYW1lAAAAAAAAAAAAAAAAAHBvaXNvbmdhc2Fycm93X2Rlc2MAAAAAAAAAAAAAAAAAAAUMAAAAAAAAAAAA"
            + "AAACAAAAAAAAAAAA",
            "cG9pc29uZ2FzYXJyb3cAAAAAAAAAAAAAAAAAAAAAAAAmAAAAAQAIAAwAAAAAACBBAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAgA=",
            "LAAsABEA1wBEAAAAwmQWAIAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAEgAAAAAAAEA4AAAAAAAAAAAAAAAAAAQBQAAAAAAAAAAAAAAAAAAAA"
            + "UQAAAAAAAAAQAAAAEAAAAFIAAAAAAAAAAAAAAAAAAABTAAAAAAAAAECAAAAAAAAIAAAAAAAAAAAAAAAAWZQxAkpyNghHfSt/OW0kFk6ELUglSxph"
            + "RHAoQxMrEGA4bCQHiXY6JXSqQj4iSRhyOGwkATVnIn9Rhi9/WpYyDClOGk0QJg0XMlogLUZ7Kn83Syl/d6JCfitDJW00ZiJ/K1cdcmGZN34SKg5/"
            + "IEUXcg8jDGNfkzdTKE8cWol7PHA4ayR/IkMZf1uWMzk/cigmJU4afxAlDVRZlTIPXJUzLRUwEH1JfStjKFAbdcHNtn8ZNxN/K1cdfiBFF3ZaljIB"
            + "GzcTTUN3KX8cPBV/LVgeaX2WbHtMgS1yWpYyDliOM3FalTIlOW4lEmKaOD84bCQFWpYyIER3KTwxWB5KUo0vATplIkdHeCkxWpYyDT1rJmYTLQ9m"
            + "S4Etf0uBLSMOIgw4X5Y2ci5THEpIfyoBdWUuZyhTHHJ0qkJyUX03N1qVMjYzZCFyO2wkSlaKMn9GeytxQHYnAREnDnYuXB91T4YtMlFgSn5BbSdS"
            + "WpYyGSRGGChaljIGJ1Ebci9dH38hRhdyLU4ifxw9FV5Jfix/f7NJVB0/FXMxYSB9WpYyAVOJMH9Gbyl+GjoTfl50PX8uXB9yGTkTckBuJhdaljIH"
            + "NFgiflyXMzNSfS9/TYMubDNhIX8zZCF/O28lfxczEX9aljIHeK1EaFqWMgk4bCQCPHAmHbDCoX8SKQ56kaiAflqWMghJfix4cKY/VThsJAFaljIE"
            + "GjkTfy9cH048byU0I0cYS1SNLyoaOBNnY5Y8ZlOGMXI0ZSJcNV4gS3doMHdBdih/WpYyEidPG3FxZzFMUYcwfzxwJnlOXTB9eppAZ4JxNn1ZdkNw"
            + "NGUif4m7T25aljIDWpYyCBUuEHgmTxp/bX9hfcjRwH8qVRx/HDkUezRlIkAWMBFWZ507chg2Em87byVyDyUNQC1bHn9AcidwOGwkATxwJgZpoTtI"
            + "XZYzQhw9FFhCdylyVG9GRVyRNX9pZix0NGUif5vJWnI+ZiZ/QV8wZFaLM0pWkDA0Nmkjfw4jDGk4bCQBNmgjcmKaOFwQJw1xWZUyHUZ8KkQtWh4K"
            + "RHcqeFqVMhUjSxlrDyUNbhQrEC8RJw5MaGgvGoKKe39gmTZGQXAmPx9DFmIkTBlyHUAWf0BzJ38tWh5/WpUyAkh9KiQTLA9/WpYyAVqWMgpSeToP"
            + "I0oZfzFgIHI4bCQBWokwHlqVMgERJw5rJ1Abfl6TMxGAdjs4PXEmf2GDPH4gRRdrS4ArLCxZHnIrWB1dPHAmHZzAgn84bCQKd6xDTA8kDVosWR5/"
            + "WpYyAk56M1gwYCB/T2koIRczEXIhQhd7T4Qvf4R3ORJ5rkVVZpw7IipWHXVQhi9/GjkUd06DLn84bCQBPXImGxQvEGs3YCJ/AYAAAAAAABAOAAAA"
            + "AAAAAAAAAAAAAAEAUAAAAAAAAAADgAAAAAAAEA4AAAAAAAAAGAAAABcAAABSAAAAAAAAAAAAAAAAAAAAUQAAAAAAAAAAAAAAAAAAAFMAAAAAAAAA"
            + "ioAAAAAAAAgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "APcAAACZAAAAcwAAAAAA0gAAAAAAAAAAAHsAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAiwAAAHMAAACZAAAA9wAAAAAAVwAAAADY5QAAAEcAAAAAAGcAgwD2APIA9gAkAPYAJAAoAPIAKABNAAAA3QAAAAAAAAAoAHsA9hGH"
            + "APY6pgD2AIEAKAAyACgAVAAAAJjhEgAAAAAAAAAAAAAAAAAAOAAAAMQAAABUAAAAmAAAACQAAMHyAAA5tAAAkQAAADkAQADBg8sAifKAAHokiQCE"
            + "AM8AOgDF/TIAPff3ADl20gA5AFQAwQAgAIkAEgB4AAAAAAAAAAAAAAAAAAAAAAAAiwAAAJgAAAASAAAAAAAAAAA/pUYAJrzJAE7AXygr3tldKKWi"
            + "xV0/bZHFPsmWQuVj1UuBAJAOBwAdmFQAwCAgAMci7dyUDQCEDiEAKI1GAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAdMWMAMcji"
            + "9sjfrc+i6GHBz5qt1dw2iClBkkZOKfT7x5iXABsgIADIt+0ANlsAAOLTAHhjoAAoTK8AAPfjAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAIUpDKvZvIgaym9ok63Z0x7csuJaQSEDoNFjRn/AS0Z9RH0oyEp/AOsHmQDkzB8AkNPWxUrTZS+RWXKdJ44rhISDUwAAABEAAAAEAAAABAAA"
            + "ABEAAL2DAAB7AAAAewAAACUdRqbob6+2Idmg7wz1LQvVIYzjKWNLk96rSweFGvkjoSfF6DTDnRZZXPYYENAAZn9iKec061JKoSVBWKH15cUEdwAA"
            + "e90AAAAAAAAAAAAAewAAAAQAAAC5AAAAuQAAAKf6WhQlN1xgmtcKFIhVCopw0FzmqxAL5qtOXBmUntBe0LDi4lC+4nBQ524asagWUSkZspLk2orr"
            + "yr4J2buwwvU9TQAACPAAAJy5AAAAfAAAANQAewBpAAAAiQAAAIQAAJC+6oras6dVyamndHGpkr6vxxCwaHUQvusURXTqdP5V6pSabHVMFktlLG1W"
            + "HOToE8apbHUGLhj6Bjht6084ld/CUwAAOTwAAAjyAAD2PAAAACMAAAA5AAAALwAAAJwAAFYAvgBWAKkApgCUAFEApgDGAC4AsACpAJQAvgCmAEkA"
            + "HgCmAAUAggBKAO8A5wAwAFYA5gBWAL4AVgBMAJMAUQAAAAAAAAAAAAAAAAAAAAAAMAAAAIkAAACEAAAAKAAAAGQAVgDzAJsAtgCuANQArgAFAAUA"
            + "uQD3AIIAawBkADgAaQATADAAaQC6AEgABQA9AJsAwgDzALkAtgCCALcAZAAAAAAAAAAAAAAAAAAAAAAALwAAAIRXAAAomQAAANIAAAA8AAAAgwAA"
            + "AAAAAAAAAAAAAAAAAAAAAACDAAAAPAAAAAAAAAAAAAAAAADCAAAAMgAAAAAAAAAAAAAAAAAAAAAAAPcAAADYALUAAAA+AAAAAj4AAJmsAABzmAAA"
            + "mRIAAAAeAAAAOgAAAPwAAAAAAAAAAAAAAIMAAAA6AAAAHgAAAAAAPgAAAPcAAABCAAAAVAAAAAAAAAAAAAAAvQAAADvvAAAAQgAAAFcAAACLAAAA"
            + "mJgAAOESAAAAAAAAAAAAADwAnX5NAITNASj2hYSEKCIAJ53rAJEIYhVBQ5A8JggJCClas3rHrJeEG5gg9jUS7ZyfAABdiIcAz+yBAAhuswAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAPYAB8id9pGixXhbbZHFieIAQtUqAAk/IQAdCd8AzT+jP28gIMIr7e3CsgAozG4AAD2MWgBYqzIA"
            + "B4/pAFKXVAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAIQi6AMnNm4M5W1jWZZu7NPR5UYMxwnJ7G/0bfnZMaJGWaBDeFkPnPYqBgAA"
            + "+5YAAA7TANWYfwDlIKEAwTB9AIkAEQAAAIMAAAAAAAAAAAAAAPwAAAARAAAABAAAAAQAAImiIWuHrSqOkIhGLfT5Rn9+lgyrKybiq59+u6tfyFk4"
            + "06CEWNMPnMUNkihdmP4oASB9AEUFNAClVzQAKYl9AJYABAAAAHsAAAAAAAAAAAAAAHsAAAAEAAAAuQAAALkAAOjWuGLJyn/RjMnx0asXLbEmSxpQ"
            + "fmqTUCtx1LGfLDDRYiaGbqMQwm7IEMG72aXPbaqo3+gL1jb1C/g2LJAWLJbbWAAAAFsAAAB4AAAAKAAAAQAAAE0AAAB7AAAAewAAACpcB2BLHzAU"
            + "Goq6FBrrWBzAox5lyKMwdaL62HWtZdBlKWBPMzeKCfWiGUrobXSRbQp0ISEKLG6ICh9uY9AGGEwABQAAADAAAAAnAAAAnAAAiVMAAIQ8AADO8gAA"
            + "ADwAAPSbSS7Am9pR6nnmVmWTXptehtdr5mvXTNqmXqlJm3RMSdT+m7C5nhOpwm85LqQiaS6u/5Wzm6tMLO4OUWK2eVY7AHsAMAAAAIkAAACEAAAA"
            + "TcEAAPB6AAC59gAAfAAAAKe3THleO7CbviPam6kj5mSUOea3OCPauTi3sLmU1C6/OIlgI1FDXjCmxU89UTmNLzi2gUgy1Di3lTC2O8bB7ztIAAAA"
            + "wQAAAHoAAAD2AAAAAIQAAAAoAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=="),
        new("dimensiondoor", 0x800, 1, 50, 39, 0x0012F790, "dimensiondoor", "icon_dimensiondoor.tex",
            "AQAAAAAAAABkaW1lbnNpb25kb29yX25hbWUAAAAAAAAAAAAAAAAAAGRpbWVuc2lvbmRvb3JfZGVzYwAAAAAAAAAAAAAAAAAAAAMMAAAAAAAAAAAA"
            + "AAAEAAAAAAAAAAAA",
            "ZGltZW5zaW9uZG9vcgAAAAAAAAAAAAAAAAAAAAAAAAAnAGQAAwAAAAAAAAAAACBBtABkAP8AAABoAcgA/gGg/wAAAAAAAAAAAAAAAAAABAA=",
            "LAAsABEA1wBEAAAAwmQWAIAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAEgAAAAAAAEA4AAAAAAAAAAAAAAAAAAQBQAAAAAAAAAAAAAAAAAAAA"
            + "UQAAAAAAAAAQAAAAEAAAAFIAAAAAAAAAAAAAAAAAAABTAAAAAAAAAECAAAAAAAAIAAAAAAAAAAAAAAAAvIr9CSoXU39DLm9/ej/dP0IchH85G2h/"
            + "dFKuZ45S6g6FTd8ElGLkB8WZ/X+ydpYCeDzcA3xA1wK5he8TeT3cLp5m6n+/i+oViU7kKiYQVn94PNwXez/aDn1B1gVpL75/9M+GSIZK3RXNu+V/"
            + "ORh2f7Ca0H+9jfVATCCTfxkLQX/Ii3YCJhNNf6lwkVyicewPll3tDMaKeQyOebB/sX31f65ynDp4PNwBJQ9XfyYQWH9yNsR/f0TZQJh+wn94PNwT"
            + "nmGtCdWekhK0fc0q3qJuAo5Xr1K9ivsRik7UFVQ1jX+1gOwceDzcG45S2AOPVdkK3aBaDoFF3xDgpGkUjVHcOXg93A6RXOADez/dNKeGqX8oElN/"
            + "fEDXAd+iXwTVm34j5bJsGqRy7By5hvYDMhplfx4MR3+eZMoOm2nmBoNHzgqibO8EJA5Uf+uuRx94PNwBeDzcGCANTH+8iPMMajC7f82UdVmqksR/"
            + "Wz1Wf5BU4n+/kft/h0vlBrmI8CyUWO4EeT3aASEOTX+rb5sQgkfhCT8dfH/16/9/klbsBGAqsn+CRtAYxI3GFXg83AV5PdwmNBtqf4pM3H+/i+Ag"
            + "GwtEfyENT3+ehr5/iU7kQbaC8AskDlZ/569fSr2L+xlAG4F/7r9sapBW5wV5PdsO5adRA0EnTn/UmGo9ej7ZAYFF0AOOUukBeT3bAuapVwZ7QNwe"
            + "xI/ZEpRZ7gNFIoR/xZ2Tf6OC038eDEl/gUXPD41R6hWQVepOxouGLbJ82xEaDEB/eT3bAUwqg3++h8MC15poApFX1yUjDlJ/1Zp8AhoLQ396PtsF"
            + "3cL/f5Ng4hJ8Qd4ZjFDpMjQVbn/rrUYI5KhaFuywTTNEJ3l/GQpAf4pQ5R3Xt/1tZC+rf4tP5wOLUNQTfkLUAnk92wmOUsIdez/dBp2DyH9+StFb"
            + "uX2JCqtuoQQ3IWF/UiSZf38/0n93Rb9QKBBcf7F3uw2VWu4Iz6r+f8SQ3Rx4PNwORByHfzwkbH/PlpkEkFXsCXFcmH9+Zal6u4fxIb2EqybHkL8I"
            + "fkTcAsSOpxiCTNhjQCddf6NmpAJ2N85/v4veB7yn2394PNwegUXiFGYut39DIH9/LBJifxwMRn/Qk2gIzJSwBIZJywQdDkR/ez/cQOfV+36Wgbh/"
            + "SyKOf5NX7gJ7P9gCPRl8f5hdxzSLT8QFKhFef5l7yn+RdMR/ej/aPIZP11AyH1d/9NGFeB4MS38wE2p/o2iuHng83AJ4PNwWhEbaf0s1eH/x4/9/"
            + "eDzcAXo+2QJHHot/eT3cC1sppn+9hKgD46ZeJZRYuQR4PNwUbzLFf4VJ5BfrrkYSPSNyfy4TZn+DR+NJLxhff4ZKyynEmfpVAYAAAAAAABAOAAAA"
            + "AAAAAAAAAAAAAAEAUAAAAAAAAAADgAAAAAAAEA4AAAAAAAAAGAAAABcAAABSAAAAAAAAAAAAAAAAAAAAUQAAAAAAAAAAAAAAAAAAAFMAAAAAAAAA"
            + "ioAAAAAAAAgAAAAAAAAAAAAyiekAiYlBABZhKACxxE0AABEWAABCmQAAEnOJABKyEXQRDpkLEiVzXxIkc41CEXOEEYRzjXpNc19JKJkLSUFC60cA"
            + "7YssANC9AAAU+QAA68wAR4vtAADL0AAALBQAAAAy9zoA94J0AMwVCADMFeIAAHMPTAC0smH2ZLp/MhIoJM8kC5taEh4OwGRSuge0m7qsmay6B3oE"
            + "DsBJCJtaEgvMPYMA7T+PANChAAAUoQAA6ykA+Yu3AHzLygCMgz0AAPEUQ9SAIQcIKcpqrN49Gx0AUelaTBkox/bFOoj2KHSIvCD/DPSjpd+/o//7"
            + "wCBfih2YHZHAaBsdv5LYrPRpB0PDjQDDKvAALG4rAEehwQCMUh75AJdfnwCuhZ8ArmyfABVh2P4pYRvit4K/wA4VAhu3t+700LfUTrEOCGnxcayO"
            + "jko3yW1K406tjkMM/WmEyCJOVDgiknXTYmi/wGloGzYX3QCFCisAKj8eAMu9pQBTcc/rACmljwBjHowArm+MAGEPdKJhss7f90HYre0N0623YaJ0"
            + "SLGezkGAHLUNgNzTeNbmWwWryZAFdpKQeKuO5dzIvKae51qi39xqfUqqrH0xUD8Alio/ACtQ0gBfblMAHuUA5cF5AG4XbgCh0pYAS2FzdKl/ss5p"
            + "zHtqbe0N0/vefzh0skZ4zkEWeNgN9wUdu1wGx9kJAkSqXSKQ6l0iWaYFqQmmcGpw6tFanorzI0VZUKEAfsJLAJZQnwDCS3wAxXkAS8VuAPkqfgCf"
            + "7FAAn5nQqXSysd8EDvG7Jw0V7x2JKXSiibcEqoIO2BgVDR3R9dG8XM/Rte9mXFpw9bgj6glmXaa4ZjWR9ZrcWmYwyC3B/gAuHo0ALgrCAAEqXwAB"
            + "Mf8qAMIevQDCXywAKwp8AA/K+nTtR7sEkz0YJw1RXB1MGXSiTMUEH/FBJ+oVDR3qZqrZdma7kdH16B3WuNYt6jB2XKoTCXDguDC7as8J3Kyl3QBv"
            + "pZYAUx6WAEl3lwAA3V8KAB5sAQAebFMA/2xTADKZ/uQysjZp8bonrSYNHecmf/p0st7czpPQHyez7apa6Odl/Z7IHOHIrQY33zOprMjcx0qeeESO"
            + "3HhEjngcI45ZVv8AeTtvAG5JUwCWT1MAdzEAizmWACFf8ACM/1YARzJkdNhhZM7Y0D4nNyacHTfMp6l0sjvIzro7HCcNEejT6ODm4dzgkpHn4FXg"
            + "3+DJrEo3AuFp4CfhTuBq4ebgLeFZXrkAeafDAG5JvQBQScMAUN0Ahj+zAIEqGgCBXxAAQpQAtmGUALaJpADG8QsAxhWvAEDEvgD8Eb6JtkIQYbZX"
            + "xhK2EsZktlfGsrZCxrLjEcayxsTGssZ6xrLGScZktkmUOwCDEO0ALCXQAACvFAAAQOuvAAuLYACky2AAlCyGABAAC9sQAAsVlACdFfgAnXNnMqSy"
            + "Z0wL6b5/C+m+Fgs+nZsLm51SCz6dpAvpnQ2N6Z0NnbKdDZ0lnaSdJJ1SC3oQzACDvu0AAL7QAABgFAAAjetnAKSLZwCUy9oAEIPaAAAVD6wA3rJa"
            + "AGPpGzJIusAyAChBYWFNDbFM1HSA8XRDCDQItkOtdJVrVNSdHrycpJUD+PQeqVLAa/1S00MzPFrtl4MAqIWMABTsAAAUgwAA67AAR71WAI8/rgCM"
            + "S64AfAAPshsAJrrA9mMoIvZBOs3xjHRNFoxDdN7KrOLe0MDAWpJaGxtOwMAbjrbiGzSVdNPN/8gbaKX7G2gedRvJUsBWwaEAKXf5ALcqnwDKi58A"
            + "P3EAi6HwAI95rgBHeZcAjCphv3QZYc3OUIBoavApaNNGKf6KRrJD3NtBBwXbDRvomNnADJgfG05oqgdVyaprYsmqYqnJ2QxqaGXfWmj7dS2lMQBL"
            + "Hn4A+cHCAEdPKwDL3cHDACuXRwBfckcAHiq9ACZhA3TtRq3OQczftQ3eyNNhD3SKYbIEnoJBJ56ADdOe356iijPc08dp6Cd9kqo2ffvWaFvnNclE"
            + "+zVikMimNFmleQDlHm4AbsF+APlvlgCf3VC5ADHsxQAroYUAHm4BAH+ZdLxGSM4csZNq6hYNHXaZTLx0skxKBEHxjicNFTQdyGarohxmqpHoZv0d"
            + "6vUCI+8wxzB2mkRYCfWQeAlm5Z7lHqEAef9LAG5f+QBucp8A7DEAw1ArANJQKwCLoV8Aj2HMdKJ/YwQf95PY7xUNHfNzial0svaOBA7x+ycNFR8d"
            + "NWYw6glm75FdmhxauDDfLRNcqfMTCWpwmjBaqpqaIx9ZpaAAlqWDAJb/gwDFHp8Act0AX1+NAG9yKwAuoB4AASYyinRjMh/OusxwJw3K1h1HKXS8"
            + "yrIEntC6J7vEDR0fuOjqNJockQy4yGpFMDSsInBKXOTqSqsnGDPvRAk0cCMeeQD/weUAXxd5AG9PfgBP/lAuAPAqUwDCuVMAOf9TABlMonRQgLsE"
            + "YxSqJ7Ooqh1GD3SpRrLONBa6J+cqDR3cqrui5tYFN5I1HGqSNectkujfWAIcberh52mqahxO1y13MQDLl34Aj2yWAEdvxQB8Ma5fAH6oAQCW61MA"
            + "wrkAAMSUdGvelM6V7aQnlcwLWpW0r6mdsr4Ca0G+/WtNEHVrHJV1a9eVdWuHlXVrh5V1a9eV+pVlleGVHJW1lXWVrJUxlFcA8BDtALAlqAA8r7cA"
            + "PEAArxELAGBJpABgbJQAhmdeQ9V6ZPz4emS/+L4+avgQgWo+lKdq1finatWkXmrVavi1+LX4tRq1+LUatfi1L7X4tfi1+Df4tfjA+LX4rPjdXmAA"
            + "jadnAKSn2gCUgdoAELAAgb4+AIElZACBr14AgQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=="),
        new("expertfocus", 0x1000, 0, 51, 40, 0x0020C1E8, "expertfocus", "icon_expertfocus.tex",
            "AAAAAAAAAABleHBlcnRmb2N1c19uYW1lAAAAAAAAAAAAAAAAAAAAAGV4cGVydGZvY3VzX2Rlc2MAAAAAAAAAAAAAAAAAAAAAAAUMAAAAAAAAAAAA"
            + "AAAIAAAAAAAAAAAA",
            "ZXhwZXJ0Zm9jdXMAAAAAAAAAAAAAAAAAAAAAAAAAAAAoAAAAAQAAAAAAAAAAACBB/wDSAFoAAAD+AaQBtACg/wAAAAAAAAAAAAAAAAAACAA=",
            "LAAsABEA1wBEAAAAwmQWAIAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAEgAAAAAAAEA4AAAAAAAAAAAAAAAAAAQBQAAAAAAAAAAAAAAAAAAAA"
            + "UQAAAAAAAAAQAAAAEAAAAFIAAAAAAAAAAAAAAAAAAABTAAAAAAAAAECAAAAAAAAIAAAAAAAAAAAAAAAA/vHFf9KhXX/PolJ07bdbAZw+K3/nqUMW"
            + "7btzAu24U3/+6p4C8cyVAf/xyn/azKx/6axUD+ejQBn22ol/0qTOAmsnHH/77sV/0Ydtf/TMdgzLclt/2J+BMuahPQVqRB9d7t2vf+/DewLgqngJ"
            + "8MBnEMKUPnnvyHZ/56M6GNau0gHMmMw/46dVFLmQTF//7KABkDQnf/Pgun9VJRh/tUo7f+y+dA7hzKF+6KhHAf/yvn+kXyh//u7Jf3tVLmT63YwB"
            + "37GJD+63Wiq2YkB/67FYBOuzVzv30nx/+NiFBuahPyGpMCV/5qE/A+e3gwJEJBd//eiiAbqogxu4kFZ+6KlNAophj37Tor8B6apKIeelQyWCVyhU"
            + "5qNCEvfguAHnxX5/sScef9Gs+FfRsoN/5qFABuahPgnloEEI67BPB0EjEnaKIRl/5aZGAf3llwT52YUB7bRXG+ahPgHWxqN/my8jf+y3ZAPnpUcW"
            + "5qFACuuxVxC6km9/ei4if7mCNEjttVcbWS0vfufXqh1UPCZD/umcAeekRAP955kGl3lDCPTUnwHoqE4Y6KY9E//rngHwv2UK8sVsF8BnL3/vzql/"
            + "99J+Du23Vwjko00I5qA8F6ldQ3/kn0UO3qBoAlgyGG7ruVlSsVk/f/LHcgK0Myh/277re5x2S3jmoT4Q5qA9H+ejPij/66ABaj1EeNuvtALg0KVo"
            + "xpPgEtqyxQKXbUha4aJfBeiqTDLopkIH0pI7OfvgkQTmoD0T5LF5F+WhRCHwvmMG/eWWB/C+ZCHHk9cD1JufIDgeD3fnpEAC0ZloFumqRxT95pgB"
            + "6KdKKuuyYAHWrbVV4aFbC2U8I366RTF/7r5pAuOiTwK9VEF/67ZqBvzikwfcnnECrYBDffHAZhCQJx5/0IRcf+W/lH/ImkQW8taqAfrejQlhQiRo"
            + "6KVFC9O6mH/mpkQE0pV9f8t4Mn/dooAC5KhXAdubdQr84pMH+d6af7NyYn/97a5/6KpJB+qyYQrImOME+9+QBdCfxgzpq1QItSogf7daQH/f0LB/"
            + "5qdFBsWU6ynwv2YK6axOBKVLP3/414QL4aJbAf/763/erJcD5aFGA+ipSw3moD0H4qxWBPTMdgPTna0WypTKAqooH3/85JUG7LFTC9KetQfopz4N"
            + "5KZWK/7pnAP31HwCq0Etf9SZnAP/9c1/4K5xA++7YQT1zXYH+9+QBVQwF3XmoD0O7cBqe/vhkQHoqj8YuZCMbZ5qLkzloj0x6atSGuekQgX/9MV/"
            + "5qJBAfLMigEzGQ5978SFAti17WrlqUg+5KVJAemrSgJEHRJ/oiYcf+msUgHnozwJ5qE9HOmsQg3moT0L6rBXCsJ5WH/56MF/AYAAAAAAABAOAAAA"
            + "AAAAAAAAAAAAAAEAUAAAAAAAAAADgAAAAAAAEA4AAAAAAAAAGAAAABcAAABSAAAAAAAAAAAAAAAAAAAAUQAAAAAAAAAAAAAAAAAAAFMAAAAAAAAA"
            + "ioAAAAAAAAgAAAAAAAAAAAAAAAcAAADpAAAAEgAAAHAAcwAAAGQAAACZAAAAAAAAAOkAzwAHADsAAACDAAAAvQC0AN0AtADLAHAAiwASvYu925kA"
            + "CCNzAIMdJAASAiQATL0AU9vUAD7b2wAkTCgAJHNMAPgAegBZAAAABwAAAOkAEQD2AGQATgCZAPYA5kczR98A1EesANRHpYsIAKWLhQDpy90A6QDL"
            + "AOlCiwA7vcu9I2QAQgIkABIyJAAohCQATL8AnLN6AGIj2wAks98AJEzNZbd6pMd+BLUsle21oxba0fb62gxOl0vGTkSUzjqakNm/jKXZhcWsmsXK"
            + "3UQhlqM4yzGj+oUpo3GF8KOPhT+mfQAgRWMAg2dnAKzbiADfyudkACmjPQAj1D0AP4YAAC8vW0PJL7eA4NHW8sdoFfFmaPRvZnVUPM11VCe+TVYJ"
            + "t17P6ltexepWXlIhamFSDlxhriNcgoU/FYe/ptacGyN48gAAeJgAAERpAAAGVwAA8Lv7AF0auwB998cAhwD4AODyh/WI4ycmdLCwC6d7waJ118Gf"
            + "ivzBoiqVwXctf3v1e3dB43sZ8JN7U/CAzAJdH8yifT/MooemOaJ4q9AL5QV41UoA8qFKAETSSgDnAOwARvwAAJLXAADEDwAAqToAAPRvMA2SSZ+i"
            + "t1Gfe5hRe59DWw0NkX533of63vWe8t6y3j9dNRMCXZ4TP6brNl03gHexkVANsUMuOZ9E5TDcgKtGqaEAajPsAJDt7ABu+OwApR8ABG5+AFnS+QAA"
            + "oZIAACcVe//BcQL/UZoXMJ9FLjnWCRPcWjkTEEQ5Cy7xDcy6PwFYMl4BJ09YukW5/i6AsLgQMrC43FAnUFgrGBfQWJpXcQBwLBUAtCy8AAAEowAA"
            + "FgegAJgaoADW93oAu3AaAF63sTJ7Ri4mezi4d6qTAdxc7hPC8+82sZrvMAI376IQHh5YT/4eCajQELaw0AKaSdOxMknTeTLoCgWq8TKoWDgHRgAA"
            + "GrcAABq7AADpxwAA8yz3AFxZ9wC8B/cAo+kSANdgOUnkKrKway0Z0IA3GblFx1Oe7lcFBe/OsFjvXFiw0ElTKtBJJWBYSUlcsLBJzgVd7iVTXRie"
            + "MrmAtjLQa+vkMwAA1wAAAA8AAAA6AAAAZVcAADPHAAD47QAAAO0AAA913AlVdTBJfop5sBYqqLCAycOR8eHD8TwMw0gFDMPuw0nQdcMJBXUU7iUM"
            + "T+g8DE+wRUVPsICRU7AWKmxJfopVegAAD3oAADoAAABOAAAATuEAAE7JAAAAyQAAAHoAAGgA8gAMAF8ApwBFAFwAGADiAJgAtQAWALUAfwDRAIAA"
            + "8QAMAPIAaACAANEAfwC1i7YAFgC2AJgAGABcQkUAp73mvQCZ5ggAgeYSACQAKAAktUwAAOLJAACUyAAAlEwAAC9zfgDhZEYAiABaAFcAcQBmAJIA"
            + "iQBVAK8A/HC1AOS0RkfhAH5HL8vktLWL/HCvi/MAVULzAJLUcQBXQloAiL3avQCHZIMAJGT3ACRkKAAkidtkAGZfAADaHQAA2h0AABFMAPRkyQC7"
            + "meBKwOZA+LwA2ptlAGYHOgBL6ToAlBIPEmrdxRJqy8USkIvq6aXL6ju83d3fvDuFrbyDhdLAhYWLAzIAGniEANuYtADmWSQAsyEAGh0jABoCPwAa"
            + "MqYAGtov9kba4U5GmYhOWuZXZXFMZkDEeoksVgSkWVsztQfkGkbPKRp+i78HbYvF353P8N/z1K6s87+F3XGFyt1axbzPmoEAOyKBAN8bgQAj1IEA"
            + "Pz8A3z99AMhjYwDHZ48AQGjH8p4MV5PcHM7xe1xcRcHiYJjBtSoWUbUtf5/RN4ANk6qWuZGqI13ZsV1dDjCmfTfMIUU3zCHZRTmM2fHcluv7QwCp"
            + "ySIASnCdAEq0nQBKRWoAALNqAADhxAAAaakAAHUPSZ91Vcyiin7BoioWe7LJgJHe4V9f3gwn4xMM3CcTORNdXSU2P6aCCgM1Qf8unnv/fPF7/3ya"
            + "e7KcgHs/8Pq7/ABuyFQA0q3VAK2toQAAeNKtAESlrQDWpQAA1qUAANcJe5/ke7IQa3v1LoANNLrxFbITJ3GfNsGazKLBRTk5oj8CWKLc5Qk5WBAY"
            + "OdB2mp8BnjICCkWyArry/j9T+lh+WQAA+VkAAA+gAAA6oAAAjnEAekoVABpA/QAa+GkA97ded7FGe94uOHsmuJOqnwHuXKI2zPOxNlGaAjBRNxCi"
            + "Lh6yWC7+5QkQ0Ha2AtDcmlPTWDKm0ycyJQpfqrK4gFj6GgQAcumgANj3oACSEgAAxPMAcMdcAHDHvABwLKMAACfXOdzM/ALCzJUXMjB/LhnW8v8Z"
            + "Wkj/T0ReC7nxOe/QAtCwbFjQ7iWw0EWwublECbioT+i4T0+TLjLQfxeoSZVZ/AAAB9cAAOkPAAASTgAAWk5wANb2AADASgAALAAAAG/0/txeksLc"
            + "77c5Be+Y3FhbQ/65fpEmqPo3JhTynrIUecMJBf4UjQVTT/KeT0/6tv6oT5H+ublD3CVJmFglSbdAkgAA+PQAAJuOAAAHjgAAfk4AAFtOAAD0AAAA"
            + "ZQAAAODRN/qIDDw4dHReRKfOsJp1zUm3iq9Jfiq1SZUttbBrJTg8DCX6ttGwa+u1SZUqtUmain5JmnW3SZqnzglEdHSImQAA4JkAAOCZAAAEmQAA"
            + "BK8AAADNAAAASwAAAEsAAC8vQ1svyYC30eCT1mjHGBVoZkj0dWboD3XN7tdg4ifXCbeAyQlbQy8n12Di7td1zehcdQ+NXGj0GBVox5PW0eAv2gAA"
            + "LxEAAEwRAABMcwAATGYAc0xmAHMA2gAAANoAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=="),
        new("phaseaxe", 0x2000, 2, 52, 41, 0x0013CDF8, "phaseaxe", "icon_phaseaxe.tex",
            "AgAAAAAAAABwaGFzZWF4ZV9uYW1lAAAAAAAAAAAAAAAAAAAAAAAAAHBoYXNlYXhlX2Rlc2MAAAAAAAAAAAAAAAAAAAAAAAAAAAUMAAAAAAAAAAAA"
            + "AAAQAAAAAAAAAAAA",
            "cGhhc2VheGUAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAApAAAAAQAIAAwAAAAAACBBvgCqAP8AAAB8AVQB/gGg/wAAAAAAAAAAAAAAAAAAEAA=",
            "LAAsABEA1wBEAAAAwmQWAIAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAEgAAAAAAAEA4AAAAAAAAAAAAAAAAAAQBQAAAAAAAAAAAAAAAAAAAA"
            + "UQAAAAAAAAAQAAAAEAAAAFIAAAAAAAAAAAAAAAAAAABTAAAAAAAAAECAAAAAAAAIAAAAAAAAAAAAAAAAtob0Kr6M/wR9iKB/voz/Ac+u/zq+jP8E"
            + "voz/Ar6M/wy+jP8Dvoz/A76M/xvKpP8jvoz/A76M/wy+jP8Nvoz/C76M/ya+jP8CQjBaZ76M/wG+jP8Xvoz/Icmi/06+jP8IwpP/F8TC3H/Jov9X"
            + "voz/Ar6M/wS+jP8Bvoz/B8mh/y++jP8Kvoz/Ar6M/wK+jP8Mv43/Hr6M/w6FXDl/voz/Asab/zi+jP8Ck2zHOXt9l37Dlf89xJn/ML6M/x2+jP8H"
            + "voz/B76M/yO+jP8LPyw7d82q/xy+jP8Yvoz/AXBLLH9rT5NOvo3/Kr6M/wG+jP8VwpP+aL6M/xDHn/8/PzVTd1VGbXa+jP8Cvoz/CMun/ybKpP86"
            + "elU4fb6M/wK+jP8Tvoz/Ar6M/yC+jP8C09Dsf4JcPny+jP8Dvoz/Ab6M/wG9i/4ivoz/Eo2Zs3/Ck/9Ovoz/Br6M/wl9XKpGuon5K76M/wWDYLE+"
            + "voz/B76N/xG+jP8CvIv9Kb6M/wi+jP8Dj5Sxf8GR/yS+jP8Mvoz/Ar6M/wG+jP8Mvoz/C76M/wdza4Z+voz/Br6M/wK+jP8Ly6b/IEo6Y3C+jP8B"
            + "voz/Ab6M/wOGhZ99iZWvf2FHhVGSnbp+voz/Ar6M/wPGnP5evoz/HNKu/2iDj6d/xJj/JdCw/x7Hnf8Yvoz/ImZogny9jP1KaHGFf76M/wHBkv9D"
            + "voz/Ab6M/wNIP2B6yaL/SMCQ/yC6ifoovoz/Cr6M/x++jP8Ovoz/AdrX8n++jP8Bvoz/Acad/yu+jP8Cvoz/DL6M/wK+jP8Dvoz/Ab6M/wi+jP8B"
            + "hGSvZr6M/w++jf9Zvoz/CKJ32zTNq/8aV0B3XLeP7lXJof8svoz/A76M/wi+jP8BxZn/Hr+N/zu+jP8cxZr/NL6M/wpqYHp8voz/Bb6M/y/Jof8n"
            + "voz/Ab6M/wVye5N/dnCJfmNGN3q+jP8BT05pe76M/wG+jP8Btob1KL6M/wK/jv8iZUuJar6M/wK+jP8Bvoz/BL6N/jO+jP8Ez63/Rb6M/wU6KUVu"
            + "voz/Ar6M/wK/jf9AnHPSOL6M/wGRasNLvo3/Gr6M/wO+jP8Evoz/Ab6M/wFMNTZ5voz/Ar6M/wG+jP8Bvoz/AXNVm13Prf9Mvoz/Ar6M/x1tdox/"
            + "pH3YW4tiPn6+jP8HyqT/Kr6M/wK+jP8BxZn+U76M/wy+jP8J5M3/fKWlwn/Nqv8jvoz/DXVQMX6+jP8Cvoz/BHeCmH+AhqB/RjNhY7S2z3++jP8C"
            + "3sP/dr6M/wW+jP8Ivoz/C76M/wLDlv8rjYqlfr6M/wm+jP8Jvoz/Ecmi/zKJkKh/roDqLmBsjH9ibIl+voz/Ar6M/wbClf5IAYAAAAAAABAOAAAA"
            + "AAAAAAAAAAAAAAEAUAAAAAAAAAADgAAAAAAAEA4AAAAAAAAAGAAAABcAAABSAAAAAAAAAAAAAAAAAAAAUQAAAAAAAAAAAAAAAAAAAFMAAAAAAAAA"
            + "ioAAAAAAAAgAAAAAAAAAAAAAACgAAAAoAAAvWQAAiXAAAFcvAACvZQAAOzsAAFdTZcZQDVdM/hOZeld5U3plOvUIO4SjSlcJEXllLrK1VzwHuQAA"
            + "WR4AAEdlAAAAAAAAAIQAAAAuAAAAmAAAABIAAAAAHlAAAAToAK8EFwBXRzcAAPBkAAD3aQBX/h0AL/5b/pAT2f7fIZr+PU3MGHio2Yw68N/vv/Cc"
            + "PKfwnDo/HT2yKwAA4FkAAItWAACJOwAAL0wAiQA6AAAAeQAAAFAAAI4Av6T3AFI+cAANOkMAPCAAAClEBFckRDtl0kS5YL+ikD/0qRvN4/F4zeMD"
            + "eM2zeyobmmsxmkFrwezjYcH0VIfhhwCOWfwAIxVAABwyTwBlPVJlAISbrgATnQAAm1kAAAAAeFmJADEXIgCKJcAANn0AAJN9AGVSfQBX3+QAB+Ki"
            + "zTFtNBvBS6nZzW3Ys812qTUbl/HBh/nswex72M2AS/tK+wAXsoAAnVlAAPBWxwBuDyqvAHjSLwCnPFcAlO8AAAAA8OEAALKVAAA+PABlflAAAH25"
            + "AAB9vGUA5GAcAET3IKcP0THZ2Qu1QHY0sUat2DGV/EYxlfvmxjz7VZAqqcP5Gn8A+ew+AGE5nQDsJdQAgAMAm60DAJ1BewDUoHsAbgAAjvcAAFlw"
            + "AABDsgA7ZhcAADzWAAB9BGUAfc5lAHVW0iy7Djza12w8ONqzJSc4gKJKs6sxE4DzkDo0+t858ZR7dhMAa54XAEsu/gBtFxUAbdgAcPT5APDsGgC5"
            + "OfQArwAAKOEAACuVAABKDVcAeZIAHldsAOcK1wBWVtoA/vA4ONolRNrXbHUsoKo8mv+qZvqQxuHzwTrbq78u/ieKSv4uWQAAm/AAAOFgAABZRwAA"
            + "QbxgAPOZMABaiWUACWUAAAAAPHmvAFIzmQBawwdXw6sAMlYnANNZ5gBwcFUAlWYsQE9K5Nm1DeQPv9J+NQUulVUFIEPm+JHwRipKjkaRmyIoZQAA"
            + "1DsAAGC5AACLVwAAPADWAO8AVwAXAAAA4QAAAAAEkmcAZcOyALnaZ8nUJzy8AKv+1AAL4eEAnghQAA4rN6d8piqn5Gb4333+BfY8VgW1snoqkFmn"
            + "DQ+O+A2QB9zUAAAA1AAAAJkAAAAAAAAAAAQAAABlAAAAAAAAAAAAAAAAPPAAAElZAMnH28lXM2YEAF5u0wCnbv4AxlYIAAlWN0q/POvvdZUFsH3h"
            + "BaefWYpQm7rSlGYNDWKVDXl54S4wAAAArwAAAAAAAAAAAAAAAFYAAABlAAAA1gAAAAAAAAAA1gAAALweAABgmQAA8MkAAAAKAAAv0wAAmRUAAB5T"
            + "/grwLnIHYLRQVrmjFsAiB/7CBrlDH7654ZjAU5081AoA8AAAAEMAAAAHAAAALwAAVwAAAAQAAACJAAAAAAAAAAAAAGUAADsvAAAEZQAAL7kAAABZ"
            + "AAAA2wAAACgAAABDuWaJCf4+mTzhPGWVVjwA6TLSV7IHCZlDIr8E7mSB1iQA0gAAAOUAAAACAAAAUwAAAFcAAACJAAAAAAAAAAAAAABuL/8A8Lxi"
            + "AJ0H0gCb8BMAAAjSAABDOgAv1ITJV9t6Ps09bZQbnG21eIFtxpqnQd86tXE9wQlU9s2/4+LNG2v2FwAAp3AAAFDwAADoCgAA1D0AiVM9AGVXhAAA"
            + "VzwAAAAAhjFlV00gV1eTsQcSSjEHAFJfuQANLrkAN78yAFLfKg//Szp4oUsxQbthBVRBh8E/NPkPzeMDeM3je0zNu2vGLmUAKhcEADxwAABmjgAA"
            + "Q8wACv52AFfUzADJvLUAiQAA5PAEAOSdBADkK44AolwAAP6fAAAQfQAEYn0AueTkG3x7D0EqA8yXJWHX2HyAs0WigvvBsan7zTXxNM2QA/IOAwB/"
            + "PmsAPghrAJ1DawDw+0soAPvjWQCtVG8AbPO5AAAAfQqZAH3wZQDkWW4A3EMAAFmVAAAXGQAADS0AHi0Z/0pqq9mb/CfMSvs4u5T7VTE2dizBhEBA"
            + "waGANJB2NKk5AwA8x3sAsgFrAFkqSwAiqUsNALOXFwBAdv4A8/rTAAAAZSsAACJKAGUyJQDJ8McAAP4KAADhFQAAF9RXAJVwlVUnLkos5vo32cNP"
            + "tf+eM9HDOrEsq6B/VSeAUObmgu/xF2cAA0NZAAP3FQD004kAGgEACgsqANa40gBXPFAAAAA7ZUkAZTs5AEcyLADTFdoAAFn3AABDnQAAZhdXAO88"
            + "E6G7ZliQv2agNQ5m17E8K9o4lNw4J2ym2qtqUNd29BdUYFkAGm4yAKoKVwDzHh4A+rIAVxOdAACb/gAA4VkAAB4Aq1JvANpsWQA4LOEE2tcAIu+7"
            + "APATgQBD+n8ePvMuBdLbOkUJWeQ6BfB9fAWOfiwgRBc5JeT+pzwZ1D88F0Me/gAAO2UAAAAAAAAAAAAAMgAAABwAAABHAAAAmQAAAAcARmbUAEZK"
            + "4QDDDUpXMw0AV5I8AAfD7wDUVSpXcOanBcbhxus/WXx5BWDk0gW55Am1FCUJUuQ8NgktsgU1PmAAyQAAAB4AAAAAAAAAAAAAOwAAANMAAADnAAAA"
            + "AAAAAAAAFckAANSJAABZUwAA/gcAAP4AyQAoAGUA4QAHAJtXz1YbCiD+xmUFlbEesf62BDpg0jI61JXnhPBw53rw8G5TAAAAyQAAAMkAAAAAAAAA"
            + "AIkAAAAAAAAAAAAAAAAAAAAA1gAAANYAAABlHgAAUy8AALkAAABvAAAAWQCJANsAZlNjAD5ZIQBKWU4AmwrhAHSvnWXtV3DWDTtZOxPJ8DvTAAAA"
            + "CgAAAIkAAABXAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=="),
        new("shatter", 0x4000, 1, 53, 42, 0x0010C338, "shatter", "icon_shatter.tex",
            "AQAAAAAAAABzaGF0dGVyX25hbWUAAAAAAAAAAAAAAAAAAAAAAAAAAHNoYXR0ZXJfZGVzYwAAAAAAAAAAAAAAAAAAAAAAAAAAAAUMAAAAAAAAAAAA"
            + "AAAgAAAAAAAAAAAA",
            "c2hhdHRlcgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAqAAAAAQABAAMAAAAAACBBqgDcAP8AAABUAbgB/gGg/wAAAAAAAAAAAAAAAAAAIAA=",
            "LAAsABEA1wBEAAAAwmQWAIAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAEgAAAAAAAEA4AAAAAAAAAAAAAAAAAAQBQAAAAAAAAAAAAAAAAAAAA"
            + "UQAAAAAAAAAQAAAAEAAAAFIAAAAAAAAAAAAAAAAAAABTAAAAAAAAAECAAAAAAAAIAAAAAAAAAAAAAAAAy+X3fMjs/394yP8Bfsr/JvP5/wE9cLdl"
            + "q9//dN3z/n9+yv8OeMj/BzBdp3OCzP8TosDheb/l/1/n+P9/h87/KnG79C2GrNd74PL9fen5/3/i9v5/eMj/AX6j0n1tksZ4vOP/B3rJ/xXC6P96"
            + "eMj+Ms3s/26AtORvT4jKWdPk9Hx4yP8Iwej/cqHZ/0x4yP8FRHvAXHnI/wJ8yv804/b+f8ba733P6/x+utfve73m/2V5yP8UueX/dqnc/yWBzP83"
            + "eMj/A1WAvXjS7v1+iM7/CJOw1n3l9/9/eMj/Ad3x/wXB5f8dsN79aZTT/wt1mst8ktL/BHnI/wd7yf8MSHy/b9Xu/3p5yP8Pfcr/HHjG/SZllM5u"
            + "gcz/V9Xt/wHk9v5/5vj/f3rJ/wF5yP8Bfp7Metvx/3zg8v9yecj/BdHs/HyJzfxKYIfAeoPM/x2m2/9TeMj/AYjO/jKb1v8pk7nge0Z0t2t4yP8Z"
            + "5vf/f3jI/wy10et8ebzxWHjI/wmx3/9Wyur+eeT0/n6S0/9AeMj/Atbw/n+Nv+llesn/EnnI/y56yP47ntj/bnnI/wN4yP8EgMv/A57X/wHX8f5/"
            + "eMj/AX7K/zJyvfY1fsr/Adbw/n3i9f5/nbvefave/0d4yP8C4PT+f3jI/wHR7f9zTXm5cITN/wOz3/8B0u7/bnCl23GQ0f8CkM/7cqDY/wV8yv8D"
            + "eMj/I0V6vmR5yP8BSYPHZcrq/2GZ1v9nfcr/E4rP/wX0+v8FeMj/G3rI/kO14f9u6/b/AuHz/3vL6f8Ggsz/Abvk/3yGzf8ZeMj/FYyn0HqAy/8I"
            + "3fL/eH7K/wR8yv8Becj/NZjV/znN6fp8fcr/GOr2/n2x4P9Le8n/THjI/x+Q0v8yic//AnnI/whHeLtyptz+ePL5/35SfrxugMv/Brfi/1F8yv8f"
            + "eMj/Cqfb/wGtxuN8UZDRTn7K/xal0/V8bbjyO3zK/y94yP8Sq97/XoDL/xDb8v5/gMv/Po3R/2fE5/8Cptr/CnvJ/whYl9Zo6/n/f5fV/1TZ8f5/"
            + "fMr/B3jI/wV7yf8ge8n/BI6v2Hqd2P9C3/H/FFaS0lB4yP8GbKzlZXrJ/wNlot1S+fv/Al6h30Z4yP8DoNj/FNXu/2Xb7fl9ecn/BoTN/zR4yP8H"
            + "eMj/F3rJ/xh8yv83hs7/I8rp/xBGf8NW5vb+f3jI/wF4yP8C7ff/CabS9GdEbbB3zOz/fILM/wuCzP8wfcr/K3vJ/wK95/9rh87/YHnI/inF6v9s"
            + "s+D/NrXj/1hPd7Z2ltT/Asfq/3x4yP8Ba5bLdNDt/3yL0P83teL+eXqfznm45P9iwuD1e8Hm/1qt3v1jzer/VsPe83xoredGAYAAAAAAABAOAAAA"
            + "AAAAAAAAAAAAAAEAUAAAAAAAAAADgAAAAAAAEA4AAAAAAAAAGAAAABcAAABSAAAAAAAAAAAAAAAAAAAAUQAAAAAAAAAAAAAAAAAAAFMAAAAAAAAA"
            + "ioAAAAAAAAgAAAAAAAAAAAAAABkAdgDSAOkA0gCBAN0AAACBAABHPQAARzsAAH6+jPzhXHQ0LzVCNOsEpbjg+W/LAEmOjpwomaVrxnP7da+bnAAA"
            + "mwAAAH0AAACmAAAA6TQAAACkAAAAvwAAAIQAAADpvdwAqL03AH69CACDvdEAAL0sAABHk72BOES9gagJ1ohc0V86ISiWciT890rKNOXMJg/uioSs"
            + "lorEQUKaQpEEJgAABHgAAD9oAACbaAAAjM8AAAA1AAAAvwAAAHQAAK8AIO9CDl28P+GNRhF0B0kAXI6GJoUIIMVJXi5ciC0uHKEHeXGY6kpZE+0f"
            + "zYod6JLkXf9GMcAnTFDz/3EMkoZCKgDKX1gAU8UyAADKPAAAtR5oAKUnUwC22QAANSEAAE4AXZLyAFCj+Q4xJ0nhKvBNIUwc4dpBWdSi76GvVYAX"
            + "HMGXEAYuo58gIEYQDd/zGF55C1hjbQZIlw1xEMkNuzAfeQAm9F0AUq+wAAASggAAcivKAI2iUwBh2gAAZiEAAAAn8wzgHpcMZEquMCQtrQtCduTN"
            + "BKY8bcyZSg0bqxUXC3upw22KEBBiip/6VHts5CDQWzQqgMG4M7hR7NA0bPoqvNYA/+UAAKORAADz3AAAG9MA5swnAJTc2wB2t6MAfozRwDHxVZDN"
            + "znIGYkJGWRWFfkqXSXY8/+8szYZYq3kYbZrjwzyy7Hnssp9R0JoBUPkYnx24ZhU64zQYCvkdEzPe1ccArXKMAJftAABG0QAAB7oAQ5r6AOaePACU"
            + "Z0AA6aIAAbxVnAteknMQsKNfw58A2nBQpp70wcSayVGg7+gQfxX6K/XDbGz1bbouf/rDW0or/BDsbGYV+Fj4H/zDWP8KuwCvPHEAXCqjAF/zvAAh"
            + "bGHbAEhWRAAznjUAbpa3AJ6BVA+afRBuNpl5w0ZnMMMA8HH+UkxwPMW4cFmvcqofgPhsZfgVd/hm+BW67LolF7oM0MHQW58QPEjQ+C36n9fzowCL"
            + "7/MAvWE2AH5GoQCoHMzyAHD2IgCdOfkAl91jALThGJGTKSqKCHRYGzR0cnLyA/PFuE2Xp0nhrjno4U72cQ/ocmmAJe+zvNPld7xKHheSAfO6vHnz"
            + "GIDD89MtSryCx4kA5WQFAJFNBQD2AAUAk+QAANTeAAAmSwAAqOYAAFcA3LlcAE9Lr4f9pUKcsqIRAGGBuQCSOKAASdJLACi+59UcTJ24HO4l93AE"
            + "+vvbQ7r7oxY8NEA61zpYG53kMvxK8QAAwlMAAAgAAADLAAAAtKsAAOG5AAB65gAAALQAAAAAdkcAAKa9AADHvQAAp70AAAV+AACVRwAAlTgAR0d+"
            + "NXWkdELUPylcNbThXG+U4Lf1j4dCBK6cr0IihI/Y+cSMPwAAAEIAAADEAAAAdQAAPaYAAOkAAACBAAAApgAAAAAAAH4AAAB+AAAAGQAAUhkAAMiL"
            + "AADIOAB+yDgAfgWD4VdapOFcp9lklnVcZNHpEuC4dM7h27e/4fCkQikE0QQA9wAAAPQAAAA/AAAAmwAAqGsAAH4AAAB+AAAAAAAAAADutMEA8Jmp"
            + "AKS5GKikFLyo4MvwqGQ5I+nYyyJ92TdK9vQq8/3JiJJWVCMHsnkIGytGQwEbScaGjdFMo1iuAqNMhQAA2Y8AAMS3AACExQAAUmwAeId/AFMAIwAA"
            + "AK4AAADZyxhrY0/fa+rTMJs25M20AF4blA5M74MpNLy+IZrz7dNuuwcYHts6bQe7Rhflo6MYmnmj1zSz1SWS2y0qu5cE5GgA2moAAFyPAAAhIQAA"
            + "xV4AynSAAFN4SgAAaDwAAF9SyfOWh5iXY3RQnUFfwZ0A2QclAM9GqQ57uxcp78/fIAsfLQtuWTxbCxjQDNAzuhfseQ0XZlR5mPgVw9+AVPhhqgDW"
            + "Y50Aa9mXAAASRgAAdzZ4AMNWhwBYCAAAPFwAALeB/6m2dd4x+8SpC+01UN8AHLzfTeSXUOs8rmIpLa4geW4Q/BUPUfh5Zgzsw/wwSlt/AStb9W13"
            + "ofW6bGJ/Ffrl6ACZ+8kApkNxAHZfowAAAXImALBVAABeogAAvKAAAH5vQBPpilQYLHIgqaujUEjuAC4ce2tR9LHOMK3VGmLeMfkdENDjNAE6uGYY"
            + "HfkYs5qfFdCyn2zssuxIPJrjXXcY1RQAhoIsAP+N6QCjkqgA81kAOu8GAPl7JQDupR8Aln7cJ7rpkfBRme1pwaCS01uiAOSjVYcBl5Ipef+jXBAq"
            + "AQo0WOxduEq4KoAYNPoK5HtsKzyKAQ1siroMunsVDJ8XcgQADe2TALo0mQABaX0AD60AcCpwAERYcQCkwqMAWlrELknw2hAjTO5Ie+rnWGyBBLGS"
            + "ddwqo8dOXSi313kIDQQXyXmubK1lBllwEGy7/xgNw3y6+jNdAQYww2ydwW6hCgAAonsAAEOeAABcNwAAY5k+AMIpdACSetYAYQAmAPl4RVwCdP9n"
            + "iNQnT9XF/8ymxQ+KmRIyNpNfbpLwt0rCDElQlzCjUJczwPqX5KlAo+gyYUYfsBO8Skb4PLqjwzxPPAAAEf8AABncAADiOwAA85viAAfhlQBV64kA"
            + "zACJAAAA8b0AAIPpLwDSjC8AyyZ6AAjITQC1yOAAVsh4AFWJ7djqlhskjaesdO91PMXqgUrKbF+NJHvZ5dRg0e3YKPlqAAAARAAAABQAAACZAAAA"
            + "1nYAAOCoAAADdgAAAHYAAAAARwAAAJUAAADiAADIGQAAAJTIAADmyAAAZ8gAANoAti/0rwR6SdSuTQJrnQP8ga4A9OEE69rUxi8RQsYO5pYsAAAA"
            + "LAAAAJQAAAB9AAAAa34AAFIAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA==")
    };

    /// <summary>The hooks: where, the game's word, the patch's word, and the spells (mask) any one of which needs it (<see cref="EnemyAiMask"/>: Improved Enemy AI's).</summary>
    public static readonly (int Va, uint Game, uint Patched, int Spells)[] Hooks =
    {
        (0x0010F4AC, 0x1462000B, 0x0808236C, 0x480),
        (0x0010F648, 0x12000207, 0x0806E55C, 0x1480),
        (0x001109C0, 0x14620004, 0x080666D9, 0x80),
        (0x001182C0, 0x27BDFE80, 0x0804F4F9, 0x8000),
        (0x001182C4, 0xFFB00150, 0x00000000, 0x8000),
        (0x001185D8, 0x27BDFFA0, 0x08084328, 0x8000),
        (0x001185DC, 0xFFB40040, 0x00000000, 0x8000),
        (0x0011B758, 0x0100F809, 0x0C083132, 0x600),
        (0x0011B7D8, 0xDFB10030, 0x08081ED7, 0x4000),
        (0x0011C168, 0x27BDFF20, 0x08084061, 0x8000),
        (0x0011C16C, 0xFFB20070, 0x00000000, 0x8000),
        (0x00129540, 0x27BDFFB0, 0x08084280, 0x8000),
        (0x00129544, 0xFFB30030, 0x00000000, 0x8000),
        (0x00129778, 0x27BDFFF0, 0x0804F486, 0x8000),
        (0x0012977C, 0x68C20007, 0x00000000, 0x8000),
        (0x00129C14, 0x0C047E0E, 0x0C083060, 0x8000),
        (0x0015E220, 0xC661002C, 0x0C059B8A, 0x480),
        (0x0015E424, 0x2C420003, 0x2C420004, 0x80),
        (0x0015E43C, 0x54620078, 0x08072EC6, 0x400),
        (0x0015E50C, 0x10000013, 0x0804AFD1, 0x480),
        (0x0015F160, 0x0C043C8C, 0x0C08207D, 0x1000),
        (0x0015F278, 0x0C043C8C, 0x0C08207D, 0x1000),
        (0x0015F354, 0x0C043C8C, 0x0C08207D, 0x1000),
        (0x00161EF8, 0x2C830017, 0x2C83002A, 0x7FFF),
        (0x00161F00, 0x3C020030, 0x3C020020, 0x7FFF),
        (0x00161F08, 0x24427BC0, 0x24427A10, 0x7FFF),
        (0x001676E4, 0x0C05AA52, 0x0C04F3F6, 0x20),
        (0x00167AFC, 0x0240282D, 0x0C04A712, 0x18),
        (0x0016DD2C, 0x0C04EB96, 0x0C0836C2, 0x80),
        (0x0016DD44, 0x0C04EB96, 0x0C04C150, 0x7FFF),
        (0x0017A4BC, 0x0C05ED8E, 0x0C07A156, 0x7FFF),
        (0x0017B594, 0x0C059CE4, 0x0C068A6C, 0x20),
        (0x0017E52C, 0x14830009, 0x0804625E, 0x7FCE),
        (0x0017EA44, 0x8C440000, 0x0C083175, 0x7FCE),
        (0x0017EAB4, 0x8C440000, 0x0C083175, 0x7FCE),
        (0x0018331C, 0x0C058ADE, 0x0C070DBA, 0x7FFF),
        (0x0019A464, 0x24020014, 0x0808139F, 0x8000),
        (0x0019F8F4, 0x0C043C8C, 0x0C040ADB, 0x8000),
        (0x001A1134, 0x24630032, 0x08081378, 0x8000),
        (0x001A3B9C, 0x0C043C8C, 0x0C040AD7, 0x8000),
        (0x001B177C, 0x3C030031, 0x8E230038, 0x1),
        (0x001B1784, 0x24636798, 0x24630080, 0x1),
        (0x001BCBFC, 0x24630096, 0x08081385, 0x8000),
        (0x001BE134, 0x0C047470, 0x0C052718, 0x8000),
        (0x001BED7C, 0x0C04C3CC, 0x0C0822B4, 0x100),
        (0x001BED94, 0x0C04C3A6, 0x0C0822BA, 0x100),
        (0x001BEFD0, 0x24040058, 0x24040060, 0x100),
        (0x001BF004, 0x0C06FD68, 0x0C081D22, 0x100),
        (0x001BF9C0, 0x0C043C8C, 0x0C087284, 0x8000),
        (0x001BF9E0, 0xAE030038, 0x0808728A, 0x8000),
        (0x001CEAE0, 0x0C043C8C, 0x0C04C291, 0x8000),
        (0x001CEC60, 0x0C043C8C, 0x0C04C293, 0x8000),
        (0x001D060C, 0x0C07488C, 0x0C04FF44, 0x8000),
        (0x001D0830, 0x24630096, 0x08081CF4, 0x8000),
        (0x001D0C00, 0x244201F4, 0x0806EC36, 0x8000),
        (0x001D5C10, 0x0C047470, 0x0C04BA6B, 0x8000),
        (0x001D97C0, 0x80830294, 0x080813AA, 0x8000),
        (0x001D97C4, 0x54600006, 0x00000000, 0x8000),
        (0x001DA950, 0x0C043C8C, 0x0C040AD9, 0x8000),
        (0x001DBFE0, 0x24630064, 0x08081392, 0x8000),
        (0x001DDFA0, 0x24840064, 0x08083094, 0x8000),
        (0x001DE078, 0x0C04B4F6, 0x08083FE5, 0x8000),
        (0x001DE178, 0x8E2202E0, 0x080836E4, 0x8000),
        (0x001E6648, 0x284201C2, 0x0C0433CD, 0x8000),
        (0x001E6774, 0x8642015A, 0x0C0433CC, 0x8000),
        (0x001E6874, 0x0C063F7E, 0x0C060669, 0x8000),
        (0x001E6994, 0x0C079272, 0x0C05F276, 0x8000),
        (0x001E6C3C, 0x8FA3010C, 0x0C065F94, 0x8000),
        (0x001E7B60, 0x10000049, 0x08084391, 0x8000),
        (0x001E7C84, 0x246300C8, 0x0808438F, 0x8000),
        (0x0030A104, 0x001663F8, 0x001D32D8, 0xC),
        (0x0030A11C, 0x0015C2F0, 0x002089C0, 0x8),
        (0x0031AE64, 0x001D9FB0, 0x001B8DA0, 0x8000),
        (0x0031B794, 0x001DB590, 0x001B8DAC, 0x8000)
    };

    /// <summary>The game's words the code relies on (checked before it goes in, never changed).</summary>
    public static readonly (int Va, uint Word)[] Checks =
    {
        (0x00108B88, 0x27BDFFD0),
        (0x0010D2F0, 0x27BDFF50),
        (0x0010DD28, 0xC6000284),
        (0x0010E200, 0x27BDFAB0),
        (0x0010E204, 0xC78380EC),
        (0x0010F230, 0x27BDFF00),
        (0x0010F438, 0x8E0300C0),
        (0x0010F46C, 0x3C040032),
        (0x0010F47C, 0x24A53470),
        (0x0010F4A8, 0x00000000),
        (0x0010F4B0, 0x3C040032),
        (0x0010F4B4, 0x3C050030),
        (0x0010F4C4, 0x3C013F80),
        (0x0010F4CC, 0x0040202D),
        (0x0010F4D0, 0x0C07C9D0),
        (0x0010F4D4, 0x0200282D),
        (0x0010F4DC, 0x10600003),
        (0x0010F4FC, 0xAE0200C4),
        (0x0010F504, 0xAE0200C8),
        (0x0010F520, 0x8FA20138),
        (0x0010F524, 0xAE0200D8),
        (0x0010F538, 0xB60C011C),
        (0x0010F53C, 0xAE0D0124),
        (0x0010F550, 0xA6160034),
        (0x0010F558, 0xAE17012C),
        (0x0010F55C, 0xAE1E0130),
        (0x0010F568, 0xAE0300DC),
        (0x0010F62C, 0x0C043880),
        (0x0010F644, 0x0044800B),
        (0x0010F64C, 0x8E2300C0),
        (0x0010F650, 0x2462FFFF),
        (0x0010F654, 0x2C420002),
        (0x0010F6D0, 0x8E2300C0),
        (0x0010F6DC, 0xAE2000DC),
        (0x0010FE68, 0x24020001),
        (0x0010FE6C, 0x146200DD),
        (0x0010FE74, 0xC62B011C),
        (0x001101E4, 0x1462000B),
        (0x00110214, 0x146200A2),
        (0x00110618, 0x27BDFF40),
        (0x0011061C, 0xFFB30090),
        (0x00110620, 0xFFB10070),
        (0x00110624, 0xFFB00060),
        (0x00110628, 0x0080882D),
        (0x0011062C, 0xFFBF00B0),
        (0x00110630, 0x27B00030),
        (0x00110634, 0xFFB400A0),
        (0x00110638, 0x03A0202D),
        (0x0011063C, 0xFFB20080),
        (0x00110640, 0x86260034),
        (0x00110644, 0x6A220017),
        (0x00110648, 0x6E220010),
        (0x0011064C, 0x8E230018),
        (0x00110650, 0xB3A20037),
        (0x00110654, 0xB7A20030),
        (0x00110658, 0xAFA30038),
        (0x0011065C, 0x0C04BE82),
        (0x00110660, 0x0200282D),
        (0x00110664, 0x0000202D),
        (0x001108E0, 0xDFBF00B0),
        (0x001108FC, 0x27BD00C0),
        (0x0011094C, 0x8E42000C),
        (0x00110950, 0x30420020),
        (0x00110954, 0x1040001F),
        (0x001109B0, 0x14620003),
        (0x001109B4, 0x24020006),
        (0x001109C4, 0x24020080),
        (0x001109C8, 0x240303E8),
        (0x001109CC, 0xAE2200D8),
        (0x001109D0, 0xAE2300D4),
        (0x001109D4, 0x1200001E),
        (0x001182C8, 0xFFBF0170),
        (0x001182CC, 0x0080802D),
        (0x001182D0, 0xFFB10160),
        (0x001182D4, 0x82020172),
        (0x001182E0, 0x8E02023C),
        (0x00118448, 0x8F88AA44),
        (0x00118470, 0x25438980),
        (0x0011847C, 0x8C453368),
        (0x001185E0, 0xFFB10010),
        (0x001185E4, 0x24140001),
        (0x001185E8, 0xFFBF0050),
        (0x001185EC, 0x0080882D),
        (0x001185F0, 0xFFB30030),
        (0x001185F4, 0xFFB20020),
        (0x001185F8, 0xFFB00000),
        (0x001185FC, 0x8E2701A4),
        (0x00118600, 0x14E00003),
        (0x00118604, 0x00E0182D),
        (0x00118610, 0x8F84A4C8),
        (0x00118614, 0x82220171),
        (0x00118618, 0x0080482D),
        (0x0011861C, 0x00821026),
        (0x00118620, 0x3042000F),
        (0x00118624, 0x14400089),
        (0x00118628, 0x92280171),
        (0x0011862C, 0x8E22023C),
        (0x0011876C, 0x01221026),
        (0x0011884C, 0x26320010),
        (0x00118850, 0xC6270010),
        (0x00118854, 0x8CE5000C),
        (0x00118860, 0x2484FE70),
        (0x0011889C, 0x8E220244),
        (0x00118950, 0x0280102D),
        (0x00118954, 0xDFBF0050),
        (0x00119470, 0x0C04A588),
        (0x0011B3C8, 0x27BDFFA0),
        (0x0011B3CC, 0xFFB10030),
        (0x0011B3D0, 0xFFB00020),
        (0x0011B3D4, 0x0080882D),
        (0x0011B3D8, 0xFFBF0040),
        (0x0011B3DC, 0xE7B40050),
        (0x0011B3FC, 0x82250248),
        (0x0011B400, 0x24060032),
        (0x0011B404, 0x10A6001C),
        (0x0011B5FC, 0xC6210284),
        (0x0011B614, 0xC7808280),
        (0x0011B628, 0xE6200284),
        (0x0011B630, 0xA2200248),
        (0x0011B680, 0xA2220248),
        (0x0011B684, 0xA2220248),
        (0x0011B6F0, 0x45000037),
        (0x0011B704, 0x15600032),
        (0x0011B70C, 0x8623015A),
        (0x0011B72C, 0x0220302D),
        (0x0011B734, 0x24070040),
        (0x0011B738, 0x8CA80014),
        (0x0011B75C, 0x0200282D),
        (0x0011B760, 0x8F82A964),
        (0x0011B764, 0x1040001B),
        (0x0011B768, 0xDFBF0040),
        (0x0011B774, 0x14400018),
        (0x0011B778, 0x0000102D),
        (0x0011B798, 0x1040000D),
        (0x0011B7D0, 0xDFBF0040),
        (0x0011B7D4, 0x0000102D),
        (0x0011B7DC, 0xDFB00020),
        (0x0011B7E0, 0xC7B40050),
        (0x0011B7E4, 0x03E00008),
        (0x0011B7E8, 0x27BD0060),
        (0x0011C168, 0x27BDFF20),
        (0x0011C170, 0xFFB10060),
        (0x0011C174, 0x00A0902D),
        (0x0011C17C, 0x0080882D),
        (0x0011C184, 0x46006D86),
        (0x0011C18C, 0x46007546),
        (0x0011C324, 0x8F84A4D4),
        (0x0011C32C, 0xC78C82B8),
        (0x0011C364, 0x0C0533A2),
        (0x0011C368, 0x24070003),
        (0x0011C398, 0x8E2201F0),
        (0x0011C5F4, 0x8E42000C),
        (0x0011C5F8, 0x30420020),
        (0x0011C6E4, 0x52420005),
        (0x0011D1C0, 0x27BDFF00),
        (0x0011D20C, 0xC6400210),
        (0x00129548, 0xFFB10010),
        (0x0012954C, 0x00A0982D),
        (0x00129560, 0x82020172),
        (0x00129564, 0x14400027),
        (0x001295EC, 0x9602015A),
        (0x001295F0, 0x9603015C),
        (0x001295F4, 0x00531023),
        (0x001295F8, 0x00731821),
        (0x001295FC, 0xA602015A),
        (0x00129600, 0xA603015C),
        (0x00129620, 0x27BDFD40),
        (0x0012962C, 0x0080A02D),
        (0x0012964C, 0x46006506),
        (0x001296E8, 0x0C0422E2),
        (0x00129708, 0x0280282D),
        (0x00129714, 0x0260302D),
        (0x0012971C, 0x8C880038),
        (0x00129720, 0x0080382D),
        (0x00129728, 0x85020050),
        (0x0012972C, 0x8D030054),
        (0x00129730, 0x0060F809),
        (0x00129734, 0x00822021),
        (0x00129B40, 0x8E0201A4),
        (0x00129BE8, 0x8C62000C),
        (0x00129BEC, 0x30420004),
        (0x00129BF8, 0x0220202D),
        (0x00129BFC, 0x0200282D),
        (0x00129C0C, 0x02A0582D),
        (0x00129C10, 0x0000482D),
        (0x00129C18, 0x0000502D),
        (0x00129C1C, 0xDFBF0070),
        (0x0012C338, 0x27BDFF50),
        (0x0012C5D0, 0x27BDFF40),
        (0x0012D3D8, 0x27BDFD80),
        (0x0012D4E8, 0x8C820008),
        (0x0012E330, 0x27BDFFD0),
        (0x0012EC10, 0x3C080023),
        (0x0012FA08, 0x27BDFFD0),
        (0x00130E48, 0x27BDFFD0),
        (0x00130E98, 0x2483C000),
        (0x00130F30, 0x00042400),
        (0x0013E618, 0x27BDFEE0),
        (0x0013F788, 0x2403002D),
        (0x0013FA58, 0x90820024),
        (0x0013FA5C, 0x00021100),
        (0x0013FA60, 0x00822021),
        (0x0013FA64, 0x03E00008),
        (0x0013FA68, 0x8C820004),
        (0x0013FEA0, 0xDC830008),
        (0x00147800, 0x27BDFE30),
        (0x00148C20, 0x27BDFA90),
        (0x0014CE88, 0x27BDFB20),
        (0x0014FF48, 0x0080502D),
        (0x00150128, 0xC78285C8),
        (0x0015091C, 0x3C013980),
        (0x00153F10, 0x8FAC0008),
        (0x00153F40, 0x3C020025),
        (0x00153F48, 0x24427190),
        (0x0015B080, 0x8F83C0BC),
        (0x0015B198, 0x8C840000),
        (0x0015C8F4, 0x0C047470),
        (0x0015CAE4, 0x86820348),
        (0x0015CAF4, 0xA6820348),
        (0x0015DCE4, 0x8E820024),
        (0x0015DCF4, 0x8C57001C),
        (0x0015E070, 0x86820348),
        (0x0015E20C, 0x82620024),
        (0x0015E224, 0x01761018),
        (0x0015E228, 0x00513021),
        (0x0015E22C, 0xC4C000BC),
        (0x0015E24C, 0x46010001),
        (0x0015E250, 0xE4C000BC),
        (0x0015E30C, 0xA6830348),
        (0x0015E420, 0x3042FFFF),
        (0x0015E428, 0x14400007),
        (0x0015E430, 0x24020015),
        (0x0015E434, 0x10620003),
        (0x0015E438, 0x24020014),
        (0x0015E440, 0x96630022),
        (0x0015E444, 0x240238E0),
        (0x0015E448, 0x3C03002A),
        (0x0015E4B4, 0x86630020),
        (0x0015E4C4, 0xA282032F),
        (0x0015E504, 0x50620014),
        (0x0015E508, 0xA284032F),
        (0x0015E510, 0x240238E0),
        (0x0015E558, 0x240238E0),
        (0x0015E55C, 0x3C05002A),
        (0x0015EED4, 0x9285032F),
        (0x0015EEDC, 0x10C00065),
        (0x0015EEE4, 0x0000382D),
        (0x0015EEE8, 0x24040002),
        (0x0015EEEC, 0x85220026),
        (0x0015EEF0, 0x85230028),
        (0x0015EEF8, 0x14C40006),
        (0x0015EF00, 0x8E840290),
        (0x0015EF10, 0x0040382D),
        (0x0015EF1C, 0x1462001A),
        (0x0015EF88, 0x14620015),
        (0x0015EFE0, 0x1462001F),
        (0x0015EFE4, 0x8FA30988),
        (0x0015F060, 0x00071080),
        (0x0015F064, 0x00471021),
        (0x0015F068, 0x00621821),
        (0x0015F06C, 0x1000000B),
        (0x0015F070, 0xAFA30988),
        (0x0015F09C, 0x30A300FF),
        (0x0015F128, 0x0280582D),
        (0x0015F12C, 0xAFA20010),
        (0x0015F13C, 0xAFA30018),
        (0x0015F22C, 0x0280582D),
        (0x0015F238, 0xAFA20010),
        (0x0015F248, 0xAFA30018),
        (0x0015F304, 0xAFA20010),
        (0x0015F318, 0x0280582D),
        (0x0015F324, 0xAFA30018),
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
        (0x00164894, 0x3C020020),
        (0x001648B4, 0x8E030204),
        (0x001648B8, 0x00621024),
        (0x001648C4, 0x30621000),
        (0x001648CC, 0x30622000),
        (0x001648D0, 0x3C0142C8),
        (0x001648E4, 0x3C020010),
        (0x001648E8, 0x3C014316),
        (0x00164904, 0x3C014348),
        (0x00164910, 0xE6000210),
        (0x0016491C, 0x24030003),
        (0x00164930, 0xAE0303D8),
        (0x00165170, 0x0C060D14),
        (0x001664CC, 0x8E030204),
        (0x001664D4, 0x00711825),
        (0x001664E0, 0xAE030204),
        (0x001673DC, 0x00448821),
        (0x001673E0, 0x8E233368),
        (0x001676A4, 0x3C050010),
        (0x001676E0, 0x46010000),
        (0x001676E8, 0xE62000C0),
        (0x001676EC, 0x00021880),
        (0x001676F0, 0xC62000C0),
        (0x00167708, 0x46010000),
        (0x00167710, 0xE62000C0),
        (0x00167AF4, 0x016F3821),
        (0x00167AF8, 0x0280202D),
        (0x00167B00, 0x018F3021),
        (0x00167B08, 0x0260582D),
        (0x0016A94C, 0x2C620003),
        (0x0016A950, 0x14400008),
        (0x0016DD24, 0x0260282D),
        (0x0016DD28, 0x8F84C0D0),
        (0x0016DD30, 0xAE030020),
        (0x0016DD34, 0x8C430038),
        (0x0016DD38, 0x3C050031),
        (0x0016DD3C, 0x8F84C0D0),
        (0x0016DD40, 0x24A5A9C0),
        (0x0016DD48, 0xAE030024),
        (0x0016DD4C, 0x8C430038),
        (0x0016DD60, 0xAE030028),
        (0x0017A4C0, 0xDC440048),
        (0x0017B3C4, 0x00828021),
        (0x0017B4B0, 0xA2030055),
        (0x0017B598, 0x00000000),
        (0x0017B5B4, 0xC60000A8),
        (0x0017B5B8, 0xC60100C0),
        (0x0017B5BC, 0xE60000A4),
        (0x0017B5C0, 0xE60100BC),
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
        (0x0017E5D8, 0x1000005B),
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
        (0x0017E9AC, 0x84450348),
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
        (0x00183404, 0xACC20008),
        (0x00183414, 0xAC82000C),
        (0x00183450, 0x3C06002B),
        (0x00183470, 0x2528E4F0),
        (0x00183474, 0x2442BCE8),
        (0x00183478, 0x2463BD28),
        (0x0018347C, 0x24A56B60),
        (0x00183480, 0x24C6BCF8),
        (0x00183484, 0x24070003),
        (0x0018C360, 0x27BDFE70),
        (0x0018FDF8, 0x27BDFE50),
        (0x0018FED0, 0xA250006A),
        (0x0018FF14, 0x8242006A),
        (0x00191130, 0x27BDFBF0),
        (0x00196D68, 0x27BDFF50),
        (0x0019A468, 0x00832021),
        (0x0019A46C, 0x00042C02),
        (0x0019A470, 0xAF84A49C),
        (0x0019A474, 0x00A2001B),
        (0x0019A480, 0x00001810),
        (0x0019A484, 0x1460000C),
        (0x0019F89C, 0x24080004),
        (0x0019F8AC, 0xAFA80038),
        (0x0019F8F8, 0x0220582D),
        (0x001A1124, 0x8E220240),
        (0x001A1130, 0x00001810),
        (0x001A1138, 0x00431021),
        (0x001A113C, 0xAE220290),
        (0x001A1140, 0x84C40090),
        (0x001A1144, 0x00E0F809),
        (0x001A1B68, 0x27BDFE30),
        (0x001A1B90, 0x8E620048),
        (0x001A1B94, 0x0062102A),
        (0x001A1BA0, 0x8E64004C),
        (0x001A1DE8, 0x24020032),
        (0x001A1F7C, 0x0C06FF34),
        (0x001A1FB4, 0x0C06FF0C),
        (0x001A2024, 0x12600006),
        (0x001A2028, 0x24050003),
        (0x001A202C, 0x8E620038),
        (0x001A2260, 0x27BDFF90),
        (0x001A22C4, 0x0C042C56),
        (0x001A22D0, 0x2A04000A),
        (0x001A22D4, 0x24633140),
        (0x001A22D8, 0xAE630038),
        (0x001A22F4, 0x8F82A4C8),
        (0x001A22F8, 0xAE710050),
        (0x001A22FC, 0x00521021),
        (0x001A2300, 0xAE70004C),
        (0x001A2308, 0xAE620048),
        (0x001A26C0, 0x27BDFFD0),
        (0x001A3B50, 0x24030004),
        (0x001A3B68, 0xAFA30038),
        (0x001A3BA0, 0x0220582D),
        (0x001B1250, 0x27BDFEE0),
        (0x001B1450, 0x24636798),
        (0x001B156C, 0x0080882D),
        (0x001B1790, 0xAE030038),
        (0x001BC004, 0x24140001),
        (0x001BC188, 0xAFA00030),
        (0x001BC1A4, 0x0C06FE36),
        (0x001BC214, 0xAFB40030),
        (0x001BC23C, 0x0C06FE36),
        (0x001BCBEC, 0x8E220240),
        (0x001BCBF8, 0x00001810),
        (0x001BCC00, 0x00431021),
        (0x001BCC04, 0xAE2202A8),
        (0x001BCC08, 0x84C40090),
        (0x001BCC0C, 0x00E0F809),
        (0x001BDA00, 0x86420134),
        (0x001BDA04, 0x2842EC79),
        (0x001BDA08, 0x14400003),
        (0x001BDA0C, 0x96430134),
        (0x001BDA10, 0x2462FF38),
        (0x001BDA14, 0xA6420134),
        (0x001BDA18, 0xC6400124),
        (0x001BDA20, 0xC781958C),
        (0x001BDA24, 0x46010001),
        (0x001BDA28, 0xE6400124),
        (0x001BDA44, 0x0C043880),
        (0x001BE12C, 0x44806000),
        (0x001BE138, 0x0200202D),
        (0x001BE13C, 0x86030174),
        (0x001BED28, 0x27BDFF70),
        (0x001BED34, 0x0080802D),
        (0x001BED80, 0x86040034),
        (0x001BED84, 0x46000000),
        (0x001BED88, 0xC6010010),
        (0x001BED98, 0xE6010010),
        (0x001BED9C, 0x46000000),
        (0x001BEDA0, 0xC6010014),
        (0x001BEFCC, 0x0C04BB04),
        (0x001BF000, 0x27A50020),
        (0x001BF008, 0x8C47000C),
        (0x001BF00C, 0x10000008),
        (0x001BF010, 0xAE000040),
        (0x001BF0D0, 0x27BDFF90),
        (0x001BF0F0, 0x8E040050),
        (0x001BF3C4, 0x8E02004C),
        (0x001BF3C8, 0x0044102A),
        (0x001BF5A0, 0x27BDFFB0),
        (0x001BF608, 0x24427470),
        (0x001BF61C, 0x246300FA),
        (0x001BF63C, 0xE6010048),
        (0x001BF648, 0xAE03004C),
        (0x001BF66C, 0xA2220248),
        (0x001BF670, 0x0200102D),
        (0x001BF674, 0xE620028C),
        (0x001BF690, 0x27BDFFA0),
        (0x001BF700, 0x246374F0),
        (0x001BF710, 0x0C056C20),
        (0x001BF720, 0xA6110034),
        (0x001BF724, 0x2463012C),
        (0x001BF734, 0x0C0425B2),
        (0x001BF738, 0xAE030040),
        (0x001BF878, 0x86060134),
        (0x001BF87C, 0x03A0202D),
        (0x001BF880, 0x0C04BCBE),
        (0x001BF884, 0x0000282D),
        (0x001BF8F4, 0x8FB10110),
        (0x001BF8F8, 0x24120004),
        (0x001BF918, 0x00118E00),
        (0x001BF930, 0x00118E03),
        (0x001BF984, 0xAFB20038),
        (0x001BF9C4, 0x27A90070),
        (0x001BF9CC, 0x3C030031),
        (0x001BF9D4, 0x24637570),
        (0x001BF9D8, 0xA2110136),
        (0x001BF9E4, 0xDFBF00D0),
        (0x001BF9E8, 0xDFB400C0),
        (0x001CDA70, 0xFFBF01C0),
        (0x001CEA24, 0x24040138),
        (0x001CEA30, 0x24100004),
        (0x001CEA58, 0x0000282D),
        (0x001CEAC4, 0xAFB00038),
        (0x001CEAE4, 0x0240582D),
        (0x001CEAE8, 0x3C030032),
        (0x001CEAEC, 0xAE7000D8),
        (0x001CEAF0, 0x24639CE8),
        (0x001CEAF4, 0xAE630038),
        (0x001CEAF8, 0x8642015A),
        (0x001CEB68, 0x24040138),
        (0x001CEBB0, 0x24100004),
        (0x001CEC04, 0x0000282D),
        (0x001CEC44, 0xAFB00038),
        (0x001CEC64, 0x0240582D),
        (0x001CEC68, 0x3C030032),
        (0x001CEC6C, 0xAE3000D8),
        (0x001CEC70, 0x24639CE8),
        (0x001CEC74, 0xAE230038),
        (0x001CEC78, 0x8F85A49C),
        (0x001CFAE8, 0x27BDFFD0),
        (0x001CFB00, 0x0C04B676),
        (0x001D0610, 0x0220202D),
        (0x001D0614, 0x86230174),
        (0x001D0828, 0x8E220240),
        (0x001D082C, 0x00001810),
        (0x001D0834, 0x00431021),
        (0x001D0838, 0x100003E8),
        (0x001D083C, 0xAE2202A0),
        (0x001D0BAC, 0x8E280240),
        (0x001D0BFC, 0x00001010),
        (0x001D0C04, 0x01021021),
        (0x001D0C08, 0x100001DE),
        (0x001D0C0C, 0xAE2202A4),
        (0x001D2230, 0x27BDFF30),
        (0x001D4AD8, 0xAE0002BC),
        (0x001D4AE0, 0xAE0002C0),
        (0x001D5250, 0x8C44000C),
        (0x001D5260, 0xFE420148),
        (0x001D5C08, 0xC78C9A54),
        (0x001D5C14, 0x0240202D),
        (0x001D5C18, 0x86430174),
        (0x001D5DC0, 0xAE40017C),
        (0x001D62F4, 0x0C0765F0),
        (0x001D7398, 0x0C0765F0),
        (0x001D78E0, 0x0C0765F0),
        (0x001D97C8, 0x24020001),
        (0x001D97CC, 0x8C82017C),
        (0x001D97D0, 0x00021043),
        (0x001D97D4, 0x38420001),
        (0x001D97D8, 0x03E00008),
        (0x001D97DC, 0x30420001),
        (0x001D97E0, 0x14620003),
        (0x001D97E4, 0x8C82017C),
        (0x001D97E8, 0x1000FFFB),
        (0x001D97EC, 0x38420001),
        (0x001D97F0, 0x00021083),
        (0x001D97F4, 0x38420001),
        (0x001D97F8, 0x03E00008),
        (0x001D97FC, 0x30420001),
        (0x001DA02C, 0x86230174),
        (0x001DA24C, 0x8E220290),
        (0x001DA250, 0x1440001C),
        (0x001DA920, 0xAFAB0038),
        (0x001DA954, 0x0220582D),
        (0x001DBFD0, 0x8E420240),
        (0x001DBFDC, 0x00001810),
        (0x001DBFE4, 0x00431021),
        (0x001DBFE8, 0xAE4202A0),
        (0x001DBFEC, 0x84C40090),
        (0x001DDF98, 0x8E220240),
        (0x001DDF9C, 0x00002010),
        (0x001DDFA4, 0x00441021),
        (0x001DDFA8, 0xAE220298),
        (0x001DE058, 0x3C0143FA),
        (0x001DE05C, 0x44816000),
        (0x001DE07C, 0x02C0202D),
        (0x001DE080, 0x28420005),
        (0x001DE084, 0x10400045),
        (0x001DE088, 0x8F86A49C),
        (0x001DE08C, 0x3C130019),
        (0x001DE090, 0x3673660D),
        (0x001DE094, 0x3C123C6E),
        (0x001DE098, 0x00D33018),
        (0x001DE09C, 0x3652F35F),
        (0x001DE0A0, 0x6A270017),
        (0x001DE0A4, 0x6E270010),
        (0x001DE0A8, 0x8E280018),
        (0x001DE0AC, 0xB3A70057),
        (0x001DE0B0, 0xB7A70050),
        (0x001DE0B4, 0xAFA80058),
        (0x001DE0B8, 0x0220202D),
        (0x001DE0BC, 0x27A50050),
        (0x001DE0C0, 0x3C0141F0),
        (0x001DE0C4, 0x4481A800),
        (0x001DE0C8, 0x00D23021),
        (0x001DE0CC, 0x00D31818),
        (0x001DE0D0, 0x00063403),
        (0x001DE0D4, 0x00721021),
        (0x001DE0D8, 0x00023C02),
        (0x001DE0DC, 0xAF82A49C),
        (0x001DE0E0, 0x0C04635C),
        (0x001DE0E4, 0x30E70001),
        (0x001DE0E8, 0x0040802D),
        (0x001DE0EC, 0x0C04C3CC),
        (0x001DE0F0, 0x0200202D),
        (0x001DE0F4, 0x46150002),
        (0x001DE0F8, 0xC6340010),
        (0x001DE0FC, 0x0200202D),
        (0x001DE100, 0x0C04C3A6),
        (0x001DE104, 0x4600A500),
        (0x001DE108, 0x46150002),
        (0x001DE10C, 0xC6210014),
        (0x001DE110, 0xC6220018),
        (0x001DE114, 0x240402A0),
        (0x001DE118, 0xE7B40050),
        (0x001DE11C, 0x46000840),
        (0x001DE120, 0xE7A20058),
        (0x001DE124, 0x0C04BB04),
        (0x001DE128, 0xE7A10054),
        (0x001DE12C, 0x0040202D),
        (0x001DE130, 0x10800011),
        (0x001DE134, 0x8F82A49C),
        (0x001DE138, 0x27A50060),
        (0x001DE13C, 0x86260034),
        (0x001DE140, 0x0000382D),
        (0x001DE144, 0x00531818),
        (0x001DE148, 0x6BA80057),
        (0x001DE14C, 0x6FA80050),
        (0x001DE150, 0x8FA90058),
        (0x001DE154, 0xB3A80067),
        (0x001DE158, 0xB7A80060),
        (0x001DE15C, 0xAFA90068),
        (0x001DE160, 0x00721021),
        (0x001DE164, 0x00024402),
        (0x001DE168, 0xAF82A49C),
        (0x001DE16C, 0x31080003),
        (0x001DE170, 0x0C068A82),
        (0x001DE174, 0x25080001),
        (0x001DE17C, 0x8E230240),
        (0x001DE180, 0x0043102A),
        (0x001DEDE8, 0x27BDFF40),
        (0x001DEDF4, 0x0080882D),
        (0x001DEE10, 0x10A0000B),
        (0x001DEE44, 0x24120032),
        (0x001DEE50, 0x8C44000C),
        (0x001DEEDC, 0x0C04FDE2),
        (0x001DEF00, 0x26E46CB0),
        (0x001DEF28, 0x0C053FD2),
        (0x001DFA2C, 0x0C04B8CC),
        (0x001DFA70, 0x27BDFF20),
        (0x001DFAB0, 0x0C07CAA0),
        (0x001DFAB4, 0x8E84029C),
        (0x001DFABC, 0x8E830294),
        (0x001DFAC4, 0x28440064),
        (0x001DFF64, 0x0C04705A),
        (0x001E0830, 0x3C04005E),
        (0x001E083C, 0x24846CB0),
        (0x001E0CA8, 0x27BDFFB0),
        (0x001E0D58, 0x27BDFF90),
        (0x001E0DC4, 0x0C0434BC),
        (0x001E0DE0, 0xC7829C50),
        (0x001E4778, 0x8E420044),
        (0x001E477C, 0x24420001),
        (0x001E4780, 0xAE420044),
        (0x001E479C, 0x0C043880),
        (0x001E47A4, 0x14400006),
        (0x001E47AC, 0x8E420044),
        (0x001E47B0, 0x284201F5),
        (0x001E49C8, 0x27BDFF60),
        (0x001E5450, 0xFFBF01C0),
        (0x001E5874, 0x8E420290),
        (0x001E58DC, 0x8E420290),
        (0x001E663C, 0x8642015A),
        (0x001E664C, 0xE7B50058),
        (0x001E6698, 0x1040000A),
        (0x001E6778, 0x284201C2),
        (0x001E677C, 0x10400008),
        (0x001E6810, 0x2622FFFF),
        (0x001E6814, 0x2C420005),
        (0x001E6848, 0x24040078),
        (0x001E684C, 0x0C04BB04),
        (0x001E6868, 0x24080001),
        (0x001E686C, 0x0240482D),
        (0x001E6870, 0x0000502D),
        (0x001E6878, 0x0000582D),
        (0x001E687C, 0x100000F6),
        (0x001E6880, 0x26310001),
        (0x001E6884, 0x2622FFF8),
        (0x001E6888, 0x2C420002),
        (0x001E6894, 0x24040100),
        (0x001E68E4, 0x0C04BB04),
        (0x001E6964, 0x03C0282D),
        (0x001E6968, 0x0240382D),
        (0x001E696C, 0x24080001),
        (0x001E6998, 0x00063403),
        (0x001E699C, 0x26310001),
        (0x001E69E8, 0x24630001),
        (0x001E69EC, 0xAFA3010C),
        (0x001E6BAC, 0x12000024),
        (0x001E6BB0, 0x8FA3010C),
        (0x001E6C34, 0x0C0425B2),
        (0x001E6C38, 0x0200202D),
        (0x001E6C40, 0x28620003),
        (0x001E6C44, 0x5440FF68),
        (0x001E6C48, 0x8E4201A4),
        (0x001E7B54, 0x8E020240),
        (0x001E7B5C, 0x00001810),
        (0x001E7B64, 0x24630096),
        (0x001E7C78, 0x8E020240),
        (0x001E7C80, 0x00001810),
        (0x001E7C88, 0x00431021),
        (0x001E7C8C, 0x1000001E),
        (0x001E7C90, 0xAE020290),
        (0x001E8320, 0x27BDFFB0),
        (0x001E8380, 0x0C04B200),
        (0x001E8384, 0x0000382D),
        (0x001E8388, 0x3C030032),
        (0x001E838C, 0x2463C3C8),
        (0x001E8390, 0xAE030038),
        (0x001E83A8, 0xAE020050),
        (0x001E83AC, 0x0C0425B2),
        (0x001F2A84, 0x2402FFFF),
        (0x001F2ABC, 0x120200A1),
        (0x002027F4, 0x0C060CE2),
        (0x002260C0, 0x3C020030),
        (0x002337FC, 0x001A2068),
        (0x00233804, 0x001BF410),
        (0x0023381C, 0x001E0748),
        (0x00257190, 0x0000000A),
        (0x00257194, 0x00000019),
        (0x00257198, 0x00000032),
        (0x0025719C, 0x0000004B),
        (0x002A79C8, 0x0030BDA0),
        (0x002A79D8, 0x0030BDC0),
        (0x002A79E8, 0x0030BDE0),
        (0x002A79F8, 0x0030BE00),
        (0x002A7A08, 0x0030BE20),
        (0x002A7A18, 0x0030BE40),
        (0x002A7A28, 0x0030BE60),
        (0x002A7A2C, 0x0030BE70),
        (0x003058AC, 0x0010F5C8),
        (0x003058C4, 0x00110900),
        (0x00305FDC, 0x00129778),
        (0x00307C04, 0x001628F0),
        (0x0030A0F4, 0x0016AC68),
        (0x00312348, 0x00000000),
        (0x0031234C, 0x0010DEF0),
        (0x00312350, 0x00000000),
        (0x00312608, 0x00000000),
        (0x0031260C, 0x0010DEF0),
        (0x00312610, 0x00000000),
        (0x00312C00, 0x00000000),
        (0x00312C04, 0x0010DEF0),
        (0x00312C08, 0x00000000),
        (0x0031314C, 0x001A26C0),
        (0x00313164, 0x001A1B68),
        (0x0031316C, 0x001A2258),
        (0x0031317C, 0x0012E418),
        (0x003132C8, 0x00000000),
        (0x003132CC, 0x0010DEF0),
        (0x003132D0, 0x00000000),
        (0x00313368, 0x00000000),
        (0x0031336C, 0x0010DEF0),
        (0x00313370, 0x00000000),
        (0x00313818, 0x00000000),
        (0x0031381C, 0x0010DEF0),
        (0x00313820, 0x00000000),
        (0x00313BB8, 0x00000000),
        (0x00313BBC, 0x0010DEF0),
        (0x00313BC0, 0x00000000),
        (0x00313F08, 0x00000000),
        (0x00313F0C, 0x0010DEF0),
        (0x00313F10, 0x00000000),
        (0x003141B0, 0x00000000),
        (0x003141B4, 0x0010DEF0),
        (0x003141B8, 0x00000000),
        (0x003145A8, 0x00000000),
        (0x003145AC, 0x0010DEF0),
        (0x003145B0, 0x00000000),
        (0x00314990, 0x00000000),
        (0x00314994, 0x0010DEF0),
        (0x00314998, 0x00000000),
        (0x00314C80, 0x00000000),
        (0x00314C84, 0x0010DEF0),
        (0x00314C88, 0x00000000),
        (0x00314D30, 0x00000000),
        (0x00314D34, 0x0010DEF0),
        (0x00314D38, 0x00000000),
        (0x003167BC, 0x001B0F98),
        (0x003167D4, 0x001B0EB8),
        (0x0031747C, 0x001BF058),
        (0x00317494, 0x001BF0D0),
        (0x0031749C, 0x001BF598),
        (0x003174FC, 0x001BFB30),
        (0x00317514, 0x001BED28),
        (0x003175F4, 0x001BFC30),
        (0x00317614, 0x001BE000),
        (0x00317718, 0x00000000),
        (0x0031771C, 0x0010DEF0),
        (0x00317720, 0x00000000),
        (0x00317A10, 0x00000000),
        (0x00317A14, 0x0010DEF0),
        (0x00317A18, 0x00000000),
        (0x00317E08, 0x00000000),
        (0x00317E0C, 0x0010DEF0),
        (0x00317E10, 0x00000000),
        (0x00318040, 0x00000000),
        (0x00318044, 0x0010DEF0),
        (0x00318048, 0x00000000),
        (0x00318408, 0x00000000),
        (0x0031840C, 0x0010DEF0),
        (0x00318410, 0x00000000),
        (0x00318698, 0x00000000),
        (0x0031869C, 0x0010DEF0),
        (0x003186A0, 0x00000000),
        (0x00318900, 0x00000000),
        (0x00318904, 0x0010DEF0),
        (0x00318908, 0x00000000),
        (0x00318B70, 0x00000000),
        (0x00318B74, 0x0010DEF0),
        (0x00318B78, 0x00000000),
        (0x00318DF0, 0x00000000),
        (0x00318DF4, 0x0010DEF0),
        (0x00318DF8, 0x00000000),
        (0x00319160, 0x00000000),
        (0x00319164, 0x0010DEF0),
        (0x00319168, 0x00000000),
        (0x003193E8, 0x00000000),
        (0x003193EC, 0x0010DEF0),
        (0x003193F0, 0x00000000),
        (0x00319880, 0x00000000),
        (0x00319884, 0x0010DEF0),
        (0x00319888, 0x00000000),
        (0x00319CF4, 0x001CFAE8),
        (0x00319D0C, 0x001CD4A0),
        (0x00319D14, 0x001CF830),
        (0x00319D24, 0x00110900),
        (0x00319D28, 0x00000000),
        (0x00319D2C, 0x0012E410),
        (0x00319DF0, 0x00000000),
        (0x00319DF4, 0x0010DEF0),
        (0x00319DF8, 0x00000000),
        (0x0031AEC8, 0x00000000),
        (0x0031AECC, 0x0010DEF0),
        (0x0031AED0, 0x00000000),
        (0x0031AED4, 0x001DABE8),
        (0x0031B53C, 0x001DF9A0),
        (0x0031B554, 0x001DFF20),
        (0x0031B55C, 0x001DFA70),
        (0x0031B5DC, 0x001DF620),
        (0x0031B5F4, 0x001E0EC8),
        (0x0031B5FC, 0x001DF6D0),
        (0x0031B65C, 0x001DF120),
        (0x0031B674, 0x001DF3B0),
        (0x0031B67C, 0x001DF1F0),
        (0x0031B7F8, 0x00000000),
        (0x0031B7FC, 0x0010DEF0),
        (0x0031B800, 0x00000000),
        (0x0031B804, 0x001DC250),
        (0x0031BAD8, 0x00000000),
        (0x0031BADC, 0x0010DEF0),
        (0x0031BAE0, 0x00000000),
        (0x0031BD90, 0x00000000),
        (0x0031BD94, 0x0010DEF0),
        (0x0031BD98, 0x00000000),
        (0x0031BFE8, 0x00000000),
        (0x0031BFEC, 0x0010DEF0),
        (0x0031BFF0, 0x00000000),
        (0x0031C3D4, 0x001E8418),
        (0x0031C3EC, 0x001E4748),
        (0x0031C3F4, 0x001E82A0),
        (0x0031C404, 0x001E8258),
        (0x0031C890, 0x00000000),
        (0x0031C894, 0x0010DEF0),
        (0x0031C898, 0x00000000),
        (0x00322228, 0x3F8CCCCD),
        (0x00322538, 0x3F999999)
    };

    /// <summary>The hero's melee hit: the game's call of the hit function, or Corrosive's (a jal either way).</summary>
    public const int MeleeCallVa = 0x00167B04;

    /// <summary>The game's strings the code uses (sounds, fallback icons, the HUD's damage line's key).</summary>
    public static readonly (int Va, string Text)[] Strings =
    {
        (0x003033F8, "spel_cas_fireball.anm"),
        (0x00303428, "spel_cas_otiluk.anm"),
        (0x00303470, "spel_cas_firearrow.anm"),
        (0x003034B8, "spel_hit_explarrow.anm"),
        (0x003034F0, "spel_elctric_afteraffct.anm"),
        (0x00303528, "impct_mtl_stone%d.anm"),
        (0x00303B30, "spel_dsrptn_afteraffct.anm"),
        (0x00307928, "spel_cas_clangadin.anm"),
        (0x00307958, "spel_cas_bullstart.anm"),
        (0x00307970, "spel_cas_whirlwind.anm"),
        (0x00307B38, "spel_cas_copstart.anm"),
        (0x003095F8, "spel_cas_bullend.anm"),
        (0x0030A8F0, "icon_fireball.tex"),
        (0x0030A928, "icon_magicmissile.tex"),
        (0x0030A940, "icon_melfsacidarrow.tex"),
        (0x0030A970, "icon_flamingarrow.tex"),
        (0x0030A9D8, "icon_coneofcold.tex"),
        (0x0030A9F0, "icon_protection.tex"),
        (0x0030AA20, "icon_whirlwind.tex"),
        (0x0030AA78, "icon_powerattack.tex"),
        (0x0030AA90, "icon_morden.tex"),
        (0x0030AAD0, "icon_sphere.tex"),
        (0x0030BE60, "axe4.vif"),
        (0x0030BE70, "axe4.tex"),
        (0x0030E770, "PowerSpellDamageWeapon"),
        (0x0030E7D8, "SpellDamage"),
        (0x003110D0, "spel_cas_copend.anm"),
        (0x00311150, "spel_cas_mm.anm"),
        (0x00311170, "spel_cas_acid.anm"),
        (0x003111F8, "expl_ice%d.anm"),
        (0x00315E20, "spel_cas_mord.anm"),
        (0x00315E68, "spel_hit_mord.anm"),
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
    /// An earlier version with Improved Enemy AI (18, 22, 24 and 25, which WorldExplorer wrote, and the monster stages' test
    /// versions 19 to 21 and 23): its eight spell blocks where version 26's are, with version 26's layout, so its settings are
    /// read where version 26 keeps them; its ninth block (<see cref="Data9Size"/> bytes, here with the AI off: on and its parts
    /// where version 26 has them; <see cref="AiParts"/> all of that version's); its tenth block where version 26's is
    /// (<see cref="Data10Size"/> bytes, the same whether the AI is on or off; 22 to 25 only, else 0 bytes); no eleventh block
    /// and nothing in the second pool. A file of one may have the AI alone. Its hooks (the spells' masks as version 26's,
    /// <see cref="EnemyAiMask"/> the AI's), its code's pieces with their SHA-256, its vtable copies and its cast table's
    /// place.
    /// </summary>
    public sealed record EarlierAiVersion(int Version, string Test, int AiParts, int CastTableVa,
                                          (int Va, uint Game, uint Patched, int Spells)[] Hooks, (int Va, int Length)[] Code,
                                          string CodeSha256, (int Va, int GameVa, (int At, uint Word)[] Words)[] VTables,
                                          int Data9Size, string Data9Base64, int Data10Size, string Data10Base64);

    /// <summary>Versions 25, 24, 23, 22, 21, 20, 19 and 18, the newest first.</summary>
    public static readonly EarlierAiVersion[] EarlierAiVersions =
    {
        new(25, "Improved Enemy AI with nine parts in three cards (Aggressive Bosses the first time), as WorldExplorer wrote it", 0x1FF, 0x00207A10,
            new (int Va, uint Game, uint Patched, int Spells)[]
            {
            (0x0010F4AC, 0x1462000B, 0x0808236C, 0x480),
            (0x0010F648, 0x12000207, 0x0806E55C, 0x1480),
            (0x001109C0, 0x14620004, 0x080666D9, 0x80),
            (0x001182C0, 0x27BDFE80, 0x0804F4F9, 0x8000),
            (0x001182C4, 0xFFB00150, 0x00000000, 0x8000),
            (0x001185D8, 0x27BDFFA0, 0x08084328, 0x8000),
            (0x001185DC, 0xFFB40040, 0x00000000, 0x8000),
            (0x0011B758, 0x0100F809, 0x0C083132, 0x600),
            (0x0011B7D8, 0xDFB10030, 0x08081ED7, 0x4000),
            (0x0011C168, 0x27BDFF20, 0x08084061, 0x8000),
            (0x0011C16C, 0xFFB20070, 0x00000000, 0x8000),
            (0x00129540, 0x27BDFFB0, 0x08084280, 0x8000),
            (0x00129544, 0xFFB30030, 0x00000000, 0x8000),
            (0x00129778, 0x27BDFFF0, 0x0804F486, 0x8000),
            (0x0012977C, 0x68C20007, 0x00000000, 0x8000),
            (0x00129C14, 0x0C047E0E, 0x0C083060, 0x8000),
            (0x0015E220, 0xC661002C, 0x0C059B8A, 0x480),
            (0x0015E424, 0x2C420003, 0x2C420004, 0x80),
            (0x0015E43C, 0x54620078, 0x08072EC6, 0x400),
            (0x0015E50C, 0x10000013, 0x0804AFD1, 0x480),
            (0x0015F160, 0x0C043C8C, 0x0C08207D, 0x1000),
            (0x0015F278, 0x0C043C8C, 0x0C08207D, 0x1000),
            (0x0015F354, 0x0C043C8C, 0x0C08207D, 0x1000),
            (0x00161EF8, 0x2C830017, 0x2C83002A, 0x7FFF),
            (0x00161F00, 0x3C020030, 0x3C020020, 0x7FFF),
            (0x00161F08, 0x24427BC0, 0x24427A10, 0x7FFF),
            (0x001676E4, 0x0C05AA52, 0x0C04F3F6, 0x20),
            (0x00167AFC, 0x0240282D, 0x0C04A712, 0x18),
            (0x0016DD2C, 0x0C04EB96, 0x0C0836C2, 0x80),
            (0x0016DD44, 0x0C04EB96, 0x0C04C150, 0x7FFF),
            (0x0017A4BC, 0x0C05ED8E, 0x0C07A156, 0x7FFF),
            (0x0017B594, 0x0C059CE4, 0x0C068A6C, 0x20),
            (0x0017E52C, 0x14830009, 0x0804625E, 0x7FCE),
            (0x0017EA44, 0x8C440000, 0x0C083175, 0x7FCE),
            (0x0017EAB4, 0x8C440000, 0x0C083175, 0x7FCE),
            (0x0018331C, 0x0C058ADE, 0x0C070DBA, 0x7FFF),
            (0x0019A464, 0x24020014, 0x0808139F, 0x8000),
            (0x0019F8F4, 0x0C043C8C, 0x0C040ADB, 0x8000),
            (0x001A1134, 0x24630032, 0x08081378, 0x8000),
            (0x001A3B9C, 0x0C043C8C, 0x0C040AD7, 0x8000),
            (0x001B177C, 0x3C030031, 0x8E230038, 0x1),
            (0x001B1784, 0x24636798, 0x24630080, 0x1),
            (0x001BCBFC, 0x24630096, 0x08081385, 0x8000),
            (0x001BED7C, 0x0C04C3CC, 0x0C0822B4, 0x100),
            (0x001BED94, 0x0C04C3A6, 0x0C0822BA, 0x100),
            (0x001BEFD0, 0x24040058, 0x24040060, 0x100),
            (0x001BF004, 0x0C06FD68, 0x0C081D22, 0x100),
            (0x001BF9C0, 0x0C043C8C, 0x0C087284, 0x8000),
            (0x001BF9E0, 0xAE030038, 0x0808728A, 0x8000),
            (0x001D0830, 0x24630096, 0x08081CF4, 0x8000),
            (0x001D0C00, 0x244201F4, 0x0806EC36, 0x8000),
            (0x001D97C0, 0x80830294, 0x080813AA, 0x8000),
            (0x001D97C4, 0x54600006, 0x00000000, 0x8000),
            (0x001DA950, 0x0C043C8C, 0x0C040AD9, 0x8000),
            (0x001DBFE0, 0x24630064, 0x08081392, 0x8000),
            (0x001DDFA0, 0x24840064, 0x08083094, 0x8000),
            (0x001DE078, 0x0C04B4F6, 0x08083FE5, 0x8000),
            (0x001DE178, 0x8E2202E0, 0x080836E4, 0x8000),
            (0x001E6648, 0x284201C2, 0x0C0433CD, 0x8000),
            (0x001E6774, 0x8642015A, 0x0C0433CC, 0x8000),
            (0x001E7B60, 0x10000049, 0x08084391, 0x8000),
            (0x001E7C84, 0x246300C8, 0x0808438F, 0x8000),
            (0x0030A104, 0x001663F8, 0x001D32D8, 0xC),
            (0x0030A11C, 0x0015C2F0, 0x002089C0, 0x8),
            (0x0031AE64, 0x001D9FB0, 0x001B8DA0, 0x8000),
            (0x0031B794, 0x001DB590, 0x001B8DAC, 0x8000)
            },
            new (int Va, int Length)[]
            {
            (0x00102910, 0x264), (0x00102D80, 0x280), (0x0010C338, 0x394), (0x0010CD50, 0x1FC),
            (0x00118978, 0x3F0), (0x00129318, 0x21C), (0x00129C48, 0x11C), (0x0012A710, 0x190),
            (0x0012BAE8, 0x4A8), (0x0012CD30, 0x350), (0x0012F790, 0x278), (0x00130540, 0x1C4),
            (0x00131170, 0x148), (0x00132018, 0x154), (0x00132D90, 0x64C), (0x00134CE0, 0x118),
            (0x00134F18, 0x1B0), (0x0013CDF8, 0x208), (0x0013D218, 0x214), (0x00166E28, 0x198),
            (0x0016ACB8, 0x1C4), (0x00191A20, 0x2F8), (0x00196AB8, 0x144), (0x00198968, 0x13C),
            (0x00199A60, 0x138), (0x001A29B0, 0x50), (0x001AE930, 0xF4), (0x001B8DA0, 0x170),
            (0x001B9570, 0x114), (0x001B9E68, 0xE4), (0x001BAFF8, 0x104), (0x001C36E8, 0x188),
            (0x001CBA38, 0x10C), (0x001D32D8, 0x144), (0x001E8558, 0x234), (0x001FF340, 0x198),
            (0x00204DE0, 0xFC), (0x002068C8, 0x1F0), (0x002071C0, 0x234), (0x00207488, 0xAC),
            (0x00207AB8, 0x13C), (0x00208160, 0x1B8), (0x002089C0, 0x90), (0x00208AD0, 0x40),
            (0x00208DB0, 0x80), (0x0020ACB0, 0x80), (0x0020C180, 0xF4), (0x0020C4C8, 0x14C),
            (0x0020DA88, 0x13C), (0x0020E928, 0x210), (0x0020F958, 0x64), (0x0020FF94, 0x24),
            (0x00210058, 0x240), (0x00210A00, 0x204), (0x00210CA0, 0x1C0), (0x0021BEE8, 0xE0),
            (0x0021CA10, 0x58), (0x002222C0, 0x58), (0x00224C68, 0x48)
            },
            "27e7dd2301ec2d807cd4b55ca051d274b7c24c96a6f027e540fd51e41c51150b",
            new (int Va, int GameVa, (int At, uint Word)[] Words)[]
            {
            (0x001A9A40, 0x00316718, new (int, uint)[] {  }),
            (0x001A9AC0, 0x00316798, new (int, uint)[] { (0x24, 0x00196AB8), (0x3C, 0x0012E418) }),
            (0x00208CB0, 0x0031B650, new (int, uint)[] { (0x0C, 0x00208288), (0x24, 0x0010CD50) }),
            (0x00207540, 0x0031B5D0, new (int, uint)[] { (0x0C, 0x00207274) }),
            (0x0020C100, 0x0031C3C8, new (int, uint)[] { (0x3C, 0x0012F998) }),
            (0x0020AC30, 0x00316798, new (int, uint)[] { (0x24, 0x0012A710), (0x3C, 0x0012E418) }),
            (0x00208D30, 0x00317470, new (int, uint)[] { (0x0C, 0x0010C634), (0x24, 0x001FF40C) }),
            (0x001A28B0, 0x0031B530, new (int, uint)[] { (0x0C, 0x0020ACB0), (0x24, 0x00102D80) }),
            (0x001A2930, 0x0031B5B0, new (int, uint)[] {  }),
            (0x0020C080, 0x00313140, new (int, uint)[] { (0x24, 0x0016ACB8) }),
            (0x00208A50, 0x00319CE8, new (int, uint)[] { (0x24, 0x00191BC4), (0x2C, 0x00206990), (0x3C, 0x002068C8) }),
            (0x0021C990, 0x00305888, new (int, uint)[] { (0x24, 0x0021CA40), (0x2C, 0x0020F958) })
            },
            0x3A0,
            "RkVBORkAAAAAAAAAAAAAAGQAAAAAABBASOH6PgAA8EEAAEhDAADAQQAAQEEAAKBBAABAQgAAAEAyAAAAAADIQwAAyEP0AQAAAAB6RAAAAAAAAIA/"
            + "AAAAAPQENT/0BDU/AAAAAAAAgD/0BDW/9AQ1PwAAgL8AAAAA9AQ1v/QENb8AAAAAAACAv/QENT/0BDW/gIMxABCGMQB4iDEA2JAxAIB9MQAIvTEA"
            + "cLcxAECuMQBQujEAwCIxAIAlMQBgkzEACEkxAHgrMQBgvzEA4DIxAEAyMQCQdjEAKEExAIA+MQDoijEAkDcxADA7MQD4SzEAqEwxAGiNMQBonTEA"
            + "iHkxALh/MQAIyDEAIEUxAPiXMQAAAAAAFwQcSgAQIADIAAAAAADhQwAANENAAAAACgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAA==",
            0x2B4,
            "RkVBQRkAAAAoAAAACMALJMgAAAAAAMhDAADAQQAAgEHcBQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAABAAAAAEAAAAAAAAA"
            + "AwABAwAAAAAEAAECBAAAAAIAAQAAAAAAAQAgAAUAAAQGAIAAAwAAAAIAAAC+FHs/wsVHPgAIAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAA="),
        new(24, "Improved Enemy AI with eight parts in two cards, as WorldExplorer wrote it", 0xFF, 0x00207A10,
            new (int Va, uint Game, uint Patched, int Spells)[]
            {
            (0x0010F4AC, 0x1462000B, 0x0808236C, 0x480),
            (0x0010F648, 0x12000207, 0x0806E55C, 0x1480),
            (0x001109C0, 0x14620004, 0x080666D9, 0x80),
            (0x001182C0, 0x27BDFE80, 0x0804F4F9, 0x8000),
            (0x001182C4, 0xFFB00150, 0x00000000, 0x8000),
            (0x001185D8, 0x27BDFFA0, 0x08084328, 0x8000),
            (0x001185DC, 0xFFB40040, 0x00000000, 0x8000),
            (0x0011B758, 0x0100F809, 0x0C083132, 0x600),
            (0x0011B7D8, 0xDFB10030, 0x08081ED7, 0x4000),
            (0x0011C168, 0x27BDFF20, 0x08084061, 0x8000),
            (0x0011C16C, 0xFFB20070, 0x00000000, 0x8000),
            (0x00129540, 0x27BDFFB0, 0x08084280, 0x8000),
            (0x00129544, 0xFFB30030, 0x00000000, 0x8000),
            (0x00129778, 0x27BDFFF0, 0x0804F486, 0x8000),
            (0x0012977C, 0x68C20007, 0x00000000, 0x8000),
            (0x00129C14, 0x0C047E0E, 0x0C083060, 0x8000),
            (0x0015E220, 0xC661002C, 0x0C059B8A, 0x480),
            (0x0015E424, 0x2C420003, 0x2C420004, 0x80),
            (0x0015E43C, 0x54620078, 0x08072EC6, 0x400),
            (0x0015E50C, 0x10000013, 0x0804AFD1, 0x480),
            (0x0015F160, 0x0C043C8C, 0x0C08207D, 0x1000),
            (0x0015F278, 0x0C043C8C, 0x0C08207D, 0x1000),
            (0x0015F354, 0x0C043C8C, 0x0C08207D, 0x1000),
            (0x00161EF8, 0x2C830017, 0x2C83002A, 0x7FFF),
            (0x00161F00, 0x3C020030, 0x3C020020, 0x7FFF),
            (0x00161F08, 0x24427BC0, 0x24427A10, 0x7FFF),
            (0x001676E4, 0x0C05AA52, 0x0C04F3F6, 0x20),
            (0x00167AFC, 0x0240282D, 0x0C04A712, 0x18),
            (0x0016DD2C, 0x0C04EB96, 0x0C0836C2, 0x80),
            (0x0016DD44, 0x0C04EB96, 0x0C04C150, 0x7FFF),
            (0x0017A4BC, 0x0C05ED8E, 0x0C07A156, 0x7FFF),
            (0x0017B594, 0x0C059CE4, 0x0C068A6C, 0x20),
            (0x0017E52C, 0x14830009, 0x0804625E, 0x7FCE),
            (0x0017EA44, 0x8C440000, 0x0C083175, 0x7FCE),
            (0x0017EAB4, 0x8C440000, 0x0C083175, 0x7FCE),
            (0x0018331C, 0x0C058ADE, 0x0C070DBA, 0x7FFF),
            (0x0019A464, 0x24020014, 0x0808139F, 0x8000),
            (0x0019F8F4, 0x0C043C8C, 0x0C040ADB, 0x8000),
            (0x001A1134, 0x24630032, 0x08081378, 0x8000),
            (0x001A3B9C, 0x0C043C8C, 0x0C040AD7, 0x8000),
            (0x001B177C, 0x3C030031, 0x8E230038, 0x1),
            (0x001B1784, 0x24636798, 0x24630080, 0x1),
            (0x001BCBFC, 0x24630096, 0x08081385, 0x8000),
            (0x001BED7C, 0x0C04C3CC, 0x0C0822B4, 0x100),
            (0x001BED94, 0x0C04C3A6, 0x0C0822BA, 0x100),
            (0x001BEFD0, 0x24040058, 0x24040060, 0x100),
            (0x001BF004, 0x0C06FD68, 0x0C081D22, 0x100),
            (0x001BF9C0, 0x0C043C8C, 0x0C087284, 0x8000),
            (0x001BF9E0, 0xAE030038, 0x0808728A, 0x8000),
            (0x001DA950, 0x0C043C8C, 0x0C040AD9, 0x8000),
            (0x001DBFE0, 0x24630064, 0x08081392, 0x8000),
            (0x0030A104, 0x001663F8, 0x001D32D8, 0xC),
            (0x0030A11C, 0x0015C2F0, 0x002089C0, 0x8),
            (0x0031AE64, 0x001D9FB0, 0x001B8DA0, 0x8000),
            (0x0031B794, 0x001DB590, 0x001B8DAC, 0x8000)
            },
            new (int Va, int Length)[]
            {
            (0x00102910, 0x264), (0x00102D80, 0x280), (0x0010C338, 0x394), (0x0010CD50, 0x1E0),
            (0x00118978, 0x3F0), (0x00129318, 0x21C), (0x00129C48, 0x11C), (0x0012A710, 0x190),
            (0x0012BAE8, 0x4A8), (0x0012CD30, 0x350), (0x0012F790, 0x278), (0x00130540, 0x1C4),
            (0x00131170, 0x148), (0x00132018, 0x154), (0x00132D90, 0x64C), (0x00134CE0, 0x118),
            (0x00134F18, 0x1B0), (0x0013CDF8, 0x208), (0x0013D218, 0x214), (0x00166E28, 0x198),
            (0x0016ACB8, 0x1C4), (0x00191A20, 0x2F8), (0x00196AB8, 0x144), (0x00198968, 0x13C),
            (0x00199A60, 0x138), (0x001A29B0, 0x50), (0x001AE930, 0xF4), (0x001B8DA0, 0x170),
            (0x001B9570, 0x114), (0x001B9E68, 0xE4), (0x001BAFF8, 0xE0), (0x001C36E8, 0x188),
            (0x001CBA38, 0x10C), (0x001D32D8, 0x144), (0x001E8558, 0x234), (0x001FF340, 0x198),
            (0x00204DE0, 0xC8), (0x002068C8, 0x1F0), (0x002071C0, 0x210), (0x00207488, 0xAC),
            (0x00207AB8, 0x13C), (0x00208160, 0x1B8), (0x002089C0, 0x90), (0x00208AD0, 0x40),
            (0x00208DB0, 0x80), (0x0020ACB0, 0x80), (0x0020C180, 0xD0), (0x0020C4C8, 0x14C),
            (0x0020DA88, 0x108), (0x0020E928, 0x210), (0x0020F958, 0x64), (0x00210058, 0x240),
            (0x00210A00, 0x204), (0x00210CA0, 0x19C), (0x0021BEE8, 0xE0), (0x0021CA10, 0x58)
            },
            "0bb2852e3c90e76264c7b4ac761d54b30c8d8d6266bf0abd51b3532395f402be",
            new (int Va, int GameVa, (int At, uint Word)[] Words)[]
            {
            (0x001A9A40, 0x00316718, new (int, uint)[] {  }),
            (0x001A9AC0, 0x00316798, new (int, uint)[] { (0x24, 0x00196AB8), (0x3C, 0x0012E418) }),
            (0x00208CB0, 0x0031B650, new (int, uint)[] { (0x0C, 0x00208288), (0x24, 0x0010CD50) }),
            (0x00207540, 0x0031B5D0, new (int, uint)[] { (0x0C, 0x00207274) }),
            (0x0020C100, 0x0031C3C8, new (int, uint)[] { (0x3C, 0x0012F998) }),
            (0x0020AC30, 0x00316798, new (int, uint)[] { (0x24, 0x0012A710), (0x3C, 0x0012E418) }),
            (0x00208D30, 0x00317470, new (int, uint)[] { (0x0C, 0x0010C634), (0x24, 0x001FF40C) }),
            (0x001A28B0, 0x0031B530, new (int, uint)[] { (0x0C, 0x0020ACB0), (0x24, 0x00102D80) }),
            (0x001A2930, 0x0031B5B0, new (int, uint)[] {  }),
            (0x0020C080, 0x00313140, new (int, uint)[] { (0x24, 0x0016ACB8) }),
            (0x00208A50, 0x00319CE8, new (int, uint)[] { (0x24, 0x00191BC4), (0x2C, 0x00206990), (0x3C, 0x002068C8) }),
            (0x0021C990, 0x00305888, new (int, uint)[] { (0x24, 0x0021CA40), (0x2C, 0x0020F958) })
            },
            0x3A0,
            "RkVBORgAAAAAAAAAAAAAAGQAAAAAABBASOH6PgAA8EEAAEhDAADAQQAAQEEAAKBBAABAQgAAAEAyAAAAAADIQwAAyEP0AQAAAAB6RAAAAAAAAIA/"
            + "AAAAAPQENT/0BDU/AAAAAAAAgD/0BDW/9AQ1PwAAgL8AAAAA9AQ1v/QENb8AAAAAAACAv/QENT/0BDW/gIMxABCGMQB4iDEA2JAxAIB9MQAIvTEA"
            + "cLcxAECuMQBQujEAwCIxAIAlMQBgkzEACEkxAHgrMQBgvzEA4DIxAEAyMQCQdjEAKEExAIA+MQDoijEAkDcxADA7MQD4SzEAqEwxAGiNMQBonTEA"
            + "iHkxALh/MQAIyDEAIEUxAPiXMQAAAAAAFwQcSgAQIADIAAAAAADhQwAANENAAAAACgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAA==",
            0x2B0,
            "RkVBQRgAAAAoAAAACMALJMgAAAAAAMhDAADAQQAAgEHcBQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAABAAAAAEAAAAAAAAA"
            + "AwABAwAAAAAEAAECBAAAAAIAAQAAAAAAAQAgAAUAAAQGAIAAAwAAAAIAAAC+FHs/wsVHPgAIAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAA=="),
        new(23, "the test of Improved Enemy AI with the archers (the lizardmen's spears flew straight)", 0xFF, 0x00207A10,
            new (int Va, uint Game, uint Patched, int Spells)[]
            {
            (0x0010F4AC, 0x1462000B, 0x0808236C, 0x480),
            (0x0010F648, 0x12000207, 0x0806E55C, 0x1480),
            (0x001109C0, 0x14620004, 0x080666D9, 0x80),
            (0x001182C0, 0x27BDFE80, 0x0804F4F9, 0x8000),
            (0x001182C4, 0xFFB00150, 0x00000000, 0x8000),
            (0x001185D8, 0x27BDFFA0, 0x08084328, 0x8000),
            (0x001185DC, 0xFFB40040, 0x00000000, 0x8000),
            (0x0011B758, 0x0100F809, 0x0C083132, 0x600),
            (0x0011B7D8, 0xDFB10030, 0x08081ED7, 0x4000),
            (0x0011C168, 0x27BDFF20, 0x08084061, 0x8000),
            (0x0011C16C, 0xFFB20070, 0x00000000, 0x8000),
            (0x00129540, 0x27BDFFB0, 0x08084280, 0x8000),
            (0x00129544, 0xFFB30030, 0x00000000, 0x8000),
            (0x00129778, 0x27BDFFF0, 0x0804F486, 0x8000),
            (0x0012977C, 0x68C20007, 0x00000000, 0x8000),
            (0x00129C14, 0x0C047E0E, 0x0C083060, 0x8000),
            (0x0015E220, 0xC661002C, 0x0C059B8A, 0x480),
            (0x0015E424, 0x2C420003, 0x2C420004, 0x80),
            (0x0015E43C, 0x54620078, 0x08072EC6, 0x400),
            (0x0015E50C, 0x10000013, 0x0804AFD1, 0x480),
            (0x0015F160, 0x0C043C8C, 0x0C08207D, 0x1000),
            (0x0015F278, 0x0C043C8C, 0x0C08207D, 0x1000),
            (0x0015F354, 0x0C043C8C, 0x0C08207D, 0x1000),
            (0x00161EF8, 0x2C830017, 0x2C83002A, 0x7FFF),
            (0x00161F00, 0x3C020030, 0x3C020020, 0x7FFF),
            (0x00161F08, 0x24427BC0, 0x24427A10, 0x7FFF),
            (0x001676E4, 0x0C05AA52, 0x0C04F3F6, 0x20),
            (0x00167AFC, 0x0240282D, 0x0C04A712, 0x18),
            (0x0016DD2C, 0x0C04EB96, 0x0C0836C2, 0x80),
            (0x0016DD44, 0x0C04EB96, 0x0C04C150, 0x7FFF),
            (0x0017A4BC, 0x0C05ED8E, 0x0C07A156, 0x7FFF),
            (0x0017B594, 0x0C059CE4, 0x0C068A6C, 0x20),
            (0x0017E52C, 0x14830009, 0x0804625E, 0x7FCE),
            (0x0017EA44, 0x8C440000, 0x0C083175, 0x7FCE),
            (0x0017EAB4, 0x8C440000, 0x0C083175, 0x7FCE),
            (0x0018331C, 0x0C058ADE, 0x0C070DBA, 0x7FFF),
            (0x0019A464, 0x24020014, 0x0808139F, 0x8000),
            (0x0019F8F4, 0x0C043C8C, 0x0C0813B0, 0x8000),
            (0x001A1134, 0x24630032, 0x08081378, 0x8000),
            (0x001A3B9C, 0x0C043C8C, 0x0C0813AA, 0x8000),
            (0x001B177C, 0x3C030031, 0x8E230038, 0x1),
            (0x001B1784, 0x24636798, 0x24630080, 0x1),
            (0x001BCBFC, 0x24630096, 0x08081385, 0x8000),
            (0x001BED7C, 0x0C04C3CC, 0x0C0822B4, 0x100),
            (0x001BED94, 0x0C04C3A6, 0x0C0822BA, 0x100),
            (0x001BEFD0, 0x24040058, 0x24040060, 0x100),
            (0x001BF004, 0x0C06FD68, 0x0C081D22, 0x100),
            (0x001BF9C0, 0x0C043C8C, 0x0C0813AE, 0x8000),
            (0x001BF9E0, 0xAE030038, 0x080813B2, 0x8000),
            (0x001DA950, 0x0C043C8C, 0x0C0813AC, 0x8000),
            (0x001DBFE0, 0x24630064, 0x08081392, 0x8000),
            (0x0030A104, 0x001663F8, 0x001D32D8, 0xC),
            (0x0030A11C, 0x0015C2F0, 0x002089C0, 0x8),
            (0x0031AE64, 0x001D9FB0, 0x001B8DA0, 0x8000),
            (0x0031B794, 0x001DB590, 0x001B8DAC, 0x8000)
            },
            new (int Va, int Length)[]
            {
            (0x00102910, 0x24C), (0x00102D80, 0x280), (0x0010C338, 0x394), (0x0010CD50, 0x1E0),
            (0x00118978, 0x3F0), (0x00129318, 0x21C), (0x00129C48, 0x11C), (0x0012A710, 0x190),
            (0x0012BAE8, 0x4A8), (0x0012CD30, 0x350), (0x0012F790, 0x278), (0x00130540, 0x1C4),
            (0x00131170, 0x148), (0x00132018, 0x154), (0x00132D90, 0x64C), (0x00134CE0, 0x118),
            (0x00134F18, 0x1B0), (0x0013CDF8, 0x208), (0x0013D218, 0x214), (0x00166E28, 0x198),
            (0x0016ACB8, 0x1C4), (0x00191A20, 0x2F8), (0x00196AB8, 0x144), (0x00198968, 0x13C),
            (0x00199A60, 0x138), (0x001A29B0, 0x50), (0x001AE930, 0xF4), (0x001B8DA0, 0x170),
            (0x001B9570, 0x114), (0x001B9E68, 0xE4), (0x001BAFF8, 0xE0), (0x001C36E8, 0x188),
            (0x001CBA38, 0x10C), (0x001D32D8, 0x144), (0x001E8558, 0x234), (0x001FF340, 0x198),
            (0x00204DE0, 0xFC), (0x002068C8, 0x1F0), (0x002071C0, 0x210), (0x00207488, 0xAC),
            (0x00207AB8, 0x13C), (0x00208160, 0x1B8), (0x002089C0, 0x90), (0x00208AD0, 0x40),
            (0x00208DB0, 0x80), (0x0020ACB0, 0x80), (0x0020C180, 0xD0), (0x0020C4C8, 0x14C),
            (0x0020DA88, 0x108), (0x0020E928, 0x210), (0x00210058, 0x240), (0x00210A00, 0x204),
            (0x00210CA0, 0x19C), (0x0021BEE8, 0xE0)
            },
            "bcdae0577ec50e8e50862e1d0125e31099f17f5e70e4d8b85e182c63a5191d21",
            new (int Va, int GameVa, (int At, uint Word)[] Words)[]
            {
            (0x001A9A40, 0x00316718, new (int, uint)[] {  }),
            (0x001A9AC0, 0x00316798, new (int, uint)[] { (0x24, 0x00196AB8), (0x3C, 0x0012E418) }),
            (0x00208CB0, 0x0031B650, new (int, uint)[] { (0x0C, 0x00208288), (0x24, 0x0010CD50) }),
            (0x00207540, 0x0031B5D0, new (int, uint)[] { (0x0C, 0x00207274) }),
            (0x0020C100, 0x0031C3C8, new (int, uint)[] { (0x3C, 0x0012F998) }),
            (0x0020AC30, 0x00316798, new (int, uint)[] { (0x24, 0x0012A710), (0x3C, 0x0012E418) }),
            (0x00208D30, 0x00317470, new (int, uint)[] { (0x0C, 0x0010C634), (0x24, 0x001FF40C) }),
            (0x001A28B0, 0x0031B530, new (int, uint)[] { (0x0C, 0x0020ACB0), (0x24, 0x00102D80) }),
            (0x001A2930, 0x0031B5B0, new (int, uint)[] {  }),
            (0x0020C080, 0x00313140, new (int, uint)[] { (0x24, 0x0016ACB8) }),
            (0x00208A50, 0x00319CE8, new (int, uint)[] { (0x24, 0x00191BC4), (0x2C, 0x00206990), (0x3C, 0x002068C8) })
            },
            0x3A0,
            "RkVBORcAAAAAAAAAAAAAAGQAAAAAABBASOH6PgAA8EEAAEhDAADAQQAAQEEAAKBBAABAQgAAAEAyAAAAAADIQwAAyEP0AQAAAAB6RAAAAAAAAIA/"
            + "AAAAAPQENT/0BDU/AAAAAAAAgD/0BDW/9AQ1PwAAgL8AAAAA9AQ1v/QENb8AAAAAAACAv/QENT/0BDW/gIMxABCGMQB4iDEA2JAxAIB9MQAIvTEA"
            + "cLcxAECuMQBQujEAwCIxAIAlMQBgkzEACEkxAHgrMQBgvzEA4DIxAEAyMQCQdjEAKEExAIA+MQDoijEAkDcxADA7MQD4SzEAqEwxAGiNMQBonTEA"
            + "iHkxALh/MQAIyDEAIEUxAPiXMQAAAAAAFwQcSgAQIADIAAAAAADhQwAANENAAAAACgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAA==",
            0x2B0,
            "RkVBQRcAAAAoAAAACMALJMgAAAAAAMhDAADAQQAAgEHcBQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAABAAAAAEAAAAAAAAA"
            + "AwABAwAAAAAFAAECAwQAAAIAAQAAAAAAAQAgAAUAAAQGAIAAAwAAAAIAAAC+FHs/wsVHPgAIAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAA=="),
        new(22, "Improved Enemy AI with seven parts, as WorldExplorer wrote it", 0x7F, 0x00207A10,
            new (int Va, uint Game, uint Patched, int Spells)[]
            {
            (0x0010F4AC, 0x1462000B, 0x0808236C, 0x480),
            (0x0010F648, 0x12000207, 0x0806E55C, 0x1480),
            (0x001109C0, 0x14620004, 0x080666D9, 0x80),
            (0x001182C0, 0x27BDFE80, 0x0804F4F9, 0x8000),
            (0x001182C4, 0xFFB00150, 0x00000000, 0x8000),
            (0x001185D8, 0x27BDFFA0, 0x08084328, 0x8000),
            (0x001185DC, 0xFFB40040, 0x00000000, 0x8000),
            (0x0011B758, 0x0100F809, 0x0C083132, 0x600),
            (0x0011B7D8, 0xDFB10030, 0x08081ED7, 0x4000),
            (0x0011C168, 0x27BDFF20, 0x08084061, 0x8000),
            (0x0011C16C, 0xFFB20070, 0x00000000, 0x8000),
            (0x00129540, 0x27BDFFB0, 0x08084280, 0x8000),
            (0x00129544, 0xFFB30030, 0x00000000, 0x8000),
            (0x00129778, 0x27BDFFF0, 0x0804F486, 0x8000),
            (0x0012977C, 0x68C20007, 0x00000000, 0x8000),
            (0x00129C14, 0x0C047E0E, 0x0C083060, 0x8000),
            (0x0015E220, 0xC661002C, 0x0C059B8A, 0x480),
            (0x0015E424, 0x2C420003, 0x2C420004, 0x80),
            (0x0015E43C, 0x54620078, 0x08072EC6, 0x400),
            (0x0015E50C, 0x10000013, 0x0804AFD1, 0x480),
            (0x0015F160, 0x0C043C8C, 0x0C08207D, 0x1000),
            (0x0015F278, 0x0C043C8C, 0x0C08207D, 0x1000),
            (0x0015F354, 0x0C043C8C, 0x0C08207D, 0x1000),
            (0x00161EF8, 0x2C830017, 0x2C83002A, 0x7FFF),
            (0x00161F00, 0x3C020030, 0x3C020020, 0x7FFF),
            (0x00161F08, 0x24427BC0, 0x24427A10, 0x7FFF),
            (0x001676E4, 0x0C05AA52, 0x0C04F3F6, 0x20),
            (0x00167AFC, 0x0240282D, 0x0C04A712, 0x18),
            (0x0016DD2C, 0x0C04EB96, 0x0C0836C2, 0x80),
            (0x0016DD44, 0x0C04EB96, 0x0C04C150, 0x7FFF),
            (0x0017A4BC, 0x0C05ED8E, 0x0C07A156, 0x7FFF),
            (0x0017B594, 0x0C059CE4, 0x0C068A6C, 0x20),
            (0x0017E52C, 0x14830009, 0x0804625E, 0x7FCE),
            (0x0017EA44, 0x8C440000, 0x0C083175, 0x7FCE),
            (0x0017EAB4, 0x8C440000, 0x0C083175, 0x7FCE),
            (0x0018331C, 0x0C058ADE, 0x0C070DBA, 0x7FFF),
            (0x0019A464, 0x24020014, 0x0808139F, 0x8000),
            (0x001A1134, 0x24630032, 0x08081378, 0x8000),
            (0x001B177C, 0x3C030031, 0x8E230038, 0x1),
            (0x001B1784, 0x24636798, 0x24630080, 0x1),
            (0x001BCBFC, 0x24630096, 0x08081385, 0x8000),
            (0x001BED7C, 0x0C04C3CC, 0x0C0822B4, 0x100),
            (0x001BED94, 0x0C04C3A6, 0x0C0822BA, 0x100),
            (0x001BEFD0, 0x24040058, 0x24040060, 0x100),
            (0x001BF004, 0x0C06FD68, 0x0C081D22, 0x100),
            (0x001DBFE0, 0x24630064, 0x08081392, 0x8000),
            (0x0030A104, 0x001663F8, 0x001D32D8, 0xC),
            (0x0030A11C, 0x0015C2F0, 0x002089C0, 0x8),
            (0x0031AE64, 0x001D9FB0, 0x001B8DA0, 0x8000),
            (0x0031B794, 0x001DB590, 0x001B8DAC, 0x8000)
            },
            new (int Va, int Length)[]
            {
            (0x00102910, 0x24C), (0x00102D80, 0x280), (0x0010C338, 0x394), (0x0010CD50, 0x1E0),
            (0x00118978, 0x3F0), (0x00129318, 0x21C), (0x00129C48, 0x11C), (0x0012A710, 0x190),
            (0x0012BAE8, 0x4A8), (0x0012CD30, 0x350), (0x0012F790, 0x278), (0x00130540, 0x1C4),
            (0x00131170, 0x148), (0x00132018, 0x154), (0x00132D90, 0x64C), (0x00134CE0, 0x118),
            (0x00134F18, 0x1B0), (0x0013CDF8, 0x208), (0x0013D218, 0x214), (0x00166E28, 0x198),
            (0x0016ACB8, 0x1C4), (0x00191A20, 0x2F8), (0x00196AB8, 0x144), (0x00198968, 0x13C),
            (0x00199A60, 0x138), (0x001A29B0, 0x50), (0x001AE930, 0xF4), (0x001B8DA0, 0x170),
            (0x001B9570, 0x114), (0x001B9E68, 0xE4), (0x001BAFF8, 0xE0), (0x001C36E8, 0x188),
            (0x001CBA38, 0x10C), (0x001D32D8, 0x144), (0x001E8558, 0x234), (0x001FF340, 0x198),
            (0x00204DE0, 0xC8), (0x002068C8, 0x1F0), (0x002071C0, 0x210), (0x00207488, 0xAC),
            (0x00207AB8, 0x13C), (0x00208160, 0x1B8), (0x002089C0, 0x90), (0x00208AD0, 0x40),
            (0x00208DB0, 0x80), (0x0020ACB0, 0x80), (0x0020C180, 0xD0), (0x0020C4C8, 0x14C),
            (0x0020DA88, 0x108), (0x0020E928, 0x210), (0x00210058, 0x240), (0x00210A00, 0x204),
            (0x00210CA0, 0xF0)
            },
            "a91ff40e4b58145d07aa080c07733c1030288dedddf124e54335ef8c4772872e",
            new (int Va, int GameVa, (int At, uint Word)[] Words)[]
            {
            (0x001A9A40, 0x00316718, new (int, uint)[] {  }),
            (0x001A9AC0, 0x00316798, new (int, uint)[] { (0x24, 0x00196AB8), (0x3C, 0x0012E418) }),
            (0x00208CB0, 0x0031B650, new (int, uint)[] { (0x0C, 0x00208288), (0x24, 0x0010CD50) }),
            (0x00207540, 0x0031B5D0, new (int, uint)[] { (0x0C, 0x00207274) }),
            (0x0020C100, 0x0031C3C8, new (int, uint)[] { (0x3C, 0x0012F998) }),
            (0x0020AC30, 0x00316798, new (int, uint)[] { (0x24, 0x0012A710), (0x3C, 0x0012E418) }),
            (0x00208D30, 0x00317470, new (int, uint)[] { (0x0C, 0x0010C634), (0x24, 0x001FF40C) }),
            (0x001A28B0, 0x0031B530, new (int, uint)[] { (0x0C, 0x0020ACB0), (0x24, 0x00102D80) }),
            (0x001A2930, 0x0031B5B0, new (int, uint)[] {  }),
            (0x0020C080, 0x00313140, new (int, uint)[] { (0x24, 0x0016ACB8) }),
            (0x00208A50, 0x00319CE8, new (int, uint)[] { (0x24, 0x00191BC4), (0x2C, 0x00206990), (0x3C, 0x002068C8) })
            },
            0x3A0,
            "RkVBORYAAAAAAAAAAAAAAGQAAAAAABBASOH6PgAA8EEAAEhDAADAQQAAQEEAAKBBAABAQgAAAEAyAAAAAADIQwAAyEP0AQAAAAB6RAAAAAAAAIA/"
            + "AAAAAPQENT/0BDU/AAAAAAAAgD/0BDW/9AQ1PwAAgL8AAAAA9AQ1v/QENb8AAAAAAACAv/QENT/0BDW/gIMxABCGMQB4iDEA2JAxAIB9MQAIvTEA"
            + "cLcxAECuMQBQujEAwCIxAIAlMQBgkzEACEkxAHgrMQBgvzEA4DIxAEAyMQCQdjEAKEExAIA+MQDoijEAkDcxADA7MQD4SzEAqEwxAGiNMQBonTEA"
            + "iHkxALh/MQAIyDEAIEUxAPiXMQAAAAAAFwQcSgAQIADIAAAAAADhQwAANENAAAAACgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAA==",
            0x240,
            "RkVBQRYAAAAoAAAACMALJMgAAAAAAMhDAADAQQAAgEHcBQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"),
        new(21, "the test of Improved Enemy AI with the drow blink", 0x3F, 0x00207A10,
            new (int Va, uint Game, uint Patched, int Spells)[]
            {
            (0x0010F4AC, 0x1462000B, 0x0808236C, 0x480),
            (0x0010F648, 0x12000207, 0x0806E55C, 0x1480),
            (0x001109C0, 0x14620004, 0x080666D9, 0x80),
            (0x001182C0, 0x27BDFE80, 0x0804F4F9, 0x8000),
            (0x001182C4, 0xFFB00150, 0x00000000, 0x8000),
            (0x001185D8, 0x27BDFFA0, 0x0804B34C, 0x8000),
            (0x001185DC, 0xFFB40040, 0x00000000, 0x8000),
            (0x0011B758, 0x0100F809, 0x0C083132, 0x600),
            (0x0011B7D8, 0xDFB10030, 0x08081ED7, 0x4000),
            (0x0011C168, 0x27BDFF20, 0x0804AEBA, 0x8000),
            (0x0011C16C, 0xFFB20070, 0x00000000, 0x8000),
            (0x00129778, 0x27BDFFF0, 0x0804F486, 0x8000),
            (0x0012977C, 0x68C20007, 0x00000000, 0x8000),
            (0x00129C14, 0x0C047E0E, 0x0C083060, 0x8000),
            (0x0015E220, 0xC661002C, 0x0C059B8A, 0x480),
            (0x0015E424, 0x2C420003, 0x2C420004, 0x80),
            (0x0015E43C, 0x54620078, 0x08072EC6, 0x400),
            (0x0015E50C, 0x10000013, 0x0804AFD1, 0x480),
            (0x0015F160, 0x0C043C8C, 0x0C08207D, 0x1000),
            (0x0015F278, 0x0C043C8C, 0x0C08207D, 0x1000),
            (0x0015F354, 0x0C043C8C, 0x0C08207D, 0x1000),
            (0x00161EF8, 0x2C830017, 0x2C83002A, 0x7FFF),
            (0x00161F00, 0x3C020030, 0x3C020020, 0x7FFF),
            (0x00161F08, 0x24427BC0, 0x24427A10, 0x7FFF),
            (0x001676E4, 0x0C05AA52, 0x0C04F3F6, 0x20),
            (0x00167AFC, 0x0240282D, 0x0C04A712, 0x18),
            (0x0016DD2C, 0x0C04EB96, 0x0C0836C2, 0x80),
            (0x0016DD44, 0x0C04EB96, 0x0C04C150, 0x7FFF),
            (0x0017A4BC, 0x0C05ED8E, 0x0C07A156, 0x7FFF),
            (0x0017B594, 0x0C059CE4, 0x0C068A6C, 0x20),
            (0x0017E52C, 0x14830009, 0x0804625E, 0x7FCE),
            (0x0017EA44, 0x8C440000, 0x0C083175, 0x7FCE),
            (0x0017EAB4, 0x8C440000, 0x0C083175, 0x7FCE),
            (0x0018331C, 0x0C058ADE, 0x0C070DBA, 0x7FFF),
            (0x0019A464, 0x24020014, 0x0808139F, 0x8000),
            (0x001A1134, 0x24630032, 0x08081378, 0x8000),
            (0x001B177C, 0x3C030031, 0x8E230038, 0x1),
            (0x001B1784, 0x24636798, 0x24630080, 0x1),
            (0x001BCBFC, 0x24630096, 0x08081385, 0x8000),
            (0x001BED7C, 0x0C04C3CC, 0x0C0822B4, 0x100),
            (0x001BED94, 0x0C04C3A6, 0x0C0822BA, 0x100),
            (0x001BEFD0, 0x24040058, 0x24040060, 0x100),
            (0x001BF004, 0x0C06FD68, 0x0C081D22, 0x100),
            (0x001DBFE0, 0x24630064, 0x08081392, 0x8000),
            (0x0030A104, 0x001663F8, 0x001D32D8, 0xC),
            (0x0030A11C, 0x0015C2F0, 0x002089C0, 0x8),
            (0x0031AE64, 0x001D9FB0, 0x001B8DA0, 0x8000),
            (0x0031B794, 0x001DB590, 0x001B8DAC, 0x8000)
            },
            new (int Va, int Length)[]
            {
            (0x00102910, 0x24C), (0x00102D80, 0x280), (0x0010C338, 0x394), (0x0010CD50, 0x1E0),
            (0x00118978, 0x3F0), (0x00129318, 0x21C), (0x00129C48, 0x11C), (0x0012A710, 0x190),
            (0x0012BAE8, 0x4A8), (0x0012CD30, 0x350), (0x0012F790, 0x278), (0x00130540, 0x1C4),
            (0x00131170, 0x148), (0x00132018, 0x154), (0x00132D90, 0x64C), (0x00134CE0, 0x118),
            (0x00134F18, 0x1B0), (0x0013CDF8, 0x208), (0x0013D218, 0x214), (0x00166E28, 0x198),
            (0x0016ACB8, 0x1C4), (0x00191A20, 0x2F8), (0x00196AB8, 0x144), (0x00198968, 0x13C),
            (0x00199A60, 0x138), (0x001A29B0, 0x50), (0x001AE930, 0xF4), (0x001B8DA0, 0x170),
            (0x001B9570, 0x114), (0x001B9E68, 0xE4), (0x001BAFF8, 0xE0), (0x001C36E8, 0x188),
            (0x001CBA38, 0x10C), (0x001D32D8, 0x144), (0x001E8558, 0x234), (0x001FF340, 0x198),
            (0x00204DE0, 0xC8), (0x002068C8, 0x1F0), (0x002071C0, 0x210), (0x00207488, 0xAC),
            (0x00207AB8, 0x13C), (0x00208160, 0x1B8), (0x002089C0, 0x90), (0x00208AD0, 0x40),
            (0x00208DB0, 0x80), (0x0020ACB0, 0x80), (0x0020C180, 0xD0), (0x0020C4C8, 0x14C),
            (0x0020DA88, 0xBC)
            },
            "40c6c7430aa6c420e02a4b6936401fdb06ae46c9f58f1138f3ab79db6e6368ee",
            new (int Va, int GameVa, (int At, uint Word)[] Words)[]
            {
            (0x001A9A40, 0x00316718, new (int, uint)[] {  }),
            (0x001A9AC0, 0x00316798, new (int, uint)[] { (0x24, 0x00196AB8), (0x3C, 0x0012E418) }),
            (0x00208CB0, 0x0031B650, new (int, uint)[] { (0x0C, 0x00208288), (0x24, 0x0010CD50) }),
            (0x00207540, 0x0031B5D0, new (int, uint)[] { (0x0C, 0x00207274) }),
            (0x0020C100, 0x0031C3C8, new (int, uint)[] { (0x3C, 0x0012F998) }),
            (0x0020AC30, 0x00316798, new (int, uint)[] { (0x24, 0x0012A710), (0x3C, 0x0012E418) }),
            (0x00208D30, 0x00317470, new (int, uint)[] { (0x0C, 0x0010C634), (0x24, 0x001FF40C) }),
            (0x001A28B0, 0x0031B530, new (int, uint)[] { (0x0C, 0x0020ACB0), (0x24, 0x00102D80) }),
            (0x001A2930, 0x0031B5B0, new (int, uint)[] {  }),
            (0x0020C080, 0x00313140, new (int, uint)[] { (0x24, 0x0016ACB8) }),
            (0x00208A50, 0x00319CE8, new (int, uint)[] { (0x24, 0x00191BC4), (0x2C, 0x00206990), (0x3C, 0x002068C8) })
            },
            0x3A0,
            "RkVBORUAAAAAAAAAAAAAAGQAAAAAABBASOH6PgAA8EEAAEhDAADAQQAAQEEAAKBBAABAQgAAAEAyAAAAAADIQwAAyEP0AQAAAAB6RAAAAAAAAIA/"
            + "AAAAAPQENT/0BDU/AAAAAAAAgD/0BDW/9AQ1PwAAgL8AAAAA9AQ1v/QENb8AAAAAAACAv/QENT/0BDW/gIMxABCGMQB4iDEA2JAxAIB9MQAIvTEA"
            + "cLcxAECuMQBQujEAwCIxAIAlMQBgkzEACEkxAHgrMQBgvzEA4DIxAEAyMQCQdjEAKEExAIA+MQDoijEAkDcxADA7MQD4SzEAqEwxAGiNMQBonTEA"
            + "iHkxALh/MQAIyDEAIEUxAPiXMQAAAAAAFwQcSgAQIADIAAAAAADhQwAANENAAAAACgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAA==",
            0x0,
            ""),
        new(20, "the test of Improved Enemy AI with the brutes knocking you back", 0x1F, 0x00207320,
            new (int Va, uint Game, uint Patched, int Spells)[]
            {
            (0x0010F4AC, 0x1462000B, 0x0806E3A5, 0x480),
            (0x0010F648, 0x12000207, 0x0806E55C, 0x1480),
            (0x001109C0, 0x14620004, 0x080666D9, 0x80),
            (0x001182C0, 0x27BDFE80, 0x08082378, 0x8000),
            (0x001182C4, 0xFFB00150, 0x00000000, 0x8000),
            (0x001185D8, 0x27BDFFA0, 0x0804B34C, 0x8000),
            (0x001185DC, 0xFFB40040, 0x00000000, 0x8000),
            (0x0011B758, 0x0100F809, 0x0C0822AB, 0x600),
            (0x0011B7D8, 0xDFB10030, 0x0804BE5C, 0x4000),
            (0x0011C168, 0x27BDFF20, 0x0804AEBA, 0x8000),
            (0x0011C16C, 0xFFB20070, 0x00000000, 0x8000),
            (0x00129778, 0x27BDFFF0, 0x0804F37E, 0x8000),
            (0x0012977C, 0x68C20007, 0x00000000, 0x8000),
            (0x00129C14, 0x0C047E0E, 0x0C08232C, 0x8000),
            (0x0015E220, 0xC661002C, 0x0C059B8A, 0x480),
            (0x0015E424, 0x2C420003, 0x2C420004, 0x80),
            (0x0015E43C, 0x54620078, 0x0806E596, 0x400),
            (0x0015E50C, 0x10000013, 0x0804AFD1, 0x480),
            (0x0015F160, 0x0C043C8C, 0x0C07A1BC, 0x1000),
            (0x0015F278, 0x0C043C8C, 0x0C07A1BC, 0x1000),
            (0x0015F354, 0x0C043C8C, 0x0C07A1BC, 0x1000),
            (0x00161EF8, 0x2C830017, 0x2C83002A, 0x7FFF),
            (0x00161F00, 0x3C020030, 0x3C020020, 0x7FFF),
            (0x00161F08, 0x24427BC0, 0x24427320, 0x7FFF),
            (0x001676E4, 0x0C05AA52, 0x0C06EC36, 0x20),
            (0x00167AFC, 0x0240282D, 0x0C04A712, 0x18),
            (0x0016DD2C, 0x0C04EB96, 0x0C04F3F1, 0x80),
            (0x0016DD44, 0x0C04EB96, 0x0C04C150, 0x7FFF),
            (0x0017A4BC, 0x0C05ED8E, 0x0C0646EA, 0x7FFF),
            (0x0017B594, 0x0C059CE4, 0x0C04F4F7, 0x20),
            (0x0017E52C, 0x14830009, 0x0804625E, 0x7FCE),
            (0x0017EA44, 0x8C440000, 0x0C0820B8, 0x7FCE),
            (0x0017EAB4, 0x8C440000, 0x0C0820B8, 0x7FCE),
            (0x0018331C, 0x0C058ADE, 0x0C064688, 0x7FFF),
            (0x0019A464, 0x24020014, 0x0807FCF7, 0x8000),
            (0x001A1134, 0x24630032, 0x0807FCD0, 0x8000),
            (0x001B177C, 0x3C030031, 0x8E230038, 0x1),
            (0x001B1784, 0x24636798, 0x24630080, 0x1),
            (0x001BCBFC, 0x24630096, 0x0807FCDD, 0x8000),
            (0x001BED7C, 0x0C04C3CC, 0x0C0813A7, 0x100),
            (0x001BED94, 0x0C04C3A6, 0x0C0813AD, 0x100),
            (0x001BEFD0, 0x24040058, 0x24040060, 0x100),
            (0x001BF004, 0x0C06FD68, 0x0C081C9B, 0x100),
            (0x001DBFE0, 0x24630064, 0x0807FCEA, 0x8000),
            (0x0030A104, 0x001663F8, 0x001CBA38, 0xC),
            (0x0030A11C, 0x0015C2F0, 0x00207488, 0x8)
            },
            new (int Va, int Length)[]
            {
            (0x00102910, 0x24C), (0x00102D80, 0x280), (0x0010C338, 0x394), (0x0010CD50, 0x1E0),
            (0x00118978, 0x3F0), (0x00129318, 0x208), (0x00129C48, 0x11C), (0x0012A710, 0x190),
            (0x0012BAE8, 0x4A8), (0x0012CD30, 0x350), (0x0012F790, 0x278), (0x00130540, 0x1C4),
            (0x00131170, 0x148), (0x00132018, 0x154), (0x00132D90, 0x64C), (0x00134CE0, 0x118),
            (0x00134F18, 0x1B0), (0x0013CDF8, 0x208), (0x0013D218, 0x214), (0x00166E28, 0x198),
            (0x0016ACB8, 0x1D0), (0x00191A20, 0x2F0), (0x00196AB8, 0x154), (0x00198968, 0x144),
            (0x00199A60, 0x138), (0x001A28B0, 0x13C), (0x001B8DA0, 0x174), (0x001B9570, 0x114),
            (0x001B9E68, 0xE4), (0x001BAFF8, 0x108), (0x001C36E8, 0x174), (0x001CBA38, 0xD8),
            (0x001D32D8, 0x13C), (0x001E8558, 0x22C), (0x001FF340, 0x190), (0x00204DE0, 0xFC),
            (0x002068C8, 0x1F4), (0x002071C0, 0x158), (0x00207488, 0x90), (0x00207B90, 0x6C),
            (0x002081E0, 0x80), (0x002082E0, 0x40), (0x00208A40, 0xD0), (0x00208CB0, 0x178),
            (0x0020AC28, 0xC8)
            },
            "70c5570203071ac2957d512ed9897728bd2d865df6f4000bd2c8d892fbcaed24",
            new (int Va, int GameVa, (int At, uint Word)[] Words)[]
            {
            (0x001AE930, 0x00316718, new (int, uint)[] {  }),
            (0x001AE9B0, 0x00316798, new (int, uint)[] { (0x24, 0x00198968), (0x3C, 0x0012E418) }),
            (0x00207A90, 0x0031B650, new (int, uint)[] { (0x0C, 0x00206A2C), (0x24, 0x0010CD50) }),
            (0x00207520, 0x0031B5D0, new (int, uint)[] { (0x0C, 0x0020697C) }),
            (0x002089C0, 0x0031C3C8, new (int, uint)[] { (0x3C, 0x001D33A4) }),
            (0x00208160, 0x00316798, new (int, uint)[] { (0x24, 0x0012A710), (0x3C, 0x0012E418) }),
            (0x00207B10, 0x00317470, new (int, uint)[] { (0x0C, 0x0010C634), (0x24, 0x001E8624) }),
            (0x001A9A40, 0x0031B530, new (int, uint)[] { (0x0C, 0x002081E0), (0x24, 0x00102D80) }),
            (0x001A9AC0, 0x0031B5B0, new (int, uint)[] {  }),
            (0x00208260, 0x00313140, new (int, uint)[] { (0x24, 0x0013D218) }),
            (0x00207A10, 0x00319CE8, new (int, uint)[] { (0x24, 0x00196AB8), (0x2C, 0x00204DE0), (0x3C, 0x001FF408) })
            },
            0x390,
            "RkVBORQAAAAAAAAAAAAAAGQAAAAAABBASOH6PgAA8EEAAEhDAADAQQAAQEEAAKBBAABAQgAAAEAyAAAAAADIQwAAyEP0AQAAAAB6RAAAAAAAAIA/"
            + "AAAAAPQENT/0BDU/AAAAAAAAgD/0BDW/9AQ1PwAAgL8AAAAA9AQ1v/QENb8AAAAAAACAv/QENT/0BDW/gIMxABCGMQB4iDEA2JAxAIB9MQAIvTEA"
            + "cLcxAECuMQBQujEAwCIxAIAlMQBgkzEACEkxAHgrMQBgvzEA4DIxAEAyMQCQdjEAKEExAIA+MQDoijEAkDcxADA7MQD4SzEAqEwxAGiNMQBonTEA"
            + "iHkxALh/MQAIyDEAIEUxAPiXMQAAAAAAFwQcSgAQIAAAAAAAAAAAAAAAAABAAAAACgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",
            0x0,
            ""),
        new(19, "the test of Improved Enemy AI with the casters casting more", 0xF, 0x00207320,
            new (int Va, uint Game, uint Patched, int Spells)[]
            {
            (0x0010F4AC, 0x1462000B, 0x0806E3A5, 0x480),
            (0x0010F648, 0x12000207, 0x0806E55C, 0x1480),
            (0x001109C0, 0x14620004, 0x080666D9, 0x80),
            (0x001182C0, 0x27BDFE80, 0x0808235E, 0x8000),
            (0x001182C4, 0xFFB00150, 0x00000000, 0x8000),
            (0x001185D8, 0x27BDFFA0, 0x0804B34C, 0x8000),
            (0x001185DC, 0xFFB40040, 0x00000000, 0x8000),
            (0x0011B758, 0x0100F809, 0x0C0822AB, 0x600),
            (0x0011B7D8, 0xDFB10030, 0x0804BE5C, 0x4000),
            (0x0011C168, 0x27BDFF20, 0x0804AEBA, 0x8000),
            (0x0011C16C, 0xFFB20070, 0x00000000, 0x8000),
            (0x00129778, 0x27BDFFF0, 0x0804F37E, 0x8000),
            (0x0012977C, 0x68C20007, 0x00000000, 0x8000),
            (0x0015E220, 0xC661002C, 0x0C059B8A, 0x480),
            (0x0015E424, 0x2C420003, 0x2C420004, 0x80),
            (0x0015E43C, 0x54620078, 0x0806E596, 0x400),
            (0x0015E50C, 0x10000013, 0x0804AFD1, 0x480),
            (0x0015F160, 0x0C043C8C, 0x0C07A1BC, 0x1000),
            (0x0015F278, 0x0C043C8C, 0x0C07A1BC, 0x1000),
            (0x0015F354, 0x0C043C8C, 0x0C07A1BC, 0x1000),
            (0x00161EF8, 0x2C830017, 0x2C83002A, 0x7FFF),
            (0x00161F00, 0x3C020030, 0x3C020020, 0x7FFF),
            (0x00161F08, 0x24427BC0, 0x24427320, 0x7FFF),
            (0x001676E4, 0x0C05AA52, 0x0C06EC36, 0x20),
            (0x00167AFC, 0x0240282D, 0x0C04A712, 0x18),
            (0x0016DD2C, 0x0C04EB96, 0x0C04F3F1, 0x80),
            (0x0016DD44, 0x0C04EB96, 0x0C04C150, 0x7FFF),
            (0x0017A4BC, 0x0C05ED8E, 0x0C0646EA, 0x7FFF),
            (0x0017B594, 0x0C059CE4, 0x0C04F4F7, 0x20),
            (0x0017E52C, 0x14830009, 0x0804625E, 0x7FCE),
            (0x0017EA44, 0x8C440000, 0x0C0820B8, 0x7FCE),
            (0x0017EAB4, 0x8C440000, 0x0C0820B8, 0x7FCE),
            (0x0018331C, 0x0C058ADE, 0x0C064688, 0x7FFF),
            (0x0019A464, 0x24020014, 0x0807FCF7, 0x8000),
            (0x001A1134, 0x24630032, 0x0807FCD0, 0x8000),
            (0x001B177C, 0x3C030031, 0x8E230038, 0x1),
            (0x001B1784, 0x24636798, 0x24630080, 0x1),
            (0x001BCBFC, 0x24630096, 0x0807FCDD, 0x8000),
            (0x001BED7C, 0x0C04C3CC, 0x0C0813A7, 0x100),
            (0x001BED94, 0x0C04C3A6, 0x0C0813AD, 0x100),
            (0x001BEFD0, 0x24040058, 0x24040060, 0x100),
            (0x001BF004, 0x0C06FD68, 0x0C081C9B, 0x100),
            (0x001DBFE0, 0x24630064, 0x0807FCEA, 0x8000),
            (0x0030A104, 0x001663F8, 0x001CBA38, 0xC),
            (0x0030A11C, 0x0015C2F0, 0x00207488, 0x8)
            },
            new (int Va, int Length)[]
            {
            (0x00102910, 0x24C), (0x00102D80, 0x280), (0x0010C338, 0x394), (0x0010CD50, 0x1E0),
            (0x00118978, 0x3F0), (0x00129318, 0x208), (0x00129C48, 0x11C), (0x0012A710, 0x190),
            (0x0012BAE8, 0x4A8), (0x0012CD30, 0x350), (0x0012F790, 0x278), (0x00130540, 0x1C4),
            (0x00131170, 0x148), (0x00132018, 0x154), (0x00132D90, 0x64C), (0x00134CE0, 0x118),
            (0x00134F18, 0x1B0), (0x0013CDF8, 0x208), (0x0013D218, 0x214), (0x00166E28, 0x198),
            (0x0016ACB8, 0x1D0), (0x00191A20, 0x2F0), (0x00196AB8, 0x154), (0x00198968, 0x144),
            (0x00199A60, 0x138), (0x001A28B0, 0x13C), (0x001B8DA0, 0x174), (0x001B9570, 0x114),
            (0x001B9E68, 0xE4), (0x001BAFF8, 0x108), (0x001C36E8, 0x174), (0x001CBA38, 0xD8),
            (0x001D32D8, 0x13C), (0x001E8558, 0x22C), (0x001FF340, 0x190), (0x00204DE0, 0xFC),
            (0x002068C8, 0x1F4), (0x002071C0, 0x158), (0x00207488, 0x90), (0x00207B90, 0x6C),
            (0x002081E0, 0x80), (0x002082E0, 0x40), (0x00208A40, 0xD0), (0x00208CB0, 0x198),
            (0x0020AC28, 0x40)
            },
            "fa950eeb3395e9a93a6a34be091b60eedb7482a8580fcefb4a0e85f94971b088",
            new (int Va, int GameVa, (int At, uint Word)[] Words)[]
            {
            (0x001AE930, 0x00316718, new (int, uint)[] {  }),
            (0x001AE9B0, 0x00316798, new (int, uint)[] { (0x24, 0x00198968), (0x3C, 0x0012E418) }),
            (0x00207A90, 0x0031B650, new (int, uint)[] { (0x0C, 0x00206A2C), (0x24, 0x0010CD50) }),
            (0x00207520, 0x0031B5D0, new (int, uint)[] { (0x0C, 0x0020697C) }),
            (0x002089C0, 0x0031C3C8, new (int, uint)[] { (0x3C, 0x001D33A4) }),
            (0x00208160, 0x00316798, new (int, uint)[] { (0x24, 0x0012A710), (0x3C, 0x0012E418) }),
            (0x00207B10, 0x00317470, new (int, uint)[] { (0x0C, 0x0010C634), (0x24, 0x001E8624) }),
            (0x001A9A40, 0x0031B530, new (int, uint)[] { (0x0C, 0x002081E0), (0x24, 0x00102D80) }),
            (0x001A9AC0, 0x0031B5B0, new (int, uint)[] {  }),
            (0x00208260, 0x00313140, new (int, uint)[] { (0x24, 0x0013D218) }),
            (0x00207A10, 0x00319CE8, new (int, uint)[] { (0x24, 0x00196AB8), (0x2C, 0x00204DE0), (0x3C, 0x001FF408) })
            },
            0x390,
            "RkVBORMAAAAAAAAAAAAAAGQAAAAAABBASOH6PgAA8EEAAEhDAADAQQAAQEEAAKBBAABAQgAAAEAyAAAAAADIQwAAyEP0AQAAAAB6RAAAAAAAAIA/"
            + "AAAAAPQENT/0BDU/AAAAAAAAgD/0BDW/9AQ1PwAAgL8AAAAA9AQ1v/QENb8AAAAAAACAv/QENT/0BDW/gIMxABCGMQB4iDEA2JAxAIB9MQAIvTEA"
            + "cLcxAECuMQBQujEAwCIxAIAlMQBgkzEACEkxAHgrMQBgvzEA4DIxAEAyMQCQdjEAKEExAIA+MQDoijEAkDcxADA7MQD4SzEAqEwxAGiNMQBonTEA"
            + "iHkxALh/MQAIyDEAIEUxAPiXMQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAABAAAAACgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",
            0x0,
            ""),
        new(18, "the first Improved Enemy AI: smarter targeting, surround the hero, raise the alarm", 0x7, 0x00207270,
            new (int Va, uint Game, uint Patched, int Spells)[]
            {
            (0x0010F4AC, 0x1462000B, 0x0806E3A5, 0x480),
            (0x0010F648, 0x12000207, 0x0806E55C, 0x1480),
            (0x001109C0, 0x14620004, 0x080666D9, 0x80),
            (0x001182C0, 0x27BDFE80, 0x080813A5, 0x8000),
            (0x001182C4, 0xFFB00150, 0x00000000, 0x8000),
            (0x001185D8, 0x27BDFFA0, 0x0804B34C, 0x8000),
            (0x001185DC, 0xFFB40040, 0x00000000, 0x8000),
            (0x0011B758, 0x0100F809, 0x0C0822A5, 0x600),
            (0x0011B7D8, 0xDFB10030, 0x0804BE5C, 0x4000),
            (0x0011C168, 0x27BDFF20, 0x0804AEBA, 0x8000),
            (0x0011C16C, 0xFFB20070, 0x00000000, 0x8000),
            (0x00129778, 0x27BDFFF0, 0x0804F37E, 0x8000),
            (0x0012977C, 0x68C20007, 0x00000000, 0x8000),
            (0x0015E220, 0xC661002C, 0x0C059B8A, 0x480),
            (0x0015E424, 0x2C420003, 0x2C420004, 0x80),
            (0x0015E43C, 0x54620078, 0x0806E596, 0x400),
            (0x0015E50C, 0x10000013, 0x0804AFD1, 0x480),
            (0x0015F160, 0x0C043C8C, 0x0C07A1BC, 0x1000),
            (0x0015F278, 0x0C043C8C, 0x0C07A1BC, 0x1000),
            (0x0015F354, 0x0C043C8C, 0x0C07A1BC, 0x1000),
            (0x00161EF8, 0x2C830017, 0x2C83002A, 0x7FFF),
            (0x00161F00, 0x3C020030, 0x3C020020, 0x7FFF),
            (0x00161F08, 0x24427BC0, 0x24427270, 0x7FFF),
            (0x001676E4, 0x0C05AA52, 0x0C06EC36, 0x20),
            (0x00167AFC, 0x0240282D, 0x0C04A712, 0x18),
            (0x0016DD2C, 0x0C04EB96, 0x0C04F3F1, 0x80),
            (0x0016DD44, 0x0C04EB96, 0x0C04C150, 0x7FFF),
            (0x0017A4BC, 0x0C05ED8E, 0x0C0646EA, 0x7FFF),
            (0x0017B594, 0x0C059CE4, 0x0C04F4F7, 0x20),
            (0x0017E52C, 0x14830009, 0x0804625E, 0x7FCE),
            (0x0017EA44, 0x8C440000, 0x0C082344, 0x7FCE),
            (0x0017EAB4, 0x8C440000, 0x0C082344, 0x7FCE),
            (0x0018331C, 0x0C058ADE, 0x0C064688, 0x7FFF),
            (0x001B177C, 0x3C030031, 0x8E230038, 0x1),
            (0x001B1784, 0x24636798, 0x24630080, 0x1),
            (0x001BED7C, 0x0C04C3CC, 0x0C0820B8, 0x100),
            (0x001BED94, 0x0C04C3A6, 0x0C0820BE, 0x100),
            (0x001BEFD0, 0x24040058, 0x24040060, 0x100),
            (0x001BF004, 0x0C06FD68, 0x0C081C70, 0x100),
            (0x0030A104, 0x001663F8, 0x001CBA38, 0xC),
            (0x0030A11C, 0x0015C2F0, 0x00207318, 0x8)
            },
            new (int Va, int Length)[]
            {
            (0x00102910, 0x24C), (0x00102D80, 0x280), (0x0010C338, 0x394), (0x0010CD50, 0x1E0),
            (0x00118978, 0x3F0), (0x00129318, 0x208), (0x00129C48, 0x11C), (0x0012A710, 0x190),
            (0x0012BAE8, 0x4A8), (0x0012CD30, 0x350), (0x0012F790, 0x278), (0x00130540, 0x1C4),
            (0x00131170, 0x148), (0x00132018, 0x154), (0x00132D90, 0x64C), (0x00134CE0, 0x118),
            (0x00134F18, 0x1B0), (0x0013CDF8, 0x208), (0x0013D218, 0x214), (0x00166E28, 0x198),
            (0x0016ACB8, 0x1D0), (0x00191A20, 0x2F0), (0x00196AB8, 0x154), (0x00198968, 0x144),
            (0x00199A60, 0x138), (0x001A28B0, 0x13C), (0x001B8DA0, 0x174), (0x001B9570, 0x114),
            (0x001B9E68, 0xE4), (0x001BAFF8, 0x108), (0x001C36E8, 0x174), (0x001CBA38, 0xD8),
            (0x001D32D8, 0x13C), (0x001E8558, 0x22C), (0x001FF340, 0x184), (0x00204DE0, 0xFC),
            (0x002068C8, 0x1EC), (0x002071C0, 0xAC), (0x00207318, 0xD8), (0x00207B90, 0x6C),
            (0x00208160, 0x80), (0x002082E0, 0x40), (0x002089C0, 0x138), (0x00208CB0, 0x120)
            },
            "7bf2ec0866d853109f1cc18b717041df61f48e00b5966bca51df3b3920fb80e6",
            new (int Va, int GameVa, (int At, uint Word)[] Words)[]
            {
            (0x001AE930, 0x00316718, new (int, uint)[] {  }),
            (0x001AE9B0, 0x00316798, new (int, uint)[] { (0x24, 0x00198968), (0x3C, 0x0012E418) }),
            (0x00207A10, 0x0031B650, new (int, uint)[] { (0x0C, 0x00206A24), (0x24, 0x0010CD50) }),
            (0x00207490, 0x0031B5D0, new (int, uint)[] { (0x0C, 0x002068C8) }),
            (0x00208260, 0x0031C3C8, new (int, uint)[] { (0x3C, 0x001D33A4) }),
            (0x00207B10, 0x00316798, new (int, uint)[] { (0x24, 0x0012A710), (0x3C, 0x0012E418) }),
            (0x00207A90, 0x00317470, new (int, uint)[] { (0x0C, 0x0010C634), (0x24, 0x001E8624) }),
            (0x001A9A40, 0x0031B530, new (int, uint)[] { (0x0C, 0x00208160), (0x24, 0x00102D80) }),
            (0x001A9AC0, 0x0031B5B0, new (int, uint)[] {  }),
            (0x002081E0, 0x00313140, new (int, uint)[] { (0x24, 0x0013D218) }),
            (0x00207510, 0x00319CE8, new (int, uint)[] { (0x24, 0x00196AB8), (0x2C, 0x001FF408), (0x3C, 0x001FF340) })
            },
            0x390,
            "RkVBORIAAAAAAAAAAAAAAGQAAAAAABBASOH6PgAA8EEAAEhDAADAQQAAQEEAAKBBAABAQgAAAEAyAAAAAADIQwAAyEP0AQAAAAB6RAAAAAAAAIA/"
            + "AAAAAPQENT/0BDU/AAAAAAAAgD/0BDW/9AQ1PwAAgL8AAAAA9AQ1v/QENb8AAAAAAACAv/QENT/0BDW/gIMxABCGMQB4iDEA2JAxAIB9MQAIvTEA"
            + "cLcxAECuMQBQujEAwCIxAIAlMQBgkzEACEkxAHgrMQBgvzEA4DIxAEAyMQCQdjEAKEExAIA+MQDoijEAkDcxADA7MQD4SzEAqEwxAGiNMQBonTEA"
            + "iHkxALh/MQAIyDEAIEUxAPiXMQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",
            0x0,
            "")
    };

    /// <summary>
    /// Version 17 (shatter-test1: all fifteen, Shatter the newest; its eight data blocks where version 26's are, with version
    /// 26's layout, so its settings are read where version 26 keeps them; no ninth block, no Improved Enemy AI): its hooks
    /// (and which of its spells need each, the same masks as version 26's), its code's pieces with their SHA-256, its vtable
    /// copies and its cast table's place.
    /// </summary>
    public const int V17Version = 17, V17CastTableVa = 0x001C36F0;

    public static readonly (int Va, uint Game, uint Patched, int Spells)[] V17Hooks =
    {
        (0x0010F4AC, 0x1462000B, 0x08068A62, 0x480),
        (0x0010F648, 0x12000207, 0x08065AAE, 0x1480),
        (0x001109C0, 0x14620004, 0x0806BA7F, 0x80),
        (0x0011B758, 0x0100F809, 0x0C081378, 0x600),
        (0x0011B7D8, 0xDFB10030, 0x0804BE5C, 0x4000),
        (0x0015E220, 0xC661002C, 0x0C04D3C6, 0x480),
        (0x0015E424, 0x2C420003, 0x2C420004, 0x80),
        (0x0015E43C, 0x54620078, 0x08074CFC, 0x400),
        (0x0015E50C, 0x10000013, 0x080666D0, 0x480),
        (0x0015F160, 0x0C043C8C, 0x0C070DE6, 0x1000),
        (0x0015F278, 0x0C043C8C, 0x0C070DE6, 0x1000),
        (0x0015F354, 0x0C043C8C, 0x0C070DE6, 0x1000),
        (0x00161EF8, 0x2C830017, 0x2C83002A, 0x7FFF),
        (0x00161F00, 0x3C020030, 0x3C02001C, 0x7FFF),
        (0x00161F08, 0x24427BC0, 0x244236F0, 0x7FFF),
        (0x001676E4, 0x0C05AA52, 0x0C04D42C, 0x20),
        (0x00167AFC, 0x0240282D, 0x0C04A712, 0x18),
        (0x0016DD2C, 0x0C04EB96, 0x0C070E0B, 0x80),
        (0x0016DD44, 0x0C04EB96, 0x0C04C150, 0x7FFF),
        (0x0017A4BC, 0x0C05ED8E, 0x0C04F486, 0x7FFF),
        (0x0017B594, 0x0C059CE4, 0x0C059BD9, 0x20),
        (0x0017E52C, 0x14830009, 0x0804625E, 0x7FCE),
        (0x0017EA44, 0x8C440000, 0x0C06E58B, 0x7FCE),
        (0x0017EAB4, 0x8C440000, 0x0C06E58B, 0x7FCE),
        (0x0018331C, 0x0C058ADE, 0x0C04F37E, 0x7FFF),
        (0x001B177C, 0x3C030031, 0x8E230038, 0x1),
        (0x001B1784, 0x24636798, 0x24630080, 0x1),
        (0x001BED7C, 0x0C04C3CC, 0x0C06A6C1, 0x100),
        (0x001BED94, 0x0C04C3A6, 0x0C06A6C7, 0x100),
        (0x001BEFD0, 0x24040058, 0x24040060, 0x100),
        (0x001BF004, 0x0C06FD68, 0x0C06EBFE, 0x100),
        (0x0030A104, 0x001663F8, 0x001A28B0, 0xC),
        (0x0030A11C, 0x0015C2F0, 0x001D32D8, 0x8)
    };

    public static readonly (int Va, int Length)[] V17Code =
    {
        (0x00102910, 0x24C), (0x00102D80, 0x280), (0x0010C338, 0x394), (0x0010CD50, 0x1E0),
        (0x00118978, 0x3F0), (0x00129318, 0x208), (0x00129C48, 0x11C), (0x0012A710, 0x190),
        (0x0012BAE8, 0x4A8), (0x0012CD30, 0x350), (0x0012F790, 0x278), (0x00130540, 0x1C4),
        (0x00131170, 0x148), (0x00132018, 0x154), (0x00132D90, 0x65C), (0x00134CE0, 0x118),
        (0x00134F18, 0x1C0), (0x0013CDF8, 0x188), (0x0013D218, 0x218), (0x00166E28, 0x18C),
        (0x0016ACB8, 0x1D0), (0x00191C20, 0xF4), (0x00196AB8, 0x158), (0x00198968, 0x14C),
        (0x00199A60, 0x12C), (0x001A28B0, 0x158), (0x001A9A38, 0x10C), (0x001AE930, 0x100),
        (0x001B8DA0, 0x174), (0x001B9570, 0xFC), (0x001B9E68, 0xEC), (0x001BAFF8, 0xEC),
        (0x001C3798, 0xD0), (0x001CBA38, 0x110), (0x001D32D8, 0x90), (0x001D33F0, 0x2C),
        (0x001FF3C0, 0xD4), (0x00204DE0, 0xC4)
    };

    public const string V17CodeSha256 = "52963bcbb7346fe5365041931b4825515d1698cdd26d505c5743048f5c01f2e9";

    public static readonly (int Va, int GameVa, (int At, uint Word)[] Words)[] V17VTables =
    {
        (0x00191B20, 0x00316718, new (int, uint)[] {  }),
        (0x00191BA0, 0x00316798, new (int, uint)[] { (0x24, 0x001332A8), (0x3C, 0x0012E418) }),
        (0x001E8560, 0x0031B650, new (int, uint)[] { (0x0C, 0x001CBA38), (0x24, 0x0010CD50) }),
        (0x0013CF80, 0x0031B5D0, new (int, uint)[] { (0x0C, 0x0013D380) }),
        (0x001FF340, 0x0031C3C8, new (int, uint)[] { (0x3C, 0x00196BA0) }),
        (0x001E8660, 0x00316798, new (int, uint)[] { (0x24, 0x0012A710), (0x3C, 0x0012E418) }),
        (0x001E85E0, 0x00317470, new (int, uint)[] { (0x0C, 0x0010C634), (0x24, 0x001AE930) }),
        (0x00191A20, 0x0031B530, new (int, uint)[] { (0x0C, 0x001CBAC8), (0x24, 0x00102D80) }),
        (0x00191AA0, 0x0031B5B0, new (int, uint)[] {  }),
        (0x001E86E0, 0x00313140, new (int, uint)[] { (0x24, 0x00132D90) }),
        (0x001D3370, 0x00319CE8, new (int, uint)[] { (0x24, 0x00132018), (0x2C, 0x001B9570), (0x3C, 0x0012CFB8) })
    };

    /// <summary>
    /// Version 16 (phase-axe-test1: all fourteen, Phase Axe the newest; its seven data blocks where version 17's are, with
    /// version 17's layout but a second block 8 bytes shorter, so its settings are read where version 17 keeps them): its hooks
    /// (and which of its spells need each, the same masks as version 17's), its code's pieces with their SHA-256, its vtable
    /// copies and its cast table's place.
    /// </summary>
    public const int V16Version = 16, V16CastTableVa = 0x001B8DA0;

    public static readonly (int Va, uint Game, uint Patched, int Spells)[] V16Hooks =
    {
        (0x0010F4AC, 0x1462000B, 0x0804D415, 0x480),
        (0x0010F648, 0x12000207, 0x08059B8A, 0x1480),
        (0x001109C0, 0x14620004, 0x080431A7, 0x80),
        (0x0011B758, 0x0100F809, 0x0C070DFC, 0x600),
        (0x0015E220, 0xC661002C, 0x0C04CBCD, 0x480),
        (0x0015E424, 0x2C420003, 0x2C420004, 0x80),
        (0x0015E43C, 0x54620078, 0x0804634F, 0x400),
        (0x0015E50C, 0x10000013, 0x08072EA6, 0x480),
        (0x0015F160, 0x0C043C8C, 0x0C065AE1, 0x1000),
        (0x0015F278, 0x0C043C8C, 0x0C065AE1, 0x1000),
        (0x0015F354, 0x0C043C8C, 0x0C065AE1, 0x1000),
        (0x00161EF8, 0x2C830017, 0x2C830029, 0x3FFF),
        (0x00161F00, 0x3C020030, 0x3C02001C, 0x3FFF),
        (0x00161F08, 0x24427BC0, 0x24428DA0, 0x3FFF),
        (0x001676E4, 0x0C05AA52, 0x0C074CD5, 0x20),
        (0x00167AFC, 0x0240282D, 0x0C04A712, 0x18),
        (0x0016DD2C, 0x0C04EB96, 0x0C074CC6, 0x80),
        (0x0016DD44, 0x0C04EB96, 0x0C04CB64, 0x3FFF),
        (0x0017A4BC, 0x0C05ED8E, 0x0C04CC33, 0x3FFF),
        (0x0017B594, 0x0C059CE4, 0x0C06BA77, 0x20),
        (0x0017E52C, 0x14830009, 0x080430CE, 0x3FCE),
        (0x0017EA44, 0x8C440000, 0x0C06E3B5, 0x3FCE),
        (0x0017EAB4, 0x8C440000, 0x0C06E3B5, 0x3FCE),
        (0x0018331C, 0x0C058ADE, 0x0C04B3BD, 0x3FFF),
        (0x001B177C, 0x3C030031, 0x8E230038, 0x1),
        (0x001B1784, 0x24636798, 0x24630080, 0x1),
        (0x001BED7C, 0x0C04C3CC, 0x0C04AFD4, 0x100),
        (0x001BED94, 0x0C04C3A6, 0x0C04AFDA, 0x100),
        (0x001BEFD0, 0x24040058, 0x24040060, 0x100),
        (0x001BF004, 0x0C06FD68, 0x0C06BA4C, 0x100),
        (0x0030A104, 0x001663F8, 0x00191B00, 0xC),
        (0x0030A11C, 0x0015C2F0, 0x001B9570, 0x8)
    };

    public static readonly (int Va, int Length)[] V16Code =
    {
        (0x00102910, 0x24C), (0x00102D80, 0x280), (0x0010C338, 0x398), (0x0010CD50, 0x1E0),
        (0x00118978, 0x3F0), (0x00129318, 0x208), (0x00129C48, 0x11C), (0x0012A710, 0x190),
        (0x0012BAE8, 0x4A8), (0x0012CD30, 0x338), (0x0012F790, 0x278), (0x00130540, 0x1A4),
        (0x00131170, 0x148), (0x00132018, 0x154), (0x00132D90, 0x5D4), (0x00134CE0, 0x118),
        (0x00134F18, 0x1BC), (0x0013CDF8, 0x104), (0x0013D320, 0xF4), (0x00166E28, 0x198),
        (0x0016ACB8, 0x1C4), (0x00191A20, 0x2F4), (0x00196AB8, 0x160), (0x00198968, 0xCC),
        (0x00199A60, 0x128), (0x001A28B0, 0x14C), (0x001A9A38, 0x118), (0x001AE930, 0xFC),
        (0x001B8E44, 0xD0), (0x001B9570, 0x90), (0x001B9EF0, 0x68), (0x001BB080, 0x80),
        (0x001C37F0, 0x64), (0x001CBA38, 0xEC), (0x001D32D8, 0xA4)
    };

    public const string V16CodeSha256 = "801c745b12b97df3f13c520aaee0506fb093bf1ab87afbbd679a2bedd4d1de8b";

    public static readonly (int Va, int GameVa, (int At, uint Word)[] Words)[] V16VTables =
    {
        (0x0013D220, 0x00316718, new (int, uint)[] {  }),
        (0x0013D2A0, 0x00316798, new (int, uint)[] { (0x24, 0x00133220), (0x3C, 0x0012E418) }),
        (0x001B9600, 0x0031B650, new (int, uint)[] { (0x0C, 0x001B8E44), (0x24, 0x0010CD50) }),
        (0x00133370, 0x0031B5D0, new (int, uint)[] { (0x0C, 0x00166F10) }),
        (0x001C3770, 0x0031C3C8, new (int, uint)[] { (0x3C, 0x00191CA4) }),
        (0x001BB000, 0x00316798, new (int, uint)[] { (0x24, 0x0012A710), (0x3C, 0x0012E418) }),
        (0x001B9E70, 0x00317470, new (int, uint)[] { (0x0C, 0x001A2964), (0x24, 0x00198968) }),
        (0x0013CF00, 0x0031B530, new (int, uint)[] { (0x0C, 0x001BB080), (0x24, 0x00102D80) }),
        (0x0013CF80, 0x0031B5B0, new (int, uint)[] {  }),
        (0x001C36F0, 0x00313140, new (int, uint)[] { (0x24, 0x0012CD30) }),
        (0x00198A40, 0x00319CE8, new (int, uint)[] { (0x24, 0x00132018), (0x2C, 0x00199A60), (0x3C, 0x0012F940) })
    };

    /// <summary>
    /// Version 15 (expert-focus-test1: all thirteen, Expert Focus the newest; its six data blocks where version 17's are, with
    /// version 17's layout but a second block 8 bytes shorter, so its settings are read where version 17 keeps them): its hooks
    /// (and which of its spells need each, the same masks as version 17's), its code's pieces with their SHA-256, its vtable
    /// copies and its cast table's place.
    /// </summary>
    public const int V15Version = 15, V15CastTableVa = 0x00191B80;

    public static readonly (int Va, uint Game, uint Patched, int Spells)[] V15Hooks =
    {
        (0x0010F4AC, 0x1462000B, 0x08068A2C, 0x480),
        (0x0010F648, 0x12000207, 0x0804F3BB, 0x1480),
        (0x001109C0, 0x14620004, 0x0804634F, 0x80),
        (0x0011B758, 0x0100F809, 0x0C04CCE2, 0x600),
        (0x0015E220, 0xC661002C, 0x0C04B3B8, 0x480),
        (0x0015E424, 0x2C420003, 0x2C420004, 0x80),
        (0x0015E43C, 0x54620078, 0x0804D378, 0x400),
        (0x0015E50C, 0x10000013, 0x0804F4F7, 0x480),
        (0x0015F160, 0x0C043C8C, 0x0C065AAE, 0x1000),
        (0x0015F278, 0x0C043C8C, 0x0C065AAE, 0x1000),
        (0x0015F354, 0x0C043C8C, 0x0C065AAE, 0x1000),
        (0x00161EF8, 0x2C830017, 0x2C830028, 0x1FFF),
        (0x00161F00, 0x3C020030, 0x3C020019, 0x1FFF),
        (0x00161F08, 0x24427BC0, 0x24421B80, 0x1FFF),
        (0x001676E4, 0x0C05AA52, 0x0C04F3F5, 0x20),
        (0x00167AFC, 0x0240282D, 0x0C04A712, 0x18),
        (0x0016DD2C, 0x0C04EB96, 0x0C065AF7, 0x80),
        (0x0016DD44, 0x0C04EB96, 0x0C04C150, 0x1FFF),
        (0x0017A4BC, 0x0C05ED8E, 0x0C04CBBC, 0x1FFF),
        (0x0017B594, 0x0C059CE4, 0x0C04AFCD, 0x20),
        (0x0017E52C, 0x14830009, 0x080430CE, 0x1FCE),
        (0x0017EA44, 0x8C440000, 0x0C0662A0, 0x1FCE),
        (0x0017EAB4, 0x8C440000, 0x0C0662A0, 0x1FCE),
        (0x0018331C, 0x0C058ADE, 0x0C04CB64, 0x1FFF),
        (0x001B177C, 0x3C030031, 0x8E230038, 0x1),
        (0x001B1784, 0x24636798, 0x24630080, 0x1),
        (0x001BED7C, 0x0C04C3CC, 0x0C04C1B1, 0x100),
        (0x001BED94, 0x0C04C3A6, 0x0C04C1B7, 0x100),
        (0x001BEFD0, 0x24040058, 0x24040060, 0x100),
        (0x001BF004, 0x0C06FD68, 0x0C0646B3, 0x100),
        (0x0030A104, 0x001663F8, 0x00166E28, 0xC),
        (0x0030A11C, 0x0015C2F0, 0x00198968, 0x8)
    };

    public static readonly (int Va, int Length)[] V15Code =
    {
        (0x00102910, 0x24C), (0x00102D80, 0x280), (0x0010C338, 0x398), (0x0010CD50, 0x1E0),
        (0x00118978, 0x3F8), (0x00129318, 0x208), (0x00129C48, 0x11C), (0x0012A710, 0x190),
        (0x0012BAE8, 0x49C), (0x0012CD30, 0x348), (0x0012F790, 0x270), (0x00130540, 0x1C4),
        (0x00131170, 0x148), (0x00132018, 0x144), (0x00132D90, 0x65C), (0x00134DE0, 0x2C),
        (0x00135020, 0xB4), (0x0013CDF8, 0x204), (0x0013D218, 0x210), (0x00166E28, 0x188),
        (0x0016ACB8, 0x198), (0x00191A20, 0x158), (0x00191C20, 0xF8), (0x00196AB8, 0x160),
        (0x00198968, 0x90), (0x00198A80, 0x40), (0x001A28B0, 0x80), (0x001A29B0, 0x40),
        (0x001A9A38, 0x80), (0x001AE9B0, 0x70), (0x001B8DA0, 0x118)
    };

    public const string V15CodeSha256 = "fe84a72032c5fb0746e31e6db603f4c16241f59baa8446e77d4b2e8233698353";

    public static readonly (int Va, int GameVa, (int At, uint Word)[] Words)[] V15VTables =
    {
        (0x00134F20, 0x00316718, new (int, uint)[] {  }),
        (0x00134FA0, 0x00316798, new (int, uint)[] { (0x24, 0x00132018), (0x3C, 0x0012E418) }),
        (0x00199A60, 0x0031B650, new (int, uint)[] { (0x0C, 0x00196B4C), (0x24, 0x0010CD50) }),
        (0x00198A00, 0x0031B5D0, new (int, uint)[] { (0x0C, 0x00166F00) }),
        (0x001AE930, 0x0031C3C8, new (int, uint)[] { (0x3C, 0x001AE9B0) }),
        (0x001A2930, 0x00316798, new (int, uint)[] { (0x24, 0x0012A710), (0x3C, 0x0012E418) }),
        (0x00199AE0, 0x00317470, new (int, uint)[] { (0x0C, 0x00191C20), (0x24, 0x0016AD84) }),
        (0x00134CE0, 0x0031B530, new (int, uint)[] { (0x0C, 0x001A9A38), (0x24, 0x00102D80) }),
        (0x00134D60, 0x0031B5B0, new (int, uint)[] {  }),
        (0x001A9AC0, 0x00313140, new (int, uint)[] { (0x24, 0x0012BD70) })
    };

    /// <summary>
    /// Version 14 (dimension-door-test1: all twelve, Dimension Door the newest; its five data blocks where version 17's are, with
    /// version 17's layout but a second block 16 bytes shorter, so its settings are read where version 17 keeps them): its hooks
    /// (and which of its spells need each, the same masks as version 17's), its code's pieces with their SHA-256, its vtable
    /// copies and its cast table's place.
    /// </summary>
    public const int V14Version = 14, V14CastTableVa = 0x00133350;

    public static readonly (int Va, uint Game, uint Patched, int Spells)[] V14Hooks =
    {
        (0x0010F4AC, 0x1462000B, 0x08065AD0, 0x480),
        (0x0010F648, 0x12000207, 0x08059BBD, 0x480),
        (0x001109C0, 0x14620004, 0x0804634F, 0x80),
        (0x0011B758, 0x0100F809, 0x0C04C1A9, 0x600),
        (0x0015E220, 0xC661002C, 0x0C04B3B8, 0x480),
        (0x0015E424, 0x2C420003, 0x2C420004, 0x80),
        (0x0015E43C, 0x54620078, 0x0804D378, 0x400),
        (0x0015E50C, 0x10000013, 0x08065AF0, 0x480),
        (0x00161EF8, 0x2C830017, 0x2C830027, 0xFFF),
        (0x00161F00, 0x3C020030, 0x3C020013, 0xFFF),
        (0x00161F08, 0x24427BC0, 0x24423350, 0xFFF),
        (0x001676E4, 0x0C05AA52, 0x0C0666D8, 0x20),
        (0x00167AFC, 0x0240282D, 0x0C04CC04, 0x18),
        (0x0016DD2C, 0x0C04EB96, 0x0C068A6F, 0x80),
        (0x0016DD44, 0x0C04EB96, 0x0C04C150, 0xFFF),
        (0x0017A4BC, 0x0C05ED8E, 0x0C04A712, 0xFFF),
        (0x0017B594, 0x0C059CE4, 0x0C04AFCD, 0x20),
        (0x0017E52C, 0x14830009, 0x080430CE, 0xFCE),
        (0x0017EA44, 0x8C440000, 0x0C064738, 0xFCE),
        (0x0017EAB4, 0x8C440000, 0x0C064738, 0xFCE),
        (0x0018331C, 0x0C058ADE, 0x0C04C45C, 0xFFF),
        (0x001B177C, 0x3C030031, 0x8E230038, 0x1),
        (0x001B1784, 0x24636798, 0x24630080, 0x1),
        (0x001BED7C, 0x0C04C3CC, 0x0C04F3EF, 0x100),
        (0x001BED94, 0x0C04C3A6, 0x0C04F3F5, 0x100),
        (0x001BEFD0, 0x24040058, 0x24040060, 0x100),
        (0x001BF004, 0x0C06FD68, 0x0C05AB59, 0x100),
        (0x0030A104, 0x001663F8, 0x0013D218, 0xC),
        (0x0030A11C, 0x0015C2F0, 0x00191B48, 0x8)
    };

    public static readonly (int Va, int Length)[] V14Code =
    {
        (0x00102910, 0x24C), (0x00102D80, 0x280), (0x0010C338, 0x398), (0x0010CD50, 0x1E0),
        (0x00118978, 0x3F8), (0x00129318, 0x208), (0x00129C48, 0x12C), (0x0012A710, 0x190),
        (0x0012BAE8, 0x49C), (0x0012CD30, 0x348), (0x0012F790, 0x270), (0x00130540, 0x1C8),
        (0x00131170, 0x14C), (0x00132018, 0x148), (0x00132D90, 0x5B8), (0x00134DE0, 0x2C),
        (0x00135020, 0xB0), (0x0013CDF8, 0x204), (0x0013D218, 0x214), (0x00166E28, 0x190),
        (0x0016ACB8, 0x1C4), (0x00191A20, 0x1B8), (0x00191CE0, 0x40), (0x00196B40, 0xCC),
        (0x001989F0, 0xC0), (0x00199B60, 0x28), (0x001A28B0, 0x148)
    };

    public const string V14CodeSha256 = "4c1419f63a6fd1d419ae557a6088cc29777bad114f819f64baded718eca64114";

    public static readonly (int Va, int GameVa, (int At, uint Word)[] Words)[] V14VTables =
    {
        (0x00134F20, 0x00316718, new (int, uint)[] {  }),
        (0x00134FA0, 0x00316798, new (int, uint)[] { (0x24, 0x00132D90), (0x3C, 0x0012E418) }),
        (0x00191C60, 0x0031B650, new (int, uint)[] { (0x0C, 0x00191AB8), (0x24, 0x0010CD50) }),
        (0x00191BE0, 0x0031B5D0, new (int, uint)[] { (0x0C, 0x00135020) }),
        (0x00199AE0, 0x0031C3C8, new (int, uint)[] { (0x3C, 0x0013D3BC) }),
        (0x00198970, 0x00316798, new (int, uint)[] { (0x24, 0x0012A710), (0x3C, 0x0012E418) }),
        (0x00196AC0, 0x00317470, new (int, uint)[] { (0x0C, 0x00191A20), (0x24, 0x00166E28) }),
        (0x00134CE0, 0x0031B530, new (int, uint)[] { (0x0C, 0x001989F0), (0x24, 0x00102D80) }),
        (0x00134D60, 0x0031B5B0, new (int, uint)[] {  }),
        (0x00199A60, 0x00313140, new (int, uint)[] { (0x24, 0x0012BD70) })
    };

    /// <summary>
    /// Version 13 (poison-gas-arrow-test1: all eleven, Poison Gas Arrow the newest; its four data blocks where version 17's are,
    /// with version 17's layout, so its settings are read where version 17 keeps them): its hooks (and which of its spells need
    /// each, the same masks as version 17's), its code's pieces with their SHA-256, its vtable copies and its cast table's place.
    /// </summary>
    public const int V13Version = 13, V13CastTableVa = 0x00166E30;

    public static readonly (int Va, uint Game, uint Patched, int Spells)[] V13Hooks =
    {
        (0x0010F4AC, 0x1462000B, 0x0804C1A1, 0x480),
        (0x0010F648, 0x12000207, 0x0804F3B1, 0x480),
        (0x001109C0, 0x14620004, 0x0804634F, 0x80),
        (0x0011B758, 0x0100F809, 0x0C04CCE2, 0x600),
        (0x0015E220, 0xC661002C, 0x0C04B3B5, 0x480),
        (0x0015E424, 0x2C420003, 0x2C420004, 0x80),
        (0x0015E43C, 0x54620078, 0x0805AB94, 0x400),
        (0x0015E50C, 0x10000013, 0x08065AAE, 0x480),
        (0x00161EF8, 0x2C830017, 0x2C830025, 0x7FF),
        (0x00161F00, 0x3C020030, 0x3C020016, 0x7FF),
        (0x00161F08, 0x24427BC0, 0x24426E30, 0x7FF),
        (0x001676E4, 0x0C05AA52, 0x0C04D42C, 0x20),
        (0x00167AFC, 0x0240282D, 0x0C04A712, 0x18),
        (0x0016DD2C, 0x0C04EB96, 0x0C06625A, 0x80),
        (0x0016DD44, 0x0C04EB96, 0x0C04C45C, 0x7FF),
        (0x0017A4BC, 0x0C05ED8E, 0x0C04BE36, 0x7FF),
        (0x0017B594, 0x0C059CE4, 0x0C04A537, 0x20),
        (0x0017E52C, 0x14830009, 0x0804AEBA, 0x7CE),
        (0x0017EA44, 0x8C440000, 0x0C065AD1, 0x7CE),
        (0x0017EAB4, 0x8C440000, 0x0C065AD1, 0x7CE),
        (0x0018331C, 0x0C058ADE, 0x0C04CB64, 0x7FF),
        (0x001B177C, 0x3C030031, 0x8E230038, 0x1),
        (0x001B1784, 0x24636798, 0x24630080, 0x1),
        (0x001BED7C, 0x0C04C3CC, 0x0C065AC1, 0x100),
        (0x001BED94, 0x0C04C3A6, 0x0C065AC7, 0x100),
        (0x001BEFD0, 0x24040058, 0x24040060, 0x100),
        (0x001BF004, 0x0C06FD68, 0x0C04F4B1, 0x100),
        (0x0030A104, 0x001663F8, 0x00134CE0, 0xC),
        (0x0030A11C, 0x0015C2F0, 0x0016ACB8, 0x8)
    };

    public static readonly (int Va, int Length)[] V13Code =
    {
        (0x00102910, 0x24C), (0x00102D80, 0x280), (0x0010C338, 0x38C), (0x0010CD50, 0x1E0),
        (0x00118978, 0x3F8), (0x00129318, 0x214), (0x00129C48, 0x11C), (0x0012A710, 0x190),
        (0x0012BAE8, 0x4A4), (0x0012CD30, 0x33C), (0x0012F790, 0x260), (0x00130540, 0x1C4),
        (0x00131170, 0x140), (0x00132018, 0x13C), (0x00132D90, 0x138), (0x001330D0, 0x31C),
        (0x00134CE0, 0x138), (0x00134F18, 0x1C0), (0x0013CDF8, 0x200), (0x0013D218, 0x1F0),
        (0x00166EC4, 0xFC), (0x0016ACB8, 0x90), (0x0016AE50, 0x2C), (0x00191B20, 0x80),
        (0x00191CA0, 0x6C), (0x00196AB8, 0x14C), (0x00198968, 0x3C)
    };

    public const string V13CodeSha256 = "c2cb4902b7d9d51c092807ae64d7f118001d60765fe0590dae66aef0356b74bb";

    public static readonly (int Va, int GameVa, (int At, uint Word)[] Words)[] V13VTables =
    {
        (0x00132FD0, 0x00316718, new (int, uint)[] {  }),
        (0x00133050, 0x00316798, new (int, uint)[] { (0x24, 0x00130540), (0x3C, 0x0012E418) }),
        (0x0016ADD0, 0x0031B650, new (int, uint)[] { (0x0C, 0x00166EC4), (0x24, 0x0010CD50) }),
        (0x0016AD50, 0x0031B5D0, new (int, uint)[] { (0x0C, 0x0012BEDC) }),
        (0x00191C20, 0x0031C3C8, new (int, uint)[] { (0x3C, 0x0013CF88) }),
        (0x00191AA0, 0x00316798, new (int, uint)[] { (0x24, 0x0012A710), (0x3C, 0x0012E418) }),
        (0x00191A20, 0x00317470, new (int, uint)[] { (0x0C, 0x0013D370), (0x24, 0x0013CDF8) }),
        (0x00132ED0, 0x0031B530, new (int, uint)[] { (0x0C, 0x00191B20), (0x24, 0x00102D80) }),
        (0x00132F50, 0x0031B5B0, new (int, uint)[] {  }),
        (0x00191BA0, 0x00313140, new (int, uint)[] { (0x24, 0x00129318) })
    };

    /// <summary>
    /// Version 12 (insect-plague-test2: all ten, Insect Plague the newest; its three data blocks where version 17's are, with
    /// version 17's layout, so its settings are read where version 17 keeps them): its hooks (and which of its spells need each,
    /// the same masks as version 17's), its code's pieces with their SHA-256, its vtable copies and its cast table's place.
    /// </summary>
    public const int V12Version = 12, V12CastTableVa = 0x00134D80;

    public static readonly (int Va, uint Game, uint Patched, int Spells)[] V12Hooks =
    {
        (0x0010F4AC, 0x1462000B, 0x08059BD9, 0x80),
        (0x0010F648, 0x12000207, 0x08059BBB, 0x80),
        (0x001109C0, 0x14620004, 0x0804634F, 0x80),
        (0x0011B758, 0x0100F809, 0x0C059B8A, 0x200),
        (0x0015E220, 0xC661002C, 0x0C04C191, 0x80),
        (0x0015E424, 0x2C420003, 0x2C420004, 0x80),
        (0x0015E50C, 0x10000013, 0x08040AD7, 0x80),
        (0x00161EF8, 0x2C830017, 0x2C830025, 0x3FF),
        (0x00161F00, 0x3C020030, 0x3C020013, 0x3FF),
        (0x00161F08, 0x24427BC0, 0x24424D80, 0x3FF),
        (0x001676E4, 0x0C05AA52, 0x0C059BE4, 0x20),
        (0x00167AFC, 0x0240282D, 0x0C04BE2B, 0x18),
        (0x0016DD2C, 0x0C04EB96, 0x0C059BCA, 0x80),
        (0x0016DD44, 0x0C04EB96, 0x0C04BDE4, 0x3FF),
        (0x0017A4BC, 0x0C05ED8E, 0x0C043170, 0x3FF),
        (0x0017B594, 0x0C059CE4, 0x0C04C49C, 0x20),
        (0x0017E52C, 0x14830009, 0x0804A4C6, 0x3CE),
        (0x0017EA44, 0x8C440000, 0x0C04C848, 0x3CE),
        (0x0017EAB4, 0x8C440000, 0x0C04C848, 0x3CE),
        (0x0018331C, 0x0C058ADE, 0x0C04A712, 0x3FF),
        (0x001B177C, 0x3C030031, 0x8E230038, 0x1),
        (0x001B1784, 0x24636798, 0x24630080, 0x1),
        (0x001BED7C, 0x0C04C3CC, 0x0C04BE72, 0x100),
        (0x001BED94, 0x0C04C3A6, 0x0C04BE78, 0x100),
        (0x001BEFD0, 0x24040058, 0x24040060, 0x100),
        (0x001BF004, 0x0C06FD68, 0x0C04CCC8, 0x100),
        (0x0030A104, 0x001663F8, 0x00132F54, 0xC),
        (0x0030A11C, 0x0015C2F0, 0x00134FA8, 0x8)
    };

    public static readonly (int Va, int Length)[] V12Code =
    {
        (0x00102910, 0x264), (0x00102D80, 0x280), (0x0010C338, 0x38C), (0x0010CD50, 0x1E0),
        (0x00118978, 0x3F8), (0x00129318, 0x210), (0x00129C48, 0x124), (0x0012A710, 0x190),
        (0x0012BAE8, 0x49C), (0x0012CD30, 0x34C), (0x0012F790, 0x278), (0x00130540, 0x1C4),
        (0x00131270, 0x50), (0x00132120, 0x40), (0x00132D90, 0x63C), (0x00134CE0, 0x98),
        (0x00134F18, 0x120), (0x0013CF80, 0x80), (0x0013D2A0, 0x188), (0x00166E28, 0x190)
    };

    public const string V12CodeSha256 = "2dd021b30272842967b0ef764e70d76e3b8ff0f835323041e198a88df572dbad";

    public static readonly (int Va, int GameVa, (int At, uint Word)[] Words)[] V12VTables =
    {
        (0x00132020, 0x00316718, new (int, uint)[] {  }),
        (0x001320A0, 0x00316798, new (int, uint)[] { (0x24, 0x0012CD30), (0x3C, 0x0012E418) }),
        (0x0013CE00, 0x0031B650, new (int, uint)[] { (0x0C, 0x00134F18), (0x24, 0x0010CD50) }),
        (0x00135040, 0x0031B5D0, new (int, uint)[] { (0x0C, 0x001331C4) }),
        (0x0013D220, 0x0031C3C8, new (int, uint)[] { (0x3C, 0x0013D2A0) }),
        (0x0013CF00, 0x00316798, new (int, uint)[] { (0x24, 0x0012A710), (0x3C, 0x0012E418) }),
        (0x0013CE80, 0x00317470, new (int, uint)[] { (0x0C, 0x00134CE0), (0x24, 0x001330F8) }),
        (0x00131170, 0x0031B530, new (int, uint)[] { (0x0C, 0x0013CF80), (0x24, 0x00102D80) }),
        (0x001311F0, 0x0031B5B0, new (int, uint)[] {  })
    };

    /// <summary>
    /// Version 11 (entangle-test1: all nine, Entangle the newest; its data block and second block where version 17's are, with
    /// version 17's layout, so its settings are read where version 17 keeps them): its hooks (and which of its spells need each, the
    /// same masks as version 13's), its code's pieces with their SHA-256, its vtable copies and its cast table's place.
    /// </summary>
    public const int V11Version = 11, V11CastTableVa = 0x00132EF0;

    public static readonly (int Va, uint Game, uint Patched, int Spells)[] V11Hooks =
    {
        (0x0010F4AC, 0x1462000B, 0x0804C1B6, 0x80),
        (0x0010F648, 0x12000207, 0x0804CCB7, 0x80),
        (0x001109C0, 0x14620004, 0x0804634F, 0x80),
        (0x0015E220, 0xC661002C, 0x0C04C806, 0x80),
        (0x0015E424, 0x2C420003, 0x2C420004, 0x80),
        (0x0015E50C, 0x10000013, 0x0804CCDF, 0x80),
        (0x00161EF8, 0x2C830017, 0x2C830024, 0x1FF),
        (0x00161F00, 0x3C020030, 0x3C020013, 0x1FF),
        (0x00161F08, 0x24427BC0, 0x24422EF0, 0x1FF),
        (0x001676E4, 0x0C05AA52, 0x0C04CCD5, 0x20),
        (0x00167AFC, 0x0240282D, 0x0C04A712, 0x18),
        (0x0016DD2C, 0x0C04EB96, 0x0C04CCC6, 0x80),
        (0x0016DD44, 0x0C04EB96, 0x0C04B3D0, 0x1FF),
        (0x0017A4BC, 0x0C05ED8E, 0x0C04BDE4, 0x1FF),
        (0x0017B594, 0x0C059CE4, 0x0C0433C0, 0x20),
        (0x0017E52C, 0x14830009, 0x08040B60, 0x1CE),
        (0x0017EA44, 0x8C440000, 0x0C04CC97, 0x1CE),
        (0x0017EAB4, 0x8C440000, 0x0C04CC97, 0x1CE),
        (0x0018331C, 0x0C058ADE, 0x0C043170, 0x1FF),
        (0x001B177C, 0x3C030031, 0x8E230038, 0x1),
        (0x001B1784, 0x24636798, 0x24630080, 0x1),
        (0x001BED7C, 0x0C04C3CC, 0x0C04B40B, 0x100),
        (0x001BED94, 0x0C04C3A6, 0x0C04B411, 0x100),
        (0x001BEFD0, 0x24040058, 0x24040060, 0x100),
        (0x001BF004, 0x0C06FD68, 0x0C04CB8F, 0x100),
        (0x0030A104, 0x001663F8, 0x0012F874, 0xC),
        (0x0030A11C, 0x0015C2F0, 0x00132F80, 0x8)
    };

    public static readonly (int Va, int Length)[] V11Code =
    {
        (0x00102910, 0x1E0), (0x00102D80, 0x274), (0x0010C338, 0x38C), (0x0010CD50, 0x200),
        (0x00118978, 0x3F8), (0x00129318, 0x214), (0x00129C48, 0x11C), (0x0012A710, 0x190),
        (0x0012BAE8, 0x4A8), (0x0012CD30, 0x104), (0x0012CF40, 0x12C), (0x0012F790, 0x26C),
        (0x00130540, 0x1C4), (0x00131170, 0xCC), (0x00132018, 0x150), (0x00132D90, 0x158),
        (0x00132F80, 0x90), (0x00133190, 0x204)
    };

    public const string V11CodeSha256 = "218a475ec4c335c6127537b92c129ad3fbf0c2739fb29575cc950d9eb45712b3";

    public static readonly (int Va, int GameVa, (int At, uint Word)[] Words)[] V11VTables =
    {
        (0x0012CE40, 0x00316718, new (int, uint)[] {  }),
        (0x0012CEC0, 0x00316798, new (int, uint)[] { (0x24, 0x0012BC30), (0x3C, 0x0012E418) }),
        (0x00131240, 0x0031B650, new (int, uint)[] { (0x0C, 0x001320D8), (0x24, 0x00102910) }),
        (0x00102AF0, 0x0031B5D0, new (int, uint)[] { (0x0C, 0x0012F94C) }),
        (0x00133110, 0x0031C3C8, new (int, uint)[] { (0x3C, 0x001294BC) }),
        (0x00133090, 0x00316798, new (int, uint)[] { (0x24, 0x0012A710), (0x3C, 0x0012E418) }),
        (0x00133010, 0x00317470, new (int, uint)[] { (0x0C, 0x00102F5C), (0x24, 0x00131170) })
    };

    /// <summary>
    /// Version 10 (inferno-arrow-test1: all eight, Inferno Arrow the newest; its data block at <see cref="DataVa"/>, as version
    /// 17's, with version 10's layout): its hooks (and which of its spells need each, the same masks as version 17's), its code's
    /// pieces with their SHA-256, its vtable copies and its cast table's place, and where its data block keeps the settings.
    /// </summary>
    public const int V10Version = 10, V10EntriesAt = 0x80, V10AbilitiesAt = 0x380,
                     V10BarrierDamageMinAt = 0x510, V10BarrierDamageMaxAt = 0x518, V10FieldFramesAt = 0x530,
                     V10MightDamageAt = 0x540, V10MightDefenceAt = 0x548, V10PowerHitsAt = 0x558,
                     V10OrbsCountAt = 0x568, V10OrbsDamageMinAt = 0x570, V10OrbsDamageMaxAt = 0x578,
                     V10InfernoManaAt = 0x630, V10InfernoDamageMinAt = 0x638, V10InfernoDamageMaxAt = 0x640,
                     V10InfernoBurnAt = 0x648, V10InfernoBlastAt = 0x650, V10InfernoSpreadAt = 0x658,
                     V10InfernoFlameMinAt = 0x660, V10InfernoFlameMaxAt = 0x668, V10CastTableVa = 0x00131170;

    public static readonly (int Va, uint Game, uint Patched, int Spells)[] V10Hooks =
    {
        (0x0010F4AC, 0x1462000B, 0x0804CBA7, 0x80),
        (0x0010F648, 0x12000207, 0x0804C848, 0x80),
        (0x001109C0, 0x14620004, 0x0804634F, 0x80),
        (0x0015E220, 0xC661002C, 0x0C04BE4A, 0x80),
        (0x0015E424, 0x2C420003, 0x2C420004, 0x80),
        (0x0015E50C, 0x10000013, 0x0804AFDC, 0x80),
        (0x00161EF8, 0x2C830017, 0x2C830023, 0xFF),
        (0x00161F00, 0x3C020030, 0x3C020013, 0xFF),
        (0x00161F08, 0x24427BC0, 0x24421170, 0xFF),
        (0x001676E4, 0x0C05AA52, 0x0C04CBB2, 0x20),
        (0x00167AFC, 0x0240282D, 0x0C04A712, 0x18),
        (0x0016DD2C, 0x0C04EB96, 0x0C04CB98, 0x80),
        (0x0016DD44, 0x0C04EB96, 0x0C04B385, 0xFF),
        (0x0017A4BC, 0x0C05ED8E, 0x0C04B34C, 0xFF),
        (0x0017B594, 0x0C059CE4, 0x0C0433BD, 0x20),
        (0x0017E52C, 0x14830009, 0x08040B60, 0xCE),
        (0x0017EA44, 0x8C440000, 0x0C04AA16, 0xCE),
        (0x0017EAB4, 0x8C440000, 0x0C04AA16, 0xCE),
        (0x0018331C, 0x0C058ADE, 0x0C043170, 0xFF),
        (0x001B177C, 0x3C030031, 0x8E230038, 0x1),
        (0x001B1784, 0x24636798, 0x24630080, 0x1),
        (0x0030A104, 0x001663F8, 0x00102F28, 0xC),
        (0x0030A11C, 0x0015C2F0, 0x0012CFD8, 0x8)
    };

    public static readonly (int Va, int Length)[] V10Code =
    {
        (0x00102910, 0x1E0), (0x00102D80, 0x280), (0x0010C338, 0x38C), (0x0010CD50, 0x1F4),
        (0x00118978, 0x3F8), (0x00129318, 0x220), (0x00129C48, 0x11C), (0x0012A710, 0x188),
        (0x0012BAE8, 0x384), (0x0012BF70, 0x18), (0x0012CD30, 0x338), (0x0012F790, 0x258),
        (0x00130540, 0x1C8), (0x00131280, 0x40), (0x00132120, 0x3C), (0x00132D90, 0x160)
    };

    public const string V10CodeSha256 = "e7c3505440ac6622cba7fb16261e81e2dcd140352c3a26acbea46089a0fb1d59";

    public static readonly (int Va, int GameVa, (int At, uint Word)[] Words)[] V10VTables =
    {
        (0x0012BE70, 0x00316718, new (int, uint)[] {  }),
        (0x0012BEF0, 0x00316798, new (int, uint)[] { (0x24, 0x0012BAE8), (0x3C, 0x0012E418) }),
        (0x00131200, 0x0031B650, new (int, uint)[] { (0x0C, 0x001294A8), (0x24, 0x00102910) }),
        (0x00102AF0, 0x0031B5D0, new (int, uint)[] { (0x0C, 0x00130540) }),
        (0x001320A0, 0x0031C3C8, new (int, uint)[] { (0x3C, 0x00132D90) }),
        (0x00132020, 0x00316798, new (int, uint)[] { (0x24, 0x00129318), (0x3C, 0x0012E418) })
    };

    /// <summary>
    /// Version 9 (xantam-orbs-test2: all seven, the orbs in a fan; its data block at <see cref="LegacyDataVa"/>, as every
    /// earlier version's): its hooks (and which of its spells need each, the same masks as version 17's), its code's pieces
    /// with their SHA-256, its vtable copies and its cast table's place, and where its data block keeps the settings.
    /// </summary>
    public const int V9Version = 9, V9EntriesAt = 0x80, V9AbilitiesAt = 0x320,
                     V9BarrierDamageMinAt = 0x4B0, V9BarrierDamageMaxAt = 0x4B8, V9FieldFramesAt = 0x4D0,
                     V9MightDamageAt = 0x4E0, V9MightDefenceAt = 0x4E8, V9PowerHitsAt = 0x4F8,
                     V9OrbsCountAt = 0x508, V9OrbsDamageMinAt = 0x510, V9OrbsDamageMaxAt = 0x518,
                     V9CastTableVa = 0x0012CD30;

    public static readonly (int Va, uint Game, uint Patched, int Spells)[] V9Hooks =
    {
        (0x00161EF8, 0x2C830017, 0x2C830023, 0x7F),
        (0x00161F00, 0x3C020030, 0x3C020013, 0x7F),
        (0x00161F08, 0x24427BC0, 0x2442CD30, 0x7F),
        (0x001676E4, 0x0C05AA52, 0x0C046350, 0x20),
        (0x00167AFC, 0x0240282D, 0x0C04A4C6, 0x18),
        (0x0016DD44, 0x0C04EB96, 0x0C04AEBA, 0x7F),
        (0x0017A4BC, 0x0C05ED8E, 0x0C04A9C4, 0x7F),
        (0x0017B594, 0x0C059CE4, 0x0C04B3FF, 0x20),
        (0x0017E52C, 0x14830009, 0x08043354, 0x4E),
        (0x0017EA44, 0x8C440000, 0x0C04BDE4, 0x4E),
        (0x0017EAB4, 0x8C440000, 0x0C04BDE4, 0x4E),
        (0x0018331C, 0x0C058ADE, 0x0C043170, 0x7F),
        (0x001B177C, 0x3C030031, 0x8E230038, 0x1),
        (0x001B1784, 0x24636798, 0x24630080, 0x1),
        (0x0030A104, 0x001663F8, 0x00102F24, 0xC),
        (0x0030A11C, 0x0015C2F0, 0x0012BEF4, 0x8)
    };

    public static readonly (int Va, int Length)[] V9Code =
    {
        (0x00102910, 0x1E0), (0x00102D80, 0x27C), (0x0010C338, 0x38C), (0x0010CD50, 0x1EC),
        (0x00118978, 0x3F0), (0x00129318, 0x220), (0x0012A710, 0x190), (0x0012BAE8, 0x49C),
        (0x0012CEC0, 0x18C), (0x0012F790, 0x80)
    };

    public const string V9CodeSha256 = "d0c34871bb902c3d95b264be6712baa257410c99bb1f170f5b39995501f81d2f";

    public static readonly (int Va, int GameVa, (int At, uint Word)[] Words)[] V9VTables =
    {
        (0x00129C50, 0x00316718, new (int, uint)[] {  }),
        (0x00129CD0, 0x00316798, new (int, uint)[] { (0x24, 0x00118AC0), (0x3C, 0x0012E418) }),
        (0x0012CDC0, 0x0031B650, new (int, uint)[] { (0x0C, 0x0010CEAC), (0x24, 0x00102910) }),
        (0x00102AF0, 0x0031B5D0, new (int, uint)[] { (0x0C, 0x0012BE44) }),
        (0x0012CE40, 0x0031C3C8, new (int, uint)[] { (0x3C, 0x0012CEC0) })
    };

    /// <summary>
    /// Version 5 (kromlech-spells-test1: Xantam's Fire and Kromlech's three, without Power Attack): its hooks (and which of its
    /// spells need each, the same masks as version 17's), its code's pieces with their SHA-256 and its vtable copies, and where
    /// its data block keeps the settings (its mask, Xantam's Fire's damage and the entries' prices where version 16 has them).
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

    /// <summary>
    /// Version 6 (power-attack-test1: the five spells, without Sorcerer's Blood): its hooks (and which of its spells need each,
    /// the same masks as version 17's), its code's pieces with their SHA-256 and its vtable copies, and where its data block keeps
    /// the settings (its mask, its entries and Xantam's Fire's damage where version 16 has them).
    /// </summary>
    public const int V6Version = 6, V6EntriesAt = 0x80, V6AbilitiesAt = 0x260,
                     V6BarrierDamageMinAt = 0x3A0, V6BarrierDamageMaxAt = 0x3A8, V6FieldFramesAt = 0x3C0,
                     V6MightDamageAt = 0x3D0, V6MightDefenceAt = 0x3D8, V6PowerHitsAt = 0x3E8;

    public static readonly (int Va, uint Game, uint Patched, int Spells)[] V6Hooks =
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

    public static readonly (int Va, int Length)[] V6Code =
    {
        (0x00102910, 0x1E0), (0x00102D80, 0x280), (0x0010C338, 0x390), (0x0010CD50, 0x1EC),
        (0x00118978, 0x3F4), (0x00129420, 0xE4), (0x00129C48, 0x120), (0x0012A710, 0x168),
        (0x0012BAE8, 0x228), (0x0012BD90, 0x10C)
    };

    public const string V6CodeSha256 = "1d9676d681991d4d1a0c436ee8a2d820ad0031d8f9ad5f9f66f37f7a014b92c8";

    public static readonly (int Va, int GameVa, (int At, uint Word)[] Words)[] V6VTables =
    {
        (0x00129320, 0x00316718, new (int, uint)[] {  }),
        (0x001293A0, 0x00316798, new (int, uint)[] { (0x24, 0x0010C480), (0x3C, 0x0012E418) }),
        (0x0012BD10, 0x0031B650, new (int, uint)[] { (0x0C, 0x0012A7E8), (0x24, 0x00102910) }),
        (0x00102AF0, 0x0031B5D0, new (int, uint)[] { (0x0C, 0x0010CE8C) })
    };

    /// <summary>
    /// Version 7 (sorcerers-blood-test1: the six, without Xantam's Orbs): its hooks (and which of its spells need each, the same
    /// masks as version 10's), its code's pieces with their SHA-256 and its vtable copies, and where its data block keeps the
    /// settings (its mask, its entries and Xantam's Fire's damage where version 16 has them).
    /// </summary>
    public const int V7Version = 7, V7EntriesAt = 0x80, V7AbilitiesAt = 0x2C0,
                     V7BarrierDamageMinAt = 0x400, V7BarrierDamageMaxAt = 0x408, V7FieldFramesAt = 0x420,
                     V7MightDamageAt = 0x430, V7MightDefenceAt = 0x438, V7PowerHitsAt = 0x448;

    public static readonly (int Va, uint Game, uint Patched, int Spells)[] V7Hooks =
    {
        (0x00161EF8, 0x2C830017, 0x2C830022, 0x3F),
        (0x00161F00, 0x3C020030, 0x3C020013, 0x3F),
        (0x00161F08, 0x24427BC0, 0x24423290, 0x3F),
        (0x001676E4, 0x0C05AA52, 0x0C04A541, 0x20),
        (0x00167AFC, 0x0240282D, 0x0C0462A8, 0x18),
        (0x0016DD44, 0x0C04EB96, 0x0C040BC9, 0x3F),
        (0x0017A4BC, 0x0C05ED8E, 0x0C04A508, 0x3F),
        (0x0017B594, 0x0C059CE4, 0x0C04A74A, 0x20),
        (0x0017E52C, 0x14830009, 0x0804625E, 0xE),
        (0x0017EA44, 0x8C440000, 0x0C04AF97, 0xE),
        (0x0017EAB4, 0x8C440000, 0x0C04AF97, 0xE),
        (0x0018331C, 0x0C058ADE, 0x0C043171, 0x3F),
        (0x001B177C, 0x3C030031, 0x8E230038, 0x1),
        (0x001B1784, 0x24636798, 0x24630080, 0x1),
        (0x0030A104, 0x001663F8, 0x0012A710, 0xC),
        (0x0030A11C, 0x0015C2F0, 0x0012BC80, 0x8)
    };

    public static readonly (int Va, int Length)[] V7Code =
    {
        (0x00102910, 0x1E0), (0x00102D80, 0x280), (0x0010C338, 0x390), (0x0010CD50, 0x1EC),
        (0x00118978, 0x3F4), (0x00129420, 0x10C), (0x00129C48, 0x130), (0x0012A710, 0x168),
        (0x0012BAE8, 0x228), (0x0012BD90, 0x14C)
    };

    public const string V7CodeSha256 = "f0d54fa0b962627f51b5ea58d6e94031009b56dd3670f974035d1a0b9fbb4c5e";

    public static readonly (int Va, int GameVa, (int At, uint Word)[] Words)[] V7VTables =
    {
        (0x00129320, 0x00316718, new (int, uint)[] {  }),
        (0x001293A0, 0x00316798, new (int, uint)[] { (0x24, 0x0010C480), (0x3C, 0x0012E418) }),
        (0x0012BD10, 0x0031B650, new (int, uint)[] { (0x0C, 0x0012A7E8), (0x24, 0x00102910) }),
        (0x00102AF0, 0x0031B5D0, new (int, uint)[] { (0x0C, 0x0010CE8C) })
    };

    /// <summary>
    /// Version 8 (xantam-orbs-test1: all seven, the orbs aimed with a random scatter): its hooks (and which of its spells need
    /// each, the same masks as version 17's), its code's pieces with their SHA-256 and its vtable copies, and where its data block
    /// keeps the settings (its mask, its entries and Xantam's Fire's damage where version 16 has them).
    /// </summary>
    public const int V8Version = 8, V8EntriesAt = 0x80, V8AbilitiesAt = 0x320,
                     V8BarrierDamageMinAt = 0x4B0, V8BarrierDamageMaxAt = 0x4B8, V8FieldFramesAt = 0x4D0,
                     V8MightDamageAt = 0x4E0, V8MightDefenceAt = 0x4E8, V8PowerHitsAt = 0x4F8,
                     V8OrbsCountAt = 0x508, V8OrbsDamageMinAt = 0x510, V8OrbsDamageMaxAt = 0x518;

    public static readonly (int Va, uint Game, uint Patched, int Spells)[] V8Hooks =
    {
        (0x00161EF8, 0x2C830017, 0x2C830023, 0x7F),
        (0x00161F00, 0x3C020030, 0x3C020013, 0x7F),
        (0x00161F08, 0x24427BC0, 0x2442CD30, 0x7F),
        (0x001676E4, 0x0C05AA52, 0x0C04A754, 0x20),
        (0x00167AFC, 0x0240282D, 0x0C0462FE, 0x18),
        (0x0016DD44, 0x0C04EB96, 0x0C04AEBA, 0x7F),
        (0x0017A4BC, 0x0C05ED8E, 0x0C04A9C4, 0x7F),
        (0x0017B594, 0x0C059CE4, 0x0C046345, 0x20),
        (0x0017E52C, 0x14830009, 0x08043137, 0x4E),
        (0x0017EA44, 0x8C440000, 0x0C04B3FD, 0x4E),
        (0x0017EAB4, 0x8C440000, 0x0C04B3FD, 0x4E),
        (0x0018331C, 0x0C058ADE, 0x0C04A4C6, 0x7F),
        (0x001B177C, 0x3C030031, 0x8E230038, 0x1),
        (0x001B1784, 0x24636798, 0x24630080, 0x1),
        (0x0030A104, 0x001663F8, 0x0012BCAC, 0xC),
        (0x0030A11C, 0x0015C2F0, 0x0010C638, 0x8)
    };

    public static readonly (int Va, int Length)[] V8Code =
    {
        (0x00102910, 0x268), (0x00102D80, 0x270), (0x0010C338, 0x390), (0x0010CD50, 0x1F8),
        (0x00118978, 0x3EC), (0x00129318, 0x208), (0x00129D50, 0x28), (0x0012A710, 0x190),
        (0x0012BAE8, 0x4A0), (0x0012CF40, 0x134)
    };

    public const string V8CodeSha256 = "624a0bd81a2b997713c550fc11ddb6ce95113364824924c61bb081978ff79eb3";

    public static readonly (int Va, int GameVa, (int At, uint Word)[] Words)[] V8VTables =
    {
        (0x00129C50, 0x00316718, new (int, uint)[] {  }),
        (0x00129CD0, 0x00316798, new (int, uint)[] { (0x24, 0x00118978), (0x3C, 0x0012E418) }),
        (0x0012CE40, 0x0031B650, new (int, uint)[] { (0x0C, 0x00102F60), (0x24, 0x00102D80) }),
        (0x0012CDC0, 0x0031B5D0, new (int, uint)[] { (0x0C, 0x0010CE98) }),
        (0x0012CEC0, 0x0031C3C8, new (int, uint)[] { (0x3C, 0x0012CF40) })
    };

    /// <summary>Version 4 (Xantam's Fire alone, test4): its data block's settings, its code and two vtable copies, its six hooks.</summary>
    public const int V4CodeVa = 0x00221B88, V4CodeEndVa = 0x002222F0, V4PriceAt = 0xCA, V4ManaAt = 0x3AC,
                     V4DamageMinAt = 0x48, V4DamageMaxAt = 0x50;

    public const string V4CodeBase64 =
        "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";

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

    /// <summary>The spells' names, descriptions and HUD lines (0 English, 1 French, 2 German, 3 Italian, 4 Spanish), as gtext_feats17.py writes them (none for Power Attack, Sorcerer's Blood and Inferno Arrow: the game's own).</summary>
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
        ("righteousmight", 4, "righteousmight_hud", "+%d%% de daño, -%d%% de daño recibido"),
        ("xantamorbs", 0, "xantamorbs_name", "Xantam's Orbs"),
        ("xantamorbs", 0, "xantamorbs_desc", "Like the beholder Xantam, the caster hurls glowing orbs at the nearest foe ahead. Each orb bursts on the first foe it strikes. One more orb with each level."),
        ("xantamorbs", 1, "xantamorbs_name", "Orbes de Xantam"),
        ("xantamorbs", 1, "xantamorbs_desc", "Comme le tyrannoeil Xantam, le lanceur projette des orbes ardents sur l'adversaire le plus proche. Chaque orbe éclate sur le premier adversaire touché. Un orbe de plus à chaque niveau."),
        ("xantamorbs", 2, "xantamorbs_name", "Xantams Kugeln"),
        ("xantamorbs", 2, "xantamorbs_desc", "Wie der Betrachter Xantam schleudert der Zaubernde glühende Kugeln auf den nächsten Gegner vor ihm. Jede Kugel zerplatzt am ersten Gegner, den sie trifft. Eine Kugel mehr mit jeder Stufe."),
        ("xantamorbs", 3, "xantamorbs_name", "Sfere di Xantam"),
        ("xantamorbs", 3, "xantamorbs_desc", "Come il beholder Xantam, l'incantatore scaglia sfere ardenti contro il nemico più vicino. Ogni sfera esplode sul primo nemico che colpisce. Una sfera in più a ogni livello."),
        ("xantamorbs", 4, "xantamorbs_name", "Orbes de Xantam"),
        ("xantamorbs", 4, "xantamorbs_desc", "Como el contemplador Xantam, el lanzador arroja orbes ardientes al enemigo más cercano. Cada orbe estalla en el primer enemigo que alcanza. Un orbe más en cada nivel."),
        ("entangle", 0, "entangle_name", "Entangle"),
        ("entangle", 0, "entangle_desc", "Thorny vines burst from the ground and crawl to the nearest foes. Each foe a vine wraps moves slower, and the thorns tear at it every second. One more vine with each level."),
        ("entangle", 1, "entangle_name", "Enchevêtrement"),
        ("entangle", 1, "entangle_desc", "Des lianes épineuses jaillissent du sol et rampent vers les adversaires les plus proches. Chaque adversaire enlacé se déplace plus lentement et les épines le déchirent chaque seconde. Une liane de plus à chaque niveau."),
        ("entangle", 2, "entangle_name", "Verstricken"),
        ("entangle", 2, "entangle_desc", "Dornige Ranken brechen aus dem Boden und kriechen zu den nächsten Gegnern. Jeder umschlungene Gegner bewegt sich langsamer, und die Dornen verletzen ihn jede Sekunde. Eine Ranke mehr mit jeder Stufe."),
        ("entangle", 3, "entangle_name", "Intralciare"),
        ("entangle", 3, "entangle_desc", "Liane spinose spuntano dal terreno e strisciano verso i nemici più vicini. Ogni nemico avvolto si muove più lentamente e le spine lo feriscono ogni secondo. Una liana in più a ogni livello."),
        ("entangle", 4, "entangle_name", "Enmarañar"),
        ("entangle", 4, "entangle_desc", "Enredaderas espinosas brotan del suelo y reptan hacia los enemigos más cercanos. Cada enemigo atrapado se mueve más despacio y las espinas lo hieren cada segundo. Una enredadera más en cada nivel."),
        ("insectplague", 0, "insectplague_name", "Insect Plague"),
        ("insectplague", 0, "insectplague_desc", "A swarm of insects gathers before the caster and flies to the nearest foe, stinging and poisoning it every second. When the foe dies, the swarm moves on to the next. One more swarm with each level."),
        ("insectplague", 1, "insectplague_name", "Fléau d'insectes"),
        ("insectplague", 1, "insectplague_desc", "Un essaim d'insectes se forme devant le lanceur et vole vers l'adversaire le plus proche, qu'il pique et empoisonne chaque seconde. Quand l'adversaire meurt, l'essaim passe au suivant. Un essaim de plus à chaque niveau."),
        ("insectplague", 2, "insectplague_name", "Insektenplage"),
        ("insectplague", 2, "insectplague_desc", "Ein Insektenschwarm sammelt sich vor dem Zaubernden und fliegt zum nächsten Gegner, den er jede Sekunde sticht und vergiftet. Stirbt der Gegner, zieht der Schwarm zum nächsten weiter. Ein Schwarm mehr mit jeder Stufe."),
        ("insectplague", 3, "insectplague_name", "Piaga di Insetti"),
        ("insectplague", 3, "insectplague_desc", "Uno sciame di insetti si raduna davanti all'incantatore e vola verso il nemico più vicino, pungendolo e avvelenandolo ogni secondo. Quando il nemico muore, lo sciame passa al successivo. Uno sciame in più a ogni livello."),
        ("insectplague", 4, "insectplague_name", "Plaga de insectos"),
        ("insectplague", 4, "insectplague_desc", "Un enjambre de insectos se reúne ante el lanzador y vuela hacia el enemigo más cercano, al que pica y envenena cada segundo. Cuando el enemigo muere, el enjambre pasa al siguiente. Un enjambre más en cada nivel."),
        ("gasarrow", 0, "poisongasarrow_name", "Poison Gas Arrow"),
        ("gasarrow", 0, "poisongasarrow_desc", "An arrow that bursts into a cloud of poison gas where it strikes. Every foe in the cloud is hurt and poisoned each second. The arrow, the cloud and its poison grow stronger with each level."),
        ("gasarrow", 1, "poisongasarrow_name", "Flèche de gaz toxique"),
        ("gasarrow", 1, "poisongasarrow_desc", "Une flèche qui libère un nuage de gaz toxique là où elle frappe. Chaque adversaire dans le nuage est blessé et empoisonné chaque seconde. La flèche, le nuage et son poison gagnent en puissance à chaque niveau."),
        ("gasarrow", 2, "poisongasarrow_name", "Giftgaspfeil"),
        ("gasarrow", 2, "poisongasarrow_desc", "Ein Pfeil, der beim Aufprall eine Giftgaswolke freisetzt. Jeder Gegner in der Wolke wird jede Sekunde verletzt und vergiftet. Pfeil, Wolke und Gift werden mit jeder Stufe stärker."),
        ("gasarrow", 3, "poisongasarrow_name", "Freccia di Gas Velenoso"),
        ("gasarrow", 3, "poisongasarrow_desc", "Una freccia che libera una nube di gas velenoso dove colpisce. Ogni nemico nella nube viene ferito e avvelenato ogni secondo. La freccia, la nube e il veleno si rafforzano a ogni livello."),
        ("gasarrow", 4, "poisongasarrow_name", "Flecha de gas venenoso"),
        ("gasarrow", 4, "poisongasarrow_desc", "Una flecha que libera una nube de gas venenoso donde impacta. Cada enemigo dentro de la nube recibe daño y queda envenenado cada segundo. La flecha, la nube y su veneno se refuerzan en cada nivel."),
        ("dimensiondoor", 0, "dimensiondoor_name", "Dimension Door"),
        ("dimensiondoor", 0, "dimensiondoor_desc", "The caster steps through a magical door and reappears a short way ahead. Walls, closed doors, ledges and the edge of the screen cut the step short. Each level reaches further."),
        ("dimensiondoor", 0, "dimensiondoor_hud", "Distance: %d units"),
        ("dimensiondoor", 1, "dimensiondoor_name", "Porte dimensionnelle"),
        ("dimensiondoor", 1, "dimensiondoor_desc", "Le lanceur franchit une porte magique et réapparaît un peu plus loin devant lui. Les murs, les portes fermées, les corniches et le bord de l'écran écourtent le saut. Chaque niveau porte plus loin."),
        ("dimensiondoor", 1, "dimensiondoor_hud", "Distance : %d unités"),
        ("dimensiondoor", 2, "dimensiondoor_name", "Dimensionstür"),
        ("dimensiondoor", 2, "dimensiondoor_desc", "Der Zaubernde tritt durch eine magische Tür und erscheint ein Stück weiter vorn wieder. Wände, geschlossene Türen, Abgründe und der Bildschirmrand verkürzen den Sprung. Jede Stufe reicht weiter."),
        ("dimensiondoor", 2, "dimensiondoor_hud", "Entfernung: %d Einheiten"),
        ("dimensiondoor", 3, "dimensiondoor_name", "Porta Dimensionale"),
        ("dimensiondoor", 3, "dimensiondoor_desc", "L'incantatore attraversa una porta magica e riappare poco più avanti. Muri, porte chiuse, sporgenze e il bordo dello schermo accorciano il salto. Ogni livello porta più lontano."),
        ("dimensiondoor", 3, "dimensiondoor_hud", "Distanza: %d unità"),
        ("dimensiondoor", 4, "dimensiondoor_name", "Puerta dimensional"),
        ("dimensiondoor", 4, "dimensiondoor_desc", "El lanzador cruza una puerta mágica y reaparece un poco más adelante. Las paredes, las puertas cerradas, las cornisas y el borde de la pantalla acortan el salto. Cada nivel llega más lejos."),
        ("dimensiondoor", 4, "dimensiondoor_hud", "Distancia: %d unidades"),
        ("expertfocus", 0, "expertfocus_name", "Expert Focus"),
        ("expertfocus", 0, "expertfocus_desc", "The archer focuses on every shot: for a while each of his arrows does more damage. The focus lasts longer with each level."),
        ("expertfocus", 0, "expertfocus_hud", "Arrows +%d%% damage for %d.%d seconds"),
        ("expertfocus", 1, "expertfocus_name", "Concentration experte"),
        ("expertfocus", 1, "expertfocus_desc", "L'archer se concentre sur chaque tir : pendant un moment, chacune de ses flèches inflige plus de dégâts. La concentration dure plus longtemps à chaque niveau."),
        ("expertfocus", 1, "expertfocus_hud", "Flèches +%d%% de dégâts pendant %d,%d secondes"),
        ("expertfocus", 2, "expertfocus_name", "Expertenfokus"),
        ("expertfocus", 2, "expertfocus_desc", "Der Bogenschütze konzentriert sich auf jeden Schuss: Eine Zeit lang richtet jeder seiner Pfeile mehr Schaden an. Die Konzentration hält mit jeder Stufe länger an."),
        ("expertfocus", 2, "expertfocus_hud", "Pfeile +%d%% Schaden für %d,%d Sekunden"),
        ("expertfocus", 3, "expertfocus_name", "Concentrazione Esperta"),
        ("expertfocus", 3, "expertfocus_desc", "L'arciere si concentra su ogni tiro: per un po' ogni sua freccia infligge più danni. La concentrazione dura più a lungo a ogni livello."),
        ("expertfocus", 3, "expertfocus_hud", "Frecce +%d%% danni per %d,%d secondi"),
        ("expertfocus", 4, "expertfocus_name", "Concentración experta"),
        ("expertfocus", 4, "expertfocus_desc", "El arquero se concentra en cada disparo: durante un tiempo, cada una de sus flechas causa más daño. La concentración dura más en cada nivel."),
        ("expertfocus", 4, "expertfocus_hud", "Flechas +%d%% de daño durante %d,%d segundos"),
        ("phaseaxe", 0, "phaseaxe_name", "Phase Axe"),
        ("phaseaxe", 0, "phaseaxe_desc", "A magic axe is hurled at the nearest foe. It passes through every foe in its path, striking each one once, until a wall stops it. More damage with each level."),
        ("phaseaxe", 1, "phaseaxe_name", "Hache de phase"),
        ("phaseaxe", 1, "phaseaxe_desc", "Une hache magique est lancée sur l'adversaire le plus proche. Elle traverse chaque adversaire sur son passage et frappe chacun une fois, jusqu'à ce qu'un mur l'arrête. Plus de dégâts à chaque niveau."),
        ("phaseaxe", 2, "phaseaxe_name", "Phasenaxt"),
        ("phaseaxe", 2, "phaseaxe_desc", "Eine magische Axt wird auf den nächsten Gegner geschleudert. Sie fliegt durch jeden Gegner auf ihrem Weg und trifft jeden einmal, bis eine Wand sie aufhält. Mehr Schaden mit jeder Stufe."),
        ("phaseaxe", 3, "phaseaxe_name", "Ascia di Fase"),
        ("phaseaxe", 3, "phaseaxe_desc", "Un'ascia magica viene scagliata contro il nemico più vicino. Attraversa ogni nemico sul suo cammino e colpisce ciascuno una volta, finché un muro non la ferma. Più danni a ogni livello."),
        ("phaseaxe", 4, "phaseaxe_name", "Hacha de fase"),
        ("phaseaxe", 4, "phaseaxe_desc", "Se lanza un hacha mágica contra el enemigo más cercano. Atraviesa a cada enemigo en su camino y golpea a cada uno una vez, hasta que una pared la detiene. Más daño en cada nivel."),
        ("shatter", 0, "shatter_name", "Shatter"),
        ("shatter", 0, "shatter_desc", "A ring of frost bursts out all around the caster. Every foe it reaches takes a little cold damage and is frozen solid for a moment. Barrels, crates and pots shatter. Each level reaches further and freezes longer."),
        ("shatter", 0, "shatter_hud", "Reach %d, frozen for %d.%d seconds"),
        ("shatter", 1, "shatter_name", "Fracassement"),
        ("shatter", 1, "shatter_desc", "Un anneau de givre jaillit tout autour du lanceur. Chaque adversaire qu'il atteint subit de légers dégâts de froid et reste figé dans la glace un instant. Les tonneaux, les caisses et les pots volent en éclats. Plus de portée et un gel plus long à chaque niveau."),
        ("shatter", 1, "shatter_hud", "Portée %d, gel de %d,%d secondes"),
        ("shatter", 2, "shatter_name", "Zerschmettern"),
        ("shatter", 2, "shatter_desc", "Ein Ring aus Frost bricht rings um den Zaubernden hervor. Jeder Gegner, den er erreicht, erleidet etwas Kälteschaden und erstarrt einen Moment lang zu Eis. Fässer, Kisten und Töpfe zerspringen. Mit jeder Stufe reicht er weiter und friert länger ein."),
        ("shatter", 2, "shatter_hud", "Reichweite %d, %d,%d Sekunden eingefroren"),
        ("shatter", 3, "shatter_name", "Frantumare"),
        ("shatter", 3, "shatter_desc", "Un anello di gelo esplode tutto intorno all'incantatore. Ogni nemico che raggiunge subisce lievi danni da freddo e resta congelato per un momento. Barili, casse e vasi vanno in pezzi. A ogni livello arriva più lontano e congela più a lungo."),
        ("shatter", 3, "shatter_hud", "Portata %d, congelati per %d,%d secondi"),
        ("shatter", 4, "shatter_name", "Estallar"),
        ("shatter", 4, "shatter_desc", "Un anillo de escarcha estalla alrededor del lanzador. Cada enemigo al que alcanza sufre un poco de daño por frío y queda congelado un momento. Los barriles, las cajas y las vasijas se hacen añicos. Cada nivel llega más lejos y congela durante más tiempo."),
        ("shatter", 4, "shatter_hud", "Alcance %d, congelados %d,%d segundos")
    };
}
