using UnityEngine;

/// <summary>
/// วัตถุที่ผู้เล่นกด E โต้ตอบได้ (คันโยก, ประตู, ไอเท็ม ฯลฯ)
/// </summary>
public interface IInteractable
{
    /// <summary>ข้อความที่แสดงบน HUD เมื่อเล็งหรือเอาเมาส์ชี้</summary>
    string Prompt { get; }

    /// <summary>ถ้า false จะไม่แสดง prompt และกด E ไม่ได้</summary>
    bool CanInteract { get; }

    void Interact(GameObject interactor);
}
