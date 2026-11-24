using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>
/// Registro de alumnos y estrellas de Chuwi AR. Se guarda en el mismo dispositivo
/// (Application.persistentDataPath/chuwi_alumnos.json), sin internet.
/// Regla: cada 2 países distintos explorados = 1 estrella (máximo 5).
/// </summary>
public static class ChuwiProgreso
{
    public const int PaisesPorEstrella = 2;
    public const int MaxEstrellas = 5;
    public const int MinDestacado = 3;

    [Serializable] public class Alumno
    {
        public string codigo;
        public string nombre;
        public List<string> paises = new List<string>();
        public string ultimaVez;
        public int Estrellas() { return Mathf.Min(MaxEstrellas, paises.Count / PaisesPorEstrella); }
    }

    [Serializable] class Datos { public List<Alumno> alumnos = new List<Alumno>(); }

    static Datos datos;
    public static Alumno Actual { get; private set; }

    static string Ruta => Path.Combine(Application.persistentDataPath, "chuwi_alumnos.json");

    static void Cargar()
    {
        if (datos != null) return;
        try { datos = File.Exists(Ruta) ? JsonUtility.FromJson<Datos>(File.ReadAllText(Ruta)) : null; }
        catch (Exception e) { Debug.LogWarning("[Chuwi] No pude leer el progreso: " + e.Message); }
        if (datos == null) datos = new Datos();
        if (datos.alumnos == null) datos.alumnos = new List<Alumno>();
    }

    static void Guardar()
    {
        try { File.WriteAllText(Ruta, JsonUtility.ToJson(datos, true)); }
        catch (Exception e) { Debug.LogWarning("[Chuwi] No pude guardar el progreso: " + e.Message); }
    }

    /// <summary>Busca al alumno por código (sin importar mayúsculas) o lo crea. Actualiza el nombre.</summary>
    public static Alumno Registrar(string codigo, string nombre)
    {
        Cargar();
        codigo = (codigo ?? "").Trim(); nombre = (nombre ?? "").Trim();
        var a = datos.alumnos.FirstOrDefault(x => string.Equals(x.codigo, codigo, StringComparison.OrdinalIgnoreCase));
        if (a == null) { a = new Alumno { codigo = codigo }; datos.alumnos.Add(a); }
        if (!string.IsNullOrEmpty(nombre)) a.nombre = nombre;
        if (a.paises == null) a.paises = new List<string>();
        a.ultimaVez = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        Actual = a;
        PlayerPrefs.SetString("chuwi_ultimo_codigo", a.codigo);
        PlayerPrefs.SetString("chuwi_ultimo_nombre", a.nombre);
        Guardar();
        return a;
    }

    public static string UltimoCodigo => PlayerPrefs.GetString("chuwi_ultimo_codigo", "");
    public static string UltimoNombre => PlayerPrefs.GetString("chuwi_ultimo_nombre", "");

    public static void CerrarSesion() { Actual = null; }

    /// <summary>Marca un país como explorado. Devuelve true si con eso ganó una estrella nueva.</summary>
    public static bool Visitar(string codigoPais)
    {
        if (Actual == null || string.IsNullOrEmpty(codigoPais)) return false;
        if (Actual.paises.Contains(codigoPais)) return false;
        int antes = Actual.Estrellas();
        Actual.paises.Add(codigoPais);
        Actual.ultimaVez = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        Guardar();
        return Actual.Estrellas() > antes;
    }

    /// <summary>Alumnos con al menos MinDestacado estrellas, de más a menos estrellas y países.</summary>
    public static List<Alumno> Destacados()
    {
        Cargar();
        return datos.alumnos.Where(a => a.Estrellas() >= MinDestacado)
            .OrderByDescending(a => a.Estrellas()).ThenByDescending(a => a.paises.Count).ThenBy(a => a.nombre)
            .ToList();
    }
}
