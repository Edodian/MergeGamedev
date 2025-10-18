using UnityEngine;

public class InventoryPersistence : MonoBehaviour
{
    public InventoryGridData grid;
    public InventoryGridUI ui;
    public string saveKey = "inv_main";

    void Awake()
    {
        if (!grid) grid = FindObjectOfType<InventoryGridData>();
        if (!ui) ui = FindObjectOfType<InventoryGridUI>();

        // Load saved state, if any
        if (PlayerPrefs.HasKey(saveKey))
        {
            grid.FromJson(PlayerPrefs.GetString(saveKey));
            ui?.Refresh();
        }

        // Autosave on any change
        grid.Changed += SaveNow;
    }

    void OnDestroy()
    {
        if (grid) grid.Changed -= SaveNow;
    }

    void SaveNow()
    {
        PlayerPrefs.SetString(saveKey, grid.ToJson());
        PlayerPrefs.Save();
        ui?.Refresh(); // keep header weight in sync
    }
}
