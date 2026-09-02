using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlayerHealthHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    [SerializeField] private PlayerActor playerActor;
    [SerializeField] private Vector2 offset = new Vector2(0f, 44f);
    [SerializeField] private Vector2 tooltipSize = new Vector2(120f, 34f);

    private RectTransform tooltipRoot;
    private Text tooltipText;
    private bool isHovering;

    private void Awake()
    {
        if (!playerActor)
        {
            playerActor = GetComponentInParent<PlayerActor>();
        }

        BuildTooltip();
        SetTooltipVisible(false);
    }

    private void OnEnable()
    {
        if (playerActor)
        {
            playerActor.HealthChanged += HandleHealthChanged;
        }
    }

    private void OnDisable()
    {
        if (playerActor)
        {
            playerActor.HealthChanged -= HandleHealthChanged;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        UpdateTooltip(eventData);
        SetTooltipVisible(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        SetTooltipVisible(false);
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (isHovering)
        {
            UpdateTooltip(eventData);
        }
    }

    private void HandleHealthChanged(Actor actor)
    {
        if (isHovering)
        {
            UpdateTooltipText();
        }
    }

    private void BuildTooltip()
    {
        if (tooltipRoot) return;

        Canvas canvas = GetComponentInParent<Canvas>();
        Transform parent = canvas ? canvas.transform : transform;

        GameObject root = new GameObject("HealthValueTooltip");
        tooltipRoot = root.AddComponent<RectTransform>();
        tooltipRoot.SetParent(parent, false);
        tooltipRoot.sizeDelta = tooltipSize;
        tooltipRoot.pivot = new Vector2(0.5f, 0f);

        Image background = root.AddComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.78f);
        background.raycastTarget = false;

        GameObject textObject = new GameObject("Text");
        RectTransform textRect = textObject.AddComponent<RectTransform>();
        textRect.SetParent(tooltipRoot, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        tooltipText = textObject.AddComponent<Text>();
        tooltipText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tooltipText.fontSize = 16;
        tooltipText.alignment = TextAnchor.MiddleCenter;
        tooltipText.color = Color.white;
        tooltipText.raycastTarget = false;
    }

    private void UpdateTooltip(PointerEventData eventData)
    {
        UpdateTooltipText();

        if (!tooltipRoot || eventData == null) return;

        Canvas canvas = tooltipRoot.GetComponentInParent<Canvas>();
        RectTransform canvasRect = canvas ? canvas.transform as RectTransform : null;
        if (canvasRect && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint))
        {
            tooltipRoot.anchoredPosition = localPoint + offset;
            return;
        }

        tooltipRoot.position = eventData.position + offset;
    }

    private void UpdateTooltipText()
    {
        if (!tooltipText || !playerActor) return;

        int currentHealth = Mathf.CeilToInt(Mathf.Max(0f, playerActor.health));
        int maxHealth = Mathf.CeilToInt(Mathf.Max(0f, playerActor.maxHealth));
        tooltipText.text = $"{currentHealth} / {maxHealth}";
    }

    private void SetTooltipVisible(bool visible)
    {
        if (tooltipRoot)
        {
            tooltipRoot.gameObject.SetActive(visible);
        }
    }
}
