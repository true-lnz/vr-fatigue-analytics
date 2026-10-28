using UnityEngine;

public class DistrictAlarmVisual : MonoBehaviour
{
    public string districtId;
    public Color alertColor = new Color(1f, .08f, .025f);
    public Color idleColor = new Color(.08f, .42f, .52f);
    public float pulseSpeed = 4f;
    Renderer[] renderers;
    Material[] materials;
    Light warningLight;
    bool alert;

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        materials = new Material[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) materials[i] = renderers[i].material;
        var lightObject = new GameObject("Аварийная подсветка района");
        lightObject.transform.SetParent(transform, false);
        lightObject.transform.localPosition = new Vector3(0f, 2.1f, -.3f);
        warningLight = lightObject.AddComponent<Light>();
        warningLight.type = LightType.Point;
        warningLight.range = 3.8f;
        warningLight.intensity = 0f;
        warningLight.color = alertColor;
    }

    void Update()
    {
        float wave = .55f + .45f * Mathf.Sin(Time.unscaledTime * pulseSpeed);
        Color target = alert ? Color.Lerp(alertColor * .55f, alertColor, wave) : idleColor * .45f;
        for (int i = 0; i < materials.Length; i++) materials[i].color = Color.Lerp(materials[i].color, target, Time.unscaledDeltaTime * 5f);
        if (warningLight != null) warningLight.intensity = alert ? .9f + wave * 1.8f : 0f;
    }

    public void SetAlert(bool value)
    {
        alert = value;
    }
}
