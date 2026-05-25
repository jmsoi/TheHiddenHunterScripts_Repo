using Unity.Netcode;
using System.Collections.Generic;

/// <summary>
/// 스킬 구매(네트워크·자원·패시브 전역 잠금). 슬롯 반영은 SkillManager.TryAddSkillToFirstEmptySlot을 호출합니다.
/// </summary>
public class PlayerShop : NetworkBehaviour
{
    /// <summary>패시브(타입/인덱스 기준) 구매 잠금 — 서버에서 확정 후 SyncPassiveLockClientRpc로 모든 클라이언트에 반영.</summary>
    private static readonly HashSet<int> PassiveSkillLocks = new HashSet<int>();

    public static bool IsPassiveLocked(int passiveSkillIndex) => PassiveSkillLocks.Contains(passiveSkillIndex);

    public static void RegisterPassiveLock(int passiveSkillIndex) => PassiveSkillLocks.Add(passiveSkillIndex);

    private PlayerStateManager stateManager;
    
    private void Awake()
    {
        stateManager = GetComponent<PlayerStateManager>();
    }

    public void RequestPurchaseSkill(int skillIndex)
    {
        if (!IsOwner || SkillManager.Instance == null)
            return;

        // 호스트는 바로 서버 로직 수행
        if (IsServer)
        {
            ProcessPurchaseOnServer(skillIndex, OwnerClientId);
            return;
        }

        RequestPurchaseSkillServerRpc(skillIndex);
    }

    [ServerRpc(RequireOwnership = true)]
    private void RequestPurchaseSkillServerRpc(int skillIndex, ServerRpcParams serverRpcParams = default)
    {
        ProcessPurchaseOnServer(skillIndex, serverRpcParams.Receive.SenderClientId);
    }

    private void ProcessPurchaseOnServer(int skillIndex, ulong buyerClientId)
    {
        if (SkillManager.Instance == null || SkillManager.Instance.skillDatabase == null)
            return;
        if (skillIndex < 0 || skillIndex >= SkillManager.Instance.skillDatabase.skills.Count)
            return;

        var skill = SkillManager.Instance.skillDatabase.skills[skillIndex];
        if (skill.type == SkillType.Passive)
        {
            if (IsPassiveLocked(skill.index))
            {
                PurchaseResultClientRpc(false, skillIndex, new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new List<ulong> { buyerClientId }
                    }
                });
                return;
            }

            RegisterPassiveLock(skill.index);
            SyncPassiveLockClientRpc(skill.index);
        }

        PurchaseResultClientRpc(true, skillIndex, new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new List<ulong> { buyerClientId }
            }
        });
    }

    [ClientRpc]
    private void SyncPassiveLockClientRpc(int passiveIndex)
    {
        RegisterPassiveLock(passiveIndex);
    }

    [ClientRpc]
    private void PurchaseResultClientRpc(bool success, int skillIndex, ClientRpcParams clientRpcParams = default)
    {
        if (!IsOwner || SkillManager.Instance == null)
            return;

        if (!success)
        {
            UnityEngine.Debug.Log("스킬 구매 실패 (서버 잠금/검증 실패)");
            MessageManager.Enqueue(MessageManager.FormatPurchaseFailedPassiveLocked(skillIndex));
            return;
        }

        if (!TryApplyLocalPurchase(skillIndex, out var failKind))
        {
            switch (failKind)
            {
                case LocalPurchaseFailKind.Invalid:
                    MessageManager.Enqueue("스킬 구매에 실패했습니다.");
                    break;
                case LocalPurchaseFailKind.NotEnoughResource:
                    MessageManager.Enqueue(MessageManager.FormatPurchaseFailedNotEnoughResources(skillIndex));
                    break;
                case LocalPurchaseFailKind.NoEmptySlot:
                    MessageManager.Enqueue(MessageManager.FormatPurchaseFailedNoEmptySlot());
                    break;
            }
            UnityEngine.Debug.Log($"스킬 {skillIndex} 구매 실패 ({failKind})");
            return;
        }

        var skillData = SkillManager.Instance.skillDatabase.skills[skillIndex];
        if (skillData.type == SkillType.Passive)
            RequestPassivePurchaseAnnounceServerRpc(skillIndex);
        else
            MessageManager.Enqueue(MessageManager.FormatPurchaseSuccessLocal(skillIndex));

        SoundManager.Instance?.PlayShopPurchase();
        UnityEngine.Debug.Log($"스킬 {skillIndex} 구매 완료!");
    }

    [ServerRpc(RequireOwnership = true)]
    void RequestPassivePurchaseAnnounceServerRpc(int skillIndex)
    {
        AnnouncePassivePurchaseAllClientRpc(skillIndex);
    }

    [ClientRpc]
    void AnnouncePassivePurchaseAllClientRpc(int skillIndex)
    {
        MessageManager.Enqueue(MessageManager.FormatPassivePurchaseGlobal(skillIndex));
    }

    enum LocalPurchaseFailKind { None, Invalid, NotEnoughResource, NoEmptySlot }

    /// <summary>서버 승인 후 구매자 클라이언트에서 자원 차감 및 슬롯 장착.</summary>
    private bool TryApplyLocalPurchase(int skillIndex, out LocalPurchaseFailKind failKind)
    {
        failKind = LocalPurchaseFailKind.None;
        var db = SkillManager.Instance.skillDatabase;
        if (db == null || skillIndex < 0 || skillIndex >= db.skills.Count)
        {
            failKind = LocalPurchaseFailKind.Invalid;
            return false;
        }

        var skillData = db.skills[skillIndex];
        if (!ResourceManager.Instance.SpendResource(skillData.resource_type, skillData.resource_amount))
        {
            failKind = LocalPurchaseFailKind.NotEnoughResource;
            return false;
        }

        if (!SkillManager.Instance.TryAddSkillToFirstEmptySlot(skillData))
        {
            failKind = LocalPurchaseFailKind.NoEmptySlot;
            return false;
        }

        return true;
    }
    
    // public void Purchasing()
    // {
    //     if (stateManager.CurrentState == PlayerState.Purchasing)
    //     {
    //         stateManager.ChangeState(PlayerState.Idle);
    //     }
    //     else
    //     {
    //         stateManager.ChangeState(PlayerState.Purchasing);
    //     }
    // }
}
