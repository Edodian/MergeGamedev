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
    public string SceneName => sceneName;
#endif
    [SerializeField, HideInInspector]
    private string sceneName;

#if UNITY_EDITOR
    public void OnBeforeSerialize()
    {
        if (SceneAsset != null)
            sceneName = SceneAsset.name;
    }

public void OnAfterDeserialize() { }
#endif
}
