using System.Collections;
using UnityEngine;

public class PrologueSequence : MonoBehaviour
{
    [SerializeField] private CanvasGroup hotspotLayer;
    [SerializeField] private AudioSource rainLoopSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip phoneRingClip;
    [SerializeField] private AudioClip voicemailHumClip;
    [TextArea][SerializeField] private string voicemailLine = "...오늘은 말할 수 있을까.";
    [SerializeField] private float delayBeforeRing = 1.0f;
    [SerializeField] private float delayAfterRing = 0.6f;

    private bool playing;

    private void Start()
    {
        SetExplorable(false);
        if (rainLoopSource != null)
        {
            rainLoopSource.loop = true;
            rainLoopSource.Play();
        }
        StartCoroutine(FadeInThenCall());
    }

    private IEnumerator FadeInThenCall()
    {
        if (FadeScreen.Instance != null)
            yield return FadeScreen.Instance.FadeTo(0f, 1.2f);
        yield return PlayVoicemailRoutine();
        SetExplorable(true);
    }

    public void ReplayVoicemail()
    {
        if (playing) return;
        StartCoroutine(PlayVoicemailRoutine());
    }

    private IEnumerator PlayVoicemailRoutine()
    {
        playing = true;
        yield return new WaitForSeconds(delayBeforeRing);
        if (sfxSource != null && phoneRingClip != null)
        {
            sfxSource.PlayOneShot(phoneRingClip);
            yield return new WaitForSeconds(phoneRingClip.length);
        }
        yield return new WaitForSeconds(delayAfterRing);
        if (sfxSource != null && voicemailHumClip != null)
        {
            sfxSource.PlayOneShot(voicemailHumClip);
        }
        SubtitleUI.Instance?.ShowLine(voicemailLine, 1.5f);
        playing = false;
    }

    private void SetExplorable(bool value)
    {
        if (hotspotLayer == null) return;
        hotspotLayer.interactable = value;
        hotspotLayer.blocksRaycasts = value;
    }
}
