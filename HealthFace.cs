using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// แสดงสีหน้าตัวละครตามเลือดที่เหลือ (แทนหลอดเลือด)
/// วิธีใช้: สร้าง UI Image ใน Canvas ของ HUD (วางตำแหน่ง/ขนาดตามใจ) แล้วใส่สคริปต์นี้
///
/// สถานะเรียงจาก "เลือดน้อยสุด" ไปถึง "เลือดเต็ม" แต่ละสถานะใส่รูปได้ 1 รูป (รูปนิ่ง) หรือหลายรูป (เล่นวนเป็นแอนิเมชัน)
/// Unity เล่นไฟล์ .gif ตรงๆ ไม่ได้ ให้แยก GIF เป็นรูปทีละเฟรมก่อน แล้วลากทุกเฟรมใส่ช่อง Frames
/// ไม่มีสีหน้าตอนตาย เพราะเลือดหมดแล้วเกมตัดไปหน้า Game Over (DeathScreen) ทันที
///
/// จำนวนสถานะไม่ต้องเท่ากับจำนวนหน่วยเลือดก็ได้ (เช่นเลือด 5 หน่วย แต่ทำ 4 สีหน้า) ระบบจะจับคู่ตามสัดส่วนเลือดให้
/// </summary>
[RequireComponent(typeof(Image))]
public class HealthFace : MonoBehaviour
{
    [Serializable]
    public class FaceState
    {
        public string label;
        [Tooltip("1 รูป = รูปนิ่ง / หลายรูป = เล่นเป็นแอนิเมชันวนซ้ำ (แทน GIF)")]
        public Sprite[] frames;
        [Tooltip("ความเร็วแอนิเมชัน (เฟรมต่อวินาที) ใช้เมื่อมีหลายรูป")]
        public float framesPerSecond = 8f;
    }

    public SurvivalStats stats;

    [Tooltip("เรียงจากสีหน้าตอนเลือดน้อยสุด (เหลือ 1 หน่วย) ไปถึงตอนเลือดเต็ม")]
    public FaceState[] states = new FaceState[0];

    Image image;
    int current = -1;
    float frameTimer;

    // ใส่คอมโพเนนต์ครั้งแรกใน Editor จะได้ช่องสถานะ 5 ช่องให้เลย
    void Reset()
    {
        states = new FaceState[5];
        for (int i = 0; i < states.Length; i++)
        {
            string note = i == 0 ? " (lowest)" : i == states.Length - 1 ? " (full)" : "";
            states[i] = new FaceState { label = "Health " + (i + 1) + note };
        }
    }

    void Awake()
    {
        image = GetComponent<Image>();
        image.preserveAspect = true;
        image.raycastTarget = false;
    }

    void Start()
    {
        if (stats == null) stats = FindFirstObjectByType<SurvivalStats>();
        if (stats == null)
        {
            Debug.LogWarning("HealthFace: ไม่พบ SurvivalStats", this);
            enabled = false;
        }
    }

    void Update()
    {
        if (states == null || states.Length == 0)
        {
            image.enabled = false;
            return;
        }

        int index = ChooseState();
        if (index != current)
        {
            current = index;
            frameTimer = 0f;   // เริ่มแอนิเมชันของสีหน้าใหม่จากเฟรมแรก
        }

        var state = states[index];
        if (state == null || state.frames == null || state.frames.Length == 0)
        {
            image.enabled = false;   // สถานะนี้ยังไม่ได้ใส่รูป
            return;
        }

        int frame = 0;
        if (state.frames.Length > 1 && state.framesPerSecond > 0f)
        {
            // ใช้เวลาแบบไม่ถูกหยุด เพื่อให้สีหน้ายังขยับตอนเปิดกระเป๋า
            float loop = state.frames.Length / state.framesPerSecond;
            frameTimer = (frameTimer + Time.unscaledDeltaTime) % loop;
            frame = Mathf.Min(state.frames.Length - 1, Mathf.FloorToInt(frameTimer * state.framesPerSecond));
        }

        image.sprite = state.frames[frame];
        image.enabled = image.sprite != null;
    }

    /// <summary>
    /// เลือกสถานะสีหน้า ตอนนี้ดูจากเลือดอย่างเดียว
    /// ถ้าจะเพิ่มสถานการณ์อื่นทีหลัง (เช่นโดนตี ตกใจ) มาเพิ่มเงื่อนไขที่ฟังก์ชันนี้
    /// </summary>
    int ChooseState()
    {
        int n = states.Length;
        int index = Mathf.CeilToInt(stats.Health01 * n) - 1;   // เลือดเต็ม = สถานะสุดท้าย, เหลือ 1 หน่วย = สถานะแรกๆ
        return Mathf.Clamp(index, 0, n - 1);
    }
}
