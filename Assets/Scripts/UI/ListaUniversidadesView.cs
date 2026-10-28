using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Llena PanelListaUniversidades: cabecera con el pais y una tarjeta por universidad.
public class ListaUniversidadesView : MonoBehaviour
{
    // Overflow Truncate provisional; ver el comentario en TarjetaUniversidadView.
    [SerializeField] TMP_Text tituloPais;
    [SerializeField] Transform contenedorTarjetas;
    [SerializeField] TarjetaUniversidadView plantillaTarjeta;
    [SerializeField] ScrollRect scroll;

    public void Mostrar(Pais pais, Action<string> alElegir)
    {
        tituloPais.text = pais.nombre;
        ListaDinamica.Llenar(contenedorTarjetas, plantillaTarjeta.gameObject, pais.universidades,
            (go, u) => go.GetComponent<TarjetaUniversidadView>().Configurar(u, alElegir));
        if (scroll != null) scroll.verticalNormalizedPosition = 1f;
    }
}
