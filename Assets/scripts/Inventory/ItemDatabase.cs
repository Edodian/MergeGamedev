using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class ItemDatabase : MonoBehaviour
{
    public static ItemDatabase Instance { get; private set; }

    [SerializeField] private string itemLabel = "ItemDefinitions";

    private readonly Dictionary<string, ItemDefinition> byId = new();
    private AsyncOperationHandle<IList<ItemDefinition>> loadHandle;
    public bool IsLoaded { get; private set; }
    public event System.Action OnLoaded;

    private void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (string.IsNullOrWhiteSpace(itemLabel))
        {
            Debug.LogError("ItemDatabase: itemLabel is empty. Set it to 'ItemDefinitions'.");
            return;
        }

        // Optional pre-check: helps diagnose missing labels before load
        var locs = Addressables.LoadResourceLocationsAsync(itemLabel);
        locs.Completed += _ =>
        {
            if (locs.Status != AsyncOperationStatus.Succeeded || locs.Result == null || locs.Result.Count == 0)
            {
                Debug.LogError("ItemDatabase: no Addressables found for label '" + itemLabel +
                               "'. Make sure your ItemDefinition assets are Addressable and have this label.");
            }
        };

        // Load every ItemDefinition with that single label
        loadHandle = Addressables.LoadAssetsAsync<ItemDefinition>(itemLabel, null);
        loadHandle.Completed += OnItemsLoaded;
    }

    private void OnDestroy()
    {
        if (loadHandle.IsValid()) Addressables.Release(loadHandle);
        if (Instance == this) Instance = null;
    }

    private void OnItemsLoaded(AsyncOperationHandle<IList<ItemDefinition>> op)
    {
        if (op.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError("ItemDatabase: Addressables load failed. Check labels on your ItemDefinition assets.");
            return;
        }

        byId.Clear();
        foreach (var def in op.Result)
        {
            if (def == null || string.IsNullOrEmpty(def.Id)) continue;
            if (!byId.ContainsKey(def.Id)) byId.Add(def.Id, def);
        }

        if (byId.Count == 0)
            Debug.LogWarning("ItemDatabase: 0 items loaded. Is the 'ItemDefinitions' label on the ItemDefinition asset (not just the prefab)?");

        IsLoaded = true;
        OnLoaded?.Invoke();
        Debug.Log($"ItemDatabase: loaded {byId.Count} items.");
    }

    public bool TryGet(string id, out ItemDefinition def) => byId.TryGetValue(id, out def);
    public IEnumerable<ItemDefinition> AllItems() => byId.Values;
}
