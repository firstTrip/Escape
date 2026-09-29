using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ChannelZero.Runtime.Core
{
    public static class ChannelZeroRuntimeEnvironment
    {
        private static Func<IChannelZeroSaveStore> saveStoreFactory = ChannelZeroSaveService.CreateDefaultStore;

        public static IChannelZeroSaveStore CreateSaveStore() => saveStoreFactory();

        public static void SetSaveStoreFactory(Func<IChannelZeroSaveStore> factory) =>
            saveStoreFactory = factory ?? throw new ArgumentNullException(nameof(factory));

        public static void Reset() => saveStoreFactory = ChannelZeroSaveService.CreateDefaultStore;
    }

    public interface IChannelZeroSaveStore
    {
        bool HasKey(string key);
        string Read(string key);
        void Write(string key, string value);
        void Delete(string key);
    }

    public interface IChannelZeroBackupSaveStore
    {
        bool TryReadBackup(string key, out string value);
    }

    public sealed class PlayerPrefsChannelZeroSaveStore : IChannelZeroSaveStore
    {
        public bool HasKey(string key) => PlayerPrefs.HasKey(key);
        public string Read(string key) => PlayerPrefs.GetString(key, string.Empty);

        public void Write(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
            PlayerPrefs.Save();
        }

        public void Delete(string key)
        {
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }
    }

    public sealed class FileChannelZeroSaveStore : IChannelZeroSaveStore, IChannelZeroBackupSaveStore
    {
        private readonly string directory;

        public FileChannelZeroSaveStore(string directory = null)
        {
            this.directory = string.IsNullOrWhiteSpace(directory)
                ? Path.Combine(Application.persistentDataPath, "Saves")
                : directory;
        }

        public bool HasKey(string key) => File.Exists(PathFor(key)) || File.Exists(BackupPathFor(key));

        public string Read(string key)
        {
            string path = PathFor(key);
            try
            {
                if (File.Exists(path))
                    return File.ReadAllText(path);
            }
            catch (IOException) { }

            string backup = BackupPathFor(key);
            return File.Exists(backup) ? File.ReadAllText(backup) : string.Empty;
        }

        public void Write(string key, string value)
        {
            Directory.CreateDirectory(directory);
            string path = PathFor(key);
            string temporary = path + ".tmp";
            string backup = BackupPathFor(key);
            File.WriteAllText(temporary, value ?? string.Empty);
            if (File.Exists(path))
                File.Replace(temporary, path, backup, true);
            else
                File.Move(temporary, path);
        }

        public void Delete(string key)
        {
            DeleteIfExists(PathFor(key));
            DeleteIfExists(BackupPathFor(key));
            DeleteIfExists(PathFor(key) + ".tmp");
        }

        public bool TryReadBackup(string key, out string value)
        {
            string backup = BackupPathFor(key);
            if (!File.Exists(backup))
            {
                value = string.Empty;
                return false;
            }
            value = File.ReadAllText(backup);
            return !string.IsNullOrWhiteSpace(value);
        }

        private string PathFor(string key) => Path.Combine(directory, Sanitize(key) + ".json");
        private string BackupPathFor(string key) => PathFor(key) + ".bak";
        private static string Sanitize(string key)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars())
                key = key.Replace(invalid, '_');
            return key;
        }
        private static void DeleteIfExists(string path) { if (File.Exists(path)) File.Delete(path); }
    }

    public sealed class LegacyFallbackSaveStore : IChannelZeroSaveStore, IChannelZeroBackupSaveStore
    {
        private readonly IChannelZeroSaveStore primary;
        private readonly IChannelZeroSaveStore legacy;
        private readonly string legacyKey;

        public LegacyFallbackSaveStore(IChannelZeroSaveStore primary, IChannelZeroSaveStore legacy,
            string legacyKey)
        {
            this.primary = primary;
            this.legacy = legacy;
            this.legacyKey = legacyKey;
        }

        public bool HasKey(string key) => primary.HasKey(key) || legacy.HasKey(legacyKey);
        public string Read(string key) => primary.HasKey(key) ? primary.Read(key) : legacy.Read(legacyKey);
        public void Write(string key, string value)
        {
            primary.Write(key, value);
            if (legacy.HasKey(legacyKey))
                legacy.Delete(legacyKey);
        }
        public void Delete(string key)
        {
            primary.Delete(key);
            legacy.Delete(legacyKey);
        }
        public bool TryReadBackup(string key, out string value)
        {
            if (primary is IChannelZeroBackupSaveStore backup && backup.TryReadBackup(key, out value))
                return true;
            value = string.Empty;
            return false;
        }
    }

    public interface IChannelZeroSaveMigration
    {
        int FromVersion { get; }
        void Migrate(ChannelZeroSessionState state);
    }

    public sealed class ChannelZeroSaveMigrationV1ToV2 : IChannelZeroSaveMigration
    {
        public int FromVersion => 1;
        public void Migrate(ChannelZeroSessionState state)
        {
            state.inventoryOrigins ??= new List<InventoryOriginEntry>();
            foreach (string itemId in state.inventoryItemIds ?? new List<string>())
                if (state.inventoryOrigins.Find(entry => entry.itemId == itemId) == null)
                    state.inventoryOrigins.Add(new InventoryOriginEntry(itemId, InferItemRoom(itemId)));
            state.saveVersion = 2;
        }

        private static string InferItemRoom(string itemId) => itemId == ChannelZeroPuzzleIds.NormalTube ||
            itemId == ChannelZeroPuzzleIds.FaultyTube || itemId == ChannelZeroIds.FoldingCrankItem
                ? ChannelZeroIds.WorkshopRoom
                : ChannelZeroIds.LivingRoom;
    }

    public sealed class ChannelZeroSaveMigrationV2ToV3 : IChannelZeroSaveMigration
    {
        public int FromVersion => 2;
        public void Migrate(ChannelZeroSessionState state)
        {
            UpgradeEntries(state.puzzleStates, state.roomId);
            foreach (TimelineSnapshot snapshot in state.timelineSnapshots ?? new List<TimelineSnapshot>())
                UpgradeEntries(snapshot.puzzleStates, snapshot.roomId);
            state.saveVersion = 3;
        }

        private static void UpgradeEntries(List<PuzzleStateEntry> entries, string fallbackRoom)
        {
            foreach (PuzzleStateEntry entry in entries ?? new List<PuzzleStateEntry>())
            {
                StateDescriptor descriptor = ChannelZeroStateCatalog.ResolveLegacy(entry.puzzleId, fallbackRoom);
                entry.persistence = descriptor.Persistence;
                entry.roomId = descriptor.RoomId;
                entry.eraScoped = descriptor.EraScoped;
            }
        }
    }

    public sealed class ChannelZeroSaveMigrationV3ToV4 : IChannelZeroSaveMigration
    {
        private static readonly HashSet<string> LegacyPuzzleIds = new(StringComparer.Ordinal)
        {
            ChannelZeroPuzzleIds.Rug2749,
            ChannelZeroPuzzleIds.Lockbox8888,
            ChannelZeroPuzzleIds.RecMaster,
        };

        private static readonly HashSet<string> LegacyFlags = new(StringComparer.Ordinal)
        {
            "Rec1961", "Rec1981", "Rec2001", "Rec2021", "LiveReference2001Seen",
            "RecMasterComplete", "Rug2749Solved", "Lockbox8888Solved",
        };

        public int FromVersion => 3;

        public void Migrate(ChannelZeroSessionState state)
        {
            state.inventoryItemIds?.RemoveAll(item => item == ChannelZeroPuzzleIds.MasterTape);
            state.inventoryOrigins?.RemoveAll(entry => entry.itemId == ChannelZeroPuzzleIds.MasterTape);
            state.puzzleStates?.RemoveAll(entry => LegacyPuzzleIds.Contains(entry.puzzleId) ||
                (entry.puzzleId.StartsWith("flag:", StringComparison.Ordinal) &&
                 LegacyFlags.Contains(entry.puzzleId.Substring("flag:".Length))));
            state.recordIds?.RemoveAll(id => id.StartsWith("REC:", StringComparison.Ordinal));
            state.timelineSnapshots?.Clear();
            state.operation = ChannelOperation.None;
            state.ClearTransientInteraction();

            if (state.roomId != ChannelZeroIds.EntryRoom)
            {
                state.roomId = ChannelZeroIds.LivingRoom;
                state.chapter = StoryChapter.Chapter1;
                state.era = ChannelEra.Year2001;
                state.roomVisualStateId = ChannelZeroIds.DefaultVisualState;
                state.SetFlag(Chapter1Flags.ServiceRequestSeen);
            }
            state.SetFlag(Chapter1Flags.LegacyMigrationNoticePending);
            state.SetFlag(Chapter1Flags.Chapter1Complete, false);
            state.saveVersion = 4;
        }
    }

    public sealed class ChannelZeroSaveService
    {
        public const string DefaultSaveKey = "ChannelZero.Session.v3";
        public const string LegacySaveKey = "ChannelZero.Session.v1";

        private readonly IChannelZeroSaveStore store;
        private readonly string saveKey;
        private readonly Dictionary<int, IChannelZeroSaveMigration> migrations;

        public ChannelZeroSaveService(IChannelZeroSaveStore store, string saveKey = DefaultSaveKey,
            IEnumerable<IChannelZeroSaveMigration> migrations = null)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.saveKey = saveKey;
            this.migrations = new Dictionary<int, IChannelZeroSaveMigration>();
            foreach (IChannelZeroSaveMigration migration in migrations ?? DefaultMigrations())
                this.migrations[migration.FromVersion] = migration;
        }

        public void Save(ChannelZeroSessionState state)
        {
            if (state == null)
                return;

            store.Write(saveKey, JsonUtility.ToJson(state));
        }

        public bool TryLoad(out ChannelZeroSessionState state)
        {
            state = null;
            if (!store.HasKey(saveKey))
                return false;

            string json = store.Read(saveKey);
            if (string.IsNullOrWhiteSpace(json))
                return false;

            if (TryDeserializeAndMigrate(json, out state))
                return true;
            if (store is IChannelZeroBackupSaveStore backup &&
                backup.TryReadBackup(saveKey, out string backupJson))
                return TryDeserializeAndMigrate(backupJson, out state);
            return false;
        }

        private bool TryDeserializeAndMigrate(string json, out ChannelZeroSessionState state)
        {
            state = null;
            try
            {
                state = JsonUtility.FromJson<ChannelZeroSessionState>(json);
                if (state == null || state.saveVersion > ChannelZeroSessionState.CurrentSaveVersion)
                {
                    state = null;
                    return false;
                }

                if (state.saveVersion <= 0)
                    state.saveVersion = 1;
                while (state.saveVersion < ChannelZeroSessionState.CurrentSaveVersion)
                {
                    if (!migrations.TryGetValue(state.saveVersion, out IChannelZeroSaveMigration migration))
                    {
                        state = null;
                        return false;
                    }
                    migration.Migrate(state);
                }
                state.NormalizeAfterLoad();
                return true;
            }
            catch (System.ArgumentException)
            {
                state = null;
                return false;
            }
        }

        public void Clear() => store.Delete(saveKey);

        public static IChannelZeroSaveStore CreateDefaultStore() =>
            new LegacyFallbackSaveStore(new FileChannelZeroSaveStore(),
                new PlayerPrefsChannelZeroSaveStore(), LegacySaveKey);

        private static IEnumerable<IChannelZeroSaveMigration> DefaultMigrations()
        {
            yield return new ChannelZeroSaveMigrationV1ToV2();
            yield return new ChannelZeroSaveMigrationV2ToV3();
            yield return new ChannelZeroSaveMigrationV3ToV4();
        }
    }
}
