using System.Collections.Generic;
using System.IO;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class OperatorTestController : MonoBehaviour
{
    public Text statusText;
    public Text timerText;
    public Text scoreText;
    public Text resultText;
    public Button startButton;
    public List<Button> targetButtons = new List<Button>();
    private float elapsed;
    private bool running;
    private int correct;
    private int errors;
    private ClientWebSocket socket;
    private string sessionId;
    private readonly Queue<string> incoming = new Queue<string>();
    private float lastGazeEvent;
    private readonly string[] activeTargets = { "TargetNorth", "TargetSouth", "TargetReserve", "TargetNorth", "TargetReserve", "TargetSouth", "TargetNorth", "TargetSouth", "TargetReserve", "TargetNorth" };
    private readonly string[] incidentIds = { "line_north_1", "line_south_1", "line_north_2", "line_central_1", "line_south_2", "line_east_1", "cascade_north", "reserve_check_1", "alarm_false_1", "cascade_north_central" };
    private readonly float[] eventStarts = { 0f, 8f, 16f, 22f, 28f, 40f, 44f, 48f, 52f, 56f };
    private readonly float[] phaseLimits = { 8f, 8f, 6f, 6f, 6f, 4f, 4f, 4f, 4f, 4f };
    private int eventIndex;
    private float eventElapsed;
    private float responseStart;
    private string currentTarget;
    private string selectedTargetId;
    private Color normalColor = new Color(.08f, .25f, .4f);
    public Text alarmText;
    public Text phaseText;
    public Text connectionText;
    private int phase;
    private bool falseAlarm;
    private bool eventResolved;
    private bool eventStarted;
    private Button selectedButton;
    private int cascadeStep;
    private bool analyzing;
    private readonly string[] incidentNames = { "Перегрузка района Северный", "Снижение напряжения Южного района", "Отказ линии Северная-2", "Пиковая нагрузка Центрального района", "Перегрев Южной подстанции", "Падение напряжения Восточного района", "Каскадная перегрузка Северного узла", "Проверка резервного маршрута", "Сигнал датчика Северного узла", "Каскадная перегрузка Северный + Центральный" };
    public GazeSimulator gazeSimulator;
    public bool IsMapMode => Camera.main != null && Camera.main.GetComponent<MouseLookController>() != null && Camera.main.GetComponent<MouseLookController>().MapMode;
    string backendStatus = "проверка";
    string cortexStatus = "ожидание";

    void Start()
    {
        if (GetComponent<CrosshairOverlay>() == null)
            gameObject.AddComponent<CrosshairOverlay>();
        statusText.text = "Наблюдайте за тревогой и нажимайте подсвеченное действие.";
        if (connectionText != null) connectionText.text = "● СЕРВЕР: ПРОВЕРКА    ● CORTEX: ОЖИДАНИЕ";
        timerText.text = "60.0 с";
        scoreText.text = "Ответы: 0 | Ошибки: 0";
        UpdateBottomStatistics();
        if (gazeSimulator == null) gazeSimulator = gameObject.AddComponent<GazeSimulator>();
        gazeSimulator.sourceCamera = Camera.main;
        startButton.gameObject.SetActive(false);
        // Управление выполняется физическими кнопками на консолях.
        // Старые UI-кнопки оставлены в сериализованной сцене для совместимости,
        // но полностью скрываются, чтобы нижняя панель показывала только статистику.
        foreach (var button in targetButtons)
            if (button != null) button.gameObject.SetActive(false);
        currentTarget = activeTargets[0];
        SetTargetVisuals();
        startButton.onClick.AddListener(StartTest);
        foreach (var button in targetButtons)
            if (button != null) button.onClick.AddListener(() => Answer(button));
        Connect();
    }

    void Update()
    {
        lock (incoming)
        {
            while (incoming.Count > 0) HandleResponse(incoming.Dequeue());
        }
        if (!running) return;
        HandleMapClick();
        if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame && !string.IsNullOrEmpty(selectedTargetId))
            ConfirmSelection();
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && elapsed >= 60f)
            SceneManager.LoadScene("Results");
        elapsed += Time.unscaledDeltaTime;
        phase = elapsed < 16f ? 0 : elapsed < 34f ? 1 : 2;
        if (phaseText != null) phaseText.text = phase == 0 ? "ФАЗА 1 · СТАБИЛЬНАЯ РАБОТА" : phase == 1 ? "ФАЗА 2 · ПОВЫШЕННАЯ НАГРУЗКА" : "ФАЗА 3 · КРИЗИСНЫЙ ПИК";
        if (eventIndex < activeTargets.Length && !eventStarted && elapsed >= eventStarts[eventIndex]) ActivateNextTarget();
        float limit = phaseLimits[Mathf.Min(eventIndex, phaseLimits.Length - 1)];
        eventElapsed = eventStarted ? elapsed - eventStarts[eventIndex] : 0f;
        float remaining = Mathf.Max(0f, limit - eventElapsed);
        timerText.color = remaining <= limit * .2f ? new Color(1f, .2f, .15f) : remaining <= limit * .5f ? new Color(1f, .7f, .15f) : Color.white;
        if (remaining <= limit * .2f && eventIndex < activeTargets.Length && !eventResolved)
        {
            statusText.text = "! КРИТИЧЕСКОЕ ВРЕМЯ · " + incidentNames[Mathf.Min(eventIndex, incidentNames.Length - 1)];
        }
        if (eventIndex < activeTargets.Length && eventStarted && eventElapsed >= phaseLimits[eventIndex])
        {
            if (!eventResolved) Send("\"type\":\"behavior_event\",\"session_id\":\"" + sessionId + "\",\"t_s\":" + elapsed.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + ",\"event\":\"target_missed\",\"target_id\":\"" + incidentIds[eventIndex] + "\",\"correct\":false");
            eventIndex++;
            eventStarted = false;
            if (eventIndex < activeTargets.Length && elapsed >= eventStarts[eventIndex]) ActivateNextTarget();
            else if (eventIndex == 5) statusText.text = "Кризисный пик приближается. Контролируйте нагрузку и подготовьте резервные линии.";
            else if (eventIndex >= activeTargets.Length) statusText.text = "Все тревоги обработаны. Дождитесь окончания 60-секундной смены";
        }
        if (Time.unscaledTime - lastGazeEvent >= 1f)
        {
            lastGazeEvent = Time.unscaledTime;
            string gazeTarget = gazeSimulator == null ? "panel" : gazeSimulator.CurrentTarget;
            float dispersion = gazeSimulator == null ? .1f : gazeSimulator.LastDispersion;
            float fixation = gazeSimulator == null ? 1f : gazeSimulator.FixationDuration;
            var gazePosition = gazeSimulator == null ? new Vector2(.5f, .5f) : gazeSimulator.NormalizedPosition;
            Send("\"type\":\"gaze_event\",\"session_id\":\"" + sessionId + "\",\"t_s\":" + elapsed.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + ",\"target_id\":\"" + gazeTarget + "\",\"gaze_x\":" + gazePosition.x.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + ",\"gaze_y\":" + gazePosition.y.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + ",\"event\":\"fixation_end\",\"duration_s\":" + fixation.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + ",\"dispersion\":" + dispersion.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + ",\"distracted\":" + (gazeTarget != currentTarget).ToString().ToLowerInvariant());
        }
        timerText.text = Mathf.Max(0, 60f - elapsed).ToString("0.0") + " с";
        if (elapsed >= 60f) FinishTest();
    }

    void HandleMapClick()
    {
        var mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame || !IsMapMode || Camera.main == null) return;
        var ray = Camera.main.ScreenPointToRay(mouse.position.ReadValue());
        if (Physics.Raycast(ray, out var hit, 100f))
        {
            var line = hit.collider.GetComponent<DispatchLineTarget>();
            if (line != null) SelectMapLine(line.targetId);
        }
    }

    void StartTest()
    {
        elapsed = 0; correct = 0; errors = 0; running = true;
        eventIndex = 0; eventElapsed = 0;
        cascadeStep = 0;
        selectedTargetId = null;
        ActivateNextTarget();
        sessionId = "s-" + Guid.NewGuid().ToString("N").Substring(0, 8);
        if (connectionText != null) connectionText.text = "● СЕРВЕР: СЕССИЯ ЗАПУЩЕНА    ● CORTEX: СБОР МЕТРИК";
        Send("\"type\":\"session_start\",\"session_id\":\"" + sessionId + "\",\"duration_s\":60");
        statusText.text = "Тест идёт: выбирайте активную линию";
        startButton.gameObject.SetActive(false);
        scoreText.text = "РЕШЕНО  0 / 10    ОШИБОК  0";
        UpdateBottomStatistics();
        var look = Camera.main == null ? null : Camera.main.GetComponent<MouseLookController>();
        if (look != null) look.SetMapMode(false);
    }

    void Answer(Button pressed)
    {
        if (pressed == null) return;
        AnswerTarget(pressed.name);
    }

    public void SelectPhysicalControl(string targetId)
    {
        if (!running || string.IsNullOrEmpty(targetId)) return;
        AnswerTarget(targetId);
    }

    void AnswerTarget(string targetId)
    {
        if (!running || eventResolved || string.IsNullOrEmpty(currentTarget)) return;
        selectedTargetId = targetId;
        selectedButton = null;
        statusText.text = "Выбрано: " + TargetLabel(targetId) + ". Нажмите ENTER для подтверждения.";
        SetTargetVisuals();
    }

    public void SelectMapLine(string targetId)
    {
        foreach (var button in targetButtons)
        {
            if (button.name == targetId || (falseAlarm && button.name == "IgnoreButton"))
            {
                AnswerTarget(button.name);
                return;
            }
        }
        AnswerTarget(targetId);
    }

    void ConfirmSelection()
    {
        if (string.IsNullOrEmpty(selectedTargetId)) return;
        string pressedId = selectedTargetId;
        selectedTargetId = null;
        selectedButton = null;
        bool isCorrect = pressedId == ExpectedTarget();
        float reaction = Time.unscaledTime - responseStart;
        if (isCorrect) correct++; else errors++;
        Send("\"type\":\"behavior_event\",\"session_id\":\"" + sessionId + "\",\"t_s\":" + elapsed.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + ",\"event\":\"target_response\",\"target_id\":\"" + incidentIds[eventIndex] + "\",\"reaction_time_s\":" + reaction.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + ",\"correct\":" + (isCorrect ? "true" : "false"));
        scoreText.text = "ВЕРНЫХ ДЕЙСТВИЙ  " + correct + "    ОШИБОК  " + errors;
        UpdateBottomStatistics();
        statusText.text = isCorrect ? "Питание стабилизировано" : "Неверный маршрут. Попробуйте ещё раз.";
        if (isCorrect)
        {
            if (eventIndex == 9 && cascadeStep == 0)
            {
                cascadeStep = 1;
                currentTarget = "TargetSouth";
                responseStart = Time.unscaledTime;
                SetTargetVisuals();
                statusText.text = "Каскадная перегрузка: теперь переключите питание Юга.";
            }
            else
            {
                eventResolved = true;
                var lineTarget = GameObject.Find(currentTarget);
                if (lineTarget != null)
                {
                    var renderer = lineTarget.GetComponent<Renderer>();
                    if (renderer != null) renderer.material.color = new Color(.18f, .85f, .42f);
                    lineTarget.transform.localScale *= 1.18f;
                }
                statusText.text = "Питание стабилизировано. Ожидайте следующую тревогу.";
            }
        }
        SetTargetVisuals();
    }

    void FinishTest()
    {
        running = false;
        analyzing = true;
        Send("\"type\":\"session_finish\",\"session_id\":\"" + sessionId + "\",\"t_s\":60.0");
        statusText.text = "Смена завершена. Готовим результат...";
        if (connectionText != null) connectionText.text = "● СЕРВЕР: АНАЛИЗ    ● CORTEX: ДАННЫЕ ПОЛУЧЕНЫ";
    }

    void ActivateNextTarget()
    {
        eventStarted = true;
        currentTarget = activeTargets[eventIndex];
        eventElapsed = Mathf.Max(0f, elapsed - eventStarts[eventIndex]);
        eventResolved = false;
        selectedButton = null;
        responseStart = Time.unscaledTime;
        falseAlarm = eventIndex == 8;
        SetTargetVisuals();
        statusText.text = falseAlarm
            ? "Сигнал датчика: проверьте показания и выберите действие."
            : "Перегрузка: " + TargetLabel(currentTarget) + ". Нажмите подсвеченную кнопку.";
        if (alarmText != null) alarmText.text = "⚠  " + incidentNames[Mathf.Min(eventIndex, incidentNames.Length - 1)];
    }

    string TargetLabel(string id)
    {
        if (id == "TargetNorth") return "Северная";
        if (id == "TargetSouth") return "Южная";
        return "Резервная";
    }

    public void IgnoreFalseAlarm() { if (running && falseAlarm) AnswerTarget(ExpectedTarget()); }

    void AnswerButtonByName(string buttonName) { foreach (var button in targetButtons) if (button.name == buttonName) { Answer(button); return; } }

    void SetTargetVisuals()
    {
        foreach (var button in targetButtons)
        {
            button.transition = Selectable.Transition.None;
            if (button.name == "IgnoreButton") button.gameObject.SetActive(falseAlarm);
            var image = button.GetComponent<Image>();
            image.color = button.name == ExpectedTarget() ? new Color(.68f, .38f, .12f) : normalColor;
        }
        var physical = FindObjectsByType<ConsoleControlButton>(FindObjectsSortMode.None);
        foreach (var control in physical)
        {
            bool isTarget = control.targetId == ExpectedTarget();
            bool isSelected = control.targetId == selectedTargetId;
            control.SetState(isSelected, isTarget);
        }
        foreach (var screen in FindObjectsByType<ConsoleScreenAnimator>(FindObjectsSortMode.None))
            screen.SetAlert(running && !eventResolved && screen.districtId == ExpectedTarget());
        foreach (var district in FindObjectsByType<DistrictAlarmVisual>(FindObjectsSortMode.None))
            district.SetAlert(running && !eventResolved && district.districtId == ExpectedTarget());
        var alarm = GameObject.Find("RegionalAlarm");
        if (alarm != null) alarm.SetActive(running && !eventResolved && !string.IsNullOrEmpty(currentTarget));
    }

    string ExpectedTarget()
    {
        // The physical console has one action button per district.
        // A false alarm is resolved through the reserve district control.
        return falseAlarm ? "TargetReserve" : currentTarget;
    }

    void UpdateBottomStatistics()
    {
        if (resultText == null) return;
        resultText.text = "СТАТИСТИКА СМЕНЫ  ·  Ответы: " + correct + "  ·  Ошибки: " + errors +
            "  ·  WASD — перемещение по карте  ·  Выберите кнопку у консоли  ·  ENTER — подтвердить";
    }

    void OnDestroy()
    {
        if (socket != null)
        {
            if (socket.State == WebSocketState.Open) socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "session complete", CancellationToken.None);
            socket.Dispose();
        }
    }

    async void Connect()
    {
        try
        {
            socket = new ClientWebSocket();
            await socket.ConnectAsync(new Uri("ws://127.0.0.1:8765"), CancellationToken.None);
            if (connectionText != null) connectionText.text = "● СЕРВЕР: ПОДКЛЮЧЁН    ● CORTEX: ОПРОС";
            _ = ReceiveLoop();
        }
        catch (Exception e) { if (connectionText != null) connectionText.text = "● СЕРВЕР: НЕТ СВЯЗИ    ● CORTEX: НЕДОСТУПЕН"; Debug.LogWarning("Python-сервис недоступен: " + e.Message); }
    }

    async Task ReceiveLoop()
    {
        var buffer = new byte[4096];
        while (socket != null && socket.State == WebSocketState.Open)
        {
            var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            if (result.MessageType == WebSocketMessageType.Close) break;
            lock (incoming) incoming.Enqueue(Encoding.UTF8.GetString(buffer, 0, result.Count));
        }
    }

    async void Send(string fields)
    {
        if (socket == null || socket.State != WebSocketState.Open) return;
        string json = "{\"schema\":\"fatigue.v1\"," + fields + "}";
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        try { await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None); }
        catch (Exception e) { Debug.LogWarning("Не удалось отправить событие: " + e.Message); }
    }

    void HandleResponse(string json)
    {
        string compact = json.Replace(" ", "").Replace("\n", "").Replace("\r", "");
        if (compact.Contains("\"type\":\"service_status\""))
        {
            backendStatus = "подключён";
            if (compact.Contains("\"emotiv_state\":\"connected\"")) cortexStatus = "метрики подключены";
            else cortexStatus = "нет потока метрик";
            if (connectionText != null) connectionText.text = "● СЕРВЕР: " + "ПОДКЛЮЧЁН" + "    ● CORTEX: " + cortexStatus.ToUpperInvariant();
            if (!running && string.IsNullOrEmpty(sessionId)) StartTest();
        }
        if (compact.Contains("\"type\":\"session_result\""))
        {
            analyzing = false;
            SessionResultStore.LastJson = json;
            SceneManager.LoadScene("Results");
        }
    }
}
