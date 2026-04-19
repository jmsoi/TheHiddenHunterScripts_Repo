using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;

/// <summary>
/// 플레이어 컨트롤러 - 각 컴포넌트들을 조율하는 메인 컨트롤러
/// </summary>
[RequireComponent(typeof(PlayerStateManager))]
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerAnimationController))]
[RequireComponent(typeof(PlayerMining))]
[RequireComponent(typeof(PlayerCombat))]
[RequireComponent(typeof(PlayerHealth))]
[RequireComponent(typeof(PlayerShop))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Animator))]
public class PlayerController : NetworkBehaviour
{
    [Header("Component References")]
    public VariableJoystick variableJoystick;
    public Rigidbody playerRigidbody;
    public Animator playerAnimator;

    public Material myheadMaterial;
    public Material mybodyMaterial;
    public Material otherheadMaterial;
    public Material otherbodyMaterial;
    public SkinnedMeshRenderer playerHeadMeshRenderer;
    public SkinnedMeshRenderer playerBodyMeshRenderer;
    
    private PlayerStateManager stateManager;
    private PlayerMovement movement;
    private PlayerAnimationController animationController;
    private PlayerMining mining;
    private PlayerCombat combat;
    private PlayerHealth health;
    private PlayerShop shop;
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // 컴포넌트 참조 가져오기
        stateManager = GetComponent<PlayerStateManager>();
        movement = GetComponent<PlayerMovement>();
        animationController = GetComponent<PlayerAnimationController>();
        mining = GetComponent<PlayerMining>();
        combat = GetComponent<PlayerCombat>();
        health = GetComponent<PlayerHealth>();
        shop = GetComponent<PlayerShop>();
        playerRigidbody = GetComponent<Rigidbody>();
        playerAnimator = GetComponent<Animator>();

        // playerHeadMeshRenderer.material = isOwner ? myheadMaterial : otherheadMaterial;
        // playerBodyMeshRenderer.material = isOwner ? mybodyMaterial : otherbodyMaterial;
        if (IsOwner)
            OwnerInit();
        else
            OtherInit();
        
        Debug.Log($"Player {NetworkObjectId} OnNetworkSpawn - IsOwner: {IsOwner}, IsServer: {IsServer}");
    }
    
    public void OwnerInit()
    {
        playerHeadMeshRenderer.material = myheadMaterial;
        playerBodyMeshRenderer.material = mybodyMaterial;

        // UI 요소 찾기
        var canvas = GameObject.Find("Canvas");
        if (canvas != null)
        {
            var joystickTransform = canvas.transform.Find("Variable Joystick");
            if (joystickTransform != null)
            {
                variableJoystick = joystickTransform.GetComponent<VariableJoystick>();
                if (movement != null)
                {
                    movement.variableJoystick = variableJoystick;
                }
            }
        }
    }

    public void OtherInit()
    {
        playerHeadMeshRenderer.material = otherheadMaterial;
        playerBodyMeshRenderer.material = otherbodyMaterial;
    }


    // 공개 메서드들 - 외부에서 호출 가능
    public void Mining()
    {
        mining?.Mining();
    }
    
    public void CompletedMining()
    {
        mining?.CompletedMining();
    }
    
    public void Dead()
    {
        health?.Dead();
    }
    
    public PlayerState GetCurrentState()
    {
        return stateManager != null ? stateManager.CurrentState : PlayerState.Idle;
    }
}
