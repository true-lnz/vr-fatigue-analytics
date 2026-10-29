using UnityEngine;
using UnityEngine.InputSystem;

public class ConsoleControlButton : MonoBehaviour
{
    public string targetId;
    public OperatorTestController controller;
    public Renderer buttonRenderer;
    public Renderer outlineRenderer;
    public Color idleColor = new Color(.055f, .2f, .25f);
    public Color selectedColor = new Color(.12f, .9f, 1f);
    public Color alarmColor = new Color(1f, .25f, .08f);
    public Color outlineColor = new Color(.15f, .95f, 1f);
    public float pressDepth = .055f;
    Vector3 restPosition;
    Vector3 targetPosition;
    bool selected;
    bool alarm;
    float pressTimer;

    void Awake()
    {
        restPosition = transform.localPosition;
        targetPosition = restPosition;
        if (buttonRenderer == null) buttonRenderer = GetComponent<Renderer>();
        ApplyVisual();
    }

    void Update()
    {
        if (pressTimer > 0f)
        {
            pressTimer -= Time.unscaledDeltaTime;
            targetPosition = restPosition + Vector3.forward * pressDepth;
        }
        else targetPosition = restPosition;
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, 18f * Time.unscaledDeltaTime);
        if (outlineRenderer != null && selected)
        {
            float pulse = .65f + .35f * Mathf.Sin(Time.unscaledTime * 6f);
            outlineRenderer.material.color = outlineColor * pulse;
        }
    }

    void OnMouseEnter()
    {
        if (!selected && buttonRenderer != null) buttonRenderer.material.color = Color.Lerp(idleColor, selectedColor, .4f);
    }

    void OnMouseExit() { ApplyVisual(); }

    void OnMouseDown() { Press(); }

    public void Press()
    {
        pressTimer = .13f;
        if (controller != null) controller.SelectPhysicalControl(targetId);
    }

    public void SetState(bool isSelected, bool isAlarm)
    {
        selected = isSelected;
        alarm = isAlarm;
        if (outlineRenderer != null) outlineRenderer.gameObject.SetActive(selected);
        ApplyVisual();
    }

    void ApplyVisual()
    {
        if (buttonRenderer != null) buttonRenderer.material.color = selected ? selectedColor : alarm ? alarmColor : idleColor;
    }
}
