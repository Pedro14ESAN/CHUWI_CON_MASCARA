using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Efecto de presion para botones (se encoge un poco al tocar).</summary>
public sealed class ChuwiPress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    float target = 1;
    public void OnPointerDown(PointerEventData e) { target = .94f; ChuwiUI.Log("pointer down en " + name); }
    public void OnPointerUp(PointerEventData e) { target = 1; }
    public void OnPointerExit(PointerEventData e) { target = 1; }
    void Update()
    {
        float s = Mathf.MoveTowards(transform.localScale.x, target, Time.unscaledDeltaTime * 1.5f);
        transform.localScale = new Vector3(s, s, 1);
    }
}
