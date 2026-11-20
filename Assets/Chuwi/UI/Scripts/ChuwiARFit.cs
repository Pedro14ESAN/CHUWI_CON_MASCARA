using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Ajusta el tamaño del mapa y de Chuwi al marcador para que se vean completos en celular o tablet,
/// y permite acercar o alejar con dos dedos (pellizco). En el editor, la rueda del mouse hace zoom.
/// ChuwiUI lo agrega solo al iniciar; no hay que tocar la escena.
/// </summary>
[DisallowMultipleComponent]
public sealed class ChuwiARFit : MonoBehaviour
{
    [Tooltip("Ancho del mapa comparado con el ancho del marcador (1 = mismo ancho que el marcador).")]
    public float mapWidthVsMarker = 1.2f;
    [Tooltip("Tamaño de Chuwi comparado con su tamaño original en la escena.")]
    public float characterScale = 0.7f;
    [Tooltip("Zoom mínimo y máximo al pellizcar.")]
    public float minZoom = 0.5f, maxZoom = 2.5f;

    public float Zoom => zoom;
    public bool Ready => content != null;

    ChuwiMapTravel travel;
    Transform target, content;
    Vector3 center;
    float baseScale = 1, zoom = 1, pinchStartDist, pinchStartZoom;
    bool pinching;

    public void Setup(ChuwiMapTravel t)
    {
        if (t == null || content != null) return;
        travel = t; target = t.transform;
        if (travel.map == null) travel.map = target.Find("Mapa_Coloreado");

        // Todo el contenido del marcador pasa a un contenedor que se puede escalar sin mover el marcador.
        content = new GameObject("Contenido_AR").transform;
        content.SetParent(target, false);
        content.localPosition = Vector3.zero; content.localRotation = Quaternion.identity; content.localScale = Vector3.one;
        var kids = new List<Transform>();
        foreach (Transform c in target) if (c != content) kids.Add(c);
        foreach (var c in kids) c.SetParent(content, true);

        if (travel.character != null && characterScale > 0 && Mathf.Abs(characterScale - 1) > .001f)
            travel.character.localScale *= characterScale;

        float markerW = MarkerWidth();
        Bounds b; bool ok = LocalBounds(out b);
        float mapW = ok ? Mathf.Max(b.size.x, b.size.z) : 0;
        center = ok ? new Vector3(b.center.x, 0, b.center.z) : Vector3.zero;
        baseScale = mapW > .001f ? Mathf.Clamp(mapWidthVsMarker * markerW / mapW, .05f, 1f) : 1f;
        if (baseScale >= .999f) center = Vector3.zero; // si ya cabe, no lo movemos
        Apply();
        ChuwiUI.Log("ARFit: marcador=" + markerW + " mapa=" + mapW + " escala=" + baseScale);
    }

    public void ResetZoom() { zoom = 1; Apply(); }

    void Apply()
    {
        if (content == null) return;
        float s = baseScale * zoom;
        content.localScale = Vector3.one * s;
        content.localPosition = -center * s;
        if (travel != null) travel.contentScale = s;
    }

    float MarkerWidth()
    {
        // ImageTargetBehaviour.GetSize() (Vuforia). Se lee por reflexion para no depender de la version.
        foreach (var mb in target.GetComponents<MonoBehaviour>())
        {
            if (mb == null || mb.GetType().Name != "ImageTargetBehaviour") continue;
            var m = mb.GetType().GetMethod("GetSize", System.Type.EmptyTypes);
            if (m != null && m.ReturnType == typeof(Vector2))
            {
                var v = (Vector2)m.Invoke(mb, null);
                if (v.x > 0) return v.x;
            }
        }
        return 100f; // tamaño configurado en la escena
    }

    bool LocalBounds(out Bounds b)
    {
        b = new Bounds(); bool any = false;
        var src = travel.map != null ? travel.map : content;
        // Se usa la malla (no Renderer.bounds) porque los renderers pueden estar apagados mientras no se ve el marcador.
        foreach (var mf in src.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf.sharedMesh == null) continue;
            if (travel.character != null && mf.transform.IsChildOf(travel.character)) continue;
            var mb = mf.sharedMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                var corner = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                var p = target.InverseTransformPoint(mf.transform.TransformPoint(corner));
                if (!any) { b = new Bounds(p, Vector3.zero); any = true; } else b.Encapsulate(p);
            }
        }
        return any;
    }

    void Update()
    {
        if (content == null || travel == null || !travel.IsVisible) { pinching = false; return; }
        float dist; bool two = TwoFingers(out dist);
        if (two)
        {
            if (!pinching) { pinching = true; pinchStartDist = Mathf.Max(dist, 1); pinchStartZoom = zoom; }
            else { zoom = Mathf.Clamp(pinchStartZoom * dist / pinchStartDist, minZoom, maxZoom); Apply(); }
        }
        else pinching = false;

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            float wheel = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(wheel) > .01f) { zoom = Mathf.Clamp(zoom * (wheel > 0 ? 1.1f : 1 / 1.1f), minZoom, maxZoom); Apply(); }
        }
#else
        float wheel = Input.mouseScrollDelta.y;
        if (Mathf.Abs(wheel) > .01f) { zoom = Mathf.Clamp(zoom * (wheel > 0 ? 1.1f : 1 / 1.1f), minZoom, maxZoom); Apply(); }
#endif
    }

    static bool TwoFingers(out float dist)
    {
        dist = 0;
#if ENABLE_INPUT_SYSTEM
        var ts = Touchscreen.current;
        if (ts == null) return false;
        Vector2 a = default, c = default; int n = 0;
        foreach (var t in ts.touches)
        {
            if (!t.press.isPressed) continue;
            if (n == 0) a = t.position.ReadValue(); else if (n == 1) c = t.position.ReadValue();
            n++;
        }
        if (n < 2) return false;
        dist = Vector2.Distance(a, c);
        return true;
#else
        if (Input.touchCount < 2) return false;
        dist = Vector2.Distance(Input.GetTouch(0).position, Input.GetTouch(1).position);
        return true;
#endif
    }
}
