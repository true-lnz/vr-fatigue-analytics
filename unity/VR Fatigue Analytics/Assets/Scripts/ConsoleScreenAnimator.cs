using UnityEngine;

public class ConsoleScreenAnimator : MonoBehaviour
{
    public string districtId;
    public float idleSpeed = 1.3f;
    public Color normalColor = new Color(.05f, .45f, .62f);
    public Color alertColor = new Color(1f, .12f, .05f);

    Renderer screenRenderer;
    Renderer[] bars;
    Material screenMaterial;
    Material[] barMaterials;
    bool alert;
    float[] barSeeds;

    void Awake()
    {
        screenRenderer = GetComponent<Renderer>();
        screenMaterial = screenRenderer == null ? null : screenRenderer.material;
        bars = GetComponentsInChildren<Renderer>();
        barMaterials = new Material[bars.Length];
        barSeeds = new float[bars.Length];
        for (int i = 0; i < bars.Length; i++)
        {
            barMaterials[i] = bars[i].material;
            barSeeds[i] = i * .77f;
        }
    }

    void Update()
    {
        float wave = .5f + .5f * Mathf.Sin(Time.unscaledTime * idleSpeed);
        Color accent = alert ? alertColor : normalColor;
        if (screenMaterial != null)
            screenMaterial.color = Color.Lerp(accent * .72f, accent, wave * .35f);
        for (int i = 1; i < barMaterials.Length; i++)
        {
            float barWave = .35f + .65f * (.5f + .5f * Mathf.Sin(Time.unscaledTime * (idleSpeed + i * .09f) + barSeeds[i]));
            barMaterials[i].color = Color.Lerp(alert ? alertColor * .55f : normalColor * .5f, accent, barWave);
        }
    }

    public void SetAlert(bool value)
    {
        alert = value;
    }
}
