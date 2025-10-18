using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class InventoryToggle : MonoBehaviour
{
    public KeyCode key = KeyCode.Tab;
    public bool pauseWhileOpen = false;

    UIDocument doc; bool open;

    void Awake()
    {
        doc = GetComponent<UIDocument>();
        Show(false); // start closed
    }

    void Update()
    {
        if (InputCompat.GetKeyDown(key)) Show(!open);
    }

    void Show(bool v)
    {
        open = v;
        var root = doc.rootVisualElement;
        root.style.display = v ? DisplayStyle.Flex : DisplayStyle.None;

        UnityEngine.Cursor.visible = v;
        UnityEngine.Cursor.lockState = v ? CursorLockMode.None : CursorLockMode.Locked;

        if (pauseWhileOpen) Time.timeScale = v ? 0f : 1f;
    }
}
