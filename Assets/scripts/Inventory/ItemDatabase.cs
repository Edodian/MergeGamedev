using System.Collections.Generic;
using UnityEngine;

public class ItemDatabase : MonoBehaviour
{
    public static ItemDatabase Instance { get; private set; }

    [Tooltip("Drag ItemDefinition assets here (Glock18, etc).")]
    public List<ItemDefinition> items = new();

    void Awake() => Instance = this;

    public bool TryGet(string id, out ItemDefinition def)
    {
        def = items.Find(x => x && x.Id == id);
        return def != null;
    }
}
