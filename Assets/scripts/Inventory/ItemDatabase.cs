using System.Collections.Generic;
using UnityEngine;

/// Simple runtime DB for ItemDefinition.
/// Loading strategy:
/// 1) If 'items' list is set in the Inspector, uses that.
/// 2) Else loads all ItemDefinition from Resources/Items (put your assets there).
public class ItemDatabase : MonoBehaviour
{
    public static ItemDatabase Instance { get; private set; }

    [Tooltip("Optional: assign your ItemDefinition assets here. If empty, we'll try Resources/Items.")]
    public List<ItemDefinition> items = new List<ItemDefinition>();

    readonly Dictionary<string, ItemDefinition> _byId = new();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (items == null || items.Count == 0)
        {
            var loaded = Resources.LoadAll<ItemDefinition>("Items");
            items = new List<ItemDefinition>(loaded);
        }

        _byId.Clear();
        foreach (var it in items)
        {
            if (it == null || string.IsNullOrWhiteSpace(it.Id)) continue;
            if (!_byId.ContainsKey(it.Id)) _byId.Add(it.Id, it);
        }
    }

    public bool TryGet(string id, out ItemDefinition def) => _byId.TryGetValue(id, out def);
    public IEnumerable<ItemDefinition> AllItems() => items;
}
