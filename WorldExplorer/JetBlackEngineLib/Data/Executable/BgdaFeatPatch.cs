// ═══════════════════════════════════════════════════════════════════════════════
// GENERATED FILE: WorldExplorer/JetBlackEngineLib/Data/Executable/BgdaFeatPatch.cs
// by make_worldexplorer_feats12.py from featpatch.py version 12 (and featpool.py, featpatch_v11.py, featpatch_v10.py,
// featpatch_v9.py, featpatch_v8.py, featpatch_v7.py, featpatch_v6.py, featpatch_v5.py, featpatch_v4.py, featpatch_v3.py,
// gtext_feats12.py and the icons in ICONS.LMP). Don't edit by hand: run the script again.
//
// The feats patch as data: six spells borrowed from the game's monsters (Xantam's Fire and Xantam's Orbs for Adrianna;
// Blade Barrier, Antimagic Field and Righteous Might for Kromlech; the kobold shaman's Insect Plague for Vahn), the game's own
// unused vines (Entangle for Vahn) and three feats the game has but never offers (Inferno Arrow for Vahn, Sorcerer's Blood for
// Adrianna, Power Attack for Kromlech), each on or off by itself, in game code nothing runs (the pool), so they need no
// three-player build and WorldExplorer can turn three players on or off underneath them.
// ═══════════════════════════════════════════════════════════════════════════════
namespace JetBlackEngineLib.Data.Executable;

/// <summary>One spell of the patch: its bit in <see cref="FeatPatch.SpellsAt"/>'s mask, hero class, feat bit, cast type,
/// cast code (0: none: Sorcerer's Blood, a passive, and Inferno Arrow, whose type is the game's arrow case), ability name,
/// icon, and its level-up list entry ({class, 0, entry}) and ability record with the default settings (Power Attack,
/// Sorcerer's Blood and Inferno Arrow: none, the game's own record is the one they use; Power Attack and Sorcerer's Blood
/// bring no icon: Power Attack's is the game's).</summary>
internal sealed record SpellPatchData(string Key, int Mask, int HeroClass, int Bit, int CastType, uint Cast, string Name,
                                      string IconName, string EntryBase64, string AbilityBase64, string IconBase64)
{
    /// <summary>The spell has its own ability record in the data block (every one but Power Attack, Sorcerer's Blood and Inferno Arrow, whose records are the game's).</summary>
    public bool OwnRecord => AbilityBase64.Length > 0;

    /// <summary>The spell has a cast case in the patch's cast table (every one but Sorcerer's Blood, a passive, and Inferno Arrow, an arrow).</summary>
    public bool HasCast => Cast != 0;
}

/// <summary>featpatch.py version 12 as data (see BgdaExecutable.BorrowedSpells.cs).</summary>
internal static class FeatPatch
{
    public const int Version = 12;

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
        + "b9lsO4k=";

    public const string PoolOriginalSha256 = "61d9c74b8a8f6a052479a3505c8458611f8b9ff67bfbd76018a2e7b8a48b36ce";

    /// <summary>How much of the pool the patch takes: its three data blocks, its code, its eight vtable copies and its cast table.</summary>
    public const int PoolUsedBytes = 14816;

    /// <summary>The code (11,084 bytes in 21 pieces over the pool's stretches): the same whichever spells are on.</summary>
    public static readonly (int Va, string Base64)[] Code =
    {
        (0x00102910, "QP+9JyEAFjyQuNYmkAKEjgEABTy39AQMPCgFACG4QAAhQMIC4AASkfAACZH4AAqRfAqrjyYAaaUoAGqlMAAEPPA0hCSKmwUMISiAAgABwMYQAIHG"
            + "gQgARgAAoueACABGEACi5xQAgcaBCABGBACi54AIAEYUAKLnGACBxoEIAEYIAKLngAgARhgAouchIKADEAClJyAApifiIgQMIAAHJGcAQBohIIAC"
            + "BsgEDCAApSdjAEAQAAAAAAAAUYwAAFSsFAAsxhQAgMYBYwBGEAAtxhAAgMZBawBGsACt57QArOeSwwQMAAAAACGYQAAEuwQMsAIEJFIAQBAhgEAA"
            + "sAChx7QAoscCCAFGwhACRgAAA0aAPwE8ABiBRDQAA0YJAAFFAAAAAAQAAEbAQQE8ABiBRMMYAEZCCANGghADRgMAABAAAAAAAAiARAAQgEQQAIDG"
            + "AAABRqAAoOcUAIDGAAACRqQAoOcYAIiOqACoryEgAAKgAKUnITBgAiE4AAAQJwgkvDQEDCFIAAAEAcDGUAEA5lCcgMdUAQDmHAAgxiBBATwACIFE"
            + "AAABRhwAAOYUAAg8gM8IJTgACK4MAAiOBQAINQwACK6QAgCuyKSJjyFQ1wLoAEqRgFgKACFQagEhUCoBmAIKrggBy46c/yklIUgrAZQCCa7//wgk"
            + "nAIIrpACiI6gAgiioQIXoiFQ1wLwAEiRogIIovgASJGjAgiipAIArpACBCYhKCACIGwFDCEwIAKyJQQMISAAApn/ABD//1ImwAC9Jz2KBQgCAAIk"
            + "CQABJAIAYRQEAAEkLwOBold5BQgAAAAA"),
        (0x00102D80, "MP+9J6AAv/+oALD/sACx/7gAsv8hgIAAIQASPJC4UibIpIiPmAIJjipQKAGIAEAVAAAAAJACCI4JAAARAAAAAAwAEY0GACASAAAAAFoBKYYDACAZ"
            + "AAAAACsAABAAAAAAAAFAxhAAAcaBCABGAACi54AIAEYQAKLnFAABxoEIAEYEAKLngAgARhQAoucYAAHGgQgARggAoueACABGGACi5yEgoAMQAKUn"
            + "IACmJ+IiBAwgAAckISAAAgbIBAwgAKUnYwBAEAAAAAAAAFGMkAIIjgMAABEAAAAAZmwFDJACBCaQAgCukAIEJiEoIAIgbAUMITAgAhwAIMYgQQE8"
            + "AAiBRAAAAUYcAADmyKSIj5QCCY4jQAkBZAAIKQkAABUAAAAAISAAAiEoIAL6QwE8AGCBRIA/ATwAaIFEWnAEDABwgETIpIiPpAIJjipQCQE6AEAV"
            + "AAAAABAAIMYQAAHGAQABRgIAAEYUACLGFAABxoEQAUaCEAJGAAACRhgAIsYYAAHGgRABRoIQAkYAAAJGHAABxkIIAUY0AAFGJwAARQAAAAAMAUmO"
            + "IUgJAaQCCa6gAgqS4DgLJBhQSwEqAAs86LxrJSFQSwEAAEWNISAgAqICBpKjAgeSAQAIPKCGCDUhSAAAIVAAAAIACzwOfgQMRABrNTwAKI4IAAk8"
            + "JFAJAQ4AQBUEAAk8JFAJARABQMYCAEARgD8BPAAAgUSMAiHGNAgARgIAAEUAAAAAjAIg5iEgIALgzAQMoAIFkqAAv9+oALDfsACx37gAst8IAOAD"
            + "0AC9JyEgAAKgAL/fqACw37AAsd+4ALLf0AC9JzgAiYwIACqFDAArjSEgigAIAGABAwAFJA=="),
        (0x0010C338, "wP+9JyIAFjyIG9YmkAKEjgBABTS39AQMPCgFACFAwgIYBhKRIAYJkSgGCpEgAKmvJACqr/wJq48mAGmlKABqpTAABDz4M4QkipsFDCEogAJQwQQM"
            + "AAAAAAsAQBAAAAAAEABAxBQAQcQYAELEIABDxAA/ATwAIIFEwhgERoAQA0YIAAAQAAAAAIAAoMcAAKPGAAADRoQAoccEAKPGQAgDRogAoseAAKPH"
            + "AQADRoQAo8dBCANGiACjx4EQA0YQAKDnFACh5xgAoufCAABGAgkBRsAYBEYCEQJGABgERoA/ATwAGIFENAADRgkAAEUAAAAAAACgxgQAocYAEIBE"
            + "EACg5xQAoecYAKLnYUcBPAAAgUQEAABGMAbUxsOgAEYQAKDHAgADRhAAoOcUAKHHQggDRhQAoecYAKLHghADRhgAouc8BsPGgx0URjQG1MY4BtXG"
            + "//9QJgoAABoAAAAAwgAURgIJFUbAGARGAgkURkIBFUZBIAVGBhgARvb/ABD//xAmKACg5ywAoechgAAAKkASAjoAABEAAAAAKACgxywAoccwAKDn"
            + "NACh5xgAosc4AKLnEACix4EAAkaCEBZGgACjx4AQA0YAAKLnFACix4EIAkaCEBZGhACjx4AQA0YEAKLniACixwgAoucEuwQMYAAEJA8AQBA8AKKv"
            + "ISBAACEooAMhMIACyKAHDDAApyc8AKKPFAAIPCDSCCU4AEisIACpj1QASawkAKmPWABJrFwAQKwoAKDHLAChx8IAFEYCCRVGwRgERgIBFUZCCRRG"
            + "QCAFRgYYAEbCABRGAgkVRsEYBEYCARVGQgkURkAgBUYoAKPnLACh58X/ABABABAmQAC9Jz2KBQgCAAIkIgAIPIgbCCUQAAmN4AMKJQYAIBEAAAAA"
            + "SABL3Q0AZBH//ykl+v8AEFAASiUhAAs8kLhrJQUACxEAAAAAIUBgAQwACY3y/wAQcAAKJY7tBQgAAAAA4DgMJBhgrAAqAA08gImtJSForAEwAa7d"
            + "JHjEASIA4BW4M7glDwAZJCAAAof//wMkBQBDEP//OSf7/yAXUAAYJwgA4AMAAAAAIRAAACEYQgEAAGOMIQgCAwAAI6wEAEIkKABDKPn/YBQAAAAA"
            + "KABDhSgAA6csAAIkIRhCAQAAY4whCAIDAAAjrAQAQiREAEMo+f9gFAAAAABIAEPdSAAD/yVwxAEwAa79CADgAwAAAAA="),
        (0x0010CD50, "PACIjAcAABUAAAAAOACJjAgAKoUMACuNISCKAAgAYAEDAAUkMP+9J6AAsP+oALH/sACy/7gAs//AALT/yAC//yGAgAAMABGNAQAYJFoAIBIhIAAC"
            + "EAApjhAACa4UACmOFAAJrhgAKY4YAAmuIgASPIgbUiYcACDG3AVBxgAAAUYcAADmOrUEDCEgAAIhQAAAIVAAAkgASZX9/wslAwBrLQIAYBHoAykl"
            + "MPgpJUgASaUBAAglCQALKfb/YBUMAEolIgASPIgbUibIpIiP1AVJjjUAIBEAAAAAGwAJARBQAAAxAEAVAAAAABwAAMbYBUHGAAABRhAAAcaBCABG"
            + "AACi54AIAEYQAKLnFAABxoEIAEYEAKLngAgARhQAoucYAAHGgQgARggAoueACABGGACi5yEgoAMQAKUnIACmJ+IiBAwgAAcktAASkrUAE5IgALQn"
            + "AACEjhQAgBAEAJQm/P+REAAAAAAMAIiMNAAIMRAACST3/wkVAAAAACEoIAIhMEACIThgAgEACDyghgg1IUgAACFQAAACAAs8Dn4EDAgAazXr/wAQ"
            + "AAAAAEAACI7IpImPKsAJASEgAAKgALDfqACx37AAst+4ALPfwAC038gAv9+O/wAX0AC9JwgA4AMAAAAA"),
        (0x00118978, "oP69JwABv/8IAbD/EAGx/xgBsv8gAbP/KAG0/zABtf84AbTnPAG150ABtuchgIAAPAAIjtcAABEAAAAADAARjdQAIBIAAAAAkAIkjrf0BAwAAQUk"
            + "IgASPIgbUiYhmEICEAAIjiAAqK8UAAiOJACorxgACI4oAKivTEQGDCAApCegBmiSAKCIRCClgEYgAKDHQQAURgAAoedAABRGEACh5yQAoMdBABRG"
            + "BACh50AAFEYUAKHnKACgx0EAFEYIAKHnQAAURhgAoechIKADEAClJ0AApifiIgQMIAAHJMgACI4AqIhEYK2ARsA/ATwAsIFEg7UURkAAtCcAAISO"
            + "PwCAEAQAlCb8/5EQDACIjCQACTH5/yAVGAAJMff/IBEAAAAAEACAxCAAoccBAAFGAgAARhQAgsQkAKHHgRABRoIQAkYAAAJGBAAARhwAgcQBAAFG"
            + "AAiARCgAAUY0ABRG5v8ARQAAAAABoABGAgAVRgIAFkYpABVGJAAARgAABkQCAMAUAAAAAAEABiQhOMAAwACkryEoIAIBAAg8oIYINSFIAAAhUAAA"
            + "AwALPA5+BAxgAGs1wACkjwwAiIwQAAgxzv8AEQAAAACAAoDEAAiARDQIAEbJ/wBFAAAAAJgGaJIACIhEYAiARqBBATwAEIFEQwgCRgIAAUbA/wAQ"
            + "gAKA5CAAqI8wAKivJACojzQAqK8oAKiPOACor9SkhI8gAKUnAB4FDIQGTMYoAKHHNAgARgcAAUUAAAAAgMcBPAAQgUQ0AAJGAgABRQAAAAA4AKDn"
            + "BLsEDFQABCQqAEAQIaBAACEggAIwAKUnITAAAFYsBAwhOAAAFAAIPIDOCCU4AIiuDACIjgAECTwOAyk1JUAJAQwAiK5QAICuUACEJiEoIAIgbAUM"
            + "ITAgAkBBCDwgAIiuOrUEDCEggAKoBmiSAACIRCAAgEYcAIDmMACojzwAiK40AKiPQACIrjgAqI9EAIiuTACAopgGaJKASAgAIUAJAcikiY8hQAkB"
            + "SACIrrIlBAwhIIACqAZokgCgiEQgpYBGxAZAxgKlAEaYBmiSAKiIRGCtgEYgQQE8AACBREOtAEbABlWOIaAAACpAlQIWAAARAAAAAAS7BAxgAAQk"
            + "EgBAECEgQAAARBQAGwAVARIwAAAANAYAAzQGADAApSchOAAAIUAgAiFIAAAhUAAABqMARgBogEQAcIBElMQGDMarAEbp/wAQAQCUJgABv98IAbDf"
            + "EAGx3xgBst8gAbPfKAG03zABtd84AbTHPAG1x0ABtscIAOADYAG9JwYAASQJAGEQBAABJAUAYRQAAAAAIAACJGQAAyTYACKu1AAjrnVCBAgAAAAA"
            + "ckIECAAAAAA="),
        (0x00129318, "GACDECAAASQYAIEQIQABJEwAgRAiAAEkZACBECMAASQdAIEQCQABJAcAgRAkAAEkLgCBECUAASQ3AIEQAAAAAFX5BQgAAAAAIgABPKQbIYyAACEw"
            + "GwAgFAAAAABV+QUIAAAAAE35BQgAAAAAISDAAgAIBTS39AQMPCgFACIACDyIGwglIUACAcAFEpHIBRGR1/kFCDEABDwhIMACAEAFNLf0BAw8KAUA"
            + "IgAIPIgbCCUhQAIBIAYSkSgGEZHX+QUIMQAEPCEgwAK39AQMAAEFJCIACDyIGwglIUACAYgGEpGQBhGR1/kFCDEABDwhIMACAIAFNLf0BAw8KAUA"
            + "IgAIPGhLCCUhQAIBsAASkbgAEZHX+QUIMQAEPCEgwAIBAAU8t/QEDDwoBQAhAAg8kLgIJSFAAgHwABKR+AARkdf5BQgxAAQ88P+9JyEgwAIAEAU0"
            + "t/QEDDwoBQAiAAg8iBsIJUBIAgAhSAkB4AUplRkAKSUyAAokGwAqARJIAAAAAKmvIgAEPFLsBAzIIYQkIShAAAAAqY8AYIlEIGOARgBogEQQAL0n"
            + "0fkFCCEgoAPw/70nISDAAgAgBTS39AQMPCgFACIACDyIGwglIUACAfAFCZEAAKmv+AUJkQQAqa8iAAQ8UuwEDNwhhCQhKEAAAACpjwBgiUQgY4BG"
            + "BACpjwBoiURga4BGEAC9J9H5BQghIKAD"),
        (0x00129C48, "0P+9JwAAv/8IALD/EACx/xgAsv8gALP/3ooFDAAAAAAWAAg8fCsJjZQrCo0CXAkAAzwMJDQAbBUCXAoAaiQMJDEAbBUATAkAAFQKAANUCgAhmCoB"
            + "IgAQPIgbECYMABGOgAASJiAAIBIAAAAAAABIjgQACS0ZACARIVBgAggWCyQYWAsBIVBLAUQADDxFTow1IXAAAD8ADSQAAE+NBgDsEQAAAAABAM4l"
            + "+//NFVgASiUKAAAQAAAAAAgAWCYWABkkAAAPj///OScAAE+tBAAYJ/v/IBcEAEolAABMrf//MSbg/wAQYABSJiEACDyQuAglBQAIEgAAAAAhgAAB"
            + "CAARjtj/ABAQABImAAC/3wgAsN8QALHfGACy3yAAs98IAOADMAC9Jw=="),
        (0x0012A710, "yKSIjwoACSQaAAkBEEAAAF0AABVQAIqMWwBAEQAAAAAMAEuNWABgEQAAAABg/70nYAC//2gAsP9wALH/eACy/4AAs/+IALT/IYCAACGIYAGQAiSO"
            + "t/QEDAABBSQiAAg8iBsIJSFAAgGwBhKRuAYTkSIACDyIGwglHAAAxhAAAcaBCABGAACi54AIAEYQAKLnFAABxoEIAEYEAKLngAgARhQAouc0AADF"
            + "GAABxoEIAEYIAKLngAgARhgAouchIKADEAClJyAApifiIgQMEAAHJCAAtCcAAISOIwCAEAQAlCb8/5EQDACIjDQACDEQAAkk+P8JFQAAAAAQAIDE"
            + "EAABxgEAAUYCAABGFACCxBQAAcaBEAFGghACRgAAAkYEAABGHACBxAEAAUYcAAHGNAABRuj/AEUAAAAAISggAiEwQAIhOGACAQAIPKCGCDUhSAAA"
            + "IVAAAAMACzwOfgQMYABrNdz/ABAAAAAAISAAAmAAv99oALDfcACx33gAst+AALPfiAC036AAvSfmwwYIAAAAAA=="),
        (0x0012BAE8, "QP+9JyIAFjxoS9YmkAKEjgCABTS39AQMPCgFACG4QAAhQMICmAASkbAACZG4AAqRfAqrjyYAaaUoAGqlMQAEPHARhCSKmwUMISiAAsAAwMYQAIHG"
            + "gQgARgAAoueACABGEACi5xQAgcaBCABGBACi54AIAEYUAKLnGACBxoEIAEYIAKLngAgARhgAouchIKADEAClJyAApifiIgQMIAAHJMAA1MYCpRRG"
            + "PgBAGiGAAABGpQBGIAC5JwAAKI8ZAAARAAAAABUAFBEMAAmNNAApMRAACiQRACoVWgEJhQ8AIBkAAAAAEAAAxRAAgcYBAAFGAgAARhQAAsUUAIHG"
            + "gRABRoIQAkYAAAJGNAAVRgMAAEUAAAAARgUARiGAIAPm/wAQBAA5Jx8AABIAAAAAAAARjgAAFK4UACzGFACAxgFjAEYQAC3GEACAxpLDBAxBawBG"
            + "IZhAAAS7BAxYAAQkEQBAECGAQAAQAIiOoACorxQAiI6kAKivGACIjqgAqK8hIAACoAClJyEwYAKk/QYMITggApACiI5QAAiiUQAXosL/ABD//1Im"
            + "wAC9Jz2KBQgCAAIkoP+9JyIAFjyIG9YmqPQEDJAChI44AHKQQABzkEgAaJBQAGmQHAqqjyYASKUoAEmlIbgAAAQAYBIAAAAAUMEEDAAAAAAhuEAA"
            + "GACUxgQAYBIAAAAAAKiARAsAABAwANbGyKSIjwMACDEDAAktCkAJAIBACAArAAk80PEpJSFIKAEAADXFEAA2xSGAAAAqQBICQQAAEQAAAABASBAA"
            + "//9KJiOYKgFABgskGFhrAjQAjJYhYIsBAGQMAANkDABIAKyvzMMEDCEggAEoAMHGAgABRhAAgsYAEABGUACg56bDBAxIAKSPKADBxgIAAUYUAILG"
            + "ABAARlQAoOdYALTnBLsEDGAABCQiAEAQTACirwAQk0SgEIBGgD4BPAAIgUSCEAFGIADMxkITFUaAPwE8AHCBRCQAz8YCEBZGJAAARgAACUQATAkA"
            + "A0wJACEgQABQAKUnSACmjyE44AIhQIAClMQGDCFQAABMAKKPEwAIPHARCCU4AEisXABJjAUAIBEAAAAADAApjQIAIBGAAAglOAAorb7/ABABABAm"
            + "YAC9Jz2KBQgCAAIkwP+9JzAAv/8QALD/GACx/yAAsv8hgIAAIYigACGQwAAQACiOAACorxQAKI4EAKivGAAojggAqK8hIAACISigAyEwAABWLAQM"
            + "ITgAABMACDwwUAglOAAIrjwAAK4MAAiOAQAINQwACK4iABk8iBs5JxwAIMbcBSHHAAABRhwAAObIpIiPIUASAUAACK7//wgkRAAIrjwABCYhKCAC"
            + "IGwFDCEwIAKyJQQMISAAAiFAAAAhSAAAIVAAAkgASaU0Qgw8AwALKQYAYBUAAAAA8EEMPAYACykCAGAVAAAAAHBBDDxMAEytnKSLjxkADTwNZq01"
            + "GFhtAW48DTxf8601IVhtAZyki68CbAsAUABNpVVVKSUBAAglCQALKef/YBUMAEolMAAEPHB5hCSKmwUMISgAAjAAv98QALDfGACx3yAAst8IAOAD"
            + "QAC9Jw=="),
        (0x0012CD30, "yKSIjwoACSQaAAkBEEAAAEoAABVQAIqMSABAEQAAAAAMAEuNRQBgEQAAAABg/70nkAC//2AAsP9oALH/cACy/3gAs/+AALT/IYCAACGIYAGo9AQM"
            + "kAIkjkgAcpBQAHOQIgAIPIgbCCUcAADGEAABxoEIAEYAAKLngAgARhAAoucUAAHGgQgARgQAoueACABGFACi5zQAAMUYAAHGgQgARggAoueACABG"
            + "GACi5yEgoAMQAKUnIACmJ+IiBAwQAAckIAC0JwAAhI4UAIAQBACUJvz/kRAMAIiMNAAIMRAACST4/wkVAEAJPIACiawhKCACITBAAiE4YAIBAAg8"
            + "oIYINSFIAAAhUAAAAwALPA5+BAxgAGs16/8AEAAAAAAhIAACkAC/32AAsN9oALHfcACy33gAs9+AALTfoAC9J+bDBggAAAAAoP+9JyAAsP8oALH/"
            + "MACy/zgAtOc8ALXnQAC250gAv/8hgIAAPAAIjjsAABEAAAAADAARjTgAIBIhIKAAipsFDCEoAALMwwQMNAAkhiBBATwACIFEAgUBRqbDBAw0ACSG"
            + "IEEBPAAIgURCBQFGKAASJJykiI8ZAAk8DWYpNRhACQFuPAk8X/MpNSFACQGcpIivAyQIAMzDBAwcAKSvHAABxgIAAUYQAAHGAAABRoAFFEamwwQM"
            + "HACkjxwAAcYCAAFGFAABxgAAAUYAABVGAAC25wQAoOcYAADG8EIBPAAIgUQAAAFGCACg5xAAoK8UAKCvGACgrysABDyI7oQkISigAxAApifSPwUM"
            + "AGCARP//UibX/0AWAAAAACAAsN8oALHfMACy3zgAtMc8ALXHQAC2x0gAv98IAOADYAC9JyIAFjyIG9YmkAKEjgAIBTS39AQMPCgFACFAwgLABRKR"
            + "yAUTkbwJqo8mAFKlKABTpZACiI4EAAktIgAgEYBACAAhuMgCyAbpjhAAIBEAAAAADAAwjQ0AABIAAAAAyKSIj9AFyY4hQAkBQAAIrrQAEqK1ABOi"
            + "MAAEPHB5hCSKmwUMISgAAg8AABAAAAAABLsEDMAABCQLAEAQIYBAACEgQAAhKIACj68EDNAFxo60ABKitQATosgG5CYhKAACIGwFDCEwAAI9igUI"
            + "AgACJA=="),
        (0x0012F790, "0P+9JwAAv/8IAKT/EACl/xgAsP8gALH/IgAQPGhLECYhiAAABQAqKgoAQBEAAAAAAEERAMBIEQAhQAkBIUAIAggACCWAUBEAIVAKAgUAABCAAEol"
            + "IQAIPFC5CCUhAAo8aLlKJSgAqq8IAKTflusEDCEoAAEJAEAUKACqjwAARY2W6wQMCACk3wQAQBQAAAAACACk35brBAwQAKXfAgBAECFIAAA4AEmM"
            + "gEARACFACALQAAmtAQAxJgYAKCrZ/wAVAAAAACIAEDyIGxAmHAAIjgEACDEKAAARCACk35brBAxgAAUmCABAFAgApN8xAAU8lusEDPCppSQDAEAU"
            + "CACk35brBAwQAKXfAAC/3xgAsN8gALHfCADgAzAAvSchKEACkAJMjgQAjS1BAKARgGAMACIADTyIG60lIXCsARgHz40lAOARyKSYjygH2Y0jyDgD"
            + "//85JxAGuI0ryDgDHQAgEwwAmIwQABgzGwAAE///7yUYB8+tQDAGAEA4BwDA/70nAAC//wgApP8QAKb/GACn/yAAqP8oAKn/MACq/zEABDz4lYQk"
            + "ipsFDCEoQAIAAL/fCACk3xAApt8YAKffIACo3ygAqd8wAKrfQAC9JwIAABAhKEACGAfArZACTI6AYAwAIgANPIgbrSUhcKwB6AbPjQ8A4BH4BtiN"
            + "CAAZLwwAIBMhwLgB8AUYk2QAGCdkABkkGFjYADIAayUaAHkBEjAAABhY+AAyAGslGgB5ARI4AAAIAOADAAAAAPD/vScAAL//zMMEDAAAAAAFAAAQ"
            + "AAAAAPD/vScAAL//psMEDAAAAAAiAAE8LEwhxAIAAUYAAL/fCADgAxAAvSc="),
        (0x00130540, "QP+9J7AAv/8sAMDGEACBxoEIAEYAAKLngAgARhAAoucUAIHGgQgARgQAoueACABGFACi5xgAgcaBCABGCACi54AIAEYYAKLnISCgAxAApScgAKYn"
            + "4iIEDCAAByQAAKTGBAClxiwAxsaCMQZGgDEGRiEQAAAgALknAAAojx4AABEEADkn/P8UEQAAAAAMAAmNNAApMRAACiT3/yoVAAAAABAAAMUQAIHG"
            + "AQABRhQAAsUUAIHGgRABRsIABEZCEAVGwBgBRgAIgEQ2GAFG6v8BRQAAAAACAABGghACRgAAAkY0AAZG5P8ARQAAAACGAQBG4f8AECEQAAGwAL/f"
            + "CADgA8AAvScgAGGG9/8hJCsAIBQAAAAAgP+9JwAAv/8IAKL/EACj/xgApP8gAKX/KACm/zAAp/84AKj/QACp/0gAqv8SQAAAUACo/xBAAABYAKj/"
            + "kAKEjrf0BAwAAQUkIgAIPIgbCCUhSAIBiAYqkSYAaqaQBiqRKABqpoAGCo0sAGquUACo3xMAAAFYAKjfEQAAAQAAv98IAKLfEACj3xgApN8gAKXf"
            + "KACm3zAAp984AKjfQACp30gAqt+AAL0nCADgAywAYcY="),
        (0x00131270, "8P+9JwAAv/80AQiOAQAIMQwAABFoMwmOCgAgEQEABTSQAiSNUqoFDDwoBQAjAQiSIUACAQABCS3/AAokCkBJASMBCKIAAL/f5JwFCBAAvSc="),
        (0x00132018, "IQABPJC5JMQCIQRGQCEERkApBUaAIQVGIRAAACHIoAAUABg8gM8YJwAAKI8rAAARAAAAACcABBEMAAmNNAApMRAACiQjACoVWgEJhSEAIBkAAAAA"
            + "EAAAxRAAgcQBAAFGAgAARhQAAsUUAIHEgRABRoIQAkYAAAJGNAAERhUAAEUhWKAAAABsjQ0AgBEAAAAAOACNjQgAuBWQAo2NBgCgEQAAAAAMAK2N"
            + "AwCoFQAAAAADAAAQAAAFRvL/ABAEAGslNAAGRgMAAEUAAAAAhgEARiEQIAPU/wAQBAA5JwgA4AMAAAAAXACIjBgAABUMAKmMJAAqMRUAQBUYAC0x"
            + "EwCgETwAi4wCAGARIWAAAAwAbI0BAAgkXACIrPQBCCREAIisVACGjFgAh4whIKAAISiAAQEACDyghgg1IUgAACFQAAADAAs8Dn4ECEABazUIAOAD"
            + "AAAAAA=="),
        (0x00132D90, "wP+9JzAAv/8QALD/GACx/yAAsv8hgIAAIYigACGQwAAQACiOAACorxQAKI4EAKivGAAojggAqK8hIAACISigAyEwAABWLAQMITgAAAwACI4BAAg1"
            + "DAAIrsikiI8hQBIBQAAIrhMACDywTwglOAAIrjwAAK48ACiOAgAJPCRQCQFEAAquJUAJATwAKK4cACDGIEEBPAAIgUQAAAFGHAAA5jwABCYhKCAC"
            + "IGwFDCEwIAKyJQQMISAAAiEgAAIwAAU8nbMEDDh7pSQwAL/fEACw3xgAsd8gALLfCADgA0AAvSeQAoyMBACNLTEAoBGAYAwAIgANPIgbrSUhcKwB"
            + "6AbPjQ8A4BENAPgwDQAAEwEAuCgLAAAX+AbYjQgAGS8IACATIcC4AfgFGJNkABkkI8A4AxjAuAAyABgnGgAZAxIoAADYBs+NGgDgEaAF+DAYAAAT"
            + "yKSYjwgH2Y0jyBkDCgA5LxMAIBcMAO+NEQDgEQgH2K3Q/70nAAC//wgApP8QAKX/GACm/yAAp/8hIOABMAAFPJ2zBAwwO6UkAAC/3wgApN8QAKXf"
            + "GACm3yAAp98wAL0n/pgFCAAAAAAiABY8iBvWJpAChI4AIAU0t/QEDDwoBQAhkEAAkAKIjgQACS0nACARgEAIACG4yAL4BvKuAAbTjugG6Y4RACAR"
            + "AAAAAAwAMI0OAAASAAAAAMikiI8hSBMBRAAJrkgAAMZIQgE8AAiBRAIAAUYkAABGAAAKRCNQCgFAAAquDQAAEAAAAAAEuwQMUAAEJA0AQBAhgEAA"
            + "ISBAACEogAIqgwcMITBgAugG5CYhKAACIGwFDCEwAAIwAAQ8KHmEJIqbBQwhKIACPYoFCAIAAiRQAIiMLgAAEQAAAAAMAAmNKwAgEQAAAABaASqF"
            + "KABAGQAAAABIAiqRWQCLkCtgSwECAIARAAAAAEgCK6HIpIqPXACLjCpgSwEeAIAVAAAAACIADTwwTKyNIWBMAVwAjKzg/70nAAC//wgApP9YAIqQ"
            + "4DgLJBhQSwEqAAs86LxrJSFQSwEAAEWNWgCGkFsAh5AhICABAQAIPKCGCDUhSAAAIVAAAAIACzwOfgQMRABrNQAAv98IAKTfAgAAECAAvSdMAICs"
            + "NPwGCAAAAADg/70nAACw/wgAsf8QAL//IYCAACGIoAATAAg8sE8IJTgACK4hIAACMQAFPJ2zBAzQEKUkPAAIjgwAABEAAAAADAAJjQcAIBFEAAqO"
            + "BQBAFTwAK439/ww8//+MNSRYbAE8ACutZmwFDDwABCY8AAQmjCAEDAIABSQhIAACNi0EDCEoAAABACgyISAAAgAAsN8IALHfEAC/3wMAABEgAL0n"
            + "GrkECAAAAAAIAOADAAAAACIAFjyIG9YmkAKEjgAQBTS39AQMPCgFAEBAAgAhQMgC4AUSlZACiI4EAAktHQAgEYBACAAhuMgC2Abpjg0AIBEAAAAA"
            + "DAAwjQoAABIAAAAAyKSIjyFAEgFAAAiuISAAAjAABTydswQMOHulJA0AABAAAAAABLsEDFAABCQJAEAQIYBAACEgQAAhKIACZMsEDCEwQALYBuQm"
            + "ISgAAiBsBQwhMAACPYoFCAIAAiTQ/70nAAC//wgAsf8QALL/GACz/yGI4ACMAvKMaP0GDEgC85CMAjKuFAAIPADOCCU4AEisUAAJklgASaBRAAqS"
            + "IgAPPGhL7yXIAPiNIVjqAaAAbJFZAEygK2hsAgpgbQJIAiyisABskVoATKC4AGyRWwBMoKgAbJGAaAwAIWCsAcikjo8hYMwBTABMrCHA2AFcAFis"
            + "AAC/3wgAsd8QALLfGACz3wgA4AMwAL0n0P+9JwAAsP8QALH/IAC//yGIgAAhgKAAMQAIPHB0CCU4ACiuUAAojgkAABEAAAAADAAJjQYAIBEAAAAA"
            + "SAIqkVkAK5ICAEsVAAAAAEgCIKFQACQmjCAEDAIABSQhICACNi0EDCEoAAABABAyBgAAEiAAv98hICACEACx3wAAsN8auQQIMAC9JxAAsd8AALDf"
            + "CADgAzAAvSchAAg8sLkIJcikiY8hABk8pLk5JwAAOY8hyDkBIVAAAf9/Djz//841EAALJAAADI0KAIQRCAAPjSrA7gEDAAATAAAAACFw4AEhUAAB"
            + "//9rJfb/YBUMAAglIUBAAQAABK0EAAWtCADgAwgAGa0="),
        (0x00134D74, "4P+9JwAAsP8IALH/EAC//yGAgAAhiKAAEwAIPDBQCCU4AAiuISAAAjEABDyAXoQkipsFDCEoAAI8AAiOAwAAEQAAAABmbAUMPAAEJjwABCaMIAQM"
            + "AgAFJCEgAAI2LQQMISgAAAEAKDIhIAACAACw3wgAsd8QAL/fAwAAESAAvScauQQIAAAAAAgA4AMAAAAA"),
        (0x00134F18, "kAKMjAQAjS0fAKARgGAMACIADTyIG60lIXCsAegGz40ZAOARAAAAAAwA740WAOARAAAAAEgA4MUEBqHFAgABRoA/ATwACIFEAAABRuD/vScAAL//"
            + "CACw/xAAtOchgIAAaAEUxgIAFEa8cAUMaAEA5mgBFOYAAL/fCACw3xAAtMcIAOADIAC9J7xwBQgAAAAA"),
        (0x001350B0, "IwEokgYAABEAAAAAwAAgxgAIiERgCIBGAAABRsAAIOZSqgUIAAAAAA=="),
        (0x0013CF00, "0P+9JwAAsP8QALH/IAC//yGIgAAhgKAAMgAIPDC1CCU4ACiukAIojgMAABEAAAAAZmwFDJACJCaQAiQmjCAEDAIABSQhICACzLgEDCEoAAABABAy"
            + "BgAAEiAAv98hICACEACx3wAAsN8auQQIMAC9JxAAsd8AALDfCADgAzAAvSc="),
        (0x0013D2A0, "8P+9JwAAv/9SqgUMAAQFPAEAQygLEGMABgBDKAUAASQKECMAIgADPIgbYyQhGGIAAAC/3wgA4AMQAL0n8P+9JwAAv/9SqgUMAAAAAAEAQygLEGMA"
            + "BgBDKAUAASQKECMAAAC/3wgA4AMQAL0nIQAJPLC5KSXIpIqPEAALJAAALI0OAIYVCAAtjSpwqgELAMAVBAAtjeA4DiQYaK4BKgAOPOi8ziUhaK4B"
            + "AACtjQYAoBEAAAAACAAAASEwoAH//2sl7v9gFQwAKSUIAAABAAAAAJAChI4EAAU0t/QEDDwoBQCQAoiOBAAJLQ8AIBGAQAgAIgANPIgbrSUhcKgB"
            + "IUiiAQgGKZEYB8mtyKSKjxAGq40hUEsBKAfKrTAABDxYeYQkipsFDCEogAI9igUIAgACJCoAATzYXSE0IwhBABgAISwDACAUAAAAAAgA4AMAAESM"
            + "KgABPNhdITQjCEEAIgAEPDhMhCQhIIEACADgAwAAhIw="),
        (0x00166E28, "4P+9JwAAv/8IAKX/ISiAADIABDwS6gQMGEqEJCEgQAAIAKXfgD8BPABggUTQyQcMAAAAAAAAv98IAOADIAC9JwYAABYEAAEkAgBhFAAAAAABAAMk"
            + "mj8ECAAAAAAFAGEUAAAAAF5iBAwhICACAQADJMAAI66UPQQIAAAAAOD/vScAAL//CACk/xAApf8iAAU88CGlJJbrBAwAAAAAAwBAFAgApN+W6wQM"
            + "EACl3wAAv98IAOADIAC9JwYAASQFAGEQBAABJAUAYRAAAAAANz0ECAAAAAAtPQQIAAAAABs9BAgAAAAA")
    };

    /// <summary>
    /// The eight vtable copies (0x80 bytes each): the game's vtable at GameVa as the file has it, with these words
    /// replaced (FireTrailMaker; FireTrail: her trail's update, no touch; BladeBarrierSpell: its destructor and update;
    /// AntimagicFieldSpell: its destructor; DodgeBall: its touch, Xantam's Orbs'; FireTrail again: Inferno Arrow's flames'
    /// update, no touch; VineSpell: Entangle's vines' destructor and update; InsectPlagueSpell: Insect Plague's swarms'
    /// destructor and update).
    /// </summary>
    public static readonly (int Va, int GameVa, (int At, uint Word)[] Words)[] VTables =
    {
        (0x00131170, 0x00316718, new (int, uint)[] {  }),
        (0x001311F0, 0x00316798, new (int, uint)[] { (0x24, 0x0012CD30), (0x3C, 0x0012E418) }),
        (0x00135030, 0x0031B650, new (int, uint)[] { (0x0C, 0x00134D74), (0x24, 0x0010CD50) }),
        (0x00134FB0, 0x0031B5D0, new (int, uint)[] { (0x0C, 0x001330E0) }),
        (0x0013D220, 0x0031C3C8, new (int, uint)[] { (0x3C, 0x001320FC) }),
        (0x0013CE80, 0x00316798, new (int, uint)[] { (0x24, 0x0012A710), (0x3C, 0x0012E418) }),
        (0x0013CE00, 0x00317470, new (int, uint)[] { (0x0C, 0x001332E8), (0x24, 0x00133014) }),
        (0x0013CF80, 0x0031B530, new (int, uint)[] { (0x0C, 0x0013CF00), (0x24, 0x00102D80) })
    };

    public const int VTableSize = 0x80;

    /// <summary>The data block (in the pool's stretch 59 since version 10; every earlier version's is at <see cref="LegacyDataVa"/>; the icons of types 32-36 and Entangle's settings are in the second block, <see cref="Data2Va"/>, since version 11; Insect Plague's list entry, ability record and settings in the third, <see cref="Data3Va"/>, since version 12: the mask at <see cref="SpellsAt"/> has its bit, but the counts and entries here are the other nine's).</summary>
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
                     CastTypes = 37, GameCastTypes = 23;

    /// <summary>
    /// The second data block (version 11: the start of the pool's stretch 60, marked "FEA2" and the version): the names
    /// of the icons of types 32-36 and their fallbacks, Entangle's settings by rank (its vines, how much slower a wrapped monster
    /// walks: the game's slow byte, half the percentage; how long a vine holds it, in tenths of a second; its thorns' least and
    /// most damage), its fixed values, then (run time) the icons' textures. The same whichever spells are on, but for
    /// Entangle's settings.
    /// </summary>
    public const int Data2Va = 0x00224B68, Data2Size = 0xE8, EntangleVinesAt = 0x98, EntangleSlowAt = 0xA0,
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
        "RkVBMgwAAABpY29uX2JsYWRlYmFycmllci50ZXgAAABpY29uX2FudGltYWdpY2ZpZWxkLnRleABpY29uX3JpZ2h0ZW91c21pZ2h0LnRleABpY29u"
        + "X3hhbnRhbW9yYnMudGV4AAAAAABpY29uX2VudGFuZ2xlLnRleAAAAAAAAAAgqjAA8KkwAHiqMADwqDAAQKkwAAAAAAAAAQIDBAUAAAAPERQWGQAA"
        + "ACgtMjc8AAAAAgMEBQYAAAAEBQYHCAAAAABwQwAAAEAyAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA==";

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
        "RkVBMwwAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAaWNvbl9pbnNlY3RwbGFndWUudGV4AAAAKKkwAAAAAAAAAQIDBAUAAABQUFBQUAAAAAEBAQEBAAAAAwMD"
        + "AwMAAAAAcEMAABZDGQAAADIAAAAAAKBABAEAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

    /// <summary>
    /// The orbs' fan (the data block has them, from the template): 200 a second, 12 degrees between neighbouring
    /// orbs (OrbsFanAt holds the cos and sin of half of it), the fan's point 32 behind her hand.
    /// </summary>
    public const float OrbsSpeed = 200f, OrbsFanDegrees = 12f, OrbsApex = 32f;

    /// <summary>The cast table (37 types: the game's 23 cases, then the default but for the spells that are on): a piece of the pool of its own since version 8.</summary>
    public const int CastTableVa = 0x00134CE0;

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
        "RkVBVAwAAAAAAAAAAAAAAAAAAAA4BwAASJwSAAAAAACamZk/AAAAQAAAwEEAALRDAIA7RQAAAEIAAwMFBQUAAAAAAAAAAQAAAAEBAgIDAAAAAgME"
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
        new("xantam", 0x1, 1, 26, 10, 0x0012BC98, "xantamfire", "icon_xantamfire.tex",
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
        new("powerattack", 0x10, 2, 34, 18, 0x0013D370, "powerattack", "icon_powerattack.tex",
            "AgAAAAAAAABwb3dlcmF0dGFja19uYW1lAAAAAAAAAAAAAAAAAAAAAHBvd2VyYXR0YWNrX2Rlc2MAAAAAAAAAAAAAAAAAAAAAAAUFAAAAAAAAAAAA"
            + "BAAAAAAAAAAAAAAA",
            "",
            ""),
        new("bladebarrier", 0x2, 2, 43, 32, 0x0012CFB0, "bladebarrier", "icon_bladebarrier.tex",
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
        new("antimagic", 0x4, 2, 44, 33, 0x00133190, "antimagicfield", "icon_antimagicfield.tex",
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
        new("righteousmight", 0x8, 2, 45, 34, 0x00132F48, "righteousmight", "icon_righteousmight.tex",
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
        new("xantamorbs", 0x40, 1, 46, 35, 0x0010C338, "xantamorbs", "icon_xantamorbs.tex",
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
        new("entangle", 0x100, 0, 47, 36, 0x0012BAE8, "entangle", "icon_entangle.tex",
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
            + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA==")
    };

    /// <summary>The hooks: where, the game's word, the patch's word, and the spells (mask) any one of which needs it.</summary>
    public static readonly (int Va, uint Game, uint Patched, int Spells)[] Hooks =
    {
        (0x0010F4AC, 0x1462000B, 0x08059BB8, 0x80),
        (0x0010F648, 0x12000207, 0x08059B9A, 0x80),
        (0x001109C0, 0x14620004, 0x0804634F, 0x80),
        (0x0011B758, 0x0100F809, 0x0C04F4C3, 0x200),
        (0x0015E220, 0xC661002C, 0x0C04C191, 0x80),
        (0x0015E424, 0x2C420003, 0x2C420004, 0x80),
        (0x0015E50C, 0x10000013, 0x08040AD7, 0x80),
        (0x00161EF8, 0x2C830017, 0x2C830025, 0x3FF),
        (0x00161F00, 0x3C020030, 0x3C020013, 0x3FF),
        (0x00161F08, 0x24427BC0, 0x24424CE0, 0x3FF),
        (0x001676E4, 0x0C05AA52, 0x0C04D42C, 0x20),
        (0x00167AFC, 0x0240282D, 0x0C04BE2B, 0x18),
        (0x0016DD2C, 0x0C04EB96, 0x0C059BA9, 0x80),
        (0x0016DD44, 0x0C04EB96, 0x0C04BDE4, 0x3FF),
        (0x0017A4BC, 0x0C05ED8E, 0x0C043170, 0x3FF),
        (0x0017B594, 0x0C059CE4, 0x0C04C49C, 0x20),
        (0x0017E52C, 0x14830009, 0x0804A4C6, 0x3CE),
        (0x0017EA44, 0x8C440000, 0x0C04F4F4, 0x3CE),
        (0x0017EAB4, 0x8C440000, 0x0C04F4F4, 0x3CE),
        (0x0018331C, 0x0C058ADE, 0x0C04A712, 0x3FF),
        (0x001B177C, 0x3C030031, 0x8E230038, 0x1),
        (0x001B1784, 0x24636798, 0x24630080, 0x1),
        (0x001BED7C, 0x0C04C3CC, 0x0C04BE72, 0x100),
        (0x001BED94, 0x0C04C3A6, 0x0C04BE78, 0x100),
        (0x001BEFD0, 0x24040058, 0x24040060, 0x100),
        (0x001BF004, 0x0C06FD68, 0x0C04CC8F, 0x100),
        (0x0030A104, 0x001663F8, 0x00132E70, 0xC),
        (0x0030A11C, 0x0015C2F0, 0x00134F18, 0x8)
    };

    /// <summary>The game's words the code relies on (checked before it goes in, never changed).</summary>
    public static readonly (int Va, uint Word)[] Checks =
    {
        (0x0010D2F0, 0x27BDFF50),
        (0x0010F438, 0x8E0300C0),
        (0x0010F46C, 0x3C040032),
        (0x0010F47C, 0x24A53470),
        (0x0010F4A8, 0x00000000),
        (0x0010F4B0, 0x3C040032),
        (0x0010F4B4, 0x3C050030),
        (0x0010F4DC, 0x10600003),
        (0x0010F62C, 0x0C043880),
        (0x0010F644, 0x0044800B),
        (0x0010F64C, 0x8E2300C0),
        (0x0010F650, 0x2462FFFF),
        (0x0010F654, 0x2C420002),
        (0x0010F6D0, 0x8E2300C0),
        (0x0010F6DC, 0xAE2000DC),
        (0x0010FE68, 0x24020001),
        (0x0010FE6C, 0x146200DD),
        (0x001109B0, 0x14620003),
        (0x001109B4, 0x24020006),
        (0x001109C4, 0x24020080),
        (0x001109C8, 0x240303E8),
        (0x001109CC, 0xAE2200D8),
        (0x001109D0, 0xAE2300D4),
        (0x001109D4, 0x1200001E),
        (0x0011B70C, 0x8623015A),
        (0x0011B72C, 0x0220302D),
        (0x0011B734, 0x24070040),
        (0x0011B738, 0x8CA80014),
        (0x0011B75C, 0x0200282D),
        (0x0011B760, 0x8F82A964),
        (0x0011C168, 0x27BDFF20),
        (0x0012E330, 0x27BDFFD0),
        (0x00130E48, 0x27BDFFD0),
        (0x00147800, 0x27BDFE30),
        (0x0015E20C, 0x82620024),
        (0x0015E224, 0x01761018),
        (0x0015E228, 0x00513021),
        (0x0015E22C, 0xC4C000BC),
        (0x0015E24C, 0x46010001),
        (0x0015E250, 0xE4C000BC),
        (0x0015E420, 0x3042FFFF),
        (0x0015E428, 0x14400007),
        (0x0015E430, 0x24020015),
        (0x0015E438, 0x24020014),
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
        (0x00191130, 0x27BDFBF0),
        (0x001B1250, 0x27BDFEE0),
        (0x001B1450, 0x24636798),
        (0x001B156C, 0x0080882D),
        (0x001B1790, 0xAE030038),
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
        (0x001DFA2C, 0x0C04B8CC),
        (0x001DFA70, 0x27BDFF20),
        (0x001DFAB0, 0x0C07CAA0),
        (0x001DFAB4, 0x8E84029C),
        (0x001DFABC, 0x8E830294),
        (0x001DFAC4, 0x28440064),
        (0x001DFF64, 0x0C04705A),
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
        (0x001E8320, 0x27BDFFB0),
        (0x001E8380, 0x0C04B200),
        (0x001E8384, 0x0000382D),
        (0x001E8388, 0x3C030032),
        (0x001E838C, 0x2463C3C8),
        (0x001E8390, 0xAE030038),
        (0x001E83A8, 0xAE020050),
        (0x001E83AC, 0x0C0425B2),
        (0x00233804, 0x001BF410),
        (0x003058AC, 0x0010F5C8),
        (0x003058C4, 0x00110900),
        (0x00307C04, 0x001628F0),
        (0x0030A0F4, 0x0016AC68),
        (0x003167BC, 0x001B0F98),
        (0x003167D4, 0x001B0EB8),
        (0x0031747C, 0x001BF058),
        (0x00317494, 0x001BF0D0),
        (0x0031749C, 0x001BF598),
        (0x003174FC, 0x001BFB30),
        (0x00317514, 0x001BED28),
        (0x0031B53C, 0x001DF9A0),
        (0x0031B554, 0x001DFF20),
        (0x0031B55C, 0x001DFA70),
        (0x0031B5DC, 0x001DF620),
        (0x0031B5F4, 0x001E0EC8),
        (0x0031B5FC, 0x001DF6D0),
        (0x0031B65C, 0x001DF120),
        (0x0031B674, 0x001DF3B0),
        (0x0031B67C, 0x001DF1F0),
        (0x0031C3D4, 0x001E8418),
        (0x0031C3EC, 0x001E4748),
        (0x0031C3F4, 0x001E82A0),
        (0x0031C404, 0x001E8258)
    };

    /// <summary>The hero's melee hit: the game's call of the hit function, or Corrosive's (a jal either way).</summary>
    public const int MeleeCallVa = 0x00167B04;

    /// <summary>The game's strings the code uses (sounds, fallback icons, the HUD's damage line's key).</summary>
    public static readonly (int Va, string Text)[] Strings =
    {
        (0x003033F8, "spel_cas_fireball.anm"),
        (0x00303470, "spel_cas_firearrow.anm"),
        (0x003034B8, "spel_hit_explarrow.anm"),
        (0x003034F0, "spel_elctric_afteraffct.anm"),
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
        (0x0030A9F0, "icon_protection.tex"),
        (0x0030AA20, "icon_whirlwind.tex"),
        (0x0030AA78, "icon_powerattack.tex"),
        (0x0030E770, "PowerSpellDamageWeapon"),
        (0x0030E7D8, "SpellDamage"),
        (0x003110D0, "spel_cas_copend.anm"),
        (0x00311170, "spel_cas_acid.anm"),
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
    /// Version 11 (entangle-test1: all nine, Entangle the newest; its data block and second block where version 12's are, with
    /// version 12's layout, so its settings are read where version 12 keeps them): its hooks (and which of its spells need each, the
    /// same masks as version 12's), its code's pieces with their SHA-256, its vtable copies and its cast table's place.
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
    /// 12's, with version 10's layout): its hooks (and which of its spells need each, the same masks as version 12's), its code's
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
    /// earlier version's): its hooks (and which of its spells need each, the same masks as version 12's), its code's pieces
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
    /// spells need each, the same masks as version 12's), its code's pieces with their SHA-256 and its vtable copies, and where
    /// its data block keeps the settings (its mask, Xantam's Fire's damage and the entries' prices where version 12 has them).
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
    /// the same masks as version 12's), its code's pieces with their SHA-256 and its vtable copies, and where its data block keeps
    /// the settings (its mask, its entries and Xantam's Fire's damage where version 12 has them).
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
    /// settings (its mask, its entries and Xantam's Fire's damage where version 12 has them).
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
    /// each, the same masks as version 12's), its code's pieces with their SHA-256 and its vtable copies, and where its data block
    /// keeps the settings (its mask, its entries and Xantam's Fire's damage where version 12 has them).
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

    /// <summary>The spells' names, descriptions and HUD lines (0 English, 1 French, 2 German, 3 Italian, 4 Spanish), as gtext_feats12.py writes them (none for Power Attack, Sorcerer's Blood and Inferno Arrow: the game's own).</summary>
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
        ("insectplague", 4, "insectplague_desc", "Un enjambre de insectos se reúne ante el lanzador y vuela hacia el enemigo más cercano, al que pica y envenena cada segundo. Cuando el enemigo muere, el enjambre pasa al siguiente. Un enjambre más en cada nivel.")
    };
}
