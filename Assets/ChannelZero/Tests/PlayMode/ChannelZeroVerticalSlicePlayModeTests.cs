using System.Collections;
using System.Collections.Generic;
using ChannelZero.Runtime.Core;
using ChannelZero.Runtime.Presentation;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ChannelZero.Tests.PlayMode
{
    public sealed class ChannelZeroVerticalSlicePlayModeTests
    {
        [TearDown]
        public void TearDown() => ChannelZeroRuntimeEnvironment.Reset();

        [UnityTest]
        public IEnumerator EntryAndOpeningSequence_EnforcesPhotoTvSofaChildOrder()
        {
            yield return LoadNewGame();
            ChannelZeroVerticalSliceController controller = Controller();
            yield return EnterLiving(controller);

            Assert.That(controller.State.GetFlag(Chapter1Flags.Photo2001BaselineSeen), Is.False);
            yield return ClickHotspot(controller, "Living_CRT", false);
            Assert.That(controller.State.activeCloseupId, Is.Empty,
                "The TV must remain locked until the baseline family photo was inspected.");

            yield return ClickHotspot(controller, "Living_Photos");
            Assert.That(controller.State.activeCloseupId, Is.EqualTo(ChannelZeroIds.LivingFamilyPhotosCloseup));
            yield return CloseCloseup(controller);
            Assert.That(controller.State.GetFlag(Chapter1Flags.Photo2001BaselineSeen), Is.True);

            yield return ClickHotspot(controller, "Living_CRT");
            Assert.That(controller.State.activeCloseupId, Is.EqualTo(ChannelZeroIds.LivingCrtFrontCloseup));
            Assert.That(controller.State.GetFlag(Chapter1Flags.ChildSofaIntroSeen), Is.False);
            Transform tvStack = GameObject.Find("TVLayerStack")?.transform;
            Assert.That(tvStack, Is.Not.Null);
            Assert.That(tvStack.GetChild(0).name, Is.EqualTo("ScreenContent"));
            Assert.That(tvStack.GetChild(5).name, Is.EqualTo("ScreenGlow"));
            Assert.That(Object.FindFirstObjectByType<ChannelZeroCloseupCanvasController>().Locale,
                Is.EqualTo("ko-KR"));
            yield return CloseCloseup(controller);
            yield return new WaitForSecondsRealtime(1.05f);
            Assert.That(controller.State.GetFlag(Chapter1Flags.SofaChildAppeared), Is.True);

            yield return ClickHotspot(controller, "Living_CRT", false);
            Assert.That(controller.State.activeCloseupId, Is.Empty,
                "Only the child is interactable during the sofa encounter.");
            yield return ClickHotspot(controller, "Living_Mina", false);
            yield return DrainNarrative(controller, 6);
            yield return new WaitForSecondsRealtime(1.05f);

            Assert.That(controller.State.GetFlag(Chapter1Flags.ChildSofaIntroSeen), Is.True);
            Assert.That(controller.State.GetFlag("ChildTVIntroSeen"), Is.False);
        }

        [UnityTest]
        public IEnumerator PhysicalRepairAndReturnCircuit_CompleteThroughSceneButtons()
        {
            yield return LoadNewGame();
            ChannelZeroVerticalSliceController controller = Controller();
            yield return EnterLiving(controller);
            yield return CompleteOpening(controller);

            yield return ClickHotspot(controller, "Living_CRT");
            yield return ClickAction(controller, "inspect_rear");
            Assert.That(controller.State.activeCloseupId, Is.EqualTo(ChannelZeroIds.LivingCrtRearCloseup));
            yield return ClickAction(controller, "inspect_screws");
            yield return CloseCloseup(controller);

            yield return ClickHotspot(controller, "Living_Toolbox");
            yield return ClickAction(controller, "open");
            yield return ClickAction(controller, "take:Screwdriver");
            yield return CloseCloseup(controller);
            Assert.That(controller.State.HasItem(ChannelZeroPuzzleIds.Screwdriver), Is.True);

            yield return ClickHotspot(controller, "Living_Toolbox");
            yield return ClickAction(controller, "open");
            yield return ClickAction(controller, "take:ReplacementTube");
            yield return CloseCloseup(controller);
            Assert.That(controller.State.HasItem(ChannelZeroPuzzleIds.ReplacementTube), Is.True);

            yield return ClickHotspot(controller, "Living_CRT");
            yield return ClickInventory(controller, 0);
            yield return ClickAction(controller, "remove_cover");
            yield return ClickAction(controller, "remove_tube");
            yield return ClickInventory(controller, 1);
            yield return ClickAction(controller, "lock_bracket");
            yield return new WaitForSecondsRealtime(1.05f);

            Assert.That(controller.State.GetFlag(Chapter1Flags.TVPhysicalRepairDone), Is.True);
            Assert.That(controller.State.GetFlag(Chapter1Flags.FirstSlip1961Done), Is.True);
            Assert.That(controller.State.era, Is.EqualTo(ChannelEra.Year1961));
            Assert.That(controller.State.HasItem(ChannelZeroPuzzleIds.ReplacementTube), Is.False);

            yield return ClickHotspot(controller, "Living_REC");
            yield return ClickAction(controller, "inspect_frame");
            yield return ClickAction(controller, "open_frame_back");
            yield return ClickAction(controller, "inspect_tone_trace");
            foreach (string color in new[] { "red", "blue", "yellow", "green" })
                yield return ClickAction(controller, "wire:" + color);
            foreach (string tube in new[] { "2", "4", "1", "3" })
                yield return ClickAction(controller, "tube:" + tube);
            yield return new WaitForSecondsRealtime(1.05f);

            Assert.That(controller.State.GetFlag(Chapter1Flags.ReturnCircuitSolved), Is.True);
            Assert.That(controller.State.GetFlag(Chapter1Flags.Photo2001ChildCreated), Is.True);
            Assert.That(controller.State.GetFlag(Chapter1Flags.ManualDialUnlocked), Is.True);
            Assert.That(controller.State.era, Is.EqualTo(ChannelEra.Year2001));
            Assert.That(controller.State.timelineSnapshots, Is.Empty);
        }

        [UnityTest]
        public IEnumerator ChapterOne_FullSceneHotspotPath_ReachesEndingDialogue()
        {
            yield return LoadNewGame();
            ChannelZeroVerticalSliceController controller = Controller();
            yield return EnterLiving(controller);
            yield return CompleteOpening(controller);
            yield return CompleteRepairAndReturn(controller);

            yield return TuneTo(controller, ChannelEra.Year1961);
            yield return ClickHotspot(controller, "Living_Lockbox");
            yield return ClickAction(controller, "leave");
            yield return CloseCloseup(controller);
            yield return TuneTo(controller, ChannelEra.Year1981);
            yield return ClickHotspot(controller, "Living_Lockbox");
            yield return ClickAction(controller, "observe_broken");
            yield return CloseCloseup(controller);
            yield return TuneTo(controller, ChannelEra.Year1961);
            yield return ClickHotspot(controller, "Living_Lockbox");
            yield return ClickAction(controller, "move");
            yield return CloseCloseup(controller);
            yield return TuneTo(controller, ChannelEra.Year1981);
            yield return ClickHotspot(controller, "Living_Lockbox");
            yield return ClickAction(controller, "confirm");
            yield return CloseCloseup(controller);
            Assert.That(controller.State.GetFlag(Chapter1Flags.VaseCausalityLearned), Is.True);

            foreach (ChannelEra era in new[]
                     { ChannelEra.Year1961, ChannelEra.Year1981, ChannelEra.Year2001, ChannelEra.Year2021 })
            {
                yield return TuneTo(controller, era);
                yield return ClickHotspot(controller, "Living_Photos");
                yield return ClickAction(controller, "observe");
                yield return CloseCloseup(controller);
            }
            Assert.That(controller.State.GetFlag(Chapter1Flags.ChildTrace2749Known), Is.True);

            yield return TuneTo(controller, ChannelEra.Year2001);
            yield return ClickHotspot(controller, "Living_NumberRug");
            foreach (string digit in new[] { "2", "7", "4", "9" })
                yield return ClickAction(controller, "digit:" + digit);
            yield return CloseCloseup(controller);
            Assert.That(controller.State.GetFlag(Chapter1Flags.SignalStabilizerSolved), Is.True);
            Assert.That(controller.State.HasItem(ChannelZeroPuzzleIds.ClockKey), Is.True);

            yield return TuneTo(controller, ChannelEra.Year1961);
            yield return ClickHotspot(controller, "Living_JinwooHand");
            yield return CloseCloseup(controller);
            Assert.That(controller.State.GetFlag(Chapter1Flags.JinwooInjurySeen), Is.True);

            yield return TuneTo(controller, ChannelEra.Year2001);
            yield return ClickHotspot(controller, "Living_JinwooHand");
            yield return ClickAction(controller, "observe_amputation");
            yield return CloseCloseup(controller);
            Assert.That(controller.State.GetFlag(Chapter1Flags.JinwooAmputationFutureSeen), Is.True);

            yield return TuneTo(controller, ChannelEra.Year1961);
            yield return ClickHotspot(controller, "Living_DisplayMedical");
            yield return ClickAction(controller, "open");
            yield return ClickAction(controller, "take:Disinfectant");
            yield return ClickAction(controller, "take:Bandage");
            yield return CloseCloseup(controller);
            yield return ClickHotspot(controller, "Living_JinwooHand");
            yield return ClickInventory(controller, ChannelZeroPuzzleIds.Disinfectant);
            yield return ClickInventory(controller, ChannelZeroPuzzleIds.Bandage);
            yield return CloseCloseup(controller);

            yield return TuneTo(controller, ChannelEra.Year2001);
            yield return ClickHotspot(controller, "Living_JinwooHand");
            yield return ClickAction(controller, "confirm_preserved");
            yield return CloseCloseup(controller);
            Assert.That(controller.State.GetFlag(Chapter1Flags.JinwooHandPreserved), Is.True);

            yield return TuneTo(controller, ChannelEra.Year1981);
            yield return ClickHotspot(controller, "Living_Clock");
            foreach (string action in new[] { "enter", "insert_key", "fix_gear", "set_0808", "release_second" })
                yield return ClickAction(controller, action);
            yield return CloseCloseup(controller);
            Assert.That(controller.State.GetFlag(Chapter1Flags.ClockKnotSolved), Is.True);
            Assert.That(controller.State.HasItem(ChannelZeroPuzzleIds.WorkshopKey), Is.True);

            yield return TuneTo(controller, ChannelEra.Year2001);
            yield return ClickHotspot(controller, ChannelZeroIds.LivingWorkshopDoor, false);
            yield return DrainNarrative(controller, 10);

            Assert.That(controller.State.roomId, Is.EqualTo(ChannelZeroIds.WorkshopRoom));
            Assert.That(controller.State.GetFlag(Chapter1Flags.WorkshopKeyUsed), Is.True);
            Assert.That(controller.State.GetFlag(Chapter1Flags.Chapter1Complete), Is.True);
            Assert.That(controller.State.HasSeenText("CH1.ENDING.CHILD.DIALOGUE.04"), Is.True);
        }

        [UnityTest]
        public IEnumerator NewChapterOne_HidesLegacyTransportControlsAndUsesOnlyYearDial()
        {
            yield return LoadNewGame();
            ChannelZeroVerticalSliceController controller = Controller();

            foreach (string objectName in new[]
            {
                "RecButton", "PlayButton", "LoadButton", "RewButton", "HoldButton",
                "RecordSlots", "TapeSlots", "MasterSlot",
            })
            {
                GameObject legacy = FindIncludingInactive(objectName);
                Assert.That(legacy == null || !legacy.activeInHierarchy, Is.True, objectName);
            }
            Assert.That(GameObject.Find("RetiredControlsMask"), Is.Not.Null);
            Assert.That(controller.State.operation, Is.EqualTo(ChannelOperation.None));
            Assert.That(controller.State.timelineSnapshots, Is.Empty);
        }

        [UnityTest]
        public IEnumerator CloseupLayout_RemainsMaskedAndTextDoesNotOverlapInventory()
        {
            yield return LoadNewGame();
            ChannelZeroVerticalSliceController controller = Controller();
            yield return ClickHotspot(controller, ChannelZeroIds.EntryMail);

            RectTransform viewport = GameObject.Find("MonitorViewport").GetComponent<RectTransform>();
            Assert.That(viewport.GetComponent<RectMask2D>(), Is.Not.Null);
            Assert.That(viewport.anchorMin.x, Is.EqualTo(0.04f).Within(0.001f));
            Assert.That(viewport.anchorMax.x, Is.EqualTo(0.89f).Within(0.001f));
            TMP_Text exactText = GameObject.Find("ExactText").GetComponent<TMP_Text>();
            ChannelZeroCloseupCanvasController closeup =
                Object.FindFirstObjectByType<ChannelZeroCloseupCanvasController>(FindObjectsInactive.Include);
            RectTransform inventory = GameObject.Find("InventoryStrip").GetComponent<RectTransform>();
            Assert.That(exactText.font, Is.Not.Null);
            Assert.That(exactText.font.name, Does.StartWith("NotoSansKR"));
            Assert.That(closeup.NarrativeLabel, Is.SameAs(exactText));
            Assert.That(closeup.CloseButtonLabel, Is.Not.Null);
            Assert.That(closeup.CloseButtonLabel, Is.InstanceOf<TextMeshProUGUI>());
            Assert.That(closeup.CloseButtonLabel.text, Is.EqualTo("닫기"));
            Assert.That(closeup.Locale, Is.EqualTo("ko-KR"));
            Assert.That(exactText.rectTransform.anchorMax.y, Is.LessThan(inventory.anchorMin.y));
            Assert.That(GameObject.Find("CrtBezelOverlay").GetComponent<Image>().enabled, Is.True);
        }

        [UnityTest]
        public IEnumerator SaveReload_ClearsTransientCloseupAndPreservesCommittedOpeningFlags()
        {
            MemorySaveStore store = new();
            ChannelZeroRuntimeEnvironment.SetSaveStoreFactory(() => store);
            SceneManager.LoadScene("ChannelZero_VerticalSlice");
            yield return null;
            ChannelZeroVerticalSliceController controller = Controller();
            yield return EnterLiving(controller);
            yield return ClickHotspot(controller, "Living_Photos");
            yield return CloseCloseup(controller);
            Assert.That(controller.State.GetFlag(Chapter1Flags.Photo2001BaselineSeen), Is.True);

            SceneManager.LoadScene("ChannelZero_VerticalSlice");
            yield return null;
            controller = Controller();
            Assert.That(controller.State.GetFlag(Chapter1Flags.Photo2001BaselineSeen), Is.True);
            Assert.That(controller.State.activeCloseupId, Is.Empty);
            Assert.That(controller.State.closeupInputLocked, Is.False);
            Assert.That(controller.State.operation, Is.EqualTo(ChannelOperation.None));
        }

        private static IEnumerator LoadNewGame()
        {
            ChannelZeroRuntimeEnvironment.SetSaveStoreFactory(() => new MemorySaveStore());
            SceneManager.LoadScene("ChannelZero_VerticalSlice");
            yield return null;
            Assert.That(Controller(), Is.Not.Null);
        }

        private static IEnumerator EnterLiving(ChannelZeroVerticalSliceController controller)
        {
            yield return ClickHotspot(controller, ChannelZeroIds.EntryMail);
            yield return CloseCloseup(controller);
            yield return ClickHotspot(controller, ChannelZeroIds.EntryLivingDoor);
            Assert.That(controller.State.roomId, Is.EqualTo(ChannelZeroIds.LivingRoom));
            Assert.That(controller.State.era, Is.EqualTo(ChannelEra.Year2001));
        }

        private static IEnumerator CompleteOpening(ChannelZeroVerticalSliceController controller)
        {
            yield return ClickHotspot(controller, "Living_Photos");
            yield return CloseCloseup(controller);
            yield return ClickHotspot(controller, "Living_CRT");
            yield return CloseCloseup(controller);
            yield return new WaitForSecondsRealtime(1.05f);
            yield return ClickHotspot(controller, "Living_Mina", false);
            yield return DrainNarrative(controller, 6);
            yield return new WaitForSecondsRealtime(1.05f);
            Assert.That(controller.State.GetFlag(Chapter1Flags.ChildSofaIntroSeen), Is.True);
        }

        private static IEnumerator CompleteRepairAndReturn(ChannelZeroVerticalSliceController controller)
        {
            yield return ClickHotspot(controller, "Living_CRT");
            yield return ClickAction(controller, "inspect_rear");
            yield return ClickAction(controller, "inspect_screws");
            yield return CloseCloseup(controller);
            yield return ClickHotspot(controller, "Living_Toolbox");
            yield return ClickAction(controller, "open");
            yield return ClickAction(controller, "take:Screwdriver");
            yield return CloseCloseup(controller);
            yield return ClickHotspot(controller, "Living_Toolbox");
            yield return ClickAction(controller, "open");
            yield return ClickAction(controller, "take:ReplacementTube");
            yield return CloseCloseup(controller);
            yield return ClickHotspot(controller, "Living_CRT");
            yield return ClickInventory(controller, ChannelZeroPuzzleIds.Screwdriver);
            yield return ClickAction(controller, "remove_cover");
            yield return ClickAction(controller, "remove_tube");
            yield return ClickInventory(controller, ChannelZeroPuzzleIds.ReplacementTube);
            yield return ClickAction(controller, "lock_bracket");
            yield return new WaitForSecondsRealtime(1.05f);
            yield return ClickHotspot(controller, "Living_REC");
            yield return ClickAction(controller, "inspect_frame");
            yield return ClickAction(controller, "open_frame_back");
            yield return ClickAction(controller, "inspect_tone_trace");
            foreach (string color in new[] { "red", "blue", "yellow", "green" })
                yield return ClickAction(controller, "wire:" + color);
            foreach (string tube in new[] { "2", "4", "1", "3" })
                yield return ClickAction(controller, "tube:" + tube);
            yield return new WaitForSecondsRealtime(1.05f);
            Assert.That(controller.State.GetFlag(Chapter1Flags.ManualDialUnlocked), Is.True);
        }

        private static IEnumerator TuneTo(ChannelZeroVerticalSliceController controller, ChannelEra target)
        {
            GameObject dialObject = GameObject.Find("ChannelDial");
            Assert.That(dialObject, Is.Not.Null);
            ChannelZeroEraDialDrag dial = dialObject.GetComponent<ChannelZeroEraDialDrag>();
            Assert.That(dial, Is.Not.Null);
            int guard = 0;
            while (controller.State.era != target && guard++ < 4)
            {
                float delta = (int)target > (int)controller.State.era ? 80f : -80f;
                PointerEventData eventData = new(EventSystem.current) { delta = new Vector2(delta, 0f) };
                dial.OnBeginDrag(eventData);
                dial.OnDrag(eventData);
                dial.OnEndDrag(eventData);
                yield return null;
                yield return DrainNarrative(controller, 2);
            }
            Assert.That(controller.State.era, Is.EqualTo(target));
        }

        private static ChannelZeroVerticalSliceController Controller() =>
            Object.FindFirstObjectByType<ChannelZeroVerticalSliceController>();

        private static GameObject FindIncludingInactive(string name)
        {
            foreach (Transform transform in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
                if (transform.name == name) return transform.gameObject;
            return null;
        }

        private static IEnumerator DrainNarrative(ChannelZeroVerticalSliceController controller, int steps)
        {
            for (int i = 0; i < steps; i++)
            {
                controller.SkipNarrative();
                yield return null;
            }
        }

        private static IEnumerator ClickHotspot(ChannelZeroVerticalSliceController controller, string logicalId,
            bool drainNarrative = true)
        {
            GameObject hotspot = GameObject.Find("Hotspot_" + logicalId);
            Assert.That(hotspot, Is.Not.Null, logicalId);
            ExecuteEvents.Execute(hotspot, new PointerEventData(EventSystem.current),
                ExecuteEvents.pointerClickHandler);
            yield return null;
            if (drainNarrative) yield return DrainNarrative(controller, 3);
        }

        private static IEnumerator ClickAction(ChannelZeroVerticalSliceController controller, string actionId)
        {
            GameObject action = GameObject.Find("PuzzleAction_" + actionId);
            Assert.That(action, Is.Not.Null, actionId);
            action.GetComponent<Button>().onClick.Invoke();
            yield return null;
            yield return DrainNarrative(controller, 3);
        }

        private static IEnumerator ClickInventory(ChannelZeroVerticalSliceController controller, int slot)
        {
            GameObject item = GameObject.Find("InventorySlot_" + slot);
            Assert.That(item, Is.Not.Null, "inventory slot " + slot);
            item.GetComponent<Button>().onClick.Invoke();
            yield return null;
            yield return DrainNarrative(controller, 3);
        }

        private static IEnumerator ClickInventory(ChannelZeroVerticalSliceController controller, string itemId)
        {
            int slot = controller.State.inventoryItemIds.IndexOf(itemId);
            Assert.That(slot, Is.GreaterThanOrEqualTo(0), itemId);
            yield return ClickInventory(controller, slot);
        }

        private static IEnumerator CloseCloseup(ChannelZeroVerticalSliceController controller)
        {
            yield return DrainNarrative(controller, 3);
            GameObject close = GameObject.Find("CloseButton");
            Assert.That(close, Is.Not.Null);
            Assert.That(close.GetComponent<Button>().interactable, Is.True);
            close.GetComponent<Button>().onClick.Invoke();
            yield return null;
            yield return DrainNarrative(controller, 3);
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
