using UnityEngine;

public class ItemDBTest : MonoBehaviour
{
    void Start()
    {
        var db = ItemDatabase.Instance;
        if (!db) { Debug.LogError("No ItemDatabase in scene"); return; }

        // If your DB is synchronous (Resources / list in Inspector), this is enough:
        Log();

        // If later you switch to an async Addressables DB that has IsLoaded/OnLoaded,
        // you can revert to:
        // if (db.IsLoaded) Log(); else db.OnLoaded += Log;
    }

    void Log()
    {
        foreach (var d in ItemDatabase.Instance.AllItems())
            Debug.Log($"Loaded: {d.displayName} ({d.Id})");
    }
}
