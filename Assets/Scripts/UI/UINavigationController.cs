using UnityEngine;
using UnityEngine.UI;

// Controla la navegacion entre los paneles de UI_Navegacion:
// Bienvenida -> Exploracion -> Lista de universidades -> Detalle.
// Va en la raiz del prefab (el Canvas). Todas las referencias apuntan a hijos
// del propio prefab; si alguna queda vacia se busca por nombre en Awake, asi el
// prefab funciona en cualquier escena. MostrarLista y MostrarDetalle son
// publicos para que el script de AR los llame al tocar un pin del mapa.
// El Titulo de PanelBienvenida usa overflow Truncate provisional; ver el
// comentario en TarjetaUniversidadView.
public class UINavigationController : MonoBehaviour
{
    [SerializeField] GameObject panelBienvenida;
    [SerializeField] GameObject panelExploracion;
    [SerializeField] GameObject panelLista;
    [SerializeField] GameObject panelDetalle;
    [SerializeField] Button botonIniciar;
    [SerializeField] Button botonVolver;
    [SerializeField] Button botonVolverLista;
    [SerializeField] Button botonVolverDetalle;
    [SerializeField] ListaUniversidadesView vistaLista;
    [SerializeField] DetalleUniversidadView vistaDetalle;

    UniversidadRepository repositorio;
    string paisActual;      // pais del ultimo detalle o lista abiertos
    string paisEnLista;     // pais cuyas tarjetas estan cargadas ahora

    void Awake()
    {
        if (panelBienvenida == null) panelBienvenida = BuscarHijo("PanelBienvenida")?.gameObject;
        if (panelExploracion == null) panelExploracion = BuscarHijo("PanelExploracion")?.gameObject;
        if (panelLista == null) panelLista = BuscarHijo("PanelListaUniversidades")?.gameObject;
        if (panelDetalle == null) panelDetalle = BuscarHijo("PanelDetalleUniversidad")?.gameObject;
        if (botonIniciar == null) botonIniciar = BuscarHijo("BotonIniciar")?.GetComponent<Button>();
        if (botonVolver == null) botonVolver = BuscarHijo("BotonVolver")?.GetComponent<Button>();
        if (botonVolverLista == null) botonVolverLista = BuscarHijo("BotonVolverLista")?.GetComponent<Button>();
        if (botonVolverDetalle == null) botonVolverDetalle = BuscarHijo("BotonVolverDetalle")?.GetComponent<Button>();
        if (vistaLista == null) vistaLista = GetComponentInChildren<ListaUniversidadesView>(true);
        if (vistaDetalle == null) vistaDetalle = GetComponentInChildren<DetalleUniversidadView>(true);

        if (panelBienvenida == null || panelExploracion == null)
        {
            Debug.LogError("UINavigationController: faltan PanelBienvenida o PanelExploracion dentro de " + name, this);
            enabled = false;
            return;
        }

        Conectar(botonIniciar, MostrarExploracion, "BotonIniciar");
        Conectar(botonVolver, MostrarBienvenida, "BotonVolver");
        Conectar(botonVolverLista, MostrarExploracion, "BotonVolverLista");
        Conectar(botonVolverDetalle, VolverDesdeDetalle, "BotonVolverDetalle");

        repositorio = UniversidadRepository.PorDefecto;
        MostrarBienvenida();
    }

    void OnDestroy()
    {
        if (botonIniciar != null) botonIniciar.onClick.RemoveListener(MostrarExploracion);
        if (botonVolver != null) botonVolver.onClick.RemoveListener(MostrarBienvenida);
        if (botonVolverLista != null) botonVolverLista.onClick.RemoveListener(MostrarExploracion);
        if (botonVolverDetalle != null) botonVolverDetalle.onClick.RemoveListener(VolverDesdeDetalle);
    }

    public void MostrarBienvenida() => Activar(panelBienvenida);

    public void MostrarExploracion() => Activar(panelExploracion);

    // Abre la lista de universidades de un pais. Acepta el codigo del JSON
    // ("CA") o el nombre ("Canadá", "canada").
    public void MostrarLista(string codigoPais)
    {
        var pais = repositorio?.ObtenerPais(codigoPais);
        if (pais == null || panelLista == null || vistaLista == null)
        {
            Debug.LogWarning("UINavigationController: no hay datos para el pais '" + codigoPais + "'.", this);
            return;
        }
        paisActual = paisEnLista = pais.codigo;
        vistaLista.Mostrar(pais, MostrarDetalle);
        Activar(panelLista);
    }

    // Abre el detalle; funciona tambien sin pasar por la lista (p. ej. desde AR).
    public void MostrarDetalle(string idUniversidad)
    {
        var universidad = repositorio?.ObtenerUniversidad(idUniversidad);
        if (universidad == null || panelDetalle == null || vistaDetalle == null)
        {
            Debug.LogWarning("UINavigationController: no existe la universidad '" + idUniversidad + "'.", this);
            return;
        }
        var pais = repositorio.PaisDe(idUniversidad);
        paisActual = pais?.codigo;
        vistaDetalle.Mostrar(universidad, pais);
        Activar(panelDetalle);
    }

    // VOLVER respeta la jerarquia: detalle -> lista del mismo pais -> exploracion.
    // Si la lista ya tiene cargado ese pais, se reactiva tal cual (conserva el scroll).
    void VolverDesdeDetalle()
    {
        if (paisActual == null) MostrarExploracion();
        else if (paisActual == paisEnLista) Activar(panelLista);
        else MostrarLista(paisActual);
    }

    void Activar(GameObject panel)
    {
        foreach (var p in new[] { panelBienvenida, panelExploracion, panelLista, panelDetalle })
            if (p != null) p.SetActive(p == panel);
    }

    void Conectar(Button boton, UnityEngine.Events.UnityAction accion, string nombre)
    {
        if (boton != null) boton.onClick.AddListener(accion);
        else Debug.LogWarning("UINavigationController: no se encontro " + nombre + ".", this);
    }

    // Busca en toda la jerarquia del prefab, incluidos hijos desactivados.
    Transform BuscarHijo(string nombre)
    {
        foreach (var t in GetComponentsInChildren<Transform>(true))
            if (t.name == nombre) return t;
        return null;
    }
}
