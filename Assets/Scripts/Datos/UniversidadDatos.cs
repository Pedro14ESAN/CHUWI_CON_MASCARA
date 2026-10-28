using System;

// Estructura de Resources/universidades.json. JsonUtility exige clases
// [Serializable] con campos publicos que se llamen igual que las claves del JSON.
[Serializable]
public class CatalogoUniversidades
{
    public Pais[] paises;
}

[Serializable]
public class Pais
{
    public string codigo;
    public string nombre;
    public Universidad[] universidades;
}

[Serializable]
public class Universidad
{
    public string id;
    public string nombre;
    public string ciudad;
    public string[] modalidades;
    public DatoClave[] datosClave;
    public string[] requisitos;
}

[Serializable]
public class DatoClave
{
    public string etiqueta;
    public string valor;
}
