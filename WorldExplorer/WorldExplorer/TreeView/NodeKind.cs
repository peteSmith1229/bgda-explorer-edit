using System;
using System.IO;

namespace WorldExplorer.TreeView;

/// <summary>What a tree node represents; drives its icon, colour and description.</summary>
public enum NodeKind
{
    File,
    Gob,
    Archive,
    Folder,
    Texture,
    Model,
    Animation,
    World,
    Element,
    Script,
    Cutscene,
    Audio,
    Objects,
    Dialog,
    Text,
    Database,
    Entity,
    Missing
}

public static class NodeKinds
{
    /// <summary>Classifies an archive entry by its file extension.</summary>
    public static NodeKind FromFileName(string name)
    {
        string ext;
        try
        {
            ext = (Path.GetExtension(name) ?? "").ToLowerInvariant();
        }
        catch (ArgumentException)
        {
            return NodeKind.File;
        }

        return ext switch
        {
            ".tex" or ".etex" => NodeKind.Texture,
            ".vif" => NodeKind.Model,
            ".anm" => NodeKind.Animation,
            ".world" => NodeKind.World,
            ".scr" => NodeKind.Script,
            ".cut" => NodeKind.Cutscene,
            ".vag" or ".adpcm" => NodeKind.Audio,
            ".ob" => NodeKind.Objects,
            ".bin" => NodeKind.Dialog,
            ".names" or ".txt" or ".sdb" => NodeKind.Text,
            ".lmp" or ".clp" or ".yak" => NodeKind.Archive,
            ".gob" => NodeKind.Gob,
            ".ddf" => NodeKind.Database,
            _ => NodeKind.File
        };
    }

    public static string IconKey(NodeKind kind) => kind switch
    {
        NodeKind.Gob => "Icon.Kind.Gob",
        NodeKind.Archive => "Icon.Kind.Archive",
        NodeKind.Folder => "Icon.Kind.Folder",
        NodeKind.Texture => "Icon.View.Texture",
        NodeKind.Model => "Icon.View.Model",
        NodeKind.Animation => "Icon.Kind.Animation",
        NodeKind.World => "Icon.View.Level",
        NodeKind.Element => "Icon.Kind.Element",
        NodeKind.Script => "Icon.Kind.Script",
        NodeKind.Cutscene => "Icon.Kind.Cutscene",
        NodeKind.Audio => "Icon.Kind.Audio",
        NodeKind.Objects => "Icon.Kind.Objects",
        NodeKind.Dialog => "Icon.Kind.Dialog",
        NodeKind.Text => "Icon.Kind.Text",
        NodeKind.Database => "Icon.Kind.Database",
        NodeKind.Entity => "Icon.Kind.Entity",
        NodeKind.Missing => "Icon.Warning",
        _ => "Icon.Kind.File"
    };

    public static string BrushKey(NodeKind kind) => kind switch
    {
        NodeKind.Gob => "Brush.Icon.Gob",
        NodeKind.Archive => "Brush.Icon.Archive",
        NodeKind.Folder => "Brush.Icon.Folder",
        NodeKind.Texture => "Brush.Icon.Texture",
        NodeKind.Model => "Brush.Icon.Model",
        NodeKind.Animation => "Brush.Icon.Animation",
        NodeKind.World or NodeKind.Element => "Brush.Icon.World",
        NodeKind.Script or NodeKind.Cutscene => "Brush.Icon.Script",
        NodeKind.Audio => "Brush.Icon.Audio",
        NodeKind.Objects => "Brush.Icon.Objects",
        NodeKind.Dialog or NodeKind.Text => "Brush.Icon.Text",
        NodeKind.Database or NodeKind.Entity => "Brush.Icon.Database",
        NodeKind.Missing => "Brush.Icon.Missing",
        _ => "Brush.Icon.Data"
    };

    public static string Describe(NodeKind kind) => kind switch
    {
        NodeKind.Gob => "GOB archive",
        NodeKind.Archive => "Archive",
        NodeKind.Folder => "Folder",
        NodeKind.Texture => "Texture",
        NodeKind.Model => "Model",
        NodeKind.Animation => "Animation",
        NodeKind.World => "Level",
        NodeKind.Element => "Level element",
        NodeKind.Script => "Script",
        NodeKind.Cutscene => "Cutscene",
        NodeKind.Audio => "Audio",
        NodeKind.Objects => "Object list",
        NodeKind.Dialog => "Dialog table",
        NodeKind.Text => "Text",
        NodeKind.Database => "Entity database",
        NodeKind.Entity => "Entity",
        NodeKind.Missing => "Missing asset",
        _ => "Data"
    };
}
