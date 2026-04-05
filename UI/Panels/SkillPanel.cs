using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class SkillPanel : MonoBehaviour
{
    [Header("스킬 UI")]
    public Button[] skillButtons = new Button[2];
    public Image[] skillImages = new Image[2];
    
    [Header("쿨다운 UI")]
    public TextMeshProUGUI[] cooldownTexts = new TextMeshProUGUI[2]; // 쿨다운 텍스트
    
    [Header("스킬 프레임")]
    public Sprite noFrame;
    public Sprite blueSkillFrame;
    public Sprite redSkillFrame;
    public Sprite yellowSkillFrame;

    void Start()
    {
        SkillManager.Instance.OnSkillPurchased += UpdateSkillDisplay;
        SkillManager.Instance.OnSkillUsed += OnSkillUsed;
        SkillManager.Instance.OnSkillCooldownEnded += OnSkillCooldownEnded;
        // 버튼 이벤트 연결
        for (int i = 0; i < skillButtons.Length; i++)
        {
            int slotIndex = i; // 클로저 문제 해결
            skillButtons[i].onClick.AddListener(() => OnSkillButtonClick(slotIndex));
        }

        for (int i = 0; i < skillImages.Length; i++)
        {
            skillImages[i].sprite = SkillManager.Instance.skillDatabase.skills[0].sprite;
            skillButtons[i].image.sprite = noFrame;
            cooldownTexts[i].text = "";
            cooldownTexts[i].gameObject.SetActive(false);
        }
    }
    
    void OnDestroy()
    {
        if (SkillManager.Instance != null)
        {
            SkillManager.Instance.OnSkillPurchased -= UpdateSkillDisplay;
            SkillManager.Instance.OnSkillUsed -= OnSkillUsed;
            SkillManager.Instance.OnSkillCooldownEnded -= OnSkillCooldownEnded;
        }
    }
    
    void OnSkillButtonClick(int slotIndex)
    {
        SkillManager.Instance.UseSkill(slotIndex);
    }
    
    void UpdateSkillDisplay(int slotIndex, Skill skill)
    {
        if (slotIndex < skillImages.Length)
        {
            skillImages[slotIndex].sprite = skill.sprite;
            skillButtons[slotIndex].image.sprite = GetFrame(skill.resource_type);
        }
    }
    Sprite GetFrame(ResourceType resourceType)
    {
        switch (resourceType)
        {
            case ResourceType.Blue: return blueSkillFrame;
            case ResourceType.Red: return redSkillFrame;
            case ResourceType.Yellow: return yellowSkillFrame;
            default: return noFrame;
        }
    }
    
    void OnSkillUsed(int slotIndex)
    {
        Debug.Log($"스킬 {slotIndex} 사용됨");
        StartCoroutine(CooldownCoroutine(slotIndex));
    }
    
    IEnumerator CooldownCoroutine(int slotIndex)
    {
        Debug.Log($"스킬 {slotIndex} 쿨다운 시작");
        while (SkillManager.Instance.GetSkill(slotIndex).currentCooldown > 0f)
        {
            yield return null;
            SkillManager.Instance.GetSkill(slotIndex).UpdateCooldown(Time.deltaTime);
            cooldownTexts[slotIndex].text = SkillManager.Instance.GetSkill(slotIndex).currentCooldown.ToString("F1");
            skillImages[slotIndex].fillAmount = SkillManager.Instance.GetSkill(slotIndex).currentCooldown / SkillManager.Instance.GetSkill(slotIndex).cooldown;
            cooldownTexts[slotIndex].gameObject.SetActive(true);
        }   
        SkillManager.Instance.ResetSkill(slotIndex);
    }

    void OnSkillCooldownEnded(int slotIndex)
    {
        Debug.Log($"스킬 {slotIndex} 쿨다운 종료");
        skillImages[slotIndex].sprite = SkillManager.Instance.skillDatabase.skills[0].sprite;
        skillButtons[slotIndex].image.sprite = noFrame;
        skillImages[slotIndex].fillAmount = 1f;
        cooldownTexts[slotIndex].text = "";
    }
} 