using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
[RequireComponent(typeof(UIDocument))]
public class InventoryGridUI : MonoBehaviour
{
    // ------------------ DATA ------------------
    public InventoryGridData grid;

    [Header("Visuals")]
    public int cellSize = 64;
    public int cellGap = 2;
    public Color cellColor = new Color(0.09f, 0.09f, 0.10f, 1f);
    public Color lineColor = new Color(0.17f, 0.19f, 0.21f, 1f);
    public Color ghostOk = new Color(0.25f, 0.90f, 0.45f, 0.25f);
    public Color ghostBad = new Color(1f, 0.25f, 0.25f, 0.25f);

    [Header("Drop / Throw (use REAL render Camera)")]
    [SerializeField, Tooltip("Assign the MainCamera (GameObject that has a Camera component + CinemachineBrain).")]
    private Transform dropOrigin;              // camera's Transform (NOT the Cinemachine virtual camera)
    public float dropDistance = 1.2f;          // meters forward from camera
    public float throwForce = 6f;            // forward impulse
    public float upwardBoost = 1.5f;          // upward impulse
    public float groundSnap = 0.05f;         // small lift to avoid clipping

    // ------------------ UI Toolkit refs ------------------
    UIDocument doc;
    VisualElement root, gridVE, ghost;
    Label weightLabel;

    readonly List<VisualElement> _tiles = new();
    int step => cellSize + cellGap;

    // ------------------ drag state ------------------
    bool dragging;
    int draggedIndex = -1;
    bool ghostRotated;
    int hoverIndex = -1;

    bool _warnedDropOrigin;

    // ------------------ LIFECYCLE ------------------
    void OnEnable()
    {
        doc = GetComponent<UIDocument>();

        if (!grid)
            grid = Object.FindFirstObjectByType<InventoryGridData>(FindObjectsInactive.Exclude);

        if (grid)
            grid.Changed += OnGridChanged;

        EnsureDropOrigin(); // Unity 6: auto-wire a real Camera

        root = doc ? doc.rootVisualElement : null;
        if (root == null) return;

        if (root.panel != null) SetupUI();
        else root.RegisterCallback<AttachToPanelEvent>(_ => SetupUI());
    }

    void OnDisable()
    {
        if (grid) grid.Changed -= OnGridChanged;

        dragging = false;
        draggedIndex = -1;
        hoverIndex = -1;

        ghost = null; gridVE = null; weightLabel = null;
    }

    // ------------------ CAMERA AUTO-WIRE ------------------
    void EnsureDropOrigin()
    {
        if (dropOrigin) return;

        // Prefer tagged MainCamera
        var mainCam = Camera.main;
        if (mainCam) { dropOrigin = mainCam.transform; return; }

        // Otherwise first active Camera in scene (Unity 6 API)
        var cam = Object.FindFirstObjectByType<Camera>(FindObjectsInactive.Exclude);
        if (cam) { dropOrigin = cam.transform; return; }

        if (!_warnedDropOrigin)
        {
            _warnedDropOrigin = true;
            Debug.LogWarning("InventoryGridUI: dropOrigin not assigned. " +
                             "Assign your MainCamera (with Camera + CinemachineBrain).");
        }
    }

    // ------------------ UI SETUP ------------------
    void OnGridChanged() => Refresh();

    void SetupUI()
    {
        gridVE = root.Q<VisualElement>("GridRoot");
        weightLabel = root.Q<Label>("WeightLabel");

        if (gridVE == null)
        {
            Debug.LogError("InventoryGridUI: 'GridRoot' not found in UXML.");
            return;
        }

        root.RegisterCallback<PointerUpEvent>(_ => EndDragAndTryDrop());
        Refresh();
    }

    // ------------------ COORD HELPERS ------------------
    Vector2 ContentOffset()
    {
        if (gridVE == null) return Vector2.zero;
        var rs = gridVE.resolvedStyle;
        return new Vector2(rs.borderLeftWidth + rs.paddingLeft, rs.borderTopWidth + rs.paddingTop);
    }

    Vector2 GetPointerScreenPos()
    {
#if ENABLE_INPUT_SYSTEM
        var m = Mouse.current;
        if (m != null) return m.position.ReadValue();
#endif
        return Input.mousePosition;
    }

    Vector2 MouseLocalInGrid()
    {
        var panelPt = RuntimePanelUtils.ScreenToPanel(root.panel, GetPointerScreenPos());

        // Fix Y flip between screen/panel spaces
        float panelH = root.panel.visualTree.worldBound.height;
        panelPt.y = panelH - panelPt.y;

        var local = gridVE.WorldToLocal(panelPt);
        var off = ContentOffset();
        local.x -= off.x; local.y -= off.y;

        int W = grid.cellsWide * cellSize + (grid.cellsWide - 1) * cellGap;
        int H = grid.cellsHigh * cellSize + (grid.cellsHigh - 1) * cellGap;

        local.x = Mathf.Clamp(local.x, 0f, W - 0.0001f);
        local.y = Mathf.Clamp(local.y, 0f, H - 0.0001f);
        return local;
    }

    // ------------------ INPUT HELPERS (Unity 6) ------------------
    static bool KeyDelPressed()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current; return kb != null && kb.deleteKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Delete);
#endif
    }

    static bool KeyXPressed()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current; return kb != null && kb.xKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.X);
#endif
    }

    static bool KeyRPressed()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current; return kb != null && kb.rKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.R);
#endif
    }

    static bool MouseLeftReleased()
    {
#if ENABLE_INPUT_SYSTEM
        var m = Mouse.current; return m != null && m.leftButton.wasReleasedThisFrame;
#else
        return Input.GetMouseButtonUp(0);
#endif
    }

    // ------------------ UPDATE ------------------
    void Update()
    {
        if (root == null || root.panel == null || gridVE == null || grid == null) return;

        // Make sure we have a camera
        EnsureDropOrigin();

        // Delete hovered stack
        if (!dragging && hoverIndex >= 0 && KeyDelPressed())
        {
            if (grid.RemoveAt(hoverIndex)) { hoverIndex = -1; Refresh(); return; }
        }

        // Drop one item from hovered stack into world
        if (!dragging && hoverIndex >= 0 && KeyXPressed())
        {
            TryDropHovered(hoverIndex, 1);
            return;
        }

        // Rotate hovered (not dragging)
        if (!dragging && hoverIndex >= 0 && KeyRPressed())
        {
            if (RotateIndex(hoverIndex)) Refresh();
        }

        if (!dragging || draggedIndex < 0) return;

        // Rotate while dragging (toggle ghost)
        if (KeyRPressed())
        {
            var e = grid.items[draggedIndex];
            if (TryGetDef(e.itemId, out var def) && def.canRotate) ghostRotated = !ghostRotated;
        }

        var local = MouseLocalInGrid();
        int cx = Mathf.FloorToInt(local.x / step);
        int cy = Mathf.FloorToInt(local.y / step);

        var e0 = grid.items[draggedIndex];
        TryGetDef(e0.itemId, out var d0);
        var sz = grid.GetSize(d0, ghostRotated);

        cx = Mathf.Clamp(cx, 0, Mathf.Max(0, grid.cellsWide - sz.w));
        cy = Mathf.Clamp(cy, 0, Mathf.Max(0, grid.cellsHigh - sz.h));

        PositionGhost(cx, cy, sz.w, sz.h);

        bool can = grid.CanPlace(e0.itemId, cx, cy, ghostRotated, draggedIndex);
        if (ghost != null) ghost.style.backgroundColor = can ? ghostOk : ghostBad;

        if (MouseLeftReleased()) EndDragAndTryDrop();
    }

    // ------------------ RENDER / TILES ------------------
    public void Refresh()
    {
        if (gridVE == null || grid == null) return;

        _tiles.Clear(); gridVE.Clear();

        if (weightLabel != null)
        {
            var cap = grid.maxWeightKg > 0 ? grid.maxWeightKg.ToString("0.##") : "Unlimited";
            weightLabel.text = $"Weight: {grid.TotalWeight():0.##} / {cap} kg";
        }

        int W = grid.cellsWide * cellSize + (grid.cellsWide - 1) * cellGap;
        int H = grid.cellsHigh * cellSize + (grid.cellsHigh - 1) * cellGap;

        gridVE.style.width = W; gridVE.style.height = H;
        gridVE.style.backgroundColor = cellColor;
        gridVE.style.borderTopWidth = gridVE.style.borderLeftWidth =
        gridVE.style.borderRightWidth = gridVE.style.borderBottomWidth = 0;

        var off = ContentOffset();

        // grid lines (vertical)
        for (int x = 1; x < grid.cellsWide; x++)
        {
            var v = new VisualElement { pickingMode = PickingMode.Ignore };
            v.style.position = Position.Absolute;
            v.style.left = off.x + x * step - (cellGap / 2f);
            v.style.top = off.y; v.style.width = cellGap; v.style.height = H;
            v.style.backgroundColor = lineColor; gridVE.Add(v);
        }
        // grid lines (horizontal)
        for (int y = 1; y < grid.cellsHigh; y++)
        {
            var h = new VisualElement { pickingMode = PickingMode.Ignore };
            h.style.position = Position.Absolute;
            h.style.left = off.x; h.style.top = off.y + y * step - (cellGap / 2f);
            h.style.width = W; h.style.height = cellGap;
            h.style.backgroundColor = lineColor; gridVE.Add(h);
        }

        // tiles
        for (int i = 0; i < grid.items.Count; i++)
        {
            var e = grid.items[i];
            if (!TryGetDef(e.itemId, out var def)) continue;

            var sz = grid.GetSize(def, e.rotated);
            var tile = CreateTile(def, e.amount);
            PlaceTile(tile, e.x, e.y, sz.w, sz.h);

            int capture = i;
            tile.RegisterCallback<PointerEnterEvent>(_ => { hoverIndex = capture; tile.AddToClassList("tile--hover"); });
            tile.RegisterCallback<PointerLeaveEvent>(_ => { if (hoverIndex == capture) hoverIndex = -1; tile.RemoveFromClassList("tile--hover"); });
            tile.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button == 1) { if (RotateIndex(capture)) Refresh(); evt.StopImmediatePropagation(); return; }
                BeginDrag(capture); tile.AddToClassList("tile--active");
            });

            gridVE.Add(tile); _tiles.Add(tile);
        }
    }

    VisualElement CreateTile(ItemDefinition def, int amount)
    {
        var tile = new VisualElement();
        tile.style.position = Position.Absolute;
        tile.AddToClassList("tile");

        var img = new Image { scaleMode = ScaleMode.ScaleToFit, image = def.icon ? def.icon.texture : null };
        img.style.width = new Length(100, LengthUnit.Percent);
        img.style.height = new Length(100, LengthUnit.Percent);
        tile.Add(img);

        var count = new Label { text = amount > 1 ? amount.ToString() : "", pickingMode = PickingMode.Ignore };
        count.AddToClassList("tile__badge");
        tile.Add(count);

        return tile;
    }

    void PlaceTile(VisualElement tile, int cx, int cy, int w, int h)
    {
        var off = ContentOffset();
        tile.style.left = off.x + cx * step; tile.style.top = off.y + cy * step;
        tile.style.width = w * cellSize + (w - 1) * cellGap;
        tile.style.height = h * cellSize + (h - 1) * cellGap;
    }

    void BeginDrag(int index)
    {
        if (index < 0 || index >= grid.items.Count || gridVE == null) return;

        dragging = true; draggedIndex = index; hoverIndex = index;

        var e = grid.items[index]; TryGetDef(e.itemId, out var def);
        ghostRotated = e.rotated; var s = grid.GetSize(def, ghostRotated);

        ghost = new VisualElement { pickingMode = PickingMode.Ignore };
        ghost.style.position = Position.Absolute;
        ghost.style.backgroundColor = ghostOk;
        ghost.style.opacity = 0.9f;

        var img = new Image { scaleMode = ScaleMode.ScaleToFit, image = def.icon ? def.icon.texture : null };
        img.style.width = new Length(100, LengthUnit.Percent);
        img.style.height = new Length(100, LengthUnit.Percent);
        ghost.Add(img);

        gridVE.Add(ghost);

        if (draggedIndex < _tiles.Count)
            _tiles[draggedIndex].style.display = DisplayStyle.None;

        PositionGhost(e.x, e.y, s.w, s.h);
    }

    void PositionGhost(int cx, int cy, int w, int h)
    {
        if (ghost == null) return;
        var off = ContentOffset();
        ghost.style.left = off.x + cx * step;
        ghost.style.top = off.y + cy * step;
        ghost.style.width = w * cellSize + (w - 1) * cellGap;
        ghost.style.height = h * cellSize + (h - 1) * cellGap;
    }

    void EndDragAndTryDrop()
    {
        if (!dragging) return;

        var off = ContentOffset();
        float gx = ghost.resolvedStyle.left - off.x;
        float gy = ghost.resolvedStyle.top - off.y;

        int cx = Mathf.RoundToInt(gx / step);
        int cy = Mathf.RoundToInt(gy / step);

        grid.TryMove(draggedIndex, cx, cy, ghostRotated);

        if (draggedIndex < _tiles.Count)
            _tiles[draggedIndex].RemoveFromClassList("tile--active");

        dragging = false; draggedIndex = -1;
        ghost?.RemoveFromHierarchy(); ghost = null; Refresh();
    }

    // ------------------ WORLD DROP ------------------
    void TryDropHovered(int index, int amount)
    {
        if ((uint)index >= (uint)grid.items.Count) return;

        EnsureDropOrigin();
        if (!dropOrigin) return; // already warned once

        var e = grid.items[index];
        if (!TryGetDef(e.itemId, out var def)) return;

        if (!def.worldPrefab)
        {
            Debug.LogWarning($"Item '{def.Id}' has no worldPrefab assigned.");
            return;
        }

        // spawn pose
        Vector3 pos = dropOrigin.position + dropOrigin.forward * Mathf.Max(0.1f, dropDistance);
        Quaternion rot = Quaternion.LookRotation(dropOrigin.forward, Vector3.up);

        // soft ground snap if there is ground below
        if (Physics.Raycast(pos + Vector3.up * 0.5f, Vector3.down, out var hit, 1.0f, ~0, QueryTriggerInteraction.Ignore))
            pos = hit.point + Vector3.up * groundSnap;

        // instantiate
        var go = Instantiate(def.worldPrefab, pos, rot);

        // ensure WorldPickup
        var wp = go.GetComponent<WorldPickup>();
        if (!wp) wp = go.AddComponent<WorldPickup>();
        wp.itemId = def.Id;
        wp.amount = Mathf.Max(1, amount);

        // impulse if rigidbody exists
        var rb = go.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.AddForce(dropOrigin.forward * throwForce + Vector3.up * upwardBoost, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * 1.5f, ForceMode.Impulse);
        }

        // remove from inventory
        if (grid.RemoveAt(index, 1))
            Refresh();
    }

    // ------------------ UTIL ------------------
    bool RotateIndex(int idx) => grid.TryRotateInPlace(idx) || grid.TryRotateOrRepack(idx);

    bool TryGetDef(string id, out ItemDefinition def)
    {
        def = null;
        return ItemDatabase.Instance && ItemDatabase.Instance.TryGet(id, out def);
    }
}
