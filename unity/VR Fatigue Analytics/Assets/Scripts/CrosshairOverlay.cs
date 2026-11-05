using UnityEngine;
using UnityEngine.UI;

public class CrosshairOverlay : MonoBehaviour
{
    public Color color = new Color(.82f, .98f, 1f, .92f);
    public float armLength = 9f;
    public float thickness = 2f;
    public float gap = 4f;

    Canvas canvas;
    GameObject root;
    MouseLookController look;

    void Start()
    {
        look = Camera.main == null ? null : Camera.main.GetComponent<MouseLookController>();
        canvas = new GameObject("Прицел", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        root = new GameObject("Крестик", typeof(RectTransform));
        root.transform.SetParent(canvas.transform, false);
        var rect = root.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(.5f, .5f);
        rect.anchorMax = new Vector2(.5f, .5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;

        CreateArm("Верх", new Vector2(0f, gap + armLength * .5f), new Vector2(thickness, armLength));
        CreateArm("Низ", new Vector2(0f, -(gap + armLength * .5f)), new Vector2(thickness, armLength));
        CreateArm("Лево", new Vector2(-(gap + armLength * .5f), 0f), new Vector2(armLength, thickness));
        CreateArm("Право", new Vector2(gap + armLength * .5f, 0f), new Vector2(armLength, thickness));
    }

    void Update()
    {
        if (root != null && look != null)
            root.SetActive(!look.MapMode);
    }

    void CreateArm(string name, Vector2 position, Vector2 size)
    {
        var arm = new GameObject(name, typeof(RectTransform), typeof(Image));
        arm.transform.SetParent(root.transform, false);
        var rect = arm.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        arm.GetComponent<Image>().color = color;
    }

    void OnDestroy()
    {
        if (canvas != null) Destroy(canvas.gameObject);
    }
}
