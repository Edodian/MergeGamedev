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

        if (PlayerPrefs.HasKey(saveKey))
        {
            grid.FromJson(PlayerPrefs.GetString(saveKey));
            ui?.Refresh();
        }

        grid.Changed += SaveNow;
    }

    void OnDestroy() { if (grid) grid.Changed -= SaveNow; }

    void SaveNow()
    {
        PlayerPrefs.SetString(saveKey, grid.ToJson());
        PlayerPrefs.Save();
        ui?.Refresh();
    }
}
