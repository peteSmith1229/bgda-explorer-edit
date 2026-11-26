// ═══════════════════════════════════════════════════════════════════════════════
// NEW FILE: WorldExplorer/JetBlackEngineLib/Data/Executable/BgdaExecutable.HudLayout.cs
//
// Three-player HUD layout: moves and resizes each player's HUD parts in three-player games.
//   Panel   the health / mana / experience bars in their frame, the recovery overlays and the
//           level-up icon
//   Icons   the potion and ammo symbols with their counts, and the status icons under the panel
//   Gold    the gold total and its coin (the flying gold numbers head for where it now is)
//   Skills  the skill bar: its boxes, icons, the skill's name, energy cost and damage
//
// How it works (patch data in BgdaHudLayoutPatch.cs, made by hudlayout.py):
//   - The HUD code marks which player's part it is drawing (10 hooks in the per-player HUD
//     0x0017CA90, the skill bar call and the gold function 0x0017EDE0).
//   - The game's three 2D sprite writers (their submit call) and two text writers (a wrapper) move
//     and scale the screen coordinates they have just written, about the part's anchor, while a
//     part is marked whose entry is not the identity. Scissor rectangles (the bar fills) follow.
//   - Anchors: player 1's parts hang from the top-left corner of the screen, player 2's from the
//     top-right and player 3's from the top centre; the skill bars from the matching bottom corner
//     or centre. So Size shrinks a part towards its own corner, and X / Y move it from there.
//   - Only in three-player games: one and two players always get the game's own layout.
//   - Player 3's potion / ammo symbols and status icons are drawn off screen by the game (it has
//     room for two players there); with the patch in, the game's own layout puts them under his
//     panel, as player 2's sit under hers.
//
// Where it lives: the block (0x800 bytes) in the stock executable's empty DVP overlay sections
// (file 0x227000, zero in every build, never written by the other tabs). A three-player build
// loads that part of the file at 0x00326000 (BSS); crt0's 'addiu v0, v0, 0x5E00' (0x00100010),
// reached from the three-player boot stub, jumps to HUDBOOT, which copies the block home to
// 0x000DB800 (a free stretch of the three-player arena, zeroed at boot) before crt0 clears BSS.
//
// The three-player patch never writes the block or the hook words, so three players stay On with
// the layout in. Turning three players off takes the layout out first (the hooks would jump into
// memory a stock executable doesn't fill).
// ═══════════════════════════════════════════════════════════════════════════════
using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;

namespace JetBlackEngineLib.Data.Executable;

/// <summary>The parts of a player's HUD that can be placed.</summary>
public enum HudPart
{
    /// <summary>Health, mana and experience bars in their frame (with the level-up icon).</summary>
    Panel,
    /// <summary>Potion and ammo symbols with their counts, and the status icons.</summary>
    Icons,
    /// <summary>The gold total and its coin.</summary>
    Gold,
    /// <summary>The skill bar.</summary>
    Skills
}

/// <summary>
/// Where a HUD part goes: moved by <see cref="OffsetX"/> / <see cref="OffsetY"/> screen units (the game's
/// 640 × 448 screen; +Y is down) from where the game puts it, and drawn at <see cref="SizePercent"/> of its
/// size about its player's corner.
/// </summary>
public readonly record struct HudPlacement(int OffsetX, int OffsetY, int SizePercent)
{
    public static HudPlacement Default => new(0, 0, 100);
}

/// <summary>Every player's HUD placement (three players, four parts).</summary>
public sealed class HudLayout
{
    public const int Players = 3;
    public static readonly HudPart[] Parts = { HudPart.Panel, HudPart.Icons, HudPart.Gold, HudPart.Skills };

    private readonly HudPlacement[] _parts = new HudPlacement[Players * 4];

    public HudLayout()
    {
        for (var i = 0; i < _parts.Length; i++) _parts[i] = HudPlacement.Default;
    }

    /// <summary>Player 0, 1 or 2.</summary>
    public HudPlacement this[int player, HudPart part]
    {
        get => _parts[Index(player, part)];
        set => _parts[Index(player, part)] = value;
    }

    /// <summary>The game's own layout everywhere.</summary>
    public bool IsDefault => Array.TrueForAll(_parts, p => p == HudPlacement.Default);

    public static HudLayout Default => new();

    private static int Index(int player, HudPart part)
    {
        if (player is < 0 or >= Players) throw new ArgumentOutOfRangeException(nameof(player));
        if (part is < HudPart.Panel or > HudPart.Skills) throw new ArgumentOutOfRangeException(nameof(part));
        return player * 4 + (int)part;
    }
}

/// <summary>Whether an executable has the HUD layout patch.</summary>
public enum HudLayoutState
{
    /// <summary>Three players are not on (or not recognised): there is no HUD layout to set.</summary>
    Unavailable,
    /// <summary>Three players are on with the game's own HUD code: the layout can be put in.</summary>
    Off,
    /// <summary>The HUD layout patch is in and intact.</summary>
    On,
    /// <summary>Some of its places hold something else: it is left alone.</summary>
    Unrecognized
}

public partial class BgdaExecutable
{
    public const int MinHudSize = 25, MaxHudSize = 300;
    public const int MaxHudOffsetX = 640, MaxHudOffsetY = 448;

    public static string HudPartName(HudPart part) => part switch
    {
        HudPart.Panel => "Health / mana / XP",
        HudPart.Icons => "Potions / ammo",
        HudPart.Gold => "Gold",
        _ => "Skills"
    };

    private static readonly Lazy<byte[]> HudBlock = new(() =>
    {
        var block = Convert.FromBase64String(HudLayoutPatch.BlockBase64);
        if (block.Length != HudLayoutPatch.Size ||
            Convert.ToHexString(SHA256.HashData(block)).ToLowerInvariant() != HudLayoutPatch.BlockSha256)
            throw new InvalidDataException("The built-in HUD layout data is damaged.");
        return block;
    });

    /// <summary>Block offset of the entry for a player and table part (0..4).</summary>
    private static int HudEntry(int player, int part) =>
        HudLayoutPatch.TableOffset + (player * HudLayoutPatch.PartsPerPlayer + part) * HudLayoutPatch.EntrySize;

    /// <summary>The bytes of an entry the layout changes: new x, new y and size (+4 .. +9).</summary>
    private static bool IsHudSetting(int blockOffset)
    {
        var t = blockOffset - HudLayoutPatch.TableOffset;
        if (t < 0 || t >= HudLayout.Players * HudLayoutPatch.PartsPerPlayer * HudLayoutPatch.EntrySize) return false;
        var inEntry = t % HudLayoutPatch.EntrySize;
        return inEntry is >= 4 and < 10;
    }

    /// <summary>
    /// Unavailable when three players aren't on, Off when the game's own HUD code is there, On when the
    /// HUD layout patch is in and intact, Unrecognized otherwise. <paramref name="detail"/> says why.
    /// </summary>
    public HudLayoutState GetHudLayoutState(out string detail)
    {
        var tp = GetThreePlayerState();
        if (tp is not (ThreePlayerState.On or ThreePlayerState.Outdated))
        {
            detail = "The HUD layout needs three players.";
            return HudLayoutState.Unavailable;
        }

        var block = HudBlock.Value;
        int stockHooks = 0, patchedHooks = 0;
        foreach (var h in HudLayoutPatch.Hooks)
        {
            var w = ReadU32(FileOffset(h.Va));
            if (w == h.Stock) stockHooks++;
            else if (w == h.Patched) patchedHooks++;
        }

        var allZero = true;
        var intact = true;
        for (var i = 0; i < HudLayoutPatch.Size; i++)
        {
            var b = _data[HudLayoutPatch.FileOffset + i];
            if (b != 0) allZero = false;
            if (!IsHudSetting(i) && b != block[i]) intact = false;
        }

        if (stockHooks == HudLayoutPatch.Hooks.Length && allZero)
        {
            detail = "The game's own HUD layout (player 3's potion, ammo and status icons are off screen).";
            return HudLayoutState.Off;
        }

        if (patchedHooks == HudLayoutPatch.Hooks.Length && intact)
        {
            detail = GetHudLayout().IsDefault
                ? "The HUD layout patch is in, with the game's own layout (player 3's icons under his panel)."
                : "The HUD layout patch is in, with a custom layout.";
            return HudLayoutState.On;
        }

        detail = $"The HUD layout's places hold something else ({patchedHooks} of {HudLayoutPatch.Hooks.Length} hooks " +
                 $"are the patch's, {stockHooks} the game's; its block at file offset 0x{HudLayoutPatch.FileOffset:X6} is " +
                 (allZero ? "empty" : intact ? "intact" : "different") + "), so it is left alone.";
        return HudLayoutState.Unrecognized;
    }

    public HudLayoutState GetHudLayoutState() => GetHudLayoutState(out _);

    /// <summary>The layout in the executable (the game's own when the patch isn't in).</summary>
    public HudLayout GetHudLayout()
    {
        var layout = new HudLayout();
        if (!HudBlockPresent()) return layout;

        var block = HudBlock.Value;
        for (var p = 0; p < HudLayout.Players; p++)
        {
            foreach (var part in HudLayout.Parts)
            {
                var e = HudEntry(p, (int)part);
                var at = HudLayoutPatch.FileOffset + e;
                int Cur(int k) => BitConverter.ToInt16(_data, at + k);
                int Def(int k) => BitConverter.ToInt16(block, e + k);
                var size = (int)Math.Round(Cur(8) * 100 / 256.0, MidpointRounding.AwayFromZero);
                layout[p, part] = new HudPlacement(Cur(4) - Def(4), Cur(6) - Def(6), size);
            }
        }

        return layout;
    }

    private bool HudBlockPresent() =>
        _data.Length >= HudLayoutPatch.FileOffset + 4 &&
        _data[HudLayoutPatch.FileOffset] == (byte)'H' && _data[HudLayoutPatch.FileOffset + 1] == (byte)'U' &&
        _data[HudLayoutPatch.FileOffset + 2] == (byte)'D' && _data[HudLayoutPatch.FileOffset + 3] == (byte)'L';

    /// <summary>
    /// Puts the HUD layout patch in (state Off) or updates its layout (state On). Every value is
    /// checked first; nothing is written if one is out of range or the state is neither.
    /// </summary>
    public void SetHudLayout(HudLayout layout)
    {
        ValidateHudLayout(layout);
        var state = GetHudLayoutState(out var detail);
        if (state is not (HudLayoutState.Off or HudLayoutState.On))
            throw new InvalidOperationException("The HUD layout can't be set: " + detail);

        var block = (byte[])HudBlock.Value.Clone();
        for (var p = 0; p < HudLayout.Players; p++)
        {
            for (var part = 0; part <= HudLayoutPatch.Effects; part++)
            {
                var placement = layout[p, part == HudLayoutPatch.Effects ? HudPart.Icons : (HudPart)part];
                var e = HudEntry(p, part);
                var nx = BitConverter.ToInt16(block, e + 4) + placement.OffsetX;
                var ny = BitConverter.ToInt16(block, e + 6) + placement.OffsetY;
                var size = (int)Math.Round(placement.SizePercent * 256 / 100.0, MidpointRounding.AwayFromZero);
                BitConverter.GetBytes((short)nx).CopyTo(block, e + 4);
                BitConverter.GetBytes((short)ny).CopyTo(block, e + 6);
                BitConverter.GetBytes((short)size).CopyTo(block, e + 8);
            }
        }

        block.CopyTo(_data, HudLayoutPatch.FileOffset);
        foreach (var h in HudLayoutPatch.Hooks) BitConverter.GetBytes(h.Patched).CopyTo(_data, FileOffset(h.Va));
    }

    /// <summary>Takes the HUD layout patch out (state On): the game's code and an empty block come back.</summary>
    public void RemoveHudLayout()
    {
        var state = GetHudLayoutState(out var detail);
        if (state == HudLayoutState.Off) return;
        if (state != HudLayoutState.On)
            throw new InvalidOperationException("The HUD layout can't be removed: " + detail);

        Array.Clear(_data, HudLayoutPatch.FileOffset, HudLayoutPatch.Size);
        foreach (var h in HudLayoutPatch.Hooks) BitConverter.GetBytes(h.Stock).CopyTo(_data, FileOffset(h.Va));
    }

    /// <summary>
    /// Before three players are turned off: the layout goes too. Refused when its places hold
    /// something else, since a hook left behind would jump into memory a stock executable doesn't fill.
    /// </summary>
    private void RemoveHudLayoutForThreePlayersOff()
    {
        switch (GetHudLayoutState(out var detail))
        {
            case HudLayoutState.On:
                RemoveHudLayout();
                break;
            case HudLayoutState.Unrecognized:
                throw new InvalidOperationException("Three players cannot be turned off: " + detail);
        }
    }

    /// <summary>Throws <see cref="ArgumentException"/> naming the first bad value.</summary>
    public static void ValidateHudLayout(HudLayout layout)
    {
        if (layout is null) throw new ArgumentException("No HUD layout.");
        for (var p = 0; p < HudLayout.Players; p++)
        {
            foreach (var part in HudLayout.Parts)
            {
                var v = layout[p, part];
                var name = $"Player {p + 1} {HudPartName(part).ToLower(CultureInfo.InvariantCulture)}";
                if (v.OffsetX is < -MaxHudOffsetX or > MaxHudOffsetX)
                    throw new ArgumentException($"{name}: X must be a whole number from {-MaxHudOffsetX} to {MaxHudOffsetX}.");
                if (v.OffsetY is < -MaxHudOffsetY or > MaxHudOffsetY)
                    throw new ArgumentException($"{name}: Y must be a whole number from {-MaxHudOffsetY} to {MaxHudOffsetY}.");
                if (v.SizePercent is < MinHudSize or > MaxHudSize)
                    throw new ArgumentException($"{name}: size must be a whole number from {MinHudSize} to {MaxHudSize} %.");
            }
        }
    }
}
