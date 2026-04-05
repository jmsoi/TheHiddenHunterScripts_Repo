using UnityEngine;
using Unity.Netcode;

/// <summary>
/// 플레이어 전투 및 스킬 사용 담당
/// </summary>
public class PlayerCombat : NetworkBehaviour
{
    [Header("Combat Settings")]
    public LayerMask characterLayerMask = 1 << 3; // 레이어 0, 1 (1 + 2 = 3)
    public LayerMask blockLayerMask = 1 << 7; // Block 레이어 (7번째 레이어)
    
    private PlayerStateManager stateManager;
    private PlayerAnimationController animationController;
    public GameObject bulletPrefab;
    
    private void Awake()
    {
        stateManager = GetComponent<PlayerStateManager>();
        animationController = GetComponent<PlayerAnimationController>();
    }
    
    private void OnEnable()
    {
        if (SkillManager.Instance != null)
        {
            SkillManager.Instance.OnSkillUsed += OnSkillUsed;
        }
    }
    
    private void OnDisable()
    {
        if (SkillManager.Instance != null)
        {
            SkillManager.Instance.OnSkillUsed -= OnSkillUsed;
        }
    }
    
    void OnSkillUsed(int slotIndex)
    {
        if (!IsOwner) return;
        if (SkillManager.Instance == null) return;
        
        Skill skill = SkillManager.Instance.GetSkill(slotIndex);
        
        if (skill.type == SkillType.Attack)
        {
            switch (skill.index)
            {
                case 0:
                    stateManager.ChangeState(PlayerState.Attack_Knife);
                    KnifeAttack();
                    break;
                case 1:
                    stateManager.ChangeState(PlayerState.Attack_Gun);
                    GunAttack();
                    break;
                case 2:
                    stateManager.ChangeState(PlayerState.Attack_LandMine);
                    LandMineAttack();
                    break;
            }
        }
        else if (skill.type == SkillType.Move)
        {
            switch (skill.index)
            {
                case 0:
                    animationController?.SetTrigger("Hide");
                    break;
                case 1:
                    animationController?.SetTrigger("BlindZone");
                    break;
                case 2:
                    animationController?.SetTrigger("ShadowReturn");
                    break;
            }
        }
        else if (skill.type == SkillType.Passive)
        {
            switch (skill.index)
            {
                case 0:
                    animationController?.SetTrigger("ResourceMaster");
                    break;
                case 1:
                    animationController?.SetTrigger("NPC_Killer");
                    break;
                case 2:
                    animationController?.SetTrigger("Shield");
                    break;
            }
        }
    }
    
    public void KnifeAttack()
    {
        // 서버에서만 공격 검증 및 처리
        if (IsServer)
        {
            ExecuteKnifeAttack();
        }
        else if (IsOwner)
        {
            // 클라이언트에서는 서버에 요청
            KnifeAttackServerRpc();
        }
    }
    
    [ServerRpc]
    void KnifeAttackServerRpc()
    {
        ExecuteKnifeAttack();
    }
    
    private void ExecuteKnifeAttack()
    {
        Debug.Log($"Player {NetworkObjectId} KnifeAttack 시작");
        
        Vector3 boxCenter = transform.position + transform.TransformDirection(new Vector3(0, 1, 1));
        Collider[] hitColliders = Physics.OverlapBox(boxCenter, new Vector3(0.5f, 1, 1) * 0.5f, Quaternion.identity, characterLayerMask);
        
        if (hitColliders.Length > 0)
        {
            GameObject nearObj = null;
            float nearObjDistance = 100;
            
            for (int i = 0; i < hitColliders.Length; i++)
            {
                if (hitColliders[i].CompareTag("NPC") || hitColliders[i].CompareTag("Player"))
                {
                    // 자기 자신은 공격 대상에서 제외
                    if (hitColliders[i].gameObject == gameObject)
                        continue;

                    float distance = Vector3.Distance(transform.position, hitColliders[i].transform.position);
                    if (distance < nearObjDistance)
                    {
                        nearObjDistance = distance;
                        nearObj = hitColliders[i].gameObject;
                    }
                }
            }
            
            if (nearObj == null)
            {
                Debug.Log($"Player {NetworkObjectId} KnifeAttack 감지 실패");
                stateManager.ChangeState(PlayerState.Idle);
                return;
            }
            
            if (nearObj.CompareTag("NPC"))
            {
                nearObj.GetComponent<NPCController>().Dead();
                Debug.Log($"Player {NetworkObjectId} KnifeAttack 감지 성공 - NPC 제거");
            }
            else if (nearObj.CompareTag("Player"))
            {
                // 서버에서 피격자 플레이어의 Dead() 호출
                var targetHealth = nearObj.GetComponent<PlayerHealth>();
                if (targetHealth != null)
                {
                    targetHealth.Dead();
                    Debug.Log($"Player {NetworkObjectId} KnifeAttack 감지 성공 - Player 제거");
                    
                    // 공격자(자기 자신)의 클라이언트에서만 승리 처리
                    if (IsOwner)
                    {
                        EndGameSessionClientRpc(true);
                    }
                }
            }
        }
        stateManager.ChangeState(PlayerState.Idle);
    }
    
    [ClientRpc]
    void EndGameSessionClientRpc(bool isVictory)
    {
        // 공격자의 클라이언트에서만 승리 화면 표시
        if (IsOwner)
        {
            GameManager.Instance.EndGameSession(isVictory);
        }
    }
    
    
    public void GunAttack()
    {
        Debug.Log($"Player {NetworkObjectId} GunAttack 시작");
        
        // 서버에서만 공격 검증 및 처리
        if (IsServer)
        {
            ExecuteGunAttack();
        }
        else if (IsOwner)
        {
            // 클라이언트에서는 서버에 요청
            GunAttackServerRpc();
        }
    }
    
    [ServerRpc]
    void GunAttackServerRpc()
    {
        ExecuteGunAttack();
    }
    
    private void ExecuteGunAttack()
    {
        Debug.Log($"Player {NetworkObjectId} GunAttack 시작");
        
        // 가장 가까운 적 찾기
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 4f, characterLayerMask);
        
        if (hitColliders.Length == 0)
        {
            Debug.Log($"Player {NetworkObjectId} GunAttack 감지 실패");
            return;
        }
        
        GameObject target = null;
        float minDistance = 100f;
        
        for (int i = 0; i < hitColliders.Length; i++)
        {
            if (hitColliders[i].CompareTag("NPC") || hitColliders[i].CompareTag("Player"))
            {
                // 자기 자신은 제외
                if (hitColliders[i].gameObject == gameObject)
                    continue;

                float distance = Vector3.Distance(transform.position, hitColliders[i].transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    target = hitColliders[i].gameObject;
                }
            }
        }
        
        if (target == null)
        {
            Debug.Log($"Player {NetworkObjectId} GunAttack 타겟 없음");
            return;
        }

        // Raycast로 총알 발사 (즉시 결과 확인)
        Vector3 firePosition = transform.position + Vector3.up * 1f;
        Vector3 targetPosition = target.transform.position + Vector3.up * 1f;
        Vector3 direction = (targetPosition - firePosition).normalized;
        float maxDistance = Vector3.Distance(firePosition, targetPosition) + 5f; // 여유 거리
        
        // Block과 Character를 모두 체크하기 위한 레이어 마스크
        LayerMask allLayers = blockLayerMask | characterLayerMask;
        
        RaycastHit[] hits = Physics.RaycastAll(firePosition, direction, maxDistance, allLayers);
        
        // 거리순으로 정렬
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        
        // 가장 가까운 충돌체 확인
        foreach (RaycastHit hit in hits)
        {
            GameObject hitObject = hit.collider.gameObject;
            
            // 자기 자신은 제외
            if (hitObject == gameObject)
                continue;
            
            // Block에 막힘
            if (((1 << hitObject.layer) & blockLayerMask) != 0)
            {
                Debug.Log($"Player {NetworkObjectId} 총알이 Block에 막힘");
                
                // 시각적 효과만 (선택사항)
                if (bulletPrefab != null)
                {
                    GameObject effect = Instantiate(bulletPrefab, hit.point, Quaternion.LookRotation(-direction));
                    Destroy(effect, 0.1f); // 짧은 시간만 표시
                }
                stateManager.ChangeState(PlayerState.Idle);
                return;
            }
            
            // NPC에 맞음
            if (hitObject.CompareTag("NPC"))
            {
                Debug.Log($"Player {NetworkObjectId} 총알이 NPC에 명중");
                hitObject.GetComponent<NPCController>()?.Dead();
                
                // 시각적 효과
                if (bulletPrefab != null)
                {
                    GameObject effect = Instantiate(bulletPrefab, hit.point, Quaternion.LookRotation(-direction));
                    Destroy(effect, 0.1f);
                }
                stateManager.ChangeState(PlayerState.Idle);
                return;
            }
            
            // Player에 맞음
            if (hitObject.CompareTag("Player"))
            {
                Debug.Log($"Player {NetworkObjectId} 총알이 Player에 명중");
                var targetHealth = hitObject.GetComponent<PlayerHealth>();
                if (targetHealth != null)
                {
                    targetHealth.Dead();
                    
                    // 발사자에게 승리 알림
                    if (IsOwner)
                    {
                        EndGameSessionClientRpc(true);
                    }
                }
                
                // 시각적 효과
                if (bulletPrefab != null)
                {
                    GameObject effect = Instantiate(bulletPrefab, hit.point, Quaternion.LookRotation(-direction));
                    Destroy(effect, 0.1f);
                }
                stateManager.ChangeState(PlayerState.Idle);
                return;
            }
        }
        
        // 아무것도 맞지 않음 (타겟이 이동했거나 다른 이유)
        Debug.Log($"Player {NetworkObjectId} 총알이 빗나감");
        stateManager.ChangeState(PlayerState.Idle);
    }
    
    public void LandMineAttack()
    {
        Debug.Log($"Player {NetworkObjectId} LandMineAttack 시작");
        // TODO: 지뢰 공격 로직 구현
    }
}

