using UnityEngine;

public class LookPickupController : MonoBehaviour
{
    public Camera cam;
    public float maxDistance = 3f;     // how far you can pick
    public float viewAngle = 5f;       // half-cone angle in degrees (tighter = more precise)
    public KeyCode interactKey = KeyCode.E;
    public InventoryGridData grid;

    ItemPickupTarget current;

    void Awake()
    {
        if (!cam) cam = Camera.main;
        if (!grid) grid = FindObjectOfType<InventoryGridData>();
    }

    void Update()
    {
        current = FindBestTarget();
        InteractPromptUI.SetPrompt(current ? $"[{interactKey}] Pick up {current.GetDisplayName()}" : "");

        if (current && Input.GetKeyDown(interactKey))
            TryPickup(current);
    }

    ItemPickupTarget FindBestTarget()
    {
        if (!cam) return null;

        var camPos = cam.transform.position;
        var fwd = cam.transform.forward;
        float cosThresh = Mathf.Cos(viewAngle * Mathf.Deg2Rad);

        ItemPickupTarget best = null;
        float bestScore = float.NegativeInfinity;

        foreach (var t in ItemPickupTarget.All)
        {
            if (!t) continue;

            // Vector from camera to target
            Vector3 to = t.transform.position - camPos;
            float dist = to.magnitude;
            if (dist > maxDistance) continue;

            Vector3 dir = to / (dist + 1e-6f);
            float dot = Vector3.Dot(fwd, dir);      // 1 = dead center, 0 = 90°

            if (dot < cosThresh) continue;          // outside the cone

            // Also require it's in front and on screen (optional but nice)
            var vp = cam.WorldToViewportPoint(t.transform.position);
            if (vp.z <= 0f || vp.x < 0f || vp.x > 1f || vp.y < 0f || vp.y > 1f) continue;

            // Score: prefer centered (dot) and a bit prefer closer (smaller dist)
            float score = dot * 2f - dist * 0.1f;
            if (score > bestScore) { bestScore = score; best = t; }
        }
        return best;
    }

    void TryPickup(ItemPickupTarget t)
    {
        if (!grid) { Debug.LogWarning("No InventoryGridData in scene"); return; }

        int left = grid.AddAuto(t.itemId, t.amount, allowRotate: true);
        if (left == 0)
        {
            FindObjectOfType<InventoryGridUI>()?.Refresh();
            Destroy(t.gameObject);
        }
        else
        {
            InteractPromptUI.SetPrompt("No space or overweight");
        }
    }
}
