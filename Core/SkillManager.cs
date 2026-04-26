using UnityEngine;
using System;

public class SkillManager : MonoBehaviour
{
    public static SkillManager Instance;
    
    [Header("스킬 데이터")]
    public Skill[] playerSkills = new Skill[2]; // 2개 슬롯만
    public SkillDatabase skillDatabase;
    // 이벤트
    public event Action<int, Skill> OnSkillPurchased;
    public event Action<int> OnSkillUsed;
    public event Action<int> OnSkillCooldownEnded;
    
    void Awake()
    {
        Instance = this;
        InitializeSkills();
    }
    
    void Update()
    {
        // 모든 플레이어 스킬의 쿨다운 업데이트
        for (int i = 0; i < playerSkills.Length; i++)
        {
            if (playerSkills[i] != null)
            {
                playerSkills[i].UpdateCooldown(Time.deltaTime);
            }
        }
    }
    
    void InitializeSkills()
    {
        for (int i = 0; i < playerSkills.Length; i++)
        {
            playerSkills[i] = new Skill();
            playerSkills[i].currentCooldown = 0;
        }
    }

    /// <summary>
    /// 빈 슬롯에 스킬을 넣습니다. 자원 차감·상점/패시브 잠금은 호출 쪽(PlayerShop 등)에서 처리합니다.
    /// </summary>
    public bool TryAddSkillToFirstEmptySlot(Skill skillDefinition)
    {
        if (skillDefinition == null || skillDatabase == null)
            return false;

        int emptySlot = -1;
        for (int i = 0; i < playerSkills.Length; i++)
        {
            if (playerSkills[i].type == SkillType.None && playerSkills[i].currentCooldown <= 0f)
            {
                emptySlot = i;
                break;
            }
        }

        if (emptySlot == -1)
        {
            Debug.Log("사용 가능한 스킬 슬롯이 없습니다 (쿨다운 중이거나 슬롯이 가득 찬 경우)");
            return false;
        }

        playerSkills[emptySlot] = skillDefinition;
        OnSkillPurchased?.Invoke(emptySlot, skillDefinition);
        return true;
    }
    
    public bool UseSkill(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= playerSkills.Length)
            return false;
        if (playerSkills[slotIndex].type == SkillType.Passive)
        {
            Debug.Log($"패시브 스킬은 수동 사용할 수 없습니다: {playerSkills[slotIndex]._name}");
            return false;
        }

        Debug.Log("UseSkill: " + playerSkills[slotIndex].CanUse());
        if (slotIndex >= 0 && slotIndex < playerSkills.Length && playerSkills[slotIndex].CanUse())
        {
            playerSkills[slotIndex].Use();
            OnSkillUsed?.Invoke(slotIndex);
            // 스킬을 None으로 변경하되 쿨다운 정보는 유지
            // var usedSkill = playerSkills[slotIndex];
            // playerSkills[slotIndex] = new Skill();
            // playerSkills[slotIndex].currentCooldown = usedSkill.currentCooldown; // 쿨다운 정보 유지
            return true;
        }
        return false;
    }

    public bool HasPassiveSkill(int passiveIndex)
    {
        for (int i = 0; i < playerSkills.Length; i++)
        {
            if (playerSkills[i] != null &&
                playerSkills[i].type == SkillType.Passive &&
                playerSkills[i].index == passiveIndex)
            {
                return true;
            }
        }
        return false;
    }
    
    public void ResetSkill(int slotIndex)
    {
        playerSkills[slotIndex] = skillDatabase.skills[0];
        playerSkills[slotIndex].currentCooldown = 0;
        OnSkillCooldownEnded?.Invoke(slotIndex);
    }

    public bool HasEmptySlot()
    {
        for (int i = 0; i < playerSkills.Length; i++)
        {
            if (playerSkills[i].type == SkillType.None && playerSkills[i].currentCooldown <= 0f)
                return true;
        }
        return false;
    }
    
    public Skill GetSkill(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < playerSkills.Length)
            return playerSkills[slotIndex];
        return null;
    }
}
