using UnityEngine;

public class HolographicMapAnimation : MonoBehaviour
{
    public float floatAmount = .035f;
    public float floatSpeed = .7f;
    public float scanWidth = 0f;
    public float scanSpeed = .45f;
    Vector3 startPosition;

    void Awake()
    {
        startPosition = transform.localPosition;
    }

    void Update()
    {
        float wave = Mathf.Sin(Time.unscaledTime * floatSpeed) * floatAmount;
        float scan = scanWidth <= 0f ? 0f : Mathf.Sin(Time.unscaledTime * scanSpeed) * scanWidth;
        transform.localPosition = startPosition + Vector3.up * wave + Vector3.right * scan;
    }
}
