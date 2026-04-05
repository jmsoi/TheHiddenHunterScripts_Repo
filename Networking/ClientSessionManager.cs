using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

/// <summary>
/// 클라이언트 전용 세션 관리자
/// 클라이언트만의 로직을 담당합니다.
/// </summary>
public class ClientSessionManager : MonoBehaviour
{
    private GameManager _gameManager;
    private UnityTransport _transport;
    private const int POLLING_DELAY_MS = 2000;
    private const int RATE_LIMIT_DELAY_MS = 5000;

    public void Initialize(GameManager gameManager, UnityTransport transport)
    {
        _gameManager = gameManager ?? GameManager.Instance;
        _transport = transport ?? NetworkManager.Singleton?.GetComponent<UnityTransport>();
    }

    private void EnsureInitialized()
    {
        _gameManager ??= GameManager.Instance;
        _transport ??= NetworkManager.Singleton?.GetComponent<UnityTransport>();
        
        if (_gameManager == null || _transport == null)
            throw new NullReferenceException("GameManager 또는 UnityTransport가 초기화되지 않았습니다.");
    }

    /// <summary>
    /// 클라이언트 세션 시작
    /// </summary>
    public async Task StartClientSession()
    {
        Debug.Log("[ClientSessionManager] 클라이언트 세션 시작");
        EnsureInitialized();

        NetworkSessionData.IsHostReady = false;
        await Task.Delay(2000);
        await WaitForHostReady();
        await JoinRelayServer();
        StartNetworkClient();
    }

    private async Task WaitForHostReady()
    {
        while (true)
        {
            await Task.Delay(POLLING_DELAY_MS);
            
            try
            {
                var lobby = await Unity.Services.Lobbies.LobbyService.Instance.GetLobbyAsync(NetworkSessionData.LobbyId);
                
                if (lobby.Data?[GameConstants.Network.LOBBY_DATA_HOST_READY_KEY]?.Value == "true")
                {
                    NetworkSessionData.RelayCode = lobby.Data[GameConstants.Network.LOBBY_DATA_RELAY_CODE_KEY].Value;
                    break;
                }
            }
            catch (Exception e)
            {
                await HandleLobbyPollingError(e);
            }
        }
        
        await Task.Delay(500);
    }

    private async Task HandleLobbyPollingError(Exception e)
    {
        Debug.LogError($"[ClientSessionManager] GetLobbyAsync 실패: {e}");
        await Task.Delay(e.Message.Contains("Rate limit") || e.Message.Contains("429") 
            ? RATE_LIMIT_DELAY_MS 
            : POLLING_DELAY_MS);
    }

    private async Task JoinRelayServer()
    {
        var joinAlloc = await RelayService.Instance.JoinAllocationAsync(NetworkSessionData.RelayCode);
        var relayServerData = AllocationUtils.ToRelayServerData(joinAlloc, "dtls");
        _transport.SetRelayServerData(relayServerData);
    }

    private void StartNetworkClient()
    {
        NetworkManager.Singleton.StartClient();
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton.LocalClientId == clientId)
        {
            _gameManager ??= GameManager.Instance;
            _gameManager?.mapCreator?.ClientSendMassageServerRpc();
        }
    }
}

