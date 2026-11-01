using UnityEngine;

public class AlarmBeaconBlink : MonoBehaviour
{
    public Color onColor = new Color(1f, .015f, .005f);
    public Color offColor = new Color(.08f, .005f, .002f);
    public float blinkSpeed = 3.6f;
    public float lightRange = 4.5f;

    Renderer beaconRenderer;
    Material beaconMaterial;
    Light beaconLight;
    float phase;

    void Awake()
    {
        beaconRenderer = GetComponent<Renderer>();
        beaconMaterial = beaconRenderer == null ? null : beaconRenderer.material;
        phase = Mathf.Abs(transform.position.x) * .35f;

        var lightObject = new GameObject("Мигающий красный сигнал");
        lightObject.transform.SetParent(transform, false);
        beaconLight = lightObject.AddComponent<Light>();
        beaconLight.type = LightType.Point;
        beaconLight.color = onColor;
        beaconLight.range = lightRange;
    }

    void Update()
    {
        // Короткая фаза включения и затемнённая фаза создают аварийное мигание,
        // а не мягкое декоративное пульсирование.
        float cycle = (Time.unscaledTime * blinkSpeed + phase) % 1f;
        float intensity = cycle < .42f ? 1f : .04f;
        if (beaconMaterial != null)
            beaconMaterial.color = Color.Lerp(offColor, onColor, intensity);
        if (beaconLight != null)
            beaconLight.intensity = intensity * 4.5f;
    }
}
