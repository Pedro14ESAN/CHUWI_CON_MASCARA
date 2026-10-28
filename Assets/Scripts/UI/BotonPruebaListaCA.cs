using UnityEngine;
using UnityEngine.UI;

// TEMPORAL - SOLO PARA PRUEBAS. Abre la lista de Canada desde PanelExploracion
// mientras no exista el mapa AR que llame a MostrarLista al tocar un pin.
// QUITAR cuando el mapa AR este integrado: borrar el objeto BotonTestListaCA
// del prefab UI_Navegacion (PanelExploracion/ZonaSegura) y este archivo.
[RequireComponent(typeof(Button))]
public class BotonPruebaListaCA : MonoBehaviour
{
    const string CodigoPais = "CA";

    void Awake()
    {
        var nav = GetComponentInParent<UINavigationController>(true);
        if (nav == null)
        {
            Debug.LogWarning("BotonPruebaListaCA: no hay UINavigationController en los padres.", this);
            return;
        }
        GetComponent<Button>().onClick.AddListener(() => nav.MostrarLista(CodigoPais));
    }
}
