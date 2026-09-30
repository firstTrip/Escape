using UnityEditor;
using UnityEngine;

public static class ChannelZeroPlayRoomImportConfigurator
{
    private const string ResourceRoot = "Assets/Resources/ChannelZero/PlayRooms";

    [MenuItem("Tools/Channel Zero/Configure Play Room Textures")]
    public static void ConfigureAll()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        int configuredCount = 0;

        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ResourceRoot }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                continue;

            bool changed = false;
            changed |= SetIfDifferent(importer.textureType, TextureImporterType.Sprite,
                value => importer.textureType = value);
            changed |= SetIfDifferent(importer.spriteImportMode, SpriteImportMode.Single,
                value => importer.spriteImportMode = value);
            changed |= SetIfDifferent(importer.mipmapEnabled, false,
                value => importer.mipmapEnabled = value);
            changed |= SetIfDifferent(importer.npotScale, TextureImporterNPOTScale.None,
                value => importer.npotScale = value);
            changed |= SetIfDifferent(importer.filterMode, FilterMode.Bilinear,
                value => importer.filterMode = value);
            changed |= SetIfDifferent(importer.textureCompression, TextureImporterCompression.Uncompressed,
                value => importer.textureCompression = value);
            changed |= SetIfDifferent(importer.maxTextureSize, 2048,
                value => importer.maxTextureSize = value);

            if (changed)
                importer.SaveAndReimport();

            configuredCount++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"CHANNEL_ZERO_PLAYROOM_IMPORT_CONFIGURED:{configuredCount}");
    }

    private static bool SetIfDifferent<T>(T current, T desired, System.Action<T> setter)
    {
        if (System.Collections.Generic.EqualityComparer<T>.Default.Equals(current, desired))
            return false;

        setter(desired);
        return true;
    }
}
