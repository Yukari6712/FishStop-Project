using UnityEngine;
using UnityEngine.Rendering;

public enum ViewMode { BirdEye, FirstPerson }

/// <summary>
/// ติดที่ Main Camera (กล้องต้องไม่เป็นลูกของ Player)
/// กด V เพื่อสลับระหว่างมุมมองจากด้านบน (Bird Eye) กับ First Person แบบเลื่อนกล้องนุ่มๆ
/// ขณะเปิดกระเป๋า: ปลดล็อกเมาส์และหยุดรับ input ของกล้อง
/// </summary>
public class CameraRig : MonoBehaviour
{
    [Header("References")]
    public Camera cam;
    public Transform target;                // Player
    public Transform head;                  // Empty object ลูกของ Player ตรงตำแหน่งตา
    public Renderer[] hideInFirstPerson;    // ตัวโมเดลผู้เล่นที่จะซ่อนในมุม First Person

    [Header("Input")]
    public KeyCode toggleKey = KeyCode.V;
    public float mouseSensitivity = 2f;

    [Header("Bird Eye")]
    [Range(30f, 85f)] public float birdPitch = 60f;
    public float birdYaw = 0f;
    public float birdDistance = 16f;
    public float minDistance = 8f;
    public float maxDistance = 28f;
    public float zoomSpeed = 30f;           // ลูกกลิ้ง 1 notch ≈ 0.1 → ซูม ~3 หน่วย
    public float followSmooth = 8f;

    [Header("First Person")]
    public float maxLookPitch = 80f;

    [Header("Transition")]
    public float transitionTime = 0.6f;

    public ViewMode Mode { get; private set; } = ViewMode.BirdEye;
    public float BirdYaw => birdYaw;

    /// <summary>เปิดกระเป๋าหรืออยู่หน้าจอตาย: ปลดล็อกเมาส์และหยุดรับ input ของกล้อง</summary>
    static bool UiBlocking => InventoryUI.IsOpen || DeathScreen.IsShowing;

    /// <summary>true เมื่ออยู่ First Person เมาส์ถูกล็อก และไม่ได้เปิดกระเป๋า (ใช้หมุนมุมมองได้)</summary>
    public bool LookEnabled =>
        Mode == ViewMode.FirstPerson
        && Cursor.lockState == CursorLockMode.Locked
        && !UiBlocking;

    public event System.Action<ViewMode> OnModeChanged;

    float blend;        // 0 = Bird Eye, 1 = First Person
    float fpPitch;
    Vector3 smoothedTarget;
    bool bodyHidden;
    bool wasBlocked;

    void Awake()
    {
        if (cam == null) cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
    }

    void Start()
    {
        if (target != null) smoothedTarget = target.position;
        ApplyCursor();
    }

    void Update()
    {
        if (target == null) return;

        // เปิดกระเป๋าอยู่: ปลดล็อกเมาส์ และไม่รับ input ของกล้อง
        bool blocked = UiBlocking;
        if (blocked != wasBlocked)
        {
            wasBlocked = blocked;
            ApplyCursor();
        }

        if (!blocked)
        {
            if (Input.GetKeyDown(toggleKey))
                SetMode(Mode == ViewMode.BirdEye ? ViewMode.FirstPerson : ViewMode.BirdEye);

            if (Mode == ViewMode.BirdEye)
            {
                float scroll = Input.GetAxis("Mouse ScrollWheel");
                birdDistance = Mathf.Clamp(birdDistance - scroll * zoomSpeed, minDistance, maxDistance);
            }
            else
            {
                // Esc = ปล่อยเมาส์ (สะดวกตอนเทสต์ใน Editor), คลิกซ้าย = ล็อกกลับ
                // ข้าม Esc ที่เพิ่งถูกใช้ปิดกระเป๋าในเฟรมนี้
                bool esc = Input.GetKeyDown(KeyCode.Escape) && Time.frameCount != InventoryUI.ClosedFrame;
                if (esc)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                else if (Cursor.lockState != CursorLockMode.Locked && Input.GetMouseButtonDown(0))
                {
                    ApplyCursor();
                }

                if (LookEnabled)
                {
                    fpPitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
                    fpPitch = Mathf.Clamp(fpPitch, -maxLookPitch, maxLookPitch);
                }
            }
        }

        blend = Mathf.MoveTowards(blend, Mode == ViewMode.FirstPerson ? 1f : 0f,
                                  Time.deltaTime / Mathf.Max(0.01f, transitionTime));
    }

    void LateUpdate()
    {
        if (target == null || cam == null) return;

        smoothedTarget = Vector3.Lerp(smoothedTarget, target.position,
                                      1f - Mathf.Exp(-followSmooth * Time.deltaTime));

        // ท่ากล้องมุมสูง
        Quaternion birdRot = Quaternion.Euler(birdPitch, birdYaw, 0f);
        Vector3 birdPos = smoothedTarget + birdRot * (Vector3.back * birdDistance);

        // ท่ากล้อง First Person
        Vector3 fpPos = head != null ? head.position : target.position + Vector3.up * 1.6f;
        Quaternion fpRot = Quaternion.Euler(fpPitch, target.eulerAngles.y, 0f);

        float t = Mathf.SmoothStep(0f, 1f, blend);
        cam.transform.SetPositionAndRotation(Vector3.Lerp(birdPos, fpPos, t),
                                             Quaternion.Slerp(birdRot, fpRot, t));

        SetBodyHidden(blend > 0.85f);
    }

    public void SetMode(ViewMode mode)
    {
        if (Mode == mode) return;
        Mode = mode;
        if (mode == ViewMode.FirstPerson) fpPitch = 0f;
        ApplyCursor();
        OnModeChanged?.Invoke(mode);
    }

    void ApplyCursor()
    {
        // ล็อกเมาส์เฉพาะตอน First Person ที่ไม่ได้เปิดกระเป๋า
        bool lockCursor = Mode == ViewMode.FirstPerson && !UiBlocking;
        Cursor.lockState = lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !lockCursor;
    }

    void SetBodyHidden(bool hidden)
    {
        if (hidden == bodyHidden) return;
        bodyHidden = hidden;
        foreach (var r in hideInFirstPerson)
        {
            if (r == null) continue;
            // ซ่อนตัวแต่ยังเห็นเงา
            r.shadowCastingMode = hidden ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On;
        }
    }
}
