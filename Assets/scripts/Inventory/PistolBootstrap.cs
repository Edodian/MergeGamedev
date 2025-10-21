// PistolBootstrap.cs
using UnityEngine;

public class PistolBootstrap : MonoBehaviour
{
    public InventoryGridData grid;
    public string pistolId = "item_id"; // <- your ItemDefinition Id

    void Start()
    {
        if (!grid) grid = FindFirstObjectByType<InventoryGridData>(FindObjectsInactive.Exclude);
        if (!grid) { Debug.LogError("No InventoryGridData in scene"); return; }

        int leftover = grid.AddAuto(pistolId, 1, true);
        FindFirstObjectByType<InventoryGridUI>(FindObjectsInactive.Exclude)?.Refresh();
        if (leftover > 0) Debug.LogWarning("No space for pistol.");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (grid.AddAuto(pistolId, 1, true) == 0)
                FindFirstObjectByType<InventoryGridUI>(FindObjectsInactive.Exclude);
        }
    }
}
