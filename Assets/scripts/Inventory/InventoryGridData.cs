using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GridItemEntry
{
    public string itemId;
    public int amount = 1;
    public int x, y;      // top-left cell
    public bool rotated;
}

/// Tarkov/RE7-like tetris grid with rotation, stacking, weight cap, JSON save/load.
public class InventoryGridData : MonoBehaviour
{
    [Header("Grid size (cells)")]
    public int cellsWide = 10;
    public int cellsHigh = 6;

    [Header("Limits (0 = unlimited)")]
    public float maxWeightKg = 0f;

    [Header("Contents")]
    public List<GridItemEntry> items = new();

    // ---------- Public API ----------
    public bool TryAdd(string itemId, int amount, int x, int y, bool rotated)
    {
        if (!ValidDef(itemId, out var def)) return false;
        amount = Mathf.Max(1, amount);

        // fill existing stacks first
        if (def.maxStack > 1)
        {
            foreach (var e in items)
            {
                if (e.itemId != itemId) continue;
                int space = def.maxStack - e.amount;
                if (space <= 0) continue;
                int put = Mathf.Min(space, amount);
                e.amount += put;
                amount -= put;
                if (amount <= 0) return true;
            }
        }

        if (!CanPlace(itemId, x, y, rotated, -1)) return false;

        // clamp new stack to maxStack (or 1)
        int toPut = def.maxStack > 1 ? Mathf.Min(amount, def.maxStack) : 1;
        var entry = new GridItemEntry { itemId = itemId, amount = toPut, x = x, y = y, rotated = rotated };
        items.Add(entry);

        if (ExceedsWeight()) { items.Remove(entry); return false; }
        return true; // any leftovers ignored; use AddAuto for multi-stack fill
    }

    /// Auto-place (tries rotation). Returns leftovers (0 = fully placed).
    public int AddAuto(string itemId, int amount, bool allowRotate = true)
    {
        if (!ValidDef(itemId, out var def)) return amount;
        amount = Mathf.Max(1, amount);

        // stack first
        if (def.maxStack > 1)
        {
            foreach (var e in items)
            {
                if (e.itemId != itemId) continue;
                int space = def.maxStack - e.amount;
                if (space <= 0) continue;
                int put = Mathf.Min(space, amount);
                e.amount += put;
                amount -= put;
                if (amount <= 0) return 0;
            }
        }

        while (amount > 0)
        {
            if (!TryFindSlotFor(def, allowRotate, out int px, out int py, out bool prot)) break;
            var entry = new GridItemEntry { itemId = itemId, amount = 1, x = px, y = py, rotated = prot };
            items.Add(entry);
            if (ExceedsWeight()) { items.Remove(entry); break; }
            amount -= 1;

            // fill the new stack if stackable
            if (def.maxStack > 1 && amount > 0)
            {
                int space = def.maxStack - entry.amount;
                int put = Mathf.Min(space, amount);
                entry.amount += put;
                amount -= put;
            }
        }
        return amount;
    }

    public bool TryMove(int index, int newX, int newY, bool newRotated)
    {
        if ((uint)index >= (uint)items.Count) return false;
        var e = items[index];
        var oldX = e.x; var oldY = e.y; var oldR = e.rotated;

        e.x = newX; e.y = newY; e.rotated = newRotated;

        if (!CanPlace(e.itemId, e.x, e.y, e.rotated, index) || ExceedsWeight())
        {
            e.x = oldX; e.y = oldY; e.rotated = oldR; // revert
            return false;
        }
        return true;
    }

    public float TotalWeight()
    {
        float sum = 0f;
        foreach (var e in items)
            if (ValidDef(e.itemId, out var def))
                sum += def.weightKg * Mathf.Max(1, e.amount);
        return sum;
    }
    public bool ExceedsWeight() => maxWeightKg > 0f && TotalWeight() > maxWeightKg;

    public int IndexAt(int cx, int cy)
    {
        for (int i = 0; i < items.Count; i++)
        {
            var e = items[i];
            if (!ValidDef(e.itemId, out var d)) continue;
            var s = GetSize(d, e.rotated);
            if (cx >= e.x && cx < e.x + s.w && cy >= e.y && cy < e.y + s.h) return i;
        }
        return -1;
    }

    // ---------- helpers ----------
    public (int w, int h) GetSize(ItemDefinition def, bool rotated)
        => rotated ? (def.gridHeight, def.gridWidth) : (def.gridWidth, def.gridHeight);

    public bool CanPlace(string itemId, int x, int y, bool rotated, int ignoreIndex)
    {
        if (!ValidDef(itemId, out var def)) return false;

        // respect canRotate
        if (rotated && !def.canRotate) return false;

        var size = GetSize(def, rotated);
        int w = size.w, h = size.h;

        if (x < 0 || y < 0 || x + w > cellsWide || y + h > cellsHigh) return false;

        for (int i = 0; i < items.Count; i++)
        {
            if (i == ignoreIndex) continue;
            var e = items[i];
            if (!ValidDef(e.itemId, out var d2)) continue;
            var s2 = GetSize(d2, e.rotated);
            int w2 = s2.w, h2 = s2.h;
            if (x < e.x + w2 && x + w > e.x && y < e.y + h2 && y + h > e.y)
                return false;
        }
        return true;
    }

    bool TryFindSlotFor(ItemDefinition def, bool allowRotate, out int outX, out int outY, out bool outRot)
    {
        bool[] rots = (allowRotate && def.canRotate) ? new[] { false, true } : new[] { false };
        foreach (var rot in rots)
        {
            var size = GetSize(def, rot);
            int w = size.w, h = size.h;
            for (int y = 0; y <= cellsHigh - h; y++)
                for (int x = 0; x <= cellsWide - w; x++)
                    if (CanPlace(def.Id, x, y, rot, -1)) { outX = x; outY = y; outRot = rot; return true; }
        }
        outX = outY = 0; outRot = false; return false;
    }

    bool ValidDef(string id, out ItemDefinition def)
    {
        if (ItemDatabase.Instance != null && ItemDatabase.Instance.TryGet(id, out def))
            return true;
        def = null;
        return false;
    }

    // ---------- JSON ----------
    [Serializable] class SaveBlob { public int cellsWide, cellsHigh; public float maxWeightKg; public List<GridItemEntry> items; }
    public string ToJson()
    {
        var b = new SaveBlob { cellsWide = cellsWide, cellsHigh = cellsHigh, maxWeightKg = maxWeightKg, items = items };
        return JsonUtility.ToJson(b);
    }
    public void FromJson(string json)
    {
        var b = JsonUtility.FromJson<SaveBlob>(json);
        cellsWide = b.cellsWide; cellsHigh = b.cellsHigh; maxWeightKg = b.maxWeightKg; items = b.items ?? new();
    }
}
