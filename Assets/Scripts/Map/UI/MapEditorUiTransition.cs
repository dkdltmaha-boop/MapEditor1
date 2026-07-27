using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public sealed class MapEditorUiTransition : MonoBehaviour
{
    private const float Duration = 0.16f;

    private CanvasGroup canvasGroup;
    private RectTransform panel;
    private Coroutine transitionCoroutine;

    public void PlayIn(RectTransform targetPanel)
    {
        canvasGroup = GetComponent<CanvasGroup>();
        panel = targetPanel;

        if (!Application.isPlaying)
        {
            canvasGroup.alpha = 1f;
            panel.localScale = Vector3.one;
            return;
        }

        canvasGroup.alpha = 0f;
        panel.localScale = Vector3.one * 0.96f;
        transitionCoroutine = StartCoroutine(Animate(true));
    }

    public void Close()
    {
        if (!Application.isPlaying)
        {
            MapEditorObjectUtility.DestroyObject(gameObject);
            return;
        }

        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }

        transitionCoroutine = StartCoroutine(Animate(false));
    }

    private IEnumerator Animate(bool opening)
    {
        float elapsed = 0f;
        float startAlpha = canvasGroup.alpha;
        float targetAlpha = opening ? 1f : 0f;
        Vector3 startScale = panel.localScale;
        Vector3 targetScale = opening ? Vector3.one : Vector3.one * 0.98f;

        while (elapsed < Duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / Duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, eased);
            panel.localScale = Vector3.LerpUnclamped(startScale, targetScale, eased);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        panel.localScale = targetScale;

        if (!opening)
        {
            MapEditorObjectUtility.DestroyObject(gameObject);
        }
    }
}
