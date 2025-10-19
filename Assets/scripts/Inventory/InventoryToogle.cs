using UnityEngine;
using UnityEngine.UIElements;

[DisallowMultipleComponent]
[RequireComponent(typeof(UIDocument))]
public class InventoryToggle : MonoBehaviour
{
    [Header("Toggle")]
    [SerializeField] private KeyCode key = KeyCode.Tab;

    [Header("Behaviour")]
    [SerializeField] private bool pauseWhileOpen = false;
    [SerializeField] private bool lockCursorWhenClosed = true;

    [Header("Disable these while inventory is open")]
    // Drag scripts here (e.g., FpController, FirstPersonLook). If empty, we auto-wire FpController.
    [SerializeField] private MonoBehaviour[] disableWhileOpen;

    private UIDocument doc;
    private bool open;

    void Awake()
    {
        doc = GetComponent<UIDocument>();

        // Auto-wire FpController if nothing assigned (Unity 6 API)
        if (disableWhileOpen == null || disableWhileOpen.Length == 0)
        {
            var fp = Object.FindFirstObjectByType<FpController>(FindObjectsInactive.Exclude);
            if (fp != null) disableWhileOpen = new MonoBehaviour[] { fp };
        }

        ApplyState(false); // start closed
    }

    void OnDisable()
    {
        // If this component is disabled while open, restore gameplay
        if (open) ApplyState(false);
    }

    void Update()
    {
        // Use your InputCompat wrapper (or replace with Input System binding)
        if (InputCompat.GetKeyDown(key))
        {
            ApplyState(!open);
        }
    }

    private void ApplyState(bool value)
    {
        open = value;

        // Show/hide UI Toolkit document
        var root = doc ? doc.rootVisualElement : null;
        if (root != null)
            root.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;

        // Enable/disable gameplay scripts
        if (disableWhileOpen != null)
        {
            foreach (var mb in disableWhileOpen)
                if (mb) mb.enabled = !open;
        }

        // Fully qualify to avoid ambiguity with UnityEngine.UIElements.Cursor
        UnityEngine.Cursor.lockState = open ? CursorLockMode.None :
                                           (lockCursorWhenClosed ? CursorLockMode.Locked : CursorLockMode.None);
        UnityEngine.Cursor.visible = open || !lockCursorWhenClosed;

        // Optional pause
        if (pauseWhileOpen)
            Time.timeScale = open ? 0f : 1f;
    }
}
