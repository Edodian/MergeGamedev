// PistolEquipToggle.cs — uses InputActionProperty so you can select Player/Holster directly
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class PistolEquipToggle : MonoBehaviour
{
    [Header("Input System")]
    [Tooltip("From your Input Actions asset: Player → Holster (H)")]
    [SerializeField] private InputActionProperty holsterAction;   // <- pick action here

    [Header("Inventory")]
    [SerializeField] private InventoryGridData grid;              // drag your grid asset (or auto-found)
    [SerializeField] private string pistolItemId = "glock18";     // change to your pistol's item id

    [Header("Viewmodel (in-hand gun)")]
    [SerializeField] private GameObject handgunView;              // the in-hand pistol GO to show/hide

    bool equipped;
    bool pendingAddBack;

    void Awake()
    {
        if (!grid)
            grid = Object.FindFirstObjectByType<InventoryGridData>(FindObjectsInactive.Exclude);

        if (!handgunView)
        {
            var anim = GetComponentInChildren<Animator>(true);
            if (anim) handgunView = anim.gameObject;
        }

        SetEquipped(false, force: true);
    }

    void OnEnable()
    {
        var a = holsterAction.action;
        if (a == null)
        {
            Debug.LogError("PistolEquipToggle: Assign Player/Holster to 'holsterAction'.");
            return;
        }

        a.Enable();
        a.performed += OnHolsterPressed;
    }

    void OnDisable()
    {
        var a = holsterAction.action;
        if (a != null)
        {
            a.performed -= OnHolsterPressed;
            a.Disable();
        }
    }

    private void OnHolsterPressed(InputAction.CallbackContext _)
    {
        if (!equipped) TryEquip();
        else TryHolster();
    }

    // -------- equip / holster --------
    void TryEquip()
    {
        if (!grid) { Debug.LogWarning("No InventoryGridData."); return; }

        int index = FindFirstIndexOf(grid, pistolItemId);
        if (index < 0) { Debug.LogWarning($"No '{pistolItemId}' in inventory."); return; }

        if (!grid.RemoveAt(index, 1)) { Debug.LogWarning("RemoveAt failed."); return; }

        SetEquipped(true);
        pendingAddBack = true;
        RefreshInventoryUI();
    }

    void TryHolster()
    {
        if (!grid) { Debug.LogWarning("No InventoryGridData."); return; }

        if (TryAddAnywhere(grid, pistolItemId, 1))
        {
            SetEquipped(false);
            pendingAddBack = false;
            RefreshInventoryUI();
        }
        else
        {
            Debug.LogWarning("No room in inventory to holster pistol.");
        }
    }

    void SetEquipped(bool v, bool force = false)
    {
        if (!force && equipped == v) return;
        equipped = v;
        if (handgunView) handgunView.SetActive(v);
    }

    void OnDestroy()
    {
        if (pendingAddBack && grid)
            if (TryAddAnywhere(grid, pistolItemId, 1)) RefreshInventoryUI();
    }

    // -------- helpers --------
    static int FindFirstIndexOf(InventoryGridData grid, string itemId)
    {
        if (grid == null || grid.items == null) return -1;
        for (int i = 0; i < grid.items.Count; i++)
        {
            var e = grid.items[i];
            if (e != null && e.itemId == itemId && e.amount > 0) return i;
        }
        return -1;
    }

    static bool TryAddAnywhere(InventoryGridData grid, string itemId, int amount)
    {
        if (amount <= 0) return true;
        if (!ItemDatabase.Instance || !ItemDatabase.Instance.TryGet(itemId, out var def)) return false;

        for (int attempt = 0; attempt < 2; attempt++)
        {
            bool rotated = attempt == 1;
            var sz = grid.GetSize(def, rotated);

            for (int y = 0; y <= grid.cellsHigh - sz.h; y++)
                for (int x = 0; x <= grid.cellsWide - sz.w; x++)
                {
                    if (!grid.CanPlace(itemId, x, y, rotated, -1)) continue;
                    if (InsertEntryReflection(grid, itemId, amount, x, y, rotated)) return true;
                }
        }
        return false;
    }

    static bool InsertEntryReflection(InventoryGridData grid, string itemId, int amount, int x, int y, bool rotated)
    {
        var list = grid.items as System.Collections.IList;
        if (list == null) return false;

        var elemType = list.GetType().GetGenericArguments()[0];
        var entry = System.Activator.CreateInstance(elemType);

        var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        elemType.GetField("itemId", flags)?.SetValue(entry, itemId);
        elemType.GetField("amount", flags)?.SetValue(entry, amount);
        elemType.GetField("x", flags)?.SetValue(entry, x);
        elemType.GetField("y", flags)?.SetValue(entry, y);
        elemType.GetField("rotated", flags)?.SetValue(entry, rotated);

        list.Add(entry);
        return true;
    }

    static void RefreshInventoryUI()
    {
        var ui = Object.FindFirstObjectByType<InventoryGridUI>(FindObjectsInactive.Exclude);
        if (ui) ui.Refresh();
    }
}
