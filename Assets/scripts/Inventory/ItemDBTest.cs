using UnityEngine;
public class ItemDBTest : MonoBehaviour
{
    void Start()
    {
        var db = ItemDatabase.Instance; if (!db) { Debug.LogError("No ItemDatabase in scene"); return; }
        if (db.IsLoaded) Log(); else db.OnLoaded += Log;
    }
    void Log()
    {
        foreach (var d in ItemDatabase.Instance.AllItems())
            Debug.Log($"Loaded: {d.displayName} ({d.Id})");
    }
}
