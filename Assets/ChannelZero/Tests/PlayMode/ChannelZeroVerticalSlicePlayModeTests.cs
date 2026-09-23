using System.Collections;
using ChannelZero.Runtime.Core;
using ChannelZero.Runtime.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ChannelZero.Tests.PlayMode
{
    public sealed class ChannelZeroVerticalSlicePlayModeTests
    {
        [UnityTest]
        public IEnumerator EntryDoorClick_OpensLivingRoomAndEraSwitchReplacesWholeBackground()
        {
            SceneManager.LoadScene("ChannelZero_VerticalSlice");
            yield return null;

            ChannelZeroVerticalSliceController controller =
                Object.FindFirstObjectByType<ChannelZeroVerticalSliceController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.State.roomId, Is.EqualTo(ChannelZeroIds.EntryRoom));
            Assert.That(controller.State.era, Is.EqualTo(ChannelEra.Year2001));
            Assert.That(GameObject.Find("BackButton").GetComponent<Button>().interactable, Is.False);

            GameObject mail = GameObject.Find("Hotspot_" + ChannelZeroIds.EntryMail);
            ExecuteEvents.Execute(mail, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
            yield return null;

            controller.SkipNarrative();
            yield return null;

            ChannelZeroCloseupCanvasController closeup =
                Object.FindFirstObjectByType<ChannelZeroCloseupCanvasController>();
            Assert.That(closeup.IsOpen, Is.True);
            Assert.That(controller.State.activeCloseupId,
                Is.EqualTo(ChannelZeroIds.PrologueServiceRequestCloseup));
            Assert.That(controller.State.recordIds,
                Contains.Item(ChannelZeroIds.PrologueServiceRequestCloseup));

            GameObject.Find("CloseButton").GetComponent<Button>().onClick.Invoke();
            yield return null;
            controller.SkipNarrative();
            yield return null;
            Assert.That(closeup.IsOpen, Is.False);
            Assert.That(controller.State.roomId, Is.EqualTo(ChannelZeroIds.EntryRoom));
            Assert.That(controller.State.era, Is.EqualTo(ChannelEra.Year2001));

            GameObject entryDoor = GameObject.Find("Hotspot_" + ChannelZeroIds.EntryLivingDoor);
            Assert.That(entryDoor, Is.Not.Null);
            ExecuteEvents.Execute(entryDoor, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
            yield return null;

            controller.SkipNarrative();
            yield return null;
            Assert.That(controller.State.roomId, Is.EqualTo(ChannelZeroIds.LivingRoom));
            Assert.That(controller.State.era, Is.EqualTo(ChannelEra.Year2001));
            Assert.That(GameObject.Find("BackButton").GetComponent<Button>().interactable, Is.True);
            Image background = GameObject.Find("RoomBackground").GetComponent<Image>();
            Assert.That(background.sprite.name, Is.EqualTo("bg_livingroom_2001_play_v01"));

            controller.Tune1961();
            yield return null;

            Assert.That(controller.State.era, Is.EqualTo(ChannelEra.Year1961));
            Assert.That(background.sprite.name, Is.EqualTo("bg_livingroom_1961_play_v01"));
            Assert.That(Object.FindObjectsByType<Collider>(FindObjectsInactive.Include, FindObjectsSortMode.None), Is.Empty);
            Assert.That(Object.FindObjectsByType<Collider2D>(FindObjectsInactive.Include, FindObjectsSortMode.None), Is.Empty);

            GameObject.Find("BackButton").GetComponent<Button>().onClick.Invoke();
            yield return null;

            Assert.That(controller.State.roomId, Is.EqualTo(ChannelZeroIds.EntryRoom));
            Assert.That(controller.State.era, Is.EqualTo(ChannelEra.Year2001));
            Assert.That(background.sprite.name, Is.EqualTo("bg_entryhall_2001_arrival_v01"));
            Assert.That(GameObject.Find("BackButton"), Is.Not.Null);
            Assert.That(GameObject.Find("BackButton").GetComponent<Button>().interactable, Is.False);
        }
    }
}
