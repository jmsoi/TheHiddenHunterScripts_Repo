using UnityEngine;
using Unity.Netcode;

/// <summary>
/// 플레이어 이동 담당 컴포넌트
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : NetworkBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = GameConstants.Player.DEFAULT_SPEED;
    public float maxSpeed = 3f;
    public float dragWhenMoving = 0f;
    public float dragWhenStopped = 1f;
    
    public VariableJoystick variableJoystick;
    private Rigidbody playerRigidbody;
    private PlayerStateManager stateManager;
    
    private void Awake()
    {
        playerRigidbody = GetComponent<Rigidbody>();
        stateManager = GetComponent<PlayerStateManager>();
    }
    
    public void FixedUpdate()
    {
        // Owner인 플레이어만 입력 처리
        if (!IsOwner) return;
        
        // 채굴 중에는 이동 입력 무시
        if (stateManager != null && stateManager.CurrentState == PlayerState.Mining)
        {
            // 수평 속도도 0으로 고정
            if (playerRigidbody != null)
            {
                var v = playerRigidbody.linearVelocity;
                playerRigidbody.linearVelocity = new Vector3(0, v.y, 0);
                playerRigidbody.linearDamping = dragWhenStopped;
            }
            return;
        }
        
        if (playerRigidbody == null)
        {
            playerRigidbody = GetComponent<Rigidbody>();
            if (playerRigidbody == null)
            {
                Debug.LogError($"Player {NetworkObjectId}: Rigidbody component not found!");
                return;
            }
        }
        
        if (variableJoystick == null) return;
        
        Vector3 direction = Vector3.forward * variableJoystick.Vertical + Vector3.right * variableJoystick.Horizontal;
        
        if (direction.sqrMagnitude > 0.001f)
        {
            direction = direction.normalized;
            transform.forward = direction;
            
            Vector3 currentVelocity = playerRigidbody.linearVelocity;
            Vector3 targetVelocity = direction * moveSpeed;
            Vector3 newVelocity = new Vector3(targetVelocity.x, currentVelocity.y, targetVelocity.z);
            
            if (newVelocity.magnitude > maxSpeed)
            {
                newVelocity = newVelocity.normalized * maxSpeed;
                newVelocity.y = currentVelocity.y;
            }
            
            playerRigidbody.linearVelocity = newVelocity;
            playerRigidbody.linearDamping = dragWhenMoving;
            
            // 이동 중일 때만 상태 변경 (다른 상태면 유지)
            if (stateManager != null && stateManager.CurrentState != PlayerState.Mining && 
                stateManager.CurrentState != PlayerState.Attack_Knife && 
                stateManager.CurrentState != PlayerState.Attack_Gun && 
                stateManager.CurrentState != PlayerState.Attack_LandMine &&
                stateManager.CurrentState != PlayerState.Dead)
            {
                stateManager.ChangeState(PlayerState.Walking);
            }
        }
        else
        {
            Vector3 currentVelocity = playerRigidbody.linearVelocity;
            playerRigidbody.linearVelocity = new Vector3(0, currentVelocity.y, 0);
            playerRigidbody.linearDamping = dragWhenStopped;
            
            // 정지 중일 때만 상태 변경 (다른 상태면 유지)
            if (stateManager != null && stateManager.CurrentState != PlayerState.Mining && 
                stateManager.CurrentState != PlayerState.Attack_Knife && 
                stateManager.CurrentState != PlayerState.Attack_Gun && 
                stateManager.CurrentState != PlayerState.Attack_LandMine &&
                stateManager.CurrentState != PlayerState.Dead)
            {
                stateManager.ChangeState(PlayerState.Idle);
            }
        }
    }
}

