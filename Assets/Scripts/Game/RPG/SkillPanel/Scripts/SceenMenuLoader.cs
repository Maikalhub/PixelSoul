using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneMenuLoader : MonoBehaviour
{
    [Header("Название сцены для загрузки")]
    [SerializeField] private string sceneName;

    public void LoadScene()
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("SceneMenuLoader: название сцены не указано.");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }
}