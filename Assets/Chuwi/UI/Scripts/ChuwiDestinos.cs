using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Contenido verificado de Chuwi AR: "Chuwi AR · Contenido final del aplicativo" (Programas internacionales de ESAN, pregrado).
/// Fuentes: Programas internacionales (ue.edu.pe, 19/11/2026) y Guía de la Dirección de Relaciones Internacionales de Pregrado.
/// Solo se usa información de ese documento. Editar aquí para actualizar las fichas y la guía.
/// </summary>
public static class ChuwiDestinos
{
    public sealed class Item
    {
        public string title, detail; public bool warn;
        public Item(string t, string d = "", bool w = false) { title = t; detail = d; warn = w; }
    }
    public sealed class Section
    {
        public string name, lead, note, noteTitle = "Requisitos";
        public List<Item> items = new List<Item>();
    }
    public sealed class Info
    {
        public string code, intro, resumen, continent;
        public List<Section> sections = new List<Section>();
        public int IndexOf(string section)
        {
            for (int i = 0; i < sections.Count; i++) if (Key(sections[i].name) == Key(section)) return i;
            return 0;
        }
    }
    /// <summary>Destinos que no tienen pin en el mapa impreso (se muestran como tarjetas, sin viaje).</summary>
    public sealed class SinPin { public string country, continent; }

    public const string Contacto = "exchange@esan.edu.pe";
    public const string Telefono = "317-7200 (anexos 44748 / 44254)";
    public const string Aviso = "Convenio en proceso de renovación. Consulta su disponibilidad con la Oficina Internacional (exchange@esan.edu.pe) antes de postular.";
    public const string AvisoBecas = "Varias becas son de convocatorias pasadas (2021–2025). Confirma con la Oficina Internacional cuáles siguen vigentes antes de postular.";
    public const string ReqIntercambio =
        "Cursar el 5.º ciclo o más, mínimo 84 créditos aprobados, cursos del 1.º al 4.º ciclo sin pendientes y promedio ponderado acumulado y del último semestre de 13 o más (o tercio superior). " +
        "Convocatorias: hasta el 28 de febrero (viajas en agosto–diciembre) y hasta el 30 de octubre (viajas en marzo–julio del año siguiente).";
    public const string ReqDoble =
        "Cursar el 6.º ciclo o más, mínimo 105 créditos aprobados (Montpellier Business School exige 120), cursos del 1.º al 5.º ciclo sin pendientes y promedio de 13 o más (o tercio superior). " +
        "Postulación: fines de septiembre (clases desde el 1.er semestre) o fines de febrero (clases desde el 2.º semestre).";
    public const string ReqBecas =
        "Cumplir los requisitos del programa (Intercambio o Doble Grado) y presentar carta de motivación y carta de recomendación de un docente. " +
        "Evaluación: exámenes con Bienestar Estudiantil (30 %), méritos académicos (35 %) y entrevista final (35 %).";
    public const string ReqCortos =
        "Constancia de inglés intermedio o avanzado. Son cursos adicionales a la malla y no se convalidan. Puedes hacer hasta dos: uno como alumno y otro como egresado (hasta 2 años después de egresar).";
    public const string Fuente = "Fuente: Programas internacionales ESAN (ue.edu.pe, 19/11/2026) y Guía de Relaciones Internacionales de Pregrado. Confirma detalles en " + Contacto + ".";

    static readonly Dictionary<string, Info> data = new Dictionary<string, Info>();
    public static readonly Info Guia = BuildGuia();
    public static readonly List<SinPin> SinPines = new List<SinPin>();
    static readonly bool built = Build();

    public static Info Get(string country)
    {
        if (string.IsNullOrEmpty(country) || !built) return null;
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

    public static string Key(string s)
    {
        s = (s ?? "").ToLowerInvariant();
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

    static Item Warn(string t, string d) { return new Item(t, d + "\n" + Aviso, true); }

    // ------------------------------------------------------------------
    // Guía general (portada, tarjeta ESAN y botón "Guía")
    static Info BuildGuia()
    {
        var g = new Info { code = "ESAN", intro = "ESAN tiene más de 130 convenios internacionales en 4 continentes. Esta es la información verificada de la Oficina Internacional." };

        var p = Sec(g, "Programas", "",
            "Intercambio", "1 o 2 semestres (normalmente 4 a 5 cursos), en +130 convenios de 4 continentes. Se convalidan los cursos aprobados.",
            "Doble grado", "2 semestres (1 año), ampliable para prácticas pre-profesionales. Alemania, Francia, Colombia y EE. UU. Obtienes el bachiller ESAN y el de la universidad extranjera.",
            "Cursos cortos", "3 a 4 semanas en verano o invierno (FGV EAESP: 2 semanas), en Europa, América y Asia. Son cursos adicionales, no convalidables.",
            "Pregrado + maestría", "1 a 2 años de maestría en EE. UU. Obtienes el bachiller ESAN y la maestría de la universidad socia.");
        p.noteTitle = "ESAN en cifras";
        p.note = "+130 convenios internacionales · +11 convenios de Doble Grado · +30 convenios de programas cortos · +60 alumnos hicieron Doble Grado · +110 alumnos hicieron Intercambio · +100 alumnos y egresados en programas cortos · +950 alumnos extranjeros estudiaron en ESAN.";

        var r = Sec(g, "Requisitos", "",
            "Condición de alumno", "Intercambio: alumno regular en el periodo previo y al menos 2 años consecutivos como alumno regular. Doble Grado: alumno regular al menos 2 años consecutivos antes del inicio del programa.",
            "Créditos aprobados", "Intercambio: mínimo 84. Doble Grado: mínimo 105 (Montpellier Business School exige 120).",
            "Ciclo mínimo", "Intercambio: cursando como mínimo el 5.º ciclo. Doble Grado: como mínimo el 6.º ciclo.",
            "Cursos sin pendientes", "Intercambio: del 1.º al 4.º ciclo. Doble Grado: del 1.º al 5.º ciclo.",
            "Créditos por llevar al volver", "Intercambio: mínimo 12, en el ciclo matriculado (regla general). Doble Grado: mínimo 24.",
            "Rendimiento", "Promedio ponderado acumulado de 13 o más y del último semestre de 13 o más, o tercio superior de la especialidad (igual en ambos).",
            "Autorización interna", "Intercambio: autorización expresa del Decano de la Facultad. Doble Grado: visto bueno de Facultad/Dirección.",
            "Pasaporte", "Intercambio: vigencia mínima de 1 año. Doble Grado: vigencia mínima de 2 años.",
            "Pago a ESAN", "Derechos equivalentes a 12 créditos (no reembolsable), en ambos programas.");
        r.noteTitle = "Requisitos comunes";
        r.note =
            "• La graduación en la Universidad ESAN es obligatoria.\n" +
            "• No estar matriculado por tercera vez en ningún curso ni tener pendiente un curso reprobado dos veces.\n" +
            "• No tener sanciones disciplinarias que hayan llevado a suspensión, ni sanciones durante el último año académico.\n" +
            "• No tener boletas pendientes de pago al postular ni durante el proceso.\n" +
            "• Si tienes una beca de ESAN, avisa antes a la Oficina de Bienestar Estudiantil.\n" +
            "• Intercambio: presentar un plan preliminar de cursos a llevar en el extranjero.\n" +
            "• Con la carta de aceptación, tramitas la visa y firmas la Carta de Compromiso.\n" +
            "• Si no cumples algún requisito, puedes subir una Carta de Excepción al portal, dirigida a Mariella Olivos, Directora de Relaciones Internacionales del Pregrado.";

        var l = Sec(g, "Idiomas", "",
            "Inglés", "TOEFL 213 CBT / 550 PBT / 80 iBT, IELTS 6.0, TOEIC 750 o English Score 400. TOEIC y English Score solo valen en algunas universidades socias. En Intercambio también se acepta nivel B2 del idioma de clases. Vigencia de 1 o 2 años según el programa.",
            "Alemán", "TestDaF 5, solo si quieres llevar cursos en alemán.",
            "Francés", "DELF B1 (Doble Grado en Francia, o Intercambio si las clases son en francés).",
            "Otros países (Doble Grado)", "Nivel básico del idioma oficial del país de destino.",
            "Programas cortos", "Constancia de inglés intermedio o avanzado.");
        l.noteTitle = "Importante";
        l.note = "Los certificados de inglés deben acreditar writing, speaking y reading. No se aceptan certificados incompletos.";

        var d = Sec(g, "Documentos", "",
            "Curriculum Vitae *", "Doble Grado · Intercambio · Programa corto · Becas",
            "Propuesta de cursos internacionales *", "Doble Grado · Intercambio · Becas",
            "Copia de pasaporte vigente", "2 años en Doble Grado / 1 año en Intercambio. Todos los programas.",
            "Carta al Decano de Facultad", "Doble Grado · Intercambio · Becas",
            "Carta al Director de carrera", "Doble Grado · Intercambio · Becas",
            "Ficha de inscripción", "Formulario de pre-inscripción en UE Virtual. Todos los programas.",
            "Información académica", "Todos los programas. Se pide a Servicios Académicos por correo con el asunto \"información académica para intercambio\".",
            "Histórico de notas *", "Todos los programas. Se descarga de UE Virtual – Mi Portal.",
            "Carta de compromiso", "Firmada por el tutor, con copia del DNI del alumno y del tutor. Todos los programas.",
            "Carta de recomendación de un docente *", "Programa corto · Becas",
            "Carta de motivación *", "Programa corto · Becas",
            "Lista de materias no cursadas", "Doble Grado · Intercambio · Becas",
            "Certificado de idioma", "Todos los programas (mira la pestaña Idiomas).");
        d.lead = "Todo se sube a UE Virtual – Mi Portal, sección Internacional – Link Documentos.";
        d.noteTitle = "Ten en cuenta";
        d.note =
            "* Va en inglés, salvo que postules a un país de habla hispana. Se aceptan traducciones simples.\n" +
            "La propuesta de cursos no compromete a la facultad a convalidarlos.\n" +
            "El promedio ponderado oficial se pide a wrojas@esan.edu.pe (alumnos DPA: atencionalumnodpa@esan.edu.pe).";

        var pr = Sec(g, "Proceso", "",
            "1. Inscripción", "Llena la ficha en UE Virtual antes de la fecha límite.",
            "2. Documentos", "Reúne y sube toda la documentación.",
            "3. Confirmar", "Envía un correo a exchange@esan.edu.pe para confirmar tu inscripción.",
            "4. Evaluación", "La Oficina Internacional programa entrevistas y evaluaciones con Bienestar Estudiantil.",
            "5. Selección", "Con los resultados se elige a los participantes.");
        pr.noteTitle = "Después de ser seleccionado";
        pr.note =
            "• ESAN te nomina ante la universidad extranjera, que envía la carta de aceptación.\n" +
            "• Con esa carta tramitas la visa. Compra el pasaje después de la cita en la embajada.\n" +
            "• Llega 1 a 2 semanas antes de clases para la orientación y la inscripción de cursos.\n" +
            "• Al volver, presenta sílabos, resúmenes y el certificado oficial de notas. El Comité de Convalidación evalúa los cursos; los desaprobados no se convalidan y la decisión es inapelable.";

        var f = Sec(g, "Fechas", "",
            "Intercambio, 1.ª convocatoria", "Hasta el 28 de febrero. Estudias el segundo semestre (agosto–diciembre).",
            "Intercambio, 2.ª convocatoria", "Hasta el 30 de octubre. Estudias el primer semestre del año siguiente (marzo–julio).",
            "Doble Grado", "Fines de septiembre: clases que empiezan en el primer semestre.",
            "Doble Grado", "Fines de febrero: clases que empiezan en el segundo semestre.",
            "Programas cortos · alumnos", "Verano: julio – agosto. Invierno: diciembre – febrero.",
            "Programas cortos · egresados", "Hasta 2 años después de egresar. Verano: mayo – agosto. Invierno: diciembre – febrero.");
        f.noteTitle = "";

        var c = Sec(g, "Costos", "",
            "Intercambio y Doble Grado", "Pagas a ESAN los derechos equivalentes a 12 créditos y no pagas matrícula en la universidad extranjera.",
            "Pregrado + maestría", "Pagas a ESAN hasta el cuarto año y luego las tarifas de la universidad socia, según sus becas.",
            "Programas cortos", "Costo adicional según universidad y programa. Muchas socias incluyen alojamiento, visitas culturales y a empresas.",
            "Gastos personales", "Pasaje, visa, alojamiento, alimentación, seguro de salud y otros gastos corren por tu cuenta.",
            "Visa Schengen", "Para programas cortos en el espacio Schengen, los peruanos no necesitan visa para estancias de hasta 3 meses.");
        c.noteTitle = "";

        var b = Sec(g, "Becas", ReqBecas,
            "Heilbronn University (Alemania) · beca DAAD", "Doble Grado. €400 al mes por 11 meses + €2,000 de movilidad (aprox. €6,400). 4 vacantes. Inglés B2+. Convocatoria hasta el 28/02/2026 para la movilidad 2026-2.",
            "WU Vienna (Austria)", "Intercambio. €1,735 de pasaje + €3,600 de estancia (4 meses) = €5,335. 2 vacantes. Por reembolso.",
            "FH Dortmund (Alemania)", "Intercambio. €1,625 de pasaje + subvención de estancia (aprox. €4,670 en total). Por reembolso.",
            "FH Vorarlberg (Austria)", "Intercambio. €1,050 al mes + pasaje de aprox. €1,500.",
            "Cracow University of Economics (Polonia)", "Intercambio. €1,500 de viaje + €800 al mes.",
            "Tilburg University (Países Bajos)", "Intercambio. €1,500 de viaje + €800 al mes.",
            "University of Kansas (EE. UU.)", "Intercambio. US$ 2,000.",
            "Canadá · beca ELAP", "Intercambio. CAD 5,444. La selección la hace el Gobierno de Canadá.",
            "Alianza del Pacífico (Chile, México, Colombia)", "Intercambio. Pasaje + US$ 400–700 al mes + seguro. La selección la hace Pronabec.",
            "Universidad de Texas en Austin (EE. UU.)", "LLM (Derecho). Beca del 40 % para estudiantes ESAN.",
            "FGV EAESP (Brasil)", "Programa corto. Curso \"Sustainability Gamified\" sin costo de matrícula para alumnos ESAN.");
        b.lead = AvisoBecas;
        b.noteTitle = "Requisitos y evaluación";

        var dg = Sec(g, "Doble grado", ReqDoble);
        dg.lead = "Cada universidad socia otorga un bachiller propio. Las opciones cambian según tu carrera en ESAN.";
        dg.items.Add(new Item("Pforzheim University of Applied Sciences", "Alemania · B.Sc. in International Marketing / B.Sc. in International Business"));
        dg.items.Add(new Item("FH Dortmund", "Alemania · University of Applied Sciences and Arts Dortmund · Bachelor of Arts in International Business"));
        dg.items.Add(new Item("Heilbronn University of Applied Sciences", "Alemania · B.A. in International Business and Intercultural Studies"));
        dg.items.Add(new Item("FH Münster University of Applied Sciences", "Alemania · B.A. CALA (Carrera Alemana Latinoamericana de Administración)"));
        dg.items.Add(Warn("Montpellier Business School", "Francia · Bachelor in International Business Administration (exige 120 créditos)"));
        dg.items.Add(Warn("YSchools (Group ESC Troyes)", "Francia · Bachelor in International Network in Business Administration"));
        dg.items.Add(new Item("ESC Clermont Graduate School of Management", "Francia · Bachelor in International Management"));
        dg.items.Add(new Item("Paris School of Luxury", "Francia · Bachelor of Luxury Communication & Management"));
        dg.items.Add(Warn("Universidad del Rosario", "Colombia · Administración de Empresas / Administración de Negocios Internacionales / Administración en Logística y Producción / Marketing y Negocios Digitales"));
        dg.items.Add(new Item("Pontificia Universidad Javeriana", "Colombia · Administración de Empresas"));
        dg.items.Add(new Item("International American University (IAU)", "EE. UU. · Bachelor of Business Administration (BBA)"));

        var ix = Sec(g, "Intercambio", ReqIntercambio,
            "Alemania", "FH Dortmund, Heilbronn University, Pforzheim University",
            "Austria", "WU Vienna University of Economics and Business; FH Vorarlberg",
            "Polonia", "Cracow University of Economics",
            "Países Bajos", "Tilburg University",
            "Estados Unidos", "University of Kansas (KU)",
            "Canadá", "Universidades canadienses vía beca ELAP",
            "Chile, México, Colombia", "Universidades de la Alianza del Pacífico (beca Pronabec)");
        ix.lead = "Es una lista parcial: la lista completa de los +130 convenios está en ue.edu.pe/pregrado/internacional/programa-de-intercambio o se pide a exchange@esan.edu.pe.";

        var m = Sec(g, "Maestría", "Requisitos: TOEFL o equivalente (obligatorio); GMAT, con posible exoneración según CV y entrevista. Beneficios: programas STEM; hasta 3 años en EE. UU. con OPT.",
            "MS in Artificial Intelligence Systems Management", "Carnegie Mellon University · 1 año",
            "Master of Information Systems Management", "Carnegie Mellon University · 1.5 años",
            "MISM – Business Intelligence & Data Analytics", "Carnegie Mellon University · 1.5 años",
            "MS in Information Security Policy & Management", "Carnegie Mellon University · 2 años",
            "LLM (Master of Laws) · Derecho", "Universidad de Texas en Austin. Se viaja al inicio del último año de la carrera. Incluye preparación para el Texas Bar Exam y beca del 40 % para alumnos ESAN.");
        m.lead = "Ingeniería: estudias hasta el cuarto año en ESAN y luego haces la maestría en Carnegie Mellon University (EE. UU.). ESAN emite el bachiller y CMU el diploma de maestría.";
        m.noteTitle = "Carnegie Mellon";

        var k = Sec(g, "Cursos cortos", ReqCortos,
            "Curso 1: Sustainability Gamified", "FGV EAESP · 11–16 ene 2027, São Paulo (9:00–12:30 y 13:30–16:00). Sin costo de matrícula para alumnos ESAN. No incluye alojamiento ni comidas.",
            "Curso 2: Sustainability Expedition: Tapajós", "FGV EAESP · 18–22 ene 2027, cuenca del río Tapajós (Amazonía). BRL 9,500. Incluye vuelo São Paulo–Santarém ida y vuelta, 1 noche de hotel en Santarém, 4 noches en barco regional (en hamacas), comidas y bebidas no alcohólicas.");
        k.lead = "Cursos de 3 a 4 semanas en inglés, en universidades de Europa, América y Asia. Winter Programs 2027 – FGV EAESP (Fundação Getulio Vargas, São Paulo, Brasil): del 11 al 22 de enero de 2027, en inglés. Los cursos se pueden llevar por separado o juntos.";

        var ct = Sec(g, "Contacto", "",
            "Oficina Internacional", "Primer piso, Edificio A",
            "Correo", Contacto,
            "Teléfono", Telefono,
            "Web", "ue.edu.pe/pregrado/internacional",
            "Cartas de excepción", "Mariella Olivos, Directora de Relaciones Internacionales del Pregrado (se suben al portal).");
        ct.noteTitle = "";
        return g;
    }

    // ------------------------------------------------------------------
    // Fichas por país (solo los países que aparecen en el documento verificado)
    static bool Build()
    {
        var d = data;
        string alianza = "Pasaje + US$ 400–700 al mes + seguro. La selección la hace Pronabec.";

        var pe = new Info { code = "PE", resumen = "Punto de partida", intro = Guia.intro };
        pe.sections = Guia.sections;
        d[Key("Perú · ESAN")] = pe; d[Key("Perú")] = pe;

        var ca = new Info { code = "CA", resumen = "Intercambio · Beca ELAP", intro = "Intercambio en universidades canadienses a través de la beca ELAP del Gobierno de Canadá." };
        Sec(ca, "Intercambio", ReqIntercambio, "Universidades canadienses", "Vía beca ELAP");
        Sec(ca, "Becas", ReqBecas, "Beca ELAP (Gobierno de Canadá)", "Intercambio. CAD 5,444. La selección la hace el Gobierno de Canadá.").lead = AvisoBecas;
        d[Key("Canadá")] = ca;

        var us = new Info { code = "US", resumen = "Intercambio · Doble grado · Maestría", intro = "Intercambio en Kansas, doble grado en IAU y programas de pregrado + maestría." };
        Sec(us, "Intercambio", ReqIntercambio, "University of Kansas (KU)", "Intercambio");
        Sec(us, "Doble grado", ReqDoble, "International American University (IAU)", "Bachelor of Business Administration (BBA)");
        var usm = Sec(us, "Maestría", "Requisitos: TOEFL o equivalente (obligatorio); GMAT, con posible exoneración según CV y entrevista. Beneficios: programas STEM; hasta 3 años en EE. UU. con OPT.",
            "Carnegie Mellon University · Ingeniería", "Estudias hasta el cuarto año en ESAN y luego la maestría en CMU: MS in Artificial Intelligence Systems Management (1 año), Master of Information Systems Management (1.5 años), MISM – Business Intelligence & Data Analytics (1.5 años) o MS in Information Security Policy & Management (2 años).",
            "Universidad de Texas en Austin · Derecho", "LLM (Master of Laws). Se viaja al inicio del último año de la carrera. Incluye preparación para el Texas Bar Exam y beca del 40 % para alumnos ESAN.");
        usm.noteTitle = "Carnegie Mellon";
        Sec(us, "Becas", ReqBecas, "University of Kansas", "Intercambio. US$ 2,000.",
            "Universidad de Texas en Austin", "LLM (Derecho). Beca del 40 % para estudiantes ESAN.").lead = AvisoBecas;
        d[Key("Estados Unidos")] = us;

        var mx = new Info { code = "MX", resumen = "Intercambio · Alianza del Pacífico", intro = "Intercambio en universidades de la Alianza del Pacífico, con beca Pronabec." };
        Sec(mx, "Intercambio", ReqIntercambio, "Universidades de la Alianza del Pacífico", "Con beca Pronabec");
        Sec(mx, "Becas", ReqBecas, "Beca Alianza del Pacífico", alianza).lead = AvisoBecas;
        d[Key("México")] = mx;

        var cl = new Info { code = "CL", resumen = "Intercambio · Alianza del Pacífico", intro = "Intercambio en universidades de la Alianza del Pacífico, con beca Pronabec." };
        Sec(cl, "Intercambio", ReqIntercambio, "Universidades de la Alianza del Pacífico", "Con beca Pronabec");
        Sec(cl, "Becas", ReqBecas, "Beca Alianza del Pacífico", alianza).lead = AvisoBecas;
        d[Key("Chile")] = cl;

        var co = new Info { code = "CO", resumen = "Doble grado · Intercambio", intro = "Doble grado en el Rosario o la Javeriana e intercambio por la Alianza del Pacífico." };
        var cod = Sec(co, "Doble grado", ReqDoble);
        cod.items.Add(Warn("Universidad del Rosario", "Administración de Empresas / Administración de Negocios Internacionales / Administración en Logística y Producción / Marketing y Negocios Digitales"));
        cod.items.Add(new Item("Pontificia Universidad Javeriana", "Administración de Empresas"));
        Sec(co, "Intercambio", ReqIntercambio, "Universidades de la Alianza del Pacífico", "Con beca Pronabec");
        Sec(co, "Becas", ReqBecas, "Beca Alianza del Pacífico", alianza).lead = AvisoBecas;
        d[Key("Colombia")] = co;

        var br = new Info { code = "BR", resumen = "Cursos cortos · FGV EAESP", intro = "Winter Programs 2027 en FGV EAESP (Fundação Getulio Vargas, São Paulo): del 11 al 22 de enero de 2027, en inglés. Los cursos se pueden llevar por separado o juntos." };
        Sec(br, "Cursos cortos", ReqCortos,
            "Curso 1: Sustainability Gamified", "11–16 ene 2027, São Paulo (9:00–12:30 y 13:30–16:00). Sin costo de matrícula para alumnos ESAN. No incluye alojamiento ni comidas.",
            "Curso 2: Sustainability Expedition: Tapajós", "18–22 ene 2027, cuenca del río Tapajós (Amazonía). BRL 9,500. Incluye vuelo São Paulo–Santarém ida y vuelta, 1 noche de hotel en Santarém, 4 noches en barco regional (en hamacas), comidas y bebidas no alcohólicas.");
        d[Key("Brasil")] = br;

        // ---- Europa: no tiene pin en el mapa impreso; se muestra como tarjeta ----
        var de = new Info { code = "DE", continent = "Europa", resumen = "Doble grado · Intercambio · Becas", intro = "El país con más convenios de doble grado de ESAN." };
        var ded = Sec(de, "Doble grado", ReqDoble,
            "Pforzheim University of Applied Sciences", "B.Sc. in International Marketing / B.Sc. in International Business",
            "FH Dortmund", "University of Applied Sciences and Arts Dortmund · Bachelor of Arts in International Business",
            "Heilbronn University of Applied Sciences", "B.A. in International Business and Intercultural Studies",
            "FH Münster University of Applied Sciences", "B.A. CALA (Carrera Alemana Latinoamericana de Administración)");
        Sec(de, "Intercambio", ReqIntercambio, "FH Dortmund", "", "Heilbronn University", "", "Pforzheim University", "");
        Sec(de, "Becas", ReqBecas,
            "Heilbronn University · beca DAAD", "Doble Grado. €400 al mes por 11 meses + €2,000 de movilidad (aprox. €6,400). 4 vacantes. Inglés B2+. Convocatoria hasta el 28/02/2026 para la movilidad 2026-2.",
            "FH Dortmund", "Intercambio. €1,625 de pasaje + subvención de estancia (aprox. €4,670 en total). Por reembolso.").lead = AvisoBecas;
        d[Key("Alemania")] = de;

        var fr = new Info { code = "FR", continent = "Europa", resumen = "Doble grado", intro = "Cuatro convenios de doble grado. Piden DELF B1 de francés." };
        var frd = Sec(fr, "Doble grado", ReqDoble);
        frd.items.Add(Warn("Montpellier Business School", "Bachelor in International Business Administration (exige 120 créditos)"));
        frd.items.Add(Warn("YSchools (Group ESC Troyes)", "Bachelor in International Network in Business Administration"));
        frd.items.Add(new Item("ESC Clermont Graduate School of Management", "Bachelor in International Management"));
        frd.items.Add(new Item("Paris School of Luxury", "Bachelor of Luxury Communication & Management"));
        d[Key("Francia")] = fr;

        var at = new Info { code = "AT", continent = "Europa", resumen = "Intercambio · Becas", intro = "Intercambio en Viena y Vorarlberg, con becas publicadas." };
        Sec(at, "Intercambio", ReqIntercambio, "WU Vienna University of Economics and Business", "Viena", "FH Vorarlberg", "");
        Sec(at, "Becas", ReqBecas,
            "WU Vienna", "Intercambio. €1,735 de pasaje + €3,600 de estancia (4 meses) = €5,335. 2 vacantes. Por reembolso.",
            "FH Vorarlberg", "Intercambio. €1,050 al mes + pasaje de aprox. €1,500.").lead = AvisoBecas;
        d[Key("Austria")] = at;

        var pl = new Info { code = "PL", continent = "Europa", resumen = "Intercambio · Becas", intro = "Intercambio en Cracovia." };
        Sec(pl, "Intercambio", ReqIntercambio, "Cracow University of Economics", "");
        Sec(pl, "Becas", ReqBecas, "Cracow University of Economics", "Intercambio. €1,500 de viaje + €800 al mes.").lead = AvisoBecas;
        d[Key("Polonia")] = pl;

        var nl = new Info { code = "NL", continent = "Europa", resumen = "Intercambio · Becas", intro = "Intercambio en Tilburg." };
        Sec(nl, "Intercambio", ReqIntercambio, "Tilburg University", "");
        Sec(nl, "Becas", ReqBecas, "Tilburg University", "Intercambio. €1,500 de viaje + €800 al mes.").lead = AvisoBecas;
        d[Key("Países Bajos")] = nl;

        foreach (var n in new[] { "Alemania", "Francia", "Austria", "Polonia", "Países Bajos" })
            SinPines.Add(new SinPin { country = n, continent = "Europa" });
        return true;
    }
}
