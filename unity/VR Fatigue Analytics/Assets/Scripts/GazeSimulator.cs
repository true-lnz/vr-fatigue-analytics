using UnityEngine;
using UnityEngine.InputSystem;

public class GazeSimulator : MonoBehaviour
{
    public Camera sourceCamera;
    public float sampleInterval = 1f;
    public string CurrentTarget { get; private set; } = "panel";
    public float LastDispersion { get; private set; }
    public float FixationDuration { get; private set; }
    public Vector2 NormalizedPosition { get; private set; } = new Vector2(.5f, .5f);
    float nextSample;
    Vector2 previousPosition;
    string previousTarget;

    void Update()
    {
        if (Time.time < nextSample) return;
        nextSample = Time.time + sampleInterval;
        Vector2 position = Mouse.current == null ? new Vector2(Screen.width / 2f, Screen.height / 2f) : Mouse.current.position.ReadValue();
        NormalizedPosition = new Vector2(
            Mathf.Clamp01(position.x / Mathf.Max(1, Screen.width)),
            Mathf.Clamp01(position.y / Mathf.Max(1, Screen.height)));
        CurrentTarget = "background";
        if (sourceCamera != null)
        {
            var ray = sourceCamera.ScreenPointToRay(position);
            if (Physics.Raycast(ray, out var hit, 100f)) CurrentTarget = hit.collider.gameObject.name;
        }
        float movement = Vector2.Distance(position, previousPosition) / Mathf.Max(Screen.width, Screen.height);
        LastDispersion = Mathf.Clamp(movement, 0f, 1f);
        FixationDuration = CurrentTarget == previousTarget && movement < .025f ? FixationDuration + sampleInterval : sampleInterval;
        previousPosition = position;
        previousTarget = CurrentTarget;
    }
}
