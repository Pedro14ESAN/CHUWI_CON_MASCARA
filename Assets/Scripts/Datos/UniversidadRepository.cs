using System;
using System.Collections.Generic;
using UnityEngine;

// Carga el catalogo desde Resources una sola vez y lo indexa por codigo de pais
// e id de universidad, sin distinguir mayusculas. Si el JSON falta o esta mal
// formado, registra el error y queda vacio en lugar de lanzar excepciones.
public class UniversidadRepository
{
    public const string RecursoPorDefecto = "universidades";

    // Tabla explicita en lugar de string.Normalize: se comporta igual en
    // cualquier plataforma (Mono, IL2CPP) sin depender de datos Unicode.
    const string ConTilde = "áàäâãéèëêíìïîóòöôõúùüûñç";
    const string SinTilde = "aaaaaeeeeiiiiooooouuuunc";

    static UniversidadRepository porDefecto;

    readonly Dictionary<string, Pais> paises = new Dictionary<string, Pais>(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<string, Pais> paisesPorNombre = new Dictionary<string, Pais>();
    readonly Dictionary<string, Universidad> universidades = new Dictionary<string, Universidad>(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<string, Pais> paisDeUniversidad = new Dictionary<string, Pais>(StringComparer.OrdinalIgnoreCase);

    public static UniversidadRepository PorDefecto => porDefecto ??= Cargar(RecursoPorDefecto);

    public IReadOnlyCollection<Pais> Paises => paises.Values;

    public static UniversidadRepository Cargar(string recurso)
    {
        var repo = new UniversidadRepository();
        var archivo = Resources.Load<TextAsset>(recurso);
        if (archivo == null)
        {
            Debug.LogError("UniversidadRepository: no existe Resources/" + recurso + ".json");
            return repo;
        }

        CatalogoUniversidades catalogo;
        try { catalogo = JsonUtility.FromJson<CatalogoUniversidades>(archivo.text); }
        catch (ArgumentException e)
        {
            Debug.LogError("UniversidadRepository: JSON invalido en " + recurso + ": " + e.Message);
            return repo;
        }
        if (catalogo?.paises == null)
        {
            Debug.LogError("UniversidadRepository: " + recurso + " no tiene la lista 'paises'.");
            return repo;
        }

        foreach (var pais in catalogo.paises)
            repo.Agregar(pais);
        return repo;
    }

    // Publico para poder probar el repositorio con datos que no vienen del JSON.
    public void Agregar(Pais pais)
    {
        if (string.IsNullOrEmpty(pais.codigo) || !paises.TryAdd(pais.codigo, pais))
        {
            Debug.LogWarning("UniversidadRepository: pais sin codigo o duplicado: '" + pais.codigo + "'.");
            return;
        }
        var clave = Normalizar(pais.nombre);
        if (clave != null && !paisesPorNombre.TryAdd(clave, pais))
            Debug.LogWarning("UniversidadRepository: dos paises se llaman '" + pais.nombre + "'.");

        foreach (var u in pais.universidades ?? Array.Empty<Universidad>())
        {
            if (string.IsNullOrEmpty(u.id) || !universidades.TryAdd(u.id, u))
            {
                Debug.LogWarning("UniversidadRepository: universidad sin id o duplicada: '" + u.id + "'.");
                continue;
            }
            paisDeUniversidad[u.id] = pais;
        }
    }

    // Acepta el codigo ISO ("CA") o el nombre ("Canadá", "canada"). El codigo
    // tiene prioridad; el nombre ignora mayusculas, tildes y espacios extremos.
    public Pais ObtenerPais(string codigoONombre)
    {
        if (string.IsNullOrWhiteSpace(codigoONombre)) return null;
        if (paises.TryGetValue(codigoONombre.Trim(), out var porCodigo)) return porCodigo;
        return paisesPorNombre.TryGetValue(Normalizar(codigoONombre), out var porNombre) ? porNombre : null;
    }

    public Universidad ObtenerUniversidad(string id) =>
        id != null && universidades.TryGetValue(id, out var u) ? u : null;

    public Pais PaisDe(string idUniversidad) =>
        idUniversidad != null && paisDeUniversidad.TryGetValue(idUniversidad, out var p) ? p : null;

    static string Normalizar(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return null;
        var letras = texto.Trim().ToLowerInvariant().ToCharArray();
        for (int i = 0; i < letras.Length; i++)
        {
            int j = ConTilde.IndexOf(letras[i]);
            if (j >= 0) letras[i] = SinTilde[j];
        }
        return new string(letras);
    }
}
