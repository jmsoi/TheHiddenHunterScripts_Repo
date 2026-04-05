using UnityEngine;
using Unity.Netcode;

/// <summary>
/// 플레이어 생명력 및 죽음 처리 담당
/// </summary>
public class PlayerHealth : NetworkBehaviour
{
    private PlayerStateManager stateManager;
    
    private void Awake()
    {
        stateManager = GetComponent<PlayerStateManager>();
    }
    
    public void Dead()
    {
        // 서버에서 호출되거나 Owner가 직접 호출할 수 있음
        stateManager.ChangeState(PlayerState.Dead);
        
        // Owner의 클라이언트에서 패배 화면 표시
        if (IsOwner)
        {
            GameManager.Instance.EndGameSession(false);
        }
        else if (IsServer)
        {
            // 서버에서 호출된 경우, Owner 클라이언트에 알림
            NotifyDeathClientRpc();
        }
    }
    
    [ClientRpc]
    void NotifyDeathClientRpc()
    {
        // 피격자의 클라이언트에서 패배 화면 표시
        if (IsOwner)
        {
            GameManager.Instance.EndGameSession(false);
        }
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        // Owner인 플레이어만 처리 (자기 자신의 캐릭터만)
        if (!IsOwner) return;
        
        if (collision.gameObject.CompareTag("Attack"))
        {
            Dead();
        }
    }
}

