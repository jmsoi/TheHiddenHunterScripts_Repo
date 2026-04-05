using UnityEngine;
using System;
using System.Collections.Generic;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance;
    
    [Header("자원 데이터")]
    public Dictionary<ResourceType, int> resources = new Dictionary<ResourceType, int>();
    
    // 이벤트
    public event Action<ResourceType, int> OnResourceChanged;
    public event Action<ResourceType, int> OnResourceSpent;
    

    public void TestAddResource()
    {
        AddResource(ResourceType.Blue, 10);
        AddResource(ResourceType.Red, 10);
        AddResource(ResourceType.Yellow, 10);
    }

    void Awake()
    {
        Instance = this;
        InitializeResources();
    }
    
    void InitializeResources()
    {
        foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
        {
            resources[type] = 0;
        }
    }
    
    public bool SpendResource(ResourceType type, int amount)
    {
        if (resources.ContainsKey(type) && resources[type] >= amount)
        {
            resources[type] -= amount;
            OnResourceChanged?.Invoke(type, resources[type]);
            OnResourceSpent?.Invoke(type, amount);
            return true;
        }
        return false;
    }
    
    public void AddResource(ResourceType type, int amount)
    {
        if (resources.ContainsKey(type))
            resources[type] += amount;
        else
            resources[type] = amount;
            
        OnResourceChanged?.Invoke(type, resources[type]);
    }
    
    public int GetResource(ResourceType type)
    {
        return resources.ContainsKey(type) ? resources[type] : 0;
    }
} 