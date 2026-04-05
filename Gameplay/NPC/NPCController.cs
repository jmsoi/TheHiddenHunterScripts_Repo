using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;

public class NPCController : NetworkBehaviour
{
    public enum NPCState { Idle, Tracking, Wandering, Mining, Purchasing, Dead }
    
    [Header("Network Synchronization")]
    public NetworkVariable<NPCState> currentState = new NetworkVariable<NPCState>();
    public NetworkVariable<int> targetObjectIndex = new NetworkVariable<int>();
    public NetworkVariable<Vector3> targetPosition = new NetworkVariable<Vector3>();
    
    [Header("NPC Settings")]
    public float moveSpeed = GameConstants.NPC.DEFAULT_SPEED;
    public float idleDuration = GameConstants.NPC.DEFAULT_IDLE_TIME;
    public float miningDuration = GameConstants.NPC.DEFAULT_MINING_TIME;
    public float trackingDuration = GameConstants.NPC.DEFAULT_TRACKING_TIME;
    public float wanderingDuration = GameConstants.NPC.DEFAULT_WANDERING_TIME;
    
    [Header("Components")]
    private Animator npcAnimator;
    private Rigidbody npcRigidbody;
    
    // State management
    private float stateTimer;
    private bool isStateInitialized = false;

    // ===== Unity 생명주기 =====
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        npcAnimator = GetComponent<Animator>();
        npcRigidbody = GetComponent<Rigidbody>();
        
        // NetworkVariable 값 변경 이벤트 구독
        currentState.OnValueChanged += OnStateChanged;
        
        Debug.Log("NPCController: 초기화 완료");
        
        if (IsServer)
        {
            StartCoroutine(NPC_Algorithm());
        }
    }
    
    void FixedUpdate()
    {
        // 이동·물리는 서버에서만 (클라이언트는 NetworkTransform 등으로 위치 동기화)
        if (IsServer && GameManager.Instance.isGameStarted && currentState.Value != NPCState.Dead)
        {
            if (currentState.Value == NPCState.Tracking || currentState.Value == NPCState.Wandering)
            {   
                MoveToTarget();
            }
            else if (currentState.Value == NPCState.Mining || currentState.Value == NPCState.Purchasing)
            {
                // Mining과 Purchasing 상태에서는 이동 중지
                if (npcRigidbody != null)
                {
                    npcRigidbody.linearVelocity = new Vector3(0, npcRigidbody.linearVelocity.y, 0);
                }
            }
        }
        
        // 상태 타이머 업데이트 (서버에서만)
        if (IsServer && stateTimer > 0)
        {
            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0)
            {
                OnStateTimeout();
            }
        }
    }
    
    void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;
        
        if (collision.gameObject.TryGetComponent<MapGameObject>(out MapGameObject mapGameObject) && mapGameObject.mapObject.IsResource())
        {
            targetObjectIndex.Value = mapGameObject.idx;
            targetPosition.Value = mapGameObject.transform.position;
            ChangeState(NPCState.Mining);
        }
        else if (collision.gameObject.CompareTag("Attack"))
        {
            ChangeState(NPCState.Dead);
        }
    }

    // ===== AI 알고리즘 =====
    
    IEnumerator NPC_Algorithm()
    {
        Debug.Log("NPCController: 알고리즘 시작");
        yield return new WaitUntil(() => GameManager.Instance.isGameStarted);
        
        // 초기 상태 설정
        ChangeState(NPCState.Idle);
        
        while (currentState.Value != NPCState.Dead)
        {
            if (!isStateInitialized)
            {
                InitializeState();
            }
            
            // 상태별 로직 실행
            switch (currentState.Value)
            {
                case NPCState.Idle:
                    yield return new WaitForSeconds(0.5f);
                    break;
                case NPCState.Tracking:
                    yield return new WaitForSeconds(0.5f);
                    break;
                case NPCState.Wandering:
                    yield return new WaitForSeconds(0.5f);
                    break;
                case NPCState.Mining:
                    yield return new WaitForSeconds(0.5f);
                    break;
                case NPCState.Purchasing:
                    yield return new WaitForSeconds(0.5f);
                    break;
            }
        }
    }

    // ===== 상태 관리 =====
    
    void InitializeState()
    {
        switch (currentState.Value)
        {
            case NPCState.Idle:
                stateTimer = idleDuration;
                // Debug.Log("NPC: 대기 상태 초기화");
                break;
            case NPCState.Tracking:
                InitializeTracking();
                break;
            case NPCState.Wandering:
                InitializeWandering();
                break;
            case NPCState.Mining:
                stateTimer = miningDuration;
                // Debug.Log("NPC: 채굴 시작");
                break;
            case NPCState.Purchasing:
                stateTimer = Random.Range(2f, 5f);
                // Debug.Log("NPC: 구매 시작");
                break;
        }
        isStateInitialized = true;
    }
    
    void InitializeTracking()
    {
        // 광물 타입 랜덤 선택
        ResourceType[] resourceTypes = { ResourceType.Blue, ResourceType.Red, ResourceType.Yellow };
        ResourceType targetResourceType = resourceTypes[Random.Range(0, resourceTypes.Length)];
        
        // 가장 가까운 광물 위치 찾기
        (int idx, Vector3 mineralPosition) = MapManager.Instance.GetMineralNearestPosition(targetResourceType, transform.position);
        
        if (idx != -1)
        {
            stateTimer = trackingDuration;
            targetObjectIndex.Value = idx;
            targetPosition.Value = mineralPosition;
            // Debug.Log($"NPC: {targetResourceType} 추적 시작");
        }
        else
        {
            ChangeState(NPCState.Idle);
            return;
        }
    }
    
    void InitializeWandering()
    {
        // 랜덤한 방향으로 이동
        Vector3 randomDirection = Random.insideUnitSphere * 10f;
        randomDirection.y = 0;
        Vector3 wanderTarget = transform.position + randomDirection;
        targetPosition.Value = wanderTarget;
        
        stateTimer = wanderingDuration;
        // Debug.Log("NPC: 탐색 시작");
    }
    
    void OnStateTimeout()
    {
        switch (currentState.Value)
        {
            case NPCState.Idle:
                // 70% 확률로 추적, 30% 확률로 탐색
                if (Random.Range(0f, 1f) < 0.7f && MapManager.Instance.GetAllMineralCount() > 0)
                {
                    // Debug.Log("NPC: 추적 시작");
                    ChangeState(NPCState.Tracking);
                }
                else
                {
                    // Debug.Log("NPC: 탐색 시작");
                    ChangeState(NPCState.Wandering);
                }
                break;
            case NPCState.Tracking:
                ChangeState(NPCState.Idle);
                break;
            case NPCState.Wandering:
                ChangeState(NPCState.Idle);
                break;
            case NPCState.Mining:
                CompletedMining();
                break;
            case NPCState.Purchasing:
                ChangeState(NPCState.Idle);
                break;
        }
    }
    
    void ChangeState(NPCState newState)
    {
        // 서버에서만 NetworkVariable 수정
        if (IsServer)
        {
            currentState.Value = newState;
            isStateInitialized = false;
            // Debug.Log($"NPC: 상태 변경 -> {newState}");
        }
        // 클라이언트에서는 아무것도 하지 않음 (서버가 자동으로 처리)
    }

    public void Dead()
    {
        ChangeState(NPCState.Dead);
    }

    // ===== 이동 시스템 =====
    
    void MoveToTarget()
    {
        Vector3 direction = targetPosition.Value - transform.position;
        
        // Y축 회전만
        Vector3 directionY = new Vector3(direction.x, 0, direction.z);
        if (directionY.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionY);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 0.1f);
        }
        
        // Rigidbody 이동
        if (npcRigidbody != null)
        {
            Vector3 moveDirection = new Vector3(direction.x, 0, direction.z).normalized;
            npcRigidbody.linearVelocity = new Vector3(moveDirection.x * moveSpeed, npcRigidbody.linearVelocity.y, moveDirection.z * moveSpeed);
        }
    }

    // ===== 채굴 시스템 =====
    
    void CompletedMining()
    {
        // 서버에서만 실행
        if (IsServer)
        {
            if (targetObjectIndex.Value != -1)
            {
                // MapObject mapObject = (MapObject)MapManager.Instance.serializedMapData[targetObjectIndex.Value];
                // ResourceManager.Instance.AddResource(mapObject.ToResourceType(), 1);
                // Debug.Log("NPC: 광산 채굴 완료");
                
                MapManager.Instance.serializedMapData[targetObjectIndex.Value] = (int)MapObject.None;
                MapManager.Instance.DestroyPointClientRpc(targetObjectIndex.Value);
            }
            ChangeState(NPCState.Idle);
        }
        // 클라이언트에서는 아무것도 하지 않음 (서버가 자동으로 처리)
    }

    // ===== 네트워크 콜백 =====
    
    void OnStateChanged(NPCState previousValue, NPCState newValue)
    {
        // 모든 클라이언트에서 애니메이션 설정
        if (npcAnimator != null)
        {
            switch (newValue)
            {
                case NPCState.Idle:
                    npcAnimator.SetInteger("state", 0);
                    break;
                case NPCState.Tracking:
                case NPCState.Wandering:
                    npcAnimator.SetInteger("state", 1);
                    break;
                case NPCState.Mining:
                    npcAnimator.SetInteger("state", 2);
                    break;
                case NPCState.Purchasing:
                    npcAnimator.SetInteger("state", 3);
                    break;
                case NPCState.Dead:
                    npcAnimator.SetTrigger("isDead");
                    break;
            }
        }
    }
}
