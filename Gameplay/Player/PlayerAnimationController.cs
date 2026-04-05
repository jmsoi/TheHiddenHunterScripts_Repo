using UnityEngine;
using Unity.Netcode;

/// <summary>
/// 플레이어 애니메이션 관리 담당
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
        
        // 이전 상태 정리
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
            case PlayerState.Dead:
                playerAnimator.ResetTrigger("Die");
                break;
        }
        
        // 새 상태 설정
        switch (newValue)
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
}

