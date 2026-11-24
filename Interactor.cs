using System;
using UnityEngine;

/// <summary>
/// ติดที่ Player ใช้กด E โต้ตอบกับวัตถุที่ implement IInteractable
/// - Bird Eye: ชี้เมาส์ที่วัตถุ (ต้องอยู่ในระยะ interactRange จากตัวผู้เล่น)
/// - First Person: เล็งด้วยกลางจอ
/// </summary>
public class Interactor : MonoBehaviour
{
    public CameraRig cameraRig;
    public float interactRange = 3f;
    public float maxRayDistance = 200f;
    public KeyCode interactKey = KeyCode.E;
    public LayerMask mask = ~0;

    public IInteractable Current { get; private set; }
    public string CurrentPrompt => Current != null ? Current.Prompt : "";

    SurvivalStats stats;

    void Awake()
    {
        stats = GetComponent<SurvivalStats>();
    }

    void Update()
    {
        // ตาย หรือเปิดกระเป๋าอยู่ = โต้ตอบกับโลกภายนอกไม่ได้
        if ((stats != null && stats.IsDead) || InventoryUI.IsOpen || GameClock.IsTimeUp)
        {
            Current = null;
            return;
        }

        Current = FindTarget();
        if (Current != null && Input.GetKeyDown(interactKey))
            Current.Interact(gameObject);
    }

    IInteractable FindTarget()
    {
        if (cameraRig == null || cameraRig.cam == null) return null;
        Camera cam = cameraRig.cam;

        Ray ray = cameraRig.Mode == ViewMode.FirstPerson
            ? cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f))
            : cam.ScreenPointToRay(Input.mousePosition);

        RaycastHit[] hits = Physics.RaycastAll(ray, maxRayDistance, mask, QueryTriggerInteraction.Collide);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            // ข้ามตัวผู้เล่นเอง
            if (hit.collider.transform.IsChildOf(transform)) continue;

            // วัตถุแรกที่โดน ถ้าไม่ใช่ของที่โต้ตอบได้ = ถูกบัง
            var interactable = hit.collider.GetComponentInParent<IInteractable>();
            if (interactable == null || !interactable.CanInteract) return null;

            Vector3 flat = hit.point - transform.position;
            flat.y = 0f;
            return flat.magnitude <= interactRange ? interactable : null;
        }
        return null;
    }
}
