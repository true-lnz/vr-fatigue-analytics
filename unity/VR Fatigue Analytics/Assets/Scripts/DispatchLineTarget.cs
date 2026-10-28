using UnityEngine;

public class DispatchLineTarget : MonoBehaviour
{
    public string targetId;
    public OperatorTestController controller;

    void OnMouseDown()
    {
        if (controller != null && controller.IsMapMode)
            controller.SelectMapLine(targetId);
    }
}
