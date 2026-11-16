using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Datos de oportunidades de ESAN pregrado por pais (Oficina Internacional, consultado oct. 2026).
/// Editar aqui para actualizar las fichas de la interfaz.
/// </summary>
public static class ChuwiDestinos
{
    public sealed class Item
    {
        public string title, detail;
        public Item(string t, string d = "") { title = t; detail = d; }
    }
    public sealed class Section
    {
        public string name, note;
        public List<Item> items = new List<Item>();
    }
    public sealed class Info
    {
        public string code, intro;
        public List<Section> sections = new List<Section>();
        public int UniversityCount
        {
            get { int n = 0; foreach (var s in sections) if (s.name != "Becas") n += s.items.Count; return n; }
        }
    }

    public const string Contacto = "exchange@esan.edu.pe";
    public const string Telefono = "317-7200 anexos 44254 / 44748";
    public const string ReqIntercambio =
        "5.º ciclo o más, 84 créditos aprobados y promedio de 13 o tercio superior. " +
        "Convocatorias: hasta el 28 de febrero (viajas en el 2.º semestre) y hasta el 30 de octubre (viajas en el 1.º semestre).";
    public const string ReqIdioma = "Al no ser hispanohablante, piden TOEFL iBT 80 o IELTS 6.0.";
    public const string ReqDoble = "6.º ciclo y entre 105 y 120 créditos aprobados.";
    public const string Fuente = "Fuente: Oficina Internacional ESAN (oct. 2026). Confirma detalles en " + Contacto + ".";

    static readonly Dictionary<string, Info> data = Build();

    public static Info Get(string country)
    {
        if (string.IsNullOrEmpty(country)) return null;
        Info i; return data.TryGetValue(Key(country), out i) ? i : null;
    }

    public static string Code(string country)
    {
        var i = Get(country);
        if (i != null) return i.code;
        return country.Length >= 2 ? country.Substring(0, 2).ToUpper() : country.ToUpper();
    }

    public static Color ContinentColor(string continent)
    {
        string k = Key(continent);
        if (k.Contains("norte")) return new Color32(0x3C, 0xB0, 0x43, 0xFF);
        if (k.Contains("sur")) return new Color32(0xD0, 0x7A, 0x2A, 0xFF);
        if (k.Contains("asia")) return new Color32(0xE3, 0xB1, 0x00, 0xFF);
        if (k.Contains("europa")) return new Color32(0xC8, 0x10, 0x2E, 0xFF);
        return new Color32(0x1B, 0x2A, 0x4A, 0xFF);
    }

    static string Key(string s)
    {
        s = s.ToLowerInvariant();
        s = s.Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ú", "u").Replace("ñ", "n");
        return s.Trim();
    }

    static Section Sec(Info info, string name, string note, params string[] titleDetail)
    {
        var s = new Section { name = name, note = note };
        for (int i = 0; i < titleDetail.Length; i += 2)
            s.items.Add(new Item(titleDetail[i], i + 1 < titleDetail.Length ? titleDetail[i + 1] : ""));
        info.sections.Add(s);
        return s;
    }

    static Dictionary<string, Info> Build()
    {
        var d = new Dictionary<string, Info>();
        string ri = ReqIntercambio, rl = ReqIntercambio + " " + ReqIdioma;
        string becaProx = "Próxima convocatoria por anunciar";

        var pe = new Info { code = "PE", intro = "Aquí empieza el viaje. La Oficina Internacional de ESAN te ayuda a postular a intercambio, doble grado, becas y programas cortos." };
        Sec(pe, "Contacto", Fuente, "Oficina Internacional ESAN", Contacto, "Teléfono", Telefono);
        Sec(pe, "Programas cortos", "Cursos de 3 a 4 semanas en inglés, en verano o invierno, en Europa, América y Asia.",
            "Universidad del Rosario", "Colombia", "FH Dortmund", "Alemania · verano");
        d[Key("Perú · ESAN")] = pe; d[Key("Perú")] = pe;

        var ca = new Info { code = "CA", intro = "Cuatro universidades socias en Toronto, Ottawa, Montreal y Quebec." };
        Sec(ca, "Intercambio", rl, "York University", "Toronto", "Carleton University", "Ottawa",
            "Université du Québec à Montréal (UQAM)", "Montreal · clases en francés", "Université Laval", "Quebec · clases en francés");
        Sec(ca, "Becas", "Confirma requisitos con la Oficina Internacional.", "Beca ELAP (Gobierno de Canadá)", becaProx);
        d[Key("Canadá")] = ca;

        var us = new Info { code = "US", intro = "Intercambio en Kansas y convenios de maestría para después de la carrera." };
        Sec(us, "Intercambio", rl + " El socio de Estados Unidos se confirma con la Oficina Internacional.", "University of Kansas", "Lawrence, Kansas");
        Sec(us, "Posgrado", "Convenios de maestría para egresados de ESAN.",
            "Carnegie Mellon University", "Heinz College · Ingeniería", "SMU Dedman School of Law", "Derecho");
        d[Key("Estados Unidos")] = us;

        var mx = new Info { code = "MX", intro = "Cinco universidades socias, sin examen de idioma." };
        Sec(mx, "Intercambio", ri, "Tecnológico de Monterrey", "", "ITAM", "Ciudad de México", "UDLAP", "Puebla",
            "Universidad de Monterrey", "", "UNLA", "Morelia");
        Sec(mx, "Becas", "Confirma requisitos con la Oficina Internacional.", "Beca Alianza del Pacífico", becaProx);
        d[Key("México")] = mx;

        var co = new Info { code = "CO", intro = "El país con más opciones: intercambio, doble grado y programas cortos." };
        Sec(co, "Intercambio", ri, "Universidad de La Sabana", "", "Universidad del Rosario", "", "Pontificia Universidad Javeriana", "",
            "Universidad EAFIT", "Medellín", "Universidad Santo Tomás", "", "Universidad Sergio Arboleda", "",
            "CEIPA Business School", "", "Universidad Externado de Colombia", "");
        Sec(co, "Doble grado", ReqDoble, "Pontificia Universidad Javeriana", "Administración de Empresas",
            "Universidad del Rosario", "Administración, Negocios Internacionales, Logística, Marketing · en renovación");
        Sec(co, "Becas", "Confirma requisitos con la Oficina Internacional.", "Beca Alianza del Pacífico", becaProx);
        Sec(co, "Programas cortos", "3 a 4 semanas en inglés.", "Universidad del Rosario", "Verano o invierno");
        d[Key("Colombia")] = co;

        var br = new Info { code = "BR", intro = "Tres de las mejores escuelas de negocios de Brasil." };
        Sec(br, "Intercambio", rl, "Universidade de São Paulo", "São Paulo", "Insper", "São Paulo", "FGV", "Fundação Getulio Vargas");
        d[Key("Brasil")] = br;

        var cl = new Info { code = "CL", intro = "Cuatro universidades socias, sin examen de idioma." };
        Sec(cl, "Intercambio", ri, "Universidad del Desarrollo", "", "Universidad Mayor", "", "Universidad de Los Lagos", "", "Universidad Central de Chile", "");
        Sec(cl, "Becas", "Confirma requisitos con la Oficina Internacional.", "Beca Alianza del Pacífico", becaProx);
        d[Key("Chile")] = cl;

        var ar = new Info { code = "AR", intro = "Seis universidades socias entre Buenos Aires, Córdoba y Mendoza." };
        Sec(ar, "Intercambio", ri, "Universidad Siglo 21", "Córdoba", "Universidad Torcuato Di Tella", "Buenos Aires",
            "Pontificia Universidad Católica Argentina", "Buenos Aires", "Universidad Austral", "Buenos Aires",
            "Universidad de Mendoza", "Mendoza", "UCEMA", "Buenos Aires");
        d[Key("Argentina")] = ar;

        var cn = new Info { code = "CN", intro = "Estudia en Pekín o Shanghái." };
        Sec(cn, "Intercambio", rl, "Peking University", "Pekín", "ESSCA Shanghai Campus", "Shanghái");
        d[Key("China")] = cn;

        var kr = new Info { code = "KR", intro = "Dos universidades socias en Corea del Sur." };
        Sec(kr, "Intercambio", rl, "SEOULTECH", "Seúl", "Sun Moon University", "Asan · factsheet disponible pronto");
        d[Key("Corea del Sur")] = kr;

        var jp = new Info { code = "JP", intro = "Waseda en Tokio y NUCB en Nagoya." };
        Sec(jp, "Intercambio", rl, "Waseda University", "Tokio", "Nagoya University of Commerce and Business", "Nagoya · NUCB");
        d[Key("Japón")] = jp;

        var tw = new Info { code = "TW", intro = "Una universidad socia en Taipéi." };
        Sec(tw, "Intercambio", rl, "National Chengchi University", "Taipéi");
        d[Key("Taiwán")] = tw;

        var th = new Info { code = "TH", intro = "Escuela de negocios en Bangkok." };
        Sec(th, "Intercambio", rl, "Thammasat Business School", "Bangkok");
        d[Key("Tailandia")] = th;

        return d;
    }
}
