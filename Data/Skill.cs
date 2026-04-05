using UnityEngine;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif


public enum SkillType { None, Attack, Move, Passive }


[System.Serializable]
public class Skill
{
    [Header("스킬 정보")]
    public SkillType type;
    public int index;
    public string _name;
    public ResourceType resource_type;
    public int resource_amount;
    
    [Header("스킬 설정")]
    public float cooldown;
    public float currentCooldown;
    public Sprite sprite;
    [TextArea(3, 5)]
    public string description;

    public Skill()
    {
        type = SkillType.None;
        index = 0;
        _name = "None";
        resource_type = ResourceType.None;
        resource_amount = 0;
        cooldown = 0f;
        currentCooldown = 0f;
        sprite = null;
        description = "";
    }
    
    public Skill(SkillType type, int index, string _name, float cooldown, ResourceType resource_type, int resource_amount, Sprite sprite, string description)
    {
        this.type = type;
        this.index = index;
        this._name = _name;
        this.resource_type = resource_type;
        this.resource_amount = resource_amount;
        this.cooldown = cooldown;
        this.currentCooldown = 0f;
        this.sprite = sprite;
        this.description = description;
    }
    
    // 스킬 사용 가능 여부 확인
    public bool CanUse()
    {
        return type != SkillType.None && currentCooldown <= 0f;
    }
    
    // 스킬 사용 (쿨다운 시작)
    public void Use()
    {
        if (CanUse())
        {
            currentCooldown = cooldown;
        }
    }
    
    // 쿨다운 업데이트
    public void UpdateCooldown(float deltaTime)
    {
        if (currentCooldown > 0f)
        {
            currentCooldown -= deltaTime;
            if (currentCooldown <= 0f)
            {
                currentCooldown = 0f;
            }
        }
    }
}