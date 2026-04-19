using UnityEngine;
using Unity.Netcode;
using System.Collections;
using UnityEngine.UI;
using System;

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

    //스킬 관련 오브젝트
    public GameObject bulletPrefab;
    public GameObject landMinePrefab_visible;
    public GameObject landMinePrefab_invisible;

    public Material myheadMaterial;
    public Material mybodyMaterial;

    public Image blindZoneImage;

    // public event Action<bool> OnKnifeAttack;
    // public event Action<bool> OnGunAttack;
    // public event Action<bool> OnLandMineAttack;
    public event Action<bool> OnHide;
    public event Action<bool> OnBlindZone;
    public event Action<float> OnFrozen;
    public float frozenDuration = 10f;
    
    
    private void Awake()
    {
        stateManager = GetComponent<PlayerStateManager>();
        animationController = GetComponent<PlayerAnimationController>();
        blindZoneImage = GameObject.Find("Canvas").transform.Find("BlindImage").GetComponent<Image>();
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
                    // KnifeAttack();
                    KnifeAttackServerRpc();
                    break;
                case 1:
                    stateManager.ChangeState(PlayerState.Attack_Gun);
                    // GunAttack();
                    GunAttackServerRpc();
                    break;
                case 2:
                    stateManager.ChangeState(PlayerState.Attack_LandMine);
                    // LandMineAttack();
                    LandMineAttackServerRpc();
                    break;
            }
        }
        else if (skill.type == SkillType.Move)
        {
            switch (skill.index)
            {
                case 0:
                    // animationController?.SetTrigger("Hide");
                    // HideMove();
                    HideMoveServerRpc();
                    break;
                case 1:
                    // animationController?.SetTrigger("BlindZone");
                    // BlindZoneMove();
                    BlindZoneMoveServerRpc();
                    break;
                case 2:
                    // animationController?.SetTrigger("Frozen");
                    // FrozenMove();
                    FrozenMoveServerRpc();
                    break;
            }
        }
        else if (skill.type == SkillType.Passive)
        {
            switch (skill.index)
            {
                case 0:
                    ResourceMasterPassiveServerRpc();
                    break;
                case 1:
                    NPCKillerPassiveServerRpc();
                    break;
                case 2:
                    // ShieldPassiveServerRpc();
                    break;
            }
        }
    }
    


    #region Attack

    [ServerRpc(RequireOwnership = true)]
    void KnifeAttackServerRpc()
    {
        Vector3 boxCenter = transform.position + transform.TransformDirection(new Vector3(0, 1, 1));
        Collider[] hitColliders = Physics.OverlapBox(boxCenter, new Vector3(0.5f, 1, 1) * 0.5f, Quaternion.identity, characterLayerMask);
        
        if (hitColliders.Length == 0) return;
        
        GameObject nearObj = null;
        float nearObjDistance = 100;
        
        // 가장 가까운 적 찾기
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
        stateManager.ChangeState(PlayerState.Idle);
    }
    [ServerRpc(RequireOwnership = true)]
    void GunAttackServerRpc()
    {
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

    [ServerRpc(RequireOwnership = true)]
    void LandMineAttackServerRpc()
    {
        LandMineAttackClientRpc();
    }
    [ClientRpc]
    void LandMineAttackClientRpc()
    {
        if (IsOwner)
        {
            GameObject landMineVisible = Instantiate(landMinePrefab_visible, transform.position, Quaternion.identity);
        }
        else
        {
            GameObject landMineInvisible = Instantiate(landMinePrefab_invisible, transform.position, Quaternion.identity);
        }
    }
    #endregion

    #region Hide Move
    [ServerRpc(RequireOwnership = true)]
    void HideMoveServerRpc() => HideMoveApplyClientRpc();

    [ClientRpc]
    void HideMoveApplyClientRpc() => StartCoroutine(HideMoveCoroutine());

    private IEnumerator HideMoveCoroutine()
    {
        var movement = GetComponent<PlayerMovement>();
        Transform child = transform.GetChild(0);

        if (IsOwner)
        {
            movement.maxSpeed *= 1.5f;
            myheadMaterial.SetColor("_EmissionColor", new Color(0f, 10f, 0f));
            mybodyMaterial.SetColor("_EmissionColor", new Color(0f, 10f, 0f));
        }
        else
        {
            child.gameObject.SetActive(false);
        }
        yield return new WaitForSeconds(3f);

        if (IsOwner)
        {
            movement.maxSpeed /= 1.5f;
            myheadMaterial.SetColor("_EmissionColor", Color.black);
            mybodyMaterial.SetColor("_EmissionColor", Color.black);
        }
        else
        {
            child.gameObject.SetActive(true);
        }
    }
    #endregion

    #region Blind Zone Move
    [ServerRpc(RequireOwnership = true)]
    void BlindZoneMoveServerRpc() => BlindZoneMoveApplyClientRpc();

    [ClientRpc]
    void BlindZoneMoveApplyClientRpc() => StartCoroutine(BlindZoneMoveCoroutine());

    private IEnumerator BlindZoneMoveCoroutine()
    {
        float duration = 3f;

        float transTimer = 0f;
        float transDuration = 0.3f;


        if (IsOwner == false)
        {
            blindZoneImage.color = new Color(0,0,0,0);

            while (transTimer < transDuration)
            {
                transTimer += Time.deltaTime;
                blindZoneImage.color = new Color(0,0,0, Mathf.Lerp(0, 1, transTimer / transDuration));
                yield return null;
            }
            
            yield return new WaitForSeconds(duration);
            transTimer = 0f;

            while (transTimer < transDuration)
            {
                transTimer += Time.deltaTime;
                blindZoneImage.color = new Color(0,0,0, Mathf.Lerp(1, 0, transTimer / transDuration));
                yield return null;
            }   
        }
    }
    #endregion

    #region Frozen Move
    [ServerRpc(RequireOwnership = true)]
    void FrozenMoveServerRpc()
    {

        OnFrozen?.Invoke(frozenDuration);
    }
    #endregion

    #region Passive

    [ServerRpc(RequireOwnership = true)]
    void ResourceMasterPassiveServerRpc()
    {
        ResourceMasterPassiveClientRpc();
    }
    [ClientRpc]
    void ResourceMasterPassiveClientRpc()
    {
        ResourceMasterPassive();
    }
    void ResourceMasterPassive()
    {
        // TODO: 자원수집 승리 로직 구현
    }


    [ServerRpc(RequireOwnership = true)]
    void NPCKillerPassiveServerRpc()
    {
        NPCKillerPassiveClientRpc();
    }
    [ClientRpc]
    void NPCKillerPassiveClientRpc()
    {
        
    }


    // [ServerRpc(RequireOwnership = true)]
    // void ShieldPassiveServerRpc()
    // {
    //     ShieldPassiveClientRpc();
    // }
    // [ClientRpc]
    // void ShieldPassiveClientRpc()
    // {
    //     ShieldPassive();
    // }
    #endregion

    
    [ClientRpc]
    void EndGameSessionClientRpc(bool isVictory)
    {
        // 공격자의 클라이언트에서만 승리 화면 표시
        if (IsOwner)
        {
            GameManager.Instance.EndGameSession(isVictory);
        }
    }
}

