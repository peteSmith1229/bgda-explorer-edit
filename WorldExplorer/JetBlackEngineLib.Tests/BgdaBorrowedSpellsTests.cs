using JetBlackEngineLib.Data.DataContainers;
using JetBlackEngineLib.Data.Executable;
using JetBlackEngineLib.Data.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace JetBlackEngineLib.Tests;

/// <summary>
/// The borrowed spells, the Enhancements tab (BgdaExecutable.BorrowedSpells.cs): Xantam's Fire, Sorcerer's Blood (the game's own
/// feat), Xantam's Orbs, Dimension Door (Ilivarra's teleport) and Shatter (new: a ring of frost that freezes the monsters round
/// her) for Adrianna; Power Attack (the game's own feat), Blade Barrier, Antimagic Field, Righteous Might and Phase Axe (new: a
/// thrown axe that passes through monsters) for Kromlech; Inferno Arrow (the game's own cut feat), Entangle (the game's own unused
/// vines), Insect Plague (the kobold shaman's swarm), Poison Gas Arrow (the monsters' poison gas, on an arrow) and Expert Focus (new:
/// Power Attack for his bow) for Vahn; and Improved Enemy AI (the monsters target the nearest hero, surround him and raise the
/// alarm; the casters cast more, the brutes knock you back, the drow blink and the wounded regroup), with or without them. On
/// the stock PAL executable in Test_files (skipped when it is not present), with the Three Players tab switching three players
/// underneath them, and versions 21 (the drow blink's test of Improved Enemy AI, as it was and with featpatch.py's choice of
/// parts), 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6 and 5 (the first
/// test of Shatter, of Phase Axe, of Expert Focus, of Dimension Door, of Poison Gas Arrow, the second of Insect Plague, the first of
/// Entangle and of Inferno Arrow, the second and first of Xantam's Orbs, the first of Sorcerer's Blood, of Power Attack and of
/// Kromlech's spells) brought up to date; their names in GTEXT.LMP and their icons in ICONS.LMP (archives built here). The patch
/// is featpatch.py version 22 (Improved Enemy AI with seven parts; its ninth data block at the pool's stretch 38, its tenth at
/// the start of stretch 53), and versions 21, 20, 19 and 18 (earlier Improved Enemy AI) are brought up to date too: the
/// checksums are of the files featpatch.py writes, and what it does in the game was checked by emulation (test_feats22.py on
/// 104 files: 516 checks on each with all fifteen and the AI; the AI's parts' broken copies on two bases).
/// </summary>
[TestFixture]
public class BgdaBorrowedSpellsTests
{
    private const int VaToFile = 0xFF000;
    private const string StockMd5 = "34874fe1aaed80fe52ad3911a7f304aa";

    /// <summary>
    /// featpatch.py version 22 on the stock executable, each with --ai off (Improved Enemy AI out, as SetBorrowedSpells writes
    /// them without it): all fifteen at the tested settings, all but Shatter (the fourteen of version 16), all but Phase Axe and
    /// Shatter (the thirteen of version 15), all but Expert Focus, Phase Axe and Shatter (the twelve of version 14), all but
    /// Dimension Door, Expert Focus, Phase Axe and Shatter (the eleven of version 13), all but Poison Gas Arrow, Dimension Door,
    /// Expert Focus, Phase Axe and Shatter (the ten of version 12), all but Insect Plague, Poison Gas Arrow, Dimension Door, Expert
    /// Focus, Phase Axe and Shatter (the nine of version 11), all but Entangle, Insect Plague, Poison Gas Arrow, Dimension Door,
    /// Expert Focus, Phase Axe and Shatter (the eight of version 10), all but Inferno Arrow, Entangle, Insect Plague, Poison Gas
    /// Arrow, Dimension Door, Expert Focus, Phase Axe and Shatter (the seven of version 9), all fifteen at the test files' settings
    /// (price 0, 4 mana), Kromlech's first four alone, Power Attack alone, Sorcerer's Blood alone, Sorcerer's Blood alone with 3 ranks
    /// at price 4, Xantam's Orbs alone, Xantam's Orbs alone with its own settings (OrbsCustom), Inferno Arrow alone, Inferno Arrow
    /// alone with its own settings (InfernoCustom), Entangle alone, Entangle alone with its own settings (EntangleCustom), Insect
    /// Plague alone, Insect Plague alone with its own settings (InsectCustom), Poison Gas Arrow alone, Poison Gas Arrow alone with its
    /// own settings (GasCustom), Dimension Door alone, Dimension Door alone with its own settings (DoorCustom), Expert Focus alone,
    /// Expert Focus alone with its own settings (FocusCustom), Phase Axe alone, Phase Axe alone with its own settings (AxeCustom),
    /// Shatter alone, Shatter alone with its own settings (ShatterCustom), and Adrianna's five alone.
    /// </summary>
    private const string AllFifteenMd5 = "db4dc69b82251b36e8a082d4307b8ca6", FourteenMd5 = "7e13eadeb4ac5d265b0890265be20a7a",
                         ThirteenMd5 = "6f896244a8ba5ed470ff3867a9616703", TwelveMd5 = "06e433a69d8d0af536e6cb96d29e3fba",
                         ElevenMd5 = "0854d5dc6b3e04da603a684a99414a89", TenMd5 = "4ad71539889a4b22b3389afa8089dcf2",
                         NineMd5 = "5827605d3e0ab5e20667db1b30a9ae26", EightMd5 = "6cd2610d617836818ddaa29c981f2574",
                         SevenMd5 = "61c68803096d818cbedbe9fb6f9a5087", TestSettingsMd5 = "a284c14fc785fdc726b026781aae9380",
                         KromlechMd5 = "1dc2c91c42e90763b0e91227d376cea7", PowerAttackMd5 = "c49ec2603bb24c49bad5c4cb527b3bcc",
                         SorcerersMd5 = "86f808e14a08f124bce98aa8e520e0c9", SorcerersThreeRanksMd5 = "05e2d688d08a435fdd1731ff6afa8f9f",
                         OrbsMd5 = "61811a1cdae333809031b9dfa9ec63da", OrbsCustomMd5 = "389480d387e565449b2764224d12830c",
                         InfernoMd5 = "6216b3bfc9f549d86acbb8873da60ed4", InfernoCustomMd5 = "442dbf02a88ec27eff170896c4dcf070",
                         EntangleMd5 = "d62a99db815d9d389a0f6b383d7fa69f", EntangleCustomMd5 = "978ea3fb4b0e78e25fe649a71b93f7f4",
                         InsectMd5 = "a9f6e9695d7cc16b755f72717ff37da5", InsectCustomMd5 = "f6c82eb6e43f7c81b397732eb36d27dd",
                         GasMd5 = "c965435f3512ebab25c9fb58c773224c", GasCustomMd5 = "423b9b8c7ff63a5b04849f9b8af05f62",
                         DoorMd5 = "2f39e6f69bbdf89295e96b0609da4375", DoorCustomMd5 = "63babac508f82e9ce4e3453b7a7104c0",
                         FocusMd5 = "f8b2221c5da69ab175be8c0a8f50b0b5", FocusCustomMd5 = "3bc8d754d7d3cc55774a88778bccb35b",
                         AxeMd5 = "6bf0446bb88f1b5128b10ddabd7f146d", AxeCustomMd5 = "356f764ac1353ddd964f5a7e6b91df73",
                         ShatterMd5 = "2d4a67cda3b829a132b2f8974fa05e9a", ShatterCustomMd5 = "2c0e5b08deeb538a16ba476be8998299",
                         AdriannaMd5 = "944923bf7d0eeffe8ea242acae9f7b20";

    /// <summary>
    /// featpatch.py version 22 with Improved Enemy AI (all seven parts): alone (--spells none), with all fifteen (no options), and
    /// its smarter targeting alone (--spells none --ai-parts targeting).
    /// </summary>
    private const string EnemyAiMd5 = "a2db69142c9483b932a0ecdc2e20695c", AllWithEnemyAiMd5 = "582bfd34a7d5988a25f79d0355c9d656",
                         EnemyAiTargetingMd5 = "391cb0bb895e3a82de196d8aa64974a5";

    /// <summary>
    /// featpatch.py --spells xantamorbs --xantamorbs-price 4 --xantamorbs-energy 6.5 --xantamorbs-orbs 2,3,5,7,10
    /// --xantamorbs-dmin 1,5,9,20,0 --xantamorbs-dmax 3,6,30,20,255.
    /// </summary>
    private static readonly BorrowedSpellSettings OrbsCustom =
        new(4, 6.5f, new[] { 1, 5, 9, 20, 0 }, new[] { 3, 6, 30, 20, 255 }, Third: new[] { 2, 3, 5, 7, 10 });

    /// <summary>
    /// featpatch.py --spells infernoarrow --infernoarrow-price 4 --infernoarrow-energy 6.5 --infernoarrow-dmin 1,5,9,20,0
    /// --infernoarrow-dmax 3,6,30,20,255 --infernoarrow-burn 0.1,1,2.5,10,25.5 --infernoarrow-blast 1,40,100,200,255
    /// --infernoarrow-spread 255,1,30,90,120 --infernoarrow-fmin 0,0,1,9,200 --infernoarrow-fmax 0,1,1,20,255.
    /// </summary>
    private static readonly BorrowedSpellSettings InfernoCustom =
        new(4, 6.5f, new[] { 1, 5, 9, 20, 0 }, new[] { 3, 6, 30, 20, 255 },
            More: new IReadOnlyList<int>[]
            {
                new[] { 1, 40, 100, 200, 255 }, new[] { 255, 1, 30, 90, 120 }, new[] { 1, 10, 25, 100, 255 }, new[] { 0, 0, 1, 9, 200 },
                new[] { 0, 1, 1, 20, 255 }
            });

    /// <summary>
    /// featpatch.py --spells entangle --entangle-price 4 --entangle-energy 6.5 --entangle-vines 1,3,5,7,10 --entangle-slow 2,20,50,80,98
    /// --entangle-time 0.1,1,2.5,10,25.5 --entangle-dmin 0,1,9,20,0 --entangle-dmax 0,2,30,20,255.
    /// </summary>
    private static readonly BorrowedSpellSettings EntangleCustom =
        new(4, 6.5f, new[] { 0, 1, 9, 20, 0 }, new[] { 0, 2, 30, 20, 255 }, Third: new[] { 1, 3, 5, 7, 10 },
            More: new IReadOnlyList<int>[] { new[] { 2, 20, 50, 80, 98 }, new[] { 1, 10, 25, 100, 255 } });

    /// <summary>
    /// featpatch.py --spells insectplague --insectplague-price 4 --insectplague-energy 6.5 --insectplague-swarms 1,3,5,7,10
    /// --insectplague-time 0.1,1,2.5,10,25.5 --insectplague-dmin 0,1,9,20,0 --insectplague-dmax 0,2,30,20,255.
    /// </summary>
    private static readonly BorrowedSpellSettings InsectCustom =
        new(4, 6.5f, new[] { 0, 1, 9, 20, 0 }, new[] { 0, 2, 30, 20, 255 }, Third: new[] { 1, 3, 5, 7, 10 },
            More: new IReadOnlyList<int>[] { new[] { 1, 10, 25, 100, 255 } });

    /// <summary>
    /// featpatch.py --spells gasarrow --gasarrow-price 4 --gasarrow-energy 6.5 --gasarrow-dmin 0,1,9,20,0 --gasarrow-dmax
    /// 0,2,30,20,255 --gasarrow-reach 10,30,64,100,255 --gasarrow-time 0.1,1,2.5,10,25.5 --gasarrow-hmin 0,1,5,20,0
    /// --gasarrow-hmax 0,2,9,20,255.
    /// </summary>
    private static readonly BorrowedSpellSettings GasCustom =
        new(4, 6.5f, new[] { 0, 1, 9, 20, 0 }, new[] { 0, 2, 30, 20, 255 },
            More: new IReadOnlyList<int>[]
            {
                new[] { 10, 30, 64, 100, 255 }, new[] { 1, 10, 25, 100, 255 }, new[] { 0, 1, 5, 20, 0 }, new[] { 0, 2, 9, 20, 255 }
            });

    /// <summary>featpatch.py --spells dimensiondoor --dimensiondoor-price 4 --dimensiondoor-energy 6.5 --dimensiondoor-dist 20,50,400.</summary>
    private static readonly BorrowedSpellSettings DoorCustom = new(4, 6.5f, new[] { 20, 50, 400 }, Array.Empty<int>(), RankCount: 3);

    /// <summary>
    /// featpatch.py --spells expertfocus --expertfocus-price 4 --expertfocus-energy 6.5 --expertfocus-pct 1,25,100,150,200
    /// --expertfocus-time 0.1,1,2.5,10,25.5.
    /// </summary>
    private static readonly BorrowedSpellSettings FocusCustom =
        new(4, 6.5f, new[] { 1, 25, 100, 150, 200 }, Array.Empty<int>(), More: new IReadOnlyList<int>[] { new[] { 1, 10, 25, 100, 255 } });

    /// <summary>featpatch.py --spells phaseaxe --phaseaxe-price 4 --phaseaxe-energy 6.5 --phaseaxe-dmin 0,1,9,20,0 --phaseaxe-dmax 0,2,30,20,255.</summary>
    private static readonly BorrowedSpellSettings AxeCustom = new(4, 6.5f, new[] { 0, 1, 9, 20, 0 }, new[] { 0, 2, 30, 20, 255 });

    /// <summary>
    /// featpatch.py --spells shatter --shatter-price 4 --shatter-energy 6.5 --shatter-dmin 0,1,9,20,0 --shatter-dmax 0,2,30,20,255
    /// --shatter-reach 10,30,100,200,255 --shatter-freeze 0.1,1,2.5,10,25.5 --shatter-cooldown 0,1,2.5,10,25.5.
    /// </summary>
    private static readonly BorrowedSpellSettings ShatterCustom =
        new(4, 6.5f, new[] { 0, 1, 9, 20, 0 }, new[] { 0, 2, 30, 20, 255 },
            More: new IReadOnlyList<int>[] { new[] { 10, 30, 100, 200, 255 }, new[] { 1, 10, 25, 100, 255 }, new[] { 0, 10, 25, 100, 255 } });

    /// <summary>
    /// featpatch.py version 22 on ADRIANNA26 (the stock file with three players turned on): all fifteen (--ai off), and all fifteen
    /// with Improved Enemy AI (no options).
    /// </summary>
    private const string ThreePlayersMd5 = "2b899a550132b2c62e89bafbdc7c5bef", ThreePlayersAiMd5 = "7caf06ed8de2839c73c5aa54363ace83";

    /// <summary>
    /// Version 17 on the stock executable (featpatch.py version 17, all fifteen at its tested settings: MD5 below), as the changes it
    /// made: {u32 file offset, u32 length, the bytes} runs, zlib, then base64.
    /// </summary>
    private const string Version17Md5 = "2e1ab067e57ca7577850079cccbc24be";

    private const string Version17RunsBase64 =
        "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"
        + "2A==";

    /// <summary>
    /// Version 21 on the stock executable (featpatch.py version 21 with no options: all fifteen at its tested settings and Improved
    /// Enemy AI with its six parts of then), as runs as <see cref="Version17RunsBase64"/>; and with featpatch.py version 21's choice
    /// of parts (--ai-parts targeting,drow), and that brought up to date keeping them (featpatch.py version 22 --ai-parts
    /// targeting,drow on it).
    /// </summary>
    private const string Version21Md5 = "8a99bd4a01549e1b5716edba76095443", Version21PartsMd5 = "cf5d4c870e13c1eb3d698562f0516ae2",
                         Version21PartsUpdatedMd5 = "d2b3d3193d1364b473dfd631e611b5a5";

    private const string Version21RunsBase64 =
        "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"
        + "ycTi+8eMy77E4v8HmJF9lw==";


    /// <summary>
    /// Version 16 on the stock executable (featpatch.py version 16, all fourteen at its tested settings: MD5 below), as the changes it
    /// made: {u32 file offset, u32 length, the bytes} runs, zlib, then base64.
    /// </summary>
    private const string Version16Md5 = "6c87b6eca521c9779b0debec3325b74b";

    private const string Version16RunsBase64 =
        "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"
        + "8nVgtn/n5nXNG+6EPty4IZ96yn6e1z678227ifYx/Y33rrt74/q7Nm7Y3Dy1h5zneeyDkk9cJ9rftvHulh0btn5h+718MbX9nOfvbX98U9f377ZuW79h"
        + "24Zt25dt3rq1eeryZT9vmMogyZYHveTKz9Ztd25/L/nRn19FfizjD099vGLLXRu2bdlan9+KZj9veC+9ehUsswHD23L35qvqo4nn16KP4vn2Tf5+3bp9"
        + "k4QctEJvr6V5ut7mfZqlj+y5cpyvPZ+OF+1ChFvrR+gFQniCVkorpfty9ZuSK7dT29P09ibtysuwntxID8LEU/SlmiunU9ubn9Vea/1aeku3UzvR4s5c"
        + "O+Wzyk1+/pXr8H9Sn2doB+JxzL9xas1tLzy5jlezD359fLLQMeH6CD4iYNjUjn9BoDm3PV5nuxMWQLcD01TEq16XJnBzvYHnRjDDhdLnpC5j2u6SeYEl"
        + "k5TrQ8d/zMTlr5vMMXG6wn9sX/hPd4SXv3Pwy7c4Zv7sZrYDOXpHN805ukIvE36BvkkmUaMxR922sBlje8VmjMMlJWXzyhlUhBuiq9lPCN4YWhbNsjP6"
        + "+Hho03es/eh/No5v+UwxXTHwxRLVtHPWzSHmp5dnM0l/opdnM2qyfFLoJrhk4CUhPBYr6gMOuAzszbjp8tHgJA43129GUcmsMiv+FvYZ/y9QTH9rEl9e"
        + "65/9hM/AxXPK5orL1ofm/H8Mjts7";


    /// <summary>
    /// Version 15 on the stock executable (featpatch.py version 15, all thirteen at its tested settings: MD5 below), as the changes it
    /// made: {u32 file offset, u32 length, the bytes} runs, zlib, then base64.
    /// </summary>
    private const string Version15Md5 = "eb5676319a7a2f5eb38bcdd265767e22";

    private const string Version15RunsBase64 =
        "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"
        + "nosrnpDW+0r/D6j9E/g=";


    /// <summary>
    /// Version 14 on the stock executable (featpatch.py version 14, all twelve at its tested settings: MD5 below), as the changes it made:
    /// {u32 file offset, u32 length, the bytes} runs, zlib, then base64.
    /// </summary>
    private const string Version14Md5 = "992fba67c99f14fe3267b384fc74f73f";

    private const string Version14RunsBase64 =
        "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"
        + "Uxuv";


    /// <summary>
    /// Version 13 on the stock executable (featpatch.py version 13, all eleven at its tested settings: MD5 below), as the changes it made:
    /// {u32 file offset, u32 length, the bytes} runs, zlib, then base64.
    /// </summary>
    private const string Version13Md5 = "b7edca53a290183aca5eb7d5e799a054";

    private const string Version13RunsBase64 =
        "eNrde2t0FNeV7q7q6lZ1q4HSCzcPO12iJbWEiBssHDmR45JodxpMJooHJxrCJI0BWxgMGkzWkBmv3MYmRHb0guBcPOMfDUg8HEkIjHPlxI4VW5aUGTth"
        + "JiThZtnTmgzLMHmS2EmIx3bdb5+qUndLja9vxp5Z62qt5lSdc+o89/72t/c5aDcRrZaJDPOZKp3mNvQM/6iyR97TJZG74UMf8/gbwm7Shw3SjefkSSre"
        + "d5m8+66Qb9/9vq93V9LmvjDd0xchpeFy3Z7Qb9Z6/Ho4KZM0MqbR7rHdKsWIDl9MItWQltp5ip1XijRg56l2XgCpHky5NOqrCtLRqp+WK/4gFYTuJmO+"
        + "HkzKf/Uiv/dVbSBDI/H3yQ6iNf2lVDtWSskxaQP3tQT9J8cat1BsiAYunqH+i/ufV/xcWz9okPItxT8kK6Hb0YaeNGiIDo2focPjsirFntNkjNkVS94i"
        + "NVBgd7QOz16SbuVvFaLYSKOV/3yAYk2qK/aA5oq5iKyxqMkoacko900kxVKUulgqnuVYL54D1N51nI6f1IMkpzAHPZKQ9Xp8XKWGnq5T/HocfWDtmiV6"
        + "tfmx5PgapAspOBbkPtXdUW5zIdGrJaQ2nP2gWlFP6qCf1C43qcuRDvbINDjR+1C33vxj+RKt2pds8ZHefI+kN9dIB2XfoCr9Y9djZnWFHl8sHZC9g6ap"
        + "hh6T+bv2rpSsHj4kX3OYv71M8X2H8X4F6RGkvWi3R1Yq9XBQDm51+/VIUD5dgfFiHo+apJnm7ZUj9EzVzQ+7VZnkUPJmIkgBRSBTKXrWPE5D5hCdMofp"
        + "tKknk6RTMeTs9sqJ3vbug7K3q6Y5LLWTUcZL2COrXV6iIn72U1Gnh4LF/LxWqt7rouA8fl7srLdkYJ+lPHIm5ZEz6V3JGc8pn4zxuFz2uO7CGvB69ORZ"
        + "l3z7xfPEenctMrzSRlKrMT8xV+6Lv31jBeondkeFzLXujq5tg6y2JaP8Xa9YH6/0YXt9NLTPc5bQNpYYcysfE/PVJMgiy66MuZWPBablLcR7E+S7Dt9V"
        + "EQl59ksru/S4V0IfgynZt3+yvjAUaL5NqqHChktPb6nQ8YyqnXowKB+WPfuPyAX7Jcheaq+6nGVVb0bLqDv7i4o/SluWN1C4SyVvQwjjnY3xKvazhn2S"
        + "ySgS86Pd0Q5ZH6vDHsj2ODrk4Kvcxw//3u1Pye79kJk0ZCYNmUlDZtIqTbrOQr4svckt4/x6eqhDpZov+WkxxvowqZSQXOQO1f8AbWPTRiCH5cC29gUW"
        + "tpHhrpvCNuBawFO8L+jx7gt7fPuCdOJkiB4/+aY3F9+u3JDBt7alFpYU2vKhkTFaSo2jAWoaDdKKUeJ5BndHnwsosSTwQbXlNUmpcaIjY4wve4A5Cp4N"
        + "YEg7sCdJR8YlkX9kvFHkHRnfjW81gSGHLrK8Pof9lr1SbATtykXYV24/B6es9SRKjSl0aMzCosz36z9mrT/jWMRzbuz5FOtLalymqX7GGdPs+uOMbfxd"
        + "g+f5sQevLY3V4Zt6zw/HTLO50kc0n3t6jkoxpjJrTN7SWJNUFmsMumMe4OPvBTZolWG0XYs29SRRjVEsf9jWI+SPI388gvI6u8969AcdHt8NmX1Amxvj"
        + "deE1JKHXyFet/D12Pus4rx/rNeN6gpTQHOxLAx0GzsJuhVMuPZKUJ1IF/ggdq0J+9zzIcHAh42e8H/vdvYZW9oeQtiBdR0a/My5nbt/huWFeTZifYc/t"
        + "amVhOiLm+gLmLpFWaWThItvYIeBhLb339pGoF3veN1ZLY2MPLPXEkvjpkLogPQV1D3dfhzVX6KaqN81SG1u9nXVUvVQjX+gPZo2NLfQCj4uxpZTkF8R4"
        + "gCHPkRJr0tzYYynGNu5GpD8zLZvIGORgTB15YhdMSwb3ShT7V6wBxiBhzkKH2UY0v0I0CVRpoPaOAhsHr6a/lv0Ygv04BftxGvbjCXOEzpgTWEO2I2wf"
        + "JAqE1sJGMDZoVN2lkXewFGkp0gDSANJy2Jv2BbdXMja/7G4cc3D5w09aNkw3BJbJcVr5yFtmYYWLtiyRKVF0yVVdEblSXYH8PuBehZcKq39vJsr8tKrC"
        + "aZMx+px7ZddyCop1XUDAO+DiUhuv0c/YebvP99pWnaHi/U9Syf4gncEe78Gck5pCByrfNPdp1h63d9SRij32Yo+9ls2BvdEjBnhHQr4alqvA8p+bFmYZ"
        + "4BfMKWpGvJKFv0PA31PA39PA3yfS2I809iPdZdI1jMW8z/xdvyIxX6JD31HUuMd6PvWaoo7Ms54/sUFRdz0q0euonXr7mSqSnjVVacjUpFNmQDptBqUn"
        + "zLB0xoxIT5r10pmLDdKTFw3pGxd532Fcu36cxRHO2RyhRw51Ma6T5A45+6MfbIJcqF1B8K5SpCGkAaRhpKujHqxjb1XK07qfUu3RYF8yFoTuN0K3MfKL"
        + "BlINacjOU+w8xsewnafaeQHGN3uPjKw9mkBfdLw9mhhIxkYYq4d2Rx98Elhh79ktWXvG+xUi79I/msEyyO3SP5iWTIFPwq4cGs/Y/AdGQ3jPtu+M6Qtp"
        + "9yjXYR0NE9v60tirtj5KKba3ZfjNjVUjBWDgO09UppFSUU6ekF4/QiPUe5JlZLpsuGzZSEA2UKebx4p1Xfo909qHpJwc5X7Zrn/X7vMgr6uKuavJWIo5"
        + "kbY7ugL4DQchNgL5wjcXsC/dEewF9qW7Din2pbse6bnePd3Mweg6t3+PZ/VYGPPltgtsLp4ct9qrI9HerRaOpIQNWAMbUMPcPgX8h88QEVyb6FO10Hfw"
        + "bcb/1HzG/3bw5/YuUrwNs13VyyvAz/A+2ExJ/Pbk8DqjETYDZRZmJOXjPDeCzFAS6558FXPoBqYNYg7dBlLMoTuKdDUlD/M6JOMquIZXEvwcaRxlFoe2"
        + "27Llb9QDrtRHMbF2ttwIPgm7vWIA/ofnji49xXb0EXmurQOO3SvmOcPmUbQUOFQmFWPOVOchF35iDTB33YD+23vqOQIZaIXPAq55YNTjH/s6xf5d2K0D"
        + "ldDHNPQxDX1MQx/T0Mc09DENfUxDH8ehj+PQx3HW94T0TJWHpJCX1kOWpZCb1guZAlyENpIrdJ7KB8/RosHPNykq5+9A2jJu4cBPwbUDR2QqhWAzlsZh"
        + "C5jvbgFfbKGvdNQkCqX1lBTYxTgRAU7UAycM4ERccvwJb0PyW9UVRNWdzGdbgPlr6Gp89pOdMRsvVrNu2nv4XmKzAb0P0mnod7RrsdDv26f028HjK8Dj"
        + "tVL7HnCkeY6O5/L6B0an8/psHRdjt3WLOb1jd4OSpb+fIs/+T9NMvq7YemxAj1G3mzn3GnLvP2fjPWM81hh7PJTGGqexxmneX96bZRj3fLZvBuOBZbsd"
        + "vP/JXo/Y28uw2QT7XCepiCGoS0GUi1pv8Hb5YB8RU6gDRnfe/rhbcO9FkrpfNwA1kneJCR7iM1ZKyDuM79MXHnOrGuxJvF+mhRLbLUPukVd3KdS5pJFS"
        + "RcmEn8ppFjB+oEJv65cCBd/vrKDJooneg93hgv/duWii3mWaN1VpnuHOxXi+loIlfjoIzArcsICoxDR/XYFvBoyIh4z6AuEj8LhV6jU1OmoG6JgJbDLD"
        + "dMKM0OPmUvYBHnF8AEMmwWl606ibRt006qZRN426aeZ8qKBxvUDByACPe/p4L3m+3zkH473iOd+p0rzr/RifPjIsXXYHvrqRAlUbaV4o0HKeltGWivn0"
        + "BaHLgZYrmff6zNpr/0umHsnSH1/WHv0l9qiZHu74DHTJspW3dbaA1/BzAvNNYL6t4Fdt4Fe7wK+S4Fft4FdCp9oTUq49VTF2tYL3awj+0rCnZJ+Txxzn"
        + "3epPHTjmu9EhTeiQZSMXTbOR2Tr0p+gO+8KsM5dsnXknTpRt917O0hGsXRprl8bapbF2aaxdGmuXTmHvX33e0oVL/yBTr8KO0oNaELg4n3ZrOtK/QFqO"
        + "9K+QLkL6AaRegZ+7tRDSCNIKpDchBRCHmpBye3f80S3axbcNvQv0DuxSZAEFS7PLPm6nenBEJjXj52bv34i7eN+Eu2jfj1GX5VrUNfLXDWKvw57cuvlk"
        + "oh31eqbVo2Rum6232fKDOOIw5dbNiTeibs+wVfcy6l6ZVleeVrftm1Zdjk9eyqrLWCTGoc2cmxGXiW3FpLv6kXlUXbEM+LOAaqTiOK/eiZPl+P72Xyj+"
        + "CX1PCHrMed2UeCga3JAUNpOx6Z/QD8vtVD/B/Gt42e3dx21eQapktf1ynrYVfm59KJrYwjYp04dhxzBab7NjGFlr68RnD2L+Q4jPDk+Lz/JatBVl4hcj"
        + "74P/OULnwFtKYx/lOC38/Bg4jON7zrN5ShmVFmX7nUVUAxvo/dIcO66X7X9aepzxQTM+ZhnHW4XextwU05NBFziuxr7tB4iKrfhHEex66WC+mDDHgTku"
        + "7MSBW8Cbiuw4sAYumBJ+QntXr/ATZsZre9/CGtYHZY6ZNpN6+JN0zeHn8sRAJ4U9GYI9OQV78qyNqSm6BnLRPGDFbrndpZBlz4TiP6v1hdiv8U/5Nd7O"
        + "AtjMKPkQ3zXKGuAbv2X6G0yzY3moZSsI4eIBjkM2kFLJv46g4odeiPjljUvAK8PM6cPL+B3jgK06BVv1bJpjmEGMc/5TFhdzbEjdKzLVuxhbrDhZGLLF"
        + "clZPO3oMuq8nTq09zbS5Z6HvcchVHHK1sk8fZj6REGvuxMRYFgN0YMzJp+PJaCG2NkI/GmP75AJ2u8i7xGd4KWmoiOV6G87+hmPSYViw5dj/G19w4kSN"
        + "9loYcY1Mc1XlooM1kuEBp2vZItdRx9f0xFck2ugnF35x6j/54vOCT0th+s6YLPztBxADo1gz/IKjKIvDb3HKSu2yNShroTNTsaNyyMJqxI5I+2o0pSVj"
        + "yY9asdwHIINBenGsqaTMirm17Y6G6PtjMmJRli/ljdJqL7nwYw7eDHmJ09FuvX4S8YWkzPya7Qna7hbx++ut+NM6WtnhtmMHfvBXgEhRkrgsPPBtO4aU"
        + "yJKrIK3f+xNTD9VRUOPnP+D5etiAbJuwGGXCR5riNIchg0fAaXrBafrAaY6C0xwz4aOYBvhNHPymGDDUjHfmDS1Iee9n4Hyccb5mXyXdc7QHKbDlaNLj"
        + "66yle+AvHU+XQOTwbbqIRU/wo8OQuSPgR73gR33gR0fBj46l0S840ok0+oXdtGIGtbR+zJkDKXrkDfO9mUNeWxFnW2HN4xJZ8/hTx3+W/VjMoafARQnZ"
        + "ijVHMF4Nuh+A7gedMw/ovt4zApELA5sYY8LgsowxHDO3MMaKVWb8VMaKszZWwAEUZz2SfdZTTvOwJzdVWTEtfTz7rEE3iiXEbcT5TlR8m//sRsS94lOx"
        + "r766Jn8D+E61hxKCV11u9Dd48C7b7214X02rBx7r/QowfVbDrLsGlgda7pW2Ncxq+NxrA8t1PKPspLy1EPvw8b477qiucOJmF+24GcfS277g2CL4Glhn"
        + "rBXW+BTW2Ir1M392ZA48r3PS5avw2DoSp9teKaaNRSbOsd7A2jejTR0cre2bWyp8VCjq6OQHf+iokMlvvbckJd1ISIypv8Q3beSr6PqlxZMm6/2hQKKf"
        + "ajCf5EPg5q39UkQafCW0a1Qqp8my4RuGK+aAi2O5vmyaLvi2KzT2K/7DDF7TDJ7u4CfHWvVAE+RmQ4euyshb1K9QUyhMK8J/NBOlVrx7xZfC5DpWCz3O"
        + "VzeaVTdOK16Jk8usaBvl8bzl9GOUuWg0D9dfZXP9eBbXvzWL6/dAJhOQyVbIZBtkche4fjKL61t4H+qKA++bgff/Vdy+9J24vQG/Xn6o/93y859n8XPM"
        + "N435gqOfAkc/DY7+BLAml597BCefilloiC2FfEi57EM3KzO4toNJc+zznGy8es0uWwb5ZpseWLUnVPwzxd9W1Bdaare1xE4X2Gmb7iIDe3k2x/fsA24M"
        + "ATdOmeWkge9plXo7kZtqanz23lJjEY3Ei0Q8STdUWYWOJZvx3uyTZxPj0Sroiy9UQDXFlj6AczwF3CNfQ+tTqyrc9vg5P/ItKz/+rVUVYZx7sU/7tZ8L"
        + "/iB5yShFXjefAXIelynIs3iDVQ/jTcvMn+Icf1vZkTR4XKp8lrwDEi2tLKBwDTCyzFovDTKlIQ5vxQZwhlTktJMgd6WKtvmd14/zLp/oC7nsPKcvxmWs"
        + "D7DilMCKCPYzEHFRA9YxhXUMYu3CWLsIZLwedr2BEDemb1yM2xyMOdZHsmLH9eLsIEUOHjGHqKPQXucMV3ZLsaPT8pqQF6bi0GPQwXngL7Puql4ewF5s"
        + "a/ACB6uX876g7KQrpBK3txAxVfY5ZfsMgLE66S4V7aKs2ykrtcugyvh946Ii7gzQ2OWmzHmyijyc1cF+pGA7UicXQ97af8V+BOvY0ap/vsXtp0Qyynz0"
        + "x6Yx14rDDcF2nYLtOg3bhdgdIXZH3xiP2+cyzC96bnbROk+2LCIWj3UMYB2DwIp/edgt+N1cyMz9i72dBxb7OuV14DsNfvipW8vkdT66J+QP3Yhn5kG0"
        + "xkcu/HRwNmffsd5dSSquDNrxN6AcbKB3yTxgu96ckNW5zO0KJY7TRYHht/5Zx3K9jegWmgWK9QncBfhFkRWr/l7Ff5gvlbVAzn22LKvUUjkXOA3t7GZ8"
        + "Rv0BBfjMOK2gHpjXgGkurZwERiQQj3O4gI9UW0e8sBnVsBle690ISuChkoox/wDfaBi3xQkQE8VaYl0gg09MySDRio5rKSHGEi+6TswuuEC4NWQ8Yk23"
        + "5YKVJpdY1wra3rhWpIkfWmpdb9drt99T/9N632Wn8Z9Y+Wf/zXq/tM9KR+z3YTs17P4v/4vVz/mfWvltk9b7QWd8v7f7v5A7vonZVvlZuzxo5wfsVLNT"
        + "1U4n7X7P22nb8QXWuOx0evuJMis/br//v7bvzH/i3/LPv/6E1X77ifz9G+6S963/IYFBzwKDhoBBp4BBp6dw5/os3KnNOrPK5qgan1vj7Krtm5DRHuvs"
        + "iuyzK8U+u2K+6Pgr8+2YP+v/FYrtTxoeWKoJukwf23+wHZjGHA++rTjzWGBxyR453LUGvquGM4o7kJIU3/8ppCrSTyNlbtGCehw3aC/KcDXMCzgyBBw5"
        + "BRyxuFoz38uAHex5KjQq60rM4F+1O5bU3eL8WZ9IUQkFcF8pIGIBi+01qCIlJxawyI4F6LmxgNFpsYDR3FiAEoNhuVVvwYEIbe2cRcki6xyoEzHV4bIe"
        + "uROYkbLXfKDTRceFHbLua7ljvxVxgy0VfG7txBT43FrXgq5zdkzB4V3vJa/P5vHZfD07NsB8n+/TyPYdGsQBBvl8CnmD+e4YXZXjczzfjjHU/21f6Gp8"
        + "m/Fv6Knqioneh7s1KgSO1nbOpr1lKi3prGl7XCqkEdznWdI5WT87FGgdxJnKbJypfA98eRCWd2BqnUFsJD2SkkxzS8WvBOevrlCFPwU9WueieollvqOD"
        + "4/hL88Txnbj4LLoSmYU4vUTDYcQPrnHi5KodJ78i4uTzQotG6l2BkWHwrkDVfJrnKkbc4zzamI82Uu4rEdjPEj4TUMWZwDyXj266voSC1/jp151FqKMW"
        + "nB+Yyb+OYn+PmXpwUnLWLvKRvlAm5t+H9TuK9TuWZux/+6DlS7C/qVAmLscxIrZvc9inR6wjM8/jkh4/LKme6n2Bgu8OiDX3fL2T7V644B8G2Edq+UIm"
        + "Xuf4/oHPuehpKROjEXFANfcukxXbLdn3tJfjNLcjTvPnfc44yu1x6MMT8oTn33FXwok7RKA7VuyM5fGs+7tdzGFYJnHH4DDuGBye7rc5HJhxaAQ4VGjH"
        + "0CwsSsoXnkFMyz3W5Xw/4blQKTBEyCdNzSlnLlnxUx1RJ2fcVVnjvuL57SB5ftB1CeMvyhr/7Kzx6/ES6It3EJR0LG5zJ+ZXVpzGF13U7MPcfIOzsubQ"
        + "jDnMmjaHmgcL/BzzuzRt7LwW4Tz7s0gK7/fY+DYCHXXO51hH8f4qzr3UK6+4xKWZZvg6H8yK9S2257JWqoGPaAgMjMs1+/6CvtKzOHGbJNvYFpcXH2J5"
        + "WSfORm+TrrPPRlm2Iqv7O/XEamkddfRPZsl0Cz3cc7W7fWtpb89n6Ms9uH8nvdPdPkf2+VyL44c41+6ve9Pyo64BLVSs89/S6T6TbMeicLct9He3WH4P"
        + "+1lvmrl12Yf6g+1D/SuvK+4DvgYMHqFFiInObNdtt/vZOxVRl9tH3cEDN7+7PupeVHL6WIf9AFYAr07gDkbNUjifuIOxZGkJMKoBa833gfQE79VW3DlS"
        + "Q6jf/7rE8ZX2/jW0t6OFvtzBfgTkQbqKj6oa0pbljj0J/NRFl6frctY5hYEjRt2YkCfdxY84enBtlh6ch/zPypJ/X7b82/ZkOvZPl3fvNHl/nNcEfvb5"
        + "q+hqBic5pn0aOPmEqbdP4v7mbztaERePy6/1dMjLBsX9uvlWfLOZvPtxry71SZzHl9Mc+LO/rpigK516y89wJWUr5Ht1anHrVtmXuFeOy7WHh5C3FnnD"
        + "SD+D9DjSZKuf9EQ/7k50deuJFyXEoPr1kfOQ85Z+Sy45tn0amJzh42ft+LsmbDV8v3ZITTKFe1ng/TtFfBUxy3DWXWNv51SMSejd4v0y3VZmvQcPNVMo"
        + "K8YezIqxa8ug88XMjTiffQOOt3N8ncfhvDvjCv7WLe49PPmE4g+97haGUWl43co88HW36v+d9Ty41qNe+aBH7GoFPVh7v5zQ5pLckAwoFNkW+aJHHPlu"
        + "DM6ly+G5tLNqLq0JW8/Tf5GXikXaE5pZVou84Y+X0MIPZPLuXzSXXg/mbyv7F/gEiN0/l9AFpPXPa1T7TDFp1Rq909HH2ZyjD6Uh+dmMSWGKnH3F+r06"
        + "2mBXbwjhD3H1BmHT3qfYPbypW5+4Ca4mSeb/mI1jle8tt2iPH6q0B2GJOZ01I7/CNUQSvoLeNmnVBa2xrgDy0R4HbxS4me4B7gv0ZODCXg/TS6o9p7z5"
        + "sEdsZPExxT/8tEdE7Rzm84GrMJ95YCSWNP66c679HKfJFxTPoRfYgomTB5v1TeZ46mfEzbhWqRSRhNLY021ufyv+9wDeX814rWfEbR1eIpRbESks4Ztm"
        + "89xsr/Dlbxe/L17h/81ru2T3P7ptbt7+m1+yvNfLv3nv+3dOVTz2qcov8OyyLUz7F6y1KrLT2l9aW7z2Tk8UjxBBWjf69JkCkXnjt5UDL1iPH+pTan9X"
        + "IETgm4OKGn3Dev4nSPuZrOeFK1RRfeM/KsEaL/1CtiIjPW9YvmvbD0ryrsaVl6z8g79b8N/iQ2vHit83H/pq2L2M45VPWtjNPmzu/8cIVfIvG6NfHP7T"
        + "MFqse+V8a1x2On3+ga/a7+Xz35f1n3jQ0obAnuvy9h//mJWfeuC6nP7bv1/yn+7fucPF97RIcSO0uiJcqG1AvHwFmAli5Li1Uo7IX/uCDTjHuJPIjiPy"
        + "fYWcbwXvm/ltdv3hv/HSaiAj338pAM/L5niF0ECLV5J2vWv3Yb4L48mqw3yO+SVzPS7/tK2hzhhetP/PlxPvdvKP2vncTu1qfVQWMdfMmGqQf/4v9bpF"
        + "aiPub+u1LvuOjWVQoh3Z5XyXo3414q/B3Xb5ng5GZjZ82J2TiOGehJ0Q9zc5dsuxWY2+FPosvuu5c0+I47Xwl12a3T+jcwbZ+0zwMnGmwGcJfKbAXA35"
        + "aWEJ8H+Gzn63wG8hRub7rY96EQMReVM+bTl44GW9L8RxdCsGMjOu7nx/f1Uh+UQcZVHXbKmpLkn3v1UoQOo7az0H7MfxtZ6zZqHgLwkFBovzTr2u9Exo"
        + "IsASu7XxhllkOf4SZf42b7tv04adbVvX3/35TZ/btv7eTZT7l1O+cdN9G6aVk9uf9SLl/Y7y/FXQRtSWxDD5JPNx6sB5HMLUUkDC/QAzt7ZEmzds3/a5"
        + "7EY/uHPTLj7DOxGxq8guxc1mif/4nf94YflP9LECbtGKeeJMSGiqoUj0/80f9rcu3/62bd983/Ztd6+/b/2OHdv/euYOTyufucM5+ytf5buZ46kU41DJ"
        + "+jrY+M6jl639zW1U7LDh7K/q9RVyY/7Zc4oYGsNLln2oISuVZZeLbbhLUdweZ48zf+2LiwnXSnmd1syy87zizJFEfs+SEjKR/t2jj94i/psFjWDMZ1ZQ"
        + "8iMch2yCHLnd7iwxlySZT4qJxc6TpS88j13rt+1cf+9dm3c4UjpTQzJV8mldTnk+rcO+aM6TQnl2qG37X2/asX7nzvUbtuTtILs8bwdTk83twGn/zq3r"
        + "N266E/u0edOOfB3klOfFDV/mWZ3ZPqa/+d71d2/ecNfmTVs3zuxhWnkeuc1qVJvZ/o7Nd7fu3LT98/fdyw8z259W/s56EZy5v3++fceGTTs27bivaev2"
        + "7Rtnbl9ueXTmAkmuPKg6XX6277jzvneSH1F+FfnJGr8xs3jltrs27di2vTG/dueWR/PKj3/m4LP+NmF42+7eehXpzyrPP/rc8Sfz6U3+fn2wOy4MSrbt"
        + "jklriRnb2xLraY7dUabLcb72gsKOKbYItzVO0EsEF49WSauk+6VpdkydLrcz29NFe1OtN75MXyNupFdSpLent6dNl9OZ7ZXntNfWuJbewIyxTmhx13Q7"
        + "G8yWm/zrt0hwlQyum7RTcJu3pUvUNr09I7OPV0P+kBifbGOM0Rijl4nhvAPhlI3T2+N9VjyqjztXfbM0sMDXpSl73mjZmQnMsFb6rNRjTdtXel14WSbl"
        + "+sD4jzp84XVncRz+oPIf2xn+E8x+xVsHv3iLu+RHN7MdmIY7rZ/fOAMrRJ7NV4SSZKyZNUdhW3z+2RrbsTnFZQF26UrnXbeICZARja9m/hK5saEpnmNn"
        + "xPh4aLN3rv3IfzW/aP0M/BCLZyyb5di5bOWw5yfycxdJlIj83IXK5GeEbmqVRL4jPFlWNPh4hC6DE+zi9Hgkww+c/ZtTVDp3XjYvsO0z/i1QHR5IxrJ3"
        + "OW/lVFBw1MnVJQvtx+H+uf8H/Njmcg==";

    /// <summary>
    /// Version 12 on the stock executable (featpatch.py version 12, all ten at its tested settings: MD5 below), as the changes it made:
    /// {u32 file offset, u32 length, the bytes} runs, zlib, then base64.
    /// </summary>
    private const string Version12Md5 = "fcf340f0fbcaf9c680ac8ac178cf2108";

    private const string Version12RunsBase64 =
        "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";

    /// <summary>
    /// Version 11 on the stock executable (featpatch.py version 11, all nine at its tested settings: MD5 below), as the changes it made:
    /// {u32 file offset, u32 length, the bytes} runs, zlib, then base64.
    /// </summary>
    private const string Version11Md5 = "35b1b1ba5fcd4508a1f6b5577fba8acf";

    private const string Version11RunsBase64 =
        "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";

    /// <summary>
    /// Version 10 on the stock executable (featpatch.py version 10, all eight at its tested settings: MD5 below), as the changes it made:
    /// {u32 file offset, u32 length, the bytes} runs, zlib, then base64.
    /// </summary>
    private const string Version10Md5 = "944c654a77454ffca092eebabe1ce05f";

    private const string Version10RunsBase64 =
        "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"
        + "+tP6Z1nfN/mszJTvD7a0rF3fsr7l7rpNW7asmym+qf3RmQziXDPWnKk/W1puv/ur9Mfqv4j+ZJ1fn9m9fPMd61s2b6ltadnyY6Kv7o/Oqj++mYefotc0"
        + "68dLeNuCSbacmmtNuoWYu/mSa7UqaVlDhel6Ntt6CtZjb2slZ71xeoPOMBDjVnD3cVPWs/Rwql7NXE+11ptYvfZdesLCjV5O4L6cvp48XY9mrlc4Zb3m"
        + "2lvoM9wYfMKKrTRtPSVbrrPzr8hyDBL5nPOZtBUoyPh3jpqnr8fkLrglLxsseefJAfu/DlfZbr6WzYe7xokquFu5HvuY3sDloasmKRsPzPwucQwI7d+2"
        + "3O3fJLEPi4bZxwojln2x+/6lYu7b1zNctfAyW4gOYlrtU4Vh9VjtU5k62T7JnAncnYYTTfesm2HbVps1f4Ot1I1MqSdW0GstX+D1zZeZc1vgzwuy+CxQ"
        + "cHlRiHVH4yvrWSHqmkhdfIpfsO7Prj5/6y3XzdDLg2E6fyBMrYz2h+mv+xGOKJarDF0pL3IeT73p/3//A80X";

    /// <summary>
    /// Version 9 on the stock executable (featpatch.py version 9, all seven at its tested settings: MD5 below), as the changes it made:
    /// {u32 file offset, u32 length, the bytes} runs, zlib, then base64.
    /// </summary>
    private const string Version9Md5 = "8c794aa8d5341ab7fa971605fcc03bfe";

    private const string Version9RunsBase64 =
        "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";

    /// <summary>
    /// Version 8 on the stock executable (featpatch.py version 8, all seven at its tested settings: MD5 below), as the changes it made:
    /// {u32 file offset, u32 length, the bytes} runs, zlib, then base64.
    /// </summary>
    private const string Version8Md5 = "bb70e0ec22b3063c64804fc403778a6d";

    private const string Version8RunsBase64 =
        "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";

    /// <summary>
    /// Version 7 on the stock executable (featpatch.py version 7, all six at its tested settings: MD5 below), as the changes it made:
    /// {u32 file offset, u32 length, the bytes} runs, zlib, then base64.
    /// </summary>
    private const string Version7Md5 = "5d6952d30c679fdcb8e7b735f854ae93";

    private const string Version7RunsBase64 =
        "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";


    /// <summary>
    /// Version 6 on the stock executable (featpatch.py version 6, all five at its tested settings: MD5 below), as the changes it made:
    /// {u32 file offset, u32 length, the bytes} runs, zlib, then base64.
    /// </summary>
    private const string Version6Md5 = "1c131fd0e186959692c371b61291b66f";

    private const string Version6RunsBase64 =
        "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";

    /// <summary>
    /// Version 5 on the stock executable (featpatch.py version 5, all four at its tested settings: MD5 below), as the changes it made:
    /// {u32 file offset, u32 length, the bytes} runs, zlib, then base64.
    /// </summary>
    private const string Version5Md5 = "64c66cca454d8dbf6e3883b7db97dd76";

    private const string Version5RunsBase64 =
        "eNq1WW1wXNV5fu7HyndXK7hayWJlnORee1dayXJYORKVyxKv5GVnMU7RuCajYRxYf0aubaIaMqNMPZ1rcKht9OUEGGj5saWSjRl92YhESUhQElsW"
        + "GULcZiZxp6GrtgymCWmUNk0cAtw+5+5dabWSM/xw78yd9+45577nnOd93ue8V9I3AmkZiOF4zwpgJXi14ESPhvov+7Gu1zROQkNaUuAJRe1v12Yw"
        + "br+Ac/Y4ztuTeMmewoQ9g1dt07LgR3mvhGDofhgB04Cso65Ph3e0kraSNkgbpK1AIDawflvNahjTO5TWaUBKrgbe/tOXVb94z4wDZjvkFO5+6gO7"
        + "NKzgwHoZ6fJ3lLpw9FpdmO1DErSwF6V1v7XTK/3YEs77nBk83r9dubuvGUa52Mut8Ep6O9CIuLM3zjPd4c6pQ5o+qiEJPH/VotVpK9021W2rpA26"
        + "bZrbFqQ1jYyiY6jWwOnaf1+j+g2sCE0g8JWXUfEVAxO1wDHu2dJVPFnzvn1KF3P7iXETtEZiEvqd7XXWY0YM2YzGZbMlLXNPsczjWrOZcvYPGaWx"
        + "m/5a9Ws40PwLG46POLS+mcET/fVTXklgxXhkGY8s45FlPLKMR5bxyPbZuOUyvl2rYVaBez1T/nHH9rzhuEL6rYBjO1w7c1Ou//JvP+ZYw20PulZ3"
        + "reba2f/I2SuuvezaGddOuXaS1roTsMizDDlUgSrG6ic1TWOqf0A+1teCwwNxPDyQQudAO/YPrPa92F+D1FCEcTYnARVpx9ELu1S/g9lkHEE8OZ1v"
        + "xwtWopTLi+In0yL+CjFW4F3vi3thxTWsgzd2+dd1YTMVkYDmizpuv2haQH08ILcCDk/iKR22vaVm7TP1UrykNBTsOCA3oedpM/2EhD1+KLxTGB57"
        + "/fuCo5YUwXenZYdDj04zMsl2ZK6eZl8Kg/35vkq3bzv7OjBxVf2W6k9DDa1BXN+K58egfzWR0a2k9WkpBu1o4lFdShp4fbqtYmXS2sS2rqOJEH40"
        + "LetVyRAET70JbPVC4W0acbSTfymc7jdbZpkzlvzkhRK/4A199wfIpfEntHALUsM7cHePx80HP+p6ZT5bEH2Rke+QVxL0mjS5cudJjyZDDl1mjMCc"
        + "1jBok+N2kDlvMOcroDNueo15nHi1lsOMazLHZJ/+BeOoeMI+xCuteK79gurpFe2iX2W7wxt3LH1mZWIgeN7CteXfmVS9IxIaaxRE6q/acPNV62Ne"
        + "NPoYp/z7aXhqNPoUvxvhiYm2ubNDIcVty8/BPWS59izXnhV5EOUeW34MDDAjprjHKPeoc29B7s2gngkNM49nYA5MMWyRPhJrrJJWpQ3SarSOPkUy"
        + "ihkFPttALrQAFcS6/RWBpzYaA0b9uTU301LrqonZxtqc1pmX8loneGrGAxLzedS2tVDCeVetEXpgHPT4zaghnw8X6GFqXhOHmtr8MYXaV4K0g9Fc"
        + "qz9Wwt+y+7uLv7di68hzg0/0V6MsVrZvpDnYcUh6KFYWe/B/RppNPrNvTD5YSg59Zui+++rCeT296uppFGqs60vHQqkzXEMEMrHKEivieY54nnfw"
        + "jBNPsQ8f9WyV0FmucwvPkHac7Ekh7vLtnt67qN2O3hDzAWKeJuadxLyLmHfzDLF4huSwT0s5TQj1pagJ7dQEge3Aei0sdPuj6nUTcPGjaLZOzV5O"
        + "qwt1+prQ6bg3Zsknhv+YViuuVqcLtFrEjvvNcr9Z7jfL/Wa53yz3m80Qu7e/X6I5ujmfb+Pk4zny8Tz5+JL9ryc9jt5Vca4j67y9T67z9co7mP8x"
        + "f6gBB1fKO3z4i5A/VMdnoQvY7oPC26SG5XOVZ3KfhUCNwTM5dwqkyGfv+mpqgNmelrUqoXWlktl+j5SAP3bXn/U0m13AJpRRcu7tLcG7TuwkvBH+"
        + "g/3DlR3kBnNRz+VzR00Vqjnu5n7b3sg93DuiIlj7B9u4ReU4KtGIbTfWzBKPNLbViHzkHsmjc+TRefLopfm8bH8TeFfKnQ8GcYgQhyhxaKFuxvDy"
        + "1Ti+djXl1hkUy747XN0WNUeLU29kkOeq0OgmhB43WnO6Knuk5Omitja2RRAIPUf+VvN8KNtX1xyMe5kjXuZIXbPJZ/aNKSENwt9qDI6tJqdkt24Q"
        + "eWx5Kh2/7OvP91W6fUwD3l+7qlL7g+TuXFtuXtGnsU1HhtqSoa5kxtYx147/17GQ0BWd3PynTYx72krY9raan9rxKucsJm7EJEtMssTkEjG5REwu"
        + "pRxMZxWh37MOj8bJo3Pk0avzeqaTP3P1OX0SnBSauf1Tqv+yPhQSWPrnsfT2riAvEvD1eVgvxVj/fWD7Y7bd0xzqOMgNrBvZR20SOiXuHkP1y6wL"
        + "hc/b1zvYkyeRDeI318F1nWOsX81SbssNrm/VN1Qtx5tcTdL9rIQGjvTiMd2AFPLiqG7SBmjX0NbTinH3/d7jvPcZ15rGlAzN07SZWhGLeJDXCDMu"
        + "S3ElcCqllJ/6Kcc2Etc5YuKM15eOj6dk6qpXSit1T1WjLrwBvtCtqJcCKTHL2bEKvr/tXdVvNYrYxEVbP9InEsZuK4lOK6FzT//IeYSuzM9jLL+u"
        + "LsV7SvjsplULfGeW8a2K584TifQBK1k4R5xzjDOuDZiiFh5dRguPLlO7Hv1ItSswyHpqaLoB09OPNpYkLd6mLnj3DXIq0s/KsFzFxtr37cp5rjSh"
        + "jhrpYy1b75w7HH5RrEty8kC+6KyHNc33oCbbdE9yKsi80KzE7bQ/t6W7xDsMf/JRXeY+5GQTSpJv2XDaH5eQ/DfqBtcgjbscnyIW438nYVjKKVnX"
        + "C7fm6kzXFtez6ZW59pT7+0bWs8530tmc/+Nnl5/fuqD/v82fr6FF/VzI7XicnI7PyGkl8NSAfNzR+o+Jeo91sDk5I8+q/9lXNl8LRnup5Y7TwnpE"
        + "5G/U1YiWvxoKlbl6L+rXdtavXlG7WXGnBjUjlmwPMvd5Ls6qb9UI/c3VL5Dz9WTwH2SoMubzgedqL2RfuMRdRwr3vOnBnnLbrgu/x5i38+zo+2Uu"
        + "12db/KFgehj1rGOsEyNhs3NYikqjb4a6L0hrMLty8lOT4Zt5BnHJf2PbSsiDzbo4i8QZ1M6zKK81gstmsI3M2d1jajLb1g6raAtFsDnyezvt1Kd8"
        + "/nIEypkGrnm5sYmCsSlsfjMFxQ53XRDr+SA/z42sKwvryML4FOq5qDdj9CHzDAu1e3mG+0bDPLvYNipqzvx5l687r1tjFsX8evVePaTYlc+ZTWu1"
        + "Vq7PbFBgVC5oeqKnsF9oXGoD9c046vYf60kNywgS2QG5p0dF7/pGZMqttJ9jy8gNxrdrWJpTf9R7M2bLy3AtSu5VSJiM8PvuFniu9Gqovk2DUWFO"
        + "TUrdSvCrexj7tVMtSnBqEhsY71WoVgI8h2bpYxV9ZDzXojx/K2YGn+nXPf/cu3amWvFh420VMG7x41e95Ryje66MLP3mOc3YnbFNY1bK4xK9YyiU"
        + "q2EGs+L7gmOIzZmsqF8+fMbl67yfIZu5wTWpscDPVX9wi8CB0eN7zrdd+mji8msr/Dkly+mbOCP1r8vYIWFxfhecdWb8e3LujKs49YpXfCdv43fy"
        + "nw/lc31NQa5fZq7rBbleVpDr7cprfaLGEXyaQOD5l1HxfHHNf3NB3k8x70uL8v6NKPVAme7Lv3/5Ovl/vbqk8LspV5eoMeuBhflFbaK4tcmNrDve"
        + "+cEyGBec2+ZAHHk8awvwhOe/Ry3lx31zxLW8ANebCjU0VcEc9I6yhJhOuTWfqAtz3+++xNp2HzH3jRZralkRtvWPrSCGaXmuCFMRo8h8jKx5jPP5"
        + "9Inr5FM1eZ5b7696q9znFGYvHlP+/qJYn8NJVydmF32PTFwV8eqUKlnfViZf6fL4OyW8zd9vL9TzE5fy/GW/g/W212V81jmn33H/6vSzM4FlT0m9"
        + "LndKzv36xv/Vac7dybYXWVGrnphEBS/Vd6OE1sMK06evJVIKkdpNxd81n4mi6lr0rvMFtPTdwvGs4uQBeWufiMLKP6JqeRXzuyrW5ahYsFYoWbDj"
        + "ChXjQHgVviQFeBIEO64t/G5ZYPDAnQp+R3STd7Vu97h/3VPd22DDBdZhzF787bPPbnL+aIipVmBiM6w7RHXVBkXxePIviihJkizOOciK6inB/LV/"
        + "9xceerB750OP7Dy0b//hvZ98ZG83Ci+3EMPCkAcf2nloL4qvgv49ex/evaQfHh0F25i/ZNfuOrhzz95dOw8f3r/38HIzLOpfbgaPb+FZW+qfy9t/"
        + "aOfn9+/et3/vwT1LZyjqXzqDp8CpvtT/4f2f73xk7xe++PAh8bDUf1H/Mv79C88GBjYpIqYFsGLZy4c9UBik3DK6Wm3cD5GMH0oijBn7X+jnhOxi"
        + "Xojhcs4MOpM4VHOdzeCHoARgi7RFOiJl7EWDtWLMlvozHX/z3lt/hqchnAxKqvRhsT+9GKOl/tYs8tfVej/e43YJEj12o8ifIZhWovkErJqvTGcN"
        + "8xu+vMHpzLSK9VBR6KFBekAayLn1VX48smHBivHMr08vypVCEIuyxelfDMqiEU7/4k0u6i/iYOcXizAt4tCS/j1GFeYiVXiktgrbI7nn4ttarzt2"
        + "ILS0r8FtW/2JhbYja6vwG2N5X4W3Tu0f7ynHO98MLA7Di1HMnY2im1ZcB9sUJHBjrmufLIHX4cVjDUfktF7hva3EIUjj+ydLHMrfy/M+8csSJz1e"
        + "K31lYgWEBE4Pqg3/u8IZ8MAuVUu8l3v+kyHVP1HwvHqz5gz/y0b1SG0pfM7/zdb23SS1NVlQzxlOZ+q5wGr3ceJbgf8DJ6YW9Q==";

    /// <summary>
    /// The words that send the game to the spells (all fifteen on: Dimension Door and Phase Axe need none of their own), and the
    /// stretches of the pool the patch takes (the same with any of them, or Improved Enemy AI alone).
    /// </summary>
    private static readonly int[] Hooks =
    {
        0x0010F4AC, 0x0010F648, 0x001109C0, 0x0011B758, 0x0011B7D8, 0x0015E220, 0x0015E424, 0x0015E43C, 0x0015E50C, 0x0015F160,
        0x0015F278, 0x0015F354, 0x00161EF8, 0x00161F00, 0x00161F08, 0x001676E4, 0x00167AFC, 0x0016DD2C, 0x0016DD44, 0x0017A4BC,
        0x0017B594, 0x0017E52C, 0x0017EA44, 0x0017EAB4, 0x0018331C, 0x001B177C, 0x001B1784, 0x001BED7C, 0x001BED94, 0x001BEFD0,
        0x001BF004, 0x0030A104, 0x0030A11C
    };

    /// <summary>Xantam's Fire's two (its maker's trail vtable); none of Kromlech's spells needs them.</summary>
    private static readonly int[] XantamOnly = { 0x001B177C, 0x001B1784 };

    /// <summary>Sorcerer's Blood's two: a level gained (FUN_00167390's call in the level-up) and the maximum mana (Willpower's rank there).</summary>
    private static readonly int[] SorcerersOnly = { 0x001676E4, 0x0017B594 };

    /// <summary>
    /// Xantam's Orbs alone: the six every spell needs (the level-up lists, the grant, the icons' loading, the cast table's three) and the
    /// HUD's three it shares with Kromlech's spells; nothing of its own. Dimension Door alone and Phase Axe alone take the same nine.
    /// </summary>
    private static readonly int[] OrbsHooks =
    {
        0x00161EF8, 0x00161F00, 0x00161F08, 0x0016DD44, 0x0017A4BC, 0x0017E52C, 0x0017EA44, 0x0017EAB4, 0x0018331C
    };

    /// <summary>
    /// Inferno Arrow's seven: the spell start's mana and the arrow types and kinds, the arrow's constructor, update and touch, and
    /// icons[9]'s lookup. With the six every spell needs and the HUD's three, the sixteen it takes alone. Four of them (ArrowShared)
    /// Poison Gas Arrow needs too.
    /// </summary>
    private static readonly int[] InfernoOnly = { 0x0010F4AC, 0x0010F648, 0x001109C0, 0x0015E220, 0x0015E424, 0x0015E50C, 0x0016DD2C };

    /// <summary>
    /// Entangle's four, in the game's VineSeek update (nothing else runs it): its step (cos and sin, scaled to 200 a second), the
    /// vine's allocation (0x60) and the call of the VineSpell constructor (EN_VINE). With the six every spell needs and the HUD's three,
    /// the thirteen it takes alone.
    /// </summary>
    private static readonly int[] EntangleOnly = { 0x001BED7C, 0x001BED94, 0x001BEFD0, 0x001BF004 };

    /// <summary>
    /// Insect Plague's one: the poison's bite in the status effects (FUN_0011B3C8), so a bite of a monster its swarm poisoned is
    /// his. With the six every spell needs and the HUD's three, the ten it takes alone.
    /// </summary>
    private static readonly int[] InsectOnly = { 0x0011B758 };

    /// <summary>The four arrow hooks Inferno Arrow and Poison Gas Arrow share: the spell start's slot, the kinds, the arrow's constructor and update.</summary>
    private static readonly int[] ArrowShared = { 0x0010F4AC, 0x0010F648, 0x0015E220, 0x0015E50C };

    /// <summary>
    /// Poison Gas Arrow's own one: the arrow types' last test (type 38 an arrow). With the four arrow hooks it shares with Inferno
    /// Arrow, the poison's bite it shares with Insect Plague, the six every spell needs and the HUD's three, the fifteen it takes
    /// alone.
    /// </summary>
    private static readonly int[] GasOnly = { 0x0015E43C };

    /// <summary>
    /// Expert Focus's own three: the shot's three calls of the arrow constructor (a single arrow, a multishot's first and its
    /// further ones). With the arrows' update (0x0010F648, which Inferno Arrow and Poison Gas Arrow use too), the six every spell
    /// needs and the HUD's three, the thirteen it takes alone.
    /// </summary>
    private static readonly int[] FocusOnly = { 0x0015F160, 0x0015F278, 0x0015F354 };

    /// <summary>
    /// Shatter's own one: the status effects' (FUN_0011B3C8's) one way out, the game's ld s1, 0x30(sp), now a jump to its thaw (a
    /// monster it froze is held still till its time is up), which ends as the game's did. With the six every spell needs and the
    /// HUD's three, the ten it takes alone.
    /// </summary>
    private static readonly int[] ShatterOnly = { 0x0011B7D8 };

    /// <summary>
    /// Improved Enemy AI's seventeen: the first two words of the monsters' target finding (FUN_001182C0), their keeping a target
    /// (FUN_001185D8), the walk (FUN_0011C168), every monster's damage (FUN_00129540) and the base creature's alert handler
    /// (FUN_00129778), each now a jump into the pool and a nop; the monsters' melee blow's call of the hit function (0x00129C14,
    /// a jal into the pool); the fire elemental's odds (0x0019A464) and the kobold shaman's, lizardman shaman's and drow
    /// priestess's waits after a spell (0x001A1134, 0x001BCBFC, 0x001DBFE0), each a jump into the pool; and the drow men's and
    /// women's thinking in their vtables (0x0031AE64, 0x0031B794), now the pool's. All written whenever it is on, whichever of its
    /// parts are; none of the spells needs any of them, nor it any of theirs.
    /// </summary>
    private static readonly int[] EnemyAiHooks =
    {
        0x001182C0, 0x001182C4, 0x001185D8, 0x001185DC, 0x0011C168, 0x0011C16C,
        0x00129540, 0x00129544, 0x00129778, 0x0012977C, 0x00129C14, 0x0019A464,
        0x001A1134, 0x001BCBFC, 0x001DBFE0, 0x0031AE64, 0x0031B794
    };

    /// <summary>The game's words there, in the order of <see cref="EnemyAiHooks"/>.</summary>
    private static readonly uint[] EnemyAiGameWords =
    {
        0x27BDFE80, 0xFFB00150, 0x27BDFFA0, 0xFFB40040, 0x27BDFF20, 0xFFB20070,
        0x27BDFFB0, 0xFFB30030, 0x27BDFFF0, 0x68C20007, 0x0C047E0E, 0x24020014,
        0x24630032, 0x24630096, 0x24630064, 0x001D9FB0, 0x001DB590
    };

    private static readonly (int Start, int End)[] Used =
    {
        (0x00102910, 0x00102B5C), (0x00102D80, 0x00103000), (0x0010C338, 0x0010C6CC), (0x0010CD50, 0x0010CF30), (0x00118978, 0x00118D68),
        (0x00129318, 0x00129534), (0x00129C48, 0x00129D64), (0x0012A710, 0x0012A8A0), (0x0012BAE8, 0x0012BF90), (0x0012CD30, 0x0012D080),
        (0x0012F790, 0x0012FA08), (0x00130540, 0x00130704), (0x00131170, 0x001312B8), (0x00132018, 0x0013216C), (0x00132D90, 0x001333DC),
        (0x00134CE0, 0x00134DF8), (0x00134F18, 0x001350C8), (0x0013CDF8, 0x0013D000), (0x0013D218, 0x0013D42C), (0x00166E28, 0x00166FC0),
        (0x0016ACB8, 0x0016AE7C), (0x00191A20, 0x00191D18), (0x00196AB8, 0x00196BFC), (0x00198968, 0x00198AA4), (0x00199A60, 0x00199B98),
        (0x001A28B0, 0x001A2A00), (0x001A9A40, 0x001A9B40), (0x001AE930, 0x001AEA24), (0x001B8DA0, 0x001B8F10), (0x001B9570, 0x001B9684),
        (0x001B9E68, 0x001B9F4C), (0x001BAFF8, 0x001BB0D8), (0x001C36E8, 0x001C3870), (0x001CBA38, 0x001CBB44), (0x001D32D8, 0x001D341C),
        (0x001E8558, 0x001E878C), (0x001FF340, 0x001FF4D8), (0x00204DE0, 0x00204EA8), (0x00206460, 0x00206800), (0x002068C8, 0x00206AB8),
        (0x002071C0, 0x002073D0), (0x00207488, 0x00207534), (0x00207540, 0x002075C0), (0x00207A10, 0x00207BF4), (0x00208160, 0x00208318),
        (0x002089C0, 0x00208B10), (0x00208CB0, 0x00208E30), (0x0020AC30, 0x0020AD30), (0x0020B890, 0x0020BCB0), (0x0020C080, 0x0020C250),
        (0x0020C4C8, 0x0020C614), (0x0020DA88, 0x0020DB90), (0x0020E6E8, 0x0020EB38), (0x0020F6A8, 0x0020F958), (0x0020FCE0, 0x0020FF20),
        (0x00210058, 0x00210298), (0x00210A00, 0x00210C04), (0x00210CA0, 0x00210D90), (0x00221B88, 0x002222C0), (0x00224B68, 0x00224C68)
    };

    /// <summary>The address is in one of the stretches of the pool the patch takes.</summary>
    private static bool InUsed(uint va) => Used.Any(u => u.Start <= va && va < u.End);

    /// <summary>The game's Power Attack record (its mana is the Feats &amp; Spells tab's: 5 in the game).</summary>
    private const int PowerRecordVa = 0x0030E2E0;

    /// <summary>The game's Sorcerer's Blood record (type 24, bit 32).</summary>
    private const int SorcerersRecordVa = 0x0030E3D0;

    /// <summary>The game's Inferno Arrow record (type 9, bit 8: 1 mana, 1-6 damage, never used).</summary>
    private const int InfernoRecordVa = 0x0030DA20;

    private string _stockPath = "";
    private byte[] _stockBytes = Array.Empty<byte>();
    private string _workDir = "";

    [OneTimeSetUp]
    public void FindStockExecutable()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "Test_files", "SLES_506.72")))
        {
            dir = dir.Parent;
        }

        if (dir == null)
        {
            Assert.Ignore("Test_files/SLES_506.72 was not found above the test directory.");
        }

        _stockPath = Path.Combine(dir!.FullName, "Test_files", "SLES_506.72");
        _stockBytes = File.ReadAllBytes(_stockPath);
        _workDir = Path.Combine(Path.GetTempPath(), "BgdaBorrowedSpellsTests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_workDir);
    }

    [OneTimeTearDown]
    public void RemoveWorkDirectory()
    {
        if (Directory.Exists(_workDir)) Directory.Delete(_workDir, recursive: true);
    }

    private BgdaExecutable Stock() => BgdaExecutable.Open(_stockPath);

    private byte[] Bytes(BgdaExecutable exe, string name)
    {
        var path = Path.Combine(_workDir, name);
        exe.Save(path);
        return File.ReadAllBytes(path);
    }

    private BgdaExecutable OpenBytes(byte[] bytes, string name)
    {
        var path = Path.Combine(_workDir, name);
        File.WriteAllBytes(path, bytes);
        return BgdaExecutable.Open(path);
    }

    private static string Md5(byte[] data) => Convert.ToHexString(MD5.HashData(data)).ToLowerInvariant();

    private void RequireStock()
    {
        if (Md5(_stockBytes) != StockMd5) Assert.Ignore("Test_files/SLES_506.72 isn't the stock PAL executable.");
    }

    private static readonly BorrowedSpell[] Kromlech =
        { BorrowedSpell.PowerAttack, BorrowedSpell.BladeBarrier, BorrowedSpell.AntimagicField, BorrowedSpell.RighteousMight };

    /// <summary>All five of Kromlech's (his first four and Phase Axe).</summary>
    private static readonly BorrowedSpell[] KromlechAll = Kromlech.Append(BorrowedSpell.PhaseAxe).ToArray();

    /// <summary>All five of Adrianna's (Xantam's Fire, Sorcerer's Blood, Xantam's Orbs, Dimension Door and Shatter).</summary>
    private static readonly BorrowedSpell[] AdriannaAll =
    {
        BorrowedSpell.XantamFire, BorrowedSpell.SorcerersBlood, BorrowedSpell.XantamOrbs, BorrowedSpell.DimensionDoor, BorrowedSpell.Shatter
    };

    /// <summary>The four version 5 had (all but Power Attack and Sorcerer's Blood).</summary>
    private static readonly BorrowedSpell[] Version5Spells =
        { BorrowedSpell.XantamFire, BorrowedSpell.BladeBarrier, BorrowedSpell.AntimagicField, BorrowedSpell.RighteousMight };

    /// <summary>The five version 6 had (all but Sorcerer's Blood, Xantam's Orbs, Inferno Arrow, Entangle, Insect Plague, Poison Gas Arrow, Dimension Door, Expert Focus, Phase Axe and Shatter).</summary>
    private static readonly BorrowedSpell[] Version6Spells =
        BorrowedSpells.All.Where(s => s is not (BorrowedSpell.SorcerersBlood or BorrowedSpell.XantamOrbs or BorrowedSpell.InfernoArrow or
                                                BorrowedSpell.Entangle or BorrowedSpell.InsectPlague or BorrowedSpell.PoisonGasArrow or
                                                BorrowedSpell.DimensionDoor or
                                                BorrowedSpell.ExpertFocus or BorrowedSpell.PhaseAxe or BorrowedSpell.Shatter)).ToArray();

    /// <summary>The six version 7 had (all but Xantam's Orbs, Inferno Arrow, Entangle, Insect Plague, Poison Gas Arrow, Dimension Door, Expert Focus, Phase Axe and Shatter).</summary>
    private static readonly BorrowedSpell[] Version7Spells =
        BorrowedSpells.All.Where(s => s is not (BorrowedSpell.XantamOrbs or BorrowedSpell.InfernoArrow or BorrowedSpell.Entangle or
                                                BorrowedSpell.InsectPlague or BorrowedSpell.PoisonGasArrow or BorrowedSpell.DimensionDoor or
                                                BorrowedSpell.ExpertFocus or BorrowedSpell.PhaseAxe or BorrowedSpell.Shatter)).ToArray();

    /// <summary>The seven versions 9 and 8 had (all but Inferno Arrow, Entangle, Insect Plague, Poison Gas Arrow, Dimension Door, Expert Focus, Phase Axe and Shatter).</summary>
    private static readonly BorrowedSpell[] Version9Spells =
        BorrowedSpells.All.Where(s => s is not (BorrowedSpell.InfernoArrow or BorrowedSpell.Entangle or BorrowedSpell.InsectPlague or
                                                BorrowedSpell.PoisonGasArrow or BorrowedSpell.DimensionDoor or
                                                BorrowedSpell.ExpertFocus or BorrowedSpell.PhaseAxe or BorrowedSpell.Shatter)).ToArray();

    /// <summary>The eight version 10 had (all but Entangle, Insect Plague, Poison Gas Arrow, Dimension Door, Expert Focus, Phase Axe and Shatter).</summary>
    private static readonly BorrowedSpell[] Version10Spells =
        BorrowedSpells.All.Where(s => s is not (BorrowedSpell.Entangle or BorrowedSpell.InsectPlague or BorrowedSpell.PoisonGasArrow or
                                                BorrowedSpell.DimensionDoor or
                                                BorrowedSpell.ExpertFocus or BorrowedSpell.PhaseAxe or BorrowedSpell.Shatter)).ToArray();

    /// <summary>The nine version 11 had (all but Insect Plague, Poison Gas Arrow, Dimension Door, Expert Focus, Phase Axe and Shatter).</summary>
    private static readonly BorrowedSpell[] Version11Spells =
        BorrowedSpells.All.Where(s => s is not (BorrowedSpell.InsectPlague or BorrowedSpell.PoisonGasArrow or BorrowedSpell.DimensionDoor or
                                                BorrowedSpell.ExpertFocus or BorrowedSpell.PhaseAxe or BorrowedSpell.Shatter)).ToArray();

    /// <summary>The ten version 12 had (all but Poison Gas Arrow, Dimension Door, Expert Focus, Phase Axe and Shatter).</summary>
    private static readonly BorrowedSpell[] Version12Spells =
        BorrowedSpells.All.Where(s => s is not (BorrowedSpell.PoisonGasArrow or BorrowedSpell.DimensionDoor or
                                                BorrowedSpell.ExpertFocus or BorrowedSpell.PhaseAxe or BorrowedSpell.Shatter)).ToArray();

    /// <summary>The eleven version 13 had (all but Dimension Door, Expert Focus, Phase Axe and Shatter).</summary>
    private static readonly BorrowedSpell[] Version13Spells =
        BorrowedSpells.All.Where(s => s is not (BorrowedSpell.DimensionDoor or BorrowedSpell.ExpertFocus or BorrowedSpell.PhaseAxe or BorrowedSpell.Shatter)).ToArray();

    /// <summary>The twelve version 14 had (all but Expert Focus, Phase Axe and Shatter).</summary>
    private static readonly BorrowedSpell[] Version14Spells =
        BorrowedSpells.All.Where(s => s is not (BorrowedSpell.ExpertFocus or BorrowedSpell.PhaseAxe or BorrowedSpell.Shatter)).ToArray();

    /// <summary>The thirteen version 15 had (all but Phase Axe and Shatter).</summary>
    private static readonly BorrowedSpell[] Version15Spells =
        BorrowedSpells.All.Where(s => s is not (BorrowedSpell.PhaseAxe or BorrowedSpell.Shatter)).ToArray();

    /// <summary>The fourteen version 16 had (all but Shatter).</summary>
    private static readonly BorrowedSpell[] Version16Spells = BorrowedSpells.All.Where(s => s != BorrowedSpell.Shatter).ToArray();

    /// <summary>The fifteen version 17 had (all of them; it had no Improved Enemy AI).</summary>
    private static readonly BorrowedSpell[] Version17Spells = BorrowedSpells.All.ToArray();

    /// <summary>The stock executable with an earlier version in, as featpatch.py wrote it (its runs, checked by their MD5).</summary>
    private byte[] Earlier(string runsBase64, string md5)
    {
        using var packed = new MemoryStream(Convert.FromBase64String(runsBase64));
        using var unpack = new ZLibStream(packed, CompressionMode.Decompress);
        using var runs = new MemoryStream();
        unpack.CopyTo(runs);
        var r = runs.ToArray();
        var bytes = (byte[])_stockBytes.Clone();
        for (var at = 0; at < r.Length;)
        {
            var offset = BitConverter.ToInt32(r, at);
            var length = BitConverter.ToInt32(r, at + 4);
            Array.Copy(r, at + 8, bytes, offset, length);
            at += 8 + length;
        }

        Assert.That(Md5(bytes), Is.EqualTo(md5));
        return bytes;
    }

    private byte[] Version5() => Earlier(Version5RunsBase64, Version5Md5);

    private byte[] Version6() => Earlier(Version6RunsBase64, Version6Md5);

    private byte[] Version7() => Earlier(Version7RunsBase64, Version7Md5);

    private byte[] Version8() => Earlier(Version8RunsBase64, Version8Md5);

    private byte[] Version9() => Earlier(Version9RunsBase64, Version9Md5);

    private byte[] Version10() => Earlier(Version10RunsBase64, Version10Md5);

    private byte[] Version11() => Earlier(Version11RunsBase64, Version11Md5);

    private byte[] Version12() => Earlier(Version12RunsBase64, Version12Md5);

    private byte[] Version13() => Earlier(Version13RunsBase64, Version13Md5);

    private byte[] Version14() => Earlier(Version14RunsBase64, Version14Md5);

    private byte[] Version15() => Earlier(Version15RunsBase64, Version15Md5);

    private byte[] Version16() => Earlier(Version16RunsBase64, Version16Md5);

    private byte[] Version17() => Earlier(Version17RunsBase64, Version17Md5);

    private byte[] Version21() => Earlier(Version21RunsBase64, Version21Md5);

    private static Dictionary<BorrowedSpell, BorrowedSpellSettings> NoSpells() => new();

    private static Dictionary<BorrowedSpell, BorrowedSpellSettings> Defaults(IEnumerable<BorrowedSpell> spells) =>
        spells.ToDictionary(s => s, s => BorrowedSpells.Info(s).Default);

    private static void AssertSame(IReadOnlyDictionary<BorrowedSpell, BorrowedSpellSettings> got, IReadOnlyDictionary<BorrowedSpell, BorrowedSpellSettings> want)
    {
        Assert.That(got.Keys.OrderBy(k => k), Is.EqualTo(want.Keys.OrderBy(k => k)));
        foreach (var (spell, settings) in want) Assert.That(got[spell].Equals(settings), Is.True, spell.ToString());
    }

    // ═══════════════════════════════ the patch ═══════════════════════════════

    [Test]
    public void TheStockExecutable_TakesTheSpells_AsFeatpatchWritesThem()
    {
        RequireStock();
        var exe = Stock();
        Assert.That(exe.GetBorrowedSpellsState(out var detail), Is.EqualTo(BorrowedSpellsState.Off), detail);
        exe.SetBorrowedSpells(Defaults(BorrowedSpells.All));
        var on = Bytes(exe, "on");
        Assert.That(Md5(on), Is.EqualTo(AllFifteenMd5), "the same bytes as featpatch.py");
        Assert.That(on.Length, Is.EqualTo(_stockBytes.Length), "the file doesn't grow");
        var reopened = OpenBytes(on, "on2");
        Assert.That(reopened.GetBorrowedSpellsState(out detail), Is.EqualTo(BorrowedSpellsState.On), detail);
        AssertSame(reopened.GetBorrowedSpells(), Defaults(BorrowedSpells.All));
        Assert.That(reopened.GetThreePlayerState(), Is.EqualTo(ThreePlayerState.Off), "the Three Players tab still sees the stock file");

        reopened.RemoveBorrowedSpells();
        Assert.That(Bytes(reopened, "off"), Is.EqualTo(_stockBytes), "taken out: the stock file, byte for byte");

        exe = Stock();
        exe.SetBorrowedSpells(BorrowedSpells.All.ToDictionary(s => s, BorrowedSpells.TestSettings));
        Assert.That(Md5(Bytes(exe, "test")), Is.EqualTo(TestSettingsMd5), "price 0 and 4 mana: featpatch.py's test settings (without the tripwire)");

        exe = Stock();
        exe.SetBorrowedSpells(Defaults(Kromlech));
        Assert.That(Md5(Bytes(exe, "krom")), Is.EqualTo(KromlechMd5),
                    "Kromlech's first four alone: featpatch.py --spells powerattack,bladebarrier,antimagic,righteousmight --ai off");

        exe = Stock();
        exe.SetBorrowedSpells(Defaults(new[] { BorrowedSpell.PowerAttack }));
        Assert.That(Md5(Bytes(exe, "power")), Is.EqualTo(PowerAttackMd5), "Power Attack alone: featpatch.py --spells powerattack --ai off");
        Assert.That(exe.GetBorrowedSpells()[BorrowedSpell.PowerAttack].Equals(BorrowedSpells.Info(BorrowedSpell.PowerAttack).Default), Is.True);

        exe = Stock();
        exe.SetBorrowedSpells(Defaults(new[] { BorrowedSpell.SorcerersBlood }));
        Assert.That(Md5(Bytes(exe, "sorcerer")), Is.EqualTo(SorcerersMd5), "Sorcerer's Blood alone: featpatch.py --spells sorcerersblood --ai off");

        exe = Stock();
        exe.SetBorrowedSpells(Defaults(new[] { BorrowedSpell.XantamOrbs }));
        Assert.That(Md5(Bytes(exe, "orbs")), Is.EqualTo(OrbsMd5), "Xantam's Orbs alone: featpatch.py --spells xantamorbs --ai off");

        exe = Stock();
        exe.SetBorrowedSpells(Defaults(Version9Spells));
        Assert.That(Md5(Bytes(exe, "seven")), Is.EqualTo(SevenMd5),
                    "all but Inferno Arrow, Entangle, Insect Plague, Poison Gas Arrow, Dimension Door, Expert Focus, Phase Axe and Shatter");

        exe = Stock();
        exe.SetBorrowedSpells(Defaults(Version10Spells));
        Assert.That(Md5(Bytes(exe, "eight")), Is.EqualTo(EightMd5),
                    "all but Entangle, Insect Plague, Poison Gas Arrow, Dimension Door, Expert Focus, Phase Axe and Shatter");

        exe = Stock();
        exe.SetBorrowedSpells(Defaults(Version11Spells));
        Assert.That(Md5(Bytes(exe, "nine")), Is.EqualTo(NineMd5), "all but Insect Plague, Poison Gas Arrow, Dimension Door, Expert Focus, Phase Axe and Shatter");

        exe = Stock();
        exe.SetBorrowedSpells(Defaults(Version12Spells));
        Assert.That(Md5(Bytes(exe, "ten")), Is.EqualTo(TenMd5), "all but Poison Gas Arrow, Dimension Door, Expert Focus, Phase Axe and Shatter");

        exe = Stock();
        exe.SetBorrowedSpells(Defaults(Version13Spells));
        Assert.That(Md5(Bytes(exe, "eleven")), Is.EqualTo(ElevenMd5), "all but Dimension Door, Expert Focus, Phase Axe and Shatter");

        exe = Stock();
        exe.SetBorrowedSpells(Defaults(Version14Spells));
        Assert.That(Md5(Bytes(exe, "twelve")), Is.EqualTo(TwelveMd5), "all but Expert Focus, Phase Axe and Shatter");

        exe = Stock();
        exe.SetBorrowedSpells(Defaults(Version15Spells));
        Assert.That(Md5(Bytes(exe, "thirteen")), Is.EqualTo(ThirteenMd5), "all but Phase Axe and Shatter");

        exe = Stock();
        exe.SetBorrowedSpells(Defaults(Version16Spells));
        Assert.That(Md5(Bytes(exe, "fourteen")), Is.EqualTo(FourteenMd5), "all but Shatter");

        exe = Stock();
        exe.SetBorrowedSpells(Defaults(new[] { BorrowedSpell.InfernoArrow }));
        Assert.That(Md5(Bytes(exe, "inferno")), Is.EqualTo(InfernoMd5), "Inferno Arrow alone: featpatch.py --spells infernoarrow --ai off");

        exe = Stock();
        exe.SetBorrowedSpells(Defaults(new[] { BorrowedSpell.Entangle }));
        Assert.That(Md5(Bytes(exe, "entangle")), Is.EqualTo(EntangleMd5), "Entangle alone: featpatch.py --spells entangle --ai off");

        exe = Stock();
        exe.SetBorrowedSpells(Defaults(new[] { BorrowedSpell.InsectPlague }));
        Assert.That(Md5(Bytes(exe, "insect")), Is.EqualTo(InsectMd5), "Insect Plague alone: featpatch.py --spells insectplague --ai off");

        exe = Stock();
        exe.SetBorrowedSpells(Defaults(new[] { BorrowedSpell.PoisonGasArrow }));
        Assert.That(Md5(Bytes(exe, "gas")), Is.EqualTo(GasMd5), "Poison Gas Arrow alone: featpatch.py --spells gasarrow --ai off");

        exe = Stock();
        exe.SetBorrowedSpells(Defaults(new[] { BorrowedSpell.DimensionDoor }));
        Assert.That(Md5(Bytes(exe, "door")), Is.EqualTo(DoorMd5), "Dimension Door alone: featpatch.py --spells dimensiondoor --ai off");

        exe = Stock();
        exe.SetBorrowedSpells(Defaults(new[] { BorrowedSpell.ExpertFocus }));
        Assert.That(Md5(Bytes(exe, "focus")), Is.EqualTo(FocusMd5), "Expert Focus alone: featpatch.py --spells expertfocus --ai off");

        exe = Stock();
        exe.SetBorrowedSpells(Defaults(new[] { BorrowedSpell.PhaseAxe }));
        Assert.That(Md5(Bytes(exe, "axe")), Is.EqualTo(AxeMd5), "Phase Axe alone: featpatch.py --spells phaseaxe --ai off");

        exe = Stock();
        exe.SetBorrowedSpells(Defaults(new[] { BorrowedSpell.Shatter }));
        Assert.That(Md5(Bytes(exe, "shatter")), Is.EqualTo(ShatterMd5), "Shatter alone: featpatch.py --spells shatter --ai off");
    }

    [Test]
    public void Entangle_IsTheGamesOwnVines_WithAThornyUpdateOfItsOwn()
    {
        RequireStock();
        var info = BorrowedSpells.Info(BorrowedSpell.Entangle);
        Assert.That((info.Bit, info.CastType, info.GameFeat, info.GameRecord, info.Passive, info.HasMana, info.HasSecond, info.HasThird,
                     info.Range, info.Hero, info.More.Count),
                    Is.EqualTo((47, 36, false, false, false, true, true, true, true, "Vahn", 2)));
        Assert.That((info.HasOwnTexts, info.UsesGameRecord, BorrowedSpellIcons.HasOwnIcon(BorrowedSpell.Entangle)), Is.EqualTo((true, false, true)),
                    "its own record, texts and icon");
        Assert.That((info.ThirdMin, info.ThirdMax, info.FirstMax, info.SecondMax), Is.EqualTo((1, 10, 255, 255)), "1 to 10 vines, 0 to 255 a thorn");
        Assert.That(info.More.Select(v => (v.Min, v.Max, v.Tenths, v.AtMost, v.Step)),
                    Is.EqualTo(new[] { (2, 98, false, -1, 2), (1, 255, true, -1, 1) }), "an even percentage slower; the time in tenths of a second");
        Assert.That(info.Default.Equals(new BorrowedSpellSettings(12, 12f, new[] { 2, 3, 4, 5, 6 }, new[] { 4, 5, 6, 7, 8 }, Third: new[] { 1, 2, 3, 4, 5 },
            More: new IReadOnlyList<int>[] { new[] { 30, 34, 40, 44, 50 }, new[] { 40, 45, 50, 55, 60 } })), Is.True,
                    "price 12, 12 mana; one vine at rank 1 up to five at rank 5; thorns of 2-4 up to 6-8; 30 up to 50 percent slower for 4 up to 6 s");
        Assert.That(BorrowedSpellTexts.NameKey(BorrowedSpell.Entangle), Is.EqualTo("entangle_name"));
        Assert.That(BorrowedSpellTexts.Of(BorrowedSpell.Entangle).Select(t => (t.Language, t.Key)),
                    Is.EqualTo(Enumerable.Range(0, 5).SelectMany(l => new[] { (l, "entangle_name"), (l, "entangle_desc") })));
        Assert.That(BorrowedSpellTexts.Of(BorrowedSpell.Entangle).Where(t => t.Key == "entangle_name").Select(t => t.Text),
                    Is.EqualTo(new[] { "Entangle", "Enchevêtrement", "Verstricken", "Intralciare", "Enmarañar" }), "the D&D spell's name in each language");
        Assert.That(BorrowedSpellIcons.EntryName(BorrowedSpell.Entangle), Is.EqualTo("icon_entangle.tex"));

        // its own settings: featpatch.py's file, read back
        var exe = Stock();
        exe.SetBorrowedSpells(new Dictionary<BorrowedSpell, BorrowedSpellSettings> { [BorrowedSpell.Entangle] = EntangleCustom });
        var bytes = Bytes(exe, "entangle-custom");
        Assert.That(Md5(bytes), Is.EqualTo(EntangleCustomMd5));
        var reopened = OpenBytes(bytes, "entangle-custom2");
        Assert.That(reopened.GetBorrowedSpellsState(out var detail), Is.EqualTo(BorrowedSpellsState.On), detail);
        var read = reopened.GetBorrowedSpells();
        Assert.That(read.Keys, Is.EqualTo(new[] { BorrowedSpell.Entangle }));
        Assert.That(read[BorrowedSpell.Entangle].Equals(EntangleCustom), Is.True, "read back: 1-10 vines, 2-98 percent, 0.1-25.5 s, 0-255 thorns");

        // its hooks: the six every spell needs, the HUD's three and its own four, in the game's VineSeek update
        var hooks = Hooks.Where(va => BitConverter.ToUInt32(bytes, va - VaToFile) != BitConverter.ToUInt32(_stockBytes, va - VaToFile)).ToList();
        Assert.That(hooks.Count, Is.EqualTo(13));
        Assert.That(EntangleOnly.All(hooks.Contains), Is.True);
        Assert.That(BitConverter.ToUInt32(bytes, 0x001BEFD0 - VaToFile), Is.EqualTo(0x24040060u), "addiu a0, zero, 0x60: room for the vine's own fields");
        Assert.That(BitConverter.ToUInt32(_stockBytes, 0x001BEFD0 - VaToFile), Is.EqualTo(0x24040058u), "(the game's 0x58)");

        // the second data block: its mark, the icons of types 32-36, Entangle's tables by rank (entry 0 is rank 0's; the slow halved)
        var data2 = FeatPatch.Data2Va - VaToFile;
        Assert.That(Encoding.ASCII.GetString(bytes, data2, 4), Is.EqualTo("FEA2"));
        Assert.That(BitConverter.ToInt32(bytes, data2 + 4), Is.EqualTo(FeatPatch.Version));
        Assert.That(Encoding.ASCII.GetString(bytes, data2 + 8 + 4 * 0x18, 18), Is.EqualTo("icon_entangle.tex\0"));
        Assert.That(bytes.Skip(data2 + FeatPatch.EntangleVinesAt).Take(8), Is.EqualTo(new byte[] { 0, 1, 3, 5, 7, 10, 0, 0 }));
        Assert.That(bytes.Skip(data2 + FeatPatch.EntangleSlowAt).Take(8), Is.EqualTo(new byte[] { 0, 1, 10, 25, 40, 49, 0, 0 }),
                    "the game's slow byte: half the percentage (49 of 50 at 98 percent)");
        Assert.That(bytes.Skip(data2 + FeatPatch.EntangleTimeAt).Take(8), Is.EqualTo(new byte[] { 0, 1, 10, 25, 100, 255, 0, 0 }));
        Assert.That(bytes.Skip(data2 + FeatPatch.EntangleThornMaxAt + 1).Take(5), Is.EqualTo(new byte[] { 0, 2, 30, 20, 255 }));
        Assert.That(BitConverter.ToSingle(bytes, data2 + 0xC0), Is.EqualTo(FeatPatch.EntangleRange));
        Assert.That(BitConverter.ToSingle(bytes, data2 + 0xC4) * 2 * 50, Is.EqualTo(FeatPatch.EntangleSpeed), "the step a frame, halved");
        Assert.That(BitConverter.ToInt32(bytes, data2 + 0xC8), Is.EqualTo(FeatPatch.EntanglePeriod));
        Assert.That((FeatPatch.EntangleRange, FeatPatch.EntangleSpeed, FeatPatch.EntanglePeriod), Is.EqualTo((240f, 200f, 50)));

        // the VineSpell vtable copy: the game's (0x00317470) but for its destructor (+0x0C) and update (+0x24)
        const int gameVine = 0x00317470 - VaToFile;
        var copy = FeatPatch.VTables.Single(v => v.GameVa == 0x00317470).Va - VaToFile;
        for (var at = 0; at < 0x80; at += 4)
        {
            var word = BitConverter.ToUInt32(bytes, copy + at);
            if (at is 0x0C or 0x24)
            {
                Assert.That(word, Is.Not.EqualTo(BitConverter.ToUInt32(_stockBytes, gameVine + at)), $"+0x{at:X2}: the vines' own");
                Assert.That(InUsed(word), Is.True, "in the pool");
            }
            else Assert.That(word, Is.EqualTo(BitConverter.ToUInt32(_stockBytes, gameVine + at)), $"+0x{at:X2}");
        }

        Assert.That(bytes.AsSpan(gameVine, 0x80).SequenceEqual(_stockBytes.AsSpan(gameVine, 0x80)), Is.True, "the game's vtable is never written");

        // the cast table: type 36 is Entangle's case, in the pool
        var table = FeatPatch.CastTableVa - VaToFile;
        Assert.That(FeatPatch.CastTypes, Is.EqualTo(42));
        var cast = BitConverter.ToUInt32(bytes, table + 4 * 35);
        Assert.That(InUsed(cast), Is.True, "type 36: its own cast");
        Assert.That(Enumerable.Range(23, 12).Concat(new[] { 36, 37, 38, 39, 40, 41 }).Select(t => BitConverter.ToUInt32(bytes, table + 4 * t)).Distinct(),
                    Is.EqualTo(new[] { 0x001628F0u }),
                    "types 24-35, 37 (Insect Plague's, off), 38 (Poison Gas Arrow's: an arrow, never cast), 39 (Dimension Door's, off), " +
                    "40 (Expert Focus's, off), 41 (Phase Axe's, off) and 42 (Shatter's, off): the default");

        // a second block that isn't version 22's: not WorldExplorer's
        var unmarked = (byte[])bytes.Clone();
        unmarked[data2 + 4] = 11;
        Assert.That(OpenBytes(unmarked, "entangle-unmarked").GetBorrowedSpellsState(out detail), Is.EqualTo(BorrowedSpellsState.Unrecognized), detail);

        // the vines' code it relies on changed: not the game's
        foreach (var va in new[] { 0x001BF00C, 0x001BF66C, 0x003174F4 + 0x20, 0x00233804 })
        {
            var changed = (byte[])_stockBytes.Clone();
            changed[va - VaToFile] ^= 4;
            Assert.That(OpenBytes(changed, "vines").GetBorrowedSpellsState(out detail), Is.EqualTo(BorrowedSpellsState.Unrecognized), $"0x{va:X8}");
            Assert.That(detail, Does.Contain($"0x{va:X8}"));
        }

        // an ability with its bit or its cast type already: refused
        foreach (var (at, value) in new[] { (0x4D, (byte)0x80), (0x20, (byte)36) })
        {
            var changed = (byte[])_stockBytes.Clone();
            changed[0x0030E1F0 - VaToFile + at] = at == 0x20 ? value : (byte)(changed[0x0030E1F0 - VaToFile + at] | value);
            if (at == 0x20) changed[0x0030E1F0 - VaToFile + 0x21] = 0;
            Assert.That(OpenBytes(changed, "entangle-taken").GetBorrowedSpellsState(out detail), Is.EqualTo(BorrowedSpellsState.Unrecognized), $"+0x{at:X2}");
            Assert.That(detail, Does.Contain("Entangle"));
        }
    }

    [Test]
    public void InsectPlague_IsTheShamansSwarm_WithItsSettingsInAThirdBlock()
    {
        RequireStock();
        var ip = BorrowedSpell.InsectPlague;
        var info = BorrowedSpells.Info(ip);
        Assert.That((info.Bit, info.CastType, info.GameFeat, info.GameRecord, info.Passive, info.HasMana, info.HasSecond, info.HasThird,
                     info.Range, info.Hero, info.More.Count),
                    Is.EqualTo((48, 37, false, false, false, true, true, true, true, "Vahn", 1)));
        Assert.That((info.HasOwnTexts, info.UsesGameRecord, BorrowedSpellIcons.HasOwnIcon(ip)), Is.EqualTo((true, false, true)),
                    "its own record, texts and icon");
        Assert.That((info.ThirdMin, info.ThirdMax, info.FirstMax, info.SecondMax), Is.EqualTo((1, 10, 255, 255)), "1 to 10 swarms, 0 to 255 a sting");
        Assert.That(info.More.Select(v => (v.Min, v.Max, v.Tenths, v.AtMost, v.Step)), Is.EqualTo(new[] { (1, 255, true, -1, 1) }),
                    "how long a swarm lasts, in tenths of a second");
        Assert.That(info.Default.Equals(new BorrowedSpellSettings(12, 15f, new[] { 1, 1, 1, 1, 1 }, new[] { 3, 3, 3, 3, 3 }, Third: new[] { 1, 2, 3, 4, 5 },
            More: new IReadOnlyList<int>[] { new[] { 80, 80, 80, 80, 80 } })), Is.True,
                    "price 12, 15 mana; one swarm at rank 1 up to five at rank 5; stings of 1-3 for 8 s");
        Assert.That(BorrowedSpellTexts.NameKey(ip), Is.EqualTo("insectplague_name"));
        Assert.That(BorrowedSpellTexts.Of(ip).Select(t => (t.Language, t.Key)),
                    Is.EqualTo(Enumerable.Range(0, 5).SelectMany(l => new[] { (l, "insectplague_name"), (l, "insectplague_desc") })));
        Assert.That(BorrowedSpellTexts.Of(ip).Where(t => t.Key == "insectplague_name").Select(t => t.Text),
                    Is.EqualTo(new[] { "Insect Plague", "Fléau d'insectes", "Insektenplage", "Piaga di Insetti", "Plaga de insectos" }),
                    "the D&D spell's name in each language");
        Assert.That(BorrowedSpellIcons.EntryName(ip), Is.EqualTo("icon_insectplague.tex"));
        Assert.That(BorrowedSpellIcons.FallbackName(ip), Is.EqualTo("icon_magicmissile.tex (Magic Missile's)"));

        // its own settings: featpatch.py's file, read back
        var exe = Stock();
        exe.SetBorrowedSpells(new Dictionary<BorrowedSpell, BorrowedSpellSettings> { [ip] = InsectCustom });
        var bytes = Bytes(exe, "insect-custom");
        Assert.That(Md5(bytes), Is.EqualTo(InsectCustomMd5));
        var reopened = OpenBytes(bytes, "insect-custom2");
        Assert.That(reopened.GetBorrowedSpellsState(out var detail), Is.EqualTo(BorrowedSpellsState.On), detail);
        var read = reopened.GetBorrowedSpells();
        Assert.That(read.Keys, Is.EqualTo(new[] { ip }));
        Assert.That(read[ip].Equals(InsectCustom), Is.True, "read back: 1-10 swarms, 0.1-25.5 s, 0-255 stings");

        // its hooks: the six every spell needs, the HUD's three and its own one, the poison's bite (a jal into the pool for the game's jalr)
        var hooks = Hooks.Where(va => BitConverter.ToUInt32(bytes, va - VaToFile) != BitConverter.ToUInt32(_stockBytes, va - VaToFile)).ToList();
        Assert.That(hooks.Count, Is.EqualTo(10));
        Assert.That(InsectOnly.All(hooks.Contains), Is.True);
        var bite = BitConverter.ToUInt32(bytes, 0x0011B758 - VaToFile);
        var biteTo = (int)((bite & 0x03FFFFFF) << 2);
        Assert.That((bite >> 26, Used.Any(u => u.Start <= biteTo && biteTo < u.End)), Is.EqualTo((3u, true)), "jal into the pool");
        Assert.That(BitConverter.ToUInt32(_stockBytes, 0x0011B758 - VaToFile), Is.EqualTo(0x0100F809u), "(the game's jalr t0)");

        // the main block names it (its bit in the mask) but has no entry for it; the third block has its entry, record and settings
        var data = FeatPatch.DataVa - VaToFile;
        Assert.That(BitConverter.ToUInt32(bytes, data + FeatPatch.SpellsAt), Is.EqualTo(0x200u), "its bit in the main block's mask");
        Assert.That((BitConverter.ToInt32(bytes, data + FeatPatch.CountAt), BitConverter.ToInt32(bytes, data + FeatPatch.AbilityCountAt)),
                    Is.EqualTo((0, 0)), "no entry or record there");
        var data3 = FeatPatch.Data3Va - VaToFile;
        Assert.That(Encoding.ASCII.GetString(bytes, data3, 4), Is.EqualTo("FEA3"));
        Assert.That(BitConverter.ToInt32(bytes, data3 + 4), Is.EqualTo(FeatPatch.Version));
        Assert.That((BitConverter.ToInt32(bytes, data3 + FeatPatch.Data3CountAt), BitConverter.ToInt32(bytes, data3 + FeatPatch.Data3AbilityCountAt)),
                    Is.EqualTo((1, 1)), "its list entry and its ability record");
        Assert.That(bytes[data3 + FeatPatch.Data3EntryAt + FeatPatch.EntryPriceAt], Is.EqualTo(4));
        Assert.That(BitConverter.ToSingle(bytes, data3 + FeatPatch.Data3AbilityAt + FeatPatch.AbilityManaAt), Is.EqualTo(6.5f));
        Assert.That(Encoding.ASCII.GetString(bytes, data3 + 0xC0, 22), Is.EqualTo("icon_insectplague.tex\0"));
        Assert.That(BitConverter.ToUInt32(bytes, data3 + 0xD8), Is.EqualTo(0x0030A928u), "its fallback: the game's icon_magicmissile.tex");
        Assert.That(bytes.Skip(data3 + FeatPatch.InsectSwarmsAt).Take(8), Is.EqualTo(new byte[] { 0, 1, 3, 5, 7, 10, 0, 0 }));
        Assert.That(bytes.Skip(data3 + FeatPatch.InsectTimeAt).Take(8), Is.EqualTo(new byte[] { 0, 1, 10, 25, 100, 255, 0, 0 }));
        Assert.That(bytes.Skip(data3 + FeatPatch.InsectStingMinAt).Take(8), Is.EqualTo(new byte[] { 0, 0, 1, 9, 20, 0, 0, 0 }));
        Assert.That(bytes.Skip(data3 + FeatPatch.InsectStingMaxAt).Take(8), Is.EqualTo(new byte[] { 0, 0, 2, 30, 20, 255, 0, 0 }));
        Assert.That((BitConverter.ToSingle(bytes, data3 + 0x100), BitConverter.ToSingle(bytes, data3 + 0x104), BitConverter.ToInt32(bytes, data3 + 0x108),
                     BitConverter.ToInt32(bytes, data3 + 0x10C), BitConverter.ToSingle(bytes, data3 + 0x110), BitConverter.ToInt32(bytes, data3 + 0x114)),
                    Is.EqualTo((FeatPatch.InsectRange, FeatPatch.InsectSpeed, FeatPatch.InsectGather, FeatPatch.InsectPeriod, FeatPatch.InsectPoison,
                                FeatPatch.InsectKeep)));
        Assert.That((FeatPatch.InsectRange, FeatPatch.InsectSpeed, FeatPatch.InsectGather, FeatPatch.InsectPeriod, FeatPatch.InsectPoison,
                     FeatPatch.InsectKeep), Is.EqualTo((240f, 150f, 25, 50, 5f, 260)),
                    "within 240, half as fast again as the shaman's swarm, half a second gathering, a sting a second, 5 s of poison");
        Assert.That(bytes.Skip(data3 + 0x120).Take(0xC0).All(b => b == 0), Is.True, "its poison records (the game's run time) zero");

        // off: the third block is there with its defaults, and no entry
        var off = Stock();
        off.SetBorrowedSpells(Defaults(new[] { BorrowedSpell.Entangle }));
        var entangle = Bytes(off, "insect-off");
        Assert.That(Encoding.ASCII.GetString(entangle, data3, 4), Is.EqualTo("FEA3"));
        Assert.That(BitConverter.ToInt32(entangle, data3 + FeatPatch.Data3CountAt), Is.EqualTo(0));
        Assert.That(entangle.Skip(data3 + FeatPatch.InsectSwarmsAt).Take(8), Is.EqualTo(new byte[] { 0, 1, 2, 3, 4, 5, 0, 0 }));
        Assert.That(BitConverter.ToUInt32(entangle, 0x0011B758 - VaToFile), Is.EqualTo(0x0100F809u), "the poison's bite the game's");

        // the InsectPlagueSpell vtable copy, all 0xA0 bytes of it (a swarm is a creature: the game's walk calls through +0x8C; a copy
        // of 0x80 crashed the first test): the game's (0x0031B530) but for its destructor (+0x0C) and update (+0x24); the game's draw
        const int gameSwarm = 0x0031B530 - VaToFile;
        var copy = FeatPatch.VTables.Single(v => v.GameVa == 0x0031B530).Va - VaToFile;
        Assert.That(FeatPatch.VTables.Single(v => v.GameVa == 0x0031B530 + 0x80).Va - VaToFile, Is.EqualTo(copy + 0x80), "its second piece follows");
        for (var at = 0; at < 0xA0; at += 4)
        {
            var word = BitConverter.ToUInt32(bytes, copy + at);
            if (at is 0x0C or 0x24)
            {
                Assert.That(word, Is.Not.EqualTo(BitConverter.ToUInt32(_stockBytes, gameSwarm + at)), $"+0x{at:X2}: the swarms' own");
                Assert.That(InUsed(word), Is.True, "in the pool");
            }
            else Assert.That(word, Is.EqualTo(BitConverter.ToUInt32(_stockBytes, gameSwarm + at)), $"+0x{at:X2}");
        }

        Assert.That(bytes.AsSpan(gameSwarm, 0xA0).SequenceEqual(_stockBytes.AsSpan(gameSwarm, 0xA0)), Is.True, "the game's vtable is never written");

        // the cast table: type 37 is Insect Plague's case, in the pool
        var cast = BitConverter.ToUInt32(bytes, FeatPatch.CastTableVa - VaToFile + 4 * 36);
        Assert.That(InUsed(cast), Is.True, "type 37: its own cast");

        // a third block that isn't version 22's, or not saying it has its entry: not WorldExplorer's
        foreach (var (at, value) in new[] { (4, (byte)11), (FeatPatch.Data3CountAt, (byte)0) })
        {
            var unmarked = (byte[])bytes.Clone();
            unmarked[data3 + at] = value;
            Assert.That(OpenBytes(unmarked, "insect-unmarked").GetBorrowedSpellsState(out detail), Is.EqualTo(BorrowedSpellsState.Unrecognized),
                        $"+0x{at:X}: {detail}");
        }

        // the shaman's swarm's code it relies on changed: not the game's
        foreach (var va in new[] { 0x001E0D58, 0x0031B530 + 0x2C, 0x001DFAB0 })
        {
            var changed = (byte[])_stockBytes.Clone();
            changed[va - VaToFile] ^= 4;
            Assert.That(OpenBytes(changed, "swarm").GetBorrowedSpellsState(out detail), Is.EqualTo(BorrowedSpellsState.Unrecognized), $"0x{va:X8}");
            Assert.That(detail, Does.Contain($"0x{va:X8}"));
        }

        // an ability with its bit or its cast type already: refused
        foreach (var (at, value) in new[] { (0x4E, (byte)0x01), (0x20, (byte)37) })
        {
            var changed = (byte[])_stockBytes.Clone();
            changed[0x0030E1F0 - VaToFile + at] = at == 0x20 ? value : (byte)(changed[0x0030E1F0 - VaToFile + at] | value);
            if (at == 0x20) changed[0x0030E1F0 - VaToFile + 0x21] = 0;
            Assert.That(OpenBytes(changed, "insect-taken").GetBorrowedSpellsState(out detail), Is.EqualTo(BorrowedSpellsState.Unrecognized), $"+0x{at:X2}");
            Assert.That(detail, Does.Contain("Insect Plague"));
        }
    }

    [Test]
    public void ImprovedEnemyAi_HasSevenParts_WithItsNumbersInANinthAndATenthBlock()
    {
        RequireStock();
        Assert.That((FeatPatch.EnemyAiTargeting, FeatPatch.EnemyAiSurround, FeatPatch.EnemyAiAlarm, FeatPatch.EnemyAiCasters, FeatPatch.EnemyAiBrutes,
                     FeatPatch.EnemyAiDrow, FeatPatch.EnemyAiRegroup, FeatPatch.EnemyAiParts, BgdaExecutable.EnemyAiParts),
                    Is.EqualTo((1, 2, 4, 8, 0x10, 0x20, 0x40, 0x7F, 0x7F)),
                    "smarter targeting, surround the hero, raise the alarm, casters cast more, brutes knock you back, drow blink, wounded ones " +
                    "regroup; WorldExplorer puts all seven in");
        Assert.That(BgdaExecutable.EnemyAiPartNames(0x7F), Is.EqualTo("smarter targeting, surround the hero, raise the alarm, casters cast more, " +
                                                                      "brutes knock you back, drow blink and wounded ones regroup"));

        // alone: no spell, so the level-up screen, the HUD and the cast table are the game's
        var exe = Stock();
        exe.SetBorrowedSpells(NoSpells(), FeatPatch.EnemyAiParts);
        var bytes = Bytes(exe, "ai");
        Assert.That(Md5(bytes), Is.EqualTo(EnemyAiMd5), "featpatch.py --spells none");
        Assert.That(bytes.Length, Is.EqualTo(_stockBytes.Length), "the file doesn't grow");
        var reopened = OpenBytes(bytes, "ai2");
        Assert.That(reopened.GetBorrowedSpellsState(out var detail), Is.EqualTo(BorrowedSpellsState.On), detail);
        Assert.That(detail, Does.Contain("Improved Enemy AI alone"));
        Assert.That(reopened.GetBorrowedSpells(), Is.Empty);
        Assert.That(reopened.GetEnemyAi(), Is.EqualTo(FeatPatch.EnemyAiParts));
        Assert.That(Stock().GetEnemyAi(), Is.EqualTo(0), "the stock file: not in");

        // its hooks: its seventeen words; none of the spells' hooks
        Assert.That(EnemyAiHooks.Select(va => BitConverter.ToUInt32(_stockBytes, va - VaToFile)), Is.EqualTo(EnemyAiGameWords), "(the game's)");
        uint Word(int va) => BitConverter.ToUInt32(bytes, va - VaToFile);
        foreach (var va in new[] { 0x001182C0, 0x001185D8, 0x0011C168, 0x00129540, 0x00129778 })
            Assert.That((Word(va) >> 26, InUsed((Word(va) & 0x03FFFFFF) << 2), Word(va + 4)), Is.EqualTo((2u, true, 0u)), $"0x{va:X8}: j into the pool, then a nop");
        foreach (var va in new[] { 0x0019A464, 0x001A1134, 0x001BCBFC, 0x001DBFE0 })
            Assert.That((Word(va) >> 26, InUsed((Word(va) & 0x03FFFFFF) << 2)), Is.EqualTo((2u, true)), $"0x{va:X8} (an odds or a wait): j into the pool");
        Assert.That((Word(0x00129C14) >> 26, InUsed((Word(0x00129C14) & 0x03FFFFFF) << 2)), Is.EqualTo((3u, true)), "the melee blow's hit: jal into the pool");
        foreach (var va in new[] { 0x0031AE64, 0x0031B794 })
            Assert.That(InUsed(Word(va)), Is.True, $"0x{va:X8} (the drow's thinking in their vtable): the pool's");
        Assert.That(Hooks.All(va => BitConverter.ToUInt32(bytes, va - VaToFile) == BitConverter.ToUInt32(_stockBytes, va - VaToFile)), Is.True,
                    "the spells' hooks the game's");
        var changed = Enumerable.Range(0, bytes.Length).Where(i => bytes[i] != _stockBytes[i]).Select(i => (uint)(i + VaToFile)).ToList();
        Assert.That(changed.All(va => InUsed(va) || EnemyAiHooks.Any(h => h <= va && va < h + 4)), Is.True, "nothing else outside the pool");
        var table = FeatPatch.CastTableVa - VaToFile;
        Assert.That(Enumerable.Range(23, 19).Select(t => BitConverter.ToUInt32(bytes, table + 4 * t)).Distinct(), Is.EqualTo(new[] { FeatPatch.CastDefault }),
                    "types 24-42 the default: no spell");

        // the ninth block: its mark, version 22, on with its seven parts, its numbers, the eight directions and the 32 kinds
        Assert.That(FeatPatch.Data9Va, Is.EqualTo(0x00206460), "the start of the pool's stretch 38");
        Assert.That(InUsed((uint)FeatPatch.Data9Va) && InUsed((uint)(FeatPatch.Data9Va + FeatPatch.Data9Size - 1)), Is.True);
        var data9 = FeatPatch.Data9Va - VaToFile;
        Assert.That(Encoding.ASCII.GetString(bytes, data9, 4), Is.EqualTo("FEA9"));
        Assert.That((BitConverter.ToInt32(bytes, data9 + 4), FeatPatch.Version, FeatPatch.Data9Size), Is.EqualTo((22, 22, 0x3A0)));
        Assert.That((BitConverter.ToInt32(bytes, data9 + FeatPatch.EnemyAiOnAt), BitConverter.ToInt32(bytes, data9 + FeatPatch.EnemyAiPartsAt)),
                    Is.EqualTo((1, 0x7F)), "on, all seven parts");
        Assert.That((BitConverter.ToInt32(bytes, data9 + 0x10), BitConverter.ToSingle(bytes, data9 + 0x14), BitConverter.ToSingle(bytes, data9 + 0x18),
                     BitConverter.ToSingle(bytes, data9 + 0x1C)), Is.EqualTo((100, 2.25f, 0.49f, 30f)),
                    "targeting: hurt within 100 frames, its attacker if within 1.5 times as far; a hero 0.7 times as far; 30 past touching is a fight");
        Assert.That(new[] { 0x20, 0x24, 0x28, 0x2C, 0x30, 0x34 }.Select(o => BitConverter.ToSingle(bytes, data9 + o)),
                    Is.EqualTo(new[] { 200f, 24f, 12f, 20f, 48f, 2f }),
                    "surround: within 200 of him, 24 past touching; its place 12 out from him; there within 20, away beyond 48; its stop 2");
        Assert.That((BitConverter.ToInt32(bytes, data9 + 0x38), BitConverter.ToSingle(bytes, data9 + 0x3C), BitConverter.ToSingle(bytes, data9 + 0x40),
                     BitConverter.ToInt32(bytes, data9 + 0x44), BitConverter.ToSingle(bytes, data9 + 0x48)), Is.EqualTo((50, 400f, 400f, 500, 1000f)),
                    "a place 50 frames old is free; the alarm heard within 400, a shout as far; 500 frames quiet after; a hero within 1000 of the noise");
        for (var k = 0; k < 8; k++)
        {
            var (x, y) = (BitConverter.ToSingle(bytes, data9 + 0x50 + 8 * k), BitConverter.ToSingle(bytes, data9 + 0x54 + 8 * k));
            Assert.That(Math.Abs(x - Math.Cos(k * Math.PI / 4)) < 1e-6 && Math.Abs(y - Math.Sin(k * Math.PI / 4)) < 1e-6, Is.True, $"direction {k}: {x}, {y}");
        }
        var kinds = Enumerable.Range(0, 32).Select(i => BitConverter.ToUInt32(bytes, data9 + 0x90 + 4 * i)).ToList();
        Assert.That(kinds.Distinct().Count(), Is.EqualTo(32));
        Assert.That(kinds.Contains(0x0031BF60u) && kinds.Contains(0x003132E0u), Is.True, "the gnoll's and the kobold's vtables among them");
        Assert.That(kinds.Contains(0x0031C4C8u), Is.False, "not the beholder's (a boss)");
        Assert.That(BitConverter.ToUInt32(bytes, data9 + 0x90 + 4 * 32), Is.EqualTo(0u), "the list's end");
        Assert.That((BitConverter.ToUInt32(bytes, data9 + 0x114), BitConverter.ToUInt32(bytes, data9 + 0x118)), Is.EqualTo((0x4A1C0417u, 0x201000u)),
                    "the brutes (a bit for each of the 32 kinds) and the bits their blow gets (knocked back, strength 100)");
        Assert.That((BitConverter.ToInt32(bytes, data9 + 0x11C), BitConverter.ToSingle(bytes, data9 + 0x120), BitConverter.ToSingle(bytes, data9 + 0x124)),
                    Is.EqualTo((200, 450f, 180f)), "the drow: 200 frames between blinks, a warrior from as far as 450, the others 180 away");
        Assert.That((BitConverter.ToInt32(bytes, data9 + 0x128), BitConverter.ToInt32(bytes, data9 + 0x12C)), Is.EqualTo((64, 10)),
                    "the casters' waits x 64 / 128; the fire elemental's fireball 1 in 10");
        Assert.That(bytes.Skip(data9 + FeatPatch.EnemyAiRunTimeAt).Take(FeatPatch.Data9Size - FeatPatch.EnemyAiRunTimeAt).All(b => b == 0), Is.True,
                    "its run-time fields (the walk's target, the places taken round each hero, who shouted when, the last blink) zero");

        // the tenth block: its mark, version 22, the wounded monsters' numbers, their records zero; the same with the AI off
        Assert.That(FeatPatch.Data10Va, Is.EqualTo(0x0020FCE0), "the start of the pool's stretch 53");
        Assert.That(InUsed((uint)FeatPatch.Data10Va) && InUsed((uint)(FeatPatch.Data10Va + FeatPatch.Data10Size - 1)), Is.True);
        var data10 = FeatPatch.Data10Va - VaToFile;
        Assert.That((Encoding.ASCII.GetString(bytes, data10, 4), BitConverter.ToInt32(bytes, data10 + 4), FeatPatch.Data10Size), Is.EqualTo(("FEAA", 22, 0x240)));
        Assert.That((BitConverter.ToInt32(bytes, data10 + 8), BitConverter.ToUInt32(bytes, data10 + 0xC), BitConverter.ToInt32(bytes, data10 + 0x10),
                     BitConverter.ToSingle(bytes, data10 + 0x14), BitConverter.ToSingle(bytes, data10 + 0x18), BitConverter.ToSingle(bytes, data10 + 0x1C),
                     BitConverter.ToInt32(bytes, data10 + 0x20)), Is.EqualTo((40, 0x240BC008u, 200, 400f, 24f, 16f, 1500)),
                    "40% of its health; the kinds (bugbear, gnoll, kobold, kobold shaman, lizardman, ogre, thief, wolf); 200 frames back; a friend " +
                    "within 400, 24 behind it, there within 16; a record free after 1500 frames");
        Assert.That(bytes.Skip(data10 + FeatPatch.Data10RunTimeAt).Take(FeatPatch.Data10Size - FeatPatch.Data10RunTimeAt).All(b => b == 0), Is.True,
                    "its 16 records (run time) zero");

        // taken out: the stock file
        reopened.RemoveBorrowedSpells();
        Assert.That(Bytes(reopened, "ai-out"), Is.EqualTo(_stockBytes), "taken out: the stock file, byte for byte");

        // with all fifteen: featpatch.py's file; the AI taken out keeping them, put back, then the spells taken out keeping it
        exe = Stock();
        exe.SetBorrowedSpells(Defaults(BorrowedSpells.All), FeatPatch.EnemyAiParts);
        var all = Bytes(exe, "ai-all");
        Assert.That(Md5(all), Is.EqualTo(AllWithEnemyAiMd5), "featpatch.py with no options: all fifteen and Improved Enemy AI");
        reopened = OpenBytes(all, "ai-all2");
        Assert.That(reopened.GetBorrowedSpellsState(out detail), Is.EqualTo(BorrowedSpellsState.On), detail);
        Assert.That(detail, Does.Contain("; Improved Enemy AI"));
        AssertSame(reopened.GetBorrowedSpells(), Defaults(BorrowedSpells.All));
        Assert.That(reopened.GetEnemyAi(), Is.EqualTo(FeatPatch.EnemyAiParts));
        reopened.SetBorrowedSpells(reopened.GetBorrowedSpells());
        Assert.That(Md5(Bytes(reopened, "ai-all-out")), Is.EqualTo(AllFifteenMd5), "the AI taken out, the fifteen kept: featpatch.py --ai off");
        Assert.That(reopened.GetEnemyAi(), Is.EqualTo(0));
        reopened.SetBorrowedSpells(reopened.GetBorrowedSpells(), FeatPatch.EnemyAiParts);
        Assert.That(Bytes(reopened, "ai-all-back"), Is.EqualTo(all), "and put back");
        reopened.SetBorrowedSpells(NoSpells(), reopened.GetEnemyAi());
        Assert.That(Md5(Bytes(reopened, "ai-alone-again")), Is.EqualTo(EnemyAiMd5), "the fifteen taken out, the AI kept");

        // a spell without it: the ninth block there, off, the rest of it the same; its hooks the game's
        var off = Stock();
        off.SetBorrowedSpells(Defaults(new[] { BorrowedSpell.XantamOrbs }));
        var orbs = Bytes(off, "ai-off");
        Assert.That(off.GetEnemyAi(), Is.EqualTo(0));
        Assert.That((Encoding.ASCII.GetString(orbs, data9, 4), BitConverter.ToInt32(orbs, data9 + FeatPatch.EnemyAiOnAt),
                     BitConverter.ToInt32(orbs, data9 + FeatPatch.EnemyAiPartsAt)), Is.EqualTo(("FEA9", 0, 0)));
        Assert.That(orbs.Skip(data9 + 0x10).Take(FeatPatch.Data9Size - 0x10), Is.EqualTo(bytes.Skip(data9 + 0x10).Take(FeatPatch.Data9Size - 0x10)));
        Assert.That(orbs.Skip(data10).Take(FeatPatch.Data10Size), Is.EqualTo(bytes.Skip(data10).Take(FeatPatch.Data10Size)), "the tenth block the same");
        Assert.That(EnemyAiHooks.Select(va => BitConverter.ToUInt32(orbs, va - VaToFile)), Is.EqualTo(EnemyAiGameWords));

        // featpatch.py's choice of parts: read, and kept
        exe = Stock();
        exe.SetBorrowedSpells(NoSpells(), FeatPatch.EnemyAiTargeting);
        var targeting = Bytes(exe, "ai-targeting");
        Assert.That(Md5(targeting), Is.EqualTo(EnemyAiTargetingMd5), "featpatch.py --spells none --ai-parts targeting");
        reopened = OpenBytes(targeting, "ai-targeting2");
        Assert.That(reopened.GetBorrowedSpellsState(out detail), Is.EqualTo(BorrowedSpellsState.On), detail);
        Assert.That(reopened.GetEnemyAi(), Is.EqualTo(FeatPatch.EnemyAiTargeting));
        Assert.That(EnemyAiHooks.Where(va => BitConverter.ToUInt32(targeting, va - VaToFile) != BitConverter.ToUInt32(_stockBytes, va - VaToFile)),
                    Is.EqualTo(EnemyAiHooks), "the same seventeen hooks (each part is switched in the pool)");

        // parts it hasn't: refused, nothing written
        foreach (var parts in new[] { 0x80, -1, 0x97 })
        {
            var e = Stock();
            Assert.Throws<ArgumentException>(() => e.SetBorrowedSpells(NoSpells(), parts));
            Assert.That(Bytes(e, "ai-bad"), Is.EqualTo(_stockBytes));
        }

        // a ninth block that isn't WorldExplorer's: another version, on without parts, a part it hasn't, on as 2, a number
        // changed, a run-time field set; or off with parts
        foreach (var (at, value) in new[] { (4, 21), (FeatPatch.EnemyAiPartsAt, 0), (FeatPatch.EnemyAiPartsAt, 0xFF), (FeatPatch.EnemyAiOnAt, 2),
                                            (0x10, 99), (0x128, 128), (FeatPatch.EnemyAiRunTimeAt, 1), (0x390, 1) })
        {
            var unmarked = (byte[])bytes.Clone();
            BitConverter.GetBytes(value).CopyTo(unmarked, data9 + at);
            Assert.That(OpenBytes(unmarked, "ai-unmarked").GetBorrowedSpellsState(out detail), Is.EqualTo(BorrowedSpellsState.Unrecognized),
                        $"+0x{at:X} = {value}: {detail}");
        }
        var offWithParts = (byte[])orbs.Clone();
        BitConverter.GetBytes(7).CopyTo(offWithParts, data9 + FeatPatch.EnemyAiPartsAt);
        Assert.That(OpenBytes(offWithParts, "ai-off-parts").GetBorrowedSpellsState(), Is.EqualTo(BorrowedSpellsState.Unrecognized));
        // a tenth block that isn't WorldExplorer's: another version, a number changed, a record set
        foreach (var (at, value) in new[] { (4, 21), (8, 50), (0xC, 0x240BC009), (FeatPatch.Data10RunTimeAt, 1) })
        {
            var unmarked = (byte[])bytes.Clone();
            BitConverter.GetBytes(value).CopyTo(unmarked, data10 + at);
            Assert.That(OpenBytes(unmarked, "ai10-unmarked").GetBorrowedSpellsState(out detail), Is.EqualTo(BorrowedSpellsState.Unrecognized),
                        $"+0x{at:X} = {value}: {detail}");
        }

        // the game code it relies on changed (the target finding's frame, the leash check it goes on to, the alert's call of each
        // object's handler, the walk's line test, the damage's third word, the area search, the gnoll's turn in its vtable): not the
        // game's
        foreach (var va in new[] { 0x001182C8, 0x0011884C, 0x00129730, 0x0011C364, 0x00129548, 0x00108B88, 0x0031BFEC })
        {
            var game = (byte[])_stockBytes.Clone();
            game[va - VaToFile] ^= 4;
            Assert.That(OpenBytes(game, "ai-game").GetBorrowedSpellsState(out detail), Is.EqualTo(BorrowedSpellsState.Unrecognized), $"0x{va:X8}");
            Assert.That(detail, Does.Contain($"0x{va:X8}"));
        }

        // three players turned on and off underneath it
        exe = OpenBytes(all, "ai-three");
        exe.EnableThreePlayers();
        Assert.That(Md5(Bytes(exe, "ai-three2")), Is.EqualTo(ThreePlayersAiMd5), "featpatch.py's file on ADRIANNA26");
        Assert.That((exe.GetBorrowedSpellsState(), exe.GetEnemyAi(), exe.GetThreePlayerState()),
                    Is.EqualTo((BorrowedSpellsState.On, FeatPatch.EnemyAiParts, ThreePlayerState.On)));
        exe.DisableThreePlayers();
        Assert.That(Bytes(exe, "ai-three-off"), Is.EqualTo(all), "turned off again: the stock file's");
    }

    [Test]
    public void Shatter_IsARingOfFrostThatFreezesTheMonstersRoundHer_WithItsSettingsInAnEighthBlock()
    {
        RequireStock();
        var sh = BorrowedSpell.Shatter;
        var info = BorrowedSpells.Info(sh);
        Assert.That((info.Bit, info.CastType, info.GameFeat, info.GameRecord, info.Passive, info.HasMana, info.HasSecond, info.HasThird,
                     info.Range, info.Hero, info.More.Count),
                    Is.EqualTo((53, 42, false, false, false, true, true, false, true, "Adrianna", 3)));
        Assert.That((info.FixedRanks, info.FirstMin, info.FirstMax, info.SecondMin, info.SecondMax), Is.EqualTo((5, 0, 255, 0, 255)),
                    "5 ranks, the least and most damage 0 to 255");
        Assert.That(info.More.Select(v => (v.Min, v.Max, v.Tenths, v.AtMost, v.Step)),
                    Is.EqualTo(new[] { (10, 255, false, -1, 1), (1, 255, true, -1, 1), (0, 255, true, -1, 1) }),
                    "a reach of 10 to 255; frozen for 0.1 to 25.5 seconds and a cooldown of 0 to 25.5 seconds, in tenths");
        Assert.That((info.HasOwnTexts, info.UsesGameRecord, BorrowedSpellIcons.HasOwnIcon(sh)), Is.EqualTo((true, false, true)),
                    "its own record, texts and icon");
        Assert.That(info.Default.Equals(new BorrowedSpellSettings(12, 10f, new[] { 1, 2, 3, 4, 5 }, new[] { 3, 4, 5, 6, 8 },
                        More: new IReadOnlyList<int>[] { new[] { 60, 70, 80, 90, 100 }, new[] { 10, 15, 20, 25, 30 }, new[] { 20, 25, 30, 35, 40 } })),
                    Is.True, "price 12, 10 mana; 1-3 damage, a reach of 60, frozen 1 second and a cooldown of 2 at rank 1, up to 5-8, 100, 3 and 4 at rank 5");
        Assert.That(BorrowedSpellTexts.NameKey(sh), Is.EqualTo("shatter_name"));
        Assert.That(BorrowedSpellTexts.Of(sh).Select(t => (t.Language, t.Key)),
                    Is.EqualTo(Enumerable.Range(0, 5).SelectMany(l => new[] { (l, "shatter_name"), (l, "shatter_desc"), (l, "shatter_hud") })));
        Assert.That(BorrowedSpellTexts.Of(sh).Where(t => t.Key == "shatter_name").Select(t => t.Text),
                    Is.EqualTo(new[] { "Shatter", "Fracassement", "Zerschmettern", "Frantumare", "Estallar" }));
        Assert.That(BorrowedSpellTexts.Of(sh).Where(t => t.Key == "shatter_hud").Select(t => t.Text),
                    Is.EqualTo(new[]
                    {
                        "Reach %d, frozen for %d.%d seconds", "Portée %d, gel de %d,%d secondes", "Reichweite %d, %d,%d Sekunden eingefroren",
                        "Portata %d, congelati per %d,%d secondi", "Alcance %d, congelados %d,%d segundos"
                    }), "its own HUD line: the rank's reach, then its freeze in seconds and tenths");
        Assert.That(BorrowedSpellIcons.EntryName(sh), Is.EqualTo("icon_shatter.tex"));
        Assert.That(BorrowedSpellIcons.FallbackName(sh), Is.EqualTo("icon_coneofcold.tex (Cone of Cold's)"));

        // its own settings: featpatch.py's file, read back
        var exe = Stock();
        exe.SetBorrowedSpells(new Dictionary<BorrowedSpell, BorrowedSpellSettings> { [sh] = ShatterCustom });
        var bytes = Bytes(exe, "shatter-custom");
        Assert.That(Md5(bytes), Is.EqualTo(ShatterCustomMd5));
        var reopened = OpenBytes(bytes, "shatter-custom2");
        Assert.That(reopened.GetBorrowedSpellsState(out var detail), Is.EqualTo(BorrowedSpellsState.On), detail);
        var read = reopened.GetBorrowedSpells();
        Assert.That(read.Keys, Is.EqualTo(new[] { sh }));
        Assert.That(read[sh].Equals(ShatterCustom), Is.True, "read back: 0-0 to 0-255, a reach of 10 to 255, 0.1 to 25.5 s, 0 to 25.5 s");

        // its hooks: the six every spell needs, the HUD's three and its own one, the status effects' way out, now a jump into the pool
        var hooks = Hooks.Where(va => BitConverter.ToUInt32(bytes, va - VaToFile) != BitConverter.ToUInt32(_stockBytes, va - VaToFile)).ToList();
        Assert.That(hooks, Is.EqualTo(OrbsHooks.Concat(ShatterOnly).OrderBy(va => va)));
        var thaw = BitConverter.ToUInt32(bytes, 0x0011B7D8 - VaToFile);
        Assert.That((thaw >> 26, InUsed((thaw & 0x03FFFFFF) << 2)), Is.EqualTo((2u, true)), "j into the pool");
        Assert.That(BitConverter.ToUInt32(_stockBytes, 0x0011B7D8 - VaToFile), Is.EqualTo(0xDFB10030u), "(the game's ld s1, 0x30(sp))");

        // the cast table: type 42 its case, in the pool; type 41 (Phase Axe's, off) the default
        var table = FeatPatch.CastTableVa - VaToFile;
        var cast = BitConverter.ToUInt32(bytes, table + 4 * 41);
        Assert.That(FeatPatch.CastTypes, Is.EqualTo(42));
        Assert.That(InUsed(cast) && cast == BorrowedSpells.Data(sh).Cast, Is.True, "type 42: its cast case, in the pool");
        Assert.That(BitConverter.ToUInt32(bytes, table + 4 * 40), Is.EqualTo(FeatPatch.CastDefault));

        // the main block names it (its bit in the mask) but has no entry for it; the eighth block has its entry, record and settings
        var data = FeatPatch.DataVa - VaToFile;
        Assert.That(BitConverter.ToUInt32(bytes, data + FeatPatch.SpellsAt), Is.EqualTo(0x4000u), "its bit in the main block's mask");
        Assert.That((BitConverter.ToInt32(bytes, data + FeatPatch.CountAt), BitConverter.ToInt32(bytes, data + FeatPatch.AbilityCountAt)),
                    Is.EqualTo((0, 0)), "no entry or record there");
        Assert.That(FeatPatch.Data8Va, Is.EqualTo(0x0020F6A8), "the start of the pool's stretch 52 (stretch 51 hasn't the room left)");
        Assert.That(InUsed((uint)FeatPatch.Data8Va) && InUsed((uint)(FeatPatch.Data8Va + FeatPatch.Data8Size - 1)), Is.True);
        var data8 = FeatPatch.Data8Va - VaToFile;
        Assert.That(Encoding.ASCII.GetString(bytes, data8, 4), Is.EqualTo("FEA8"));
        Assert.That((BitConverter.ToInt32(bytes, data8 + 4), FeatPatch.Version), Is.EqualTo((22, 22)));
        Assert.That((BitConverter.ToInt32(bytes, data8 + FeatPatch.Data3CountAt), BitConverter.ToInt32(bytes, data8 + FeatPatch.Data3AbilityCountAt)),
                    Is.EqualTo((1, 1)), "its list entry and its ability record, where the third block has Insect Plague's");
        Assert.That((bytes[data8 + FeatPatch.Data3EntryAt + FeatPatch.EntryPriceAt], bytes[data8 + FeatPatch.Data3EntryAt + FeatPatch.EntryRanksAt]),
                    Is.EqualTo(((byte)4, (byte)5)), "price 4; its entry offers 5 ranks");
        var record = data8 + FeatPatch.Data3AbilityAt;
        Assert.That(Encoding.ASCII.GetString(bytes, record, 8), Is.EqualTo("shatter\0"));
        Assert.That((BitConverter.ToInt16(bytes, record + 0x20), BitConverter.ToUInt64(bytes, record + 0x48)), Is.EqualTo(((short)42, 1UL << 53)),
                    "type 42, bit 53");
        Assert.That(BitConverter.ToSingle(bytes, record + FeatPatch.AbilityManaAt), Is.EqualTo(6.5f));
        Assert.That((BitConverter.ToUInt16(bytes, record + FeatPatch.AbilityDamageAt), BitConverter.ToUInt16(bytes, record + FeatPatch.AbilityDamageAt + 2)),
                    Is.EqualTo(((ushort)0, (ushort)0)), "its record's damage, rank 1's");
        Assert.That(Encoding.ASCII.GetString(bytes, data8 + 0xC0, 17), Is.EqualTo("icon_shatter.tex\0"));
        Assert.That(BitConverter.ToUInt32(bytes, data8 + 0xD8), Is.EqualTo(0x0030A9D8u), "its fallback: the game's icon_coneofcold.tex");
        Assert.That(bytes.Skip(data8 + FeatPatch.ShatterDamageMinAt).Take(8), Is.EqualTo(new byte[] { 0, 0, 1, 9, 20, 0, 0, 0 }),
                    "the least damage by rank (entry 0 is rank 0's)");
        Assert.That(bytes.Skip(data8 + FeatPatch.ShatterDamageMaxAt).Take(8), Is.EqualTo(new byte[] { 0, 0, 2, 30, 20, 255, 0, 0 }),
                    "the most damage by rank");
        Assert.That(bytes.Skip(data8 + FeatPatch.ShatterReachAt).Take(8), Is.EqualTo(new byte[] { 0, 10, 30, 100, 200, 255, 0, 0 }), "the reach");
        Assert.That(bytes.Skip(data8 + FeatPatch.ShatterFreezeAt).Take(8), Is.EqualTo(new byte[] { 0, 1, 10, 25, 100, 255, 0, 0 }),
                    "the freeze, in tenths of a second");
        Assert.That(bytes.Skip(data8 + FeatPatch.ShatterCooldownAt).Take(8), Is.EqualTo(new byte[] { 0, 0, 10, 25, 100, 255, 0, 0 }),
                    "the cooldown, in tenths of a second");
        Assert.That(Encoding.ASCII.GetString(bytes, data8 + 0x108, 12), Is.EqualTo("shatter_hud\0"), "its HUD line's key");
        Assert.That(bytes.Skip(data8 + FeatPatch.ShatterRunTimeAt).Take(FeatPatch.Data8Size - FeatPatch.ShatterRunTimeAt).All(b => b == 0), Is.True,
                    "its run-time fields (the last frame, the particle type, the holds) zero");
        Assert.That((FeatPatch.ShatterHolds, FeatPatch.ShatterReachMin, FeatPatch.ShatterReachMax), Is.EqualTo((32, 10, 255)),
                    "32 monsters held at once");

        // with all fifteen: Adrianna's five each in her list, in the order of BorrowedSpell, and read back
        exe = Stock();
        exe.SetBorrowedSpells(Defaults(AdriannaAll));
        var hers = Bytes(exe, "shatter-adrianna");
        Assert.That(Md5(hers), Is.EqualTo(AdriannaMd5), "featpatch.py --spells xantam,sorcerersblood,xantamorbs,dimensiondoor,shatter --ai off");
        reopened = OpenBytes(hers, "shatter-adrianna2");
        Assert.That(reopened.GetBorrowedSpellsState(out detail), Is.EqualTo(BorrowedSpellsState.On), detail);
        AssertSame(reopened.GetBorrowedSpells(), Defaults(AdriannaAll));

        // off: the eighth block is there with its defaults and no entry; type 42 the default; the status effects' way out the game's
        var off = Stock();
        off.SetBorrowedSpells(Defaults(new[] { BorrowedSpell.XantamOrbs }));
        var orbs = Bytes(off, "shatter-off");
        Assert.That(Encoding.ASCII.GetString(orbs, data8, 4), Is.EqualTo("FEA8"));
        Assert.That((BitConverter.ToInt32(orbs, data8 + FeatPatch.Data3CountAt), BitConverter.ToInt32(orbs, data8 + FeatPatch.Data3AbilityCountAt)),
                    Is.EqualTo((0, 0)));
        Assert.That(orbs.Skip(data8 + FeatPatch.ShatterDamageMinAt).Take(8), Is.EqualTo(new byte[] { 0, 1, 2, 3, 4, 5, 0, 0 }));
        Assert.That(orbs.Skip(data8 + FeatPatch.ShatterDamageMaxAt).Take(8), Is.EqualTo(new byte[] { 0, 3, 4, 5, 6, 8, 0, 0 }));
        Assert.That(orbs.Skip(data8 + FeatPatch.ShatterReachAt).Take(8), Is.EqualTo(new byte[] { 0, 60, 70, 80, 90, 100, 0, 0 }));
        Assert.That(orbs.Skip(data8 + FeatPatch.ShatterFreezeAt).Take(8), Is.EqualTo(new byte[] { 0, 10, 15, 20, 25, 30, 0, 0 }));
        Assert.That(orbs.Skip(data8 + FeatPatch.ShatterCooldownAt).Take(8), Is.EqualTo(new byte[] { 0, 20, 25, 30, 35, 40, 0, 0 }));
        Assert.That(BitConverter.ToUInt32(orbs, table + 4 * 41), Is.EqualTo(FeatPatch.CastDefault));
        Assert.That(BitConverter.ToUInt32(orbs, data + FeatPatch.SpellsAt) & 0x4000u, Is.EqualTo(0u));
        Assert.That(BitConverter.ToUInt32(orbs, 0x0011B7D8 - VaToFile), Is.EqualTo(0xDFB10030u));

        // an eighth block that isn't version 22's, or not saying it has its entry: not WorldExplorer's
        foreach (var (at, value) in new[] { (4, (byte)16), (FeatPatch.Data3CountAt, (byte)0) })
        {
            var unmarked = (byte[])bytes.Clone();
            unmarked[data8 + at] = value;
            Assert.That(OpenBytes(unmarked, "shatter-unmarked").GetBorrowedSpellsState(out detail), Is.EqualTo(BorrowedSpellsState.Unrecognized),
                        $"+0x{at:X}: {detail}");
        }

        // the game code it relies on changed (the status effects, the creature draw's blue tint by the chill, the particles' setup,
        // the spell start's cooldown from its slot): not the game's
        foreach (var va in new[] { 0x0011B3C8, 0x0010DD28, 0x00153F10, 0x0015E30C })
        {
            var changed = (byte[])_stockBytes.Clone();
            changed[va - VaToFile] ^= 4;
            Assert.That(OpenBytes(changed, "shatter").GetBorrowedSpellsState(out detail), Is.EqualTo(BorrowedSpellsState.Unrecognized), $"0x{va:X8}");
            Assert.That(detail, Does.Contain($"0x{va:X8}"));
        }

        // an ability with its bit or its cast type already: refused
        foreach (var (at, value) in new[] { (0x4E, (byte)0x20), (0x20, (byte)42) })
        {
            var changed = (byte[])_stockBytes.Clone();
            changed[0x0030E1F0 - VaToFile + at] = at == 0x20 ? value : (byte)(changed[0x0030E1F0 - VaToFile + at] | value);
            if (at == 0x20) changed[0x0030E1F0 - VaToFile + 0x21] = 0;
            Assert.That(OpenBytes(changed, "shatter-taken").GetBorrowedSpellsState(out detail), Is.EqualTo(BorrowedSpellsState.Unrecognized), $"+0x{at:X2}");
            Assert.That(detail, Does.Contain("Shatter"));
        }
    }

    [Test]
    public void PhaseAxe_IsAThrownAxeThatPassesThroughMonsters_WithItsSettingsInASeventhBlock()
    {
        RequireStock();
        var ax = BorrowedSpell.PhaseAxe;
        var info = BorrowedSpells.Info(ax);
        Assert.That((info.Bit, info.CastType, info.GameFeat, info.GameRecord, info.Passive, info.HasMana, info.HasSecond, info.HasThird,
                     info.Range, info.Hero, info.More.Count),
                    Is.EqualTo((52, 41, false, false, false, true, true, false, true, "Kromlech", 0)));
        Assert.That((info.FixedRanks, info.FirstMin, info.FirstMax, info.SecondMin, info.SecondMax), Is.EqualTo((5, 0, 255, 0, 255)),
                    "5 ranks, each hit's least and most damage 0 to 255");
        Assert.That((info.HasOwnTexts, info.UsesGameRecord, BorrowedSpellIcons.HasOwnIcon(ax)), Is.EqualTo((true, false, true)),
                    "its own record, texts and icon");
        Assert.That(info.Default.Equals(new BorrowedSpellSettings(12, 10f, new[] { 8, 10, 12, 14, 16 }, new[] { 12, 15, 18, 21, 24 })),
                    Is.True, "price 12, 10 mana; 8-12 a hit at rank 1 up to 16-24 at rank 5");
        Assert.That(BorrowedSpellTexts.NameKey(ax), Is.EqualTo("phaseaxe_name"));
        Assert.That(BorrowedSpellTexts.Of(ax).Select(t => (t.Language, t.Key)),
                    Is.EqualTo(Enumerable.Range(0, 5).SelectMany(l => new[] { (l, "phaseaxe_name"), (l, "phaseaxe_desc") })),
                    "no HUD line of its own: the game's \"Damage: %d-%d\"");
        Assert.That(BorrowedSpellTexts.Of(ax).Where(t => t.Key == "phaseaxe_name").Select(t => t.Text),
                    Is.EqualTo(new[] { "Phase Axe", "Hache de phase", "Phasenaxt", "Ascia di Fase", "Hacha de fase" }));
        Assert.That(BorrowedSpellIcons.EntryName(ax), Is.EqualTo("icon_phaseaxe.tex"));
        Assert.That(BorrowedSpellIcons.FallbackName(ax), Is.EqualTo("icon_morden.tex (Mordenkainen's Sword's)"));

        // its own settings: featpatch.py's file, read back
        var exe = Stock();
        exe.SetBorrowedSpells(new Dictionary<BorrowedSpell, BorrowedSpellSettings> { [ax] = AxeCustom });
        var bytes = Bytes(exe, "axe-custom");
        Assert.That(Md5(bytes), Is.EqualTo(AxeCustomMd5));
        var reopened = OpenBytes(bytes, "axe-custom2");
        Assert.That(reopened.GetBorrowedSpellsState(out var detail), Is.EqualTo(BorrowedSpellsState.On), detail);
        var read = reopened.GetBorrowedSpells();
        Assert.That(read.Keys, Is.EqualTo(new[] { ax }));
        Assert.That(read[ax].Equals(AxeCustom), Is.True, "read back: 0-0, 1-2, 9-30, 20-20 and 0-255");

        // its hooks: the six every spell needs and the HUD's three, as Xantam's Orbs alone; nothing of its own
        var hooks = Hooks.Where(va => BitConverter.ToUInt32(bytes, va - VaToFile) != BitConverter.ToUInt32(_stockBytes, va - VaToFile)).ToList();
        Assert.That(hooks, Is.EqualTo(OrbsHooks));

        // the cast table: type 41 its case, in the pool; type 40 (Expert Focus's, off) the default
        var table = FeatPatch.CastTableVa - VaToFile;
        var cast = BitConverter.ToUInt32(bytes, table + 4 * 40);
        Assert.That(FeatPatch.CastTypes, Is.EqualTo(42));
        Assert.That(InUsed(cast) && cast == BorrowedSpells.Data(ax).Cast, Is.True, "type 41: its cast case, in the pool");
        Assert.That(BitConverter.ToUInt32(bytes, table + 4 * 39), Is.EqualTo(FeatPatch.CastDefault));

        // the axe: a copy of the thieves' knives' vtable (0x00319CE8) with its own update, draw and touch, in the pool
        var vt = FeatPatch.VTables.Single(v => v.GameVa == 0x00319CE8);
        Assert.That(InUsed((uint)vt.Va) && InUsed((uint)(vt.Va + FeatPatch.VTableSize - 1)), Is.True);
        Assert.That(vt.Words.Select(w => w.At), Is.EqualTo(new[] { 0x24, 0x2C, 0x3C }), "its update, draw and touch");
        Assert.That(vt.Words.All(w => InUsed(w.Word)), Is.True, "the patch's own code");
        var knives = _stockBytes.AsSpan(0x00319CE8 - VaToFile, FeatPatch.VTableSize).ToArray();
        foreach (var (at, word) in vt.Words) BitConverter.GetBytes(word).CopyTo(knives, at);
        Assert.That(bytes.AsSpan(vt.Va - VaToFile, FeatPatch.VTableSize).ToArray(), Is.EqualTo(knives), "the knives' destructor and the rest");
        Assert.That(bytes.AsSpan(0x00319CE8 - VaToFile, FeatPatch.VTableSize).SequenceEqual(_stockBytes.AsSpan(0x00319CE8 - VaToFile, FeatPatch.VTableSize)),
                    Is.True, "the knives' own vtable is never written");

        // the main block names it (its bit in the mask) but has no entry for it; the seventh block has its entry, record and settings
        var data = FeatPatch.DataVa - VaToFile;
        Assert.That(BitConverter.ToUInt32(bytes, data + FeatPatch.SpellsAt), Is.EqualTo(0x2000u), "its bit in the main block's mask");
        Assert.That((BitConverter.ToInt32(bytes, data + FeatPatch.CountAt), BitConverter.ToInt32(bytes, data + FeatPatch.AbilityCountAt)),
                    Is.EqualTo((0, 0)), "no entry or record there");
        Assert.That(FeatPatch.Data7Va, Is.EqualTo(FeatPatch.Data6Va + FeatPatch.Data6Size), "in the pool's stretch 51, after the sixth block");
        Assert.That(InUsed((uint)FeatPatch.Data7Va) && InUsed((uint)(FeatPatch.Data7Va + FeatPatch.Data7Size - 1)), Is.True);
        var data7 = FeatPatch.Data7Va - VaToFile;
        Assert.That(Encoding.ASCII.GetString(bytes, data7, 4), Is.EqualTo("FEA7"));
        Assert.That((BitConverter.ToInt32(bytes, data7 + 4), FeatPatch.Version), Is.EqualTo((22, 22)));
        Assert.That((BitConverter.ToInt32(bytes, data7 + FeatPatch.Data3CountAt), BitConverter.ToInt32(bytes, data7 + FeatPatch.Data3AbilityCountAt)),
                    Is.EqualTo((1, 1)), "its list entry and its ability record, where the third block has Insect Plague's");
        Assert.That((bytes[data7 + FeatPatch.Data3EntryAt + FeatPatch.EntryPriceAt], bytes[data7 + FeatPatch.Data3EntryAt + FeatPatch.EntryRanksAt]),
                    Is.EqualTo(((byte)4, (byte)5)), "price 4; its entry offers 5 ranks");
        var record = data7 + FeatPatch.Data3AbilityAt;
        Assert.That(Encoding.ASCII.GetString(bytes, record, 9), Is.EqualTo("phaseaxe\0"));
        Assert.That((BitConverter.ToInt16(bytes, record + 0x20), BitConverter.ToUInt64(bytes, record + 0x48)), Is.EqualTo(((short)41, 1UL << 52)),
                    "type 41, bit 52");
        Assert.That(BitConverter.ToSingle(bytes, record + FeatPatch.AbilityManaAt), Is.EqualTo(6.5f));
        Assert.That((BitConverter.ToUInt16(bytes, record + FeatPatch.AbilityDamageAt), BitConverter.ToUInt16(bytes, record + FeatPatch.AbilityDamageAt + 2)),
                    Is.EqualTo(((ushort)0, (ushort)0)), "its record's damage, rank 1's: the HUD's line until the first cast");
        Assert.That(Encoding.ASCII.GetString(bytes, data7 + 0xC0, 18), Is.EqualTo("icon_phaseaxe.tex\0"));
        Assert.That(BitConverter.ToUInt32(bytes, data7 + 0xD8), Is.EqualTo(0x0030AA90u), "its fallback: the game's icon_morden.tex");
        Assert.That(bytes.Skip(data7 + FeatPatch.AxeDamageMinAt).Take(8), Is.EqualTo(new byte[] { 0, 0, 1, 9, 20, 0, 0, 0 }),
                    "the least damage by rank (entry 0 is rank 0's)");
        Assert.That(bytes.Skip(data7 + FeatPatch.AxeDamageMaxAt).Take(8), Is.EqualTo(new byte[] { 0, 0, 2, 30, 20, 255, 0, 0 }),
                    "the most damage by rank");
        Assert.That((BitConverter.ToSingle(bytes, data7 + FeatPatch.AxeSpeedAt), BitConverter.ToInt32(bytes, data7 + FeatPatch.AxeLifeAt),
                     BitConverter.ToInt32(bytes, data7 + FeatPatch.AxeModelAt)),
                    Is.EqualTo((300f, 67, 0x002A7A28)), "300 a second for 67 frames (400), the weapon table's axe4");
        Assert.That((FeatPatch.AxeSpeed, FeatPatch.AxeLife, FeatPatch.AxeRange, FeatPatch.AxeWeaponVa), Is.EqualTo((300f, 67, 400f, 0x002A7A28)));

        // with all five of his: Kromlech's five each in his list, in the order of BorrowedSpell, and read back
        exe = Stock();
        exe.SetBorrowedSpells(Defaults(KromlechAll));
        reopened = OpenBytes(Bytes(exe, "axe-krom"), "axe-krom2");
        Assert.That(reopened.GetBorrowedSpellsState(out detail), Is.EqualTo(BorrowedSpellsState.On), detail);
        AssertSame(reopened.GetBorrowedSpells(), Defaults(KromlechAll));

        // off: the seventh block is there with its defaults and no entry; type 41 the default; the axe's vtable copy still made
        var off = Stock();
        off.SetBorrowedSpells(Defaults(new[] { BorrowedSpell.XantamOrbs }));
        var orbs = Bytes(off, "axe-off");
        Assert.That(Encoding.ASCII.GetString(orbs, data7, 4), Is.EqualTo("FEA7"));
        Assert.That((BitConverter.ToInt32(orbs, data7 + FeatPatch.Data3CountAt), BitConverter.ToInt32(orbs, data7 + FeatPatch.Data3AbilityCountAt)),
                    Is.EqualTo((0, 0)));
        Assert.That(orbs.Skip(data7 + FeatPatch.AxeDamageMinAt).Take(8), Is.EqualTo(new byte[] { 0, 8, 10, 12, 14, 16, 0, 0 }));
        Assert.That(orbs.Skip(data7 + FeatPatch.AxeDamageMaxAt).Take(8), Is.EqualTo(new byte[] { 0, 12, 15, 18, 21, 24, 0, 0 }));
        Assert.That(BitConverter.ToUInt32(orbs, table + 4 * 40), Is.EqualTo(FeatPatch.CastDefault));
        Assert.That(BitConverter.ToUInt32(orbs, data + FeatPatch.SpellsAt) & 0x2000u, Is.EqualTo(0u));

        // a seventh block that isn't version 22's, or not saying it has its entry: not WorldExplorer's
        foreach (var (at, value) in new[] { (4, (byte)15), (FeatPatch.Data3CountAt, (byte)0) })
        {
            var unmarked = (byte[])bytes.Clone();
            unmarked[data7 + at] = value;
            Assert.That(OpenBytes(unmarked, "axe-unmarked").GetBorrowedSpellsState(out detail), Is.EqualTo(BorrowedSpellsState.Unrecognized),
                        $"+0x{at:X}: {detail}");
        }

        // the game code it relies on changed (the knives' touch, the arrow constructor's least damage, the weapon table's axe4, the
        // level's loading of the weapons' textures): not the game's
        foreach (var va in new[] { 0x00319D24, 0x0010F4FC, 0x002A7A28, 0x00183414 })
        {
            var changed = (byte[])_stockBytes.Clone();
            changed[va - VaToFile] ^= 4;
            Assert.That(OpenBytes(changed, "axe").GetBorrowedSpellsState(out detail), Is.EqualTo(BorrowedSpellsState.Unrecognized), $"0x{va:X8}");
            Assert.That(detail, Does.Contain($"0x{va:X8}"));
        }

        // an ability with its bit or its cast type already: refused
        foreach (var (at, value) in new[] { (0x4E, (byte)0x10), (0x20, (byte)41) })
        {
            var changed = (byte[])_stockBytes.Clone();
            changed[0x0030E1F0 - VaToFile + at] = at == 0x20 ? value : (byte)(changed[0x0030E1F0 - VaToFile + at] | value);
            if (at == 0x20) changed[0x0030E1F0 - VaToFile + 0x21] = 0;
            Assert.That(OpenBytes(changed, "axe-taken").GetBorrowedSpellsState(out detail), Is.EqualTo(BorrowedSpellsState.Unrecognized), $"+0x{at:X2}");
            Assert.That(detail, Does.Contain("Phase Axe"));
        }
    }

    [Test]
    public void ExpertFocus_IsPowerAttackForHisBow_WithItsSettingsInASixthBlock()
    {
        RequireStock();
        var ef = BorrowedSpell.ExpertFocus;
        var info = BorrowedSpells.Info(ef);
        Assert.That((info.Bit, info.CastType, info.GameFeat, info.GameRecord, info.Passive, info.HasMana, info.HasSecond, info.HasThird,
                     info.Range, info.Hero, info.More.Count),
                    Is.EqualTo((51, 40, false, false, false, true, false, false, false, "Vahn", 1)));
        Assert.That((info.FixedRanks, info.FirstMin, info.FirstMax, info.More[0].Min, info.More[0].Max, info.More[0].Tenths),
                    Is.EqualTo((5, 1, 200, 1, 255, true)), "5 ranks, each 1 to 200 percent for 0.1 to 25.5 seconds");
        Assert.That((info.HasOwnTexts, info.UsesGameRecord, BorrowedSpellIcons.HasOwnIcon(ef)), Is.EqualTo((true, false, true)),
                    "its own record, texts and icon");
        Assert.That(info.Default.Equals(new BorrowedSpellSettings(12, 10f, new[] { 50, 50, 50, 50, 50 }, Array.Empty<int>(),
                                                                  More: new IReadOnlyList<int>[] { new[] { 50, 75, 100, 125, 150 } })),
                    Is.True, "price 12, 10 mana; +50% for 5, 7.5, 10, 12.5 and 15 seconds");
        Assert.That(BorrowedSpellTexts.NameKey(ef), Is.EqualTo("expertfocus_name"));
        Assert.That(BorrowedSpellTexts.Of(ef).Select(t => (t.Language, t.Key)),
                    Is.EqualTo(Enumerable.Range(0, 5).SelectMany(l => new[] { (l, "expertfocus_name"), (l, "expertfocus_desc"), (l, "expertfocus_hud") })));
        Assert.That(BorrowedSpellTexts.Of(ef).Where(t => t.Key == "expertfocus_name").Select(t => t.Text),
                    Is.EqualTo(new[] { "Expert Focus", "Concentration experte", "Expertenfokus", "Concentrazione Esperta", "Concentración experta" }));
        Assert.That(BorrowedSpellTexts.Of(ef).Where(t => t.Key == "expertfocus_hud").Select(t => t.Text),
                    Is.EqualTo(new[]
                    {
                        "Arrows +%d%% damage for %d.%d seconds", "Flèches +%d%% de dégâts pendant %d,%d secondes",
                        "Pfeile +%d%% Schaden für %d,%d Sekunden", "Frecce +%d%% danni per %d,%d secondi",
                        "Flechas +%d%% de daño durante %d,%d segundos"
                    }), "its own HUD line: the rank's percentage, and its time in seconds and tenths");
        Assert.That(BorrowedSpellIcons.EntryName(ef), Is.EqualTo("icon_expertfocus.tex"));
        Assert.That(BorrowedSpellIcons.FallbackName(ef), Is.EqualTo("icon_powerattack.tex (Power Attack's)"));

        // its own settings: featpatch.py's file, read back
        var exe = Stock();
        exe.SetBorrowedSpells(new Dictionary<BorrowedSpell, BorrowedSpellSettings> { [ef] = FocusCustom });
        var bytes = Bytes(exe, "focus-custom");
        Assert.That(Md5(bytes), Is.EqualTo(FocusCustomMd5));
        var reopened = OpenBytes(bytes, "focus-custom2");
        Assert.That(reopened.GetBorrowedSpellsState(out var detail), Is.EqualTo(BorrowedSpellsState.On), detail);
        var read = reopened.GetBorrowedSpells();
        Assert.That(read.Keys, Is.EqualTo(new[] { ef }));
        Assert.That(read[ef].Equals(FocusCustom), Is.True, "read back: +1% to +200%, for 0.1 to 25.5 seconds");

        // its hooks: the six every spell needs, the HUD's three, the arrows' update and its own three, the shot's calls of the arrow
        // constructor, each now a call of the same code in the pool (which then goes on to the constructor)
        var hooks = Hooks.Where(va => BitConverter.ToUInt32(bytes, va - VaToFile) != BitConverter.ToUInt32(_stockBytes, va - VaToFile)).ToList();
        Assert.That(hooks, Is.EqualTo(OrbsHooks.Concat(FocusOnly).Append(0x0010F648).OrderBy(va => va)));
        Assert.That(FocusOnly.Select(va => BitConverter.ToUInt32(_stockBytes, va - VaToFile)).Distinct(), Is.EqualTo(new[] { 0x0C043C8Cu }),
                    "the game's: jal 0x0010F230, the arrow constructor");
        var calls = FocusOnly.Select(va => BitConverter.ToUInt32(bytes, va - VaToFile)).Distinct().ToList();
        Assert.That(calls.Count == 1 && calls[0] >> 26 == 3 && InUsed((calls[0] & 0x03FFFFFF) << 2), Is.True, "one jal, into the pool");

        // the cast table: type 40 its case, in the pool; type 39 (Dimension Door's, off) the default
        var table = FeatPatch.CastTableVa - VaToFile;
        var cast = BitConverter.ToUInt32(bytes, table + 4 * 39);
        Assert.That(FeatPatch.CastTypes, Is.EqualTo(42));
        Assert.That(InUsed(cast) && cast == BorrowedSpells.Data(ef).Cast, Is.True, "type 40: its cast case, in the pool");
        Assert.That(BitConverter.ToUInt32(bytes, table + 4 * 38), Is.EqualTo(FeatPatch.CastDefault));

        // the main block names it (its bit in the mask) but has no entry for it; the sixth block has its entry, record and settings
        var data = FeatPatch.DataVa - VaToFile;
        Assert.That(BitConverter.ToUInt32(bytes, data + FeatPatch.SpellsAt), Is.EqualTo(0x1000u), "its bit in the main block's mask");
        Assert.That((BitConverter.ToInt32(bytes, data + FeatPatch.CountAt), BitConverter.ToInt32(bytes, data + FeatPatch.AbilityCountAt)),
                    Is.EqualTo((0, 0)), "no entry or record there");
        Assert.That(FeatPatch.Data6Va, Is.EqualTo(0x0020E6E8), "the start of the pool's stretch 51 (stretch 47, the third block's, is full)");
        Assert.That(InUsed((uint)FeatPatch.Data6Va) && InUsed((uint)(FeatPatch.Data6Va + FeatPatch.Data6Size - 1)), Is.True);
        var data6 = FeatPatch.Data6Va - VaToFile;
        Assert.That(Encoding.ASCII.GetString(bytes, data6, 4), Is.EqualTo("FEA6"));
        Assert.That((BitConverter.ToInt32(bytes, data6 + 4), FeatPatch.Version), Is.EqualTo((22, 22)));
        Assert.That((BitConverter.ToInt32(bytes, data6 + FeatPatch.Data3CountAt), BitConverter.ToInt32(bytes, data6 + FeatPatch.Data3AbilityCountAt)),
                    Is.EqualTo((1, 1)), "its list entry and its ability record, where the third block has Insect Plague's");
        Assert.That((bytes[data6 + FeatPatch.Data3EntryAt + FeatPatch.EntryPriceAt], bytes[data6 + FeatPatch.Data3EntryAt + FeatPatch.EntryRanksAt]),
                    Is.EqualTo(((byte)4, (byte)5)), "price 4; its entry offers 5 ranks");
        var record = data6 + FeatPatch.Data3AbilityAt;
        Assert.That(Encoding.ASCII.GetString(bytes, record, 12), Is.EqualTo("expertfocus\0"));
        Assert.That((BitConverter.ToInt16(bytes, record + 0x20), BitConverter.ToUInt64(bytes, record + 0x48)), Is.EqualTo(((short)40, 1UL << 51)),
                    "type 40, bit 51");
        Assert.That(BitConverter.ToSingle(bytes, record + FeatPatch.AbilityManaAt), Is.EqualTo(6.5f));
        Assert.That(Encoding.ASCII.GetString(bytes, data6 + 0xC0, 21), Is.EqualTo("icon_expertfocus.tex\0"));
        Assert.That(BitConverter.ToUInt32(bytes, data6 + 0xD8), Is.EqualTo(0x0030AA78u), "its fallback: the game's icon_powerattack.tex");
        Assert.That(bytes.Skip(data6 + FeatPatch.FocusPercentAt).Take(8), Is.EqualTo(new byte[] { 0, 1, 25, 100, 150, 200, 0, 0 }),
                    "the percentages by rank (entry 0 is rank 0's)");
        Assert.That(bytes.Skip(data6 + FeatPatch.FocusTimeAt).Take(8), Is.EqualTo(new byte[] { 0, 1, 10, 25, 100, 255, 0, 0 }),
                    "the times by rank, in tenths of a second");
        Assert.That(Encoding.ASCII.GetString(bytes, data6 + FeatPatch.FocusHudKeyAt, 16), Is.EqualTo("expertfocus_hud\0"));
        Assert.That((BitConverter.ToInt32(bytes, data6 + FeatPatch.FocusLongestAt), FeatPatch.FocusLongest), Is.EqualTo((1275, 1275)),
                    "the longest focus the settings allow: 25.5 seconds, in frames");
        Assert.That(bytes.Skip(data6 + FeatPatch.FocusUntilAt).Take(0x20).All(b => b == 0), Is.True,
                    "each player's focus (the frame it ends, its percentage): none");
        Assert.That((FeatPatch.FocusUntilAt + 0x20, FeatPatch.FocusNowAt + 0x10), Is.EqualTo((FeatPatch.Data6Size, FeatPatch.Data6Size)));

        // off: the sixth block is there with its defaults and no entry; type 40 the default; the shot's calls the game's
        var off = Stock();
        off.SetBorrowedSpells(Defaults(new[] { BorrowedSpell.XantamOrbs }));
        var orbs = Bytes(off, "focus-off");
        Assert.That(Encoding.ASCII.GetString(orbs, data6, 4), Is.EqualTo("FEA6"));
        Assert.That(BitConverter.ToInt32(orbs, data6 + FeatPatch.Data3CountAt), Is.EqualTo(0));
        Assert.That(orbs.Skip(data6 + FeatPatch.FocusPercentAt).Take(8), Is.EqualTo(new byte[] { 0, 50, 50, 50, 50, 50, 0, 0 }));
        Assert.That(orbs.Skip(data6 + FeatPatch.FocusTimeAt).Take(8), Is.EqualTo(new byte[] { 0, 50, 75, 100, 125, 150, 0, 0 }));
        Assert.That(BitConverter.ToUInt32(orbs, table + 4 * 39), Is.EqualTo(FeatPatch.CastDefault));
        Assert.That(FocusOnly.Append(0x0010F648).All(va => BitConverter.ToUInt32(orbs, va - VaToFile) == BitConverter.ToUInt32(_stockBytes, va - VaToFile)),
                    Is.True);

        // a sixth block that isn't version 22's, or not saying it has its entry: not WorldExplorer's
        foreach (var (at, value) in new[] { (4, (byte)14), (FeatPatch.Data3CountAt, (byte)0) })
        {
            var unmarked = (byte[])bytes.Clone();
            unmarked[data6 + at] = value;
            Assert.That(OpenBytes(unmarked, "focus-unmarked").GetBorrowedSpellsState(out detail), Is.EqualTo(BorrowedSpellsState.Unrecognized),
                        $"+0x{at:X}: {detail}");
        }

        // the game code it relies on changed (the shot's hero in t3 for the constructor and its least damage on the stack, the arrow's
        // flight, the HUD's formatter of three values): not the game's
        foreach (var va in new[] { 0x0015F128, 0x0015F12C, 0x0010FE68, 0x0017E748 })
        {
            var changed = (byte[])_stockBytes.Clone();
            changed[va - VaToFile] ^= 4;
            Assert.That(OpenBytes(changed, "focus").GetBorrowedSpellsState(out detail), Is.EqualTo(BorrowedSpellsState.Unrecognized), $"0x{va:X8}");
            Assert.That(detail, Does.Contain($"0x{va:X8}"));
        }

        // an ability with its bit or its cast type already: refused
        foreach (var (at, value) in new[] { (0x4E, (byte)0x08), (0x20, (byte)40) })
        {
            var changed = (byte[])_stockBytes.Clone();
            changed[0x0030E1F0 - VaToFile + at] = at == 0x20 ? value : (byte)(changed[0x0030E1F0 - VaToFile + at] | value);
            if (at == 0x20) changed[0x0030E1F0 - VaToFile + 0x21] = 0;
            Assert.That(OpenBytes(changed, "focus-taken").GetBorrowedSpellsState(out detail), Is.EqualTo(BorrowedSpellsState.Unrecognized), $"+0x{at:X2}");
            Assert.That(detail, Does.Contain("Expert Focus"));
        }
    }

    [Test]
    public void DimensionDoor_IsIlivarrasTeleport_WithItsSettingsInAFifthBlock()
    {
        RequireStock();
        var dd = BorrowedSpell.DimensionDoor;
        var info = BorrowedSpells.Info(dd);
        Assert.That((info.Bit, info.CastType, info.GameFeat, info.GameRecord, info.Passive, info.HasMana, info.HasSecond, info.HasThird,
                     info.Range, info.Hero, info.More.Count),
                    Is.EqualTo((50, 39, false, false, false, true, false, false, false, "Adrianna", 0)));
        Assert.That((info.FixedRanks, info.FirstMin, info.FirstMax, info.HasOwnTexts, info.UsesGameRecord, BorrowedSpellIcons.HasOwnIcon(dd)),
                    Is.EqualTo((3, 20, 400, true, false, true)), "3 ranks, a distance of 20 to 400 each; its own record, texts and icon");
        Assert.That(BorrowedSpells.All.Where(s => s != dd).All(s => BorrowedSpells.Info(s).FixedRanks == 5), Is.True, "every other spell: 5");
        Assert.That(info.Default.Equals(new BorrowedSpellSettings(12, 10f, new[] { 100, 150, 200 }, Array.Empty<int>(), RankCount: 3)), Is.True,
                    "price 12, 10 mana; 100, 150 and 200 ahead at its 3 ranks");
        Assert.That(BorrowedSpellTexts.NameKey(dd), Is.EqualTo("dimensiondoor_name"));
        Assert.That(BorrowedSpellTexts.Of(dd).Select(t => (t.Language, t.Key)),
                    Is.EqualTo(Enumerable.Range(0, 5).SelectMany(l => new[] { (l, "dimensiondoor_name"), (l, "dimensiondoor_desc"), (l, "dimensiondoor_hud") })));
        Assert.That(BorrowedSpellTexts.Of(dd).Where(t => t.Key == "dimensiondoor_name").Select(t => t.Text),
                    Is.EqualTo(new[] { "Dimension Door", "Porte dimensionnelle", "Dimensionstür", "Porta Dimensionale", "Puerta dimensional" }));
        Assert.That(BorrowedSpellTexts.Of(dd).Where(t => t.Key == "dimensiondoor_hud").Select(t => t.Text),
                    Is.EqualTo(new[] { "Distance: %d units", "Distance : %d unités", "Entfernung: %d Einheiten", "Distanza: %d unità", "Distancia: %d unidades" }),
                    "its own HUD line: the rank's distance");
        Assert.That(BorrowedSpellIcons.EntryName(dd), Is.EqualTo("icon_dimensiondoor.tex"));
        Assert.That(BorrowedSpellIcons.FallbackName(dd), Is.EqualTo("icon_sphere.tex (Otiluke's Resilient Sphere's)"));

        // its own settings: featpatch.py's file, read back
        var exe = Stock();
        exe.SetBorrowedSpells(new Dictionary<BorrowedSpell, BorrowedSpellSettings> { [dd] = DoorCustom });
        var bytes = Bytes(exe, "door-custom");
        Assert.That(Md5(bytes), Is.EqualTo(DoorCustomMd5));
        var reopened = OpenBytes(bytes, "door-custom2");
        Assert.That(reopened.GetBorrowedSpellsState(out var detail), Is.EqualTo(BorrowedSpellsState.On), detail);
        var read = reopened.GetBorrowedSpells();
        Assert.That(read.Keys, Is.EqualTo(new[] { dd }));
        Assert.That(read[dd].Equals(DoorCustom), Is.True, "read back: 20, 50 and 400 ahead");

        // its hooks: the six every spell needs and the HUD's three, nothing of its own (the same nine as Xantam's Orbs alone)
        var hooks = Hooks.Where(va => BitConverter.ToUInt32(bytes, va - VaToFile) != BitConverter.ToUInt32(_stockBytes, va - VaToFile)).ToList();
        Assert.That(hooks, Is.EqualTo(OrbsHooks));

        // the cast table: type 39 its case, in the pool; type 38 (Poison Gas Arrow's, an arrow) the default
        var table = FeatPatch.CastTableVa - VaToFile;
        var cast = BitConverter.ToUInt32(bytes, table + 4 * 38);
        Assert.That(FeatPatch.CastTypes, Is.EqualTo(42));
        Assert.That(InUsed(cast) && cast == BorrowedSpells.Data(dd).Cast, Is.True, "type 39: its cast case, in the pool");
        Assert.That(BitConverter.ToUInt32(bytes, table + 4 * 37), Is.EqualTo(FeatPatch.CastDefault));

        // the main block names it (its bit in the mask) but has no entry for it; the fifth block has its entry, record and distances
        var data = FeatPatch.DataVa - VaToFile;
        Assert.That(BitConverter.ToUInt32(bytes, data + FeatPatch.SpellsAt), Is.EqualTo(0x800u), "its bit in the main block's mask");
        Assert.That((BitConverter.ToInt32(bytes, data + FeatPatch.CountAt), BitConverter.ToInt32(bytes, data + FeatPatch.AbilityCountAt)),
                    Is.EqualTo((0, 0)), "no entry or record there");
        Assert.That(FeatPatch.Data5Va, Is.EqualTo(FeatPatch.Data4Va + FeatPatch.Data4Size), "right after the fourth block");
        var data5 = FeatPatch.Data5Va - VaToFile;
        Assert.That(Encoding.ASCII.GetString(bytes, data5, 4), Is.EqualTo("FEA5"));
        Assert.That(BitConverter.ToInt32(bytes, data5 + 4), Is.EqualTo(FeatPatch.Version));
        Assert.That((BitConverter.ToInt32(bytes, data5 + FeatPatch.Data3CountAt), BitConverter.ToInt32(bytes, data5 + FeatPatch.Data3AbilityCountAt)),
                    Is.EqualTo((1, 1)), "its list entry and its ability record, where the third block has Insect Plague's");
        Assert.That((bytes[data5 + FeatPatch.Data3EntryAt + FeatPatch.EntryPriceAt], bytes[data5 + FeatPatch.Data3EntryAt + FeatPatch.EntryRanksAt]),
                    Is.EqualTo(((byte)4, (byte)3)), "price 4; its entry offers 3 ranks");
        var record = data5 + FeatPatch.Data3AbilityAt;
        Assert.That(Encoding.ASCII.GetString(bytes, record, 14), Is.EqualTo("dimensiondoor\0"));
        Assert.That((BitConverter.ToInt16(bytes, record + 0x20), BitConverter.ToUInt64(bytes, record + 0x48)), Is.EqualTo(((short)39, 1UL << 50)),
                    "type 39, bit 50");
        Assert.That(BitConverter.ToSingle(bytes, record + FeatPatch.AbilityManaAt), Is.EqualTo(6.5f));
        Assert.That(Encoding.ASCII.GetString(bytes, data5 + 0xC0, 23), Is.EqualTo("icon_dimensiondoor.tex\0"));
        Assert.That(BitConverter.ToUInt32(bytes, data5 + 0xD8), Is.EqualTo(0x0030AAD0u), "its fallback: the game's icon_sphere.tex");
        Assert.That(Enumerable.Range(0, 8).Select(i => (int)BitConverter.ToUInt16(bytes, data5 + FeatPatch.DoorDistanceAt + 2 * i)),
                    Is.EqualTo(new[] { 0, 20, 50, 400, 400, 400, 0, 0 }), "the distances by rank (4 and 5, which no entry offers, as 3)");
        Assert.That(Encoding.ASCII.GetString(bytes, data5 + FeatPatch.DoorHudKeyAt, 18), Is.EqualTo("dimensiondoor_hud\0"));
        Assert.That(Enumerable.Range(0, 5).Select(i => BitConverter.ToSingle(bytes, data5 + FeatPatch.DoorMinAt + 4 * i)),
                    Is.EqualTo(new[] { 16f, 8f, 12f, 32f, 2f }), "the least way worth going, a try's step back, a step up, a drop, short of a wall");
        Assert.That(BitConverter.ToInt32(bytes, data5 + FeatPatch.DoorSparksAt), Is.EqualTo(50), "Ilivarra's 50 sparks a burst");
        Assert.That((FeatPatch.DoorMin, FeatPatch.DoorStep, FeatPatch.DoorUp, FeatPatch.DoorDown, FeatPatch.DoorBack, FeatPatch.DoorSparks),
                    Is.EqualTo((16f, 8f, 12f, 32f, 2f, 50)));

        // off: the fifth block is there with its defaults and no entry; type 39 the default
        var off = Stock();
        off.SetBorrowedSpells(Defaults(new[] { BorrowedSpell.XantamOrbs }));
        var orbs = Bytes(off, "door-off");
        Assert.That(Encoding.ASCII.GetString(orbs, data5, 4), Is.EqualTo("FEA5"));
        Assert.That(BitConverter.ToInt32(orbs, data5 + FeatPatch.Data3CountAt), Is.EqualTo(0));
        Assert.That(Enumerable.Range(0, 8).Select(i => (int)BitConverter.ToUInt16(orbs, data5 + FeatPatch.DoorDistanceAt + 2 * i)),
                    Is.EqualTo(new[] { 0, 100, 150, 200, 200, 200, 0, 0 }));
        Assert.That(BitConverter.ToUInt32(orbs, table + 4 * 38), Is.EqualTo(FeatPatch.CastDefault));

        // a fifth block that isn't version 22's, or not saying it has its entry: not WorldExplorer's
        foreach (var (at, value) in new[] { (4, (byte)13), (FeatPatch.Data3CountAt, (byte)0) })
        {
            var unmarked = (byte[])bytes.Clone();
            unmarked[data5 + at] = value;
            Assert.That(OpenBytes(unmarked, "door-unmarked").GetBorrowedSpellsState(out detail), Is.EqualTo(BorrowedSpellsState.Unrecognized),
                        $"+0x{at:X}: {detail}");
        }

        // the game code it relies on changed (Ilivarra's sparkle's 50, the screen's edges' margins, the walls' sweep, the floor, a
        // model's vertex count): not the game's
        foreach (var va in new[] { 0x001DEE44, 0x00183474, 0x00148C20, 0x00147800, 0x0013FA58 })
        {
            var changed = (byte[])_stockBytes.Clone();
            changed[va - VaToFile] ^= 4;
            Assert.That(OpenBytes(changed, "door").GetBorrowedSpellsState(out detail), Is.EqualTo(BorrowedSpellsState.Unrecognized), $"0x{va:X8}");
            Assert.That(detail, Does.Contain($"0x{va:X8}"));
        }

        // an ability with its bit or its cast type already: refused
        foreach (var (at, value) in new[] { (0x4E, (byte)0x04), (0x20, (byte)39) })
        {
            var changed = (byte[])_stockBytes.Clone();
            changed[0x0030E1F0 - VaToFile + at] = at == 0x20 ? value : (byte)(changed[0x0030E1F0 - VaToFile + at] | value);
            if (at == 0x20) changed[0x0030E1F0 - VaToFile + 0x21] = 0;
            Assert.That(OpenBytes(changed, "door-taken").GetBorrowedSpellsState(out detail), Is.EqualTo(BorrowedSpellsState.Unrecognized), $"+0x{at:X2}");
            Assert.That(detail, Does.Contain("Dimension Door"));
        }
    }

    [Test]
    public void PoisonGasArrow_IsTheMonstersGasOnAnArrow_WithItsSettingsInAFourthBlock()
    {
        RequireStock();
        var pg = BorrowedSpell.PoisonGasArrow;
        var info = BorrowedSpells.Info(pg);
        Assert.That((info.Bit, info.CastType, info.GameFeat, info.GameRecord, info.Passive, info.HasMana, info.HasSecond, info.HasThird,
                     info.Range, info.Hero, info.More.Count),
                    Is.EqualTo((49, 38, false, false, false, true, true, false, true, "Vahn", 4)));
        Assert.That((info.HasOwnTexts, info.UsesGameRecord, BorrowedSpellIcons.HasOwnIcon(pg)), Is.EqualTo((true, false, true)),
                    "its own record, texts and icon");
        Assert.That(info.More.Select(v => (v.Min, v.Max, v.Tenths, v.AtMost, v.Step)),
                    Is.EqualTo(new[] { (10, 255, false, -1, 1), (1, 255, true, -1, 1), (0, 255, false, 3, 1), (0, 255, false, -1, 1) }),
                    "the cloud's reach (at least 10, the game's constructor's least), its time in tenths of a second, its hits' least and most");
        Assert.That(info.Default.Equals(new BorrowedSpellSettings(12, 10f, new[] { 8, 9, 10, 11, 12 }, new[] { 12, 14, 15, 17, 18 },
            More: new IReadOnlyList<int>[]
            {
                new[] { 40, 45, 50, 55, 60 }, new[] { 40, 45, 50, 55, 60 }, new[] { 2, 2, 3, 3, 4 }, new[] { 3, 4, 4, 5, 6 }
            })), Is.True, "price 12, 10 mana; arrows of 8-12 up to 12-18; clouds of 40 for 4 s up to 60 for 6 s; gas of 2-3 up to 4-6");
        Assert.That(BorrowedSpellTexts.NameKey(pg), Is.EqualTo("poisongasarrow_name"));
        Assert.That(BorrowedSpellTexts.Of(pg).Select(t => (t.Language, t.Key)),
                    Is.EqualTo(Enumerable.Range(0, 5).SelectMany(l => new[] { (l, "poisongasarrow_name"), (l, "poisongasarrow_desc") })));
        Assert.That(BorrowedSpellTexts.Of(pg).Where(t => t.Key == "poisongasarrow_name").Select(t => t.Text),
                    Is.EqualTo(new[] { "Poison Gas Arrow", "Flèche de gaz toxique", "Giftgaspfeil", "Freccia di Gas Velenoso", "Flecha de gas venenoso" }));
        Assert.That(BorrowedSpellIcons.EntryName(pg), Is.EqualTo("icon_poisongasarrow.tex"));
        Assert.That(BorrowedSpellIcons.FallbackName(pg), Is.EqualTo("icon_melfsacidarrow.tex (Melf's Acid Arrow's)"));

        // its own settings: featpatch.py's file, read back
        var exe = Stock();
        exe.SetBorrowedSpells(new Dictionary<BorrowedSpell, BorrowedSpellSettings> { [pg] = GasCustom });
        var bytes = Bytes(exe, "gas-custom");
        Assert.That(Md5(bytes), Is.EqualTo(GasCustomMd5));
        var reopened = OpenBytes(bytes, "gas-custom2");
        Assert.That(reopened.GetBorrowedSpellsState(out var detail), Is.EqualTo(BorrowedSpellsState.On), detail);
        var read = reopened.GetBorrowedSpells();
        Assert.That(read.Keys, Is.EqualTo(new[] { pg }));
        Assert.That(read[pg].Equals(GasCustom), Is.True, "read back: arrows of 0-255, clouds of 10-255 for 0.1-25.5 s, gas of 0-255");

        // its hooks: the six every spell needs, the HUD's three, the four arrow hooks it shares with Inferno Arrow, the poison's bite
        // it shares with Insect Plague, and its own one, the arrow types' last test (a j into the pool for the game's bnel)
        var hooks = Hooks.Where(va => BitConverter.ToUInt32(bytes, va - VaToFile) != BitConverter.ToUInt32(_stockBytes, va - VaToFile)).ToList();
        Assert.That(hooks.Count, Is.EqualTo(15));
        Assert.That(GasOnly.Concat(ArrowShared).Concat(InsectOnly).All(hooks.Contains), Is.True);
        Assert.That(new[] { 0x001109C0, 0x0015E424, 0x0016DD2C }.Any(hooks.Contains), Is.False,
                    "Inferno Arrow's own three (its touch, type 9 an arrow, icons[9]'s lookup) stay the game's");
        var types = BitConverter.ToUInt32(bytes, 0x0015E43C - VaToFile);
        var typesTo = (int)((types & 0x03FFFFFF) << 2);
        Assert.That((types >> 26, Used.Any(u => u.Start <= typesTo && typesTo < u.End)), Is.EqualTo((2u, true)), "j into the pool");
        Assert.That(BitConverter.ToUInt32(_stockBytes, 0x0015E43C - VaToFile), Is.EqualTo(0x54620078u), "(the game's bnel v1, v0)");

        // the main block names it (its bit in the mask) but has no entry for it; the fourth block has its entry, record and settings
        var data = FeatPatch.DataVa - VaToFile;
        Assert.That(BitConverter.ToUInt32(bytes, data + FeatPatch.SpellsAt), Is.EqualTo(0x400u), "its bit in the main block's mask");
        Assert.That((BitConverter.ToInt32(bytes, data + FeatPatch.CountAt), BitConverter.ToInt32(bytes, data + FeatPatch.AbilityCountAt)),
                    Is.EqualTo((0, 0)), "no entry or record there");
        Assert.That(FeatPatch.Data4Va, Is.EqualTo(FeatPatch.Data3Va + FeatPatch.Data3Size), "right after the third block");
        var data4 = FeatPatch.Data4Va - VaToFile;
        Assert.That(Encoding.ASCII.GetString(bytes, data4, 4), Is.EqualTo("FEA4"));
        Assert.That(BitConverter.ToInt32(bytes, data4 + 4), Is.EqualTo(FeatPatch.Version));
        Assert.That((BitConverter.ToInt32(bytes, data4 + FeatPatch.Data3CountAt), BitConverter.ToInt32(bytes, data4 + FeatPatch.Data3AbilityCountAt)),
                    Is.EqualTo((1, 1)), "its list entry and its ability record, where the third block has Insect Plague's");
        Assert.That(bytes[data4 + FeatPatch.Data3EntryAt + FeatPatch.EntryPriceAt], Is.EqualTo(4));
        var record = data4 + FeatPatch.Data3AbilityAt;
        Assert.That(Encoding.ASCII.GetString(bytes, record, 15), Is.EqualTo("poisongasarrow\0"));
        Assert.That((BitConverter.ToInt16(bytes, record + 0x20), BitConverter.ToUInt64(bytes, record + 0x48)), Is.EqualTo(((short)38, 1UL << 49)),
                    "type 38 (an arrow's, as the spell start sees it), bit 49");
        Assert.That(BitConverter.ToSingle(bytes, record + FeatPatch.AbilityManaAt), Is.EqualTo(6.5f));
        Assert.That(Encoding.ASCII.GetString(bytes, data4 + 0xC0, 24), Is.EqualTo("icon_poisongasarrow.tex\0"));
        Assert.That(BitConverter.ToUInt32(bytes, data4 + 0xD8), Is.EqualTo(0x0030A940u), "its fallback: the game's icon_melfsacidarrow.tex");
        Assert.That(bytes.Skip(data4 + FeatPatch.GasArrowMinAt).Take(8), Is.EqualTo(new byte[] { 0, 0, 1, 9, 20, 0, 0, 0 }));
        Assert.That(bytes.Skip(data4 + FeatPatch.GasArrowMaxAt).Take(8), Is.EqualTo(new byte[] { 0, 0, 2, 30, 20, 255, 0, 0 }));
        Assert.That(bytes.Skip(data4 + FeatPatch.GasReachAt).Take(8), Is.EqualTo(new byte[] { 0, 10, 30, 64, 100, 255, 0, 0 }));
        Assert.That(bytes.Skip(data4 + FeatPatch.GasTimeAt).Take(8), Is.EqualTo(new byte[] { 0, 1, 10, 25, 100, 255, 0, 0 }));
        Assert.That(bytes.Skip(data4 + FeatPatch.GasMinAt).Take(8), Is.EqualTo(new byte[] { 0, 0, 1, 5, 20, 0, 0, 0 }));
        Assert.That(bytes.Skip(data4 + FeatPatch.GasMaxAt).Take(8), Is.EqualTo(new byte[] { 0, 0, 2, 9, 20, 255, 0, 0 }));
        Assert.That((BitConverter.ToInt32(bytes, data4 + FeatPatch.GasPeriodAt), FeatPatch.GasPeriod, FeatPatch.GasKind),
                    Is.EqualTo((50, 50, 0x40040)), "a hit a second, the game's cloud's kind of hit (the hit function's poison)");

        // off: the fourth block is there with its defaults, and no entry; the arrow hooks as Inferno Arrow alone has them
        var off = Stock();
        off.SetBorrowedSpells(Defaults(new[] { BorrowedSpell.InfernoArrow }));
        var inferno = Bytes(off, "gas-off");
        Assert.That(Encoding.ASCII.GetString(inferno, data4, 4), Is.EqualTo("FEA4"));
        Assert.That(BitConverter.ToInt32(inferno, data4 + FeatPatch.Data3CountAt), Is.EqualTo(0));
        Assert.That(inferno.Skip(data4 + FeatPatch.GasReachAt).Take(8), Is.EqualTo(new byte[] { 0, 40, 45, 50, 55, 60, 0, 0 }));
        Assert.That(BitConverter.ToUInt32(inferno, 0x0015E43C - VaToFile), Is.EqualTo(0x54620078u), "the arrow types' last test the game's");
        Assert.That(ArrowShared.All(va => BitConverter.ToUInt32(inferno, va - VaToFile) == BitConverter.ToUInt32(bytes, va - VaToFile)), Is.True,
                    "the shared arrow hooks: the same words (the code checks which of the two is on)");

        // the poisonGasSpell vtable copy: the game's (0x00313140) but for its update (+0x24); the game's constructor, draw and
        // destructor (a cloud is not a creature: 0x80 bytes, the next class's vtable follows)
        const int gameGas = 0x00313140 - VaToFile;
        var copy = FeatPatch.VTables.Single(v => v.GameVa == 0x00313140).Va - VaToFile;
        for (var at = 0; at < 0x80; at += 4)
        {
            var word = BitConverter.ToUInt32(bytes, copy + at);
            if (at == 0x24)
            {
                Assert.That(word, Is.Not.EqualTo(BitConverter.ToUInt32(_stockBytes, gameGas + at)), "+0x24: the clouds' own update");
                Assert.That(InUsed(word), Is.True, "in the pool");
            }
            else Assert.That(word, Is.EqualTo(BitConverter.ToUInt32(_stockBytes, gameGas + at)), $"+0x{at:X2}");
        }

        Assert.That(bytes.AsSpan(gameGas, 0x80).SequenceEqual(_stockBytes.AsSpan(gameGas, 0x80)), Is.True, "the game's vtable is never written");

        // a fourth block that isn't version 22's, or not saying it has its entry: not WorldExplorer's
        foreach (var (at, value) in new[] { (4, (byte)12), (FeatPatch.Data3CountAt, (byte)0) })
        {
            var unmarked = (byte[])bytes.Clone();
            unmarked[data4 + at] = value;
            Assert.That(OpenBytes(unmarked, "gas-unmarked").GetBorrowedSpellsState(out detail), Is.EqualTo(BorrowedSpellsState.Unrecognized),
                        $"+0x{at:X}: {detail}");
        }

        // the monsters' gas code it relies on changed (its constructor, its update's rhythm, its destructor, its vtable's update): not
        // the game's
        foreach (var va in new[] { 0x001A22C4, 0x001A1DE8, 0x001A26C0, 0x00313164 })
        {
            var changed = (byte[])_stockBytes.Clone();
            changed[va - VaToFile] ^= 4;
            Assert.That(OpenBytes(changed, "gas").GetBorrowedSpellsState(out detail), Is.EqualTo(BorrowedSpellsState.Unrecognized), $"0x{va:X8}");
            Assert.That(detail, Does.Contain($"0x{va:X8}"));
        }

        // an ability with its bit or its cast type already: refused
        foreach (var (at, value) in new[] { (0x4E, (byte)0x02), (0x20, (byte)38) })
        {
            var changed = (byte[])_stockBytes.Clone();
            changed[0x0030E1F0 - VaToFile + at] = at == 0x20 ? value : (byte)(changed[0x0030E1F0 - VaToFile + at] | value);
            if (at == 0x20) changed[0x0030E1F0 - VaToFile + 0x21] = 0;
            Assert.That(OpenBytes(changed, "gas-taken").GetBorrowedSpellsState(out detail), Is.EqualTo(BorrowedSpellsState.Unrecognized), $"+0x{at:X2}");
            Assert.That(detail, Does.Contain("Poison Gas Arrow"));
        }
    }

    [Test]
    public void InfernoArrow_UsesTheGamesOwnRecord_AndBurnsWhereItStrikes()
    {
        RequireStock();
        var info = BorrowedSpells.Info(BorrowedSpell.InfernoArrow);
        Assert.That((info.Bit, info.CastType, info.GameFeat, info.GameRecord, info.Passive, info.HasMana, info.HasSecond, info.HasThird, info.Range,
                     info.Hero, info.More.Count),
                    Is.EqualTo((8, 9, false, true, false, true, true, false, true, "Vahn", 5)));
        Assert.That((info.HasOwnTexts, info.UsesGameRecord, BorrowedSpellIcons.HasOwnIcon(BorrowedSpell.InfernoArrow)), Is.EqualTo((false, true, true)),
                    "the game's record and texts, its own icon");
        Assert.That(info.More.Select(v => (v.Min, v.Max, v.Tenths, v.AtMost)),
                    Is.EqualTo(new[] { (1, 255, false, -1), (1, 255, false, -1), (1, 255, true, -1), (0, 255, false, 4), (0, 255, false, -1) }),
                    "the blast's reach, the spread, the time in tenths of a second, a burn's least (at most its most) and most");
        Assert.That(info.Default.Equals(new BorrowedSpellSettings(12, 12f, new[] { 10, 12, 14, 16, 18 }, new[] { 15, 18, 21, 24, 27 },
            More: new IReadOnlyList<int>[]
            {
                new[] { 64, 68, 72, 76, 80 }, new[] { 48, 54, 60, 66, 72 }, new[] { 20, 25, 30, 35, 40 }, new[] { 1, 1, 2, 2, 3 }, new[] { 2, 3, 3, 4, 5 }
            })), Is.True, "price 12, 12 mana; 10-15 up to 18-27; a blast of 64 up to 80; flames spreading 48 up to 72 for 2 up to 4 seconds, 1-2 up to 3-5 a burn");
        Assert.That(BorrowedSpellTexts.Of(BorrowedSpell.InfernoArrow), Is.Empty, "its texts are the game's own");
        Assert.That(BorrowedSpellTexts.NameKey(BorrowedSpell.InfernoArrow), Is.EqualTo("InfernoArrow"));
        Assert.That(BorrowedSpellIcons.EntryName(BorrowedSpell.InfernoArrow), Is.EqualTo("icon_infernoarrow.tex"));
        Assert.That(Encoding.ASCII.GetString(_stockBytes, InfernoRecordVa - VaToFile, 13), Is.EqualTo("infernoarrow\0"));
        Assert.That(BitConverter.ToInt16(_stockBytes, InfernoRecordVa - VaToFile + 0x20), Is.EqualTo(9));

        // its own settings: featpatch.py's file, read back
        var exe = Stock();
        exe.SetBorrowedSpells(new Dictionary<BorrowedSpell, BorrowedSpellSettings> { [BorrowedSpell.InfernoArrow] = InfernoCustom });
        var bytes = Bytes(exe, "inferno-custom");
        Assert.That(Md5(bytes), Is.EqualTo(InfernoCustomMd5));
        var read = OpenBytes(bytes, "inferno-custom2").GetBorrowedSpells();
        Assert.That(read.Keys, Is.EqualTo(new[] { BorrowedSpell.InfernoArrow }));
        Assert.That(read[BorrowedSpell.InfernoArrow].Equals(InfernoCustom), Is.True, "read back: 0.1 to 25.5 seconds among the rest");
        Assert.That(read[BorrowedSpell.InfernoArrow].MoreValues[2], Is.EqualTo(new[] { 1, 10, 25, 100, 255 }), "the times in tenths");

        // its hooks: the six every spell needs, the HUD's three and its own seven
        var hooks = Hooks.Where(va => BitConverter.ToUInt32(bytes, va - VaToFile) != BitConverter.ToUInt32(_stockBytes, va - VaToFile)).ToList();
        Assert.That(hooks.Count, Is.EqualTo(16));
        Assert.That(InfernoOnly.All(hooks.Contains), Is.True);
        Assert.That(BitConverter.ToUInt32(bytes, 0x0015E424 - VaToFile), Is.EqualTo(0x2C420004u), "sltiu v0, v0, 4: type 9 is an arrow");
        Assert.That(bytes.AsSpan(InfernoRecordVa - VaToFile, 0x50).SequenceEqual(_stockBytes.AsSpan(InfernoRecordVa - VaToFile, 0x50)), Is.True,
                    "the game's record is never written");

        // its data block: the mana and every table by rank (entry 0 is rank 0's), its icon's name
        var data = FeatPatch.DataVa - VaToFile;
        Assert.That(BitConverter.ToSingle(bytes, data + FeatPatch.InfernoManaAt), Is.EqualTo(6.5f));
        Assert.That(bytes.Skip(data + FeatPatch.InfernoBurnAt).Take(8), Is.EqualTo(new byte[] { 0, 1, 10, 25, 100, 255, 0, 0 }));
        Assert.That(bytes.Skip(data + FeatPatch.InfernoDamageMaxAt + 1).Take(5), Is.EqualTo(new byte[] { 3, 6, 30, 20, 255 }));
        Assert.That(Encoding.ASCII.GetString(bytes, data + FeatPatch.InfernoIconAt, 22), Is.EqualTo("icon_infernoarrow.tex\0"));

        // the flames' FireTrail vtable copy (the last of the two copies of the game's FireTrail vtable, 0x00316798; Xantam's Fire's is
        // the other): the game's but for its update (the flames' own burns, then the game's update) and its touch (none)
        const int gameTrail = 0x00316798 - VaToFile;
        var copies = FeatPatch.VTables.Where(v => v.GameVa == 0x00316798).Select(v => v.Va - VaToFile).ToList();
        Assert.That(copies.Count, Is.EqualTo(2), "Xantam's Fire's and Inferno Arrow's");
        var flames = copies[^1];
        for (var at = 0; at < 0x80; at += 4)
        {
            if (at is not (0x24 or 0x3C))
                Assert.That(BitConverter.ToUInt32(bytes, flames + at), Is.EqualTo(BitConverter.ToUInt32(_stockBytes, gameTrail + at)), $"+0x{at:X2}");
        }

        var update = BitConverter.ToUInt32(bytes, flames + 0x24);
        Assert.That(update, Is.Not.EqualTo(BitConverter.ToUInt32(_stockBytes, gameTrail + 0x24)), "its own update, not the game's");
        Assert.That(update, Is.Not.EqualTo(BitConverter.ToUInt32(bytes, copies[0] + 0x24)), "nor Xantam's Fire's");
        Assert.That(InUsed(update), Is.True, "in the pool");
        Assert.That(BitConverter.ToUInt32(bytes, flames + 0x3C), Is.EqualTo(0x0012E418u), "no touch");
        Assert.That(bytes.AsSpan(gameTrail, 0x80).SequenceEqual(_stockBytes.AsSpan(gameTrail, 0x80)), Is.True, "the game's vtable is never written");

        // the cast table: type 9 is the game's own case (the arrows')
        var table = FeatPatch.CastTableVa - VaToFile;
        Assert.That(BitConverter.ToUInt32(bytes, table + 4 * 8), Is.EqualTo(BitConverter.ToUInt32(_stockBytes, 0x00307BC0 - VaToFile + 4 * 8)));

        // the game's record changed, or another ability with its bit or cast type: refused
        foreach (var (va, at, value) in new[]
                 {
                     (InfernoRecordVa, 0x00, (byte)'I'), (InfernoRecordVa, 0x20, (byte)10), (InfernoRecordVa, 0x49, (byte)2),
                     (0x0030E1F0, 0x49, (byte)1), (0x0030E1F0, 0x20, (byte)9)
                 })
        {
            var changed = (byte[])_stockBytes.Clone();
            changed[va - VaToFile + at] = at == 0x49 ? (byte)(changed[va - VaToFile + at] | value) : value;
            if (va != InfernoRecordVa && at == 0x20) changed[va - VaToFile + 0x21] = 0;
            var other = OpenBytes(changed, "inferno-changed");
            Assert.That(other.GetBorrowedSpellsState(out var detail), Is.EqualTo(BorrowedSpellsState.Unrecognized), $"0x{va:X8} +0x{at:X2}");
            Assert.That(detail, Does.Contain("Inferno Arrow"));
        }

        // the arrow's code it relies on changed: not the game's
        foreach (var va in new[] { 0x0010F650, 0x001109D4, 0x0015E55C })
        {
            var changed = (byte[])_stockBytes.Clone();
            changed[va - VaToFile] ^= 4;
            Assert.That(OpenBytes(changed, "arrow").GetBorrowedSpellsState(out var detail), Is.EqualTo(BorrowedSpellsState.Unrecognized), $"0x{va:X8}");
            Assert.That(detail, Does.Contain($"0x{va:X8}"));
        }
    }

    [Test]
    public void XantamsOrbs_AreHisDodgeBalls_WithATouchOfTheirOwn()
    {
        RequireStock();
        var info = BorrowedSpells.Info(BorrowedSpell.XantamOrbs);
        Assert.That((info.Bit, info.CastType, info.GameFeat, info.Passive, info.HasMana, info.HasSecond, info.HasThird, info.Range, info.Hero),
                    Is.EqualTo((46, 35, false, false, true, true, true, true, "Adrianna")));
        Assert.That((info.ThirdMin, info.ThirdMax, info.FirstMax, info.SecondMax), Is.EqualTo((1, 10, 255, 255)), "1 to 10 orbs, 0 to 255 damage each");
        Assert.That(info.Default.Equals(new BorrowedSpellSettings(12, 10f, new[] { 8, 8, 8, 8, 8 }, new[] { 12, 12, 12, 12, 12 },
                                                                  Third: new[] { 1, 2, 3, 4, 5 })), Is.True,
                    "price 12, 10 mana, one orb at rank 1 up to five at rank 5, each 8-12");
        Assert.That(BorrowedSpellTexts.NameKey(BorrowedSpell.XantamOrbs), Is.EqualTo("xantamorbs_name"));
        Assert.That(BorrowedSpellIcons.EntryName(BorrowedSpell.XantamOrbs), Is.EqualTo("icon_xantamorbs.tex"));

        // its own settings: featpatch.py's file, read back
        var exe = Stock();
        exe.SetBorrowedSpells(new Dictionary<BorrowedSpell, BorrowedSpellSettings> { [BorrowedSpell.XantamOrbs] = OrbsCustom });
        var bytes = Bytes(exe, "orbs-custom");
        Assert.That(Md5(bytes), Is.EqualTo(OrbsCustomMd5));
        var read = OpenBytes(bytes, "orbs-custom2").GetBorrowedSpells();
        Assert.That(read.Keys, Is.EqualTo(new[] { BorrowedSpell.XantamOrbs }));
        Assert.That(read[BorrowedSpell.XantamOrbs].Equals(OrbsCustom), Is.True, "read back: 2, 3, 5, 7 and 10 orbs, 1-3 ... 0-255 damage");
        Assert.That(read[BorrowedSpell.XantamOrbs].Equals(OrbsCustom with { Third = new[] { 2, 3, 5, 7, 9 } }), Is.False, "the orbs count");

        // only the hooks every spell needs and the HUD's
        Assert.That(Hooks.Where(va => BitConverter.ToUInt32(bytes, va - VaToFile) != BitConverter.ToUInt32(_stockBytes, va - VaToFile)),
                    Is.EqualTo(OrbsHooks));

        // the game's DodgeBall vtable (0x0031C3C8) is never written; the copy's touch (+0x3C) is the orbs' own, every other word the game's
        int gameVt = 0x0031C3C8 - VaToFile, copyVt = FeatPatch.VTables.Single(v => v.GameVa == 0x0031C3C8).Va - VaToFile;
        Assert.That(bytes.AsSpan(gameVt, 0x80).SequenceEqual(_stockBytes.AsSpan(gameVt, 0x80)), Is.True);
        for (var at = 0; at < 0x80; at += 4)
        {
            var word = BitConverter.ToUInt32(bytes, copyVt + at);
            if (at == 0x3C)
            {
                Assert.That(word, Is.Not.EqualTo(BitConverter.ToUInt32(_stockBytes, gameVt + at)), "its touch: the orbs' own");
                Assert.That(InUsed(word), Is.True, "in the pool");
            }
            else Assert.That(word, Is.EqualTo(BitConverter.ToUInt32(_stockBytes, gameVt + at)), $"+0x{at:X2}");
        }

        // the cast table (its own piece of the pool): the game's 23 cases, the orbs' case for type 35, the default between
        var table = FeatPatch.CastTableVa - VaToFile;
        for (var t = 0; t < 23; t++)
            Assert.That(BitConverter.ToUInt32(bytes, table + 4 * t), Is.EqualTo(BitConverter.ToUInt32(_stockBytes, 0x00307BC0 - VaToFile + 4 * t)));
        Assert.That(Enumerable.Range(23, 11).Select(t => BitConverter.ToUInt32(bytes, table + 4 * t)).Distinct(), Is.EqualTo(new[] { 0x001628F0u }));
        Assert.That(BitConverter.ToUInt32(bytes, table + 4 * 34), Is.Not.EqualTo(0x001628F0u), "type 35: the orbs' cast");

        // the fan (version 9): 200 a second, the cos and sin of half of 12 degrees, the fan's point 32 behind her hand
        var data = FeatPatch.DataVa - VaToFile;
        Assert.That(BitConverter.ToSingle(bytes, data + FeatPatch.OrbsSpeedAt), Is.EqualTo(200f));
        Assert.That(BitConverter.ToSingle(bytes, data + FeatPatch.OrbsFanAt), Is.EqualTo((float)Math.Cos(Math.PI * 6 / 180)));
        Assert.That(BitConverter.ToSingle(bytes, data + FeatPatch.OrbsFanAt + 4), Is.EqualTo((float)Math.Sin(Math.PI * 6 / 180)));
        Assert.That(BitConverter.ToSingle(bytes, data + FeatPatch.OrbsApexAt), Is.EqualTo(32f));
        Assert.That((FeatPatch.OrbsSpeed, FeatPatch.OrbsFanDegrees, FeatPatch.OrbsApex), Is.EqualTo((200f, 12f, 32f)));

        // a cast table that isn't the one written, or the game's DodgeBall changed: not WorldExplorer's, or not the game's
        var table2 = (byte[])bytes.Clone();
        table2[table + 4 * 34] ^= 4;
        Assert.That(OpenBytes(table2, "orbs-table").GetBorrowedSpellsState(out var detail), Is.EqualTo(BorrowedSpellsState.Unrecognized), detail);
        foreach (var va in new[] { 0x001E83A8, 0x0031C3EC, 0x001E47B0 })   // its constructor, its vtable's update, its 500 frames
        {
            var changed = (byte[])_stockBytes.Clone();
            changed[va - VaToFile] ^= 4;
            Assert.That(OpenBytes(changed, "dodgeball").GetBorrowedSpellsState(out detail), Is.EqualTo(BorrowedSpellsState.Unrecognized), $"0x{va:X8}");
            Assert.That(detail, Does.Contain($"0x{va:X8}"));
        }

        // an ability with its bit or its cast type already: refused
        foreach (var (at, value) in new[] { (0x4D, (byte)0x40), (0x20, (byte)35) })
        {
            var changed = (byte[])_stockBytes.Clone();
            changed[0x0030E1F0 - VaToFile + at] = at == 0x20 ? value : (byte)(changed[0x0030E1F0 - VaToFile + at] | value);
            if (at == 0x20) changed[0x0030E1F0 - VaToFile + 0x21] = 0;
            Assert.That(OpenBytes(changed, "orbs-taken").GetBorrowedSpellsState(out detail), Is.EqualTo(BorrowedSpellsState.Unrecognized), $"+0x{at:X2}");
            Assert.That(detail, Does.Contain("Xantam's Orbs"));
        }
    }

    [Test]
    public void SorcerersBlood_UsesTheGamesOwnRecord_AndItsRanksAreASetting()
    {
        RequireStock();
        var info = BorrowedSpells.Info(BorrowedSpell.SorcerersBlood);
        Assert.That((info.Bit, info.CastType, info.GameFeat, info.Passive, info.HasMana, info.HasValues, info.Hero),
                    Is.EqualTo((32, 24, true, true, false, false, "Adrianna")));
        Assert.That((info.Default.Price, info.Default.RankCount, info.Default.Mana), Is.EqualTo((3, 1, 0f)), "one rank at price 3, no mana");
        Assert.That(Encoding.ASCII.GetString(_stockBytes, SorcerersRecordVa - VaToFile, 15), Is.EqualTo("SorcerersBlood\0"));
        Assert.That(BorrowedSpellTexts.Of(BorrowedSpell.SorcerersBlood), Is.Empty, "its texts are the game's own");
        Assert.That(BorrowedSpellTexts.NameKey(BorrowedSpell.SorcerersBlood), Is.EqualTo("SorcerersBlood"));
        Assert.That(BorrowedSpellIcons.HasOwnIcon(BorrowedSpell.SorcerersBlood), Is.False, "a passive feat has no HUD slot");

        // 3 ranks at price 4: featpatch.py --spells sorcerersblood --sorcerersblood-ranks 3 --sorcerersblood-price 4
        var three = new BorrowedSpellSettings(4, 0f, Array.Empty<int>(), Array.Empty<int>(), RankCount: 3);
        var exe = Stock();
        exe.SetBorrowedSpells(new Dictionary<BorrowedSpell, BorrowedSpellSettings> { [BorrowedSpell.SorcerersBlood] = three });
        var bytes = Bytes(exe, "sorcerer-3");
        Assert.That(Md5(bytes), Is.EqualTo(SorcerersThreeRanksMd5));
        var read = OpenBytes(bytes, "sorcerer-3b").GetBorrowedSpells();
        Assert.That(read[BorrowedSpell.SorcerersBlood].Equals(three), Is.True, "read back: price 4, 3 ranks");
        Assert.That(SorcerersOnly.All(va => BitConverter.ToUInt32(bytes, va - VaToFile) != BitConverter.ToUInt32(_stockBytes, va - VaToFile)), Is.True,
                    "its two hooks");
        Assert.That(bytes.AsSpan(SorcerersRecordVa - VaToFile, 0x50).SequenceEqual(_stockBytes.AsSpan(SorcerersRecordVa - VaToFile, 0x50)), Is.True,
                    "the game's record is never written");

        // a record that isn't the game's Sorcerer's Blood any more, or another ability with its bit: refused
        foreach (var (va, at, value) in new[]
                 {
                     (SorcerersRecordVa, 0x00, (byte)'s'), (SorcerersRecordVa, 0x20, (byte)23), (SorcerersRecordVa, 0x4C, (byte)2),
                     (0x0030E380, 0x4C, (byte)1)                     // Clangeddin's Fist with bit 32 too
                 })
        {
            var changed = (byte[])_stockBytes.Clone();
            changed[va - VaToFile + at] |= value;
            var other = OpenBytes(changed, "sorcerer-changed");
            Assert.That(other.GetBorrowedSpellsState(out var detail), Is.EqualTo(BorrowedSpellsState.Unrecognized), $"0x{va:X8} +0x{at:X2}");
            Assert.That(detail, Does.Contain("Sorcerer's Blood"));
        }
    }

    [Test]
    public void PowerAttack_UsesTheGamesOwnRecord()
    {
        RequireStock();
        var info = BorrowedSpells.Info(BorrowedSpell.PowerAttack);
        Assert.That((info.Bit, info.CastType, info.GameFeat, info.HasMana, info.HasSecond), Is.EqualTo((34, 18, true, false, false)));
        Assert.That(Encoding.ASCII.GetString(_stockBytes, PowerRecordVa - VaToFile, 12), Is.EqualTo("powerattack\0"));
        Assert.That(BitConverter.ToSingle(_stockBytes, PowerRecordVa - VaToFile + 0x2C), Is.EqualTo(5f), "the game's 5 mana");

        // the record is never written, and its mana (the Feats & Spells tab's) can change underneath the spells
        var exe = Stock();
        exe.SetBorrowedSpells(Defaults(BorrowedSpells.All));
        var on = Bytes(exe, "power-record");
        Assert.That(on.AsSpan(PowerRecordVa - VaToFile, 0x50).SequenceEqual(_stockBytes.AsSpan(PowerRecordVa - VaToFile, 0x50)), Is.True);
        var index = exe.GetFeatSpells().Single(f => f.Name == "powerattack").Index;
        exe.SetFeatEnergy(index, 7.5f);
        Assert.That(exe.GetBorrowedSpellsState(out var detail), Is.EqualTo(BorrowedSpellsState.On), detail);
        exe.RemoveBorrowedSpells();
        var off = Bytes(exe, "power-record-off");
        Assert.That(BitConverter.ToSingle(off, PowerRecordVa - VaToFile + 0x2C), Is.EqualTo(7.5f), "its mana stays the Feats & Spells tab's");

        // a record that isn't the game's Power Attack any more: refused
        foreach (var (at, value) in new[] { (0x00, (byte)'P'), (0x20, (byte)19), (0x4C, (byte)1) })
        {
            var changed = (byte[])_stockBytes.Clone();
            changed[PowerRecordVa - VaToFile + at] = value;
            var other = OpenBytes(changed, "power-changed");
            Assert.That(other.GetBorrowedSpellsState(out detail), Is.EqualTo(BorrowedSpellsState.Unrecognized), $"+0x{at:X2}");
            Assert.That(detail, Does.Contain("Power Attack record"));
        }
    }

    [Test]
    public void Version5_IsBroughtUpToDate_KeepingItsSettings()
    {
        RequireStock();
        var v5 = Version5();
        var exe = OpenBytes(v5, "v5");
        Assert.That(exe.GetBorrowedSpellsState(out var detail), Is.EqualTo(BorrowedSpellsState.Outdated), detail);
        Assert.That(detail, Does.Contain("version 5"));
        AssertSame(exe.GetBorrowedSpells(), Defaults(Version5Spells));

        // saved with Power Attack, Sorcerer's Blood, Xantam's Orbs, Inferno Arrow, Entangle, Insect Plague, Poison Gas Arrow,
        // Dimension Door, Expert Focus, Phase Axe and Shatter added: featpatch.py version 22's file
        var all = new Dictionary<BorrowedSpell, BorrowedSpellSettings>(exe.GetBorrowedSpells())
        {
            [BorrowedSpell.PowerAttack] = BorrowedSpells.Info(BorrowedSpell.PowerAttack).Default,
            [BorrowedSpell.SorcerersBlood] = BorrowedSpells.Info(BorrowedSpell.SorcerersBlood).Default,
            [BorrowedSpell.XantamOrbs] = BorrowedSpells.Info(BorrowedSpell.XantamOrbs).Default,
            [BorrowedSpell.InfernoArrow] = BorrowedSpells.Info(BorrowedSpell.InfernoArrow).Default,
            [BorrowedSpell.Entangle] = BorrowedSpells.Info(BorrowedSpell.Entangle).Default,
            [BorrowedSpell.InsectPlague] = BorrowedSpells.Info(BorrowedSpell.InsectPlague).Default,
            [BorrowedSpell.PoisonGasArrow] = BorrowedSpells.Info(BorrowedSpell.PoisonGasArrow).Default,
            [BorrowedSpell.DimensionDoor] = BorrowedSpells.Info(BorrowedSpell.DimensionDoor).Default,
            [BorrowedSpell.ExpertFocus] = BorrowedSpells.Info(BorrowedSpell.ExpertFocus).Default,
            [BorrowedSpell.PhaseAxe] = BorrowedSpells.Info(BorrowedSpell.PhaseAxe).Default,
            [BorrowedSpell.Shatter] = BorrowedSpells.Info(BorrowedSpell.Shatter).Default
        };
        exe.SetBorrowedSpells(all);
        Assert.That(Md5(Bytes(exe, "v5-updated")), Is.EqualTo(AllFifteenMd5));

        // its own settings (in its data block, where version 5 keeps them) are read and kept
        var custom = (byte[])v5.Clone();
        const int data = 0x00132D90 - VaToFile;
        custom[data + 0x80 + 8 + 0x42] = 3;                                // Xantam's Fire's price
        BitConverter.GetBytes(9.5f).CopyTo(custom, data + 0x200 + 0x50 + 0x2C);   // Blade Barrier's mana (the second record)
        custom[data + 0x340 + 1] = 7;                                       // Blade Barrier's least damage at rank 1
        BitConverter.GetBytes((ushort)(50 * 12)).CopyTo(custom, data + 0x360 + 2 * 5);   // Antimagic Field: 12 s at rank 5
        custom[data + 0x378 + 3] = 77;                                      // Righteous Might: 77 % less at rank 3
        var read = OpenBytes(custom, "v5-custom").GetBorrowedSpells();
        Assert.That(read[BorrowedSpell.XantamFire].Price, Is.EqualTo(3));
        Assert.That(read[BorrowedSpell.BladeBarrier].Mana, Is.EqualTo(9.5f));
        Assert.That(read[BorrowedSpell.BladeBarrier].First[0], Is.EqualTo(7));
        Assert.That(read[BorrowedSpell.AntimagicField].First[4], Is.EqualTo(12));
        Assert.That(read[BorrowedSpell.RighteousMight].Second[2], Is.EqualTo(77));
        var updated = OpenBytes(custom, "v5-custom2");
        updated.SetBorrowedSpells(read);
        var reread = OpenBytes(Bytes(updated, "v5-custom-saved"), "v5-custom-saved2");
        Assert.That(reread.GetBorrowedSpellsState(), Is.EqualTo(BorrowedSpellsState.On));
        AssertSame(reread.GetBorrowedSpells(), read);

        // taken out: the stock file
        var removed = OpenBytes(v5, "v5-out");
        removed.RemoveBorrowedSpells();
        Assert.That(Bytes(removed, "v5-out-saved"), Is.EqualTo(_stockBytes));

        // version 5 with its code changed: not recognised
        var broken = (byte[])v5.Clone();
        broken[0x00102910 + 8 - VaToFile] ^= 4;
        Assert.That(OpenBytes(broken, "v5-broken").GetBorrowedSpellsState(), Is.EqualTo(BorrowedSpellsState.Unrecognized));
    }

    [Test]
    public void Version6_IsBroughtUpToDate_KeepingItsSettings()
    {
        RequireStock();
        var v6 = Version6();
        var exe = OpenBytes(v6, "v6");
        Assert.That(exe.GetBorrowedSpellsState(out var detail), Is.EqualTo(BorrowedSpellsState.Outdated), detail);
        Assert.That(detail, Does.Contain("version 6"));
        AssertSame(exe.GetBorrowedSpells(), Defaults(Version6Spells));

        // saved with Sorcerer's Blood, Xantam's Orbs, Inferno Arrow, Entangle, Insect Plague, Poison Gas Arrow, Dimension Door,
        // Expert Focus, Phase Axe and Shatter added: featpatch.py version 22's file
        var all = new Dictionary<BorrowedSpell, BorrowedSpellSettings>(exe.GetBorrowedSpells())
        {
            [BorrowedSpell.SorcerersBlood] = BorrowedSpells.Info(BorrowedSpell.SorcerersBlood).Default,
            [BorrowedSpell.XantamOrbs] = BorrowedSpells.Info(BorrowedSpell.XantamOrbs).Default,
            [BorrowedSpell.InfernoArrow] = BorrowedSpells.Info(BorrowedSpell.InfernoArrow).Default,
            [BorrowedSpell.Entangle] = BorrowedSpells.Info(BorrowedSpell.Entangle).Default,
            [BorrowedSpell.InsectPlague] = BorrowedSpells.Info(BorrowedSpell.InsectPlague).Default,
            [BorrowedSpell.PoisonGasArrow] = BorrowedSpells.Info(BorrowedSpell.PoisonGasArrow).Default,
            [BorrowedSpell.DimensionDoor] = BorrowedSpells.Info(BorrowedSpell.DimensionDoor).Default,
            [BorrowedSpell.ExpertFocus] = BorrowedSpells.Info(BorrowedSpell.ExpertFocus).Default,
            [BorrowedSpell.PhaseAxe] = BorrowedSpells.Info(BorrowedSpell.PhaseAxe).Default,
            [BorrowedSpell.Shatter] = BorrowedSpells.Info(BorrowedSpell.Shatter).Default
        };
        exe.SetBorrowedSpells(all);
        Assert.That(Md5(Bytes(exe, "v6-updated")), Is.EqualTo(AllFifteenMd5));

        // its own settings (in its data block, where version 6 keeps them) are read and kept
        var custom = (byte[])v6.Clone();
        const int data = 0x00132D90 - VaToFile;
        custom[data + 0x80 + 0x60 + 8 + 0x42] = 2;                          // Power Attack's price (the second entry)
        custom[data + 0x3E8 + 4] = 17;                                      // Power Attack: 17 hits at rank 4
        BitConverter.GetBytes(9.5f).CopyTo(custom, data + 0x260 + 0x50 + 0x2C);   // Blade Barrier's mana (the second own record)
        custom[data + 0x3A0 + 1] = 7;                                       // Blade Barrier's least damage at rank 1
        BitConverter.GetBytes((ushort)(50 * 12)).CopyTo(custom, data + 0x3C0 + 2 * 5);   // Antimagic Field: 12 s at rank 5
        custom[data + 0x3D8 + 3] = 77;                                      // Righteous Might: 77 % less at rank 3
        var read = OpenBytes(custom, "v6-custom").GetBorrowedSpells();
        Assert.That(read[BorrowedSpell.PowerAttack].Price, Is.EqualTo(2));
        Assert.That(read[BorrowedSpell.PowerAttack].First[3], Is.EqualTo(17));
        Assert.That(read[BorrowedSpell.BladeBarrier].Mana, Is.EqualTo(9.5f));
        Assert.That(read[BorrowedSpell.BladeBarrier].First[0], Is.EqualTo(7));
        Assert.That(read[BorrowedSpell.AntimagicField].First[4], Is.EqualTo(12));
        Assert.That(read[BorrowedSpell.RighteousMight].Second[2], Is.EqualTo(77));
        var updated = OpenBytes(custom, "v6-custom2");
        updated.SetBorrowedSpells(read);
        var reread = OpenBytes(Bytes(updated, "v6-custom-saved"), "v6-custom-saved2");
        Assert.That(reread.GetBorrowedSpellsState(), Is.EqualTo(BorrowedSpellsState.On));
        AssertSame(reread.GetBorrowedSpells(), read);

        // taken out: the stock file
        var removed = OpenBytes(v6, "v6-out");
        removed.RemoveBorrowedSpells();
        Assert.That(Bytes(removed, "v6-out-saved"), Is.EqualTo(_stockBytes));

        // version 6 with its code changed: not recognised
        var broken = (byte[])v6.Clone();
        broken[0x00102910 + 8 - VaToFile] ^= 4;
        Assert.That(OpenBytes(broken, "v6-broken").GetBorrowedSpellsState(), Is.EqualTo(BorrowedSpellsState.Unrecognized));
    }

    [Test]
    public void Version21_IsBroughtUpToDate_KeepingItsSettingsAndItsChoiceOfParts()
    {
        RequireStock();
        var v21 = Version21();
        var exe = OpenBytes(v21, "v21");
        Assert.That(exe.GetBorrowedSpellsState(out var detail), Is.EqualTo(BorrowedSpellsState.Outdated), detail);
        Assert.That(detail, Does.StartWith("In as version 21 (the test of Improved Enemy AI with the drow blink): "));
        Assert.That(detail, Does.EndWith("; Improved Enemy AI. Saving brings it to version 22, with all seven parts of Improved Enemy AI; every spell keeps " +
                                         "its settings."));
        AssertSame(exe.GetBorrowedSpells(), Defaults(BorrowedSpells.All));
        Assert.That(exe.GetEnemyAi(), Is.EqualTo(FeatPatch.EnemyAiParts), "all six parts of version 21's: all seven when saved");
        Assert.That(FeatPatch.EarlierAiVersions.Select(v => (v.Version, v.AiParts)), Is.EqualTo(new[] { (21, 0x3F), (20, 0x1F), (19, 0xF), (18, 7) }));

        // saved as the tab saves it, with the AI left out, or the AI alone: featpatch.py version 22's files
        exe.SetBorrowedSpells(exe.GetBorrowedSpells(), exe.GetEnemyAi());
        Assert.That(Md5(Bytes(exe, "v21-updated")), Is.EqualTo(AllWithEnemyAiMd5));
        exe = OpenBytes(v21, "v21b");
        exe.SetBorrowedSpells(exe.GetBorrowedSpells());
        Assert.That(Md5(Bytes(exe, "v21-noai")), Is.EqualTo(AllFifteenMd5));
        exe = OpenBytes(v21, "v21c");
        exe.SetBorrowedSpells(NoSpells(), exe.GetEnemyAi());
        Assert.That(Md5(Bytes(exe, "v21-ai-alone")), Is.EqualTo(EnemyAiMd5));

        // featpatch.py's choice of fewer parts (smarter targeting and the drow blink): read, and kept
        var chosen = (byte[])v21.Clone();
        var data9 = FeatPatch.Data9Va - VaToFile;
        BitConverter.GetBytes(FeatPatch.EnemyAiTargeting | FeatPatch.EnemyAiDrow).CopyTo(chosen, data9 + FeatPatch.EnemyAiPartsAt);
        Assert.That(Md5(chosen), Is.EqualTo(Version21PartsMd5), "featpatch.py version 21 --ai-parts targeting,drow");
        exe = OpenBytes(chosen, "v21-parts");
        Assert.That(exe.GetBorrowedSpellsState(out detail), Is.EqualTo(BorrowedSpellsState.Outdated), detail);
        Assert.That(detail, Does.Contain("keeping featpatch.py's choice of Improved Enemy AI's parts (smarter targeting and drow blink)"));
        Assert.That(exe.GetEnemyAi(), Is.EqualTo(FeatPatch.EnemyAiTargeting | FeatPatch.EnemyAiDrow));
        exe.SetBorrowedSpells(exe.GetBorrowedSpells(), exe.GetEnemyAi());
        var parts = Bytes(exe, "v21-parts-saved");
        Assert.That(Md5(parts), Is.EqualTo(Version21PartsUpdatedMd5));
        Assert.That(OpenBytes(parts, "v21-parts-saved2").GetEnemyAi(), Is.EqualTo(FeatPatch.EnemyAiTargeting | FeatPatch.EnemyAiDrow));

        // taken out: the stock file
        var removed = OpenBytes(v21, "v21-out");
        removed.RemoveBorrowedSpells();
        Assert.That(Bytes(removed, "v21-out-saved"), Is.EqualTo(_stockBytes));

        // version 21 with its code changed, a part it hadn't (the regroup), a number of its ninth block changed or its eighth
        // block marked 22: not recognised
        var broken = (byte[])v21.Clone();
        broken[FeatPatch.EarlierAiVersions[0].Code[0].Va + 8 - VaToFile] ^= 4;
        Assert.That(OpenBytes(broken, "v21-broken").GetBorrowedSpellsState(), Is.EqualTo(BorrowedSpellsState.Unrecognized));
        foreach (var (at, value) in new[] { (FeatPatch.EnemyAiPartsAt, 0x7F), (0x11C, 100) })
        {
            var changed = (byte[])v21.Clone();
            BitConverter.GetBytes(value).CopyTo(changed, data9 + at);
            Assert.That(OpenBytes(changed, "v21-changed").GetBorrowedSpellsState(), Is.EqualTo(BorrowedSpellsState.Unrecognized), $"+0x{at:X} = {value}");
        }
        var marked = (byte[])v21.Clone();
        marked[FeatPatch.Data8Va - VaToFile + 4] = 22;
        Assert.That(OpenBytes(marked, "v21-marked").GetBorrowedSpellsState(), Is.EqualTo(BorrowedSpellsState.Unrecognized));

        // the word the regroup hooks (version 21 doesn't) changed: still version 21, but not updated over it; the file as it was
        var hooked = (byte[])v21.Clone();
        hooked[0x00129540 - VaToFile] ^= 1;
        exe = OpenBytes(hooked, "v21-hooked");
        Assert.That(exe.GetBorrowedSpellsState(), Is.EqualTo(BorrowedSpellsState.Outdated));
        var e2 = exe;
        var thrown = Assert.Throws<InvalidOperationException>(() => e2.SetBorrowedSpells(e2.GetBorrowedSpells(), e2.GetEnemyAi()));
        Assert.That(thrown!.Message, Does.Contain("0x00129540"));
        Assert.That(Bytes(exe, "v21-hooked-after"), Is.EqualTo(hooked));
    }

    [Test]
    public void Version17_IsBroughtUpToDate_KeepingItsSettings()
    {
        RequireStock();
        var v17 = Version17();
        var exe = OpenBytes(v17, "v17");
        Assert.That(exe.GetBorrowedSpellsState(out var detail), Is.EqualTo(BorrowedSpellsState.Outdated), detail);
        Assert.That(detail, Does.Contain("version 17"));
        Assert.That(detail, Does.Contain("Improved Enemy AI"));
        AssertSame(exe.GetBorrowedSpells(), Defaults(Version17Spells));
        Assert.That(exe.GetEnemyAi(), Is.EqualTo(0), "version 17 had no Improved Enemy AI");

        // saved as it is, with Improved Enemy AI added, or the AI alone: featpatch.py version 22's files
        exe.SetBorrowedSpells(exe.GetBorrowedSpells());
        Assert.That(Md5(Bytes(exe, "v17-updated")), Is.EqualTo(AllFifteenMd5));
        exe = OpenBytes(v17, "v17b");
        exe.SetBorrowedSpells(exe.GetBorrowedSpells(), FeatPatch.EnemyAiParts);
        Assert.That(Md5(Bytes(exe, "v17-updated-ai")), Is.EqualTo(AllWithEnemyAiMd5));
        exe = OpenBytes(v17, "v17c");
        exe.SetBorrowedSpells(NoSpells(), FeatPatch.EnemyAiParts);
        Assert.That(Md5(Bytes(exe, "v17-ai-alone")), Is.EqualTo(EnemyAiMd5));

        // its own settings (in its eight data blocks, where version 22 keeps them too, in the same layout) are read and kept
        var custom = (byte[])v17.Clone();
        var data = FeatPatch.DataVa - VaToFile;
        var data8 = FeatPatch.Data8Va - VaToFile;
        custom[data + FeatPatch.EntriesAt + FeatPatch.EntryStride + FeatPatch.EntryPriceAt] = 2;   // Power Attack's price (the second entry)
        custom[data8 + FeatPatch.ShatterReachAt + 5] = 255;   // Shatter: a reach of 255 at rank 5
        custom[data8 + FeatPatch.Data3EntryAt + FeatPatch.EntryPriceAt] = 9;   // ... price 9
        var read = OpenBytes(custom, "v17-custom").GetBorrowedSpells();
        Assert.That(read[BorrowedSpell.PowerAttack].Price, Is.EqualTo(2));
        Assert.That(read[BorrowedSpell.Shatter].MoreValues[0], Is.EqualTo(new[] { 60, 70, 80, 90, 255 }));
        Assert.That(read[BorrowedSpell.Shatter].Price, Is.EqualTo(9));
        var updated = OpenBytes(custom, "v17-custom2");
        updated.SetBorrowedSpells(read, FeatPatch.EnemyAiParts);
        var reread = OpenBytes(Bytes(updated, "v17-custom-saved"), "v17-custom-saved2");
        Assert.That(reread.GetBorrowedSpellsState(), Is.EqualTo(BorrowedSpellsState.On));
        AssertSame(reread.GetBorrowedSpells(), read);
        Assert.That(reread.GetEnemyAi(), Is.EqualTo(FeatPatch.EnemyAiParts));

        // taken out: the stock file
        var removed = OpenBytes(v17, "v17-out");
        removed.RemoveBorrowedSpells();
        Assert.That(Bytes(removed, "v17-out-saved"), Is.EqualTo(_stockBytes));

        // version 17 with its code changed, or its eighth block marked 18: not recognised
        var broken = (byte[])v17.Clone();
        broken[FeatPatch.V17Code[0].Va + 8 - VaToFile] ^= 4;
        Assert.That(OpenBytes(broken, "v17-broken").GetBorrowedSpellsState(), Is.EqualTo(BorrowedSpellsState.Unrecognized));
        var marked = (byte[])v17.Clone();
        marked[data8 + 4] = 18;
        Assert.That(OpenBytes(marked, "v17-marked").GetBorrowedSpellsState(), Is.EqualTo(BorrowedSpellsState.Unrecognized));
    }

    [Test]
    public void Version16_IsBroughtUpToDate_KeepingItsSettings()
    {
        RequireStock();
        var v16 = Version16();
        var exe = OpenBytes(v16, "v16");
        Assert.That(exe.GetBorrowedSpellsState(out var detail), Is.EqualTo(BorrowedSpellsState.Outdated), detail);
        Assert.That(detail, Does.Contain("version 16"));
        Assert.That(detail, Does.Contain("Shatter for Adrianna and Improved Enemy AI"));
        AssertSame(exe.GetBorrowedSpells(), Defaults(Version16Spells));

        // saved as it is, or with Shatter added: featpatch.py version 22's files
        exe.SetBorrowedSpells(exe.GetBorrowedSpells());
        Assert.That(Md5(Bytes(exe, "v16-updated")), Is.EqualTo(FourteenMd5));
        exe = OpenBytes(v16, "v16b");
        exe.SetBorrowedSpells(new Dictionary<BorrowedSpell, BorrowedSpellSettings>(exe.GetBorrowedSpells())
        {
            [BorrowedSpell.Shatter] = BorrowedSpells.Info(BorrowedSpell.Shatter).Default
        });
        Assert.That(Md5(Bytes(exe, "v16-updated-all")), Is.EqualTo(AllFifteenMd5));

        // its own settings (in its seven data blocks, where version 22 keeps them too, in the same layout) are read and kept
        var custom = (byte[])v16.Clone();
        var data = FeatPatch.DataVa - VaToFile;
        var data6 = FeatPatch.Data6Va - VaToFile;
        var data7 = FeatPatch.Data7Va - VaToFile;
        custom[data + FeatPatch.EntriesAt + FeatPatch.EntryStride + FeatPatch.EntryPriceAt] = 2;   // Power Attack's price (the second entry)
        custom[data6 + FeatPatch.FocusTimeAt + 5] = 255;   // Expert Focus: for 25.5 seconds at rank 5
        custom[data7 + FeatPatch.AxeDamageMinAt + 3] = 7;   // Phase Axe: 7-18 at rank 3
        custom[data7 + FeatPatch.AxeDamageMaxAt + 5] = 255;   // ... 16-255 at rank 5
        custom[data7 + FeatPatch.Data3EntryAt + FeatPatch.EntryPriceAt] = 9;   // ... price 9
        BitConverter.GetBytes(3.5f).CopyTo(custom, data7 + FeatPatch.Data3AbilityAt + FeatPatch.AbilityManaAt);   // ... 3.5 mana
        var read = OpenBytes(custom, "v16-custom").GetBorrowedSpells();
        Assert.That(read[BorrowedSpell.PowerAttack].Price, Is.EqualTo(2));
        Assert.That(read[BorrowedSpell.ExpertFocus].MoreValues[0], Is.EqualTo(new[] { 50, 75, 100, 125, 255 }));
        Assert.That(read[BorrowedSpell.PhaseAxe].First, Is.EqualTo(new[] { 8, 10, 7, 14, 16 }));
        Assert.That(read[BorrowedSpell.PhaseAxe].Second, Is.EqualTo(new[] { 12, 15, 18, 21, 255 }));
        Assert.That((read[BorrowedSpell.PhaseAxe].Price, read[BorrowedSpell.PhaseAxe].Mana), Is.EqualTo((9, 3.5f)));
        var updated = OpenBytes(custom, "v16-custom2");
        updated.SetBorrowedSpells(read);
        var reread = OpenBytes(Bytes(updated, "v16-custom-saved"), "v16-custom-saved2");
        Assert.That(reread.GetBorrowedSpellsState(), Is.EqualTo(BorrowedSpellsState.On));
        AssertSame(reread.GetBorrowedSpells(), read);

        // taken out: the stock file
        var removed = OpenBytes(v16, "v16-out");
        removed.RemoveBorrowedSpells();
        Assert.That(Bytes(removed, "v16-out-saved"), Is.EqualTo(_stockBytes));

        // version 16 with its code changed, or its seventh block marked 17: not recognised
        var broken = (byte[])v16.Clone();
        broken[FeatPatch.V16Code[0].Va + 8 - VaToFile] ^= 4;
        Assert.That(OpenBytes(broken, "v16-broken").GetBorrowedSpellsState(), Is.EqualTo(BorrowedSpellsState.Unrecognized));
        var marked = (byte[])v16.Clone();
        marked[data7 + 4] = 17;
        Assert.That(OpenBytes(marked, "v16-marked").GetBorrowedSpellsState(), Is.EqualTo(BorrowedSpellsState.Unrecognized));
    }

    [Test]
    public void Version15_IsBroughtUpToDate_KeepingItsSettings()
    {
        RequireStock();
        var v15 = Version15();
        var exe = OpenBytes(v15, "v15");
        Assert.That(exe.GetBorrowedSpellsState(out var detail), Is.EqualTo(BorrowedSpellsState.Outdated), detail);
        Assert.That(detail, Does.Contain("version 15"));
        Assert.That(detail, Does.Contain("Phase Axe for Kromlech and Shatter for Adrianna"));
        AssertSame(exe.GetBorrowedSpells(), Defaults(Version15Spells));

        // saved as it is, or with Phase Axe and Shatter added: featpatch.py version 22's files
        exe.SetBorrowedSpells(exe.GetBorrowedSpells());
        Assert.That(Md5(Bytes(exe, "v15-updated")), Is.EqualTo(ThirteenMd5));
        exe = OpenBytes(v15, "v15b");
        exe.SetBorrowedSpells(new Dictionary<BorrowedSpell, BorrowedSpellSettings>(exe.GetBorrowedSpells())
        {
            [BorrowedSpell.PhaseAxe] = BorrowedSpells.Info(BorrowedSpell.PhaseAxe).Default,
            [BorrowedSpell.Shatter] = BorrowedSpells.Info(BorrowedSpell.Shatter).Default
        });
        Assert.That(Md5(Bytes(exe, "v15-updated-all")), Is.EqualTo(AllFifteenMd5));

        // its own settings (in its six data blocks, where version 22 keeps them too, in the same layout) are read and kept
        var custom = (byte[])v15.Clone();
        var data = FeatPatch.DataVa - VaToFile;
        var data6 = FeatPatch.Data6Va - VaToFile;
        custom[data + FeatPatch.EntriesAt + FeatPatch.EntryStride + FeatPatch.EntryPriceAt] = 2;   // Power Attack's price (the second entry)
        custom[data6 + FeatPatch.FocusPercentAt + 3] = 77;   // Expert Focus: +77% at rank 3
        custom[data6 + FeatPatch.FocusTimeAt + 5] = 255;   // ... for 25.5 seconds at rank 5
        custom[data6 + FeatPatch.Data3EntryAt + FeatPatch.EntryPriceAt] = 9;   // ... price 9
        BitConverter.GetBytes(3.5f).CopyTo(custom, data6 + FeatPatch.Data3AbilityAt + FeatPatch.AbilityManaAt);   // ... 3.5 mana
        var read = OpenBytes(custom, "v15-custom").GetBorrowedSpells();
        Assert.That(read[BorrowedSpell.PowerAttack].Price, Is.EqualTo(2));
        Assert.That(read[BorrowedSpell.ExpertFocus].First, Is.EqualTo(new[] { 50, 50, 77, 50, 50 }));
        Assert.That(read[BorrowedSpell.ExpertFocus].MoreValues[0], Is.EqualTo(new[] { 50, 75, 100, 125, 255 }));
        Assert.That((read[BorrowedSpell.ExpertFocus].Price, read[BorrowedSpell.ExpertFocus].Mana), Is.EqualTo((9, 3.5f)));
        var updated = OpenBytes(custom, "v15-custom2");
        updated.SetBorrowedSpells(read);
        var reread = OpenBytes(Bytes(updated, "v15-custom-saved"), "v15-custom-saved2");
        Assert.That(reread.GetBorrowedSpellsState(), Is.EqualTo(BorrowedSpellsState.On));
        AssertSame(reread.GetBorrowedSpells(), read);

        // taken out: the stock file
        var removed = OpenBytes(v15, "v15-out");
        removed.RemoveBorrowedSpells();
        Assert.That(Bytes(removed, "v15-out-saved"), Is.EqualTo(_stockBytes));

        // version 15 with its code changed, or its sixth block marked 16 (version 16's): not recognised
        var broken = (byte[])v15.Clone();
        broken[FeatPatch.V15Code[0].Va + 8 - VaToFile] ^= 4;
        Assert.That(OpenBytes(broken, "v15-broken").GetBorrowedSpellsState(), Is.EqualTo(BorrowedSpellsState.Unrecognized));
        var marked = (byte[])v15.Clone();
        marked[data6 + 4] = 16;
        Assert.That(OpenBytes(marked, "v15-marked").GetBorrowedSpellsState(), Is.EqualTo(BorrowedSpellsState.Unrecognized));
    }

    [Test]
    public void Version14_IsBroughtUpToDate_KeepingItsSettings()
    {
        RequireStock();
        var v14 = Version14();
        var exe = OpenBytes(v14, "v14");
        Assert.That(exe.GetBorrowedSpellsState(out var detail), Is.EqualTo(BorrowedSpellsState.Outdated), detail);
        Assert.That(detail, Does.Contain("version 14"));
        Assert.That(detail, Does.Contain("Expert Focus for Vahn, Phase Axe for Kromlech and Shatter for Adrianna"));
        AssertSame(exe.GetBorrowedSpells(), Defaults(Version14Spells));

        // saved as it is, or with Expert Focus, Phase Axe and Shatter added: featpatch.py version 22's files
        exe.SetBorrowedSpells(exe.GetBorrowedSpells());
        Assert.That(Md5(Bytes(exe, "v14-updated")), Is.EqualTo(TwelveMd5));
        exe = OpenBytes(v14, "v14b");
        exe.SetBorrowedSpells(new Dictionary<BorrowedSpell, BorrowedSpellSettings>(exe.GetBorrowedSpells())
        {
            [BorrowedSpell.ExpertFocus] = BorrowedSpells.Info(BorrowedSpell.ExpertFocus).Default,
            [BorrowedSpell.PhaseAxe] = BorrowedSpells.Info(BorrowedSpell.PhaseAxe).Default,
            [BorrowedSpell.Shatter] = BorrowedSpells.Info(BorrowedSpell.Shatter).Default
        });
        Assert.That(Md5(Bytes(exe, "v14-updated-all")), Is.EqualTo(AllFifteenMd5));

        // its own settings (in its five data blocks, where version 22 keeps them too, in the same layout) are read and kept
        var custom = (byte[])v14.Clone();
        var data = FeatPatch.DataVa - VaToFile;
        var data5 = FeatPatch.Data5Va - VaToFile;
        custom[data + FeatPatch.EntriesAt + FeatPatch.EntryStride + FeatPatch.EntryPriceAt] = 2;   // Power Attack's price (the second entry)
        BitConverter.GetBytes((ushort)77).CopyTo(custom, data5 + FeatPatch.DoorDistanceAt + 2 * 2);   // Dimension Door: 77 ahead at rank 2
        custom[data5 + FeatPatch.Data3EntryAt + FeatPatch.EntryPriceAt] = 9;   // ... price 9
        BitConverter.GetBytes(3.5f).CopyTo(custom, data5 + FeatPatch.Data3AbilityAt + FeatPatch.AbilityManaAt);   // ... 3.5 mana
        var read = OpenBytes(custom, "v14-custom").GetBorrowedSpells();
        Assert.That(read[BorrowedSpell.PowerAttack].Price, Is.EqualTo(2));
        Assert.That(read[BorrowedSpell.DimensionDoor].First, Is.EqualTo(new[] { 100, 77, 200 }));
        Assert.That((read[BorrowedSpell.DimensionDoor].Price, read[BorrowedSpell.DimensionDoor].Mana), Is.EqualTo((9, 3.5f)));
        var updated = OpenBytes(custom, "v14-custom2");
        updated.SetBorrowedSpells(read);
        var reread = OpenBytes(Bytes(updated, "v14-custom-saved"), "v14-custom-saved2");
        Assert.That(reread.GetBorrowedSpellsState(), Is.EqualTo(BorrowedSpellsState.On));
        AssertSame(reread.GetBorrowedSpells(), read);

        // taken out: the stock file
        var removed = OpenBytes(v14, "v14-out");
        removed.RemoveBorrowedSpells();
        Assert.That(Bytes(removed, "v14-out-saved"), Is.EqualTo(_stockBytes));

        // version 14 with its code changed, or its fifth block marked 15 (version 15's): not recognised
        var broken = (byte[])v14.Clone();
        broken[FeatPatch.V14Code[0].Va + 8 - VaToFile] ^= 4;
        Assert.That(OpenBytes(broken, "v14-broken").GetBorrowedSpellsState(), Is.EqualTo(BorrowedSpellsState.Unrecognized));
        var marked = (byte[])v14.Clone();
        marked[data5 + 4] = 15;
        Assert.That(OpenBytes(marked, "v14-marked").GetBorrowedSpellsState(), Is.EqualTo(BorrowedSpellsState.Unrecognized));
    }

    [Test]
    public void Version13_IsBroughtUpToDate_KeepingItsSettings()
    {
        RequireStock();
        var v13 = Version13();
        var exe = OpenBytes(v13, "v13");
        Assert.That(exe.GetBorrowedSpellsState(out var detail), Is.EqualTo(BorrowedSpellsState.Outdated), detail);
        Assert.That(detail, Does.Contain("version 13"));
        Assert.That(detail, Does.Contain("Dimension Door and Shatter for Adrianna"));
        AssertSame(exe.GetBorrowedSpells(), Defaults(Version13Spells));

        // saved as it is, or with Dimension Door, Expert Focus, Phase Axe and Shatter added: featpatch.py version 22's files
        exe.SetBorrowedSpells(exe.GetBorrowedSpells());
        Assert.That(Md5(Bytes(exe, "v13-updated")), Is.EqualTo(ElevenMd5));
        exe = OpenBytes(v13, "v13b");
        exe.SetBorrowedSpells(new Dictionary<BorrowedSpell, BorrowedSpellSettings>(exe.GetBorrowedSpells())
        {
            [BorrowedSpell.DimensionDoor] = BorrowedSpells.Info(BorrowedSpell.DimensionDoor).Default,
            [BorrowedSpell.ExpertFocus] = BorrowedSpells.Info(BorrowedSpell.ExpertFocus).Default,
            [BorrowedSpell.PhaseAxe] = BorrowedSpells.Info(BorrowedSpell.PhaseAxe).Default,
            [BorrowedSpell.Shatter] = BorrowedSpells.Info(BorrowedSpell.Shatter).Default
        });
        Assert.That(Md5(Bytes(exe, "v13-updated-all")), Is.EqualTo(AllFifteenMd5));

        // its own settings (in its four data blocks, where version 22 keeps them too, in the same layout) are read and kept
        var custom = (byte[])v13.Clone();
        var data = FeatPatch.DataVa - VaToFile;
        var data4 = FeatPatch.Data4Va - VaToFile;
        custom[data + FeatPatch.EntriesAt + FeatPatch.EntryStride + FeatPatch.EntryPriceAt] = 2;   // Power Attack's price (the second entry)
        custom[data4 + FeatPatch.GasReachAt + 2] = 99;                      // Poison Gas Arrow: a cloud of 99 at rank 2
        custom[data4 + FeatPatch.GasTimeAt + 5] = 7;                        // ... for 0.7 s at rank 5
        custom[data4 + FeatPatch.Data3EntryAt + FeatPatch.EntryPriceAt] = 9;   // ... price 9
        BitConverter.GetBytes(3.5f).CopyTo(custom, data4 + FeatPatch.Data3AbilityAt + FeatPatch.AbilityManaAt);   // ... 3.5 mana
        var read = OpenBytes(custom, "v13-custom").GetBorrowedSpells();
        Assert.That(read[BorrowedSpell.PowerAttack].Price, Is.EqualTo(2));
        Assert.That(read[BorrowedSpell.PoisonGasArrow].MoreValues[0], Is.EqualTo(new[] { 40, 99, 50, 55, 60 }));
        Assert.That(read[BorrowedSpell.PoisonGasArrow].MoreValues[1], Is.EqualTo(new[] { 40, 45, 50, 55, 7 }));
        Assert.That((read[BorrowedSpell.PoisonGasArrow].Price, read[BorrowedSpell.PoisonGasArrow].Mana), Is.EqualTo((9, 3.5f)));
        var updated = OpenBytes(custom, "v13-custom2");
        updated.SetBorrowedSpells(read);
        var reread = OpenBytes(Bytes(updated, "v13-custom-saved"), "v13-custom-saved2");
        Assert.That(reread.GetBorrowedSpellsState(), Is.EqualTo(BorrowedSpellsState.On));
        AssertSame(reread.GetBorrowedSpells(), read);

        // taken out: the stock file
        var removed = OpenBytes(v13, "v13-out");
        removed.RemoveBorrowedSpells();
        Assert.That(Bytes(removed, "v13-out-saved"), Is.EqualTo(_stockBytes));

        // version 13 with its code changed, or its fourth block marked 14: not recognised
        var broken = (byte[])v13.Clone();
        broken[FeatPatch.V13Code[0].Va + 8 - VaToFile] ^= 4;
        Assert.That(OpenBytes(broken, "v13-broken").GetBorrowedSpellsState(), Is.EqualTo(BorrowedSpellsState.Unrecognized));
        var marked = (byte[])v13.Clone();
        marked[data4 + 4] = 14;
        Assert.That(OpenBytes(marked, "v13-marked").GetBorrowedSpellsState(), Is.EqualTo(BorrowedSpellsState.Unrecognized));
    }

    [Test]
    public void Version12_IsBroughtUpToDate_KeepingItsSettings()
    {
        RequireStock();
        var v12 = Version12();
        var exe = OpenBytes(v12, "v12");
        Assert.That(exe.GetBorrowedSpellsState(out var detail), Is.EqualTo(BorrowedSpellsState.Outdated), detail);
        Assert.That(detail, Does.Contain("version 12"));
        AssertSame(exe.GetBorrowedSpells(), Defaults(Version12Spells));

        // saved as it is, or with Poison Gas Arrow, Dimension Door, Expert Focus, Phase Axe and Shatter added: featpatch.py version 22's files
        exe.SetBorrowedSpells(exe.GetBorrowedSpells());
        Assert.That(Md5(Bytes(exe, "v12-updated")), Is.EqualTo(TenMd5));
        exe = OpenBytes(v12, "v12b");
        exe.SetBorrowedSpells(new Dictionary<BorrowedSpell, BorrowedSpellSettings>(exe.GetBorrowedSpells())
        {
            [BorrowedSpell.PoisonGasArrow] = BorrowedSpells.Info(BorrowedSpell.PoisonGasArrow).Default,
            [BorrowedSpell.DimensionDoor] = BorrowedSpells.Info(BorrowedSpell.DimensionDoor).Default,
            [BorrowedSpell.ExpertFocus] = BorrowedSpells.Info(BorrowedSpell.ExpertFocus).Default,
            [BorrowedSpell.PhaseAxe] = BorrowedSpells.Info(BorrowedSpell.PhaseAxe).Default,
            [BorrowedSpell.Shatter] = BorrowedSpells.Info(BorrowedSpell.Shatter).Default
        });
        Assert.That(Md5(Bytes(exe, "v12-updated-all")), Is.EqualTo(AllFifteenMd5));

        // its own settings (in its three data blocks, where version 22 keeps them too, in the same layout) are read and kept
        var custom = (byte[])v12.Clone();
        var data = FeatPatch.DataVa - VaToFile;
        var data2 = FeatPatch.Data2Va - VaToFile;
        var data3 = FeatPatch.Data3Va - VaToFile;
        custom[data + FeatPatch.EntriesAt + FeatPatch.EntryStride + FeatPatch.EntryPriceAt] = 2;   // Power Attack's price (the second entry)
        custom[data2 + FeatPatch.EntangleVinesAt + 2] = 9;                  // Entangle: 9 vines at rank 2
        custom[data3 + FeatPatch.InsectSwarmsAt + 3] = 7;                   // Insect Plague: 7 swarms at rank 3
        custom[data3 + FeatPatch.InsectTimeAt + 5] = 99;                    // ... for 9.9 s at rank 5
        BitConverter.GetBytes(3.5f).CopyTo(custom, data3 + FeatPatch.Data3AbilityAt + FeatPatch.AbilityManaAt);   // ... 3.5 mana
        var read = OpenBytes(custom, "v12-custom").GetBorrowedSpells();
        Assert.That(read[BorrowedSpell.PowerAttack].Price, Is.EqualTo(2));
        Assert.That(read[BorrowedSpell.Entangle].ThirdValues, Is.EqualTo(new[] { 1, 9, 3, 4, 5 }));
        Assert.That(read[BorrowedSpell.InsectPlague].ThirdValues, Is.EqualTo(new[] { 1, 2, 7, 4, 5 }));
        Assert.That(read[BorrowedSpell.InsectPlague].MoreValues[0], Is.EqualTo(new[] { 80, 80, 80, 80, 99 }));
        Assert.That(read[BorrowedSpell.InsectPlague].Mana, Is.EqualTo(3.5f));
        var updated = OpenBytes(custom, "v12-custom2");
        updated.SetBorrowedSpells(read);
        var reread = OpenBytes(Bytes(updated, "v12-custom-saved"), "v12-custom-saved2");
        Assert.That(reread.GetBorrowedSpellsState(), Is.EqualTo(BorrowedSpellsState.On));
        AssertSame(reread.GetBorrowedSpells(), read);

        // taken out: the stock file
        var removed = OpenBytes(v12, "v12-out");
        removed.RemoveBorrowedSpells();
        Assert.That(Bytes(removed, "v12-out-saved"), Is.EqualTo(_stockBytes));

        // version 12 with its code changed, or its third block marked 13: not recognised
        var broken = (byte[])v12.Clone();
        broken[FeatPatch.V12Code[0].Va + 8 - VaToFile] ^= 4;
        Assert.That(OpenBytes(broken, "v12-broken").GetBorrowedSpellsState(), Is.EqualTo(BorrowedSpellsState.Unrecognized));
        var marked = (byte[])v12.Clone();
        marked[data3 + 4] = 13;
        Assert.That(OpenBytes(marked, "v12-marked").GetBorrowedSpellsState(), Is.EqualTo(BorrowedSpellsState.Unrecognized));
    }

    [Test]
    public void Version11_IsBroughtUpToDate_KeepingItsSettings()
    {
        RequireStock();
        var v11 = Version11();
        var exe = OpenBytes(v11, "v11");
        Assert.That(exe.GetBorrowedSpellsState(out var detail), Is.EqualTo(BorrowedSpellsState.Outdated), detail);
        Assert.That(detail, Does.Contain("version 11"));
        AssertSame(exe.GetBorrowedSpells(), Defaults(Version11Spells));

        // saved as it is, or with Insect Plague, Poison Gas Arrow, Dimension Door, Expert Focus, Phase Axe and Shatter added: featpatch.py version 22's files
        exe.SetBorrowedSpells(exe.GetBorrowedSpells());
        Assert.That(Md5(Bytes(exe, "v11-updated")), Is.EqualTo(NineMd5));
        exe = OpenBytes(v11, "v11b");
        exe.SetBorrowedSpells(new Dictionary<BorrowedSpell, BorrowedSpellSettings>(exe.GetBorrowedSpells())
        {
            [BorrowedSpell.InsectPlague] = BorrowedSpells.Info(BorrowedSpell.InsectPlague).Default,
            [BorrowedSpell.PoisonGasArrow] = BorrowedSpells.Info(BorrowedSpell.PoisonGasArrow).Default,
            [BorrowedSpell.DimensionDoor] = BorrowedSpells.Info(BorrowedSpell.DimensionDoor).Default,
            [BorrowedSpell.ExpertFocus] = BorrowedSpells.Info(BorrowedSpell.ExpertFocus).Default,
            [BorrowedSpell.PhaseAxe] = BorrowedSpells.Info(BorrowedSpell.PhaseAxe).Default,
            [BorrowedSpell.Shatter] = BorrowedSpells.Info(BorrowedSpell.Shatter).Default
        });
        Assert.That(Md5(Bytes(exe, "v11-updated-all")), Is.EqualTo(AllFifteenMd5));

        // its own settings (in its two data blocks, where version 22 keeps them too, in the same layout) are read and kept
        var custom = (byte[])v11.Clone();
        var data = FeatPatch.DataVa - VaToFile;
        var data2 = FeatPatch.Data2Va - VaToFile;
        custom[data + FeatPatch.EntriesAt + FeatPatch.EntryStride + FeatPatch.EntryPriceAt] = 2;   // Power Attack's price (the second entry)
        custom[data2 + FeatPatch.EntangleVinesAt + 2] = 9;                  // Entangle: 9 vines at rank 2
        custom[data2 + FeatPatch.EntangleTimeAt + 5] = 99;                  // ... held 9.9 s at rank 5
        var read = OpenBytes(custom, "v11-custom").GetBorrowedSpells();
        Assert.That(read[BorrowedSpell.PowerAttack].Price, Is.EqualTo(2));
        Assert.That(read[BorrowedSpell.Entangle].ThirdValues, Is.EqualTo(new[] { 1, 9, 3, 4, 5 }));
        Assert.That(read[BorrowedSpell.Entangle].MoreValues[1], Is.EqualTo(new[] { 40, 45, 50, 55, 99 }));
        var updated = OpenBytes(custom, "v11-custom2");
        updated.SetBorrowedSpells(read);
        var reread = OpenBytes(Bytes(updated, "v11-custom-saved"), "v11-custom-saved2");
        Assert.That(reread.GetBorrowedSpellsState(), Is.EqualTo(BorrowedSpellsState.On));
        AssertSame(reread.GetBorrowedSpells(), read);

        // taken out: the stock file
        var removed = OpenBytes(v11, "v11-out");
        removed.RemoveBorrowedSpells();
        Assert.That(Bytes(removed, "v11-out-saved"), Is.EqualTo(_stockBytes));

        // version 11 with its code changed, or its second block marked 12: not recognised
        var broken = (byte[])v11.Clone();
        broken[FeatPatch.V11Code[0].Va + 8 - VaToFile] ^= 4;
        Assert.That(OpenBytes(broken, "v11-broken").GetBorrowedSpellsState(), Is.EqualTo(BorrowedSpellsState.Unrecognized));
        var marked = (byte[])v11.Clone();
        marked[data2 + 4] = 12;
        Assert.That(OpenBytes(marked, "v11-marked").GetBorrowedSpellsState(), Is.EqualTo(BorrowedSpellsState.Unrecognized));
    }

    [Test]
    public void Version10_IsBroughtUpToDate_KeepingItsSettings()
    {
        RequireStock();
        var v10 = Version10();
        var exe = OpenBytes(v10, "v10");
        Assert.That(exe.GetBorrowedSpellsState(out var detail), Is.EqualTo(BorrowedSpellsState.Outdated), detail);
        Assert.That(detail, Does.Contain("version 10"));
        AssertSame(exe.GetBorrowedSpells(), Defaults(Version10Spells));

        // saved as it is, or with Entangle, Insect Plague, Poison Gas Arrow, Dimension Door, Expert Focus, Phase Axe and Shatter added: featpatch.py version 22's files
        exe.SetBorrowedSpells(exe.GetBorrowedSpells());
        Assert.That(Md5(Bytes(exe, "v10-updated")), Is.EqualTo(EightMd5));
        exe = OpenBytes(v10, "v10b");
        exe.SetBorrowedSpells(new Dictionary<BorrowedSpell, BorrowedSpellSettings>(exe.GetBorrowedSpells())
        {
            [BorrowedSpell.Entangle] = BorrowedSpells.Info(BorrowedSpell.Entangle).Default,
            [BorrowedSpell.InsectPlague] = BorrowedSpells.Info(BorrowedSpell.InsectPlague).Default,
            [BorrowedSpell.PoisonGasArrow] = BorrowedSpells.Info(BorrowedSpell.PoisonGasArrow).Default,
            [BorrowedSpell.DimensionDoor] = BorrowedSpells.Info(BorrowedSpell.DimensionDoor).Default,
            [BorrowedSpell.ExpertFocus] = BorrowedSpells.Info(BorrowedSpell.ExpertFocus).Default,
            [BorrowedSpell.PhaseAxe] = BorrowedSpells.Info(BorrowedSpell.PhaseAxe).Default,
            [BorrowedSpell.Shatter] = BorrowedSpells.Info(BorrowedSpell.Shatter).Default
        });
        Assert.That(Md5(Bytes(exe, "v10-updated-all")), Is.EqualTo(AllFifteenMd5));

        // its own settings (in its data block at 0x00221B88, where version 22's is too, in version 10's layout) are read and kept
        var custom = (byte[])v10.Clone();
        var data = FeatPatch.DataVa - VaToFile;
        custom[data + 0x80 + 0x60 + 8 + 0x42] = 2;                          // Power Attack's price (the second entry)
        custom[data + 0x558 + 4] = 17;                                      // Power Attack: 17 hits at rank 4
        BitConverter.GetBytes(9.5f).CopyTo(custom, data + 0x380 + 0x50 + 0x2C);   // Blade Barrier's mana (the second own record)
        custom[data + 0x568 + 2] = 9;                                       // Xantam's Orbs: 9 orbs at rank 2
        BitConverter.GetBytes(3.5f).CopyTo(custom, data + 0x630);           // Inferno Arrow's mana
        custom[data + 0x648 + 5] = 99;                                      // its flames: 9.9 s at rank 5
        custom[data + 0x650 + 1] = 33;                                      // its blast: 33 at rank 1
        var read = OpenBytes(custom, "v10-custom").GetBorrowedSpells();
        Assert.That(read[BorrowedSpell.PowerAttack].Price, Is.EqualTo(2));
        Assert.That(read[BorrowedSpell.PowerAttack].First[3], Is.EqualTo(17));
        Assert.That(read[BorrowedSpell.BladeBarrier].Mana, Is.EqualTo(9.5f));
        Assert.That(read[BorrowedSpell.XantamOrbs].ThirdValues, Is.EqualTo(new[] { 1, 9, 3, 4, 5 }));
        Assert.That(read[BorrowedSpell.InfernoArrow].Mana, Is.EqualTo(3.5f));
        Assert.That(read[BorrowedSpell.InfernoArrow].MoreValues[2], Is.EqualTo(new[] { 20, 25, 30, 35, 99 }));
        Assert.That(read[BorrowedSpell.InfernoArrow].MoreValues[0], Is.EqualTo(new[] { 33, 68, 72, 76, 80 }));
        var updated = OpenBytes(custom, "v10-custom2");
        updated.SetBorrowedSpells(read);
        var reread = OpenBytes(Bytes(updated, "v10-custom-saved"), "v10-custom-saved2");
        Assert.That(reread.GetBorrowedSpellsState(), Is.EqualTo(BorrowedSpellsState.On));
        AssertSame(reread.GetBorrowedSpells(), read);

        // taken out: the stock file
        var removed = OpenBytes(v10, "v10-out");
        removed.RemoveBorrowedSpells();
        Assert.That(Bytes(removed, "v10-out-saved"), Is.EqualTo(_stockBytes));

        // version 10 with its code changed: not recognised
        var broken = (byte[])v10.Clone();
        broken[FeatPatch.V10Code[0].Va + 8 - VaToFile] ^= 4;
        Assert.That(OpenBytes(broken, "v10-broken").GetBorrowedSpellsState(), Is.EqualTo(BorrowedSpellsState.Unrecognized));
    }

    [Test]
    public void Version9_IsBroughtUpToDate_KeepingItsSettings()
    {
        RequireStock();
        var v9 = Version9();
        var exe = OpenBytes(v9, "v9");
        Assert.That(exe.GetBorrowedSpellsState(out var detail), Is.EqualTo(BorrowedSpellsState.Outdated), detail);
        Assert.That(detail, Does.Contain("version 9"));
        AssertSame(exe.GetBorrowedSpells(), Defaults(Version9Spells));

        // saved as it is (its data block moves to version 22's place), or with Inferno Arrow, Entangle, Insect Plague, Poison Gas
        // Arrow, Dimension Door, Expert Focus, Phase Axe and Shatter added: featpatch.py version 22's files
        exe.SetBorrowedSpells(exe.GetBorrowedSpells());
        Assert.That(Md5(Bytes(exe, "v9-updated")), Is.EqualTo(SevenMd5));
        exe = OpenBytes(v9, "v9b");
        exe.SetBorrowedSpells(new Dictionary<BorrowedSpell, BorrowedSpellSettings>(exe.GetBorrowedSpells())
        {
            [BorrowedSpell.InfernoArrow] = BorrowedSpells.Info(BorrowedSpell.InfernoArrow).Default,
            [BorrowedSpell.Entangle] = BorrowedSpells.Info(BorrowedSpell.Entangle).Default,
            [BorrowedSpell.InsectPlague] = BorrowedSpells.Info(BorrowedSpell.InsectPlague).Default,
            [BorrowedSpell.PoisonGasArrow] = BorrowedSpells.Info(BorrowedSpell.PoisonGasArrow).Default,
            [BorrowedSpell.DimensionDoor] = BorrowedSpells.Info(BorrowedSpell.DimensionDoor).Default,
            [BorrowedSpell.ExpertFocus] = BorrowedSpells.Info(BorrowedSpell.ExpertFocus).Default,
            [BorrowedSpell.PhaseAxe] = BorrowedSpells.Info(BorrowedSpell.PhaseAxe).Default,
            [BorrowedSpell.Shatter] = BorrowedSpells.Info(BorrowedSpell.Shatter).Default
        });
        Assert.That(Md5(Bytes(exe, "v9-updated-all")), Is.EqualTo(AllFifteenMd5));

        // its own settings (in its data block at 0x00132D90, where every version before 10 keeps them) are read and kept
        var custom = (byte[])v9.Clone();
        const int data = 0x00132D90 - VaToFile;
        custom[data + 0x80 + 0x60 + 8 + 0x42] = 2;                          // Power Attack's price (the second entry)
        custom[data + 0x4F8 + 4] = 17;                                      // Power Attack: 17 hits at rank 4
        BitConverter.GetBytes(9.5f).CopyTo(custom, data + 0x320 + 0x50 + 0x2C);   // Blade Barrier's mana (the second own record)
        custom[data + 0x508 + 2] = 9;                                       // Xantam's Orbs: 9 orbs at rank 2
        var read = OpenBytes(custom, "v9-custom").GetBorrowedSpells();
        Assert.That(read[BorrowedSpell.PowerAttack].Price, Is.EqualTo(2));
        Assert.That(read[BorrowedSpell.PowerAttack].First[3], Is.EqualTo(17));
        Assert.That(read[BorrowedSpell.BladeBarrier].Mana, Is.EqualTo(9.5f));
        Assert.That(read[BorrowedSpell.XantamOrbs].ThirdValues, Is.EqualTo(new[] { 1, 9, 3, 4, 5 }));
        var updated = OpenBytes(custom, "v9-custom2");
        updated.SetBorrowedSpells(read);
        var reread = OpenBytes(Bytes(updated, "v9-custom-saved"), "v9-custom-saved2");
        Assert.That(reread.GetBorrowedSpellsState(), Is.EqualTo(BorrowedSpellsState.On));
        AssertSame(reread.GetBorrowedSpells(), read);

        // taken out: the stock file
        var removed = OpenBytes(v9, "v9-out");
        removed.RemoveBorrowedSpells();
        Assert.That(Bytes(removed, "v9-out-saved"), Is.EqualTo(_stockBytes));

        // version 9 with its code changed: not recognised
        var broken = (byte[])v9.Clone();
        broken[FeatPatch.V9Code[0].Va + 8 - VaToFile] ^= 4;
        Assert.That(OpenBytes(broken, "v9-broken").GetBorrowedSpellsState(), Is.EqualTo(BorrowedSpellsState.Unrecognized));
    }

    [Test]
    public void Version8_IsBroughtUpToDate_KeepingItsSettings()
    {
        RequireStock();
        var v8 = Version8();
        var exe = OpenBytes(v8, "v8");
        Assert.That(exe.GetBorrowedSpellsState(out var detail), Is.EqualTo(BorrowedSpellsState.Outdated), detail);
        Assert.That(detail, Does.Contain("version 8"));
        AssertSame(exe.GetBorrowedSpells(), Defaults(Version9Spells));

        // saved as it is: featpatch.py version 22's file with its seven (the orbs in a fan now)
        exe.SetBorrowedSpells(exe.GetBorrowedSpells());
        Assert.That(Md5(Bytes(exe, "v8-updated")), Is.EqualTo(SevenMd5));

        // its own settings (in its data block, where version 8 keeps them) are read and kept, the orbs' among them
        var custom = (byte[])v8.Clone();
        const int data = 0x00132D90 - VaToFile;
        custom[data + 0x80 + 0x60 + 8 + 0x42] = 2;                          // Power Attack's price (the second entry)
        custom[data + 0x4F8 + 4] = 17;                                      // Power Attack: 17 hits at rank 4
        BitConverter.GetBytes(9.5f).CopyTo(custom, data + 0x320 + 0x50 + 0x2C);   // Blade Barrier's mana (the second own record)
        custom[data + 0x4B0 + 1] = 7;                                       // Blade Barrier's least damage at rank 1
        BitConverter.GetBytes((ushort)(50 * 12)).CopyTo(custom, data + 0x4D0 + 2 * 5);   // Antimagic Field: 12 s at rank 5
        custom[data + 0x4E8 + 3] = 77;                                      // Righteous Might: 77 % less at rank 3
        custom[data + 0x80 + 5 * 0x60 + 8 + 0x41] = 4;                      // Sorcerer's Blood: 4 ranks (the sixth entry)
        BitConverter.GetBytes(7.5f).CopyTo(custom, data + 0x320 + 4 * 0x50 + 0x2C);   // Xantam's Orbs' mana (the fifth own record)
        custom[data + 0x508 + 2] = 9;                                       // Xantam's Orbs: 9 orbs at rank 2
        custom[data + 0x510 + 5] = 3;                                       // ... 3-40 each at rank 5
        custom[data + 0x518 + 5] = 40;
        var read = OpenBytes(custom, "v8-custom").GetBorrowedSpells();
        Assert.That(read[BorrowedSpell.PowerAttack].Price, Is.EqualTo(2));
        Assert.That(read[BorrowedSpell.PowerAttack].First[3], Is.EqualTo(17));
        Assert.That(read[BorrowedSpell.BladeBarrier].Mana, Is.EqualTo(9.5f));
        Assert.That(read[BorrowedSpell.BladeBarrier].First[0], Is.EqualTo(7));
        Assert.That(read[BorrowedSpell.AntimagicField].First[4], Is.EqualTo(12));
        Assert.That(read[BorrowedSpell.RighteousMight].Second[2], Is.EqualTo(77));
        Assert.That(read[BorrowedSpell.SorcerersBlood].RankCount, Is.EqualTo(4));
        Assert.That(read[BorrowedSpell.XantamOrbs].Mana, Is.EqualTo(7.5f));
        Assert.That(read[BorrowedSpell.XantamOrbs].ThirdValues, Is.EqualTo(new[] { 1, 9, 3, 4, 5 }));
        Assert.That((read[BorrowedSpell.XantamOrbs].First[4], read[BorrowedSpell.XantamOrbs].Second[4]), Is.EqualTo((3, 40)));
        var updated = OpenBytes(custom, "v8-custom2");
        updated.SetBorrowedSpells(read);
        var reread = OpenBytes(Bytes(updated, "v8-custom-saved"), "v8-custom-saved2");
        Assert.That(reread.GetBorrowedSpellsState(), Is.EqualTo(BorrowedSpellsState.On));
        AssertSame(reread.GetBorrowedSpells(), read);

        // taken out: the stock file
        var removed = OpenBytes(v8, "v8-out");
        removed.RemoveBorrowedSpells();
        Assert.That(Bytes(removed, "v8-out-saved"), Is.EqualTo(_stockBytes));

        // version 8 with its code changed: not recognised
        var broken = (byte[])v8.Clone();
        broken[FeatPatch.V8Code[0].Va + 8 - VaToFile] ^= 4;
        Assert.That(OpenBytes(broken, "v8-broken").GetBorrowedSpellsState(), Is.EqualTo(BorrowedSpellsState.Unrecognized));
    }

    [Test]
    public void Version7_IsBroughtUpToDate_KeepingItsSettings()
    {
        RequireStock();
        var v7 = Version7();
        var exe = OpenBytes(v7, "v7");
        Assert.That(exe.GetBorrowedSpellsState(out var detail), Is.EqualTo(BorrowedSpellsState.Outdated), detail);
        Assert.That(detail, Does.Contain("version 7"));
        AssertSame(exe.GetBorrowedSpells(), Defaults(Version7Spells));

        // saved with Xantam's Orbs, Inferno Arrow, Entangle, Insect Plague, Poison Gas Arrow, Dimension Door, Expert Focus, Phase Axe and Shatter added: featpatch.py
        // version 22's file
        var all = new Dictionary<BorrowedSpell, BorrowedSpellSettings>(exe.GetBorrowedSpells())
        {
            [BorrowedSpell.XantamOrbs] = BorrowedSpells.Info(BorrowedSpell.XantamOrbs).Default,
            [BorrowedSpell.InfernoArrow] = BorrowedSpells.Info(BorrowedSpell.InfernoArrow).Default,
            [BorrowedSpell.Entangle] = BorrowedSpells.Info(BorrowedSpell.Entangle).Default,
            [BorrowedSpell.InsectPlague] = BorrowedSpells.Info(BorrowedSpell.InsectPlague).Default,
            [BorrowedSpell.PoisonGasArrow] = BorrowedSpells.Info(BorrowedSpell.PoisonGasArrow).Default,
            [BorrowedSpell.DimensionDoor] = BorrowedSpells.Info(BorrowedSpell.DimensionDoor).Default,
            [BorrowedSpell.ExpertFocus] = BorrowedSpells.Info(BorrowedSpell.ExpertFocus).Default,
            [BorrowedSpell.PhaseAxe] = BorrowedSpells.Info(BorrowedSpell.PhaseAxe).Default,
            [BorrowedSpell.Shatter] = BorrowedSpells.Info(BorrowedSpell.Shatter).Default
        };
        exe.SetBorrowedSpells(all);
        Assert.That(Md5(Bytes(exe, "v7-updated")), Is.EqualTo(AllFifteenMd5));

        // its own settings (in its data block, where version 7 keeps them) are read and kept
        var custom = (byte[])v7.Clone();
        const int data = 0x00132D90 - VaToFile;
        custom[data + 0x80 + 0x60 + 8 + 0x42] = 2;                          // Power Attack's price (the second entry)
        custom[data + 0x448 + 4] = 17;                                      // Power Attack: 17 hits at rank 4
        BitConverter.GetBytes(9.5f).CopyTo(custom, data + 0x2C0 + 0x50 + 0x2C);   // Blade Barrier's mana (the second own record)
        custom[data + 0x400 + 1] = 7;                                       // Blade Barrier's least damage at rank 1
        BitConverter.GetBytes((ushort)(50 * 12)).CopyTo(custom, data + 0x420 + 2 * 5);   // Antimagic Field: 12 s at rank 5
        custom[data + 0x438 + 3] = 77;                                      // Righteous Might: 77 % less at rank 3
        custom[data + 0x80 + 5 * 0x60 + 8 + 0x41] = 4;                      // Sorcerer's Blood: 4 ranks (the sixth entry)
        var read = OpenBytes(custom, "v7-custom").GetBorrowedSpells();
        Assert.That(read[BorrowedSpell.PowerAttack].Price, Is.EqualTo(2));
        Assert.That(read[BorrowedSpell.PowerAttack].First[3], Is.EqualTo(17));
        Assert.That(read[BorrowedSpell.BladeBarrier].Mana, Is.EqualTo(9.5f));
        Assert.That(read[BorrowedSpell.BladeBarrier].First[0], Is.EqualTo(7));
        Assert.That(read[BorrowedSpell.AntimagicField].First[4], Is.EqualTo(12));
        Assert.That(read[BorrowedSpell.RighteousMight].Second[2], Is.EqualTo(77));
        Assert.That(read[BorrowedSpell.SorcerersBlood].RankCount, Is.EqualTo(4));
        var updated = OpenBytes(custom, "v7-custom2");
        updated.SetBorrowedSpells(read);
        var reread = OpenBytes(Bytes(updated, "v7-custom-saved"), "v7-custom-saved2");
        Assert.That(reread.GetBorrowedSpellsState(), Is.EqualTo(BorrowedSpellsState.On));
        AssertSame(reread.GetBorrowedSpells(), read);

        // taken out: the stock file
        var removed = OpenBytes(v7, "v7-out");
        removed.RemoveBorrowedSpells();
        Assert.That(Bytes(removed, "v7-out-saved"), Is.EqualTo(_stockBytes));

        // version 7 with its code changed: not recognised
        var broken = (byte[])v7.Clone();
        broken[0x00102910 + 8 - VaToFile] ^= 4;
        Assert.That(OpenBytes(broken, "v7-broken").GetBorrowedSpellsState(), Is.EqualTo(BorrowedSpellsState.Unrecognized));
    }

    [Test]
    public void ItChangesOnlyItsHooksAndUnusedCode()
    {
        RequireStock();
        var exe = Stock();
        exe.SetBorrowedSpells(Defaults(BorrowedSpells.All));
        var bytes = Bytes(exe, "where");
        var changed = Enumerable.Range(0, bytes.Length).Where(i => bytes[i] != _stockBytes[i]).Select(i => i + VaToFile).ToList();
        bool Hook(int va) => Hooks.Any(h => h <= va && va < h + 4);
        Assert.That(changed.All(va => Hook(va) || Used.Any(u => u.Start <= va && va < u.End)), Is.True);
        Assert.That(Hooks.All(va => BitConverter.ToUInt32(bytes, va - VaToFile) != BitConverter.ToUInt32(_stockBytes, va - VaToFile)), Is.True,
                    "all thirty-three hooks");
        Assert.That(EnemyAiHooks.Select(va => BitConverter.ToUInt32(bytes, va - VaToFile)), Is.EqualTo(EnemyAiGameWords),
                    "Improved Enemy AI's eight the game's: it isn't in");
        Assert.That(bytes.AsSpan(0x0030D750 - VaToFile, 0x0030E424 - 0x0030D750).SequenceEqual(_stockBytes.AsSpan(0x0030D750 - VaToFile, 0x0030E424 - 0x0030D750)),
                    Is.True, "the game's ability table (Power Attack's, Sorcerer's Blood's and Inferno Arrow's records among them) is untouched");
        Assert.That(bytes.Take(0x100), Is.EqualTo(_stockBytes.Take(0x100)), "the ELF header and program headers are the game's");
        Assert.That(BgdaExecutable.BorrowedSpellsBytes, Is.EqualTo(Used.Sum(u => u.End - u.Start)));
        Assert.That(BgdaExecutable.FeatPoolBytes, Is.EqualTo(32724));
        Assert.That(BgdaExecutable.FeatPoolStretches, Is.EqualTo(61));

        // with Improved Enemy AI too: its eight words as well, and nothing else outside the same stretches
        exe = Stock();
        exe.SetBorrowedSpells(Defaults(BorrowedSpells.All), FeatPatch.EnemyAiParts);
        bytes = Bytes(exe, "where-ai");
        changed = Enumerable.Range(0, bytes.Length).Where(i => bytes[i] != _stockBytes[i]).Select(i => i + VaToFile).ToList();
        Assert.That(changed.All(va => Hook(va) || EnemyAiHooks.Any(h => h <= va && va < h + 4) || Used.Any(u => u.Start <= va && va < u.End)), Is.True);
        Assert.That(Hooks.Concat(EnemyAiHooks).All(va => BitConverter.ToUInt32(bytes, va - VaToFile) != BitConverter.ToUInt32(_stockBytes, va - VaToFile)),
                    Is.True, "all forty-one hooks");

        // Kromlech's four alone: Xantam's Fire's two hooks, Sorcerer's Blood's two, Inferno Arrow's seven, Entangle's four, Insect
        // Plague's one, Poison Gas Arrow's one, Expert Focus's three and Shatter's one stay the game's (and with Phase Axe too: it has
        // none of its own)
        foreach (var kromlech in new[] { Kromlech, KromlechAll })
        {
            exe = Stock();
            exe.SetBorrowedSpells(Defaults(kromlech));
            bytes = Bytes(exe, "where-krom");
            Assert.That(XantamOnly.Concat(SorcerersOnly).Concat(InfernoOnly).Concat(EntangleOnly).Concat(InsectOnly).Concat(GasOnly).Concat(FocusOnly)
                            .Concat(ShatterOnly).All(va => BitConverter.ToUInt32(bytes, va - VaToFile) == BitConverter.ToUInt32(_stockBytes, va - VaToFile)),
                        Is.True);
        }

        // Xantam's Orbs alone: nine hooks, and the same stretches of the pool as all fifteen
        exe = Stock();
        exe.SetBorrowedSpells(Defaults(new[] { BorrowedSpell.XantamOrbs }));
        bytes = Bytes(exe, "where-orbs");
        Assert.That(Hooks.Where(va => BitConverter.ToUInt32(bytes, va - VaToFile) != BitConverter.ToUInt32(_stockBytes, va - VaToFile)), Is.EqualTo(OrbsHooks));
        changed = Enumerable.Range(0, bytes.Length).Where(i => bytes[i] != _stockBytes[i]).Select(i => i + VaToFile).ToList();
        Assert.That(changed.All(va => Hook(va) || Used.Any(u => u.Start <= va && va < u.End)), Is.True);
    }

    [Test]
    public void ThreePlayers_CanBeTurnedOnAndOffUnderneathThem()
    {
        RequireStock();
        var exe = Stock();
        exe.SetBorrowedSpells(Defaults(BorrowedSpells.All));
        var withSpells = Bytes(exe, "spells");
        exe.EnableThreePlayers();
        var three = Bytes(exe, "spells-three");
        Assert.That(Md5(three), Is.EqualTo(ThreePlayersMd5), "featpatch.py's file on ADRIANNA26");
        Assert.That(exe.GetBorrowedSpellsState(), Is.EqualTo(BorrowedSpellsState.On));
        Assert.That(exe.GetThreePlayerState(), Is.EqualTo(ThreePlayerState.On));

        var other = Stock();
        other.EnableThreePlayers();
        other.SetBorrowedSpells(Defaults(BorrowedSpells.All));
        Assert.That(Bytes(other, "three-spells"), Is.EqualTo(three), "in the other order: the same file");

        exe.DisableThreePlayers();
        Assert.That(Bytes(exe, "spells-again"), Is.EqualTo(withSpells), "turned off again: the spells on the stock file");
    }

    [Test]
    public void TheThreePlayerPatch_NeverWritesInThePoolOrTheHooks()
    {
        RequireStock();
        var exe = Stock();
        exe.EnableThreePlayers();
        var three = Bytes(exe, "three-pool");
        var pool = typeof(BgdaExecutable).Assembly.GetType("JetBlackEngineLib.Data.Executable.FeatPatch")!
            .GetField("Pool")!.GetValue(null) as (int Start, int End)[];
        Assert.That(pool, Is.Not.Null);
        foreach (var (start, end) in pool!)
        {
            Assert.That(three.AsSpan(start - VaToFile, end - start).SequenceEqual(_stockBytes.AsSpan(start - VaToFile, end - start)), Is.True,
                $"0x{start:X8}-0x{end:X8}");
        }

        foreach (var va in Hooks.Concat(EnemyAiHooks))
            Assert.That(BitConverter.ToUInt32(three, va - VaToFile), Is.EqualTo(BitConverter.ToUInt32(_stockBytes, va - VaToFile)), $"hook 0x{va:X8}");
    }

    [Test]
    public void TheirSettings_AreReadBack_AndChangedInPlace()
    {
        RequireStock();
        var custom = new Dictionary<BorrowedSpell, BorrowedSpellSettings>
        {
            [BorrowedSpell.XantamFire] = new(7, 9.5f, new[] { 4, 0, 10, 255, 1 }, new[] { 9, 0, 200, 255, 2 }),
            [BorrowedSpell.PowerAttack] = new(9, 0f, new[] { 1, 20, 3, 1, 2 }, Array.Empty<int>()),
            [BorrowedSpell.AntimagicField] = new(30, 100f, new[] { 1, 60, 2, 3, 4 }, Array.Empty<int>()),
            [BorrowedSpell.RighteousMight] = new(0, 0.5f, new[] { 0, 200, 5, 6, 7 }, new[] { 90, 0, 1, 2, 3 }),
            [BorrowedSpell.SorcerersBlood] = new(7, 0f, Array.Empty<int>(), Array.Empty<int>(), RankCount: 4),
            [BorrowedSpell.XantamOrbs] = new(30, 0.5f, new[] { 0, 255, 1, 2, 3 }, new[] { 0, 255, 9, 2, 4 }, Third: new[] { 10, 1, 4, 9, 6 }),
            [BorrowedSpell.InfernoArrow] = InfernoCustom,
            [BorrowedSpell.Entangle] = EntangleCustom,
            [BorrowedSpell.InsectPlague] = InsectCustom,
            [BorrowedSpell.PoisonGasArrow] = GasCustom,
            [BorrowedSpell.DimensionDoor] = DoorCustom,
            [BorrowedSpell.ExpertFocus] = FocusCustom,
            [BorrowedSpell.PhaseAxe] = AxeCustom,
            [BorrowedSpell.Shatter] = ShatterCustom
        };
        var exe = Stock();
        exe.SetBorrowedSpells(custom);
        var reopened = OpenBytes(Bytes(exe, "custom"), "custom2");
        AssertSame(reopened.GetBorrowedSpells(), custom);

        // a spell added and two taken out in place, the orbs, the flames, the vines, the swarms, the gas, the door's distances, the
        // focus, the axe and the ring changed, then back to all fifteen at the tested settings
        var changed = new Dictionary<BorrowedSpell, BorrowedSpellSettings>(custom) { [BorrowedSpell.BladeBarrier] = BorrowedSpells.Info(BorrowedSpell.BladeBarrier).Default };
        changed.Remove(BorrowedSpell.XantamFire);
        changed.Remove(BorrowedSpell.PowerAttack);
        changed[BorrowedSpell.SorcerersBlood] = changed[BorrowedSpell.SorcerersBlood] with { RankCount = 5, Price = 0 };
        changed[BorrowedSpell.XantamOrbs] = changed[BorrowedSpell.XantamOrbs] with { Third = new[] { 1, 1, 1, 1, 1 } };
        changed[BorrowedSpell.InfernoArrow] = InfernoCustom with
        {
            More = InfernoCustom.MoreValues.Select((r, k) => k == 2 ? (IReadOnlyList<int>)new[] { 255, 255, 255, 255, 255 } : r).ToArray()
        };
        changed[BorrowedSpell.Entangle] = EntangleCustom with
        {
            Third = new[] { 10, 10, 1, 1, 2 }, More = new IReadOnlyList<int>[] { new[] { 98, 2, 4, 6, 8 }, new[] { 5, 6, 7, 8, 9 } }
        };
        changed[BorrowedSpell.InsectPlague] = InsectCustom with
        {
            Third = new[] { 2, 2, 9, 9, 10 }, More = new IReadOnlyList<int>[] { new[] { 7, 8, 9, 10, 11 } }
        };
        changed[BorrowedSpell.PoisonGasArrow] = GasCustom with
        {
            First = new[] { 0, 1, 5, 5, 5 },
            More = new IReadOnlyList<int>[] { new[] { 255, 10, 11, 12, 13 }, new[] { 5, 6, 7, 8, 9 }, new[] { 9, 9, 9, 9, 9 }, new[] { 9, 10, 11, 12, 13 } }
        };
        changed[BorrowedSpell.DimensionDoor] = DoorCustom with { First = new[] { 400, 20, 123 }, Price = 30 };
        changed[BorrowedSpell.ExpertFocus] = FocusCustom with
        {
            First = new[] { 200, 1, 1, 1, 200 }, More = new IReadOnlyList<int>[] { new[] { 255, 1, 2, 3, 4 } }, Mana = 100f
        };
        changed[BorrowedSpell.PhaseAxe] = AxeCustom with { First = new[] { 255, 0, 1, 2, 3 }, Second = new[] { 255, 0, 9, 9, 9 }, Price = 30, Mana = 0.5f };
        changed[BorrowedSpell.Shatter] = ShatterCustom with
        {
            First = new[] { 9, 9, 9, 9, 255 }, Second = new[] { 9, 9, 9, 9, 255 },
            More = new IReadOnlyList<int>[] { new[] { 255, 10, 11, 12, 13 }, new[] { 255, 1, 2, 3, 4 }, new[] { 255, 0, 0, 1, 2 } }, Price = 0, Mana = 100f
        };
        reopened.SetBorrowedSpells(changed);
        AssertSame(OpenBytes(Bytes(reopened, "changed"), "changed2").GetBorrowedSpells(), changed);
        reopened.SetBorrowedSpells(Defaults(BorrowedSpells.All));
        Assert.That(Md5(Bytes(reopened, "custom-back")), Is.EqualTo(AllFifteenMd5));

        Assert.That(Enumerable.Range(0, 5).Select(h => BorrowedSpellSettings.RankCost(16, h)), Is.EqualTo(new[] { 16, 17, 19, 21, 25 }));
        Assert.That(Enumerable.Range(0, 5).Select(h => BorrowedSpellSettings.RankCost(10, h)), Is.EqualTo(new[] { 10, 11, 13, 15, 19 }));
        Assert.That(Enumerable.Range(0, 5).Select(h => BorrowedSpellSettings.RankCost(12, h)), Is.EqualTo(new[] { 12, 13, 15, 17, 21 }),
                    "Xantam's Orbs' (Dimension Door's first three)");
        Assert.That(BorrowedSpellSettings.RankCost(3, 2), Is.EqualTo(9), "below 5: (held + 1) x price");
    }

    [Test]
    public void BadSettings_AreRefused_AndNothingIsWritten()
    {
        RequireStock();
        var exe = Stock();
        var x = BorrowedSpells.Info(BorrowedSpell.XantamFire).Default;
        var pa = BorrowedSpells.Info(BorrowedSpell.PowerAttack).Default;
        var am = BorrowedSpells.Info(BorrowedSpell.AntimagicField).Default;
        var rm = BorrowedSpells.Info(BorrowedSpell.RighteousMight).Default;
        var sb = BorrowedSpells.Info(BorrowedSpell.SorcerersBlood).Default;
        var xo = BorrowedSpells.Info(BorrowedSpell.XantamOrbs).Default;
        var ia = BorrowedSpells.Info(BorrowedSpell.InfernoArrow).Default;
        var en = BorrowedSpells.Info(BorrowedSpell.Entangle).Default;
        var ip = BorrowedSpells.Info(BorrowedSpell.InsectPlague).Default;
        var pg = BorrowedSpells.Info(BorrowedSpell.PoisonGasArrow).Default;
        var dd = BorrowedSpells.Info(BorrowedSpell.DimensionDoor).Default;
        var ef = BorrowedSpells.Info(BorrowedSpell.ExpertFocus).Default;
        var ax = BorrowedSpells.Info(BorrowedSpell.PhaseAxe).Default;
        var sh = BorrowedSpells.Info(BorrowedSpell.Shatter).Default;
        IReadOnlyList<int>[] ShRow(int k, int[] row) => sh.MoreValues.Select((r, i) => i == k ? (IReadOnlyList<int>)row : r).ToArray();
        IReadOnlyList<int>[] PgRow(int k, int[] row) => pg.MoreValues.Select((r, i) => i == k ? (IReadOnlyList<int>)row : r).ToArray();
        IReadOnlyList<int>[] Row(int k, int[] row) => ia.MoreValues.Select((r, i) => i == k ? (IReadOnlyList<int>)row : r).ToArray();
        IReadOnlyList<int>[] EnRow(int k, int[] row) => en.MoreValues.Select((r, i) => i == k ? (IReadOnlyList<int>)row : r).ToArray();
        var bad = new (BorrowedSpell, BorrowedSpellSettings)[]
        {
            (BorrowedSpell.Shatter, sh with { First = new[] { 1, 2, 9, 4, 5 } }), (BorrowedSpell.Shatter, sh with { Second = new[] { 3, 4, 5, 6, 256 } }),
            (BorrowedSpell.Shatter, sh with { First = new[] { 1, 2, 3, 4, -1 } }), (BorrowedSpell.Shatter, sh with { Second = new[] { 3, 4, 5, 6 } }),
            (BorrowedSpell.Shatter, sh with { More = ShRow(0, new[] { 60, 9, 80, 90, 100 }) }), (BorrowedSpell.Shatter, sh with { More = ShRow(0, new[] { 60, 70, 80, 90, 256 }) }),
            (BorrowedSpell.Shatter, sh with { More = ShRow(1, new[] { 10, 0, 20, 25, 30 }) }), (BorrowedSpell.Shatter, sh with { More = ShRow(1, new[] { 10, 15, 20, 25, 256 }) }),
            (BorrowedSpell.Shatter, sh with { More = ShRow(2, new[] { 20, -1, 30, 35, 40 }) }), (BorrowedSpell.Shatter, sh with { More = ShRow(2, new[] { 20, 25, 30, 35, 256 }) }),
            (BorrowedSpell.Shatter, sh with { More = ShRow(2, new[] { 20, 25, 30, 35 }) }), (BorrowedSpell.Shatter, sh with { More = sh.MoreValues.Take(2).ToArray() }),
            (BorrowedSpell.Shatter, sh with { More = null }), (BorrowedSpell.Shatter, sh with { More = ef.More }),
            (BorrowedSpell.Shatter, sh with { Third = new[] { 1, 1, 1, 1, 1 } }), (BorrowedSpell.Shatter, sh with { RankCount = 3 }),
            (BorrowedSpell.Shatter, sh with { Mana = 0f }), (BorrowedSpell.Shatter, sh with { Price = 31 }),
            (BorrowedSpell.PhaseAxe, ax with { More = sh.More }), (BorrowedSpell.Entangle, en with { More = sh.More }),
            (BorrowedSpell.PhaseAxe, ax with { First = new[] { 8, 10, 19, 14, 16 } }), (BorrowedSpell.PhaseAxe, ax with { Second = new[] { 12, 15, 18, 21, 256 } }),
            (BorrowedSpell.PhaseAxe, ax with { First = new[] { 8, 10, 12, 14, -1 } }), (BorrowedSpell.PhaseAxe, ax with { Second = new[] { 12, 15, 18, 21 } }),
            (BorrowedSpell.PhaseAxe, ax with { Second = Array.Empty<int>() }), (BorrowedSpell.PhaseAxe, ax with { Third = new[] { 1, 1, 1, 1, 1 } }),
            (BorrowedSpell.PhaseAxe, ax with { More = ef.More }), (BorrowedSpell.PhaseAxe, ax with { RankCount = 3 }),
            (BorrowedSpell.PhaseAxe, ax with { Mana = 0f }), (BorrowedSpell.PhaseAxe, ax with { Price = 31 }),
            (BorrowedSpell.ExpertFocus, ef with { First = new[] { 0, 50, 50, 50, 50 } }), (BorrowedSpell.ExpertFocus, ef with { First = new[] { 50, 50, 50, 50, 201 } }),
            (BorrowedSpell.ExpertFocus, ef with { More = new IReadOnlyList<int>[] { new[] { 50, 0, 100, 125, 150 } } }),
            (BorrowedSpell.ExpertFocus, ef with { More = new IReadOnlyList<int>[] { new[] { 50, 75, 100, 125, 256 } } }),
            (BorrowedSpell.ExpertFocus, ef with { More = new IReadOnlyList<int>[] { new[] { 50, 75, 100 } } }),
            (BorrowedSpell.ExpertFocus, ef with { More = null }), (BorrowedSpell.ExpertFocus, ef with { More = pg.More }),
            (BorrowedSpell.ExpertFocus, ef with { First = new[] { 50, 50, 50 } }), (BorrowedSpell.ExpertFocus, ef with { RankCount = 3 }),
            (BorrowedSpell.ExpertFocus, ef with { Second = new[] { 1, 2, 3, 4, 5 } }), (BorrowedSpell.ExpertFocus, ef with { Third = new[] { 1, 1, 1, 1, 1 } }),
            (BorrowedSpell.ExpertFocus, ef with { Mana = 0f }), (BorrowedSpell.ExpertFocus, ef with { Price = 31 }),
            (BorrowedSpell.DimensionDoor, dd with { More = ef.More }), (BorrowedSpell.PowerAttack, pa with { More = ef.More }),
            (BorrowedSpell.DimensionDoor, dd with { First = new[] { 19, 150, 200 } }), (BorrowedSpell.DimensionDoor, dd with { First = new[] { 100, 150, 401 } }),
            (BorrowedSpell.DimensionDoor, dd with { First = new[] { 100, 150, 200, 200, 200 } }), (BorrowedSpell.DimensionDoor, dd with { First = new[] { 100, 150 } }),
            (BorrowedSpell.DimensionDoor, dd with { RankCount = 5 }), (BorrowedSpell.DimensionDoor, dd with { RankCount = 1 }),
            (BorrowedSpell.DimensionDoor, dd with { Second = new[] { 1, 2, 3 } }), (BorrowedSpell.DimensionDoor, dd with { Third = new[] { 1, 2, 3 } }),
            (BorrowedSpell.DimensionDoor, dd with { More = ip.More }), (BorrowedSpell.DimensionDoor, dd with { Mana = 0f }),
            (BorrowedSpell.DimensionDoor, dd with { Price = 31 }), (BorrowedSpell.XantamOrbs, xo with { RankCount = 3 }),
            (BorrowedSpell.AntimagicField, am with { First = new[] { 4, 5, 6 }, RankCount = 3 }),
            (BorrowedSpell.PoisonGasArrow, pg with { More = PgRow(0, new[] { 9, 45, 50, 55, 60 }) }),
            (BorrowedSpell.PoisonGasArrow, pg with { More = PgRow(0, new[] { 40, 45, 50, 55, 256 }) }),
            (BorrowedSpell.PoisonGasArrow, pg with { More = PgRow(1, new[] { 40, 0, 50, 55, 60 }) }),
            (BorrowedSpell.PoisonGasArrow, pg with { More = PgRow(1, new[] { 40, 45, 50, 55, 256 }) }),
            (BorrowedSpell.PoisonGasArrow, pg with { More = PgRow(2, new[] { 2, 2, 5, 3, 4 }) }),
            (BorrowedSpell.PoisonGasArrow, pg with { More = PgRow(3, new[] { 3, 4, 4, 5, 256 }) }),
            (BorrowedSpell.PoisonGasArrow, pg with { More = pg.MoreValues.Take(3).ToArray() }), (BorrowedSpell.PoisonGasArrow, pg with { More = null }),
            (BorrowedSpell.PoisonGasArrow, pg with { More = ip.More }), (BorrowedSpell.PoisonGasArrow, pg with { Third = new[] { 1, 1, 1, 1, 1 } }),
            (BorrowedSpell.PoisonGasArrow, pg with { First = new[] { 8, 9, 16, 11, 12 } }), (BorrowedSpell.PoisonGasArrow, pg with { Second = new[] { 12, 14, 15, 17, 256 } }),
            (BorrowedSpell.PoisonGasArrow, pg with { Mana = 0f }), (BorrowedSpell.PoisonGasArrow, pg with { Price = 31 }),
            (BorrowedSpell.InsectPlague, ip with { More = pg.More }), (BorrowedSpell.InfernoArrow, ia with { More = pg.More }),
            (BorrowedSpell.InsectPlague, ip with { Third = null }), (BorrowedSpell.InsectPlague, ip with { Third = new[] { 0, 2, 3, 4, 5 } }),
            (BorrowedSpell.InsectPlague, ip with { Third = new[] { 1, 2, 3, 4, 11 } }), (BorrowedSpell.InsectPlague, ip with { More = null }),
            (BorrowedSpell.InsectPlague, ip with { More = en.More }), (BorrowedSpell.InsectPlague, ip with { More = new IReadOnlyList<int>[] { new[] { 80, 0, 80, 80, 80 } } }),
            (BorrowedSpell.InsectPlague, ip with { More = new IReadOnlyList<int>[] { new[] { 80, 80, 80, 80, 256 } } }),
            (BorrowedSpell.InsectPlague, ip with { First = new[] { 1, 1, 4, 1, 1 } }), (BorrowedSpell.InsectPlague, ip with { Second = new[] { 3, 3, 3, 3, 256 } }),
            (BorrowedSpell.InsectPlague, ip with { Mana = 0f }), (BorrowedSpell.InsectPlague, ip with { Price = 31 }),
            (BorrowedSpell.Entangle, en with { More = ip.More }), (BorrowedSpell.XantamOrbs, xo with { More = ip.More }),
            (BorrowedSpell.Entangle, en with { More = EnRow(0, new[] { 30, 35, 40, 44, 50 }) }), (BorrowedSpell.Entangle, en with { More = EnRow(0, new[] { 0, 34, 40, 44, 50 }) }),
            (BorrowedSpell.Entangle, en with { More = EnRow(0, new[] { 30, 34, 40, 44, 100 }) }), (BorrowedSpell.Entangle, en with { More = EnRow(1, new[] { 40, 0, 50, 55, 60 }) }),
            (BorrowedSpell.Entangle, en with { More = EnRow(1, new[] { 40, 45, 50, 55, 256 }) }), (BorrowedSpell.Entangle, en with { More = en.MoreValues.Take(1).ToArray() }),
            (BorrowedSpell.Entangle, en with { More = null }), (BorrowedSpell.Entangle, en with { Third = null }),
            (BorrowedSpell.Entangle, en with { Third = new[] { 0, 2, 3, 4, 5 } }), (BorrowedSpell.Entangle, en with { Third = new[] { 1, 2, 3, 4, 11 } }),
            (BorrowedSpell.Entangle, en with { First = new[] { 2, 3, 9, 5, 6 } }), (BorrowedSpell.Entangle, en with { Mana = 0f }),
            (BorrowedSpell.Entangle, en with { Price = 31 }), (BorrowedSpell.XantamOrbs, xo with { More = en.More }),
            (BorrowedSpell.InfernoArrow, ia with { More = null }), (BorrowedSpell.InfernoArrow, ia with { More = ia.MoreValues.Take(4).ToArray() }),
            (BorrowedSpell.InfernoArrow, ia with { More = Row(0, new[] { 0, 1, 1, 1, 1 }) }), (BorrowedSpell.InfernoArrow, ia with { More = Row(1, new[] { 1, 1, 1, 1, 256 }) }),
            (BorrowedSpell.InfernoArrow, ia with { More = Row(2, new[] { 1, 0, 1, 1, 1 }) }), (BorrowedSpell.InfernoArrow, ia with { More = Row(3, new[] { 1, 1, 9, 2, 3 }) }),
            (BorrowedSpell.InfernoArrow, ia with { First = new[] { 10, 12, 22, 16, 18 } }), (BorrowedSpell.InfernoArrow, ia with { Mana = 0f }),
            (BorrowedSpell.InfernoArrow, ia with { Third = new[] { 1, 1, 1, 1, 1 } }), (BorrowedSpell.XantamOrbs, xo with { More = ia.More }),
            (BorrowedSpell.XantamOrbs, xo with { Third = null }), (BorrowedSpell.XantamOrbs, xo with { Third = new[] { 1, 2, 3, 4 } }),
            (BorrowedSpell.XantamOrbs, xo with { Third = new[] { 0, 2, 3, 4, 5 } }), (BorrowedSpell.XantamOrbs, xo with { Third = new[] { 1, 2, 3, 4, 11 } }),
            (BorrowedSpell.XantamOrbs, xo with { First = new[] { 8, 8, 13, 8, 8 } }), (BorrowedSpell.XantamOrbs, xo with { Second = new[] { 12, 12, 12, 12, 256 } }),
            (BorrowedSpell.XantamOrbs, xo with { Mana = 0f }), (BorrowedSpell.XantamOrbs, xo with { Price = 31 }),
            (BorrowedSpell.XantamFire, x with { Third = new[] { 1, 2, 3, 4, 5 } }), (BorrowedSpell.SorcerersBlood, sb with { Third = new[] { 1, 1, 1, 1, 1 } }),
            (BorrowedSpell.XantamFire, x with { Price = -1 }), (BorrowedSpell.XantamFire, x with { Price = 31 }),
            (BorrowedSpell.XantamFire, x with { Mana = 0f }), (BorrowedSpell.XantamFire, x with { Mana = 100.5f }),
            (BorrowedSpell.XantamFire, x with { Mana = float.PositiveInfinity }),
            (BorrowedSpell.BladeBarrier, x with { First = new[] { 1, 1, 5, 2, 3 } }), (BorrowedSpell.BladeBarrier, x with { Second = new[] { 2, 3, 4, 5, 256 } }),
            (BorrowedSpell.BladeBarrier, x with { First = new[] { 1, 1, 2, 2 } }),
            (BorrowedSpell.AntimagicField, am with { First = new[] { 4, 5, 0, 7, 8 } }), (BorrowedSpell.AntimagicField, am with { Second = new[] { 1, 2, 3, 4, 5 } }),
            (BorrowedSpell.RighteousMight, rm with { First = new[] { 10, 20, 30, 40, 201 } }), (BorrowedSpell.RighteousMight, rm with { Second = new[] { 91, 20, 30, 40, 50 } }),
            (BorrowedSpell.PowerAttack, pa with { Mana = 5f }), (BorrowedSpell.PowerAttack, pa with { First = new[] { 1, 2, 0, 4, 5 } }),
            (BorrowedSpell.PowerAttack, pa with { First = new[] { 1, 2, 3, 4, 21 } }), (BorrowedSpell.PowerAttack, pa with { Second = new[] { 1, 2, 3, 4, 5 } }),
            (BorrowedSpell.PowerAttack, pa with { Price = 31 }), (BorrowedSpell.PowerAttack, pa with { RankCount = 3 }),
            (BorrowedSpell.XantamFire, x with { RankCount = 1 }),
            (BorrowedSpell.SorcerersBlood, sb with { RankCount = 0 }), (BorrowedSpell.SorcerersBlood, sb with { RankCount = 6 }),
            (BorrowedSpell.SorcerersBlood, sb with { Mana = 1f }), (BorrowedSpell.SorcerersBlood, sb with { First = new[] { 1 } }),
            (BorrowedSpell.SorcerersBlood, sb with { Price = 31 }), (BorrowedSpell.SorcerersBlood, sb with { Price = -1 })
        };
        foreach (var (spell, s) in bad)
        {
            Assert.That(s.Problem(spell), Is.Not.Null);
            Assert.Throws<ArgumentException>(() => exe.SetBorrowedSpells(new Dictionary<BorrowedSpell, BorrowedSpellSettings> { [spell] = s }));
        }

        Assert.That(Bytes(exe, "bad"), Is.EqualTo(_stockBytes));
        Assert.That(BorrowedSpells.All.All(s => BorrowedSpells.Info(s).Default.Problem(s) == null), Is.True);
    }

    [Test]
    public void AFileItDoesntKnow_IsLeftAlone()
    {
        RequireStock();
        var hooked = (byte[])_stockBytes.Clone();
        hooked[0x0017A4BC - VaToFile] ^= 1;
        var exe = OpenBytes(hooked, "hooked");
        Assert.That(exe.GetBorrowedSpellsState(out var detail), Is.EqualTo(BorrowedSpellsState.Unrecognized));
        Assert.That(detail, Does.Contain("0x0017A4BC"));
        Assert.Throws<InvalidOperationException>(() => exe.SetBorrowedSpells(Defaults(BorrowedSpells.All)));
        Assert.Throws<InvalidOperationException>(() => exe.SetBorrowedSpells(NoSpells(), FeatPatch.EnemyAiParts));
        Assert.That(exe.GetEnemyAi(), Is.EqualTo(0));
        Assert.Throws<InvalidOperationException>(() => exe.RemoveBorrowedSpells());
        Assert.That(Bytes(exe, "hooked-after"), Is.EqualTo(hooked));

        var used = (byte[])_stockBytes.Clone();
        used[0x0012BAE8 + 0x20 - VaToFile] ^= 0x10;
        Assert.That(OpenBytes(used, "used").GetBorrowedSpellsState(out detail), Is.EqualTo(BorrowedSpellsState.Unrecognized));
        Assert.That(detail, Does.Contain("another patch"));

        var off = Stock();
        off.RemoveBorrowedSpells();
        Assert.That(Bytes(off, "off-twice"), Is.EqualTo(_stockBytes), "taking them out of a file without them changes nothing");
    }

    // ═══════════════════════════════ the names ═══════════════════════════════

    private static byte[] SmallGameText()
    {
        var texts = Enumerable.Range(0, 5).Select(i => new List<(string, string)>
        {
            ("burninghands_name", $"Burning Hands {i}"), ("odd", "x"), ("powerattack_name", $"Power Attack {i}")
        }).ToArray();
        return LmpWriter.PackEntries(EngineVersion.DarkAlliance,
            GameText.Files.Select((f, i) => (f.File, new UniTextFile(texts[i]).ToArray())).ToList());
    }

    [Test]
    public void TheNames_GoInEveryLanguage_AndComeOutAgain()
    {
        var original = SmallGameText();
        var text = GameText.Read(original);
        var own = BorrowedSpells.All.Where(s => BorrowedSpells.Info(s).HasOwnTexts).ToList();
        Assert.That(own.Any(s => BorrowedSpellTexts.HasNames(text, s) || BorrowedSpellTexts.HasAnyName(text, s)), Is.False);
        Assert.That(BorrowedSpellTexts.Of(BorrowedSpell.PowerAttack), Is.Empty, "Power Attack's texts are the game's own");
        Assert.That(BorrowedSpellTexts.Of(BorrowedSpell.SorcerersBlood), Is.Empty, "and Sorcerer's Blood's");
        Assert.That(BorrowedSpellTexts.Of(BorrowedSpell.InfernoArrow), Is.Empty, "and Inferno Arrow's");
        Assert.That(BorrowedSpellTexts.NameKey(BorrowedSpell.PowerAttack), Is.EqualTo("powerattack_name"));
        Assert.That(BorrowedSpellTexts.NameKey(BorrowedSpell.XantamFire), Is.EqualTo("xantamfire_name"));
        BorrowedSpellTexts.AddNames(text, BorrowedSpells.All);
        var saved = GameText.Read(text.ToArray());
        Assert.That(BorrowedSpells.All.All(s => BorrowedSpellTexts.HasNames(saved, s)), Is.True);
        Assert.That(saved.Get("xantamfire_name"), Is.EqualTo("Xantam's Fire"));
        Assert.That(saved.Get("bladebarrier_name"), Is.EqualTo("Blade Barrier"));
        Assert.That(saved.Get("antimagicfield_name", 2), Is.EqualTo("Antimagisches Feld"));
        Assert.That(saved.Get("righteousmight_hud"), Is.EqualTo("+%d%% damage, -%d%% damage taken"), "the HUD's line: %d the rank's values, %% a percent sign");
        Assert.That(saved.Get("xantamorbs_name"), Is.EqualTo("Xantam's Orbs"));
        Assert.That(saved.Get("xantamorbs_name", 2), Is.EqualTo("Xantams Kugeln"));
        Assert.That(saved.Get("xantamorbs_hud"), Is.Null, "its HUD line is the game's own \"Damage: %d-%d\"");
        Assert.That(saved.Get("entangle_name"), Is.EqualTo("Entangle"));
        Assert.That(saved.Get("entangle_name", 1), Is.EqualTo("Enchevêtrement"));
        Assert.That(saved.Get("entangle_hud"), Is.Null, "its HUD line is the game's own \"Damage: %d-%d\" too");
        Assert.That(saved.Get("insectplague_name"), Is.EqualTo("Insect Plague"));
        Assert.That(saved.Get("insectplague_name", 2), Is.EqualTo("Insektenplage"));
        Assert.That(saved.Get("insectplague_hud"), Is.Null, "its HUD line is the game's own \"Damage: %d-%d\" too");
        Assert.That(saved.Get("poisongasarrow_name"), Is.EqualTo("Poison Gas Arrow"));
        Assert.That(saved.Get("poisongasarrow_name", 1), Is.EqualTo("Flèche de gaz toxique"));
        Assert.That(saved.Get("poisongasarrow_name", 4), Is.EqualTo("Flecha de gas venenoso"));
        Assert.That(saved.Get("poisongasarrow_hud"), Is.Null, "its HUD line is the game's own \"Damage: %d-%d\" too");
        Assert.That(saved.Get("dimensiondoor_name"), Is.EqualTo("Dimension Door"));
        Assert.That(saved.Get("dimensiondoor_name", 2), Is.EqualTo("Dimensionstür"));
        Assert.That(saved.Get("dimensiondoor_hud"), Is.EqualTo("Distance: %d units"), "its own HUD line: %d the rank's distance");
        Assert.That(saved.Get("expertfocus_name"), Is.EqualTo("Expert Focus"));
        Assert.That(saved.Get("expertfocus_name", 2), Is.EqualTo("Expertenfokus"));
        Assert.That(saved.Get("expertfocus_hud"), Is.EqualTo("Arrows +%d%% damage for %d.%d seconds"),
                    "its own HUD line: the rank's percentage, then its time in seconds and tenths");
        Assert.That(saved.Get("phaseaxe_name"), Is.EqualTo("Phase Axe"));
        Assert.That(saved.Get("phaseaxe_name", 2), Is.EqualTo("Phasenaxt"));
        Assert.That(saved.Get("phaseaxe_name", 3), Is.EqualTo("Ascia di Fase"));
        Assert.That(saved.Get("phaseaxe_hud"), Is.Null, "its HUD line is the game's own \"Damage: %d-%d\" too");
        Assert.That(saved.Get("shatter_name"), Is.EqualTo("Shatter"));
        Assert.That(saved.Get("shatter_name", 1), Is.EqualTo("Fracassement"));
        Assert.That(saved.Get("shatter_name", 2), Is.EqualTo("Zerschmettern"));
        Assert.That(saved.Get("shatter_hud"), Is.EqualTo("Reach %d, frozen for %d.%d seconds"),
                    "its own HUD line: the rank's reach, then its freeze in seconds and tenths");
        for (var language = 0; language < 5; language++)
        {
            var keys = saved.Entries(language).Select(e => e.Key).ToList();
            Assert.That(keys, Is.EqualTo(new[]
            {
                "burninghands_name", "odd", "powerattack_name", "xantamfire_name", "xantamfire_desc", "bladebarrier_name", "bladebarrier_desc",
                "antimagicfield_name", "antimagicfield_desc", "antimagicfield_hud", "righteousmight_name", "righteousmight_desc", "righteousmight_hud",
                "xantamorbs_name", "xantamorbs_desc", "entangle_name", "entangle_desc", "insectplague_name", "insectplague_desc",
                "poisongasarrow_name", "poisongasarrow_desc", "dimensiondoor_name", "dimensiondoor_desc", "dimensiondoor_hud",
                "expertfocus_name", "expertfocus_desc", "expertfocus_hud", "phaseaxe_name", "phaseaxe_desc", "shatter_name", "shatter_desc",
                "shatter_hud"
            }), "after the file's own entries, spell by spell");
            foreach (var hud in new[] { "antimagicfield_hud", "righteousmight_hud", "dimensiondoor_hud", "expertfocus_hud", "shatter_hud" })
                Assert.That(saved.Get(hud, language)!.TrimEnd().EndsWith("%"), Is.False, "a HUD line ends in plain text (the game's formatter reads on past a line ending in a %-code)");
        }

        Assert.That(BorrowedSpells.All.SelectMany(BorrowedSpellTexts.Of).All(n => n.Text.All(c => GameText.TextCharacters.Contains(c))), Is.True,
                    "letters the game's font draws");
        BorrowedSpellTexts.AddNames(saved, BorrowedSpells.All);
        Assert.That(saved.Changed, Is.False, "adding them again changes nothing");
        BorrowedSpellTexts.RemoveNames(saved, BorrowedSpells.All);
        Assert.That(saved.ToArray(), Is.EqualTo(original), "the game's own powerattack_name stays");

        text = GameText.Read(original);
        BorrowedSpellTexts.AddNames(text, new[] { BorrowedSpell.RighteousMight, BorrowedSpell.XantamFire });
        Assert.That(text.Entries(0).Select(e => e.Key).Skip(3).First(), Is.EqualTo("xantamfire_name"), "in the spells' order, whatever order they are asked in");
        Assert.That(BorrowedSpellTexts.HasAnyName(text, BorrowedSpell.BladeBarrier), Is.False);
    }

    // ═══════════════════════════════ the icons and ICONS.LMP ═══════════════════════════════

    /// <summary>An archive: the entries in this order, each file at the next multiple of 0x80, the directory with room for <paramref name="spare"/> more.</summary>
    private static byte[] Archive(IReadOnlyList<(string Name, byte[] Data)> entries, int spare)
    {
        var at = (4 + 64 * (entries.Count + spare) + 0x7F) & ~0x7F;
        var output = new List<byte>(new byte[at]);
        var dir = new List<(int, int)>();
        foreach (var (_, data) in entries)
        {
            while (output.Count % 0x80 != 0) output.Add(0);
            dir.Add((output.Count, data.Length));
            output.AddRange(data);
        }

        var bytes = output.ToArray();
        BitConverter.GetBytes(entries.Count).CopyTo(bytes, 0);
        for (var i = 0; i < entries.Count; i++)
        {
            Encoding.ASCII.GetBytes(entries[i].Name).CopyTo(bytes, 4 + 64 * i);
            BitConverter.GetBytes(dir[i].Item1).CopyTo(bytes, 4 + 64 * i + 56);
            BitConverter.GetBytes(dir[i].Item2).CopyTo(bytes, 4 + 64 * i + 60);
        }

        return bytes;
    }

    private static byte[] Fill(int n, byte b) => Enumerable.Repeat(b, n).ToArray();

    [Test]
    public void TheIcons_GoIntoAFullIconsLmp_AndComeOutAgain()
    {
        var md5 = new Dictionary<BorrowedSpell, string>
        {
            [BorrowedSpell.XantamFire] = "10628d23903f20cfe1fb1ab5e2989450", [BorrowedSpell.BladeBarrier] = "f586c2f47c445397ff313207706965db",
            [BorrowedSpell.AntimagicField] = "c6d39e2cd0b3573358519a201bd9a6d9", [BorrowedSpell.RighteousMight] = "bd6f5fbeff4c9015adc061080c9912a1",
            [BorrowedSpell.XantamOrbs] = "549084289a254b4fd6413f9ff814f308", [BorrowedSpell.InfernoArrow] = "a7200918c0b4af9f16b4ff6ce63a00b5",
            [BorrowedSpell.Entangle] = "a27ef3840af3753c6772028cd928aa66", [BorrowedSpell.InsectPlague] = "6e2878b6609482d7201c7480555c811a",
            [BorrowedSpell.PoisonGasArrow] = "f3c2e780029d954b996989b49541b84b", [BorrowedSpell.DimensionDoor] = "672b2d3ceb1db3c7d1325390e9ad6882",
            [BorrowedSpell.ExpertFocus] = "28996c19d68116f9eddbc624e5fa9223", [BorrowedSpell.PhaseAxe] = "99dbf76da3ccd4a8f360617026b3033c",
            [BorrowedSpell.Shatter] = "6d0fff8430563844cb943b3c478c755b"
        };
        var own = BorrowedSpells.All.Where(BorrowedSpellIcons.HasOwnIcon).ToList();
        Assert.That(own, Is.EqualTo(md5.Keys.OrderBy(s => s)),
                    "every spell but Power Attack (the game's icon_powerattack.tex) and Sorcerer's Blood (a passive: no HUD slot)");
        Assert.That(BorrowedSpellIcons.EntryName(BorrowedSpell.PowerAttack), Is.EqualTo("icon_powerattack.tex"));
        foreach (var spell in own)
        {
            Assert.That(BorrowedSpellIcons.Texture(spell).Length, Is.EqualTo(3568));
            Assert.That(Md5(BorrowedSpellIcons.Texture(spell)), Is.EqualTo(md5[spell]),
                        "make_xantam_icon.py's, make_kromlech_icons.py's, make_xantam_orbs_icon.py's, make_inferno_arrow_icon.py's, " +
                        "make_entangle_icon.py's, make_insect_plague_icon.py's, make_poison_gas_arrow_icon.py's, " +
                        "make_dimension_door_icon.py's, make_expert_focus_icon.py's, make_phase_axe_icon.py's and make_shatter_icon.py's " +
                        "textures");
        }

        // like the game's ICONS.LMP: sorted, the directory full (no room before the first file)
        var icons = Archive(new[]
        {
            ("exclaim.tex", Fill(90, 9)), ("icon_balllightning.tex", Fill(60, 8)), ("icon_healthpotion.tex", Fill(300, 1)),
            ("icon_powerattack.tex", Fill(100, 7)), ("icon_protection.tex", Fill(200, 2)), ("icon_quake.tex", Fill(40, 6)),
            ("icon_whirlwind.tex", Fill(129, 3)), ("levelup.tex", Fill(70, 4)), ("nomana.tex", Fill(10, 5))
        }, spare: 0);
        Assert.That(GameArchive.Problem(icons), Is.Null);

        var added = BorrowedSpellIcons.AddTo(icons, BorrowedSpells.All);
        Assert.That(GameArchive.Problem(added), Is.Null, "the game reads it as it should");
        Assert.That(GameArchive.ReadDirectory(added).Select(e => e.Name), Is.EqualTo(new[]
        {
            "exclaim.tex", "icon_antimagicfield.tex", "icon_balllightning.tex", "icon_bladebarrier.tex", "icon_dimensiondoor.tex",
            "icon_entangle.tex", "icon_expertfocus.tex", "icon_healthpotion.tex",
            "icon_infernoarrow.tex", "icon_insectplague.tex", "icon_phaseaxe.tex", "icon_poisongasarrow.tex",
            "icon_powerattack.tex", "icon_protection.tex", "icon_quake.tex", "icon_righteousmight.tex", "icon_shatter.tex", "icon_whirlwind.tex",
            "icon_xantamfire.tex",
            "icon_xantamorbs.tex", "levelup.tex", "nomana.tex"
        }), "each where its name sorts");
        Assert.That(GameArchive.AsLoaded(added).Where(f => !f.Name.StartsWith("icon_") || f.Name is "icon_healthpotion.tex" or "icon_protection.tex" or
                                                        "icon_whirlwind.tex" or "icon_balllightning.tex" or "icon_quake.tex" or "icon_powerattack.tex").Select(f => f.Data),
                    Is.EqualTo(GameArchive.AsLoaded(icons).Select(f => f.Data)), "every other file as it was (Power Attack's the game's own)");
        Assert.That(BorrowedSpellIcons.IsIn(icons, BorrowedSpell.PowerAttack), Is.True);
        Assert.That(BorrowedSpellIcons.AddTo(icons, new[] { BorrowedSpell.PowerAttack }), Is.EqualTo(icons), "Power Attack brings no icon");
        Assert.That(BorrowedSpellIcons.AddTo(icons, new[] { BorrowedSpell.SorcerersBlood }), Is.EqualTo(icons), "nor Sorcerer's Blood");
        Assert.That(BorrowedSpellIcons.RemoveFrom(icons, new[] { BorrowedSpell.PowerAttack }), Is.EqualTo(icons), "and never takes the game's out");
        foreach (var spell in own)
        {
            Assert.That(GameArchive.AsLoaded(added).Single(f => f.Name == BorrowedSpellIcons.EntryName(spell)).Data, Is.EqualTo(BorrowedSpellIcons.Texture(spell)));
            Assert.That(BorrowedSpellIcons.IsIn(added, spell) && !BorrowedSpellIcons.IsIn(icons, spell), Is.True);
        }

        Assert.That(BorrowedSpellIcons.AddTo(added, BorrowedSpells.All), Is.EqualTo(added), "adding them again changes nothing");
        Assert.That(BorrowedSpellIcons.RemoveFrom(added, BorrowedSpells.All), Is.EqualTo(icons), "taken out: the archive as it was, byte for byte");
        Assert.That(BorrowedSpellIcons.RemoveFrom(added, new[] { BorrowedSpell.BladeBarrier }),
                    Is.EqualTo(BorrowedSpellIcons.AddTo(icons, BorrowedSpells.All.Where(s => s != BorrowedSpell.BladeBarrier))),
                    "one taken out: as if it had never gone in");
        Assert.That(BorrowedSpellIcons.RemoveFrom(icons, BorrowedSpells.All), Is.EqualTo(icons));
        Assert.That(BorrowedSpellIcons.Problem(Archive(new[] { ("tinyab.tex", Fill(10, 1)) }, spare: 1)), Does.Contain("isn't ICONS.LMP"));
    }
}
