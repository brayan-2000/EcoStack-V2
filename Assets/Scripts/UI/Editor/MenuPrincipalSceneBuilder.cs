#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using TMPro;

[InitializeOnLoad]
public static class MenuPrincipalSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/Menu_Principal.unity";

    static MenuPrincipalSceneBuilder()
    {
        EditorApplication.delayCall += OnEditorReady;
    }

    private static void OnEditorReady()
    {
        // Construye automáticamente la escena si no existe aún
        if (!File.Exists(ScenePath))
        {
            BuildScene();
        }
    }

    [MenuItem("EcoStack/UI/Construir Escena Menu_Principal")]
    public static void BuildScene()
    {
        Debug.Log("<color=#00FF88><b>[MenuPrincipalSceneBuilder]</b></color> Iniciando creación de 'Menu_Principal'...");

        // 1. Crear nueva escena vacía
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 2. Configurar Main Camera
        GameObject cameraObj = new GameObject("Main Camera");
        cameraObj.tag = "MainCamera";
        Camera cam = cameraObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        ColorUtility.TryParseHtmlString("#12161F", out Color industrialBg);
        cam.backgroundColor = industrialBg;
        cameraObj.AddComponent<AudioListener>();
        cameraObj.transform.position = new Vector3(0, 0, -10);

        // 3. Crear Canvas (Screen Space - Overlay, Scale With Screen Size 1920x1080)
        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // EventSystem
        GameObject eventSystemObj = new GameObject("EventSystem");
        eventSystemObj.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
        eventSystemObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        eventSystemObj.AddComponent<StandaloneInputModule>();
#endif

        // Añadir componente MainMenuController al Canvas
        MainMenuController menuController = canvasObj.AddComponent<MainMenuController>();

        // Paleta de Estética Industrial / Eco-Neón
        ColorUtility.TryParseHtmlString("#00FF88", out Color neonGreen);
        ColorUtility.TryParseHtmlString("#12161F", out Color darkIndustrial);
        ColorUtility.TryParseHtmlString("#18212C", out Color buttonBgColor);
        ColorUtility.TryParseHtmlString("#233142", out Color buttonHoverColor);
        ColorUtility.TryParseHtmlString("#0E131B", out Color panelBgColor);

        // 4.1 Título Grande con TextMeshPro
        GameObject titleObj = new GameObject("Titulo_Principal", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(canvasObj.transform, false);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0, -90);
        titleRect.sizeDelta = new Vector2(1400, 130);

        TextMeshProUGUI titleTMP = titleObj.GetComponent<TextMeshProUGUI>();
        titleTMP.text = "ECO-STACK: RESCATE DE EXTINCIÓN";
        titleTMP.fontSize = 56;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.color = neonGreen;

        // Subtítulo temático
        GameObject subTitleObj = new GameObject("Subtitulo", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        subTitleObj.transform.SetParent(canvasObj.transform, false);
        RectTransform subTitleRect = subTitleObj.GetComponent<RectTransform>();
        subTitleRect.anchorMin = new Vector2(0.5f, 1f);
        subTitleRect.anchorMax = new Vector2(0.5f, 1f);
        subTitleRect.pivot = new Vector2(0.5f, 1f);
        subTitleRect.anchoredPosition = new Vector2(0, -165);
        subTitleRect.sizeDelta = new Vector2(800, 45);

        TextMeshProUGUI subTitleTMP = subTitleObj.GetComponent<TextMeshProUGUI>();
        subTitleTMP.text = "OPERACIONES DE RESCATE BIOCORP // NIVEL ACCESO 1";
        subTitleTMP.fontSize = 18;
        subTitleTMP.fontStyle = FontStyles.Normal;
        subTitleTMP.characterSpacing = 6;
        subTitleTMP.alignment = TextAlignmentOptions.Center;
        subTitleTMP.color = new Color(0.6f, 0.75f, 0.85f, 0.85f);

        // 4.2 Contenedor Central con 3 Botones Estilizados
        GameObject containerObj = new GameObject("Contenedor_Botones", typeof(RectTransform), typeof(VerticalLayoutGroup));
        containerObj.transform.SetParent(canvasObj.transform, false);
        RectTransform containerRect = containerObj.GetComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.pivot = new Vector2(0.5f, 0.5f);
        containerRect.anchoredPosition = new Vector2(0, -40);
        containerRect.sizeDelta = new Vector2(460, 320);

        VerticalLayoutGroup layout = containerObj.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 24;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        Button playBtn = CreateStyledButton(containerObj.transform, "Boton_Jugar", "JUGAR", buttonBgColor, neonGreen, buttonHoverColor);
        Button optionsBtn = CreateStyledButton(containerObj.transform, "Boton_Opciones", "OPCIONES", buttonBgColor, neonGreen, buttonHoverColor);
        Button quitBtn = CreateStyledButton(containerObj.transform, "Boton_Salir", "SALIR", buttonBgColor, neonGreen, buttonHoverColor);

        // 4.3 Panel Oculto por Defecto: Panel_Opciones
        GameObject optionsPanelObj = new GameObject("Panel_Opciones", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        optionsPanelObj.transform.SetParent(canvasObj.transform, false);
        RectTransform panelRect = optionsPanelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(680, 500);

        Image panelImg = optionsPanelObj.GetComponent<Image>();
        panelImg.color = new Color(panelBgColor.r, panelBgColor.g, panelBgColor.b, 0.96f);

        // Barra Neón decorativa en el borde superior del panel
        GameObject panelHeaderObj = new GameObject("Panel_Header_Accent", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelHeaderObj.transform.SetParent(optionsPanelObj.transform, false);
        RectTransform headerRect = panelHeaderObj.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0, 1);
        headerRect.anchorMax = new Vector2(1, 1);
        headerRect.pivot = new Vector2(0.5f, 1);
        headerRect.anchoredPosition = Vector2.zero;
        headerRect.sizeDelta = new Vector2(0, 6);
        panelHeaderObj.GetComponent<Image>().color = neonGreen;

        // Título dentro de Panel_Opciones
        GameObject optionsTitleObj = new GameObject("Texto_TituloOpciones", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        optionsTitleObj.transform.SetParent(optionsPanelObj.transform, false);
        RectTransform optTitleRect = optionsTitleObj.GetComponent<RectTransform>();
        optTitleRect.anchorMin = new Vector2(0.5f, 1f);
        optTitleRect.anchorMax = new Vector2(0.5f, 1f);
        optTitleRect.pivot = new Vector2(0.5f, 1f);
        optTitleRect.anchoredPosition = new Vector2(0, -35);
        optTitleRect.sizeDelta = new Vector2(600, 60);

        TextMeshProUGUI optTitleTMP = optionsTitleObj.GetComponent<TextMeshProUGUI>();
        optTitleTMP.text = "CONFIGURACIÓN";
        optTitleTMP.fontSize = 32;
        optTitleTMP.fontStyle = FontStyles.Bold;
        optTitleTMP.alignment = TextAlignmentOptions.Center;
        optTitleTMP.color = neonGreen;

        // Etiqueta de Volumen General
        GameObject volLabelObj = new GameObject("Texto_Volumen", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        volLabelObj.transform.SetParent(optionsPanelObj.transform, false);
        RectTransform volLabelRect = volLabelObj.GetComponent<RectTransform>();
        volLabelRect.anchorMin = new Vector2(0.5f, 0.5f);
        volLabelRect.anchorMax = new Vector2(0.5f, 0.5f);
        volLabelRect.pivot = new Vector2(0.5f, 0.5f);
        volLabelRect.anchoredPosition = new Vector2(0, 65);
        volLabelRect.sizeDelta = new Vector2(500, 40);

        TextMeshProUGUI volLabelTMP = volLabelObj.GetComponent<TextMeshProUGUI>();
        volLabelTMP.text = "VOLUMEN GENERAL";
        volLabelTMP.fontSize = 22;
        volLabelTMP.fontStyle = FontStyles.Bold;
        volLabelTMP.alignment = TextAlignmentOptions.Center;
        volLabelTMP.color = Color.white;

        // Slider de Volumen General
        Slider volSlider = CreateStyledSlider(optionsPanelObj.transform, "Slider_Volumen", neonGreen, buttonBgColor);
        RectTransform sliderRect = volSlider.GetComponent<RectTransform>();
        sliderRect.anchoredPosition = new Vector2(0, 15);

        // Botón Cerrar en Panel_Opciones
        Button closeBtn = CreateStyledButton(optionsPanelObj.transform, "Boton_Cerrar", "CERRAR", buttonBgColor, neonGreen, buttonHoverColor);
        RectTransform closeBtnRect = closeBtn.GetComponent<RectTransform>();
        closeBtnRect.anchoredPosition = new Vector2(0, -160);
        closeBtnRect.sizeDelta = new Vector2(240, 58);

        // Panel de opciones oculto por defecto
        optionsPanelObj.SetActive(false);

        // 5. Asignar referencia en el Inspector
        SerializedObject so = new SerializedObject(menuController);
        SerializedProperty prop = so.FindProperty("optionsPanel");
        if (prop != null)
        {
            prop.objectReferenceValue = optionsPanelObj;
            so.ApplyModifiedProperties();
        }

        // Vincular eventos persistentes en los botones para el Inspector
        UnityEventTools.AddPersistentListener(playBtn.onClick, menuController.PlayGame);
        UnityEventTools.AddPersistentListener(optionsBtn.onClick, menuController.OpenOptions);
        UnityEventTools.AddPersistentListener(quitBtn.onClick, menuController.QuitGame);
        UnityEventTools.AddPersistentListener(closeBtn.onClick, menuController.CloseOptions);

        // 6. Guardar la escena
        string folder = Path.GetDirectoryName(ScenePath);
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"<color=#00FF88><b>[MenuPrincipalSceneBuilder]</b></color> Escena '{ScenePath}' creada y guardada exitosamente.");
    }

    private static Button CreateStyledButton(Transform parent, string name, string labelText, Color bg, Color neon, Color hover)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);

        RectTransform rect = btnObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(400, 66);

        Image img = btnObj.GetComponent<Image>();
        img.color = bg;

        Button btn = btnObj.GetComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = bg;
        colors.highlightedColor = hover;
        colors.pressedColor = neon;
        colors.selectedColor = hover;
        colors.colorMultiplier = 1f;
        btn.colors = colors;

        // Acento lateral neón
        GameObject accentObj = new GameObject("Accent", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        accentObj.transform.SetParent(btnObj.transform, false);
        RectTransform accentRect = accentObj.GetComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0, 0);
        accentRect.anchorMax = new Vector2(0, 1);
        accentRect.pivot = new Vector2(0, 0.5f);
        accentRect.anchoredPosition = Vector2.zero;
        accentRect.sizeDelta = new Vector2(6, 0);
        accentObj.GetComponent<Image>().color = neon;

        // Texto TMP
        GameObject textObj = new GameObject("Text (TMP)", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(btnObj.transform, false);

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
        tmp.text = labelText;
        tmp.fontSize = 24;
        tmp.fontStyle = FontStyles.Bold;
        tmp.characterSpacing = 4;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return btn;
    }

    private static Slider CreateStyledSlider(Transform parent, string name, Color neon, Color bg)
    {
        GameObject sliderObj = new GameObject(name, typeof(RectTransform), typeof(Slider));
        sliderObj.transform.SetParent(parent, false);

        RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.5f, 0.5f);
        sliderRect.anchorMax = new Vector2(0.5f, 0.5f);
        sliderRect.pivot = new Vector2(0.5f, 0.5f);
        sliderRect.sizeDelta = new Vector2(440, 24);

        Slider slider = sliderObj.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0.8f;
        slider.direction = Slider.Direction.LeftToRight;

        // Fondo
        GameObject bgObj = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bgObj.transform.SetParent(sliderObj.transform, false);
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgObj.GetComponent<Image>().color = new Color(0.18f, 0.24f, 0.32f, 1f);

        // Área de Relleno (Fill Area)
        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0, 0.2f);
        fillAreaRect.anchorMax = new Vector2(1, 0.8f);
        fillAreaRect.sizeDelta = new Vector2(-20, 0);

        GameObject fillObj = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fillObj.transform.SetParent(fillArea.transform, false);
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.sizeDelta = Vector2.zero;
        Image fillImg = fillObj.GetComponent<Image>();
        fillImg.color = neon;
        slider.fillRect = fillRect;

        // Área del Tirador (Handle Slide Area)
        GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(sliderObj.transform, false);
        RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.sizeDelta = new Vector2(-20, 0);

        GameObject handleObj = new GameObject("Handle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        handleObj.transform.SetParent(handleArea.transform, false);
        RectTransform handleRect = handleObj.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(26, 0);
        Image handleImg = handleObj.GetComponent<Image>();
        handleImg.color = Color.white;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImg;

        return slider;
    }
}
#endif
