using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Tarjeta de la lista: nombre, ciudad y chips de modalidad. ContenedorImagen
// queda reservado (vacio) hasta que haya fotos de las universidades.
public class TarjetaUniversidadView : MonoBehaviour
{
    // nombre y ciudad usan overflow Truncate en el prefab. PROVISIONAL: LiberationSans
    // SDF no garantiza el glifo "…" y TMP cambia Ellipsis a Truncate por su cuenta.
    // Arreglo real: agregar "…" al atlas cuando tengamos la fuente definitiva de ESAN.
    [SerializeField] TMP_Text nombre;
    [SerializeField] TMP_Text ciudad;
    [SerializeField] Transform contenedorChips;
    [SerializeField] GameObject plantillaChip;
    [SerializeField] RectTransform contenedorImagen;
    [SerializeField] Button boton;

    public RectTransform ContenedorImagen => contenedorImagen;

    public void Configurar(Universidad universidad, Action<string> alTocar)
    {
        nombre.text = universidad.nombre;
        ciudad.text = universidad.ciudad;
        ListaDinamica.Llenar(contenedorChips, plantillaChip, universidad.modalidades,
            (chip, modalidad) => chip.GetComponentInChildren<TMP_Text>().text = modalidad);

        string id = universidad.id;
        boton.onClick.RemoveAllListeners();
        boton.onClick.AddListener(() => alTocar?.Invoke(id));
    }
}
