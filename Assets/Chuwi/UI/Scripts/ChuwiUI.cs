using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

/// <summary>
/// Interfaz movil de Chuwi AR. Construye todo el Canvas al iniciar (no hay que armar nada a mano).
/// Pantallas: bienvenida, escaner / marcador perdido, mapa (chips + tarjetas), viajando y ficha del pais.
/// Recursos en Assets/Chuwi/UI/Resources/ChuwiUI.
/// </summary>
[DisallowMultipleComponent]
public sealed class ChuwiUI : MonoBehaviour
{
    public ChuwiMapTravel travel;
    [Tooltip("Mostrar la pantalla de bienvenida al abrir la app.")]
    public bool showWelcome = true;

    // ---------- Paleta (estilo del Figma) ----------
    static readonly Color Red = Hex("C8102E"), RedDark = Hex("9E0C24"), Navy = Hex("1B2A4A"), Orange = Hex("F28C28"),
        Bg = Hex("F5F6F8"), White = Color.white, Muted = Hex("6B7690"), Line = Hex("E3E6EC"), Cream = Hex("FFF4E8");

    enum State { Welcome, Map, Travel, Info }
    State state = State.Welcome;

    Font fRegular, fSemi, fBold, fBlack;
    Sprite sRound, sOutline, sCircle, sShadow, sCorner, sGradient, sGlow, sChuwiHola, sChuwiSenala;
    Sprite iBack, iDance, iPin, iMail, iCamera, iPlane, iClose, iStar;

    Canvas canvas; CanvasScaler scaler; RectTransform safe;
    CanvasGroup welcome, scanner, hud, bubble, sheet;
    RectTransform welcomeChuwi, welcomeText, welcomeArt, sheetRect, scanFrame, bubbleRect;
    Text scanTitle, scanSub, bubbleText; Image scanPill;
    RectTransform mapPanel, travelPanel, infoPanel;
    RectTransform chipRow, cardRow, tabRow, infoContent; ScrollRect infoScroll, cardScroll;
    Text travelTitle; RectTransform travelBar; Text infoTitle, infoContinent; string infoIntroText = ""; Image infoBadge; Text infoBadgeText;
    Image infoContinentChip; Button danceButton;
    readonly List<(Button b, Image img, Text txt, string key)> chips = new List<(Button, Image, Text, string)>();
    readonly List<(Button b, Image img, Text txt, int idx)> tabs = new List<(Button, Image, Text, int)>();
    string continentFilter = "";
    int infoIndex = -1, infoTab = 0;
    bool seenMarker, landscape, layoutDirty = true;
    Vector2 lastScreen; Rect lastSafe;
    float bubbleTimer;

    // ======================================================================
    void Awake()
    {
        if (travel == null) travel = FindFirstObjectByType<ChuwiMapTravel>(FindObjectsInactive.Include);
        LoadResources();
        EnsureEventSystem();
        Build();
        if (travel != null)
        {
            travel.showDebugMenu = false;
            travel.Departed += OnDeparted;
            travel.Arrived += OnArrived;
        }
        state = showWelcome ? State.Welcome : State.Map;
        var es = EventSystem.current != null ? EventSystem.current : FindFirstObjectByType<EventSystem>();
        Log("Awake ok. travel=" + (travel != null) + " eventSystem=" + (es != null ? es.name + " modulo=" + (es.GetComponent<BaseInputModule>() != null ? es.GetComponent<BaseInputModule>().GetType().Name : "ninguno") : "NINGUNO")
            + " pantalla=" + Screen.width + "x" + Screen.height);
        welcome.alpha = state == State.Welcome ? 1 : 0;
        welcome.blocksRaycasts = state == State.Welcome;
        ShowPanel(State.Map, true);
        SetBubble("¿A dónde viajamos?");
    }

    void OnDestroy()
    {
        if (travel != null) { travel.Departed -= OnDeparted; travel.Arrived -= OnArrived; }
    }

    public static void Log(string msg)
    {
#if UNITY_EDITOR
        try { System.IO.File.AppendAllText(System.IO.Path.Combine(Application.dataPath, "../Logs/chuwi_ui.log"), DateTime.Now.ToString("HH:mm:ss.fff ") + msg + "\n"); } catch { }
#endif
    }

    bool updateErrorLogged;
    Image peek;
    RectTransform startButton;

    void Update()
    {
        try { Tick(); }
        catch (Exception e) { if (!updateErrorLogged) { updateErrorLogged = true; Log("UPDATE ERROR " + e); Debug.LogException(e); } }
    }

    void Tick()
    {
        WelcomeFallback();
        if (lastScreen.x != Screen.width || lastScreen.y != Screen.height || lastSafe != Screen.safeArea || layoutDirty)
            ApplyLayout();

        bool visible = travel != null && travel.IsVisible;
        if (visible) seenMarker = true;
        bool inApp = state != State.Welcome;

        Fade(scanner, inApp && !visible, 6);
        Fade(hud, inApp && visible, 6);
        Fade(sheet, inApp && visible, 6);
        Fade(bubble, inApp && visible && bubbleTimer > 0, 5);
        if (bubbleTimer > 0 && state != State.Travel) bubbleTimer -= Time.deltaTime;

        // Escaner / marcador perdido
        if (seenMarker)
        {
            scanTitle.text = "¡Perdimos el marcador!";
            scanSub.text = "Vuelve a apuntar la cámara al mapa de Chuwi.";
            scanPill.color = Red;
        }
        float pulse = 1 + Mathf.Sin(Time.time * 3f) * .03f;
        scanFrame.localScale = new Vector3(pulse, pulse, 1);

        // Bienvenida: Chuwi flota
        if (welcomeChuwi != null)
            welcomeChuwi.anchoredPosition = new Vector2(0, Mathf.Sin(Time.time * 2f) * 14f);

        if (state == State.Travel && travel != null)
        {
            float p = travel.TravelProgress;
            travelBar.anchorMax = new Vector2(Mathf.Lerp(.04f, 1, p), 1);
        }
        if (danceButton != null) danceButton.interactable = travel != null && !travel.IsMoving;
    }

    // Si el EventSystem no entrega el clic, detectamos el toque sobre el boton de inicio directamente.
    void WelcomeFallback()
    {
        if (state != State.Welcome || startButton == null) return;
        Vector2 p; bool pressed = false;
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Touchscreen.current != null && UnityEngine.InputSystem.Touchscreen.current.primaryTouch.press.wasReleasedThisFrame)
        { pressed = true; p = UnityEngine.InputSystem.Touchscreen.current.primaryTouch.position.ReadValue(); }
        else if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasReleasedThisFrame)
        { pressed = true; p = UnityEngine.InputSystem.Mouse.current.position.ReadValue(); }
        else p = Vector2.zero;
#else
        pressed = Input.GetMouseButtonUp(0); p = Input.mousePosition;
#endif
        if (!pressed) return;
        bool inside = RectTransformUtility.RectangleContainsScreenPoint(startButton, p, null);
        Log("toque en bienvenida " + p + " dentro del boton=" + inside + " eventSystem=" + (EventSystem.current != null ? EventSystem.current.name : "NINGUNO"));
        if (inside) StartApp();
    }

    // ======================================================================
    // Eventos del viaje
    void OnDeparted(int index)
    {
        try { Depart(index); } catch (Exception e) { Log("DEPART ERROR " + e); Debug.LogException(e); }
    }

    void Depart(int index)
    {
        if (index < 0 || index >= travel.destinations.Length) return;
        string c = travel.destinations[index].country;
        travelTitle.text = index == 0 ? "Volviendo a ESAN" : "Viajando a " + c;
        state = State.Travel; ShowPanel(State.Travel, false);
        SetBubble(index == 0 ? "¡Volvemos a casa!" : "¡Vamos a " + c + "!", 999);
    }

    void OnArrived(int index)
    {
        try { Arrive(index); } catch (Exception e) { Log("ARRIVE ERROR " + e); Debug.LogException(e); }
    }

    void Arrive(int index)
    {
        if (index <= 0)
        {
            state = State.Map; ShowPanel(State.Map, false);
            SetBubble("¡De vuelta en ESAN! ¿A dónde vamos ahora?");
            return;
        }
        SetBubble("¡Llegamos a " + travel.destinations[index].country + "!");
        OpenInfo(index);
    }

    void StartApp()
    {
        if (state != State.Welcome) return;
        Log("StartApp");
        state = State.Map;
        StartCoroutine(FadeOut(welcome, .35f));
        ShowPanel(State.Map, true);
        SetBubble("¡Hola! ¿A dónde viajamos?");
    }

    void SelectCountry(int index)
    {
        if (travel == null) return;
        if (index == travel.Selected && !travel.IsMoving && index > 0) { OpenInfo(index); return; }
        travel.SelectDestination(index);
        if (!travel.IsMoving) ShowToastBubble("Enfoca el marcador para viajar");
    }

    void OpenInfo(int index)
    {
        infoIndex = index; infoTab = 0;
        var dest = travel.destinations[index];
        var info = ChuwiDestinos.Get(dest.country);
        infoTitle.text = dest.country;
        infoContinent.text = dest.continent.ToUpperInvariant();
        infoContinentChip.color = ChuwiDestinos.ContinentColor(dest.continent);
        infoBadge.color = ChuwiDestinos.ContinentColor(dest.continent);
        infoBadgeText.text = ChuwiDestinos.Code(dest.country);
        infoIntroText = info != null ? info.intro : "Información pendiente de la Oficina Internacional.";
        BuildTabs(info);
        FillInfo(info);
        state = State.Info; ShowPanel(State.Info, false);
    }

    void BackToMap()
    {
        state = State.Map; ShowPanel(State.Map, false);
        SetBubble("¿A dónde viajamos ahora?");
    }

    void Dance()
    {
        if (travel == null) return;
        travel.Dance();
        SetBubble("¡A bailar!");
    }

    // ======================================================================
    // Construccion
    void LoadResources()
    {
        fRegular = LoadFont("Poppins-Regular"); fSemi = LoadFont("Poppins-SemiBold");
        fBold = LoadFont("Poppins-Bold"); fBlack = LoadFont("Poppins-ExtraBold");
        sRound = LoadSprite("ui_round", 44); sOutline = LoadSprite("ui_round_outline", 44); sCircle = LoadSprite("ui_circle", 0);
        sShadow = LoadSprite("ui_shadow", 76); sCorner = LoadSprite("ui_corner", 0); sGradient = LoadSprite("ui_gradient", 0);
        sGlow = LoadSprite("ui_glow", 0); sChuwiHola = LoadSprite("chuwi_hola", 0); sChuwiSenala = LoadSprite("chuwi_senala", 0);
        iBack = LoadSprite("icon_back", 0); iDance = LoadSprite("icon_dance", 0); iPin = LoadSprite("icon_pin", 0);
        iMail = LoadSprite("icon_mail", 0); iCamera = LoadSprite("icon_camera", 0); iPlane = LoadSprite("icon_plane", 0);
        iClose = LoadSprite("icon_close", 0); iStar = LoadSprite("icon_star", 0);
    }

    static Font LoadFont(string n)
    {
        var f = Resources.Load<Font>("ChuwiUI/" + n);
        return f != null ? f : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    static Sprite LoadSprite(string n, int border)
    {
        var s = Resources.Load<Sprite>("ChuwiUI/" + n);
        if (s != null) return s;
        var t = Resources.Load<Texture2D>("ChuwiUI/" + n);
        if (t == null) return null;
        return Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect,
            new Vector4(border, border, border, border));
    }

    void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        var go = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        go.AddComponent<InputSystemUIInputModule>();
#else
        go.AddComponent<StandaloneInputModule>();
#endif
    }

    void Build()
    {
        var root = new GameObject("ChuwiCanvas", typeof(RectTransform));
        root.transform.SetParent(transform, false);
        canvas = root.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 50;
        scaler = root.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        root.AddComponent<GraphicRaycaster>();
        safe = Rect("Safe", root.transform); Stretch(safe);

        BuildScanner();
        BuildHud();
        BuildBubble();
        BuildSheet();
        BuildWelcome(root.transform); // encima de todo (fuera del safe area para cubrir la pantalla)
    }

    // ---------------- Bienvenida ----------------
    void BuildWelcome(Transform parent)
    {
        var r = Rect("Bienvenida", parent); Stretch(r);
        welcome = r.gameObject.AddComponent<CanvasGroup>();
        var bg = Img(r, sGradient, White); Stretch(bg.rectTransform); bg.raycastTarget = true;
        // decoraciones
        Deco(r, new Vector2(.95f, .92f), 520, Red, .9f);
        Deco(r, new Vector2(.02f, .55f), 300, Orange, .85f);
        Deco(r, new Vector2(.9f, .25f), 160, Hex("2E4372"), 1);

        welcomeArt = Rect("Arte", r);
        var glow = Img(welcomeArt, sGlow, new Color(1, .8f, .55f, .55f)); Center(glow.rectTransform, new Vector2(0, 0), new Vector2(900, 900));
        welcomeChuwi = Rect("ChuwiFlota", welcomeArt); Center(welcomeChuwi, Vector2.zero, new Vector2(10, 10));
        var chuwi = Img(welcomeChuwi, sChuwiHola, White); chuwi.preserveAspect = true;
        Center(chuwi.rectTransform, Vector2.zero, new Vector2(620, 900));

        welcomeText = Rect("Textos", r);
        var tag = Pill(welcomeText, "UNIVERSIDAD ESAN · OFICINA INTERNACIONAL", fSemi, 24, new Color(1, 1, 1, .14f), White, 60);
        Anchor(tag, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, 0), new Vector2(640, 60));
        var title = Txt(welcomeText, "Chuwi AR", fBlack, 120, White, TextAnchor.MiddleCenter);
        Anchor(title.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -140), new Vector2(0, 150));
        var sub = Txt(welcomeText, "¡Hola! Soy Chuwi, tu guía para estudiar en el extranjero.", fRegular, 40, new Color(1, 1, 1, .88f), TextAnchor.UpperCenter);
        Anchor(sub.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -280), new Vector2(-80, 120));

        var opp = Rect("Oportunidades", welcomeText);
        Anchor(opp, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -430), new Vector2(0, 64));
        var h = opp.gameObject.AddComponent<HorizontalLayoutGroup>(); h.spacing = 14; h.childAlignment = TextAnchor.MiddleCenter;
        h.childControlWidth = true; h.childControlHeight = true; h.childForceExpandWidth = false;
        foreach (var s in new[] { "Intercambio", "Doble grado", "Becas", "Cursos cortos" })
        {
            var p = Pill(opp, s, fSemi, 26, new Color(1, 1, 1, .12f), White, 64);
            var le = p.gameObject.AddComponent<LayoutElement>(); le.preferredHeight = 64; le.preferredWidth = 42 + s.Length * 16;
        }

        var go = BigButton(welcomeText, "Explorar con Chuwi", Red, White, iPlane, StartApp);
        startButton = go;
        Anchor(go, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -580), new Vector2(720, 140));
        var hint = Txt(welcomeText, "Necesitarás el marcador de Chuwi o el mapa impreso.", fRegular, 28, new Color(1, 1, 1, .6f), TextAnchor.UpperCenter);
        Anchor(hint.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -740), new Vector2(-80, 60));
    }

    void Deco(RectTransform parent, Vector2 anchor, float size, Color c, float a)
    {
        var d = Img(parent, sCircle, new Color(c.r, c.g, c.b, a * .35f));
        Anchor(d.rectTransform, anchor, anchor, Vector2.zero, new Vector2(size, size));
    }

    // ---------------- Escaner ----------------
    void BuildScanner()
    {
        var r = Rect("Escaner", safe); Stretch(r);
        scanner = r.gameObject.AddComponent<CanvasGroup>(); scanner.alpha = 0; scanner.blocksRaycasts = false;
        var dim = Img(r, null, new Color(.05f, .08f, .16f, .35f)); Stretch(dim.rectTransform); dim.raycastTarget = false;

        scanFrame = Rect("Marco", r); Center(scanFrame, new Vector2(0, 40), new Vector2(640, 640));
        for (int i = 0; i < 4; i++)
        {
            var c = Img(scanFrame, sCorner, White);
            bool left = i % 2 == 0, top = i < 2;
            var a = new Vector2(left ? 0 : 1, top ? 1 : 0);
            Anchor(c.rectTransform, a, a, new Vector2(left ? 75 : -75, top ? -75 : 75), new Vector2(150, 150));
            c.rectTransform.localScale = new Vector3(left ? 1 : -1, top ? 1 : -1, 1);
        }
        var cam = Img(scanFrame, iCamera, new Color(1, 1, 1, .8f)); Center(cam.rectTransform, Vector2.zero, new Vector2(120, 120));

        var card = Rect("Aviso", r);
        Anchor(card, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -60), new Vector2(900, 230));
        card.pivot = new Vector2(.5f, 1);
        Shadow(card);
        var bg = Img(card, sRound, White); Stretch(bg.rectTransform); bg.pixelsPerUnitMultiplier = 1f;
        scanPill = Img(card, sRound, Navy); Anchor(scanPill.rectTransform, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(40, 0), new Vector2(130, 130));
        scanPill.rectTransform.pivot = new Vector2(0, .5f); scanPill.pixelsPerUnitMultiplier = .7f;
        var ic = Img(scanPill.rectTransform, iCamera, White); Center(ic.rectTransform, Vector2.zero, new Vector2(70, 70));
        scanTitle = Txt(card, "Enfoca el marcador de Chuwi", fBold, 40, Navy, TextAnchor.LowerLeft);
        Anchor(scanTitle.rectTransform, new Vector2(0, .5f), new Vector2(1, 1), new Vector2(105, -5), new Vector2(-250, -30));
        scanSub = Txt(card, "Apunta la cámara al mapa impreso para que aparezca Chuwi.", fRegular, 28, Muted, TextAnchor.UpperLeft);
        Anchor(scanSub.rectTransform, new Vector2(0, 0), new Vector2(1, .5f), new Vector2(105, 5), new Vector2(-250, -40));

        var prev = OutlineButton(r, "Ver sin marcador", White, () => { if (travel != null) travel.SetPreview(true); });
        Anchor(prev, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 110), new Vector2(560, 120));
    }

    // ---------------- HUD superior ----------------
    void BuildHud()
    {
        var r = Rect("Hud", safe);
        Anchor(r, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -30), new Vector2(-60, 130));
        r.pivot = new Vector2(.5f, 1);
        hud = r.gameObject.AddComponent<CanvasGroup>(); hud.alpha = 0;

        var brand = Rect("Marca", r); Anchor(brand, new Vector2(0, .5f), new Vector2(0, .5f), Vector2.zero, new Vector2(400, 120));
        brand.pivot = new Vector2(0, .5f);
        Shadow(brand);
        var bb = Img(brand, sRound, White); Stretch(bb.rectTransform); bb.pixelsPerUnitMultiplier = 44f / 60f;
        var dot = Img(brand, sCircle, Red); Anchor(dot.rectTransform, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(70, 0), new Vector2(84, 84));
        var dt = Txt(dot.rectTransform, "C", fBlack, 46, White, TextAnchor.MiddleCenter); Stretch(dt.rectTransform);
        var bt = Txt(brand, "Chuwi AR", fBold, 40, Navy, TextAnchor.MiddleLeft);
        Anchor(bt.rectTransform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(70, 0), new Vector2(-140, 0));
        bt.rectTransform.offsetMin = new Vector2(130, 0); bt.rectTransform.offsetMax = new Vector2(-10, 0);

        danceButton = RoundIconButton(r, iDance, Orange, Dance);
        Anchor((RectTransform)danceButton.transform, new Vector2(1, .5f), new Vector2(1, .5f), new Vector2(-150, 0), new Vector2(120, 120));
        var home = RoundIconButton(r, iClose, Navy, () =>
        {
            state = State.Welcome; welcome.alpha = 1; welcome.blocksRaycasts = true; welcome.interactable = true;
        });
        Anchor((RectTransform)home.transform, new Vector2(1, .5f), new Vector2(1, .5f), new Vector2(-10, 0), new Vector2(120, 120));
        ((RectTransform)home.transform).pivot = new Vector2(1, .5f); ((RectTransform)danceButton.transform).pivot = new Vector2(1, .5f);
    }

    // ---------------- Globo de dialogo ----------------
    void BuildBubble()
    {
        bubbleRect = Rect("Globo", safe);
        Anchor(bubbleRect, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -190), new Vector2(860, 150));
        bubbleRect.pivot = new Vector2(.5f, 1);
        bubble = bubbleRect.gameObject.AddComponent<CanvasGroup>(); bubble.alpha = 0; bubble.blocksRaycasts = false;
        Shadow(bubbleRect);
        var bg = Img(bubbleRect, sRound, Navy); Stretch(bg.rectTransform);
        var face = Img(bubbleRect, sChuwiHola, White); face.preserveAspect = true;
        Anchor(face.rectTransform, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(20, 10), new Vector2(120, 170));
        face.rectTransform.pivot = new Vector2(0, .5f);
        bubbleText = Txt(bubbleRect, "", fBold, 38, White, TextAnchor.MiddleLeft);
        Stretch(bubbleText.rectTransform); bubbleText.rectTransform.offsetMin = new Vector2(160, 10); bubbleText.rectTransform.offsetMax = new Vector2(-30, -10);
    }

    // ---------------- Hoja inferior ----------------
    void BuildSheet()
    {
        sheetRect = Rect("Hoja", safe);
        sheet = sheetRect.gameObject.AddComponent<CanvasGroup>(); sheet.alpha = 0;
        Shadow(sheetRect);
        var bg = Img(sheetRect, sRound, White); Stretch(bg.rectTransform); bg.raycastTarget = true;
        var handle = Img(sheetRect, sRound, Line); Anchor(handle.rectTransform, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -22), new Vector2(110, 12));
        handle.pixelsPerUnitMultiplier = 8;

        // ---- Panel mapa ----
        mapPanel = Rect("PanelMapa", sheetRect); Stretch(mapPanel);
        var t = Txt(mapPanel, "Elige tu destino", fBold, 46, Navy, TextAnchor.MiddleLeft);
        Anchor(t.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -90), new Vector2(-100, 70));
        var t2 = Txt(mapPanel, "Toca un país o un pin del mapa y Chuwi te lleva.", fRegular, 28, Muted, TextAnchor.MiddleLeft);
        Anchor(t2.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -145), new Vector2(-100, 44));

        var chipScroll = ScrollH(mapPanel, out chipRow, 10);
        Anchor((RectTransform)chipScroll.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -225), new Vector2(0, 86));
        AddChip("", "Todos");
        if (travel != null)
        {
            var seen = new List<string>();
            foreach (var d in travel.destinations)
                if (!string.IsNullOrEmpty(d.continent) && !seen.Contains(d.continent)) { seen.Add(d.continent); AddChip(d.continent, d.continent); }
        }

        cardScroll = ScrollH(mapPanel, out cardRow, 24);
        var cs = (RectTransform)cardScroll.transform;
        cs.anchorMin = new Vector2(0, 0); cs.anchorMax = new Vector2(1, 1); cs.offsetMin = new Vector2(0, 30); cs.offsetMax = new Vector2(0, -290);
        cardRow.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.UpperLeft;
        RebuildCards();

        // ---- Panel viajando ----
        travelPanel = Rect("PanelViaje", sheetRect); Stretch(travelPanel);
        var plane = Img(travelPanel, sCircle, Orange); Anchor(plane.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(50, -60), new Vector2(120, 120));
        plane.rectTransform.pivot = new Vector2(0, 1);
        var pi = Img(plane.rectTransform, iPlane, White); Center(pi.rectTransform, Vector2.zero, new Vector2(66, 66));
        travelTitle = Txt(travelPanel, "Viajando…", fBold, 44, Navy, TextAnchor.MiddleLeft);
        Anchor(travelTitle.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(95, -75), new Vector2(-250, 60));
        var ts = Txt(travelPanel, "Chuwi va caminando hasta el pin…", fRegular, 28, Muted, TextAnchor.MiddleLeft);
        Anchor(ts.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(95, -130), new Vector2(-250, 44));
        var track = Img(travelPanel, sRound, Line); track.pixelsPerUnitMultiplier = 44f / 12f;
        Anchor(track.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -215), new Vector2(-100, 24));
        travelBar = Img(track.rectTransform, sRound, Orange).rectTransform; ((Image)travelBar.GetComponent<Image>()).pixelsPerUnitMultiplier = 44f / 12f;
        travelBar.anchorMin = Vector2.zero; travelBar.anchorMax = new Vector2(.05f, 1); travelBar.offsetMin = travelBar.offsetMax = Vector2.zero;

        // ---- Panel ficha ----
        infoPanel = Rect("PanelFicha", sheetRect); Stretch(infoPanel);
        var back = RoundIconButton(infoPanel, iBack, Bg, BackToMap, Navy);
        Anchor((RectTransform)back.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -50), new Vector2(100, 100));
        ((RectTransform)back.transform).pivot = new Vector2(0, 1);
        infoBadge = Img(infoPanel, sCircle, Navy);
        Anchor(infoBadge.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(160, -50), new Vector2(100, 100));
        infoBadge.rectTransform.pivot = new Vector2(0, 1);
        infoBadgeText = Txt(infoBadge.rectTransform, "JP", fBlack, 36, White, TextAnchor.MiddleCenter); Stretch(infoBadgeText.rectTransform);
        peek = Img(infoPanel, sChuwiSenala, White); peek.preserveAspect = true;
        Anchor(peek.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-30, -20), new Vector2(200, 320));
        peek.rectTransform.pivot = new Vector2(1, 0);
        infoTitle = Txt(infoPanel, "País", fBlack, 52, Navy, TextAnchor.MiddleLeft);
        infoTitle.resizeTextForBestFit = true; infoTitle.resizeTextMinSize = 30; infoTitle.resizeTextMaxSize = 52;
        infoTitle.horizontalOverflow = HorizontalWrapMode.Overflow;
        Anchor(infoTitle.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -50), new Vector2(0, 66));
        infoTitle.rectTransform.pivot = new Vector2(0, 1); infoTitle.rectTransform.offsetMin = new Vector2(285, infoTitle.rectTransform.offsetMin.y);
        infoTitle.rectTransform.offsetMax = new Vector2(-40, infoTitle.rectTransform.offsetMax.y);
        infoContinentChip = Img(infoPanel, sRound, Navy); infoContinentChip.pixelsPerUnitMultiplier = 44f / 20f;
        Anchor(infoContinentChip.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(285, -118), new Vector2(330, 40));
        infoContinentChip.rectTransform.pivot = new Vector2(0, 1);
        infoContinent = Txt(infoContinentChip.rectTransform, "ASIA", fBold, 22, White, TextAnchor.MiddleCenter); Stretch(infoContinent.rectTransform);

        var tabScroll = ScrollH(infoPanel, out tabRow, 10);
        Anchor((RectTransform)tabScroll.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -205), new Vector2(0, 80));

        infoScroll = ScrollV(infoPanel, out infoContent);
        var isr = (RectTransform)infoScroll.transform;
        isr.anchorMin = Vector2.zero; isr.anchorMax = Vector2.one; isr.offsetMin = new Vector2(40, 175); isr.offsetMax = new Vector2(-40, -260);

        var ask = BigButton(infoPanel, "Solicitar información", Red, White, iMail, AskInfo);
        Anchor(ask, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 40), new Vector2(-80, 120));
        ask.pivot = new Vector2(.5f, 0);
    }

    void AskInfo()
    {
        string c = infoIndex >= 0 ? travel.destinations[infoIndex].country : "";
        string subject = Uri.EscapeDataString("Chuwi AR - Información sobre " + c);
        Application.OpenURL("mailto:" + ChuwiDestinos.Contacto + "?subject=" + subject);
    }

    void AddChip(string key, string label)
    {
        var go = new GameObject("Chip " + label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(chipRow, false);
        var img = go.GetComponent<Image>(); img.sprite = sRound; img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = 44f / 40f;
        var le = go.GetComponent<LayoutElement>(); le.preferredHeight = 80; le.preferredWidth = 70 + label.Length * 19;
        var txt = Txt((RectTransform)go.transform, label, fSemi, 30, Navy, TextAnchor.MiddleCenter); Stretch(txt.rectTransform);
        var b = go.GetComponent<Button>(); b.transition = Selectable.Transition.None;
        go.AddComponent<ChuwiPress>();
        b.onClick.AddListener(() => { continentFilter = key; RebuildCards(); });
        chips.Add((b, img, txt, key));
        PaintChips();
    }

    void PaintChips()
    {
        foreach (var c in chips)
        {
            bool on = c.key == continentFilter;
            c.img.sprite = on ? sRound : sOutline;
            c.img.color = on ? (string.IsNullOrEmpty(c.key) ? Navy : ChuwiDestinos.ContinentColor(c.key)) : Line;
            c.txt.color = on ? White : Navy;
        }
    }

    void RebuildCards()
    {
        PaintChips();
        for (int i = cardRow.childCount - 1; i >= 0; i--) Destroy(cardRow.GetChild(i).gameObject);
        if (travel == null || travel.destinations == null) return;
        for (int i = 0; i < travel.destinations.Length; i++)
        {
            var d = travel.destinations[i];
            if (!string.IsNullOrEmpty(continentFilter) && d.continent != continentFilter && i != 0) continue;
            int idx = i;
            var info = ChuwiDestinos.Get(d.country);
            var col = ChuwiDestinos.ContinentColor(d.continent);

            var go = new GameObject("Tarjeta " + d.country, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(cardRow, false);
            var le = go.GetComponent<LayoutElement>(); le.preferredWidth = 300; le.preferredHeight = 400; le.minHeight = 400;
            var img = go.GetComponent<Image>(); img.sprite = sRound; img.type = Image.Type.Sliced; img.color = idx == 0 ? Cream : Bg;
            img.pixelsPerUnitMultiplier = 44f / 34f;
            var rt = (RectTransform)go.transform;
            var strip = Img(rt, sRound, col); strip.pixelsPerUnitMultiplier = 44f / 6f;
            Anchor(strip.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -14), new Vector2(-60, 12));
            var badge = Img(rt, sCircle, col); Anchor(badge.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(30, -48), new Vector2(96, 96));
            badge.rectTransform.pivot = new Vector2(0, 1);
            var bt = Txt(badge.rectTransform, ChuwiDestinos.Code(d.country), fBlack, 34, White, TextAnchor.MiddleCenter); Stretch(bt.rectTransform);
            var name = Txt(rt, idx == 0 ? "ESAN Lima" : d.country, fBold, 34, Navy, TextAnchor.UpperLeft);
            Anchor(name.rectTransform, new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            name.rectTransform.offsetMin = new Vector2(30, 60); name.rectTransform.offsetMax = new Vector2(-20, -160);
            string sub = idx == 0 ? "Punto de partida" : info != null ? info.UniversityCount + (info.UniversityCount == 1 ? " universidad" : " universidades") : d.continent;
            var s = Txt(rt, sub, fRegular, 25, Muted, TextAnchor.LowerLeft);
            Anchor(s.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 26), new Vector2(0, 40));
            s.rectTransform.pivot = new Vector2(.5f, 0); s.rectTransform.offsetMin = new Vector2(30, 26); s.rectTransform.offsetMax = new Vector2(-20, 66);
            var b = go.GetComponent<Button>(); b.transition = Selectable.Transition.None;
            go.AddComponent<ChuwiPress>();
            b.onClick.AddListener(() => SelectCountry(idx));
        }
        cardScroll.horizontalNormalizedPosition = 0;
    }

    void BuildTabs(ChuwiDestinos.Info info)
    {
        for (int i = tabRow.childCount - 1; i >= 0; i--) Destroy(tabRow.GetChild(i).gameObject);
        tabs.Clear();
        if (info == null) return;
        for (int i = 0; i < info.sections.Count; i++)
        {
            int idx = i; string label = info.sections[i].name;
            var go = new GameObject("Tab " + label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(tabRow, false);
            var img = go.GetComponent<Image>(); img.sprite = sRound; img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = 44f / 38f;
            var le = go.GetComponent<LayoutElement>(); le.preferredHeight = 76; le.preferredWidth = 70 + label.Length * 19;
            var txt = Txt((RectTransform)go.transform, label, fSemi, 29, Navy, TextAnchor.MiddleCenter); Stretch(txt.rectTransform);
            var b = go.GetComponent<Button>(); b.transition = Selectable.Transition.None; go.AddComponent<ChuwiPress>();
            b.onClick.AddListener(() => { infoTab = idx; FillInfo(info); });
            tabs.Add((b, img, txt, idx));
        }
    }

    void FillInfo(ChuwiDestinos.Info info)
    {
        foreach (var t in tabs)
        {
            bool on = t.idx == infoTab;
            t.img.color = on ? Red : Bg; t.txt.color = on ? White : Navy;
        }
        for (int i = infoContent.childCount - 1; i >= 0; i--) Destroy(infoContent.GetChild(i).gameObject);

        AddParagraph(infoIntroText, fRegular, 30, Navy);
        if (info == null || info.sections.Count == 0) return;
        var sec = info.sections[Mathf.Clamp(infoTab, 0, info.sections.Count - 1)];
        foreach (var it in sec.items) AddItemCard(it, sec.name);
        if (!string.IsNullOrEmpty(sec.note))
        {
            var card = NewCard(Cream);
            var t1 = Txt(card, sec.name == "Contacto" ? "Más información" : "Requisitos", fBold, 30, Orange, TextAnchor.UpperLeft); FlowChild(t1, 44);
            var t2 = Txt(card, sec.note, fRegular, 27, Navy, TextAnchor.UpperLeft); FlowChild(t2, -1);
        }
        AddParagraph(ChuwiDestinos.Fuente, fRegular, 22, Muted);
        infoScroll.verticalNormalizedPosition = 1;
    }

    void AddParagraph(string text, Font f, int size, Color c)
    {
        Txt(infoContent, text, f, size, c, TextAnchor.UpperLeft);
    }

    RectTransform NewCard(Color c)
    {
        var go = new GameObject("Tarjeta", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        go.transform.SetParent(infoContent, false);
        var img = go.GetComponent<Image>(); img.sprite = sRound; img.type = Image.Type.Sliced; img.color = c; img.pixelsPerUnitMultiplier = 44f / 28f;
        var v = go.GetComponent<VerticalLayoutGroup>(); v.padding = new RectOffset(32, 32, 26, 26); v.spacing = 6;
        v.childControlWidth = true; v.childControlHeight = true; v.childForceExpandHeight = false; v.childForceExpandWidth = true;
        return (RectTransform)go.transform;
    }

    void AddItemCard(ChuwiDestinos.Item it, string section)
    {
        var go = new GameObject("Item", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
        go.transform.SetParent(infoContent, false);
        var img = go.GetComponent<Image>(); img.sprite = sRound; img.type = Image.Type.Sliced; img.color = Bg; img.pixelsPerUnitMultiplier = 44f / 28f;
        var h = go.GetComponent<HorizontalLayoutGroup>(); h.padding = new RectOffset(24, 28, 22, 22); h.spacing = 22;
        h.childAlignment = TextAnchor.MiddleLeft; h.childControlWidth = true; h.childControlHeight = true; h.childForceExpandWidth = false; h.childForceExpandHeight = false;

        var icon = Img((RectTransform)go.transform, sCircle, section == "Becas" ? Orange : section == "Contacto" ? Navy : Red);
        var ile = icon.gameObject.AddComponent<LayoutElement>(); ile.preferredWidth = 76; ile.preferredHeight = 76; ile.minWidth = 76;
        var ii = Img(icon.rectTransform, section == "Contacto" ? iMail : section == "Becas" ? iStar : iPin, White);
        Center(ii.rectTransform, Vector2.zero, new Vector2(42, 42));

        var col = new GameObject("Textos", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        col.transform.SetParent(go.transform, false);
        col.GetComponent<LayoutElement>().flexibleWidth = 1;
        var v = col.GetComponent<VerticalLayoutGroup>(); v.spacing = 2; v.childControlWidth = true; v.childControlHeight = true; v.childForceExpandHeight = false;
        Txt((RectTransform)col.transform, it.title, fSemi, 30, Navy, TextAnchor.UpperLeft);
        if (!string.IsNullOrEmpty(it.detail)) Txt((RectTransform)col.transform, it.detail, fRegular, 25, Muted, TextAnchor.UpperLeft);
    }

    static void FlowChild(Text t, float minH) { if (minH > 0) { var le = t.gameObject.AddComponent<LayoutElement>(); le.minHeight = minH; } }

    // ======================================================================
    // Layout (vertical u horizontal)
    void ApplyLayout()
    {
        layoutDirty = false;
        lastScreen = new Vector2(Screen.width, Screen.height); lastSafe = Screen.safeArea;
        landscape = Screen.width > Screen.height;
        scaler.referenceResolution = landscape ? new Vector2(1920, 1080) : new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = landscape ? 1 : 0;

        // Safe area (notch)
        var sa = Screen.safeArea;
        if (Screen.width > 0 && Screen.height > 0)
        {
            safe.anchorMin = new Vector2(sa.xMin / Screen.width, sa.yMin / Screen.height);
            safe.anchorMax = new Vector2(sa.xMax / Screen.width, sa.yMax / Screen.height);
            safe.offsetMin = safe.offsetMax = Vector2.zero;
        }

        if (landscape)
        {
            // hoja a la derecha
            sheetRect.anchorMin = new Vector2(1, 0); sheetRect.anchorMax = new Vector2(1, 1); sheetRect.pivot = new Vector2(1, .5f);
            sheetRect.sizeDelta = new Vector2(800, -60); sheetRect.anchoredPosition = new Vector2(-30, 0);
            bubbleRect.anchorMin = bubbleRect.anchorMax = new Vector2(0, 1); bubbleRect.pivot = new Vector2(0, 1);
            bubbleRect.anchoredPosition = new Vector2(30, -190); bubbleRect.sizeDelta = new Vector2(760, 140);
            ((RectTransform)hud.transform).offsetMax = new Vector2(-860, ((RectTransform)hud.transform).offsetMax.y);
            welcomeArt.anchorMin = welcomeArt.anchorMax = new Vector2(.27f, .48f); welcomeArt.sizeDelta = Vector2.zero;
            welcomeArt.localScale = Vector3.one * .95f;
            welcomeText.anchorMin = new Vector2(.5f, .5f); welcomeText.anchorMax = new Vector2(.97f, .5f);
            welcomeText.pivot = new Vector2(.5f, .5f); welcomeText.sizeDelta = new Vector2(0, 820); welcomeText.anchoredPosition = new Vector2(0, -20);
        }
        else
        {
            sheetRect.anchorMin = new Vector2(0, 0); sheetRect.anchorMax = new Vector2(1, 0); sheetRect.pivot = new Vector2(.5f, 0);
            sheetRect.anchoredPosition = new Vector2(0, -40);
            bubbleRect.anchorMin = bubbleRect.anchorMax = new Vector2(.5f, 1); bubbleRect.pivot = new Vector2(.5f, 1);
            bubbleRect.anchoredPosition = new Vector2(0, -190); bubbleRect.sizeDelta = new Vector2(940, 140);
            ((RectTransform)hud.transform).offsetMax = new Vector2(-30, ((RectTransform)hud.transform).offsetMax.y);
            welcomeArt.anchorMin = welcomeArt.anchorMax = new Vector2(.5f, .68f); welcomeArt.sizeDelta = Vector2.zero;
            welcomeArt.localScale = Vector3.one * .9f;
            welcomeText.anchorMin = new Vector2(0, .42f); welcomeText.anchorMax = new Vector2(1, .42f);
            welcomeText.pivot = new Vector2(.5f, 1); welcomeText.sizeDelta = new Vector2(-60, 820); welcomeText.anchoredPosition = new Vector2(0, 30);
        }
        if (peek != null) peek.gameObject.SetActive(!landscape);
        SizeSheet(false);
    }

    void SizeSheet(bool animate)
    {
        if (landscape) return;
        float h = state == State.Info ? 1150 : state == State.Travel ? 330 : 700;
        sheetRect.sizeDelta = new Vector2(-30, h + 40);
    }

    void ShowPanel(State s, bool instant)
    {
        mapPanel.gameObject.SetActive(s == State.Map || s == State.Welcome);
        travelPanel.gameObject.SetActive(s == State.Travel);
        infoPanel.gameObject.SetActive(s == State.Info);
        SizeSheet(!instant);
        var active = s == State.Info ? infoPanel : s == State.Travel ? travelPanel : mapPanel;
        if (!instant) StartCoroutine(PopIn(active));
    }

    void SetBubble(string text, float seconds = 4f) { bubbleText.text = text; bubbleTimer = seconds; StartCoroutine(PopIn(bubbleRect)); }
    void ShowToastBubble(string text) { SetBubble(text, 2.5f); }

    // ======================================================================
    // Animaciones
    static void Fade(CanvasGroup g, bool on, float speed)
    {
        float target = on ? 1 : 0;
        g.alpha = Mathf.MoveTowards(g.alpha, target, Time.unscaledDeltaTime * speed);
        g.blocksRaycasts = on && g.alpha > .5f; g.interactable = g.blocksRaycasts;
    }

    IEnumerator FadeOut(CanvasGroup g, float t)
    {
        g.blocksRaycasts = false; g.interactable = false;
        float a = g.alpha;
        for (float x = 0; x < t; x += Time.unscaledDeltaTime) { g.alpha = Mathf.Lerp(a, 0, x / t); yield return null; }
        g.alpha = 0;
    }

    static IEnumerator PopIn(RectTransform r)
    {
        if (r == null) yield break;
        var cg = r.GetComponent<CanvasGroup>();
        for (float x = 0; x < .28f; x += Time.unscaledDeltaTime)
        {
            float k = x / .28f; float e = 1 - Mathf.Pow(1 - k, 3);
            float s = Mathf.Lerp(.94f, 1, e) + Mathf.Sin(k * Mathf.PI) * .03f;
            r.localScale = new Vector3(s, s, 1);
            yield return null;
        }
        r.localScale = Vector3.one;
    }

    // ======================================================================
    // Ayudantes de UI
    static Color Hex(string h) { Color c; ColorUtility.TryParseHtmlString("#" + h, out c); return c; }

    static RectTransform Rect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }
    static void Stretch(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }
    static void Center(RectTransform r, Vector2 pos, Vector2 size) { r.anchorMin = r.anchorMax = new Vector2(.5f, .5f); r.anchoredPosition = pos; r.sizeDelta = size; }
    static void Anchor(RectTransform r, Vector2 min, Vector2 max, Vector2 pos, Vector2 size)
    { r.anchorMin = min; r.anchorMax = max; r.anchoredPosition = pos; r.sizeDelta = size; }

    Image Img(RectTransform parent, Sprite s, Color c)
    {
        var go = new GameObject(s != null ? s.name : "Img", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var i = go.GetComponent<Image>(); i.sprite = s; i.color = c; i.raycastTarget = false;
        if (s == sRound || s == sOutline || s == sShadow) i.type = Image.Type.Sliced;
        return i;
    }

    Text Txt(RectTransform parent, string text, Font f, int size, Color c, TextAnchor a)
    {
        var go = new GameObject("Texto", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<Text>(); t.text = text; t.font = f; t.fontSize = size; t.color = c; t.alignment = a;
        t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow; t.raycastTarget = false;
        t.lineSpacing = .95f; t.supportRichText = true;
        return t;
    }

    void Shadow(RectTransform target)
    {
        var s = Img(target, sShadow, new Color(0.05f, .1f, .25f, .45f));
        s.rectTransform.anchorMin = Vector2.zero; s.rectTransform.anchorMax = Vector2.one;
        s.rectTransform.offsetMin = new Vector2(-34, -44); s.rectTransform.offsetMax = new Vector2(34, 24);
        s.transform.SetAsFirstSibling();
    }

    RectTransform Pill(RectTransform parent, string label, Font f, int size, Color bg, Color fg, float h)
    {
        var img = Img(parent, sRound, bg); img.pixelsPerUnitMultiplier = 44f / (h / 2);
        var t = Txt(img.rectTransform, label, f, size, fg, TextAnchor.MiddleCenter); Stretch(t.rectTransform);
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        return img.rectTransform;
    }

    RectTransform BigButton(RectTransform parent, string label, Color bg, Color fg, Sprite icon, Action onClick)
    {
        var r = Rect("Boton " + label, parent);
        Shadow(r);
        var img = Img(r, sRound, bg); Stretch(img.rectTransform); img.raycastTarget = true; img.pixelsPerUnitMultiplier = 44f / 50f;
        var b = img.gameObject.AddComponent<Button>(); b.targetGraphic = img; b.transition = Selectable.Transition.None;
        img.gameObject.AddComponent<ChuwiPress>();
        b.onClick.AddListener(() => onClick());
        var t = Txt(img.rectTransform, label, fBold, 40, fg, TextAnchor.MiddleCenter); Stretch(t.rectTransform);
        if (icon != null)
        {
            t.rectTransform.offsetMin = new Vector2(70, 0);
            var ic = Img(img.rectTransform, icon, fg);
            Anchor(ic.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-label.Length * 11.5f - 10, 0), new Vector2(56, 56));
        }
        return r;
    }

    RectTransform OutlineButton(RectTransform parent, string label, Color c, Action onClick)
    {
        var img = Img(parent, sOutline, c); img.raycastTarget = true; img.pixelsPerUnitMultiplier = 44f / 56f;
        var hit = Img(img.rectTransform, sRound, new Color(1, 1, 1, .12f)); Stretch(hit.rectTransform); hit.pixelsPerUnitMultiplier = 44f / 56f;
        var b = img.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None; img.gameObject.AddComponent<ChuwiPress>();
        b.onClick.AddListener(() => onClick());
        var t = Txt(img.rectTransform, label, fBold, 36, c, TextAnchor.MiddleCenter); Stretch(t.rectTransform);
        return img.rectTransform;
    }

    Button RoundIconButton(RectTransform parent, Sprite icon, Color bg, Action onClick, Color? iconColor = null)
    {
        var img = Img(parent, sCircle, bg); img.raycastTarget = true;
        var b = img.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.ColorTint; b.targetGraphic = img;
        var colors = b.colors; colors.disabledColor = new Color(1, 1, 1, .45f); b.colors = colors;
        img.gameObject.AddComponent<ChuwiPress>();
        b.onClick.AddListener(() => onClick());
        var ic = Img(img.rectTransform, icon, iconColor ?? White); Stretch(ic.rectTransform);
        ic.rectTransform.offsetMin = new Vector2(28, 28); ic.rectTransform.offsetMax = new Vector2(-28, -28);
        return b;
    }

    ScrollRect ScrollH(RectTransform parent, out RectTransform content, float spacing)
    {
        var view = Rect("ScrollH", parent);
        view.gameObject.AddComponent<RectMask2D>();
        var hitImg = view.gameObject.AddComponent<Image>(); hitImg.color = new Color(0, 0, 0, 0);
        var sr = view.gameObject.AddComponent<ScrollRect>(); sr.horizontal = true; sr.vertical = false; sr.movementType = ScrollRect.MovementType.Elastic;
        sr.scrollSensitivity = 30;
        content = Rect("Contenido", view);
        content.anchorMin = new Vector2(0, 0); content.anchorMax = new Vector2(0, 1); content.pivot = new Vector2(0, .5f);
        content.offsetMin = content.offsetMax = Vector2.zero;
        var h = content.gameObject.AddComponent<HorizontalLayoutGroup>(); h.spacing = spacing; h.padding = new RectOffset(50, 50, 4, 4);
        h.childAlignment = TextAnchor.MiddleLeft; h.childControlWidth = true; h.childControlHeight = true;
        h.childForceExpandWidth = false; h.childForceExpandHeight = false;
        var f = content.gameObject.AddComponent<ContentSizeFitter>(); f.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        sr.viewport = view; sr.content = content;
        return sr;
    }

    ScrollRect ScrollV(RectTransform parent, out RectTransform content)
    {
        var view = Rect("ScrollV", parent);
        view.gameObject.AddComponent<RectMask2D>();
        var hitImg = view.gameObject.AddComponent<Image>(); hitImg.color = new Color(0, 0, 0, 0);
        var sr = view.gameObject.AddComponent<ScrollRect>(); sr.horizontal = false; sr.vertical = true; sr.scrollSensitivity = 30;
        content = Rect("Contenido", view);
        content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(.5f, 1);
        content.offsetMin = content.offsetMax = Vector2.zero;
        var v = content.gameObject.AddComponent<VerticalLayoutGroup>(); v.spacing = 18; v.padding = new RectOffset(0, 0, 10, 30);
        v.childControlWidth = true; v.childControlHeight = true; v.childForceExpandHeight = false; v.childForceExpandWidth = true;
        var f = content.gameObject.AddComponent<ContentSizeFitter>(); f.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        sr.viewport = view; sr.content = content;
        return sr;
    }
}

