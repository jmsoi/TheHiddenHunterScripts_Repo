using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// 왼쪽 상단에 짧은 게임 메시지(구매·스킬 사용 등)를 표시합니다.
/// 새 메시지가 오면 기존 내용을 바로 덮어쓰고, 표시 시간 타이머만 다시 시작합니다.
/// </summary>
public class MessageManager : MonoBehaviour
{
    public static MessageManager Instance { get; private set; }

    [SerializeField] GameObject messagePanel;
    [SerializeField] TextMeshProUGUI messageLabel;
    [SerializeField] float messageDuration = 4f;

    /// <summary>현재 화면에 반영 중인 메시지(빈 문자열이면 비표시).</summary>
    string _currentMessage = string.Empty;
    Coroutine _hideAfterDelayRoutine;

    public static void Enqueue(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;
        if (Instance == null)
        {
            var go = new GameObject(nameof(MessageManager));
            go.AddComponent<MessageManager>();
        }
        Instance.ShowMessageInternal(message);
    }

    public static string SkillNameFromDatabase(int skillDatabaseIndex)
    {
        var db = SkillManager.Instance != null ? SkillManager.Instance.skillDatabase : null;
        if (db == null || skillDatabaseIndex < 0 || skillDatabaseIndex >= db.skills.Count)
            return "알 수 없는 스킬";
        return db.skills[skillDatabaseIndex]._name;
    }

    /// <summary>내 클라이언트에만 보이는 구매 성공 문구.</summary>
    public static string FormatPurchaseSuccessLocal(int skillDatabaseIndex)
    {
        var name = SkillNameFromDatabase(skillDatabaseIndex);
        return $"{name}을(를) 구매했습니다.";
    }

    /// <summary>패시브 구매 시 모든 클라이언트에 표시.</summary>
    public static string FormatPassivePurchaseGlobal(int skillDatabaseIndex)
    {
        var db = SkillManager.Instance != null ? SkillManager.Instance.skillDatabase : null;
        if (db != null && skillDatabaseIndex >= 0 && skillDatabaseIndex < db.skills.Count)
        {
            var skill = db.skills[skillDatabaseIndex];
            if (skill.type == SkillType.Passive && skill.index == 0)
            {
                int amount = GameConstants.Player.RESOURCE_MASTER_WIN_AMOUNT;
                return $"누군가가 {skill._name}을(를) 구매했습니다. Blue·Red·Yellow 자원을 각 {amount}개씩 획득하면 승리합니다.";
            }
        }

        var name = SkillNameFromDatabase(skillDatabaseIndex);
        return $"누군가가 {name}을(를) 구매했습니다.";
    }

    /// <summary>내 클라이언트에만 보이는 구매 실패(패시브 선점).</summary>
    public static string FormatPurchaseFailedPassiveLocked(int skillDatabaseIndex)
    {
        var name = SkillNameFromDatabase(skillDatabaseIndex);
        return $"{name}은(는) 이미 다른 플레이어가 구매해 구매할 수 없습니다.";
    }

    public static string FormatPurchaseFailedNotEnoughResources(int skillDatabaseIndex)
    {
        var name = SkillNameFromDatabase(skillDatabaseIndex);
        return $"{name} 구매에 필요한 자원이 부족합니다.";
    }

    public static string FormatPurchaseFailedNoEmptySlot()
    {
        return "비어 있는 스킬 슬롯이 없어 구매할 수 없습니다.";
    }

    /// <summary>0=검, 1=활 (공격 결과 메시지용)</summary>
    public const byte CombatWeaponKnife = 0;
    public const byte CombatWeaponGun = 1;

    /// <summary>0=빗나감, 1=NPC 처치, 2=플레이어 처치, 3=장애물에 막힘(활만)</summary>
    public const byte CombatOutcomeMiss = 0;
    public const byte CombatOutcomeNpcKill = 1;
    public const byte CombatOutcomePlayerKill = 2;
    public const byte CombatOutcomeBlocked = 3;

    public static string FormatCombatResult(byte weapon, byte outcome)
    {
        bool isGun = weapon == CombatWeaponGun;
        switch (outcome)
        {
            case CombatOutcomeMiss:
                return isGun
                    ? "누군가가 총을 쐈으나 빗나갔습니다."
                    : "누군가가 검을 휘뒀으나 빗나갔습니다.";
            case CombatOutcomeNpcKill:
                return isGun
                    ? "누군가가 총으로 NPC를 처치했습니다."
                    : "누군가가 검으로 NPC를 처치했습니다.";
            case CombatOutcomePlayerKill:
                return isGun
                    ? "누군가가 총으로 다른 플레이어를 처치했습니다."
                    : "누군가가 검으로 다른 플레이어를 처치했습니다.";
            case CombatOutcomeBlocked:
                return "누군가가 총을 쐈으나 장애물에 막혀 빗나갔습니다.";
            default:
                return string.Empty;
        }
    }

    public static string FormatSkillUsed(int skillDatabaseIndex)
    {
        var name = SkillNameFromDatabase(skillDatabaseIndex);
        return name switch
        {
            "지뢰" => "누군가가 지뢰를 설치했습니다.",
            "은신+이속 증가" => "누군가가 은신했습니다.",
            "암흑 시야" => "누군가가 시야를 가렸습니다.",
            "동결" => "누군가에 의해 모두가 얼었습니다.",
            _ => string.Empty,
        };
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
        if (_hideAfterDelayRoutine != null)
        {
            StopCoroutine(_hideAfterDelayRoutine);
            _hideAfterDelayRoutine = null;
        }
    }

    void ShowMessageInternal(string message)
    {
        _currentMessage = message;
        if (messageLabel != null)
            messageLabel.text = message;
        if (messagePanel != null)
            messagePanel.SetActive(true);

        if (_hideAfterDelayRoutine != null)
            StopCoroutine(_hideAfterDelayRoutine);
        _hideAfterDelayRoutine = StartCoroutine(HideAfterDelay());
    }

    IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(messageDuration);
        _currentMessage = string.Empty;
        if (messageLabel != null)
            messageLabel.text = string.Empty;
        if (messagePanel != null)
            messagePanel.SetActive(false);
        _hideAfterDelayRoutine = null;
    }
}
