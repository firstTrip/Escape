using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ChannelZero.Runtime.Presentation
{
    [DisallowMultipleComponent]
    public sealed class Chapter1GlitchFrameSequencePlayer : MonoBehaviour
    {
        public static readonly string[] SofaPlaceholderFrameIds =
        {
            "CH1_SOFA_01_SEATED", "CH1_SOFA_02_HEAD_UP", "CH1_SOFA_03_TURN",
            "CH1_SOFA_04_HALF_RISE", "CH1_SOFA_05_STANDING",
            "CH1_SOFA_06_STEP_FORWARD", "CH1_SOFA_07_CUSHION_ONLY", "CH1_SOFA_08_EMPTY",
        };

        public static readonly string[] EraTransitionPlaceholderFrameIds =
        {
            "CH1_SLIP_01_2001_DOMINANT", "CH1_SLIP_02_FIRST_MIX",
            "CH1_SLIP_03_FURNITURE_SWAP_A", "CH1_SLIP_04_FURNITURE_SWAP_B",
            "CH1_SLIP_05_SIGNAL_TEAR", "CH1_SLIP_06_1961_MIX",
            "CH1_SLIP_07_1961_DOMINANT",
        };

        private static readonly float[] IrregularDurations =
        {
            0.07f, 0.12f, 0.09f, 0.18f, 0.11f, 0.15f, 0.08f, 0.14f,
        };

        [SerializeField] private Image completedFrameImage;
        [SerializeField] private Sprite[] firstTvRoomGlitchFrames = Array.Empty<Sprite>();
        [SerializeField] private Sprite[] sofaEncounterFrames = Array.Empty<Sprite>();
        [SerializeField] private Sprite[] eraTransitionFrames = Array.Empty<Sprite>();
        [SerializeField] private bool reduceFlashing;
        [SerializeField] private bool reduceMotion;

        public bool HasApprovedSofaSequence => HasValidFrames(sofaEncounterFrames, 6, 8);
        public bool HasApprovedEraTransition => HasValidFrames(eraTransitionFrames, 5, 8);
        public bool HasFirstTvRoomGlitch => HasValidFrames(firstTvRoomGlitchFrames, 6, 6);

        private void Awake()
        {
            LoadApprovedResourcesIfNeeded();
        }

        public void PlayFirstTvRoomGlitch(Action completed)
        {
            LoadApprovedResourcesIfNeeded();
            if (!HasFirstTvRoomGlitch)
            {
                Hide();
                completed?.Invoke();
                return;
            }
            Play(firstTvRoomGlitchFrames, 0, completed, Chapter1RebuildResources.GlitchFrameDurations);
        }

        public void ShowSofaChild()
        {
            EnsureImage();
            if (!HasApprovedSofaSequence) return;
            completedFrameImage.sprite = sofaEncounterFrames[0];
            completedFrameImage.gameObject.SetActive(true);
        }

        public void PlaySofaApproachAndDisappear(Action completed)
        {
            if (!HasApprovedSofaSequence)
            {
                Hide();
                completed?.Invoke();
                return;
            }
            Play(sofaEncounterFrames, 1, completed);
        }

        public void PlayEraTransition(Action completed)
        {
            if (!HasApprovedEraTransition)
            {
                Hide();
                completed?.Invoke();
                return;
            }
            Play(eraTransitionFrames, 0, completed);
        }

        public void PlayEraTransitionReverse(Action completed)
        {
            if (!HasApprovedEraTransition)
            {
                Hide();
                completed?.Invoke();
                return;
            }
            Sprite[] reversed = (Sprite[])eraTransitionFrames.Clone();
            Array.Reverse(reversed);
            Play(reversed, 0, completed);
        }

        private void Play(Sprite[] frames, int startIndex, Action completed, float[] durations = null)
        {
            EnsureImage();
            StopAllCoroutines();
            StartCoroutine(PlayFrames(frames, startIndex, completed, durations));
        }

        private IEnumerator PlayFrames(Sprite[] frames, int startIndex, Action completed, float[] durations)
        {
            completedFrameImage.gameObject.SetActive(true);
            for (int i = Mathf.Clamp(startIndex, 0, frames.Length - 1); i < frames.Length; i++)
            {
                completedFrameImage.sprite = frames[i];
                float authoredDuration = durations != null && durations.Length > 0
                    ? durations[i % durations.Length]
                    : IrregularDurations[i % IrregularDurations.Length];
                float duration = reduceMotion ? Mathf.Max(authoredDuration, 0.18f) : authoredDuration;
                if (reduceFlashing) duration = Mathf.Max(duration, 0.12f);
                yield return new WaitForSecondsRealtime(duration);
            }
            Hide();
            completed?.Invoke();
        }

        private void EnsureImage()
        {
            if (completedFrameImage != null) return;
            Transform roomView = GameObject.Find("RoomView")?.transform;
            if (roomView == null) return;
            GameObject frame = new("Chapter1CompletedFrameSequence", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            frame.transform.SetParent(roomView, false);
            RectTransform rect = frame.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            completedFrameImage = frame.GetComponent<Image>();
            completedFrameImage.preserveAspect = false;
            completedFrameImage.raycastTarget = false;
            frame.transform.SetSiblingIndex(Mathf.Max(1, roomView.childCount - 2));
            frame.SetActive(false);
        }

        private void Hide()
        {
            if (completedFrameImage != null) completedFrameImage.gameObject.SetActive(false);
        }

        private void LoadApprovedResourcesIfNeeded()
        {
            if (!HasFirstTvRoomGlitch)
                firstTvRoomGlitchFrames = Chapter1RebuildResources.LoadSprites(
                    Chapter1RebuildResources.TvGlitchRoomPaths);
            if (!HasApprovedSofaSequence)
                sofaEncounterFrames = Chapter1RebuildResources.LoadSprites(Chapter1RebuildResources.SofaPaths);
            if (!HasApprovedEraTransition)
                eraTransitionFrames = Chapter1RebuildResources.LoadSprites(Chapter1RebuildResources.TimeSlipPaths);
        }

        private static bool HasValidFrames(Sprite[] frames, int minimum, int maximum)
        {
            if (frames == null || frames.Length < minimum || frames.Length > maximum) return false;
            foreach (Sprite frame in frames) if (frame == null) return false;
            return true;
        }

#if UNITY_EDITOR
        public void EditorConfigure(Image image, Sprite[] sofaFrames, Sprite[] transitionFrames,
            Sprite[] roomGlitchFrames = null)
        {
            completedFrameImage = image;
            sofaEncounterFrames = sofaFrames ?? Array.Empty<Sprite>();
            eraTransitionFrames = transitionFrames ?? Array.Empty<Sprite>();
            firstTvRoomGlitchFrames = roomGlitchFrames ?? Array.Empty<Sprite>();
        }
#endif
    }
}
