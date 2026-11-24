using UnityEngine;

/// <summary>
/// เปลี่ยนแสงตามเวลาของ GameClock: สีและความสว่างของแสงอาทิตย์/จันทร์, แสงโดยรอบ (Ambient),
/// ความสว่างของท้องฟ้า (Skybox) และหมอก (เลือกเปิดได้)
/// ใส่ที่ GameObject ไหนก็ได้ในฉาก (เช่นอันเดียวกับ GameClock) แล้วลาก Directional Light ใส่ช่อง Sun
/// หมายเหตุ: ใช้ได้กับค่าเริ่มต้นของ Unity (Environment Lighting Source = Skybox)
/// </summary>
public class DayNightLighting : MonoBehaviour
{
    [Header("References (เว้นว่างได้ จะหาให้เอง)")]
    public GameClock clock;
    public Light sun;

    [Header("Sun / Moon")]
    public Color dayLightColor = new Color(1f, 0.96f, 0.86f);
    public float dayIntensity = 1f;
    public Color nightLightColor = new Color(0.30f, 0.38f, 0.65f);
    public float nightIntensity = 0.08f;

    [Header("Ambient (แสงโดยรอบ)")]
    public bool controlAmbient = true;
    public float dayAmbient = 1f;
    public float nightAmbient = 0.08f;

    [Header("Skybox (ใช้ได้กับ Skybox ที่มีค่า Exposure เช่น Procedural)")]
    public bool controlSkybox = true;
    public float daySkyExposure = 1.2f;
    public float nightSkyExposure = 0.05f;

    [Header("Fog (ปิดไว้ก่อน)")]
    public bool controlFog = false;
    public Color dayFogColor = new Color(0.70f, 0.78f, 0.85f);
    public Color nightFogColor = new Color(0.02f, 0.03f, 0.06f);
    public float dayFogDensity = 0.005f;
    public float nightFogDensity = 0.03f;

    Material skyInstance;
    float nextGiUpdate;

    void Start()
    {
        if (clock == null) clock = GameClock.Instance;
        if (clock == null) clock = FindFirstObjectByType<GameClock>();
        if (clock == null)
        {
            Debug.LogWarning("DayNightLighting: ไม่พบ GameClock", this);
            enabled = false;
            return;
        }

        if (sun == null) sun = RenderSettings.sun;
        if (sun == null)
        {
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (l.type != LightType.Directional) continue;
                sun = l;
                break;
            }
        }

        // ใช้สำเนาของ Skybox เพื่อไม่ให้ไปแก้ไฟล์ Material ต้นฉบับในโปรเจกต์
        if (controlSkybox)
        {
            var sky = RenderSettings.skybox;
            if (sky != null && sky.HasProperty("_Exposure"))
            {
                skyInstance = new Material(sky);
                RenderSettings.skybox = skyInstance;
            }
            else
            {
                controlSkybox = false;
            }
        }

        if (controlFog) RenderSettings.fog = true;
        Apply();
    }

    void OnDestroy()
    {
        if (skyInstance != null) Destroy(skyInstance);
    }

    void Update()
    {
        Apply();
    }

    void Apply()
    {
        float n = clock.Nightness;

        if (sun != null)
        {
            sun.color = Color.Lerp(dayLightColor, nightLightColor, n);
            sun.intensity = Mathf.Lerp(dayIntensity, nightIntensity, n);
        }

        if (controlAmbient)
            RenderSettings.ambientIntensity = Mathf.Lerp(dayAmbient, nightAmbient, n);

        if (controlSkybox && skyInstance != null)
            skyInstance.SetFloat("_Exposure", Mathf.Lerp(daySkyExposure, nightSkyExposure, n));

        if (controlFog)
        {
            RenderSettings.fogColor = Color.Lerp(dayFogColor, nightFogColor, n);
            RenderSettings.fogDensity = Mathf.Lerp(dayFogDensity, nightFogDensity, n);
        }

        // แสงโดยรอบที่มาจากท้องฟ้าต้องคำนวณใหม่ (ทำเป็นช่วงๆ ไม่ให้หนัก)
        if ((controlAmbient || controlSkybox) && Time.unscaledTime >= nextGiUpdate)
        {
            nextGiUpdate = Time.unscaledTime + 0.5f;
            DynamicGI.UpdateEnvironment();
        }
    }
}
