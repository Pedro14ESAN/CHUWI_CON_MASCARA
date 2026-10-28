using UnityEngine;

// Ajusta este RectTransform al area segura de la pantalla (notch, camara
// perforada, barra de gestos). Debe colgar de un RectTransform a pantalla
// completa dentro de un Canvas Screen Space - Overlay. Solo actua en runtime,
// para no ensuciar el prefab en el editor.
[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    RectTransform rectTransform;
    Rect ultimaArea;
    Vector2 ultimaPantalla;

    void Awake()
    {
        rectTransform = (RectTransform)transform;
        Aplicar();
    }

    void Update()
    {
        if (Screen.safeArea != ultimaArea || Screen.width != ultimaPantalla.x || Screen.height != ultimaPantalla.y)
            Aplicar();
    }

    void Aplicar()
    {
        ultimaArea = Screen.safeArea;
        ultimaPantalla = new Vector2(Screen.width, Screen.height);
        Ajustar(rectTransform, ultimaArea, ultimaPantalla);
    }

    // Publico y estatico para poder probarlo con un area segura simulada.
    public static void Ajustar(RectTransform rt, Rect area, Vector2 pantalla)
    {
        if (pantalla.x <= 0 || pantalla.y <= 0) return;
        rt.anchorMin = new Vector2(area.xMin / pantalla.x, area.yMin / pantalla.y);
        rt.anchorMax = new Vector2(area.xMax / pantalla.x, area.yMax / pantalla.y);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
