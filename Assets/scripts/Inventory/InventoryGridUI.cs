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
    public Color cellColor = new Color(0.12f, 0.12f, 0.12f, 0.95f);
    public Color lineColor = new Color(0.25f, 0.25f, 0.25f, 1f);
    public Color ghostOk = new Color(0f, 1f, 0f, 0.25f);
    public Color ghostBad = new Color(1f, 0f, 0f, 0.25f);

    UIDocument doc;
    VisualElement root, gridVE, ghost;
    Label weightLabel;

    readonly List<VisualElement> _tiles = new();
    int step => cellSize + cellGap;

    // drag state
    bool dragging;
    int draggedIndex = -1;
    bool ghostRotated;

    void Awake()
    {
        doc = GetComponent<UIDocument>();
        if (!grid) grid = FindObjectOfType<InventoryGridData>();

        root = doc.rootVisualElement;
        root.style.flexDirection = FlexDirection.Column;
        root.style.paddingLeft = 8;
        root.style.paddingTop = 8;

        var header = new VisualElement { style = { flexDirection = FlexDirection.Row } };
        var title = new Label("Inventory") { style = { unityFontStyleAndWeight = FontStyle.Bold, fontSize = 16, marginRight = 10 } };
        weightLabel = new Label();
        header.Add(title);
        header.Add(weightLabel);

        gridVE = new VisualElement();
        gridVE.style.position = Position.Relative;
        gridVE.style.flexGrow = 0;
        gridVE.style.flexShrink = 0;
        gridVE.pickingMode = PickingMode.Position;

        root.Add(header);
        root.Add(gridVE);

        root.RegisterCallback<PointerUpEvent>(_ => EndDragAndTryDrop());
        Refresh();
    }

    void Update()
    {
        if (!dragging || grid == null || draggedIndex < 0) return;

        // rotate ghost on R
        if (Input.GetKeyDown(KeyCode.R))
        {
            var e = grid.items[draggedIndex];
            if (TryGetDef(e.itemId, out var def) && def.canRotate)
                ghostRotated = !ghostRotated;
        }

        // move ghost under mouse (panel space)
        var panel = root.panel;
        var mousePanel = RuntimePanelUtils.ScreenToPanel(panel, Input.mousePosition);
        var local = mousePanel - gridVE.worldBound.position;

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

        // fallback mouse-up check (in case events missed)
        if (Input.GetMouseButtonUp(0)) EndDragAndTryDrop();
    }

    // ---------- UI build ----------
    public void Refresh()
    {
        if (!grid) return;

        _tiles.Clear();
        gridVE.Clear();

        weightLabel.text = $"Weight: {grid.TotalWeight():0.##} / {(grid.maxWeightKg > 0 ? grid.maxWeightKg.ToString("0.##") : "∞")} kg";

        int W = grid.cellsWide * cellSize + (grid.cellsWide - 1) * cellGap;
        int H = grid.cellsHigh * cellSize + (grid.cellsHigh - 1) * cellGap;

        gridVE.style.width = W;
        gridVE.style.height = H;
        gridVE.style.backgroundColor = cellColor;
        gridVE.style.borderBottomColor = lineColor;
        gridVE.style.borderLeftColor = lineColor;
        gridVE.style.borderRightColor = lineColor;
        gridVE.style.borderTopColor = lineColor;
        gridVE.style.borderBottomWidth = 1;
        gridVE.style.borderLeftWidth = 1;
        gridVE.style.borderRightWidth = 1;
        gridVE.style.borderTopWidth = 1;

        // draw grid lines (lightweight)
        for (int x = 1; x < grid.cellsWide; x++)
        {
            var v = new VisualElement();
            v.style.position = Position.Absolute;
            v.style.left = x * step - (cellGap / 2f);
            v.style.top = 0;
            v.style.width = cellGap;
            v.style.height = H;
            v.style.backgroundColor = lineColor;
            gridVE.Add(v);
        }
        for (int y = 1; y < grid.cellsHigh; y++)
        {
            var h = new VisualElement();
            h.style.position = Position.Absolute;
            h.style.left = 0;
            h.style.top = y * step - (cellGap / 2f);
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
            var tile = CreateTile(def, e.amount, sz.w, sz.h);
            PlaceTile(tile, e.x, e.y, sz.w, sz.h);
            int capture = i;
            tile.RegisterCallback<PointerDownEvent>(_ => BeginDrag(capture));
            gridVE.Add(tile);
            _tiles.Add(tile);
        }
    }

    VisualElement CreateTile(ItemDefinition def, int amount, int w, int h)
    {
        var tile = new VisualElement();
        tile.style.position = Position.Absolute;
        tile.style.backgroundColor = new Color(1, 1, 1, 0.05f);
        tile.style.borderTopLeftRadius = 4;
        tile.style.borderTopRightRadius = 4;
        tile.style.borderBottomLeftRadius = 4;
        tile.style.borderBottomRightRadius = 4;
        tile.style.borderTopWidth = 1;
        tile.style.borderLeftWidth = 1;
        tile.style.borderRightWidth = 1;
        tile.style.borderBottomWidth = 1;
        tile.style.borderTopColor = lineColor;
        tile.style.borderLeftColor = lineColor;
        tile.style.borderRightColor = lineColor;
        tile.style.borderBottomColor = lineColor;

        var img = new Image { scaleMode = ScaleMode.ScaleToFit };
        img.image = def.icon ? def.icon.texture : null;
        img.style.width = new Length(100, LengthUnit.Percent);
        img.style.height = new Length(100, LengthUnit.Percent);
        tile.Add(img);

        // stack count badge
        var count = new Label();
        count.text = amount > 1 ? amount.ToString() : "";
        count.pickingMode = PickingMode.Ignore;
        count.style.position = Position.Absolute;
        count.style.bottom = 2; count.style.right = 4;
        count.style.unityTextAlign = TextAnchor.MiddleRight;
        count.style.fontSize = 12;
        count.style.backgroundColor = new Color(0, 0, 0, 0.5f);
        count.style.paddingLeft = 4; count.style.paddingRight = 4;
        tile.Add(count);

        return tile;
    }

    void PlaceTile(VisualElement tile, int cx, int cy, int w, int h)
    {
        tile.style.left = cx * step;
        tile.style.top = cy * step;
        tile.style.width = w * cellSize + (w - 1) * cellGap;
        tile.style.height = h * cellSize + (h - 1) * cellGap;
    }

    // ---------- Drag ----------
    void BeginDrag(int index)
    {
        if (index < 0 || index >= grid.items.Count) return;
        dragging = true;
        draggedIndex = index;
        ghostRotated = grid.items[index].rotated;

        // build ghost tile
        TryGetDef(grid.items[index].itemId, out var def);
        var s = grid.GetSize(def, ghostRotated);

        ghost = new VisualElement();
        ghost.style.position = Position.Absolute;
        ghost.style.backgroundColor = ghostOk;
        ghost.style.opacity = 0.9f;
        ghost.pickingMode = PickingMode.Ignore;

        var img = new Image { scaleMode = ScaleMode.ScaleToFit };
        img.image = def.icon ? def.icon.texture : null;
        img.style.width = new Length(100, LengthUnit.Percent);
        img.style.height = new Length(100, LengthUnit.Percent);
        ghost.Add(img);

        gridVE.Add(ghost);

        // hide original during drag
        if (draggedIndex < _tiles.Count) _tiles[draggedIndex].style.display = DisplayStyle.None;

        // initial position
        PositionGhost(grid.items[index].x, grid.items[index].y, s.w, s.h);
    }

    void PositionGhost(int cx, int cy, int w, int h)
    {
        ghost.style.left = cx * step;
        ghost.style.top = cy * step;
        ghost.style.width = w * cellSize + (w - 1) * cellGap;
        ghost.style.height = h * cellSize + (h - 1) * cellGap;
    }

    void EndDragAndTryDrop()
    {
        if (!dragging) return;

        // compute target cell under ghost
        int cx = Mathf.RoundToInt(ghost.resolvedStyle.left / step);
        int cy = Mathf.RoundToInt(ghost.resolvedStyle.top / step);

        var e = grid.items[draggedIndex];
        bool ok = grid.TryMove(draggedIndex, cx, cy, ghostRotated);

        dragging = false;
        draggedIndex = -1;

        ghost.RemoveFromHierarchy();
        ghost = null;

        Refresh();
        if (!ok) ; // (could flash red or play SFX)
    }

    bool TryGetDef(string id, out ItemDefinition def)
    {
        def = null;
        return ItemDatabase.Instance && ItemDatabase.Instance.TryGet(id, out def);
    }
}
