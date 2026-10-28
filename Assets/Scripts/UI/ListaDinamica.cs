using System;
using System.Collections.Generic;
using UnityEngine;

// Rellena un contenedor clonando una plantilla, que es una hija desactivada del
// propio contenedor. Destruye las copias anteriores y conserva la plantilla.
// El contenedor debe tener SOLO la plantilla y sus copias: cualquier otro hijo
// (un titulo, un separador) se destruiria al volver a llenar.
public static class ListaDinamica
{
    public static void Llenar<T>(Transform contenedor, GameObject plantilla, IEnumerable<T> items, Action<GameObject, T> configurar)
    {
        plantilla.SetActive(false);
        for (int i = contenedor.childCount - 1; i >= 0; i--)
        {
            var hijo = contenedor.GetChild(i).gameObject;
            if (hijo == plantilla) continue;
            // Destroy espera al final del frame: se desactiva ya para que no
            // cuente en el layout mientras tanto.
            hijo.SetActive(false);
            UnityEngine.Object.Destroy(hijo);
        }
        if (items == null) return;

        int n = 0;
        foreach (var item in items)
        {
            var copia = UnityEngine.Object.Instantiate(plantilla, contenedor, false);
            copia.name = plantilla.name + "_" + n++;
            configurar(copia, item);
            copia.SetActive(true);
        }
    }
}
