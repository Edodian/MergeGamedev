#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif
using UnityEngine;

public static class InputCompat
{
    public static bool GetKeyDown(KeyCode key)
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        var k = Keyboard.current; if (k == null) return false;
        return key switch
        {
            KeyCode.Tab => k.tabKey.wasPressedThisFrame,
            KeyCode.R => k.rKey.wasPressedThisFrame,
            KeyCode.E => k.eKey.wasPressedThisFrame,
            _ => false
        };
#else
        return Input.GetKeyDown(key);
#endif
    }

    public static bool GetMouseButtonUp(int button)
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        var m = Mouse.current; if (m == null) return false;
        return button switch
        {
            0 => m.leftButton.wasReleasedThisFrame,
            1 => m.rightButton.wasReleasedThisFrame,
            2 => m.middleButton.wasReleasedThisFrame,
            _ => false
        };
#else
        return Input.GetMouseButtonUp(button);
#endif
    }

    public static Vector2 MousePosition()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        var m = Mouse.current; return m != null ? m.position.ReadValue() : Vector2.zero;
#else
        return Input.mousePosition;
#endif
    }
}
