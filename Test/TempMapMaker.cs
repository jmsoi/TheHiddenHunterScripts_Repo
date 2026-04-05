using UnityEngine;
using Unity.Netcode;

public class TempMapMaker : MonoBehaviour
{
    [Header("Test Player")]
    public NetworkObject playerPrefab;   // 테스트용 플레이어 프리팹 (NetworkObject 포함)

    private void OnEnable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }

    // Multiplayer widget에서 Host/Client가 성공적으로 붙으면 이 콜백이 호출됨
    private void OnClientConnected(ulong clientId)
    {
        // 서버/호스트에서만 플레이어 스폰
        if (!NetworkManager.Singleton.IsServer)
            return;

        // 이미 플레이어가 있으면 중복 스폰 방지 (옵션)
        if (NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject != null)
            return;

        // 플레이어 생성 위치 (원하는 테스트 위치)
        Vector3 spawnPos = new Vector3(0, 1, 0);
        Quaternion spawnRot = Quaternion.identity;

        // 프리팹 인스턴스 생성 + 해당 clientId의 PlayerObject로 스폰
        var playerInstance = Instantiate(playerPrefab, spawnPos, spawnRot);
        playerInstance.SpawnAsPlayerObject(clientId);

        Debug.Log($"TempMapMaker: Client {clientId} 플레이어 스폰 완료");
    }
}