using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 플레이어 상태 관리 및 네트워크 동기화 담당
/// </summary>
public class PlayerStateManager : NetworkBehaviour
{
    public NetworkVariable<PlayerState> networkState = new NetworkVariable<PlayerState>();
    
    public PlayerState CurrentState => networkState.Value;
    
    public void ChangeState(PlayerState newState)
    {
        if (networkState.Value == newState)
            return;
            
        Debug.Log($"Player {NetworkObjectId} ChangeState: {networkState.Value} -> {newState} (IsOwner: {IsOwner}, IsServer: {IsServer})");
            
        if (IsServer)
        {
            networkState.Value = newState;
        }
        else if (IsOwner)
        {
            ChangeStateServerRpc(newState);
        }
    }
    
    [ServerRpc]
    void ChangeStateServerRpc(PlayerState newState)
    {
        Debug.Log($"Player {NetworkObjectId} ChangeStateServerRpc: {networkState.Value} -> {newState}");
        networkState.Value = newState;
    }
}

