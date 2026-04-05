using UnityEngine;
using TMPro;

public class ResourcePanel : MonoBehaviour
{
    [Header("자원 UI")]
    public TextMeshProUGUI blueResourceText;
    public TextMeshProUGUI redResourceText;
    public TextMeshProUGUI yellowResourceText;
    
    void Start()
    {
        ResourceManager.Instance.OnResourceChanged += UpdateResourceDisplay;
        UpdateAllResources();
    }
    
    void OnDestroy()
    {
        if (ResourceManager.Instance != null)
            ResourceManager.Instance.OnResourceChanged -= UpdateResourceDisplay;
    }
    
    void UpdateResourceDisplay(ResourceType type, int amount)
    {
        switch (type)
        {
            case ResourceType.Blue:
                blueResourceText.text = amount.ToString();
                break;
            case ResourceType.Red:
                redResourceText.text = amount.ToString();
                break;
            case ResourceType.Yellow:
                yellowResourceText.text = amount.ToString();
                break;
        }
    }
    
    void UpdateAllResources()
    {
        var resources = ResourceManager.Instance.resources;
        blueResourceText.text = resources[ResourceType.Blue].ToString();
        redResourceText.text = resources[ResourceType.Red].ToString();
        yellowResourceText.text = resources[ResourceType.Yellow].ToString();
    }
} 