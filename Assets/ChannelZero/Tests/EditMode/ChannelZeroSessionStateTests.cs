using System.Collections.Generic;
using ChannelZero.Runtime.Core;
using NUnit.Framework;

namespace ChannelZero.Tests.EditMode
{
    public sealed class ChannelZeroSessionStateTests
    {
        [Test]
        public void EntryToLivingRoom_UsesLogicalRoomAndChapterState()
        {
            ChannelZeroSessionState state = ChannelZeroSessionState.CreateNew();

            state.MoveTo(ChannelZeroIds.LivingRoom, StoryChapter.Chapter1);

            Assert.That(state.roomId, Is.EqualTo(ChannelZeroIds.LivingRoom));
            Assert.That(state.chapter, Is.EqualTo(StoryChapter.Chapter1));
            Assert.That(state.visitedRoomIds, Contains.Item(ChannelZeroIds.LivingRoom));
        }

        [Test]
        public void BackNavigation_RestoresPreviousRoomEraAndVisualState()
        {
            ChannelZeroSessionState state = ChannelZeroSessionState.CreateNew();
            state.SetVisualState("entry-arrival");
            state.MoveTo(ChannelZeroIds.LivingRoom, StoryChapter.Chapter1);
            state.Tune(ChannelEra.Year1961);

            Assert.That(state.TryGoBack(), Is.True);
            Assert.That(state.roomId, Is.EqualTo(ChannelZeroIds.EntryRoom));
            Assert.That(state.chapter, Is.EqualTo(StoryChapter.Prologue));
            Assert.That(state.era, Is.EqualTo(ChannelEra.Year2001));
            Assert.That(state.roomVisualStateId, Is.EqualTo("entry-arrival"));
            Assert.That(state.CanGoBack, Is.False);
        }

        [Test]
        public void Chapter2_CannotOpenDisappearingStair()
        {
            ChannelZeroSessionState state = ChannelZeroSessionState.CreateNew();
            state.MoveTo(ChannelZeroIds.WorkshopRoom, StoryChapter.Chapter2);
            state.ForeshadowDisappearingStair(lockPlanRead: true);
            state.AddItem(ChannelZeroIds.FoldingCrankItem);
            Assert.That(state.TryHold(ChannelZeroIds.FoldingCrankItem), Is.True);

            bool opened = state.TryOpenDisappearingStair(unlockOrderKnown: true);

            Assert.That(opened, Is.False);
            Assert.That(state.disappearingStairState, Is.EqualTo(DisappearingStairState.LocksKnown));
        }

        [Test]
        public void Chapter4_WithAllRequirements_CanOpenDisappearingStair()
        {
            ChannelZeroSessionState state = ChannelZeroSessionState.CreateNew();
            state.MoveTo("CentralHall", StoryChapter.Chapter4);
            state.ForeshadowDisappearingStair(lockPlanRead: true);
            state.AddItem(ChannelZeroIds.FoldingCrankItem);
            state.TryHold(ChannelZeroIds.FoldingCrankItem);

            Assert.That(state.TryOpenDisappearingStair(unlockOrderKnown: true), Is.True);
            Assert.That(state.disappearingStairState, Is.EqualTo(DisappearingStairState.Open));
        }

        [Test]
        public void SaveRoundTrip_PreservesRoomEraInventoryAndObservations()
        {
            MemoryStore store = new();
            ChannelZeroSaveService service = new(store);
            ChannelZeroSessionState state = ChannelZeroSessionState.CreateNew();
            state.MoveTo(ChannelZeroIds.LivingRoom, StoryChapter.Chapter1);
            state.Tune(ChannelEra.Year1981);
            state.Observe("Living_CRT");
            state.AddItem("item.test");

            service.Save(state);
            bool loaded = service.TryLoad(out ChannelZeroSessionState restored);

            Assert.That(loaded, Is.True);
            Assert.That(restored.roomId, Is.EqualTo(ChannelZeroIds.LivingRoom));
            Assert.That(restored.era, Is.EqualTo(ChannelEra.Year1981));
            Assert.That(restored.observedHotspotIds, Contains.Item("Living_CRT"));
            Assert.That(restored.inventoryItemIds, Contains.Item("item.test"));
            Assert.That(restored.CanGoBack, Is.True);
        }

        [Test]
        public void CloseupExit_RestoresOriginRoomEraAndVisualState()
        {
            ChannelZeroSessionState state = ChannelZeroSessionState.CreateNew();
            state.MoveTo(ChannelZeroIds.LivingRoom, StoryChapter.Chapter1);
            state.Tune(ChannelEra.Year1981);
            state.SetVisualState("crt-noise");
            state.EnterCloseup("LIV-Z01", "noise");
            state.roomId = ChannelZeroIds.WorkshopRoom;
            state.era = ChannelEra.Year2021;

            Assert.That(state.TryExitCloseup(), Is.True);
            Assert.That(state.roomId, Is.EqualTo(ChannelZeroIds.LivingRoom));
            Assert.That(state.era, Is.EqualTo(ChannelEra.Year1981));
            Assert.That(state.roomVisualStateId, Is.EqualTo("crt-noise"));
            Assert.That(state.activeCloseupId, Is.Empty);
        }

        [Test]
        public void RewindPhysicalState_PreservesReadRecords()
        {
            ChannelZeroSessionState state = ChannelZeroSessionState.CreateNew();
            state.MarkRecordRead(ChannelZeroIds.WorkshopFloorPlanCloseup);
            state.SetPuzzleState("puzzle.test", "solved");

            state.RewindPhysicalState(new[] { new PuzzleStateEntry("puzzle.test", "initial") });

            Assert.That(state.GetPuzzleState("puzzle.test"), Is.EqualTo("initial"));
            Assert.That(state.recordIds, Contains.Item(ChannelZeroIds.WorkshopFloorPlanCloseup));
        }

        private sealed class MemoryStore : IChannelZeroSaveStore
        {
            private readonly Dictionary<string, string> values = new();
            public bool HasKey(string key) => values.ContainsKey(key);
            public string Read(string key) => values.TryGetValue(key, out string value) ? value : string.Empty;
            public void Write(string key, string value) => values[key] = value;
            public void Delete(string key) => values.Remove(key);
        }
    }
}
