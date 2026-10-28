using UnityEngine;
using UnityEngine.UI;

// Coloca a los hijos de izquierda a derecha y salta a la fila siguiente cuando
// no caben (uGUI no trae un layout con salto de linea). Cada hijo ocupa su ancho
// y alto preferidos, asi que se ajusta a su contenido sin ContentSizeFitter.
public class FlujoLayoutGroup : LayoutGroup
{
    [SerializeField] float espacioHorizontal = 16;
    [SerializeField] float espacioVertical = 16;

    public override void CalculateLayoutInputHorizontal()
    {
        base.CalculateLayoutInputHorizontal();
        float masAncho = 0, enUnaFila = 0;
        for (int i = 0; i < rectChildren.Count; i++)
        {
            float w = LayoutUtility.GetPreferredWidth(rectChildren[i]);
            masAncho = Mathf.Max(masAncho, w);
            enUnaFila += w + (i > 0 ? espacioHorizontal : 0);
        }
        SetLayoutInputForAxis(masAncho + padding.horizontal, LayoutUtility.DefaultMaxSize, enUnaFila + padding.horizontal, -1, 0);
    }

    public override void CalculateLayoutInputVertical()
    {
        float alto = Colocar(false);
        SetLayoutInputForAxis(alto, LayoutUtility.DefaultMaxSize, alto, -1, 1);
    }

    public override void SetLayoutHorizontal() => Colocar(true);

    public override void SetLayoutVertical() => Colocar(true);

    // Recorre los hijos en filas; si aplicar es true los posiciona. Devuelve el alto total.
    float Colocar(bool aplicar)
    {
        float ancho = rectTransform.rect.width - padding.horizontal;
        float x = 0, y = padding.top, altoFila = 0;
        foreach (var hijo in rectChildren)
        {
            float w = Mathf.Min(LayoutUtility.GetPreferredWidth(hijo), ancho);
            float h = LayoutUtility.GetPreferredHeight(hijo);
            if (x > 0 && x + w > ancho)
            {
                x = 0;
                y += altoFila + espacioVertical;
                altoFila = 0;
            }
            if (aplicar)
            {
                SetChildAlongAxis(hijo, 0, padding.left + x, w);
                SetChildAlongAxis(hijo, 1, y, h);
            }
            x += w + espacioHorizontal;
            altoFila = Mathf.Max(altoFila, h);
        }
        return y + altoFila + padding.bottom;
    }
}
