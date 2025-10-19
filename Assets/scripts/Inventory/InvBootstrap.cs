using UnityEngine;

public class InvBootstrap : MonoBehaviour
{
    public InventoryGridData grid;
    [Tooltip("IDs to auto-add on start (for quick testing).")]
    public string[] itemIds = { "pistol", "ammo9mm", "knife" };

    void Start()
    {
        if (!grid) grid = FindObjectOfType<InventoryGridData>();
        if (!grid) return;

        foreach (var id in itemIds)
        {
            if (string.IsNullOrWhiteSpace(id)) continue;
            grid.AddAuto(id, 1, allowRotate: true);
        }

        var ui = FindObjectOfType<InventoryGridUI>();
        ui?.Refresh();
    }
}
