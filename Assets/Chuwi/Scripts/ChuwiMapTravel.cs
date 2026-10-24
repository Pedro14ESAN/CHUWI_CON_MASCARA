using System;
using UnityEngine;
using Vuforia;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>Destinos del prototipo; no representa convenios confirmados.</summary>
public sealed class ChuwiMapTravel : MonoBehaviour
{
    [Serializable] public class Destination
    {
        public string country;
        public string continent;
        public Transform anchor;
    }
    public Transform character;
    public Transform pins;
    public Transform map;
    public Destination[] destinations;
    public float speed = 40;
    public float feetOffset;
    public float pinSize = 6;
    public bool previewWithoutMarker;
    ObserverBehaviour observer;
    Animator animator;
    ChuwiKeyboardController keyboard;
    bool keyboardWasEnabled, tracked, moving, greeted;
    int selected = -1;
    string continent = "";
    Vector2 scroll;
    Renderer[] characterRenderers, pinRenderers, mapRenderers;

    void OnEnable()
    {
        observer = GetComponent<ObserverBehaviour>();
        if (observer != null) observer.OnTargetStatusChanged += OnStatus;
        animator = character != null ? character.GetComponent<Animator>() : null;
        keyboard = character != null ? character.GetComponent<ChuwiKeyboardController>() : null;
        if (keyboard != null) { keyboardWasEnabled = keyboard.enabled; keyboard.enabled = false; }
        characterRenderers = character != null ? character.GetComponentsInChildren<Renderer>(true) : new Renderer[0];
        pinRenderers = pins != null ? pins.GetComponentsInChildren<Renderer>(true) : new Renderer[0];
        if (map == null) map = transform.Find("Mapa_Coloreado");
        mapRenderers = map != null ? map.GetComponentsInChildren<Renderer>(true) : new Renderer[0];
        greeted = false; moving = false; selected = -1;
        if (character != null && destinations != null && destinations.Length > 0)
            character.position = transform.TransformPoint(transform.InverseTransformPoint(destinations[0].anchor.position) + Vector3.up * feetOffset);
        if (observer != null) OnStatus(observer, observer.TargetStatus);
    }
    void OnDisable()
    {
        if (observer != null) observer.OnTargetStatusChanged -= OnStatus;
        SetMoving(false);
        if (keyboard != null) keyboard.enabled = keyboardWasEnabled;
    }
    void OnStatus(ObserverBehaviour sender, TargetStatus status)
    {
        tracked = status.Status == Status.TRACKED || status.Status == Status.EXTENDED_TRACKED;
    }
    void SetMoving(bool value)
    {
        if (animator != null && animator.isActiveAndEnabled && animator.runtimeAnimatorController != null)
            animator.SetBool("Moving", value);
    }
    public void SelectDestination(int index)
    {
        if (!(tracked || previewWithoutMarker) || destinations == null || index < 0 || index >= destinations.Length || destinations[index].anchor == null) return;
        selected = index; moving = true;
        if (animator != null) { animator.ResetTrigger("Greeting"); animator.ResetTrigger("Pointing"); }
    }
    void LateUpdate()
    {
        Tick(Time.deltaTime);
        if (tracked || previewWithoutMarker) ReadPointer();
    }
    public void Tick(float deltaTime)
    {
        bool visible = tracked || previewWithoutMarker;
        foreach (var r in characterRenderers) if (r != null) r.enabled = visible;
        foreach (var r in pinRenderers) if (r != null) r.enabled = visible;
        foreach (var r in mapRenderers) if (r != null) r.enabled = visible;
        if (!visible) { SetMoving(false); return; }
        if (!greeted) { greeted = true; if (animator != null) animator.SetTrigger("Greeting"); }
        if (moving && selected >= 0 && character != null)
        {
            Vector3 goal = transform.InverseTransformPoint(destinations[selected].anchor.position) + Vector3.up * feetOffset;
            Vector3 current = transform.InverseTransformPoint(character.position);
            Vector3 delta = goal - current;
            Vector3 next = Vector3.MoveTowards(current, goal, speed * Mathf.Max(0, deltaTime));
            character.position = transform.TransformPoint(next);
            Vector3 forward = Vector3.ProjectOnPlane(transform.TransformDirection(delta), transform.up);
            if (forward.sqrMagnitude > .0001f)
                character.rotation = Quaternion.RotateTowards(character.rotation, Quaternion.LookRotation(forward, transform.up), 360 * Mathf.Max(0, deltaTime));
            moving = (next - goal).sqrMagnitude > .001f;
            if (!moving) character.position = transform.TransformPoint(goal);
            SetMoving(moving);
            if (!moving && animator != null) animator.SetTrigger("Pointing");
        }
    }
    void ReadPointer()
    {
        Vector2 point = Vector2.zero; bool pressed = false;
#if ENABLE_INPUT_SYSTEM
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        { pressed = true; point = Touchscreen.current.primaryTouch.position.ReadValue(); }
        else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        { pressed = true; point = Mouse.current.position.ReadValue(); }
#else
        pressed = Input.GetMouseButtonDown(0); point = Input.mousePosition;
#endif
        float scale = Mathf.Max(1, Screen.height / 720f);
        if (!pressed || point.x < 292 * scale || Camera.main == null) return;
        Ray ray = Camera.main.ScreenPointToRay(point);
        if (Physics.Raycast(ray, out RaycastHit hit))
            for (int i = 0; i < destinations.Length; i++)
                if (hit.transform.IsChildOf(destinations[i].anchor)) { SelectDestination(i); break; }
    }
    void OnGUI()
    {
        if (destinations == null) return;
        float scale = Mathf.Max(1, Screen.height / 720f);
        Matrix4x4 old = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * scale);
        GUILayout.BeginArea(new Rect(12, 12, 268, Mathf.Min(650, Screen.height / scale - 24)), GUI.skin.box);
        GUILayout.Label("ESAN | Explora con Chuwi");
        if (!(tracked || previewWithoutMarker)) GUILayout.Label("Enfoca el marcador para ver el mapa.");
        else
        {
            GUILayout.Label("Elige una región y un destino");
            foreach (string region in new[] { "América del Norte", "América del Sur", "Asia" })
                if (GUILayout.Button(region, GUILayout.Height(36))) continent = region;
            scroll = GUILayout.BeginScrollView(scroll);
            for (int i = 0; i < destinations.Length; i++)
                if (destinations[i].continent == continent || i == 0)
                    if (GUILayout.Button(destinations[i].country, GUILayout.Height(36))) SelectDestination(i);
            GUILayout.EndScrollView();
            if (selected >= 0) GUILayout.Label((moving ? "Viajando a " : "Destino: ") + destinations[selected].country);
            GUILayout.Label("Universidades y oportunidades: pendientes de información oficial.");
        }
        GUILayout.EndArea(); GUI.matrix = old;
    }
}
