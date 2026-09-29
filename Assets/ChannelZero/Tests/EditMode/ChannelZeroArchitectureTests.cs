using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ChannelZero.Runtime.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ChannelZero.Tests.EditMode
{
    public sealed class ChannelZeroArchitectureTests
    {
        [Test]
        public void ActiveBuildScenes_DoNotDependOnLegacyPrototypeScripts()
        {
            string[] enabledScenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            Assert.That(enabledScenes, Is.Not.Empty);
            foreach (string scene in enabledScenes)
            {
                string[] legacyDependencies = AssetDatabase.GetDependencies(scene, true)
                    .Where(path => path.StartsWith("Assets/Script/", StringComparison.Ordinal))
                    .ToArray();
                Assert.That(legacyDependencies, Is.Empty,
                    $"Active scene '{scene}' must use ChannelZero runtime code, not the disabled prototype stack.");
            }
        }

        [Test]
        public void InteractionCatalog_RoutesLegacyHotspotToSingleReturnCircuitWithoutOperationVariants()
        {
            ChannelZeroInteractionCatalog catalog = ChannelZeroInteractionCatalog.LoadDefault();
            ChannelZeroInteractionRouter router = new(catalog);

            Assert.That(router.Resolve("Living_REC", ChannelOperation.None).closeupId,
                Is.EqualTo(ChannelZeroIds.LivingRecPanelCloseup));
            Assert.That(router.Resolve("Living_REC", ChannelOperation.Rec).closeupId,
                Is.EqualTo(ChannelZeroIds.LivingRecPanelCloseup));
            Assert.That(router.Resolve("Living_REC", ChannelOperation.Load).closeupId,
                Is.EqualTo(ChannelZeroIds.LivingRecPanelCloseup));
            Assert.That(router.Resolve("Living_REC", ChannelOperation.Rew).closeupId,
                Is.EqualTo(ChannelZeroIds.LivingRecPanelCloseup),
                "REW is previewed and confirmed on the CRT button; it must not hijack the REC hotspot.");

            ChannelZeroSessionState state = ChannelZeroSessionState.CreateNew();
            ChannelZeroScenarioProgressionService scenario = new(state, catalog);
            Assert.That(scenario.Evaluate(ChannelZeroIds.EntryLivingDoor).IsUnlocked, Is.False);
            state.MarkRecordRead(ChannelZeroIds.PrologueServiceRequestCloseup);
            Assert.That(scenario.Evaluate(ChannelZeroIds.EntryLivingDoor).IsUnlocked, Is.True);
        }

        [Test]
        public void InteractionCatalog_ExplicitlyRoutesEverySceneHotspot()
        {
            ChannelZeroInteractionCatalog catalog = ChannelZeroInteractionCatalog.LoadDefault();
            string scenePath = Path.Combine(Application.dataPath, "Scenes", "ChannelZero_VerticalSlice.unity");
            string sceneText = File.ReadAllText(scenePath);
            MatchCollection matches = Regex.Matches(sceneText, @"^  logicalId: (.+)$",
                RegexOptions.Multiline);
            Assert.That(matches.Count, Is.GreaterThan(0));
            foreach (Match match in matches)
            {
                string interactionId = match.Groups[1].Value.Trim();
                Assert.That(catalog.TryResolve(interactionId, ChannelOperation.None, out _), Is.True,
                    $"Missing route for {interactionId}");
            }
        }

        [Test]
        public void ContentGraphValidator_FindsNoBrokenRoutesLayoutsOrAssets()
        {
            string scenePath = Path.Combine(Application.dataPath, "Scenes", "ChannelZero_VerticalSlice.unity");
            string[] sceneIds = Regex.Matches(File.ReadAllText(scenePath), @"^  logicalId: (.+)$",
                    RegexOptions.Multiline).Cast<Match>()
                .Select(match => match.Groups[1].Value.Trim()).Distinct().ToArray();
            ChannelZeroCloseupCatalog closeups = AssetDatabase.LoadAssetAtPath<ChannelZeroCloseupCatalog>(
                "Assets/ChannelZero/Data/ChannelZeroCloseupCatalog.asset");
            var issues = ChannelZeroContentGraphValidator.Validate(sceneIds, Catalog(),
                ChannelZeroInteractionCatalog.LoadDefault(), ChannelZeroHotspotLayoutCatalog.LoadDefault(),
                NarrativeTextCatalog.LoadDefault(), closeups);
            Assert.That(issues, Is.Empty, string.Join("\n", issues));
        }

        [Test]
        public void HotspotLayoutCatalog_CoversEveryEraForPlayableRooms()
        {
            ChannelZeroHotspotLayoutCatalog layouts = ChannelZeroHotspotLayoutCatalog.LoadDefault();
            string[] ids =
            {
                "Living_CRT", "Living_Photos", "Living_NumberRug", "Living_JinwooHand",
                "Living_Clock", "Living_DisplayMedical", "Living_Lockbox", "Living_TubeCase", "Living_Toolbox",
                "Living_REC", "Living_Mina", ChannelZeroIds.LivingWorkshopDoor,
                "Workshop_Workbench", "Workshop_PartsDrawer", "Workshop_ToolBoard",
                "Workshop_TubeTester", "Workshop_Soldering", "Workshop_KeyCutter",
                "Workshop_RepairLog", ChannelZeroIds.WorkshopFloorPlan,
                ChannelZeroIds.WorkshopFoldingCrank, "Workshop_LivingDoor",
            };
            ChannelEra[] eras =
            {
                ChannelEra.Year1961, ChannelEra.Year1981, ChannelEra.Year2001, ChannelEra.Year2021,
            };

            foreach (string id in ids)
                foreach (ChannelEra era in eras)
                    Assert.That(layouts.TryResolve(id, era, out _), Is.True,
                        $"Missing hotspot layout for {id}/{(int)era}");

            layouts.TryResolve("Living_Photos", ChannelEra.Year1961, out HotspotLayoutDefinition oldPhotos);
            layouts.TryResolve("Living_Photos", ChannelEra.Year1981, out HotspotLayoutDefinition laterPhotos);
            Assert.That(oldPhotos.NormalizedTopLeftRect, Is.Not.EqualTo(laterPhotos.NormalizedTopLeftRect));
            layouts.TryResolve("Living_TubeCase", ChannelEra.Year1961, out HotspotLayoutDefinition absentCase);
            Assert.That(absentCase.enabled, Is.False);
            layouts.TryResolve("Living_DisplayMedical", ChannelEra.Year2021, out HotspotLayoutDefinition oldMedical);
            layouts.TryResolve("Living_Photos", ChannelEra.Year2021, out HotspotLayoutDefinition missingPhotoTrace);
            Assert.That(oldMedical.enabled, Is.False,
                "The inactive medical hotspot must not cover the 2021 missing-photo trace.");
            Assert.That(missingPhotoTrace.enabled, Is.True);
        }

        [Test]
        public void CloseupCatalog_UsesEraArtworkBeforeSharedFallback()
        {
            ChannelZeroCloseupCatalog catalog = ScriptableObject.CreateInstance<ChannelZeroCloseupCatalog>();
            Texture2D texture = new(4, 4);
            Sprite shared = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.zero);
            Sprite year2001 = Sprite.Create(texture, new Rect(2, 0, 2, 2), Vector2.zero);
            catalog.EditorSetArtworks(new[]
            {
                new ChannelZeroCloseupCatalog.ArtworkEntry
                {
                    closeupId = ChannelZeroIds.LivingNumberRugCloseup,
                    stateId = ChannelZeroIds.DefaultVisualState,
                    eraYear = 0,
                    artwork = shared,
                },
                new ChannelZeroCloseupCatalog.ArtworkEntry
                {
                    closeupId = ChannelZeroIds.LivingNumberRugCloseup,
                    stateId = ChannelZeroIds.DefaultVisualState,
                    eraYear = 2001,
                    artwork = year2001,
                },
            });

            Assert.That(catalog.TryGetArtwork(ChannelZeroIds.LivingNumberRugCloseup,
                ChannelZeroIds.DefaultVisualState, ChannelEra.Year2001, out Sprite exact), Is.True);
            Assert.That(exact, Is.SameAs(year2001));
            Assert.That(catalog.TryGetArtwork(ChannelZeroIds.LivingNumberRugCloseup,
                ChannelZeroIds.DefaultVisualState, ChannelEra.Year1981, out Sprite fallback), Is.True);
            Assert.That(fallback, Is.SameAs(shared));

            UnityEngine.Object.DestroyImmediate(shared);
            UnityEngine.Object.DestroyImmediate(year2001);
            UnityEngine.Object.DestroyImmediate(texture);
            UnityEngine.Object.DestroyImmediate(catalog);
        }

        [Test]
        public void CloseupCatalog_BatchAHasArtworkForEveryEra()
        {
            ChannelZeroCloseupCatalog catalog = AssetDatabase.LoadAssetAtPath<ChannelZeroCloseupCatalog>(
                "Assets/ChannelZero/Data/ChannelZeroCloseupCatalog.asset");
            Assert.That(catalog, Is.Not.Null);

            string[] closeupIds =
            {
                ChannelZeroIds.LivingNumberRugCloseup,
                ChannelZeroIds.LivingFamilyPhotosCloseup,
                ChannelZeroIds.LivingClockCloseup,
                ChannelZeroIds.WorkshopRepairLogCloseup,
            };
            ChannelEra[] eras =
            {
                ChannelEra.Year1961,
                ChannelEra.Year1981,
                ChannelEra.Year2001,
                ChannelEra.Year2021,
            };

            foreach (string closeupId in closeupIds)
            {
                HashSet<Sprite> resolved = new();
                foreach (ChannelEra era in eras)
                {
                    Assert.That(catalog.TryGetArtwork(closeupId, ChannelZeroIds.DefaultVisualState,
                        era, out Sprite artwork), Is.True, $"Missing artwork for {closeupId}/{(int)era}");
                    Assert.That(artwork.name, Does.Contain(((int)era).ToString()));
                    resolved.Add(artwork);
                }
                Assert.That(resolved, Has.Count.EqualTo(4),
                    $"Every era must resolve to a distinct sprite for {closeupId}.");
            }

            Assert.That(catalog.TryGetArtwork(ChannelZeroIds.LivingNumberRugCloseup,
                ChannelZeroIds.RugHint9VisualState, ChannelEra.Year2021, out Sprite hint9), Is.True);
            Assert.That(hint9.name, Is.EqualTo("liv_z04_number_rug_2021_hint_9_v01"));
        }

        [Test]
        public void CloseupCatalog_ToolboxHasDistinctBeforeAndAfterPickupArtwork()
        {
            ChannelZeroCloseupCatalog catalog = AssetDatabase.LoadAssetAtPath<ChannelZeroCloseupCatalog>(
                "Assets/ChannelZero/Data/ChannelZeroCloseupCatalog.asset");
            Assert.That(catalog.TryGetArtwork(ChannelZeroIds.LivingToolboxCloseup,
                ChannelZeroIds.DefaultVisualState, ChannelEra.Year2001, out Sprite before), Is.True);
            Assert.That(catalog.TryGetArtwork(ChannelZeroIds.LivingToolboxCloseup,
                "acquired", ChannelEra.Year2001, out Sprite after), Is.True);
            Assert.That(before, Is.Not.SameAs(after));
            Assert.That(after.name, Does.Contain("acquired"));
        }

        [Test]
        public void Timeline_UsesTypedPersistenceInsteadOfStateNameHeuristics()
        {
            ChannelZeroSessionState state = ChannelZeroSessionState.CreateNew();
            state.MoveTo(ChannelZeroIds.LivingRoom, StoryChapter.Chapter1);
            state.SetPuzzleState("custom.machine", "off", StatePersistence.Physical,
                ChannelZeroIds.LivingRoom);
            state.SetPuzzleState("custom.memory", "learned", StatePersistence.Knowledge,
                ChannelZeroIds.LivingRoom);
            ChannelZeroTimelineService timeline = new(state,
                new ChannelZeroInventoryService(state, Catalog()));
            timeline.Record();

            state.SetPuzzleState("custom.machine", "on", StatePersistence.Physical,
                ChannelZeroIds.LivingRoom);
            state.SetPuzzleState("custom.memory", "remembered", StatePersistence.Knowledge,
                ChannelZeroIds.LivingRoom);
            Assert.That(timeline.Rewind(), Is.True);
            Assert.That(state.GetPuzzleState("custom.machine"), Is.EqualTo("on"),
                "REW must ignore physical state that does not belong to the current era's unresolved puzzle.");
            Assert.That(state.GetPuzzleState("custom.memory"), Is.EqualTo("remembered"));
        }

        [Test]
        public void SaveMigration_UpgradesV2EntriesThenMovesLegacyChapterOneToV4Checkpoint()
        {
            MemorySaveStore store = new();
            ChannelZeroSessionState legacy = ChannelZeroSessionState.CreateNew();
            legacy.saveVersion = 2;
            legacy.roomId = ChannelZeroIds.WorkshopRoom;
            legacy.puzzleStates.Add(new PuzzleStateEntry("flag:FloorPlanSeen", "true"));
            legacy.puzzleStates.Add(new PuzzleStateEntry(ChannelZeroPuzzleIds.TubeTester, "normal"));
            legacy.timelineSnapshots.Add(new TimelineSnapshot
            {
                snapshotId = "REC:LivingRoom:2001",
                roomId = ChannelZeroIds.LivingRoom,
                era = ChannelEra.Year2001,
                puzzleStates = new List<PuzzleStateEntry>
                {
                    new(ChannelZeroPuzzleIds.Rug2749 + ".input", "2"),
                },
            });
            store.Write(ChannelZeroSaveService.DefaultSaveKey, JsonUtility.ToJson(legacy));

            ChannelZeroSaveService saves = new(store);
            Assert.That(saves.TryLoad(out ChannelZeroSessionState loaded), Is.True);
            Assert.That(loaded.saveVersion, Is.EqualTo(4));
            PuzzleStateEntry knowledge = loaded.puzzleStates.Find(entry => entry.puzzleId == "flag:FloorPlanSeen");
            PuzzleStateEntry physical = loaded.puzzleStates.Find(entry => entry.puzzleId == ChannelZeroPuzzleIds.TubeTester);
            Assert.That(knowledge.persistence, Is.EqualTo(StatePersistence.Knowledge));
            Assert.That(physical.persistence, Is.EqualTo(StatePersistence.Physical));
            Assert.That(physical.roomId, Is.EqualTo(ChannelZeroIds.WorkshopRoom));
            Assert.That(loaded.timelineSnapshots, Is.Empty,
                "The v4 single-dial flow must not retain exposed REC snapshots.");
            Assert.That(loaded.GetFlag(Chapter1Flags.LegacyMigrationNoticePending), Is.True);
        }

        [Test]
        public void FileSaveStore_RecoversBackupWhenPrimaryJsonIsCorrupt()
        {
            string directory = Path.Combine(Path.GetTempPath(), "ChannelZeroSaveTest_" + Guid.NewGuid().ToString("N"));
            try
            {
                FileChannelZeroSaveStore store = new(directory);
                ChannelZeroSaveService saves = new(store);
                ChannelZeroSessionState first = ChannelZeroSessionState.CreateNew();
                first.SetFlag("CrtRepaired");
                saves.Save(first);
                ChannelZeroSessionState second = ChannelZeroSessionState.CreateNew();
                second.SetFlag("JinwooTreated");
                saves.Save(second);

                File.WriteAllText(Path.Combine(directory, ChannelZeroSaveService.DefaultSaveKey + ".json"), "{broken");
                Assert.That(saves.TryLoad(out ChannelZeroSessionState recovered), Is.True);
                Assert.That(recovered.GetFlag("CrtRepaired"), Is.True);
                Assert.That(recovered.GetFlag("JinwooTreated"), Is.False);
            }
            finally
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
        }

        private static PuzzleDefinitionCatalog Catalog()
        {
            return PuzzleDefinitionCatalog.LoadDefault();
        }

        private sealed class MemorySaveStore : IChannelZeroSaveStore
        {
            private readonly Dictionary<string, string> values = new();
            public bool HasKey(string key) => values.ContainsKey(key);
            public string Read(string key) => values.TryGetValue(key, out string value) ? value : string.Empty;
            public void Write(string key, string value) => values[key] = value;
            public void Delete(string key) => values.Remove(key);
        }
    }
}
