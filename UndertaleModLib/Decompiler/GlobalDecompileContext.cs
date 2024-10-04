using System.Collections.Generic;
using System.Diagnostics;
using System;
using Underanalyzer;
using Underanalyzer.Decompiler;
using Underanalyzer.Decompiler.GameSpecific;
using UndertaleModLib.Models;

namespace UndertaleModLib.Decompiler;

/// <summary>
/// The DecompileContext is global for the entire decompilation run, or possibly multiple runs. It caches the decompilation results which don't change often
/// to speedup decompilation.
/// </summary>
public class GlobalDecompileContext : IGameContext
{
    public UndertaleData Data;

    // Implementation of Underanalyzer properties
    public bool UsingGMLv2 => Data?.IsVersionAtLeast(2, 3) ?? false;
    public bool Bytecode14OrLower => (Data?.GeneralInfo?.BytecodeVersion ?? 15) <= 14;
    public bool UsingGMS2OrLater => Data?.IsVersionAtLeast(2) ?? false;
    public bool UsingFinallyBeforeThrow => !(Data?.IsVersionAtLeast(2024, 6) ?? false);
    public IGlobalFunctions GlobalFunctions => Data?.GlobalFunctions;
    public bool UsingTypedBooleans => Data?.IsVersionAtLeast(2, 3, 7) ?? false;
    public bool UsingAssetReferences => Data?.IsVersionAtLeast(2023, 8) ?? false;
    public bool UsingRoomInstanceReferences => Data?.IsVersionAtLeast(2024, 2) ?? false;
    public GameSpecificRegistry GameSpecificRegistry => Data?.GameSpecificRegistry;

    public GlobalDecompileContext(UndertaleData data)
    {
        Data = data;
        BuildGlobalFunctionCache(data);
    }

    /// <summary>
    /// Builds the GlobalFunctions cache, required for Underanalyzer's decompiler to recognize function names in global scope.
    /// </summary>
    public static void BuildGlobalFunctionCache(UndertaleData data)
    {
        if (data == null || data.GlobalFunctions != null)
        {
            // Nothing to calculate
            return;
        }

        if (!data.IsVersionAtLeast(2, 3))
        {
            // Make an empty instance
            data.GlobalFunctions = new GlobalFunctions();
            return;
        }

        // Use Underanalyzer's built-in global function finder
        try
        {
            data.GlobalFunctions = new GlobalFunctions(GetGlobalScriptCodeEntries(data));
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());

            // If an error occurs, just default to empty
            data.GlobalFunctions = new GlobalFunctions();
        }
    }

    // Enumerates over all global script code entries
    private static IEnumerable<IGMCode> GetGlobalScriptCodeEntries(UndertaleData data)
    {
        foreach (UndertaleGlobalInit script in data.GlobalInitScripts)
        {
            yield return script.Code;
        }
    }

    // Implementation of Underanalyzer methods
    public string GetAssetName(AssetType assetType, int assetIndex)
    {
        if (assetIndex < 0)
        {
            return null;
        }

        switch (assetType)
        {
            case AssetType.Object:
                if (assetIndex >= Data.GameObjects.Count)
                {
                    return null;
                }
                return Data.GameObjects[assetIndex].Name?.Content;
            case AssetType.Sprite:
                if (assetIndex >= Data.Sprites.Count)
                {
                    return null;
                }
                return Data.Sprites[assetIndex].Name?.Content;
            case AssetType.Sound:
                if (assetIndex >= Data.Sounds.Count)
                {
                    return null;
                }
                return Data.Sounds[assetIndex].Name?.Content;
            case AssetType.Room:
                if (assetIndex >= Data.Rooms.Count)
                {
                    return null;
                }
                return Data.Rooms[assetIndex].Name?.Content;
            case AssetType.Background:
                if (assetIndex >= Data.Backgrounds.Count)
                {
                    return null;
                }
                return Data.Backgrounds[assetIndex].Name?.Content;
            case AssetType.Path:
                if (assetIndex >= Data.Paths.Count)
                {
                    return null;
                }
                return Data.Paths[assetIndex].Name?.Content;
            case AssetType.Script:
                if (assetIndex >= Data.Scripts.Count)
                {
                    return null;
                }
                return Data.Scripts[assetIndex].Name?.Content;
            case AssetType.Font:
                if (assetIndex >= Data.Fonts.Count)
                {
                    return null;
                }
                return Data.Fonts[assetIndex].Name?.Content;
            case AssetType.Timeline:
                if (assetIndex >= Data.Timelines.Count)
                {
                    return null;
                }
                return Data.Timelines[assetIndex].Name?.Content;
            case AssetType.Shader:
                if (assetIndex >= Data.Shaders.Count)
                {
                    return null;
                }
                return Data.Shaders[assetIndex].Name?.Content;
            case AssetType.Sequence:
                if (assetIndex >= Data.Sequences.Count)
                {
                    return null;
                }
                return Data.Sequences[assetIndex].Name?.Content;
            case AssetType.AnimCurve:
                if (assetIndex >= Data.AnimationCurves.Count)
                {
                    return null;
                }
                return Data.AnimationCurves[assetIndex].Name?.Content;
            case AssetType.ParticleSystem:
                if (assetIndex >= Data.ParticleSystems.Count)
                {
                    return null;
                }
                return Data.ParticleSystems[assetIndex].Name?.Content;
            case AssetType.RoomInstance:
                if (assetIndex < 100000)
                {
                    return null;
                }
                return $"{Data.ToolInfo.InstanceIdPrefix()}{assetIndex}";
        }

        return null;
    }
}