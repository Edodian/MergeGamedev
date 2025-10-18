using UnityEngine;

public class InventorySeeder : MonoBehaviour
{
    public InventoryGridData grid;          // drag your Inventory here
    public string pistolId = "item_id";     // EXACT match with ItemDefinition.Id
    public int count = 1;

    void Start()
    {
        if (!grid) grid = FindObjectOfType<InventoryGridData>();
        if (!grid) { Debug.LogError("No InventoryGridData in scene"); return; }

        // 0) DB / Id sanity
        if (ItemDatabase.Instance == null)
        {
            Debug.LogError("No ItemDatabase in scene (or not initialized).");
            return;
        }
        if (!ItemDatabase.Instance.TryGet(pistolId, out var def))
        {
            Debug.LogError($"Item id '{pistolId}' not found in ItemDatabase. Check ItemDefinition.Id.");
            return;
        }

        // 1) Will it fit the grid at all?
        var w = def.gridWidth; var h = def.gridHeight;
        if (w > grid.cellsWide || h > grid.cellsHigh)
        {
            Debug.LogError($"Item '{pistolId}' ({w}x{h}) larger than grid ({grid.cellsWide}x{grid.cellsHigh}).");
            return;
        }

        // 2) Overweight pre-check
        if (grid.maxWeightKg > 0 && grid.TotalWeight() + def.weightKg > grid.maxWeightKg)
        {
            Debug.LogWarning($"Overweight: {grid.TotalWeight():0.##} + {def.weightKg:0.##} > {grid.maxWeightKg:0.##}. Raise Max Weight or lower item weight.");
            return;
        }

        // 3) Try to place
        int leftover = grid.AddAuto(pistolId, count, allowRotate: true);

        // 4) Report precise cause if failed
        if (leftover > 0)
        {
            bool overweight = (grid.maxWeightKg > 0) && (grid.TotalWeight() + def.weightKg > grid.maxWeightKg);
            if (overweight)
                Debug.LogWarning("Couldn’t add: overweight. Increase InventoryGridData.maxWeightKg or reduce item weight.");
            else
                Debug.LogWarning("Couldn’t add: no free slot for this item size/orientation.");
        }

        // 5) Refresh UI
        var ui = FindObjectOfType<InventoryGridUI>();
        ui?.Refresh();
    }
}
