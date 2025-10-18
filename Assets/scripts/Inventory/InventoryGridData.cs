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

public class InventoryGridData : MonoBehaviour
{
    [Header("Grid size (cells)")] public int cellsWide = 10; public int cellsHigh = 6;
    [Header("Limits (0 = unlimited)")] public float maxWeightKg = 0f;
    [Header("Contents")] public List<GridItemEntry> items = new();

    // change event (autosave / UI refresh)
    public event Action Changed;
    void Notify() => Changed?.Invoke();

    public bool TryAdd(string itemId, int amount, int x, int y, bool rotated)
    {
        if (!ValidDef(itemId, out var def)) return false;
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
                e.amount += put; amount -= put;
                if (amount <= 0) { Notify(); return true; }
            }
        }

        if (!CanPlace(itemId, x, y, rotated, -1)) return false;

        int toPut = def.maxStack > 1 ? Mathf.Min(amount, def.maxStack) : 1;
        var entry = new GridItemEntry { itemId = itemId, amount = toPut, x = x, y = y, rotated = rotated };
        items.Add(entry);

        if (ExceedsWeight()) { items.Remove(entry); return false; }
        Notify(); return true;
    }

    public int AddAuto(string itemId, int amount, bool allowRotate = true)
    {
        if (!ValidDef(itemId, out var def)) return amount;
        amount = Mathf.Max(1, amount);
        bool changed = false;

        // stack first
        if (def.maxStack > 1)
        {
            foreach (var e in items)
            {
                if (e.itemId != itemId) continue;
                int space = def.maxStack - e.amount; if (space <= 0) continue;
                int put = Mathf.Min(space, amount);
                e.amount += put; amount -= put; changed = true;
                if (amount <= 0) { if (changed) Notify(); return 0; }
            }
        }

        while (amount > 0)
        {
            if (!TryFindSlotFor(def, allowRotate, out int px, out int py, out bool prot)) break;
            var entry = new GridItemEntry { itemId = itemId, amount = 1, x = px, y = py, rotated = prot };
            items.Add(entry);
            if (ExceedsWeight()) { items.Remove(entry); break; }
            changed = true; amount--;

            if (def.maxStack > 1 && amount > 0)
            {
                int space = def.maxStack - entry.amount;
                int put = Mathf.Min(space, amount);
                entry.amount += put; amount -= put;
            }
        }
        if (changed) Notify();
        return amount;
    }

    public bool TryMove(int index, int newX, int newY, bool newRotated)
    {
        if ((uint)index >= (uint)items.Count) return false;
        var e = items[index];
        var ox = e.x; var oy = e.y; var orot = e.rotated;

        e.x = newX; e.y = newY; e.rotated = newRotated;

        if (!CanPlace(e.itemId, e.x, e.y, e.rotated, index) || ExceedsWeight())
        {
            e.x = ox; e.y = oy; e.rotated = orot; return false;
        }
        Notify(); return true;
    }

    // remove whole stack or partial amount
    public bool RemoveAt(int index, int amount = int.MaxValue)
    {
        if ((uint)index >= (uint)items.Count) return false;
        var e = items[index];
        if (amount >= e.amount) { items.RemoveAt(index); Notify(); return true; }
        e.amount -= Mathf.Max(1, amount); Notify(); return true;
    }

    public bool TryRotateInPlace(int index)
    {
        if ((uint)index >= (uint)items.Count) return false;
        var e = items[index];
        if (!ValidDef(e.itemId, out var def) || !def.canRotate) return false;
        return TryMove(index, e.x, e.y, !e.rotated);
    }

    public bool TryRotateOrRepack(int index)
    {
        if ((uint)index >= (uint)items.Count) return false;
        var e = items[index];
        if (!ValidDef(e.itemId, out var def) || !def.canRotate) return false;
        if (TryRotateInPlace(index)) return true;
        if (TryFindSlotForOrientation(def, !e.rotated, out int nx, out int ny))
            return TryMove(index, nx, ny, !e.rotated);
        return false;
    }

    public float TotalWeight()
    {
        float sum = 0f;
        foreach (var e in items) if (ValidDef(e.itemId, out var def))
                sum += def.weightKg * Mathf.Max(1, e.amount);
        return sum;
    }
    public bool ExceedsWeight() => maxWeightKg > 0f && TotalWeight() > maxWeightKg;

    // helpers
    public (int w, int h) GetSize(ItemDefinition def, bool rotated)
        => rotated ? (def.gridHeight, def.gridWidth) : (def.gridWidth, def.gridHeight);

    public bool CanPlace(string itemId, int x, int y, bool rotated, int ignoreIndex)
    {
        if (!ValidDef(itemId, out var def)) return false;
        if (rotated && !def.canRotate) return false;

        var (w, h) = GetSize(def, rotated);
        if (x < 0 || y < 0 || x + w > cellsWide || y + h > cellsHigh) return false;

        for (int i = 0; i < items.Count; i++)
        {
            if (i == ignoreIndex) continue;
            var e = items[i];
            if (!ValidDef(e.itemId, out var d2)) continue;
            var (w2, h2) = GetSize(d2, e.rotated);
            if (x < e.x + w2 && x + w > e.x && y < e.y + h2 && y + h > e.y) return false;
        }
        return true;
    }

    public bool TryFindSlotFor(ItemDefinition def, bool allowRotate,
                               out int outX, out int outY, out bool outRot)
    {
        bool[] rots = (allowRotate && def.canRotate) ? new[] { false, true } : new[] { false };
        foreach (var rot in rots)
        {
            var (w, h) = GetSize(def, rot);
            for (int y = 0; y <= cellsHigh - h; y++)
                for (int x = 0; x <= cellsWide - w; x++)
                    if (CanPlace(def.Id, x, y, rot, -1))
                    { outX = x; outY = y; outRot = rot; return true; }
        }
        outX = outY = 0; outRot = false; return false;
    }

    public bool TryFindSlotForOrientation(ItemDefinition def, bool rot, out int outX, out int outY)
    {
        var (w, h) = GetSize(def, rot);
        for (int y = 0; y <= cellsHigh - h; y++)
            for (int x = 0; x <= cellsWide - w; x++)
                if (CanPlace(def.Id, x, y, rot, -1))
                { outX = x; outY = y; return true; }
        outX = outY = 0; return false;
    }

    bool ValidDef(string id, out ItemDefinition def)
    {
        if (ItemDatabase.Instance && ItemDatabase.Instance.TryGet(id, out def)) return true;
        def = null; return false;
    }

    // JSON
    [Serializable] class SaveBlob { public int cellsWide, cellsHigh; public float maxWeightKg; public List<GridItemEntry> items; }
    public string ToJson() => JsonUtility.ToJson(new SaveBlob
    {
        cellsWide = cellsWide,
        cellsHigh = cellsHigh,
        maxWeightKg = maxWeightKg,
        items = items
    });
    public void FromJson(string json)
    {
        var b = JsonUtility.FromJson<SaveBlob>(json);
        cellsWide = b.cellsWide; cellsHigh = b.cellsHigh; maxWeightKg = b.maxWeightKg;
        items = b.items ?? new(); Notify();
    }
}
