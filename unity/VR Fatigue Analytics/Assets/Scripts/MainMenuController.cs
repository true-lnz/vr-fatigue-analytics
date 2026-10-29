using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public Text connectionText;
    public Text instructionText;
    public Button startButton;
    public Button exitButton;
    ClientWebSocket socket;

    void Start()
    {
        startButton.onClick.AddListener(StartShift);
        if (exitButton != null) exitButton.onClick.AddListener(QuitApplication);
        startButton.interactable = false;
        connectionText.text = "●  Проверка связи с аналитическим сервером...";
        connectionText.color = new Color(1f, .75f, .2f);
        Connect();
    }

    async void Connect()
    {
        try
        {
            socket = new ClientWebSocket();
            using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
                await socket.ConnectAsync(new Uri("ws://127.0.0.1:8765"), timeout.Token);
            var buffer = new byte[2048];
            var message = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            var text = System.Text.Encoding.UTF8.GetString(buffer, 0, message.Count);
            var compact = text.Replace(" ", "").Replace("\n", "").Replace("\r", "");
            if (compact.Contains("\"state\":\"ready\""))
            {
                bool emotivConnected = compact.Contains("\"emotiv_state\":\"connected\"");
                connectionText.text = emotivConnected
                    ? "●  Сервер подключён · Emotiv Live подключён"
                    : "●  Сервер подключён · Emotiv пока недоступен";
                connectionText.color = emotivConnected
                    ? new Color(.25f, 1f, .55f)
                    : new Color(1f, .75f, .2f);
                instructionText.text = emotivConnected
                    ? "Подключение подтверждено. Можно начинать смену."
                    : "Аналитический сервер работает. Смена доступна, метрики Emotiv будут отмечены как недоступные.";
                startButton.interactable = true;
            }
        }
        catch
        {
            connectionText.text = "●  Сервер не найден · запустите start-defense.cmd";
            connectionText.color = new Color(1f, .3f, .25f);
            instructionText.text = "Старт смены станет доступен после подключения аналитического сервера.";
        }
    }

    public void StartShift() { SceneManager.LoadScene("OperatorTest"); }
    public void QuitApplication() { Application.Quit(); }
    void OnDestroy() { if (socket != null) socket.Dispose(); }
}
