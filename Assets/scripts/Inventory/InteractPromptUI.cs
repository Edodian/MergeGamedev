using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class InteractPromptUI : MonoBehaviour
{
    static Label sLabel;

    void Awake()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        sLabel = new Label();
        sLabel.style.position = Position.Absolute;
        sLabel.style.left = 0; sLabel.style.right = 0; sLabel.style.bottom = 20;
        sLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        sLabel.style.fontSize = 16;
        sLabel.style.color = Color.white;
        sLabel.style.backgroundColor = new Color(0, 0, 0, 0.45f);
        sLabel.style.marginLeft = 200; sLabel.style.marginRight = 200;
        sLabel.style.paddingLeft = 8; sLabel.style.paddingRight = 8;
        sLabel.style.display = DisplayStyle.None;
        root.Add(sLabel);
    }

    public static void SetPrompt(string text)
    {
        if (sLabel == null) return;
        sLabel.text = text;
        sLabel.style.display = string.IsNullOrEmpty(text) ? DisplayStyle.None : DisplayStyle.Flex;
    }
}
