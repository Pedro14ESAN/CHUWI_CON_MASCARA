using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Llena PanelDetalleUniversidad: nombre, ubicacion, chips, datos clave y requisitos.
public class DetalleUniversidadView : MonoBehaviour
{
    const float AnchoMaximoEtiqueta = 360;


    [SerializeField] TMP_Text nombre;
    [SerializeField] TMP_Text ubicacion;
    [SerializeField] Transform contenedorChips;
    [SerializeField] GameObject plantillaChip;
    [SerializeField] Transform contenedorDatos;
    [SerializeField] GameObject plantillaDato;          // hijos "Etiqueta" y "Valor"
    [SerializeField] Transform contenedorRequisitos;
    [SerializeField] GameObject plantillaRequisito;
    [SerializeField] ScrollRect scroll;

    public void Mostrar(Universidad universidad, Pais pais)
    {
        nombre.text = universidad.nombre;
        ubicacion.text = pais != null ? universidad.ciudad + ", " + pais.nombre : universidad.ciudad;

        ListaDinamica.Llenar(contenedorChips, plantillaChip, universidad.modalidades,
            (go, modalidad) => go.GetComponentInChildren<TMP_Text>().text = modalidad);
        ListaDinamica.Llenar(contenedorDatos, plantillaDato, universidad.datosClave, (go, dato) =>
        {
            var etiqueta = go.transform.Find("Etiqueta").GetComponent<TMP_Text>();
            etiqueta.text = dato.etiqueta;
            // Si no cabe todo en una linea se parte el valor, no la etiqueta; salvo
            // etiquetas muy largas, que tambien pueden partirse a partir de este ancho.
            etiqueta.GetComponent<LayoutElement>().minWidth = Mathf.Min(etiqueta.preferredWidth, AnchoMaximoEtiqueta);
            go.transform.Find("Valor").GetComponent<TMP_Text>().text = dato.valor;
        });
        ListaDinamica.Llenar(contenedorRequisitos, plantillaRequisito, universidad.requisitos,
            // <indent> sangra tambien las lineas partidas, que quedan alineadas con el texto y no con la viñeta.
            (go, requisito) => go.GetComponentInChildren<TMP_Text>().text = "•<indent=1em>" + requisito + "</indent>");

        if (scroll != null) scroll.verticalNormalizedPosition = 1f;
    }
}
