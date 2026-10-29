using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneNavigator : MonoBehaviour
{
    public Button button;
    public string sceneName;

    void Start()
    {
        if (button != null) button.onClick.AddListener(() => SceneManager.LoadScene(sceneName));
    }
}
