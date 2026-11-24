using System;
using System.Globalization;
using UnityEngine;

/// <summary>
/// นาฬิกาของเกม: กลางวันและกลางคืนสลับกันทุกๆ 5 นาที (ตั้งได้) 1 วัน = กลางวัน + กลางคืน
/// ผู้เล่นมีเวลา 7 วัน ครบแล้วหมดเวลา: เล่นวิดีโอฉากจบ (EndingVideo) แล้วขึ้นหน้า Game Over
/// เวลาของเกมถูกเซฟ/โหลดไปกับระบบ Save (ตายแล้วโหลดเซฟ เวลากลับไปเป็นตอนเซฟ)
/// เวลาเดินตามเวลาในเกม จึงหยุดตอนเปิดกระเป๋า และหยุดตอนตาย
/// วางไว้บน GameObject เปล่า 1 ตัวในฉาก
/// </summary>
[DefaultExecutionOrder(-50)]
public class GameClock : MonoBehaviour, ISaveable
{
    public static GameClock Instance { get; private set; }

    /// <summary>หมดเวลา 7 วันแล้ว (ใช้บล็อกการควบคุมระหว่างฉากจบ)</summary>
    public static bool IsTimeUp { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Instance = null;
        IsTimeUp = false;
    }

    [Header("Length (นาที ตามเวลาในเกม)")]
    [Min(0.1f)] public float dayMinutes = 5f;
    [Min(0.1f)] public float nightMinutes = 5f;
    [Min(1)] public int totalDays = 7;

    [Tooltip("ช่วงท้ายของแต่ละช่วงที่แสงค่อยๆ เปลี่ยน (วินาที)")]
    [Min(0f)] public float transitionSeconds = 20f;

    [Header("Time's up")]
    public string timeUpTitle = "TIME'S UP";

    /// <summary>วันที่ปัจจุบัน เริ่มที่ 1</summary>
    public int Day { get; private set; } = 1;
    public int TotalDays => totalDays;
    public bool IsNight { get; private set; }

    /// <summary>ผ่านไปกี่วินาทีในช่วงปัจจุบัน</summary>
    public float PhaseElapsed { get; private set; }
    public float PhaseDuration => (IsNight ? nightMinutes : dayMinutes) * 60f;
    public float PhaseRemaining => Mathf.Max(0f, PhaseDuration - PhaseElapsed);

    /// <summary>0 = สว่างเต็มที่, 1 = มืดเต็มที่ (ค่อยๆ เปลี่ยนช่วงท้ายของแต่ละช่วง) ใช้กับแสง</summary>
    public float Nightness { get; private set; }

    /// <summary>เปลี่ยนช่วง ส่ง true = เข้าสู่กลางคืน, false = เช้าวันใหม่</summary>
    public event Action<bool> OnPhaseChanged;
    public event Action OnTimeUp;

    string saveKey;
    public string SaveKey => saveKey;

    void Awake()
    {
        Instance = this;
        IsTimeUp = false;
        Day = 1;
        IsNight = false;
        PhaseElapsed = 0f;
        UpdateNightness();

        saveKey = SaveManager.MakeKey(this);
        SaveManager.Register(this);
    }

    void OnDestroy()
    {
        SaveManager.Unregister(this);
        if (Instance == this) Instance = null;
        IsTimeUp = false;
    }

    void Update()
    {
        if (IsTimeUp) return;
        if (DeathScreen.IsShowing) return;   // ตายแล้วเวลาหยุด

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // ปุ่มเทสต์: N = ข้ามไปช่วงถัดไป (กลางวัน <-> กลางคืน)
        if (Input.GetKeyDown(KeyCode.N)) PhaseElapsed = PhaseDuration;
#endif

        PhaseElapsed += Time.deltaTime;

        if (PhaseElapsed >= PhaseDuration)
        {
            PhaseElapsed -= PhaseDuration;

            if (IsNight)
            {
                // จบคืน -> เช้าวันใหม่ ถ้าครบ 7 วันแล้ว = หมดเวลา
                if (Day >= totalDays)
                {
                    TimeUp();
                    return;
                }
                Day++;
                IsNight = false;
            }
            else
            {
                IsNight = true;
            }
            OnPhaseChanged?.Invoke(IsNight);
        }

        UpdateNightness();
    }

    void UpdateNightness()
    {
        float duration = PhaseDuration;
        float t = Mathf.Min(transitionSeconds, duration * 0.5f);
        float remaining = duration - PhaseElapsed;

        float n = IsNight ? 1f : 0f;
        if (t > 0f && remaining < t)
        {
            float k = 1f - remaining / t;      // 0 -> 1 ช่วงท้ายของช่วงนี้
            n = IsNight ? 1f - k : k;          // ท้ายคืน = สว่างขึ้น / ท้ายวัน = มืดลง
        }
        Nightness = Mathf.SmoothStep(0f, 1f, n);
    }

    // ------------------------------------------------------------------ time's up

    void TimeUp()
    {
        IsTimeUp = true;
        Time.timeScale = 0f;     // หยุดทุกอย่างระหว่างฉากจบ
        OnTimeUp?.Invoke();

        var ending = EndingVideo.Instance;
        if (ending == null || !ending.Play(() => ShowGameOver(true)))
            ShowGameOver(false);   // ไม่มีวิดีโอให้เล่น ไปหน้า Game Over เลย
    }

    void ShowGameOver(bool instant)
    {
        var screen = DeathScreen.Instance;
        if (screen == null)
        {
            Debug.LogWarning("GameClock: ไม่พบ DeathScreen (ใส่สคริปต์ DeathScreen ที่ Player) จึงเริ่มใหม่ทันที", this);
            SaveManager.ReloadScene();
            return;
        }
        screen.ShowGameOver(timeUpTitle, instant);
    }

    // ------------------------------------------------------------------ Save / Load

    public string CaptureState()
    {
        return string.Format(CultureInfo.InvariantCulture, "{0},{1},{2}", Day, IsNight ? 1 : 0, PhaseElapsed);
    }

    public void RestoreState(string state)
    {
        var parts = state.Split(',');
        if (parts.Length < 3) return;

        if (int.TryParse(parts[0], out int day))
            Day = Mathf.Clamp(day, 1, totalDays);
        IsNight = parts[1] == "1";
        if (float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float elapsed))
            PhaseElapsed = Mathf.Clamp(elapsed, 0f, Mathf.Max(0f, PhaseDuration - 0.01f));

        UpdateNightness();
    }
}
