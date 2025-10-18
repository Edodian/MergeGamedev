using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class InventoryGridUI : MonoBehaviour
{
    public InventoryGridData grid;

    [Header("Visuals")]
    public int cellSize = 64;
    public int cellGap = 2;
    public Color cellColor = new Color(0.09f, 0.09f, 0.10f, 1f);
    public Color lineColor = new Color(0.17f, 0.19f, 0.21f, 1f);
    public Color ghostOk = new Color(0.25f, 0.9f, 0.45f, 0.25f);
    public Color ghostBad = new Color(1f, 0.25f, 0.25f, 0.25f);

    UIDocument doc;
    VisualElement root, gridVE, ghost;
    Label weightLabel;

    readonly List<VisualElement> _tiles = new();
    int step => cellSize + cellGap;

    bool dragging;
    int draggedIndex = -1;
    bool ghostRotated;
    int hoverIndex = -1;

    void OnEnable()
    {
        doc = GetComponent<UIDocument>();
        if (!grid) grid = FindObjectOfType<InventoryGridData>();
        if (grid) grid.Changed += OnGridChanged;

        root = doc ? doc.rootVisualElement : null;
        if (root == null) return;

        if (root.panel != null) SetupUI();
        else root.RegisterCallback<AttachToPanelEvent>(_ => SetupUI());
    }

    void OnDisable()
    {
        if (grid) grid.Changed -= OnGridChanged;
        dragging = false; draggedIndex = -1; hoverIndex = -1;
        ghost = null; gridVE = null; weightLabel = null;
    }

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

    // ---------- helpers ----------
    Vector2 ContentOffset()
    {
        if (gridVE == null) return Vector2.zero;
        var rs = gridVE.resolvedStyle;
        return new Vector2(rs.borderLeftWidth + rs.paddingLeft,
                           rs.borderTopWidth + rs.paddingTop);
    }

    // Convert mouse to grid-local coords with correct Y (top-left origin)
    Vector2 MouseLocalInGrid()
    {
        // 1) Screen -> Panel units
        var panelPt = RuntimePanelUtils.ScreenToPanel(root.panel, InputCompat.MousePosition());

        // 2) Invert Y because Input uses bottom-left while panel uses top-left
        float panelH = root.panel.visualTree.worldBound.height;
        panelPt.y = panelH - panelPt.y;

        // 3) Panel -> this element local
        var local = gridVE.WorldToLocal(panelPt);

        // 4) Subtract border/padding so (0,0) is first cell’s top-left
        var off = ContentOffset();
        local.x -= off.x;
        local.y -= off.y;

        // 5) Clamp to drawable content so last row/col are reachable
        int W = grid.cellsWide * cellSize + (grid.cellsWide - 1) * cellGap;
        int H = grid.cellsHigh * cellSize + (grid.cellsHigh - 1) * cellGap;
        local.x = Mathf.Clamp(local.x, 0f, W - 0.0001f);
        local.y = Mathf.Clamp(local.y, 0f, H - 0.0001f);

        return local;
    }

    void Update()
    {
        if (root == null || root.panel == null || gridVE == null || grid == null)
            return;

        // Rotate hovered (not dragging)
        if (!dragging && hoverIndex >= 0 && InputCompat.GetKeyDown(KeyCode.R))
        {
            if (RotateIndex(hoverIndex)) Refresh();
        }

        if (!dragging || draggedIndex < 0) return;

        // Rotate ghost while dragging
        if (InputCompat.GetKeyDown(KeyCode.R))
        {
            var e = grid.items[draggedIndex];
            if (TryGetDef(e.itemId, out var def) && def.canRotate)
                ghostRotated = !ghostRotated;
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
        ghost.style.backgroundColor = can ? ghostOk : ghostBad;

        if (InputCompat.GetMouseButtonUp(0))
            EndDragAndTryDrop();
    }

    public void Refresh()
    {
        if (gridVE == null || grid == null) return;

        _tiles.Clear();
        gridVE.Clear();

        // header weight
        if (weightLabel != null)
        {
            var cap = grid.maxWeightKg > 0 ? grid.maxWeightKg.ToString("0.##") : "Unlimited";
            weightLabel.text = $"Weight: {grid.TotalWeight():0.##} / {cap} kg";
        }

        int W = grid.cellsWide * cellSize + (grid.cellsWide - 1) * cellGap;
        int H = grid.cellsHigh * cellSize + (grid.cellsHigh - 1) * cellGap;

        gridVE.style.width = W;
        gridVE.style.height = H;
        gridVE.style.backgroundColor = cellColor;

        // remove built-in border (avoid 1px offset)
        gridVE.style.borderTopWidth = 0;
        gridVE.style.borderLeftWidth = 0;
        gridVE.style.borderRightWidth = 0;
        gridVE.style.borderBottomWidth = 0;

        var off = ContentOffset();

        // grid lines
        for (int x = 1; x < grid.cellsWide; x++)
        {
            var v = new VisualElement { pickingMode = PickingMode.Ignore };
            v.style.position = Position.Absolute;
            v.style.left = off.x + x * step - (cellGap / 2f);
            v.style.top = off.y;
            v.style.width = cellGap;
            v.style.height = H;
            v.style.backgroundColor = lineColor;
            gridVE.Add(v);
        }
        for (int y = 1; y < grid.cellsHigh; y++)
        {
            var h = new VisualElement { pickingMode = PickingMode.Ignore };
            h.style.position = Position.Absolute;
            h.style.left = off.x;
            h.style.top = off.y + y * step - (cellGap / 2f);
            h.style.width = W;
            h.style.height = cellGap;
            h.style.backgroundColor = lineColor;
            gridVE.Add(h);
        }

        // items
        for (int i = 0; i < grid.items.Count; i++)
        {
            var e = grid.items[i];
            if (!TryGetDef(e.itemId, out var def)) continue;

            var sz = grid.GetSize(def, e.rotated);
            var tile = CreateTile(def, e.amount);
            PlaceTile(tile, e.x, e.y, sz.w, sz.h);

            int capture = i;

            tile.RegisterCallback<PointerEnterEvent>(_ =>
            {
                hoverIndex = capture;
                tile.AddToClassList("tile--hover");
            });
            tile.RegisterCallback<PointerLeaveEvent>(_ =>
            {
                if (hoverIndex == capture) hoverIndex = -1;
                tile.RemoveFromClassList("tile--hover");
            });
            tile.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button == 1)
                {
                    if (RotateIndex(capture)) Refresh();
                    evt.StopImmediatePropagation();
                    return;
                }
                BeginDrag(capture);
                tile.AddToClassList("tile--active");
            });

            gridVE.Add(tile);
            _tiles.Add(tile);
        }
    }

    VisualElement CreateTile(ItemDefinition def, int amount)
    {
        var tile = new VisualElement();
        tile.style.position = Position.Absolute;
        tile.AddToClassList("tile");

        var img = new Image { scaleMode = ScaleMode.ScaleToFit };
        img.image = def.icon ? def.icon.texture : null;
        img.style.width = new Length(100, LengthUnit.Percent);
        img.style.height = new Length(100, LengthUnit.Percent);
        tile.Add(img);

        var count = new Label();
        count.text = amount > 1 ? amount.ToString() : "";
        count.AddToClassList("tile__badge");
        count.pickingMode = PickingMode.Ignore;
        tile.Add(count);

        return tile;
    }

    void PlaceTile(VisualElement tile, int cx, int cy, int w, int h)
    {
        var off = ContentOffset();
        tile.style.left = off.x + cx * step;
        tile.style.top = off.y + cy * step;
        tile.style.width = w * cellSize + (w - 1) * cellGap;
        tile.style.height = h * cellSize + (h - 1) * cellGap;
    }

    void BeginDrag(int index)
    {
        if (index < 0 || index >= grid.items.Count || gridVE == null) return;

        dragging = true;
        draggedIndex = index;
        hoverIndex = index;

        var e = grid.items[index];
        TryGetDef(e.itemId, out var def);
        ghostRotated = e.rotated;
        var s = grid.GetSize(def, ghostRotated);

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

        var ok = grid.TryMove(draggedIndex, cx, cy, ghostRotated);

        if (draggedIndex < _tiles.Count)
            _tiles[draggedIndex].RemoveFromClassList("tile--active");

        dragging = false;
        draggedIndex = -1;

        ghost?.RemoveFromHierarchy();
        ghost = null;

        Refresh();
    }

    bool RotateIndex(int idx)
    {
        if (grid.TryRotateInPlace(idx)) return true;
        return grid.TryRotateOrRepack(idx);
    }

    bool TryGetDef(string id, out ItemDefinition def)
    {
        def = null;
        return ItemDatabase.Instance && ItemDatabase.Instance.TryGet(id, out def);
    }
}
