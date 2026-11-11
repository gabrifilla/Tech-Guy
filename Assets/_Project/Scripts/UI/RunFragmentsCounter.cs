using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Fase 1b: discreet run counter of Fragmentos de Dados (top-right, small, no raycast), so it never competes with
/// the life and resource orbs. Owned by the run director; shows only this run's gains.
/// </summary>
public sealed class RunFragmentsCounter : MonoBehaviour
{
    private static readonly Color Cyan = new Color(.30f, .92f, 1f, .92f);
    private TMP_Text _text;
    public int Value { get; private set; }

    public static RunFragmentsCounter Create(Transform owner)
    {
        var root = new GameObject("Run fragments counter", typeof(RectTransform));
        if (owner) root.transform.SetParent(owner, false);
        var counter = root.AddComponent<RunFragmentsCounter>();
        var canvas = root.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 40;
        var scaler = root.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
        var label = new GameObject("Text", typeof(RectTransform)); label.transform.SetParent(root.transform, false);
        var rect = (RectTransform)label.transform; rect.anchorMin = rect.anchorMax = new Vector2(1, 1); rect.pivot = new Vector2(1, 1);
        rect.sizeDelta = new Vector2(360, 40); rect.anchoredPosition = new Vector2(-28, -22);
        counter._text = label.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset) counter._text.font = TMP_Settings.defaultFontAsset;
        counter._text.fontSize = 24; counter._text.color = Cyan; counter._text.alignment = TextAlignmentOptions.TopRight;
        counter._text.raycastTarget = false; counter._text.richText = false;
        counter.Set(0);
        return counter;
    }

    public void Set(int value)
    {
        Value = value;
        if (_text) _text.text = "Fragmentos: " + value;
    }
}
