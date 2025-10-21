using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class SceneReference : ISerializationCallbackReceiver
{
#if UNITY_EDITOR
    [SerializeField] 
    private SceneAsset SceneAsset;
#endif

    [SerializeField, HideInInspector]
    private string sceneName;

    // ✅ Доступ к имени сцены в любой сборке
    public string SceneName
    {
        get
        {
#if UNITY_EDITOR
            return SceneAsset != null ? SceneAsset.name : sceneName;
#else
            return sceneName;
#endif
        }
    }

#if UNITY_EDITOR
    public void OnBeforeSerialize()
    {
        if (SceneAsset != null)
            sceneName = SceneAsset.name;
    }

    public void OnAfterDeserialize() { }
#else
    public void OnBeforeSerialize() { }
    public void OnAfterDeserialize() { }
#endif
}
