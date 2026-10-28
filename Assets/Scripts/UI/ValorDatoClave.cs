using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Va en el Valor de cada fila de "Datos clave". Oculta la linea de puntos cuando
// el valor no cabe en una linea: partido en varias, los puntos quedan reducidos
// a un par y parecen un error. Se reevalua cada vez que el layout cambia su ancho.
[RequireComponent(typeof(TMP_Text))]
public class ValorDatoClave : MonoBehaviour
{
    [SerializeField] Graphic puntos;

    TMP_Text valor;

    void OnEnable() => Actualizar();

    void OnRectTransformDimensionsChange() => Actualizar();

    public bool PuntosVisibles { get; private set; } = true;

    // Usa la transparencia del CanvasRenderer y no Graphic.enabled: este metodo corre
    // dentro del rebuild del layout, y activar/desactivar un Graphic ahi da error.
    public void Actualizar()
    {
        if (valor == null) valor = GetComponent<TMP_Text>();
        if (puntos == null) return;
        PuntosVisibles = valor.preferredWidth <= valor.rectTransform.rect.width + 0.5f;
        puntos.canvasRenderer.SetAlpha(PuntosVisibles ? 1f : 0f);
    }
}
