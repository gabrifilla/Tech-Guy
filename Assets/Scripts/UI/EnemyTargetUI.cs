using UnityEngine;
using UnityEngine.UI;

public class EnemyTargetUI : MonoBehaviour
{
    private const string DefaultObjectName = "EnemyTargetUI";
    private static EnemyTargetUI instance;

    [SerializeField] private RectTransform root;
    [SerializeField] private Text nameText;
    [SerializeField] private Image healthFill;

    private Actor target;
    private float targetMaxHealth;
    private float healthFillWidth;

    public static EnemyTargetUI Instance => instance;

    public static EnemyTargetUI Ensure()
    {
        if (instance != null) return instance;

        GameObject go = new GameObject(DefaultObjectName);
        EnemyTargetUI ui = go.AddComponent<EnemyTargetUI>();
        instance = ui;
        ui.BuildUi();
        return ui;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        if (root == null || nameText == null || healthFill == null)
        {
            BuildUi();
        }

        SetTarget(null);
    }

    private void Update()
    {
        UpdateUi(false);
    }

    public void SetTarget(Actor actor)
    {
        target = actor;
        targetMaxHealth = 0f;
        healthFillWidth = 0f;
        if (target != null)
        {
            targetMaxHealth = Mathf.Max(target.maxHealth, target.health, 0.0001f);
        }
        UpdateUi(true);
    }

    public void ClearTarget()
    {
        target = null;
        targetMaxHealth = 0f;
        healthFillWidth = 0f;
        UpdateUi(true);
    }

    public void NotifyHealthChanged(Actor actor)
    {
        if (actor == null || target == null || actor != target) return;

        UpdateUi(true);
    }

    private void UpdateUi(bool force)
    {
        if (root == null || nameText == null || healthFill == null)
        {
            return;
        }

        if (target == null)
        {
            if (root.gameObject.activeSelf)
            {
                root.gameObject.SetActive(false);
            }
            return;
        }

        if (!root.gameObject.activeSelf)
        {
            root.gameObject.SetActive(true);
        }

        nameText.text = target.name;
        if (target.maxHealth > targetMaxHealth)
        {
            targetMaxHealth = target.maxHealth;
        }
        if (target.health > targetMaxHealth)
        {
            targetMaxHealth = target.health;
        }

        float maxHealth = Mathf.Max(targetMaxHealth, 0.0001f);
        float fill = Mathf.Clamp01(target.health / maxHealth);
        SetHealthFill(fill);
    }

    private void BuildUi()
    {
        if (root != null && nameText != null && healthFill != null) return;
        if (gameObject == null) return;

        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
        }
        if (canvas == null)
        {
            Debug.LogError("EnemyTargetUI: failed to create Canvas component.");
            return;
        }
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        if (GetComponent<CanvasScaler>() == null)
        {
            gameObject.AddComponent<CanvasScaler>();
        }
        if (GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }

        GameObject rootObj = new GameObject("Root");
        root = rootObj.AddComponent<RectTransform>();
        root.SetParent(transform, false);
        root.anchorMin = new Vector2(0.5f, 1f);
        root.anchorMax = new Vector2(0.5f, 1f);
        root.pivot = new Vector2(0.5f, 1f);
        root.anchoredPosition = new Vector2(0f, -20f);
        root.sizeDelta = new Vector2(360f, 60f);

        GameObject nameObj = new GameObject("EnemyName");
        RectTransform nameRect = nameObj.AddComponent<RectTransform>();
        nameRect.SetParent(root, false);
        nameRect.anchorMin = new Vector2(0f, 1f);
        nameRect.anchorMax = new Vector2(1f, 1f);
        nameRect.pivot = new Vector2(0.5f, 1f);
        nameRect.anchoredPosition = new Vector2(0f, 0f);
        nameRect.sizeDelta = new Vector2(0f, 20f);

        nameText = nameObj.AddComponent<Text>();
        Font builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (builtinFont == null)
        {
            Debug.LogWarning("EnemyTargetUI: LegacyRuntime.ttf not found; using default font.");
        }
        nameText.font = builtinFont;
        nameText.fontSize = 18;
        nameText.alignment = TextAnchor.MiddleCenter;
        nameText.color = Color.white;
        nameText.text = "Enemy";

        GameObject backObj = new GameObject("EnemyHealthBack");
        RectTransform backRect = backObj.AddComponent<RectTransform>();
        backRect.SetParent(root, false);
        backRect.anchorMin = new Vector2(0.5f, 1f);
        backRect.anchorMax = new Vector2(0.5f, 1f);
        backRect.pivot = new Vector2(0.5f, 1f);
        backRect.anchoredPosition = new Vector2(0f, -26f);
        backRect.sizeDelta = new Vector2(300f, 14f);

        Image backImage = backObj.AddComponent<Image>();
        backImage.color = new Color(0f, 0f, 0f, 0.6f);

        GameObject fillObj = new GameObject("EnemyHealthFill");
        RectTransform fillRect = fillObj.AddComponent<RectTransform>();
        fillRect.SetParent(backRect, false);
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.anchoredPosition = Vector2.zero;
        fillRect.sizeDelta = new Vector2(backRect.sizeDelta.x, 0f);

        healthFill = fillObj.AddComponent<Image>();
        healthFill.color = new Color(0.9f, 0.2f, 0.2f, 1f);
        healthFill.type = Image.Type.Filled;
        healthFill.fillMethod = Image.FillMethod.Horizontal;
        healthFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        healthFill.fillAmount = 1f;
        healthFillWidth = backRect.sizeDelta.x;

        root.gameObject.SetActive(false);
    }

    private void SetHealthFill(float fill)
    {
        if (healthFill == null) return;

        healthFill.type = Image.Type.Filled;
        healthFill.fillMethod = Image.FillMethod.Horizontal;
        healthFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        healthFill.fillAmount = fill;
        healthFill.SetVerticesDirty();

        RectTransform fillRect = healthFill.rectTransform;
        if (fillRect == null) return;

        fillRect.pivot = new Vector2(0f, 0.5f);

        if (healthFillWidth <= 0f)
        {
            healthFillWidth = fillRect.rect.width;
            if (healthFillWidth <= 0f && fillRect.parent is RectTransform parentRect)
            {
                healthFillWidth = parentRect.rect.width;
            }
        }

        if (healthFillWidth > 0f)
        {
            fillRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, healthFillWidth * fill);
            fillRect.localScale = Vector3.one;
        }
        else
        {
            fillRect.localScale = new Vector3(fill, 1f, 1f);
        }
    }
}
