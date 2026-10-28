using UnityEngine;
using UnityEngine.InputSystem;

public class MouseLookController : MonoBehaviour
{
    public float sensitivity = 0.08f;
    public float maxYaw = 28f;
    public float maxPitch = 12f;
    float yaw;
    float pitch;
    public bool mapMode = false;
    public bool MapMode => mapMode;
    public float moveSpeed = 3.2f;
    public float roomMinX = -10.2f;
    public float roomMaxX = 10.2f;
    public float roomMinZ = -5.2f;
    public float roomMaxZ = 15.2f;
    public void SetMapMode(bool enabled)
    {
        mapMode = enabled;
        Cursor.visible = mapMode;
        Cursor.lockState = mapMode ? CursorLockMode.None : CursorLockMode.Locked;
    }
    void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
        {
            SetMapMode(!mapMode);
        }
        // Режим карты освобождает мышь для выбора линий, но не блокирует
        // перемещение камеры: по карте можно ходить клавишами WASD.
        if (!mapMode && Mouse.current != null)
        {
            var delta = Mouse.current.delta.ReadValue();
            yaw = Mathf.Clamp(yaw + delta.x * sensitivity, -maxYaw, maxYaw);
            pitch = Mathf.Clamp(pitch - delta.y * sensitivity, -maxPitch, maxPitch);
            transform.localRotation = Quaternion.Euler(pitch, yaw, 0f);
        }
        if (Keyboard.current != null)
        {
            Vector3 input = new Vector3(
                (Keyboard.current.dKey.isPressed ? 1f : 0f) - (Keyboard.current.aKey.isPressed ? 1f : 0f),
                0f,
                (Keyboard.current.wKey.isPressed ? 1f : 0f) - (Keyboard.current.sKey.isPressed ? 1f : 0f));
            if (input.sqrMagnitude > 0.01f)
            {
                Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
                Vector3 right = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
                transform.position += (forward * input.z + right * input.x).normalized * moveSpeed * Time.unscaledDeltaTime;
                transform.position = new Vector3(Mathf.Clamp(transform.position.x, roomMinX, roomMaxX), transform.position.y, Mathf.Clamp(transform.position.z, roomMinZ, roomMaxZ));
            }
        }
    }

    void OnDestroy()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
}
