using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UiMicroAnimations : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public float hoverScale = 1.035f;
    public float speed = 8f;
    public bool pulse;
    public float pulseAmount = .08f;
    public float pulseSpeed = 2.2f;

    RectTransform rect;
    Vector3 baseScale;
    Vector3 targetScale;
    Graphic graphic;
    Color baseColor;
    float phase;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        baseScale = rect == null ? transform.localScale : rect.localScale;
        targetScale = baseScale;
        graphic = GetComponent<Graphic>();
        if (graphic != null) baseColor = graphic.color;
        phase = Mathf.Abs(transform.GetSiblingIndex()) * .7f;
    }

    void Update()
    {
        targetScale = Vector3.Lerp(targetScale, baseScale, Time.unscaledDeltaTime * speed);
        if (pulse)
        {
            float wave = .5f + .5f * Mathf.Sin(Time.unscaledTime * pulseSpeed + phase);
            targetScale = baseScale * (1f + pulseAmount * wave);
            if (graphic != null) graphic.color = Color.Lerp(baseColor, Color.white, wave * .08f);
        }
        if (rect != null) rect.localScale = Vector3.Lerp(rect.localScale, targetScale, Time.unscaledDeltaTime * speed);
    }

    public void OnPointerEnter(PointerEventData eventData) => targetScale = baseScale * hoverScale;
    public void OnPointerExit(PointerEventData eventData) => targetScale = baseScale;
    public void OnPointerDown(PointerEventData eventData) => targetScale = baseScale * .965f;
    public void OnPointerUp(PointerEventData eventData) => targetScale = baseScale * hoverScale;
}
