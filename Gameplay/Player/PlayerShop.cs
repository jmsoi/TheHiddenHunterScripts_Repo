using Unity.Netcode;

/// <summary>
/// 플레이어 구매 로직 담당
/// </summary>
public class PlayerShop : NetworkBehaviour
{
    private PlayerStateManager stateManager;
    
    private void Awake()
    {
        stateManager = GetComponent<PlayerStateManager>();
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

