using UnityEngine;

/// <summary>
/// ไอเท็มในโลกที่กด E เก็บเข้ากระเป๋า (อาหารหรือไอเท็มอื่น ตาม ItemData) ต้องมี Collider (ติ๊ก Is Trigger ได้)
/// ทุกชนิดกินช่องกระเป๋า 1 ช่อง กระเป๋าเต็มจะเก็บไม่ได้
/// เมื่อโหลดเซฟ ไอเท็มที่ถูกเก็บไปก่อนเซฟจะไม่โผล่อีก ส่วนที่เก็บหลังเซฟจะกลับมาวางที่เดิม
/// (ของที่ดรอประหว่างเล่นจะไม่ถูกจำในเซฟ)
/// </summary>
public class PickupItem : MonoBehaviour, IInteractable
{
    public ItemData item;

    /// <summary>false = ของที่เกิดระหว่างเล่น (เช่นดรอปจากม่อน) ไม่บันทึกลงเซฟ ตั้งโดยสคริปต์ที่สร้างมัน</summary>
    [HideInInspector] public bool saveProgress = true;

    string saveKey;

    public string Prompt => item != null ? $"[E] Take {item.itemName}" : "";
    public bool CanInteract => item != null;

    void Awake()
    {
        saveKey = SaveManager.MakeKey(this);
    }

    void Start()
    {
        // เซฟบอกว่าชิ้นนี้ถูกเก็บไปแล้ว
        if (saveProgress && SaveManager.IsCollected(saveKey)) Destroy(gameObject);
    }

    public void Interact(GameObject interactor)
    {
        if (item == null) return;

        var inv = interactor.GetComponent<Inventory>();
        if (inv == null) return;

        if (!inv.TryAdd(item))
        {
            GameMessages.Show("Backpack is full");
            return;
        }

        if (saveProgress) SaveManager.MarkCollected(saveKey);
        GameMessages.Show($"Picked up {item.itemName}");
        Destroy(gameObject);
    }
}
