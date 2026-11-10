using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Fase 1a placeholder: a non-blocking text band ("UNIVERSO 1 CONCLUÍDO") shown when the sector boss is finalized.
/// It has no raycaster and no raycast targets, so it never captures input; it fades in, holds and fades out over
/// <c>duration</c> seconds of unscaled time (paused time is not counted) and then destroys itself.
/// Palette matches the run-results sheet (gold on a dark band).
/// </summary>
public sealed class UniverseVictoryBanner : MonoBehaviour
{
    private static readonly Color Gold = new Color(.83f, .71f, .46f, 1);
    private static readonly Color Band = new Color(.05f, .06f, .08f, .72f);
    private const float FadeIn = .25f, FadeOut = .45f;

    private CanvasGroup _group;
    private float _duration, _elapsed;

    public string Text { get; private set; }

    public static UniverseVictoryBanner Show(Transform owner, string text, float duration)
    {
        var root = new GameObject("Universe victory banner", typeof(RectTransform));
        if (owner) root.transform.SetParent(owner, false);
        var banner = root.AddComponent<UniverseVictoryBanner>();
        banner.Build(text, duration);
        return banner;
    }

    private void Build(string text, float duration)
    {
        Text = text; _duration = Mathf.Max(.1f, duration);
        var canvas = gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 150;
        var scaler = gameObject.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
        _group = gameObject.AddComponent<CanvasGroup>(); _group.alpha = 0; _group.interactable = false; _group.blocksRaycasts = false;

        var band = new GameObject("Band", typeof(RectTransform)); band.transform.SetParent(transform, false);
        var bandRect = (RectTransform)band.transform; bandRect.anchorMin = new Vector2(0, .5f); bandRect.anchorMax = new Vector2(1, .5f);
        bandRect.sizeDelta = new Vector2(0, 150); bandRect.anchoredPosition = new Vector2(0, 120);
        var image = band.AddComponent<Image>(); image.color = Band; image.raycastTarget = false;

        var label = new GameObject("Text", typeof(RectTransform)); label.transform.SetParent(band.transform, false);
        var labelRect = (RectTransform)label.transform; labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one; labelRect.sizeDelta = Vector2.zero;
        var tmp = label.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset) tmp.font = TMP_Settings.defaultFontAsset;
        tmp.text = text; tmp.fontSize = 72; tmp.fontStyle = FontStyles.Bold; tmp.characterSpacing = 8;
        tmp.color = Gold; tmp.alignment = TextAlignmentOptions.Center; tmp.raycastTarget = false; tmp.richText = false;
    }

    private void Update()
    {
        if (Time.timeScale <= 0f) return; // paused: hold the current frame of the fade
        _elapsed += Time.unscaledDeltaTime;
        float fadeOutStart = Mathf.Max(FadeIn, _duration - FadeOut);
        _group.alpha = _elapsed < FadeIn ? _elapsed / FadeIn
            : _elapsed < fadeOutStart ? 1f
            : Mathf.Clamp01(1f - (_elapsed - fadeOutStart) / FadeOut);
        if (_elapsed >= _duration) Destroy(gameObject);
    }
}
