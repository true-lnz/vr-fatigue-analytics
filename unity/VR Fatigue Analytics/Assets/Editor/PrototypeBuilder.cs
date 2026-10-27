using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using System.IO;
using UnityEditor.Build.Reporting;

public static class PrototypeBuilder
{
    [MenuItem("Fatigue Analytics/Build Coursework Scenes")]
    public static void Build()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camera = new GameObject("Main Camera").AddComponent<Camera>();
        camera.tag = "MainCamera"; camera.transform.position = new Vector3(0, 1.65f, -6); camera.transform.LookAt(new Vector3(0, 1.7f, 3));
        camera.gameObject.AddComponent<MouseLookController>();
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.025f, .055f, .09f);
        new GameObject("Directional Light").AddComponent<Light>().type = LightType.Directional;
        var controller = new GameObject("OperatorTest").AddComponent<OperatorTestController>();
        var canvas = new GameObject("Operator UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1280, 720);
        var eventSystem = new GameObject("EventSystem", typeof(EventSystem));
        eventSystem.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
        UiPanel(canvas.transform, "Нижняя панель статистики", new Vector2(0, -312), new Vector2(1240, 70), new Color(.015f, .055f, .085f, .94f));
        controller.phaseText = Label(canvas.transform, "Phase", "ФАЗА 1 · СТАБИЛЬНАЯ РАБОТА", 17, new Vector2(-360, 315));
        controller.timerText = Label(canvas.transform, "Timer", "60.0 с", 28, new Vector2(505, 315));
        controller.connectionText = Label(canvas.transform, "Connection", "", 15, new Vector2(0, 265));
        controller.connectionText.gameObject.SetActive(false);
        controller.statusText = Label(canvas.transform, "Status", "Подготовка смены...", 20, new Vector2(0, 200));
        controller.scoreText = Label(canvas.transform, "Score", "РЕШЕНО  0    ОШИБОК  0", 16, new Vector2(390, 210));
        controller.alarmText = Label(canvas.transform, "Alarm", "ТРЕВОГА СЕТИ", 20, new Vector2(-330, 220));
        controller.resultText = Label(canvas.transform, "Result", "СТАТИСТИКА СМЕНЫ  ·  WASD — перемещение по карте  ·  Нажимайте кнопки у консолей  ·  ENTER — подтвердить", 14, new Vector2(0, -312));
        controller.startButton = Button(canvas.transform, "StartButton", "НАЧАТЬ СМЕНУ", new Vector2(0, -160));
        var panel = GameObject.CreatePrimitive(PrimitiveType.Cube); panel.name = "OperatorPanel"; panel.transform.position = new Vector3(0, 1.8f, 4); panel.transform.localScale = new Vector3(11, 4.2f, .3f);
        var material = Mat(new Color(.025f, .07f, .10f)); panel.GetComponent<Renderer>().sharedMaterial = material;
        BuildDispatchRoom();
        AddInteractiveGridLines(controller);
        EditorSceneManager.SaveScene(scene, "Assets/OperatorTest.unity");
        BuildMenuScene();
        BuildResultsScene();
        EditorBuildSettings.scenes = new[] {
            new EditorBuildSettingsScene("Assets/MainMenu.unity", true),
            new EditorBuildSettingsScene("Assets/OperatorTest.unity", true),
            new EditorBuildSettingsScene("Assets/Results.unity", true)
        };
        Debug.Log("Energy dispatch prototype scenes created: MainMenu, OperatorTest, Results");
    }

    [MenuItem("Fatigue Analytics/Build Windows App")]
    public static void BuildWindows()
    {
        var options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/MainMenu.unity", "Assets/OperatorTest.unity", "Assets/Results.unity" },
            locationPathName = "Build/VRFatigueAnalytics_Updated.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };
        var report = BuildPipeline.BuildPlayer(options);
        Debug.Log("Windows player build: " + report.summary.result + " at " + options.locationPathName);
    }

    public static void BuildAll()
    {
        Build();
        BuildWindows();
    }

    static void BuildMenuScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camera = new GameObject("Main Camera").AddComponent<Camera>(); camera.tag = "MainCamera"; camera.transform.position = new Vector3(0,1.7f,-8); camera.transform.LookAt(new Vector3(0,2.5f,4));
        var canvas = new GameObject("Main Menu UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        var events = new GameObject("EventSystem", typeof(EventSystem)); events.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
        BuildDispatchRoom();
        UiPanel(canvas.transform, "MenuCard", new Vector2(-250, 5), new Vector2(560, 560), new Color(.015f, .05f, .08f, .94f));
        var menu = new GameObject("MainMenuController").AddComponent<MainMenuController>();
        Label(canvas.transform, "Kicker", "ТРЕНАЖЁР ДИСПЕТЧЕРА", 16, new Vector2(-250, 205));
        Label(canvas.transform, "Title", "ДИСПЕТЧЕРСКАЯ\nЭНЕРГОСЕТИ", 34, new Vector2(-250, 125));
        Label(canvas.transform, "Description", "60 секунд · 3 фазы нагрузки", 19, new Vector2(-250, 40));
        menu.connectionText = Label(canvas.transform, "Connection", "Проверка связи...", 19, new Vector2(-250, -25));
        var start = Button(canvas.transform, "StartButton", "НАЧАТЬ СМЕНУ", new Vector2(-250, -100)); menu.startButton = start;
        menu.instructionText = Label(canvas.transform, "Hint", "Клавиша Tab — карта / обзор", 16, new Vector2(-250, -175));
        menu.exitButton = Button(canvas.transform, "ExitButton", "ВЫХОД", new Vector2(-250, -235));
        EditorSceneManager.SaveScene(scene, "Assets/MainMenu.unity");
    }

    static void AddInteractiveGridLines(OperatorTestController controller)
    {
        // Линии управления должны находиться в том же слое, что и
        // голографическая карта, иначе после переноса карты они остаются
        // в старой глубине и визуально "съезжают".
        AddGridLine("TargetNorth", controller, new Vector3(-2.25f, 4.45f, 9.08f), new Vector3(1.35f, .09f, .08f), new Color(.12f, .72f, .84f));
        AddGridLine("TargetSouth", controller, new Vector3(2.25f, 4.45f, 9.08f), new Vector3(1.35f, .09f, .08f), new Color(.12f, .72f, .84f));
        AddGridLine("TargetReserve", controller, new Vector3(-.05f, 5.55f, 9.08f), new Vector3(.09f, 1.0f, .08f), new Color(1f, .57f, .16f));
    }

    static void AddGridLine(string targetId, OperatorTestController controller, Vector3 position, Vector3 scale, Color color)
    {
        var line = Box(targetId, position, scale, color);
        if (line.GetComponent<DispatchLineTarget>() == null) line.AddComponent<DispatchLineTarget>();
        var target = line.GetComponent<DispatchLineTarget>();
        target.targetId = targetId;
        target.controller = controller;
    }

    static void BuildResultsScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camera = new GameObject("Main Camera").AddComponent<Camera>(); camera.tag = "MainCamera";
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.005f, .012f, .035f);
        var canvas = new GameObject("Results UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        var events = new GameObject("EventSystem", typeof(EventSystem)); events.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
        var result = new GameObject("ResultsController").AddComponent<ResultsScreenController>();
        UiPanel(canvas.transform, "ResultBackdrop", new Vector2(0, 0), new Vector2(1280, 720), new Color(.006f, .018f, .045f, .98f));
        for (int i = 0; i < 11; i++) UiPanel(canvas.transform, "GridV" + i, new Vector2(-600 + i * 120, 0), new Vector2(1, 720), new Color(.04f, .32f, .42f, .14f));
        for (int i = 0; i < 6; i++) UiPanel(canvas.transform, "GridH" + i, new Vector2(0, -260 + i * 104), new Vector2(1280, 1), new Color(.04f, .32f, .42f, .14f));
        UiPanel(canvas.transform, "Header", new Vector2(0, 298), new Vector2(1160, 84), new Color(.015f, .09f, .14f, .96f));
        UiPanel(canvas.transform, "ScorePanel", new Vector2(-340, 40), new Vector2(400, 430), new Color(.015f, .075f, .12f, .96f));
        var scoreGlow = UiPanel(canvas.transform, "ScoreGlow", new Vector2(-340, 95), new Vector2(270, 270), new Color(.02f, .28f, .38f, .18f));
        scoreGlow.AddComponent<UiMicroAnimations>().pulse = true;
        UiPanel(canvas.transform, "DetailsPanel", new Vector2(250, 40), new Vector2(700, 430), new Color(.012f, .055f, .095f, .98f));
        UiPanel(canvas.transform, "Footer", new Vector2(0, -285), new Vector2(1160, 64), new Color(.012f, .06f, .095f, .96f));
        result.titleText = Label(canvas.transform, "ReportTitle", "ОТЧЁТ СЕССИИ", 25, new Vector2(0, 300));
        result.scoreText = Label(canvas.transform, "ReportScore", "—", 72, new Vector2(-340, 95));
        result.categoryText = Label(canvas.transform, "ReportCategory", "ОЦЕНКА СОСТОЯНИЯ", 18, new Vector2(-340, -60));
        result.sourceText = Label(canvas.transform, "ReportSource", "ИСТОЧНИК: ОЖИДАНИЕ ДАННЫХ", 13, new Vector2(-340, -130));
        result.detailsText = Label(canvas.transform, "ReportDetails", "Подготовка отчёта...", 18, new Vector2(250, 55));
        result.resultText = result.detailsText;
        result.detailsText.rectTransform.sizeDelta = new Vector2(620, 320);
        result.detailsText.alignment = TextAnchor.UpperLeft;
        result.sourceText.rectTransform.sizeDelta = new Vector2(360, 60);
        result.categoryText.rectTransform.sizeDelta = new Vector2(360, 60);
        result.titleText.rectTransform.sizeDelta = new Vector2(900, 60);
        var back = Button(canvas.transform, "BackButton", "В ГЛАВНОЕ МЕНЮ", new Vector2(0, -285));
        result.backButton = back;
        EditorSceneManager.SaveScene(scene, "Assets/Results.unity");
    }

    static void BuildDispatchRoom()
    {
        RenderSettings.ambientLight = new Color(.22f, .27f, .32f);
        RenderSettings.fog = true; RenderSettings.fogColor = new Color(.035f, .055f, .075f); RenderSettings.fogDensity = .018f;
        Box("Room floor", new Vector3(0,-.18f,5), new Vector3(24,.35f,32), new Color(.10f,.13f,.16f));
        Box("Rear wall", new Vector3(0,4.2f,17), new Vector3(24,9,.35f), new Color(.10f,.15f,.19f));
        Box("Left wall", new Vector3(-11.8f,4.2f,5), new Vector3(.35f,9,24), new Color(.075f,.11f,.14f));
        Box("Right wall", new Vector3(11.8f,4.2f,5), new Vector3(.35f,9,24), new Color(.075f,.11f,.14f));
        for (int i=0;i<7;i++) Box("Ceiling beam",new Vector3(-9+i*3f,8.3f,5),new Vector3(.16f,.18f,24),new Color(.18f,.25f,.29f));
        Box("Console desk", new Vector3(0,.72f,5.2f), new Vector3(16,.34f,2.1f), new Color(.055f,.09f,.12f));
        Box("Console front", new Vector3(0,.23f,4.4f), new Vector3(16,.75f,.5f), new Color(.035f,.06f,.08f));
        // Framed, illuminated regional grid map elevated above consoles to avoid overlap.
        var mapRoot = new GameObject("Голографическая карта");
        var mapAnimation = mapRoot.AddComponent<HolographicMapAnimation>();
        mapAnimation.floatAmount = .018f;
        mapAnimation.floatSpeed = .65f;
        var mapFrame = Box("Интерактивная карта районов",new Vector3(0f,5.05f,9.4f),new Vector3(8.0f,2.45f,.18f),new Color(.025f,.045f,.06f));
        mapFrame.transform.SetParent(mapRoot.transform, true);
        var mapGlass = Box("Стекло карты",new Vector3(0f,5.05f,9.26f),new Vector3(7.7f,2.2f,.06f),new Color(.025f,.11f,.14f));
        mapGlass.transform.SetParent(mapRoot.transform, true);
        Color cyan=new Color(.12f,.72f,.84f), amber=new Color(1f,.57f,.16f), red=new Color(.92f,.19f,.12f), green=new Color(.2f,.78f,.48f);
        for(int x=0;x<5;x++) { var route=Box("Маршрут карты вертикальный",new Vector3(-3.1f+x*1.55f,5.05f,9.18f),new Vector3(.045f,2.0f,.025f),cyan); route.transform.SetParent(mapRoot.transform, true); }
        for(int y=0;y<3;y++) { var route=Box("Маршрут карты горизонтальный",new Vector3(0f,4.35f+y*.7f,9.17f),new Vector3(6.6f,.04f,.025f),cyan); route.transform.SetParent(mapRoot.transform, true); }
        for(int i=0;i<7;i++) { var node=Sphere("Узел района",new Vector3(-2.8f+(i%4)*1.85f,4.45f+(i/4)*1.15f,9.03f),.14f,i==4?red:cyan); node.transform.SetParent(mapRoot.transform, true); node.AddComponent<FuturisticSceneMotion>().motionKind = FuturisticSceneMotion.MotionKind.Float; }
        // Three operator consoles, each with meters, toggle banks and status lamps.
        string[] zones={"СЕВЕРНЫЙ РАЙОН","ЦЕНТРАЛЬНЫЙ РАЙОН","ЮЖНЫЙ РАЙОН"}; Color[] zone={cyan,amber,red};
        for(int i=0;i<3;i++){
            float x=-5.0f+i*5.0f;
            float z=5.5f + (i%2)*2.4f;
            var console = Box(zones[i]+" console",new Vector3(x,2.45f,z),new Vector3(3.8f,2.55f,.18f),new Color(.035f,.075f,.095f));
            var districtAlarm = console.AddComponent<DistrictAlarmVisual>();
            districtAlarm.districtId = i == 0 ? "TargetNorth" : i == 1 ? "TargetReserve" : "TargetSouth";
            var screen = Box(zones[i]+" экран",new Vector3(x,2.68f,z-.14f),new Vector3(3.3f,1.55f,.05f),new Color(.015f,.12f,.15f));
            var screenAnimator = screen.AddComponent<ConsoleScreenAnimator>();
            screenAnimator.districtId = i == 0 ? "TargetNorth" : i == 1 ? "TargetReserve" : "TargetSouth";
            // Load graph bars.
            for(int b=0;b<8;b++) { var bar=Box("Load bar",new Vector3(x-1.18f+b*.34f,2.25f+(b%4)*.13f,z-.20f),new Vector3(.18f,.16f+(b%4)*.12f,.035f),b>5?zone[i]:green); bar.transform.SetParent(screen.transform, true); }
            for(int l=0;l<4;l++) Sphere("Status lamp",new Vector3(x-1.0f+l*.62f,1.48f,z-.18f),.095f,l==2?amber:green);
            for(int k=0;k<3;k++) Box("Переключатель",new Vector3(x-.75f+k*.72f,1.15f,z-.18f),new Vector3(.30f,.09f,.12f),new Color(.35f,.42f,.45f));
            string physicalId = i == 0 ? "TargetNorth" : i == 1 ? "TargetReserve" : "TargetSouth";
            var control = Box("Кнопка " + zones[i], new Vector3(x, .86f, z-.12f), new Vector3(1.35f, .18f, .32f), new Color(.055f,.2f,.25f));
            var button = control.AddComponent<ConsoleControlButton>(); button.targetId = physicalId; var operatorObject = GameObject.Find("OperatorTest"); button.controller = operatorObject == null ? null : operatorObject.GetComponent<OperatorTestController>(); button.buttonRenderer = control.GetComponent<Renderer>();
            var outline = Box("Контур кнопки " + zones[i], new Vector3(x, .86f, z-.32f), new Vector3(1.48f, .23f, .04f), cyan); outline.transform.SetParent(control.transform); outline.transform.localPosition = new Vector3(0, 0, -.2f); button.outlineRenderer = outline.GetComponent<Renderer>(); outline.SetActive(false);
            Text3D(zones[i],new Vector3(x,3.62f,z-.10f),.19f,cyan);
        }
        // Alarm beacon, analog instruments and work lights add depth and scale.
        Box("Alarm beacon rail",new Vector3(0,7.2f,9.0f),new Vector3(10,.12f,.18f),new Color(.12f,.17f,.19f));
        var alarmRoot = new GameObject("RegionalAlarm");
        for(int i=0;i<5;i++) {
            var beacon=Sphere("Маяк тревоги",new Vector3(-4.0f+i*2.0f,7.48f,9.0f),.18f,i==2?red:amber);
            beacon.transform.SetParent(alarmRoot.transform);
            if (i == 2) beacon.AddComponent<AlarmBeaconBlink>();
            else beacon.AddComponent<FuturisticSceneMotion>().motionKind = FuturisticSceneMotion.MotionKind.Pulse;
        }
        for(int i=0;i<4;i++){
            Box("Display bezel",new Vector3(-6.5f+i*4.3f,5.95f,3.55f),new Vector3(2.4f,1.05f,.16f),new Color(.025f,.045f,.055f));
            Box("Display glass",new Vector3(-6.5f+i*4.3f,5.95f,3.43f),new Vector3(2.16f,.82f,.035f),new Color(.02f,.10f,.14f));
            for(int j=0;j<5;j++) Box("Trend trace",new Vector3(-7.25f+i*4.3f+j*.34f,5.8f+(j%3)*.14f,3.39f),new Vector3(.28f,.035f,.02f),j==4?amber:cyan);
        }
        for(int i=0;i<3;i++){
            var light=new GameObject("Industrial work light"); light.transform.position=new Vector3(-5+i*5,7.8f,0); var l=light.AddComponent<Light>(); l.type=LightType.Spot; l.color=new Color(.62f,.78f,.92f); l.intensity=2.2f; l.range=13; l.spotAngle=65;
        }
        Box("Floor safety stripe",new Vector3(0,.015f,-2.5f),new Vector3(13,.025f,.08f),amber);
    }

    static Material Mat(Color color){var m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.color=color;return m;}
    static GameObject Box(string name,Vector3 pos,Vector3 scale,Color color){var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=name;o.transform.position=pos;o.transform.localScale=scale;o.GetComponent<Renderer>().sharedMaterial=Mat(color);return o;}
    static GameObject Sphere(string name,Vector3 pos,float size,Color color){var o=GameObject.CreatePrimitive(PrimitiveType.Sphere);o.name=name;o.transform.position=pos;o.transform.localScale=Vector3.one*size;o.GetComponent<Renderer>().sharedMaterial=Mat(color);return o;}
    static void Text3D(string value,Vector3 pos,float size,Color color){var o=new GameObject("Label "+value);o.transform.position=pos;var t=o.AddComponent<TextMesh>();t.text=value;t.fontSize=64;t.characterSize=size/4f;t.anchor=TextAnchor.MiddleCenter;t.color=color;}

    static Text Label(Transform parent, string name, string value, int size, Vector2 pos)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent);
        var rect = go.GetComponent<RectTransform>();
        bool menuLabel = parent.name == "Main Menu UI";
        rect.sizeDelta = name == "Title" ? new Vector2(menuLabel ? 520 : 900, 125) : new Vector2(menuLabel ? 520 : 900, 60);
        rect.anchoredPosition = pos;
        var text = go.GetComponent<Text>(); text.text = value; text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize = size; text.alignment = TextAnchor.MiddleCenter; text.color = new Color(.83f, .92f, .98f);
        go.AddComponent<UiMicroAnimations>();
        return text;
    }
    static Button Button(Transform parent, string name, string value, Vector2 pos)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)); go.transform.SetParent(parent);
        var rect = go.GetComponent<RectTransform>(); rect.sizeDelta = new Vector2(name == "StartButton" ? 310 : 270, 58); rect.anchoredPosition = pos; go.GetComponent<Image>().color = new Color(.035f, .29f, .39f);
        go.AddComponent<UiMicroAnimations>();
        var colors=go.GetComponent<Button>().colors;colors.highlightedColor=new Color(.08f,.55f,.68f);colors.pressedColor=new Color(.025f,.18f,.24f);go.GetComponent<Button>().colors=colors;
        var label = Label(go.transform, "Text", value, 20, Vector2.zero); label.rectTransform.sizeDelta = rect.sizeDelta;
        return go.GetComponent<Button>();
    }
    static GameObject UiPanel(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent);
        var rect = go.GetComponent<RectTransform>(); rect.sizeDelta = size; rect.anchoredPosition = pos; go.GetComponent<Image>().color = color; go.transform.SetAsFirstSibling();
        return go;
    }
}

