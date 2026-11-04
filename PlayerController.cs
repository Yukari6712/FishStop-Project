using UnityEngine;

/// <summary>
/// ควบคุมตัวละคร
/// - Bird Eye: WASD เดินตามทิศของหน้าจอ ตัวละครหันหาเมาส์
/// - First Person: WASD เดินตามทิศที่หัน เมาส์หมุนตัว
/// Shift = วิ่ง (ใช้สเตมินา)
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("References")]
    public CameraRig cameraRig;

    [Header("Movement")]
    public float walkSpeed = 4f;
    public float sprintSpeed = 7f;
    public float acceleration = 12f;
    public float gravity = -20f;
    public float staminaDrainPerSecond = 18f;

    [Header("Bird Eye Facing")]
    public bool faceMouseInBirdEye = true;  // false = หันตามทิศที่เดิน
    public float turnSpeed = 720f;

    CharacterController controller;
    SurvivalStats stats;
    Vector3 planarVelocity;
    float verticalVelocity;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        stats = GetComponent<SurvivalStats>();
    }

    void Update()
    {
        if (cameraRig == null) return;
        if (stats != null && stats.IsDead) return;

        bool firstPerson = cameraRig.Mode == ViewMode.FirstPerson;

        // หมุนตัวด้วยเมาส์ใน First Person
        if (cameraRig.LookEnabled)
            transform.Rotate(0f, Input.GetAxis("Mouse X") * cameraRig.mouseSensitivity, 0f);

        // ทิศทางที่กด
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        input = Vector2.ClampMagnitude(input, 1f);
        if (InventoryUI.IsOpen) input = Vector2.zero;   // เปิดกระเป๋าอยู่: ไม่เดิน/ไม่วิ่ง

        Quaternion basis = firstPerson
            ? Quaternion.Euler(0f, transform.eulerAngles.y, 0f)
            : Quaternion.Euler(0f, cameraRig.BirdYaw, 0f);
        Vector3 wishDir = basis * new Vector3(input.x, 0f, input.y);

        // วิ่ง
        bool moving = input.sqrMagnitude > 0.01f;
        bool sprinting = moving && Input.GetKey(KeyCode.LeftShift) && (stats == null || stats.CanSprint);
        if (sprinting && stats != null)
            stats.DrainStamina(staminaDrainPerSecond * Time.deltaTime);

        float speed = sprinting ? sprintSpeed : walkSpeed;
        planarVelocity = Vector3.Lerp(planarVelocity, wishDir * speed,
                                      1f - Mathf.Exp(-acceleration * Time.deltaTime));

        // แรงโน้มถ่วง
        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        else verticalVelocity += gravity * Time.deltaTime;

        controller.Move((planarVelocity + Vector3.up * verticalVelocity) * Time.deltaTime);

        // หันตัวใน Bird Eye
        if (!firstPerson)
        {
            if (faceMouseInBirdEye)
            {
                if (!InventoryUI.IsOpen) FaceMouse();   // เปิดกระเป๋าอยู่: ไม่หันตามเมาส์
            }
            else if (wishDir.sqrMagnitude > 0.01f) RotateToward(wishDir);
        }
    }

    void FaceMouse()
    {
        Camera cam = cameraRig.cam;
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.up, transform.position);
        if (!plane.Raycast(ray, out float dist)) return;

        Vector3 dir = ray.GetPoint(dist) - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.05f) RotateToward(dir);
    }

    void RotateToward(Vector3 dir)
    {
        Quaternion goal = Quaternion.LookRotation(dir, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, goal, turnSpeed * Time.deltaTime);
    }
}
