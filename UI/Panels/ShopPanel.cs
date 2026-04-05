using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopPanel : MonoBehaviour
{
    [Header("상점 UI")]
    public GameObject shopPanel;
    public Button[] shopButtons;
    public Image[] shopImages;
    // public TextMeshProUGUI[] shopNames;
    public TextMeshProUGUI[] shopCosts;
    // public TextMeshProUGUI[] shopDescriptions;
    
    [Header("상점 프레임")]
    public Sprite noFrame;
    public Sprite blueFrame;
    public Sprite redFrame;
    public Sprite yellowFrame;
    
    void Start()
    {
        SkillManager.Instance.OnSkillPurchased += OnSkillPurchased;
        ResourceManager.Instance.OnResourceChanged += OnResourceChanged;
        InitializeShopButtons();
    }
    
    void OnDestroy()
    {
        if (SkillManager.Instance != null)
            SkillManager.Instance.OnSkillPurchased -= OnSkillPurchased;
        if (ResourceManager.Instance != null)
            ResourceManager.Instance.OnResourceChanged -= OnResourceChanged;
    }
    
    void InitializeShopButtons()
    {
        var skills = SkillManager.Instance.skillDatabase.skills;
        
        for (int i = 0; i < shopButtons.Length && i < skills.Count; i++)
        {
            var skill = skills[i+1];
            int skillIndex = i+1;
            
            // 버튼 설정
            shopButtons[i].onClick.RemoveAllListeners();
            shopButtons[i].onClick.AddListener(() => OnShopItemClick(skillIndex));
            
            // UI 설정
            shopImages[i].sprite = skill.sprite;
            shopButtons[i].image.sprite = GetFrame(skill.resource_type);
            // shopNames[i].text = skill._name;
            shopCosts[i].text = skill.resource_amount.ToString();
            // shopDescriptions[i].text = skill.description;
        }
        
        // UpdateButtonStates();
    }
    
    void OnShopItemClick(int skillIndex)
    {
        if (SkillManager.Instance.PurchaseSkill(skillIndex))
        {
            Debug.Log($"스킬 {skillIndex} 구매 완료!");
            UpdateButtonStates();
        }
        else
        {
            Debug.Log("스킬 구매 실패");
        }
    }
    
    void OnSkillPurchased(int slotIndex, Skill skill)
    {
        // UpdateButtonStates();
    }
    
    void OnResourceChanged(ResourceType type, int amount)
    {
        //  UpdateButtonStates();
    }
    
    void UpdateButtonStates()
    {
        // var skills = SkillDatabase.Instance.skills;
        // bool hasEmptySlot = SkillManager.Instance.HasEmptySlot();
        
        // // 버튼 활성화/비활성화
        // for (int i = 0; i < shopButtons.Length && i < skills.Count; i++)
        // {
        //     var skill = skills[i];
        //     bool canAfford = ResourceManager.Instance.GetResource(skill.resource_type) >= skill.resource_amount;
        //     shopButtons[i].interactable = hasEmptySlot && canAfford;
        // }
    }
    
    public void ToggleShop()
    {
        shopPanel.SetActive(!shopPanel.activeSelf);
        if (shopPanel.activeSelf)
        {
            // UpdateButtonStates();
        }
    }
    
    Sprite GetFrame(ResourceType resourceType)
    {
        switch (resourceType)
        {
            case ResourceType.Blue: return blueFrame;
            case ResourceType.Red: return redFrame;
            case ResourceType.Yellow: return yellowFrame;
            default: return noFrame;
        }
    }
} 