using UnityEngine;
using Unity.Netcode;

/// <summary>
/// 플레이어 채굴 로직 담당
/// </summary>
public class PlayerMining : NetworkBehaviour
{
    [Header("Mining Settings")]
    public LayerMask collisionLayerMask = 1 << 7; // Block 레이어 (7번째 레이어)
    public Vector3 boxSize = new Vector3(1f, 1f, 1f);
    public Vector3 boxOffset = new Vector3(0f, 1f, 1f);
    
    private PlayerStateManager stateManager;
    private int targetObjectIndex = -1;
    
    private void Awake()
    {
        stateManager = GetComponent<PlayerStateManager>();
    }
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (IsOwner)
        {
            var miningBtn = GameObject.Find("MiningBtn");
            if (miningBtn != null)
            {
                miningBtn.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(Mining);
            }
        }
    }
    
    public void Mining()
    {
        if (stateManager.CurrentState == PlayerState.Mining)
        {
            Debug.Log($"Player {NetworkObjectId} 이미 채굴 중입니다.");
            return;
        }
        
        if (!IsOwner) return;
        
        Debug.Log($"Player {NetworkObjectId} Mining 감지 시작");
        targetObjectIndex = CheckResources();
        
        if (targetObjectIndex != -1)
        {
            Debug.Log($"Player {NetworkObjectId} Mining 감지 성공");
            stateManager.ChangeState(PlayerState.Mining);
        }
        else
        {
            Debug.Log($"Player {NetworkObjectId} Mining 감지 실패");
            stateManager.ChangeState(PlayerState.Idle);
        }
    }
    
    private int CheckResources()
    {
        Vector3 boxCenter = transform.position + transform.TransformDirection(new Vector3(0, 1, 1));
        Collider[] hitColliders = Physics.OverlapBox(boxCenter, new Vector3(0.5f, 1, 1) * 0.5f, Quaternion.identity, collisionLayerMask);
        
        Debug.Log($"PlayerMining: 감지된 Collider 수 = {hitColliders.Length}, LayerMask = {collisionLayerMask.value}");
        
        if (hitColliders.Length > 0)
        {
            int nearObjIdx = -1;
            float nearObjDistance = 100;
            
            for (int i = 0; i < hitColliders.Length; i++)
            {
                GameObject hitObj = hitColliders[i].gameObject;
                Debug.Log($"PlayerMining: 감지된 오브젝트 - {hitObj.name}, Layer: {hitObj.layer}, Tag: {hitObj.tag}");
                
                // 리소스 체크 (태그로 확인)
                if (hitColliders[i].CompareTag("Blue") || hitColliders[i].CompareTag("Red") || hitColliders[i].CompareTag("Yellow"))
                {
                    float distance = Vector3.Distance(transform.position, hitColliders[i].transform.position);
                    if (distance < nearObjDistance)
                    {
                        nearObjDistance = distance;
                        nearObjIdx = hitColliders[i].GetComponent<MapGameObject>().idx;
                    }
                }
            }
            return nearObjIdx;
        }
        return -1;
    }
    
    public void CompletedMining()
    {
        if (targetObjectIndex == -1 || MapManager.Instance.serializedMapData[targetObjectIndex] == (int)MapObject.None)
        {
            Debug.Log($"Player {NetworkObjectId} CompletedMining 실패 - 채굴 대상이 없습니다.");
            return;
        }
        
        if (!IsOwner) return;
        
        // 리소스 추가
        MapObject targetMapObject = (MapObject)MapManager.Instance.serializedMapData[targetObjectIndex];
        ResourceType targetResource = targetMapObject.ToResourceType();
        ResourceManager.Instance.AddResource(targetResource, 1);
        Debug.Log($"Player {NetworkObjectId} Mining 완료 - 리소스 추가: {targetResource}");
        
        // 서버에서만 데이터 처리
        if (IsServer)
        {
            MapManager.Instance.serializedMapData[targetObjectIndex] = (int)MapObject.None;
            MapManager.Instance.DestroyPointClientRpc(targetObjectIndex);
            targetObjectIndex = -1;
            
            StartCoroutine(DelayedStateChange(PlayerState.Idle, 0.1f));
        }
        else
        {
            CompletedMiningServerRpc(targetObjectIndex);
            targetObjectIndex = -1;
        }
    }
    
    private System.Collections.IEnumerator DelayedStateChange(PlayerState newState, float delay)
    {
        yield return new WaitForSeconds(delay);
        stateManager.ChangeState(newState);
    }
    
    [ServerRpc]
    void CompletedMiningServerRpc(int miningTargetIdx)
    {
        Debug.Log($"Player {NetworkObjectId} CompletedMiningServerRpc 호출");
        
        if (miningTargetIdx != -1)
        {
            MapManager.Instance.serializedMapData[miningTargetIdx] = (int)MapObject.None;
            MapManager.Instance.DestroyPointClientRpc(miningTargetIdx);
        }
        
        stateManager.networkState.Value = PlayerState.Idle;
    }
}

