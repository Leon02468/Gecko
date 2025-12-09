using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class PanelSlideController : MonoBehaviour
{
    public RectTransform panelRect;       // if null, will use this.gameObject's RectTransform
    public float animTime = 0.35f;
    public float offscreenOffset = 800f;  // how far above the panel starts (increase for big screens)
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1); // can edit in Inspector
    public bool fadeDuringSlide = true;

    CanvasGroup cg;
    Vector2 shownPos;
    Vector2 hiddenPos;
    Coroutine running;

    void Awake()
    {
        if (panelRect == null) panelRect = GetComponent<RectTransform>();
        cg = GetComponent<CanvasGroup>();

        if (panelRect == null)
        {
            Debug.LogError("PanelSlideController needs a RectTransform.");
            enabled = false;
            return;
        }

        // Save shown position and compute hidden (above) position
        shownPos = panelRect.anchoredPosition;
        hiddenPos = shownPos + new Vector2(0, offscreenOffset);

        // Start hidden
        panelRect.anchoredPosition = hiddenPos;
        cg.alpha = 0f;
        gameObject.SetActive(false);
    }

    // Call this instead of SetActive(true)
    public void Show()
    {
        // If already visible and animating, ignore
        if (gameObject.activeSelf && running != null) return;

        gameObject.SetActive(true);
        if (running != null) StopCoroutine(running);
        running = StartCoroutine(Slide(hiddenPos, shownPos, true));
    }

    // Call this instead of SetActive(false)
    public void Hide()
    {
        // If already hidden, ignore
        if (!gameObject.activeSelf && running == null) return;

        if (running != null) StopCoroutine(running);
        running = StartCoroutine(Slide(shownPos, hiddenPos, false));
    }

    IEnumerator Slide(Vector2 from, Vector2 to, bool showing)
    {
        float t = 0f;
        while (t < animTime)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / animTime);
            float e = ease.Evaluate(p);
            panelRect.anchoredPosition = Vector2.LerpUnclamped(from, to, e);

            if (fadeDuringSlide)
            {
                cg.alpha = showing ? e : 1 - e;
            }

            yield return null;
        }

        panelRect.anchoredPosition = to;
        cg.alpha = showing ? 1f : 0f;

        // If we just hid it, actually disable the GameObject to block interactions
        if (!showing)
        {
            gameObject.SetActive(false);
        }

        running = null;
    }
}
