using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

/// <summary>
/// 호스트 전용 세션 관리자
/// 호스트만의 로직을 담당합니다.
/// </summary>
public class HostSessionManager : MonoBehaviour
{
    private GameManager _gameManager;
    private UnityTransport _transport;

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
    /// 호스트 세션 시작
    /// </summary>
    public async Task StartHostSession()
    {
        Debug.Log("[HostSessionManager] 호스트 세션 시작");
        EnsureInitialized();

        try
        {
            // 1. Relay 서버 할당 생성
            var alloc = await RelayService.Instance.CreateAllocationAsync(_gameManager.requiredPlayerCount);
            NetworkSessionData.RelayCode = await RelayService.Instance.GetJoinCodeAsync(alloc.AllocationId);
            
            // 2. Unity Transport 설정 및 호스트 시작
            var relayServerData = AllocationUtils.ToRelayServerData(alloc, "dtls");
            _transport.SetRelayServerData(relayServerData);
            NetworkManager.Singleton.StartHost();
            
            // 3. 로비 데이터 업데이트
            await UpdateLobbyRelayCode();
            
            // 4. 맵 생성
            await Task.Delay(2000);
            _gameManager.mapCreator?.LoadScene();
        }
        catch (Exception e)
        {
            Debug.LogError($"[HostSessionManager] 호스트 세션 시작 실패: {e}");
            throw;
        }
    }

    /// <summary>
    /// 로비에 RelayCode 업데이트
    /// </summary>
    private async Task UpdateLobbyRelayCode()
    {
        await Unity.Services.Lobbies.LobbyService.Instance.UpdateLobbyAsync(
            NetworkSessionData.LobbyId,
            new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    {
                        GameConstants.Network.LOBBY_DATA_RELAY_CODE_KEY,
                        new DataObject(DataObject.VisibilityOptions.Public, NetworkSessionData.RelayCode)
                    }
                }
            }
        );
    }
}

