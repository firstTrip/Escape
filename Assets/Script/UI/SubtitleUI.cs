using System.Collections;
using TMPro;
using UnityEngine;

public class SubtitleUI : MonoBehaviour, IPuzzleFeedbackView
{
    public static SubtitleUI Instance { get; private set; }

    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text label;
    [SerializeField] private float charactersPerSecond = 28f;
    [SerializeField] private float holdAfterComplete = 1.6f;
    [SerializeField] private float fadeDuration = 0.3f;

    private Coroutine routine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (label == null)
            label = transform.Find("Label")?.GetComponent<TMP_Text>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    public void ShowLine(string text, float extraHold = 0f)
    {
        if (label == null)
            return;
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(PlayLine(text, extraHold));
    }

    public void ShowFeedback(string message, float extraDuration = 0f) =>
        ShowLine(message, extraDuration);

    private IEnumerator PlayLine(string text, float extraHold)
    {
        label.text = "";
        yield return Fade(1f);

        float delay = 1f / Mathf.Max(1f, charactersPerSecond);
        for (int i = 0; i <= text.Length; i++)
        {
            label.text = text.Substring(0, i);
            yield return new WaitForSeconds(delay);
        }

        yield return new WaitForSeconds(holdAfterComplete + extraHold);
        yield return Fade(0f);
    }

    private IEnumerator Fade(float target)
    {
        if (canvasGroup == null) yield break;
        float start = canvasGroup.alpha;
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, target, t / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = target;
    }
}
