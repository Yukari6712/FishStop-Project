using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// หน้าจอตาย: จอค่อยๆ มืดลง ขึ้นคำว่า YOU DIED และปุ่ม RETRY
/// กด RETRY = โหลดเซฟล่าสุด (ถ้ายังไม่เคยเซฟ = เริ่มใหม่ตั้งแต่ต้น)
/// ติดที่ Player คู่กับ SurvivalStats UI ทั้งหมดสร้างด้วยโค้ดตอนเริ่มเกม ไม่ต้องทำเอง
/// </summary>
public class DeathScreen : MonoBehaviour
{
    /// <summary>true ตั้งแต่ตาย (ใช้ให้กล้องปลดล็อกเมาส์เพื่อกดปุ่มได้)</summary>
    public static bool IsShowing { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        IsShowing = false;
        Instance = null;
    }

    /// <summary>ใช้โดย GameClock เพื่อขึ้นหน้า Game Over ตอนหมดเวลา</summary>
    public static DeathScreen Instance { get; private set; }

    [Header("References (เว้นว่างได้ ถ้าสคริปต์อยู่บน Player)")]
    public SurvivalStats stats;

    [Header("Timing (วินาที)")]
    [Tooltip("รอก่อนเริ่มมืดลง ให้ผู้เล่นเห็นช็อตที่ตาย")]
    public float showDelay = 1f;
    public float fadeTime = 1.5f;

    [Header("Text")]
    public string title = "YOU DIED";
    public string retryLabel = "RETRY";
    [Tooltip("ถ้าใช้ข้อความภาษาไทย ให้ใส่ TMP Font Asset ที่รองรับภาษาไทย")]
    public TMP_FontAsset font;

    GameObject canvasGO;
    CanvasGroup group;
    TMP_Text subtitle;
    TMP_Text titleLabel;
    Button retryButton;
    float diedAt = -1f;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (stats == null) stats = GetComponent<SurvivalStats>();
        if (stats == null) stats = FindFirstObjectByType<SurvivalStats>();
        if (stats == null)
        {
            Debug.LogError("DeathScreen: ไม่พบ SurvivalStats", this);
            enabled = false;
            return;
        }

        EnsureEventSystem();
        Build();
        stats.OnDied += OnDied;
    }

    void OnDestroy()
    {
        if (stats != null) stats.OnDied -= OnDied;
        if (canvasGO != null) Destroy(canvasGO);
        IsShowing = false;
        if (Instance == this) Instance = null;
    }

    void OnDied()
    {
        ShowGameOver(title);
    }

    /// <summary>ขึ้นหน้า Game Over พร้อมหัวข้อที่กำหนด (instant = ไม่รอ/ไม่ค่อยๆ มืด เช่นหลังวิดีโอจบ)</summary>
    public void ShowGameOver(string heading, bool instant = false)
    {
        if (titleLabel == null) return;   // ยังไม่ได้สร้าง UI
        titleLabel.text = heading;
        IsShowing = true;
        diedAt = instant ? Time.unscaledTime - showDelay - fadeTime - 0.01f : Time.unscaledTime;
        subtitle.text = SaveManager.HasCheckpoint ? "Return to your last save" : "Start over from the beginning";
    }

    void Update()
    {
        if (diedAt < 0f) return;

        float t = Time.unscaledTime - diedAt - showDelay;
        float a = fadeTime <= 0f ? (t >= 0f ? 1f : 0f) : Mathf.Clamp01(t / fadeTime);

        group.alpha = a;
        bool ready = a >= 1f;           // กดปุ่มได้หลังจอมืดสนิท
        group.interactable = ready;
        group.blocksRaycasts = ready;
    }

    void Retry()
    {
        retryButton.interactable = false;   // กันกดซ้ำ
        SaveManager.ReloadScene();
    }

    // ------------------------------------------------------------------ build

    static void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    void Build()
    {
        canvasGO = new GameObject("DeathCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
                                  typeof(GraphicRaycaster), typeof(CanvasGroup));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;      // เหนือกระเป๋า (100)

        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        group = canvasGO.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        var root = (RectTransform)canvasGO.transform;

        var bg = MakeImage("Background", root, new Color(0.04f, 0f, 0f, 0.88f));
        bg.rectTransform.anchorMin = Vector2.zero;
        bg.rectTransform.anchorMax = Vector2.one;
        bg.rectTransform.offsetMin = bg.rectTransform.offsetMax = Vector2.zero;

        var titleText = titleLabel = MakeText("Title", root, title, 110f, new Color(0.75f, 0.05f, 0.05f, 1f));
        Center(titleText.rectTransform, new Vector2(0f, 130f), new Vector2(1400f, 160f));

        var btnImg = MakeImage("RetryButton", root, Color.white);
        Center(btnImg.rectTransform, new Vector2(0f, -20f), new Vector2(340f, 84f));
        retryButton = btnImg.gameObject.AddComponent<Button>();
        retryButton.targetGraphic = btnImg;
        var colors = retryButton.colors;
        colors.normalColor = new Color(0.16f, 0.16f, 0.16f, 1f);
        colors.highlightedColor = new Color(0.40f, 0.08f, 0.08f, 1f);
        colors.pressedColor = new Color(0.55f, 0.05f, 0.05f, 1f);
        colors.selectedColor = colors.normalColor;
        colors.disabledColor = new Color(0.1f, 0.1f, 0.1f, 1f);
        retryButton.colors = colors;
        retryButton.onClick.AddListener(Retry);

        var label = MakeText("Label", btnImg.rectTransform, retryLabel, 38f, Color.white);
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;

        subtitle = MakeText("Subtitle", root, "", 24f, new Color(1f, 1f, 1f, 0.55f));
        Center(subtitle.rectTransform, new Vector2(0f, -100f), new Vector2(1200f, 40f));
    }

    static Image MakeImage(string objName, Transform parent, Color color)
    {
        var go = new GameObject(objName, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = color;
        return img;
    }

    TMP_Text MakeText(string objName, Transform parent, string text, float size, Color color)
    {
        var go = new GameObject(objName, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = text;
        t.fontSize = size;
        t.alignment = TextAlignmentOptions.Center;
        t.color = color;
        t.raycastTarget = false;
        return t;
    }

    static void Center(RectTransform rt, Vector2 position, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
    }
}
