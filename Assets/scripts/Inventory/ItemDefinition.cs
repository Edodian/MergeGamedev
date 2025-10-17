using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(menuName = "Inv/Item", fileName = "NewItem")]
public class ItemDefinition : ScriptableObject
{
    [SerializeField, HideInInspector] private string id;
    public string Id => id;

    [Header("Presentation")]
    public string displayName;
    public AssetReferenceT<Sprite> icon;
    public AssetReferenceGameObject worldPrefab; 

    [Header("Grid (cells)")]
    [Min(1)] public int gridWidth = 1; 
    [Min(1)] public int gridHeight = 1;
    public bool canRotate = true;

    [Header("Gameplay")]
    [Min(1)] public int maxStack = 1; 
    [Min(0.01f)] public float weightKg = 1f;

#if UNITY_EDITOR
    void OnValidate()
    {
        if (string.IsNullOrEmpty(id))
            id = System.Guid.NewGuid().ToString();
        gridWidth = Mathf.Max(1, gridWidth);
        gridHeight = Mathf.Max(1, gridHeight);
        maxStack = Mathf.Max(1, maxStack);
        weightKg = Mathf.Max(0.01f, weightKg);
    }
#endif
}
