using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;

public class MapManager : NetworkBehaviour
{
    public static MapManager Instance;
    void Awake()
    {
        Instance = this;
    }
    
    // ===== 맵 관리 =====
    [Header("맵 데이터")]
    public NetworkList<int> serializedMapData = new NetworkList<int>(); // 네트워크 동기화
    public GameObject[] mapGameObjects; // 네트워크 동기화 불필요

    public int width = 20;
    public int height = 20;

    // 맵 객체 삭제 (모든 클라이언트에 동기화)
    [ClientRpc]
    public void DestroyPointClientRpc(int idx)
    {
        if (mapGameObjects[idx] != null)
            mapGameObjects[idx].SetActive(false);
    }

    public int GetAllMineralCount()
    {
        int count = 0;
        foreach (int obj in serializedMapData)
        {
            if (obj == (int)MapObject.Stone || obj == (int)MapObject.Yellow || obj == (int)MapObject.Blue || obj == (int)MapObject.Red)
            {
                count++;
            }
        }
        return count;
    }

    // 광석 개수 확인
    public int GetMineralCount(ResourceType resourceType)
    {
        int count = 0;
        foreach (int obj in serializedMapData)
        {
            if (obj == (int)resourceType)
            {
                count++;
            }
        }
        return count;
    }


    public (int, Vector3) GetMineralNearestPosition(ResourceType resourceType, Vector3 myPos)
    {
        if (GetMineralCount(resourceType) == 0)
        {
            return (-1, Vector3.zero);
        }

        float minDistance = float.MaxValue;
        int idx = -1;
        for (int i = 0; i < serializedMapData.Count; i++)
        {
            if (serializedMapData[i] == (int)resourceType)
            {
                float distance = Vector3.Distance(mapGameObjects[i].transform.position, myPos);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    idx = i;
                }
            }
        }
        return (idx, TargetPosition(idx));
    }

    // 광산 위치 변환 (서버에서 계산 후 클라이언트에 전달)
    Vector3 TargetPosition(int idx)
    {
        if (idx < 0 || idx >= mapGameObjects.Length || mapGameObjects[idx] == null)
            return Vector3.zero;
        Vector3 pos = mapGameObjects[idx].transform.position;
        float x = pos.x + Random.Range(-1f, 1f);
        float z = pos.z + Random.Range(-1f, 1f);
        pos = new Vector3(x, pos.y, z);
        return pos;
    }

}
