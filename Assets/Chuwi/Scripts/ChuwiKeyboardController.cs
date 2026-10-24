using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public sealed class ChuwiKeyboardController : MonoBehaviour
{
    [Tooltip("Velocidad en alturas del personaje por segundo. Se adapta a su escala.")]
    [Min(0)] public float speedInBodyHeights = 0.65f;
    [Min(0)] public float turnDegreesPerSecond = 540f;
    [Tooltip("Opcional. Si está vacío, se utiliza la cámara principal.")]
    public Camera movementCamera;

    private Animator animator;
    private static readonly int Moving = Animator.StringToHash("Moving");
    private Vector3 fallbackForward;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        animator.applyRootMotion = false;
        fallbackForward = transform.parent != null ? transform.parent.forward : Vector3.forward;
    }

    private void Update()
    {
        Vector2 input = Application.isFocused ? ReadKeyboard() : Vector2.zero;
        Move(input, Time.deltaTime);
    }

    private static Vector2 ReadKeyboard()
    {
#if ENABLE_INPUT_SYSTEM
        var keyboard = Keyboard.current;
        if (keyboard == null) return Vector2.zero;
        return new Vector2(
            ((keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) ? 1 : 0)
                - ((keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) ? 1 : 0),
            ((keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) ? 1 : 0)
                - ((keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) ? 1 : 0));
#elif ENABLE_LEGACY_INPUT_MANAGER
        return new Vector2(
            ((Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) ? 1 : 0)
                - ((Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) ? 1 : 0),
            ((Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) ? 1 : 0)
                - ((Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) ? 1 : 0));
#else
        return Vector2.zero;
#endif
    }

    // Also allows deterministic simulation in editor validation without sending keys.
    public void Move(Vector2 input, float deltaTime)
    {
        if (animator == null) animator = GetComponent<Animator>();
        Vector3 up = transform.parent != null ? transform.parent.up : Vector3.up;
        Camera camera = movementCamera != null ? movementCamera : Camera.main;
        Vector3 forward = camera != null ? Vector3.ProjectOnPlane(camera.transform.forward, up) : Vector3.ProjectOnPlane(fallbackForward, up);
        if (forward.sqrMagnitude < 0.001f && camera != null)
            forward = Vector3.ProjectOnPlane(camera.transform.up, up);
        if (forward.sqrMagnitude < 0.001f)
            forward = transform.parent != null ? transform.parent.forward : Vector3.forward;
        forward.Normalize();
        Vector3 right = Vector3.Cross(up, forward).normalized;
        Vector2 clamped = Vector2.ClampMagnitude(input, 1f);
        Vector3 direction = right * clamped.x + forward * clamped.y;
        bool moving = direction.sqrMagnitude > 0.0001f && speedInBodyHeights > 0;
        if (animator != null && animator.isActiveAndEnabled && animator.runtimeAnimatorController != null)
            animator.SetBool(Moving, moving);
        if (!moving || deltaTime <= 0) return;
        float speed = speedInBodyHeights * 1.2f * Mathf.Abs(transform.lossyScale.y);
        transform.position += direction * speed * deltaTime;
        transform.rotation = Quaternion.RotateTowards(transform.rotation,
            Quaternion.LookRotation(direction, up), turnDegreesPerSecond * deltaTime);
    }

    private void OnDisable()
    {
        if (animator != null && animator.isActiveAndEnabled && animator.runtimeAnimatorController != null)
            animator.SetBool(Moving, false);
    }
}
