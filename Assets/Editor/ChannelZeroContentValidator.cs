using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ChannelZero.Runtime.Core;
using UnityEditor;
using UnityEngine;

public static class ChannelZeroContentValidator
{
    private const string ScenePath = "Assets/Scenes/ChannelZero_VerticalSlice.unity";

    [MenuItem("Tools/Channel Zero/Validate Content Graph")]
    public static void ValidateFromMenu()
    {
        string[] issues = Validate().ToArray();
        if (issues.Length == 0)
        {
            Debug.Log("CHANNEL_ZERO_VALIDATION_PASS: routes, puzzles, narratives, artwork, and era layouts are consistent.");
            return;
        }
        foreach (string issue in issues)
            Debug.LogError("CHANNEL_ZERO_VALIDATION: " + issue);
        throw new InvalidOperationException($"Channel Zero content validation failed with {issues.Length} issue(s).");
    }

    public static void ValidateBatch() => ValidateFromMenu();

    public static System.Collections.Generic.IReadOnlyList<string> Validate()
    {
        string sceneText = File.ReadAllText(Path.GetFullPath(ScenePath));
        string[] sceneIds = Regex.Matches(sceneText, @"^  logicalId: (.+)$", RegexOptions.Multiline)
            .Cast<Match>().Select(match => match.Groups[1].Value.Trim()).Distinct().ToArray();
        ChannelZeroCloseupCatalog closeups = AssetDatabase.LoadAssetAtPath<ChannelZeroCloseupCatalog>(
            "Assets/ChannelZero/Data/ChannelZeroCloseupCatalog.asset");
        if (closeups == null)
            return new[] { "Required catalog asset is missing." };
        return ChannelZeroContentGraphValidator.Validate(sceneIds,
            PuzzleDefinitionCatalog.LoadDefault(),
            ChannelZeroInteractionCatalog.LoadDefault(),
            ChannelZeroHotspotLayoutCatalog.LoadDefault(),
            NarrativeTextCatalog.LoadDefault(), closeups);
    }
}
