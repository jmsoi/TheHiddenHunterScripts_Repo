using Unity.Netcode;
using UnityEngine;
using System.Collections;

/// <summary>
/// 플레이어 상태 관리 및 네트워크 동기화 담당
/// </summary>
public class PlayerStateManager : NetworkBehaviour
{
    public NetworkVariable<PlayerState> networkState = new NetworkVariable<PlayerState>();
    
    public PlayerState CurrentState => networkState.Value;
    

    private void OnEnable()
    {
        GetComponent<PlayerCombat>().OnFrozen += OnFrozen;
    }
    private void OnDisable()
    {
        GetComponent<PlayerCombat>().OnFrozen -= OnFrozen;
    }
    private void OnFrozen(float duration)
    {
        StartCoroutine(FrozenCoroutine(duration));
    }
    private IEnumerator FrozenCoroutine(float duration)
    {
        ChangeState(PlayerState.Idle);
        yield return new WaitForSeconds(duration);
        ChangeState(PlayerState.Idle);
    }

    public void ChangeState(PlayerState newState)
    {
        if (networkState.Value == newState)
            return;
        
        if (IsServer) networkState.Value = newState;
        else if (IsOwner) ChangeStateServerRpc(newState);
    }
    
    [ServerRpc]
    void ChangeStateServerRpc(PlayerState newState)
    {
        Debug.Log($"Player {NetworkObjectId} ChangeStateServerRpc: {networkState.Value} -> {newState}");
        networkState.Value = newState;
    }
}

