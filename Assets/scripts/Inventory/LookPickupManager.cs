using UnityEngine;

public class LookPickupManager : MonoBehaviour
{
    [Header("Refs")]
    public Camera cam;                 // drag your camera (or auto)
    public InventoryGridData grid;     // drag InventoryGridData (Inventory GO)
    public InventoryGridUI ui;         // drag InventoryGridUI (for Refresh)

    [Header("Targeting")]
    public float maxDistance = 3.0f;
    public float maxAngleDeg = 8f;
    public bool drawPrompt = true;

    WorldPickup target; string targetName = "";

    void Awake()
    {
        if (!cam) cam = Camera.main;
        if (!grid) grid = FindObjectOfType<InventoryGridData>();
        if (!ui) ui = FindObjectOfType<InventoryGridUI>();
    }

    void Update()
    {
        if (!cam || !grid) return;

        target = null; targetName = "";
        float bestScore = -1f;

        var cpos = cam.transform.position;
        var fwd = cam.transform.forward;

        foreach (var p in WorldPickup.All)
        {
            if (!p) continue;
            var pivot = p.pivot ? p.pivot : p.transform;

            Vector3 to = pivot.position - cpos;
            float dist = to.magnitude;
            if (dist < 0.0001f || dist > maxDistance) continue;

            Vector3 dir = to / dist;
            float dot = Vector3.Dot(fwd, dir);
            float angle = Mathf.Acos(Mathf.Clamp(dot, -1f, 1f)) * Mathf.Rad2Deg;
            if (angle > maxAngleDeg) continue;

            float score = dot / (1f + dist * 0.25f);
            if (score > bestScore) { bestScore = score; target = p; }
        }

        if (target) targetName = target.DisplayName;

        if (target && InputCompat.GetKeyDown(KeyCode.E))
            TryPickup(target);
    }

    void TryPickup(WorldPickup p)
    {
        int want = Mathf.Max(1, p.amount);

        if (!ItemDatabase.Instance || !ItemDatabase.Instance.TryGet(p.itemId, out var def))
        {
            Debug.LogWarning($"Unknown itemId '{p.itemId}' on pickup {p.name}.");
            return;
        }

        int leftover = grid.AddAuto(p.itemId, want, allowRotate: true);
        int added = want - leftover;

        if (added <= 0) { Debug.Log("No space / overweight."); return; }

        p.amount = leftover;
        if (p.amount <= 0) Destroy(p.gameObject);

        ui?.Refresh();
        Debug.Log($"Picked up {added} × {def.displayName ?? p.itemId}");
    }

    void OnGUI()
    {
        if (!drawPrompt || !target) return;
        const int w = 320, h = 22;
        var rect = new Rect((Screen.width - w) / 2f, (Screen.height) / 2f + 36f, w, h);
        GUI.Label(rect, $"[E] Pick up {targetName}");
    }
}
