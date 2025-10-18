// PistolBootstrap.cs
using UnityEngine;

public class PistolBootstrap : MonoBehaviour
{
    public InventoryGridData grid;
    public string pistolId = "item_id"; // <- your ItemDefinition Id

    void Start()
    {
        if (!grid) grid = FindObjectOfType<InventoryGridData>();
        if (!grid) { Debug.LogError("No InventoryGridData in scene"); return; }

        int leftover = grid.AddAuto(pistolId, 1, true);
        FindObjectOfType<InventoryGridUI>()?.Refresh();
        if (leftover > 0) Debug.LogWarning("No space for pistol.");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (grid.AddAuto(pistolId, 1, true) == 0)
                FindObjectOfType<InventoryGridUI>()?.Refresh();
        }
    }
}
