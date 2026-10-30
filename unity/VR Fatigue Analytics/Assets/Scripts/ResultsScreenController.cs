using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public static class SessionResultStore
{
    public static string LastJson;
}

public class ResultsScreenController : MonoBehaviour
{
    public Text resultText;
    public Text titleText;
    public Text scoreText;
    public Text categoryText;
    public Text detailsText;
    public Text sourceText;
    public Button backButton;

    [Serializable] class Result
    {
        public float fatigue_score;
        public string @class;
        public string source;
        public Features features;
    }

    [Serializable] class Features
    {
        public float reaction_time_mean_s;
        public float reaction_time_median_s;
        public float duration_s;
        public int errors;
        public int missed_targets;
        public int late_responses;
        public float fixation_duration_mean_s;
        public float gaze_dispersion_mean;
        public int distractions;
        public int emotiv_samples;
    }

    void Start()
    {
        if (backButton != null) backButton.onClick.AddListener(() => SceneManager.LoadScene("MainMenu"));
        if (string.IsNullOrEmpty(SessionResultStore.LastJson))
        {
            SetEmptyState();
            return;
        }
        var result = JsonUtility.FromJson<Result>(SessionResultStore.LastJson);
        var f = result.features;
        string details = "Длительность: " + f.duration_s.ToString("0.0") + " с" +
            "\nРеакция: " + f.reaction_time_mean_s.ToString("0.00") + " с (медиана " + f.reaction_time_median_s.ToString("0.00") + " с)" +
            "\nОшибки: " + f.errors + "   Пропуски: " + f.missed_targets + "   Поздние ответы: " + f.late_responses +
            "\nФиксация: " + f.fixation_duration_mean_s.ToString("0.00") + " с   Разброс: " + f.gaze_dispersion_mean.ToString("0.000") +
            "\nОтвлечения: " + f.distractions + "   Выборок метрик Emotiv: " + f.emotiv_samples;
        if (titleText != null) titleText.text = "ОТЧЁТ СЕССИИ";
        if (scoreText != null) scoreText.text = result.fatigue_score.ToString("0.000");
        if (categoryText != null) categoryText.text = CategoryLabel(result.@class);
        if (detailsText != null) detailsText.text = details;
        if (sourceText != null) sourceText.text = "ИСТОЧНИК: " + SourceLabel(result.source) + "\nОтчёт: reports/session_<id>.html";
        if (resultText != null) resultText.text = details;
    }

    void SetEmptyState()
    {
        if (titleText != null) titleText.text = "ОТЧЁТ СЕССИИ";
        if (scoreText != null) scoreText.text = "—";
        if (categoryText != null) categoryText.text = "ДАННЫЕ НЕ ПОЛУЧЕНЫ";
        if (detailsText != null) detailsText.text = "Результат сессии ещё не получен.\nПроведите смену и дождитесь анализа.";
        if (sourceText != null) sourceText.text = "ИСТОЧНИК: ОЖИДАНИЕ ДАННЫХ";
        if (resultText != null) resultText.text = "Результат сессии ещё не получен.\nПроведите смену и дождитесь анализа.";
    }

    static string CategoryLabel(string value)
    {
        switch ((value ?? string.Empty).ToLowerInvariant())
        {
            case "low": return "НИЗКАЯ НАГРУЗКА";
            case "medium": return "СРЕДНЯЯ НАГРУЗКА";
            case "high": return "ВЫСОКАЯ НАГРУЗКА";
            default: return (value ?? "НЕ ОПРЕДЕЛЕНО").ToUpperInvariant();
        }
    }

    static string SourceLabel(string value)
    {
        return string.Equals(value, "live", StringComparison.OrdinalIgnoreCase)
            ? "ЖИВОЙ ПОТОК"
            : (value ?? "НЕИЗВЕСТНО").ToUpperInvariant();
    }
}
