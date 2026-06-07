using UnityEngine;
using Unity.Netcode;

/// <summary>
/// 플레이어 애니메이션 관리 담당.
/// 공격 등은 PlayerState로 트리거만 맞추고, Idle 클립의 Animation Event <c>IdleState</c>에서 로직 상태를 Idle로 맞춤(A 방식).
/// </summary>
[RequireComponent(typeof(Animator))]
public class PlayerAnimationController : NetworkBehaviour
{
    private Animator playerAnimator;
    private PlayerStateManager stateManager;
    
    private void Awake()
    {
        playerAnimator = GetComponent<Animator>();
        stateManager = GetComponent<PlayerStateManager>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (stateManager != null)
            ApplyAnimatorForState(stateManager.networkState.Value, resetAttackTriggers: true);
    }
    
    private void OnEnable()
    {
        if (stateManager != null)
        {
            stateManager.networkState.OnValueChanged += OnStateChanged;
        }
    }
    
    private void OnDisable()
    {
        if (stateManager != null)
        {
            stateManager.networkState.OnValueChanged -= OnStateChanged;
        }
    }
    
    void OnStateChanged(PlayerState previousValue, PlayerState newValue)
    {
        Debug.Log($"Player {NetworkObjectId} OnStateChanged: {previousValue} -> {newValue}");
        
        if (playerAnimator == null)
        {
            playerAnimator = GetComponent<Animator>();
            if (playerAnimator == null)
            {
                Debug.LogError($"Player {NetworkObjectId}: Animator component not found!");
                return;
            }
        }
        
        ClearPreviousStateTriggers(previousValue);
        ApplyAnimatorForState(newValue, resetAttackTriggers: false);
    }

    void ClearPreviousStateTriggers(PlayerState previousValue)
    {
        if (playerAnimator == null) return;
        switch (previousValue)
        {
            case PlayerState.Mining:
                playerAnimator.ResetTrigger("Mining");
                break;
            case PlayerState.Attack_Knife:
                playerAnimator.ResetTrigger("Attack_Knife");
                break;
            case PlayerState.Attack_Gun:
                playerAnimator.ResetTrigger("Attack_Gun");
                break;
            case PlayerState.Attack_LandMine:
                playerAnimator.ResetTrigger("Attack_LandMine");
                break;
            case PlayerState.Dead:
                playerAnimator.ResetTrigger("Die");
                break;
        }
    }

    /// <param name="resetAttackTriggers">스폰 직후 등 — 이전 트리거 잔류 방지</param>
    void ApplyAnimatorForState(PlayerState state, bool resetAttackTriggers)
    {
        if (playerAnimator == null)
        {
            playerAnimator = GetComponent<Animator>();
            if (playerAnimator == null) return;
        }

        if (resetAttackTriggers)
        {
            playerAnimator.ResetTrigger("Attack_Knife");
            playerAnimator.ResetTrigger("Attack_Gun");
            playerAnimator.ResetTrigger("Attack_LandMine");
            playerAnimator.ResetTrigger("Mining");
            playerAnimator.ResetTrigger("Die");
        }

        switch (state)
        {
            case PlayerState.Idle:
                playerAnimator.SetInteger("Move", 0);
                break;
            case PlayerState.Walking:
                playerAnimator.SetInteger("Move", 1);
                break;
            case PlayerState.Mining:
                playerAnimator.SetTrigger("Mining");
                break;
            case PlayerState.Attack_Knife:
                playerAnimator.SetTrigger("Attack_Knife");
                break;
            case PlayerState.Attack_Gun:
                playerAnimator.SetTrigger("Attack_Gun");
                break;
            case PlayerState.Attack_LandMine:
                playerAnimator.SetTrigger("Attack_LandMine");
                break;
            case PlayerState.Dead:
                playerAnimator.SetTrigger("Die");
                break;
        }
    }
    
    public void SetTrigger(string triggerName)
    {
        if (playerAnimator != null)
        {
            playerAnimator.SetTrigger(triggerName);
        }
    }

    public void MineSound()
    {
        SoundManager.Instance?.PlayMiningAt(transform.position);
    }

    /// <summary>
    /// Idle 애니 클립에 넣은 Animation Event 함수명과 동일해야 합니다.
    /// 오너만 <see cref="PlayerStateManager.ChangeState"/>를 호출해 네트워크 상태를 Idle로 맞춥니다.
    /// </summary>
    public void IdleState()
    {
        if (!IsOwner || stateManager == null)
            return;
        stateManager.ChangeState(PlayerState.Idle);
    }
}
