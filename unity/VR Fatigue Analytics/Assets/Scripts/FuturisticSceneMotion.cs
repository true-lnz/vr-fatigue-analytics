using UnityEngine;

public class FuturisticSceneMotion : MonoBehaviour
{
    public enum MotionKind { Pulse, Float, Rotate, Bar }
    public MotionKind motionKind = MotionKind.Pulse;
    public float speed = 2f;
    public float amount = .08f;
    public Color colorA = new Color(.05f, .35f, .45f);
    public Color colorB = new Color(.15f, .9f, 1f);
    Vector3 initialPosition;
    Vector3 initialScale;
    Renderer targetRenderer;
    Material runtimeMaterial;
    float seed;

    void Awake()
    {
        initialPosition = transform.localPosition;
        initialScale = transform.localScale;
        targetRenderer = GetComponent<Renderer>();
        if (targetRenderer != null)
        {
            runtimeMaterial = targetRenderer.material;
            colorA = runtimeMaterial.color;
        }
        seed = Mathf.Abs(transform.position.x * 1.37f + transform.position.y * 2.11f + transform.position.z * .73f);
    }

    void Update()
    {
        float wave = .5f + .5f * Mathf.Sin(Time.unscaledTime * speed + seed);
        if (motionKind == MotionKind.Pulse)
        {
            transform.localScale = initialScale * (1f + amount * wave);
            if (runtimeMaterial != null) runtimeMaterial.color = Color.Lerp(colorA, colorB, wave);
        }
        else if (motionKind == MotionKind.Float)
            transform.localPosition = initialPosition + Vector3.up * (wave - .5f) * amount;
        else if (motionKind == MotionKind.Rotate)
            transform.Rotate(Vector3.up, speed * 25f * Time.unscaledDeltaTime, Space.Self);
        else
        {
            transform.localScale = new Vector3(initialScale.x, initialScale.y * (.35f + .65f * wave), initialScale.z);
            transform.localPosition = initialPosition + Vector3.up * (transform.localScale.y - initialScale.y) * .5f;
            if (runtimeMaterial != null) runtimeMaterial.color = Color.Lerp(colorA, colorB, wave);
        }
    }
}
