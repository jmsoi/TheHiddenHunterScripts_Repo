using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic; // Added for List
using System.Threading.Tasks;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System.Collections.Generic;

public class MapCreator : NetworkBehaviour
{
    public GameObject[] stonePrefabs;
    public GameObject[] bluePrefabs;
    public GameObject[] redPrefabs;
    public GameObject[] yellowPrefabs;
    public GameObject[] hiddingPointPrefabs;
    public GameObject[] wallPrefabs;
    // public GameObject[] mapObjectPrefabs; // MapObject 순서와 동일하게 프리팹 배열
    public GameObject playerPrefab;
    public GameObject npcPrefab;
    public MapObject[] tempMapData;// = new MapObject[MapManager.Instance.width * MapManager.Instance.height]; // 맵 데이터를 1차원 int 배열로 직렬화
    public GameObject[] tempMapGameObjects;

    public void LoadMapData()
    {
        if (IsHost)
        {
            tempMapData = new MapObject[MapManager.Instance.width * MapManager.Instance.height];
            Debug.Log("호스트: 맵 데이터 준비");
            HostPrepareMapData();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ClientSendMassageServerRpc()
    {
        GameManager.Instance.clientReadyCount++;
        Debug.Log("MapCreator: 클라이언트 준비 완료" + GameManager.Instance.clientReadyCount);
    }

    // 호스트 맵 데이터 생성
    private async void HostPrepareMapData()
    {
        // 맵 데이터 생성
        MapObject[] tempMapData = new MapObject[MapManager.Instance.width * MapManager.Instance.height];

        //npc, player 스폰하기 위해 빈 위치 저장
        List<int> emptyPositions = new List<int>();


        // 맵 데이터 위치 선정
        for (int x = 0; x < MapManager.Instance.width; x++)
        {
            for (int y = 0; y < MapManager.Instance.height; y++)
            {
                // 벽 생성
                if (x == 0 || x == MapManager.Instance.width - 1 || y == 0 || y == MapManager.Instance.height - 1)
                {
                    tempMapData[x * MapManager.Instance.height + y] = MapObject.Wall; // Wall
                    continue;
                }

                // 빈 위치 생성
                int random = Random.Range(0, 10);
                if (random < 5)
                {
                    tempMapData[x * MapManager.Instance.height + y] = MapObject.None; // None
                    emptyPositions.Add(x * MapManager.Instance.height + y);
                }
                else
                {
                    int objIndex = Random.Range(1, 6);
                    tempMapData[x * MapManager.Instance.height + y] = (MapObject)objIndex;
                }
            }
        }

        // 플레이어, NPC 스폰 위치 선정
        {
            // 플레이어 1 스폰 위치 선정
            int idx = Random.Range(0, emptyPositions.Count);
            tempMapData[emptyPositions[idx]] = MapObject.Player1;
            emptyPositions.RemoveAt(idx);

            // 플레이어 2 스폰 위치 선정
            idx = Random.Range(0, emptyPositions.Count);
            tempMapData[emptyPositions[idx]] = MapObject.Player2;
            emptyPositions.RemoveAt(idx);

            // NPC 스폰 위치 선정
            for (int i = 0; i < GameManager.Instance.maxNPCCount; i++)
            {
                idx = Random.Range(0, emptyPositions.Count);
                tempMapData[emptyPositions[idx]] = MapObject.NPC;
                emptyPositions.RemoveAt(idx);
            }
        }
        MapManager.Instance.serializedMapData.Clear();
        foreach (var obj in tempMapData)
            MapManager.Instance.serializedMapData.Add((int)obj);
        
        Debug.Log("MapCreator: 맵 데이터 생성 완료");
        Unity.Services.Lobbies.LobbyService.Instance.UpdateLobbyAsync(
            NetworkSessionData.LobbyId,
            new UpdateLobbyOptions {
                Data = new Dictionary<string, DataObject> {
                    { GameConstants.Network.LOBBY_DATA_HOST_READY_KEY, new DataObject(DataObject.VisibilityOptions.Public, "true") }
                }
            }
        );
        // ReadyToSpawnMapClientRpc();
        // 클라이언트 준비 확인 후 맵 생성
        while (GameManager.Instance.clientReadyCount < GameManager.Instance.requiredPlayerCount - 1)
        {
            await Task.Delay(100);
        }
        Debug.Log("MapCreator: 클라이언트 준비 완료");
        SpawnPrefabsFromMapClientRpc();
    }

    // [ClientRpc]
    // public void ReadyToSpawnMapClientRpc()
    // {
    //     Debug.Log("client ready to spawn map");
    //     NetworkSessionData.IsHostReady = true;
    // }

    // 맵 데이터에 따라 프리팹을 생성하는 함수
    [ClientRpc]
    public void SpawnPrefabsFromMapClientRpc()
    {
        tempMapGameObjects = new GameObject[MapManager.Instance.width * MapManager.Instance.height];
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        // 맵 오브젝트 생성
        for (int x = 0; x < MapManager.Instance.width; x++)
        {
            for (int y = 0; y < MapManager.Instance.height; y++)
            {
                MapObject objType = (MapObject)MapManager.Instance.serializedMapData[x * MapManager.Instance.height + y];
                Vector3 pos = new Vector3(x, 0, y);
                GameObject obj = null;
                switch (objType)
                {
                    case MapObject.Stone:
                        obj = Instantiate(stonePrefabs[Random.Range(0, stonePrefabs.Length)], pos * 2, Quaternion.identity, this.transform);
                        obj.GetComponent<MapGameObject>().idx = x * MapManager.Instance.height + y;
                        break;
                    case MapObject.Blue:
                        obj = Instantiate(bluePrefabs[Random.Range(0, bluePrefabs.Length)], pos * 2, Quaternion.identity, this.transform);
                        obj.GetComponent<MapGameObject>().idx = x * MapManager.Instance.height + y;
                        break;
                    case MapObject.Red:
                        obj = Instantiate(redPrefabs[Random.Range(0, redPrefabs.Length)], pos * 2, Quaternion.identity, this.transform);
                        obj.GetComponent<MapGameObject>().idx = x * MapManager.Instance.height + y;
                        break;
                    case MapObject.Yellow:
                        obj = Instantiate(yellowPrefabs[Random.Range(0, yellowPrefabs.Length)], pos * 2, Quaternion.identity, this.transform);
                        obj.GetComponent<MapGameObject>().idx = x * MapManager.Instance.height + y;
                        break;
                    case MapObject.HiddingPoint:
                        obj = Instantiate(hiddingPointPrefabs[Random.Range(0, hiddingPointPrefabs.Length)], pos * 2, Quaternion.identity, this.transform);
                        obj.GetComponent<MapGameObject>().idx = x * MapManager.Instance.height + y;
                        break;
                    case MapObject.Wall:
                        obj = Instantiate(wallPrefabs[Random.Range(0, wallPrefabs.Length)], pos * 2, Quaternion.identity, this.transform);
                        break;
                    case MapObject.Player1:
                    case MapObject.Player2:
                        break;
                    case MapObject.NPC:
                        if (IsHost)
                        {
                            var npc = Instantiate(npcPrefab, pos * 2, Quaternion.identity, this.transform);
                            npc.GetComponent<NetworkObject>().Spawn();
                        }
                        break;
                }
                if (obj != null)
                {
                    tempMapGameObjects[x * MapManager.Instance.height + y] = obj;
                }

            }
        }

        Debug.Log("MapCreator: 맵 생성 완료");

        // 플레이어 스폰 위치 설정
        Vector3 myPlayerSpawnPosition = Vector3.zero;
        if (IsHost)
        {
            int p1_idx = MapManager.Instance.serializedMapData.IndexOf((int)MapObject.Player1);
            Vector2Int p1_pos = new Vector2Int(p1_idx / MapManager.Instance.width, p1_idx % MapManager.Instance.width);
            myPlayerSpawnPosition = new Vector3(p1_pos.x * 2, 2, p1_pos.y * 2);
        }
        else
        {
            int p2_idx = MapManager.Instance.serializedMapData.IndexOf((int)MapObject.Player2);
            Vector2Int p2_pos = new Vector2Int(p2_idx / MapManager.Instance.width, p2_idx % MapManager.Instance.width);
            myPlayerSpawnPosition = new Vector3(p2_pos.x * 2, 2, p2_pos.y * 2);
        }
        
        PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (PlayerController player in players)
        {
            if (player.IsOwner)
            {
                player.transform.position = myPlayerSpawnPosition;
                CameraMovement cameraMovement = Camera.main.GetComponent<CameraMovement>();
                cameraMovement.target = player.transform;
            }
        }
        Debug.Log("MapCreator: 플레이어 스폰 완료");
        MapManager.Instance.mapGameObjects = tempMapGameObjects;        
        
        GameManager.Instance.ReadyGame();
    }
}
