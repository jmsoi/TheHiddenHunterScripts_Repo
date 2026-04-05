using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

/// <summary>
/// 로비 네트워크 서비스 (비즈니스 로직만 담당)
/// UI와 분리된 순수한 네트워크 연결 로직
/// </summary>
public class LobbyNetworkService : MonoBehaviour
{
    private bool _isLobbyPollingActive = false;
    private bool _isRoomCreationInProgress = false;

    // 멀티 플레이 모드 대응: 초기화 상태 추적
    private static bool _isInitializing = false;
    private static bool _isInitialized = false;

    // 이벤트: UI 업데이트를 위한 콜백
    public event Action<string> OnQuickMatchStatusChanged;
    public event Action<bool> OnQuickMatchPanelVisibilityChanged;
    public event Action<string> OnRoomCodeChanged;
    public event Action<bool> OnCreateButtonInteractableChanged;
    public event Action<string> OnCreateButtonTextChanged;
    public event Action<bool> OnWaitingPanelVisibilityChanged;
    public event Action<bool> OnJoinPanelVisibilityChanged;
    public event Action<bool> OnJoinNoRoomPanelVisibilityChanged;
    public event Action<bool> OnJoinWaitingPanelVisibilityChanged;
    public event Action OnLobbyCreated;
    public event Action OnLobbyJoined;
    public event Action OnLobbyLeft;
    public event Action<int, int> OnPlayerCountChanged; // current, required

    async void Start()
    {
        // 이미 초기화되었거나 초기화 중이면 스킵
        if (_isInitialized || _isInitializing)
        {
            Debug.Log("[LobbyNetworkService] 이미 초기화되었거나 초기화 중입니다.");
            return;
        }

        _isInitializing = true;

        try
        {
            // Unity Services 초기화 (이미 초기화되었는지 확인)
            try
            {
                await UnityServices.InitializeAsync();
            }
            catch (Exception ex)
            {
                // 이미 초기화된 경우 무시
                if (ex.Message.Contains("already initialized") || ex.Message.Contains("Invalid state"))
                {
                    Debug.Log("[LobbyNetworkService] Unity Services는 이미 초기화되었습니다.");
                }
                else
                {
                    throw;
                }
            }

            // 익명 로그인 (이미 로그인되어 있거나 로그인 중이면 스킵)
            if (AuthenticationService.Instance.IsSignedIn)
            {
                Debug.Log("[LobbyNetworkService] 이미 로그인되어 있습니다.");
            }
            else if (AuthenticationService.Instance.IsAuthorized)
            {
                Debug.Log("[LobbyNetworkService] 이미 인증되어 있습니다.");
            }
            else
            {
                // 로그인 중이 아닐 때만 로그인 시도
                try
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                    Debug.Log("[LobbyNetworkService] 익명 로그인 완료");
                }
                catch (AuthenticationException ex)
                {
                    // 이미 로그인 중이거나 다른 인스턴스가 로그인 중인 경우
                    if (ex.Message.Contains("already signing in") || ex.Message.Contains("Invalid state"))
                    {
                        Debug.LogWarning("[LobbyNetworkService] 이미 로그인 중입니다. 대기합니다...");
                        // 잠시 대기 후 상태 확인
                        await Task.Delay(1000);
                        if (AuthenticationService.Instance.IsSignedIn)
                        {
                            Debug.Log("[LobbyNetworkService] 로그인 완료 확인");
                        }
                    }
                    else
                    {
                        Debug.LogError($"[LobbyNetworkService] 로그인 실패: {ex}");
                        throw;
                    }
                }
            }

            _isInitialized = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[LobbyNetworkService] 초기화 실패: {e}");
            _isInitializing = false;
        }
    }

    /// <summary>
    /// 빠른 매칭 요청 (로비 조회 및 참가/생성)
    /// </summary>
    public async Task<bool> RequestQuickMatchAsync()
    {
        // 이미 방 생성 중이면 무시
        if (_isRoomCreationInProgress)
        {
            Debug.LogWarning("방 생성 중입니다. 잠시 기다려주세요.");
            return false;
        }

        // 이미 로비 체크 중이면 무시
        if (_isLobbyPollingActive)
        {
            Debug.LogWarning("이미 매칭 중입니다.");
            return false;
        }

        OnQuickMatchPanelVisibilityChanged?.Invoke(true);
        OnQuickMatchStatusChanged?.Invoke("찾는 중...");

        try
        {
            // 로비 조회 (빈 자리가 있는 로비만)
            var queryLobbiesOptions = new QueryLobbiesOptions
            {
                Count = 25,
                Filters = new List<QueryFilter>
                {
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
                }
            };
            var lobbies = await Unity.Services.Lobbies.LobbyService.Instance.QueryLobbiesAsync(queryLobbiesOptions);

            // 방 있으면 참가(클라이언트)
            if (lobbies.Results.Count > 0)
            {
                foreach (var lobbyData in lobbies.Results)
                {
                    if (lobbyData.Players.Count >= GameManager.Instance.requiredPlayerCount)
                        continue;

                    NetworkSessionData.LobbyId = lobbyData.Id;
                    NetworkSessionData.RelayCode = lobbyData.Data[GameConstants.Network.LOBBY_DATA_RELAY_CODE_KEY].Value;
                    NetworkSessionData.IsHost = false;
                    NetworkSessionData.IsHostReady = false;

                    await Unity.Services.Lobbies.LobbyService.Instance.JoinLobbyByIdAsync(lobbyData.Id);
                    OnLobbyJoined?.Invoke();
                    StartLobbyPolling();
                    return true;
                }
            }

            // 방 없으면 생성(호스트)
            return await CreateQuickMatchLobbyAsync();
        }
        catch (Exception e)
        {
            Debug.LogError($"Quick Match 실패: {e}");
            OnQuickMatchStatusChanged?.Invoke("매칭 실패");
            OnQuickMatchPanelVisibilityChanged?.Invoke(false);
            return false;
        }
    }

    /// <summary>
    /// 빠른 매칭용 로비 생성
    /// </summary>
    private async Task<bool> CreateQuickMatchLobbyAsync()
    {
        _isRoomCreationInProgress = true;
        OnQuickMatchStatusChanged?.Invoke("방 생성 중...");

        try
        {
            var createLobbyOptions = new CreateLobbyOptions { IsPrivate = false };
            var lobby = await Unity.Services.Lobbies.LobbyService.Instance.CreateLobbyAsync(
                UnityEngine.Random.Range(10000, 99999).ToString(),
                GameManager.Instance.requiredPlayerCount,
                createLobbyOptions
            );

            var allocation = await RelayService.Instance.CreateAllocationAsync(GameManager.Instance.requiredPlayerCount);
            var relayCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            NetworkSessionData.RelayCode = relayCode;
            NetworkSessionData.LobbyId = lobby.Id;
            NetworkSessionData.IsHost = true;
            NetworkSessionData.IsHostReady = false;

            // 로비 데이터 업데이트
            await Unity.Services.Lobbies.LobbyService.Instance.UpdateLobbyAsync(lobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    {
                        GameConstants.Network.LOBBY_DATA_RELAY_CODE_KEY,
                        new DataObject(DataObject.VisibilityOptions.Public, relayCode)
                    }
                }
            });

            _isRoomCreationInProgress = false;
            OnLobbyCreated?.Invoke();

            if (!_isLobbyPollingActive)
            {
                StartLobbyPolling();
            }

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"로비 생성 실패: {e}");
            _isRoomCreationInProgress = false;
            return false;
        }
    }

    /// <summary>
    /// 커스텀 방 생성
    /// </summary>
    public async Task<string> CreateCustomRoomAsync()
    {
        if (_isRoomCreationInProgress)
        {
            Debug.LogWarning("방 생성 중입니다. 잠시 기다려주세요.");
            return null;
        }

        _isRoomCreationInProgress = true;
        OnWaitingPanelVisibilityChanged?.Invoke(true);
        OnCreateButtonInteractableChanged?.Invoke(false);
        OnCreateButtonTextChanged?.Invoke("생성 중..");
        OnJoinPanelVisibilityChanged?.Invoke(false);

        try
        {
            var createLobbyOptions = new CreateLobbyOptions { IsPrivate = true };
            var lobby = await Unity.Services.Lobbies.LobbyService.Instance.CreateLobbyAsync(
                UnityEngine.Random.Range(10000, 99999).ToString(),
                GameManager.Instance.requiredPlayerCount,
                createLobbyOptions
            );

            // Relay 서버 생성
            var allocation = await RelayService.Instance.CreateAllocationAsync(GameManager.Instance.requiredPlayerCount);
            var relayCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            NetworkSessionData.RelayCode = relayCode;
            NetworkSessionData.LobbyId = lobby.Id;
            NetworkSessionData.IsHost = true;
            NetworkSessionData.IsHostReady = false;

            // 로비 데이터 업데이트
            await Unity.Services.Lobbies.LobbyService.Instance.UpdateLobbyAsync(lobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    {
                        GameConstants.Network.LOBBY_DATA_RELAY_CODE_KEY,
                        new DataObject(DataObject.VisibilityOptions.Public, relayCode)
                    }
                }
            });

            Debug.Log($"RelayCode: {relayCode}");

            _isRoomCreationInProgress = false;
            OnLobbyCreated?.Invoke();
            OnRoomCodeChanged?.Invoke(lobby.LobbyCode);
            OnCreateButtonTextChanged?.Invoke("취소");
            OnCreateButtonInteractableChanged?.Invoke(true);
            StartLobbyPolling();

            return lobby.LobbyCode;
        }
        catch (Exception e)
        {
            Debug.LogError($"방 생성 실패: {e}");
            _isRoomCreationInProgress = false;
            OnWaitingPanelVisibilityChanged?.Invoke(false);
            OnJoinPanelVisibilityChanged?.Invoke(true);
            OnCreateButtonTextChanged?.Invoke("생성");
            OnCreateButtonInteractableChanged?.Invoke(true);
            return null;
        }
    }

    /// <summary>
    /// 커스텀 방 참가
    /// </summary>
    public async Task<bool> JoinCustomRoomAsync(string lobbyCode)
    {
        if (_isRoomCreationInProgress)
        {
            Debug.LogWarning("방 생성 중입니다. 잠시 기다려주세요.");
            return false;
        }

        if (string.IsNullOrEmpty(lobbyCode))
        {
            Debug.LogWarning("방 코드를 입력해주세요.");
            return false;
        }

        OnJoinWaitingPanelVisibilityChanged?.Invoke(true);

        try
        {
            // 방 찾기
            var joinedLobby = await Unity.Services.Lobbies.LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode.Trim().ToUpper());
            if (joinedLobby == null)
            {
                Debug.LogError("해당 방을 찾을 수 없습니다.");
                OnJoinNoRoomPanelVisibilityChanged?.Invoke(true);
                OnJoinWaitingPanelVisibilityChanged?.Invoke(false);
                return false;
            }

            // 세션 정보 설정 (로비 ID는 먼저 저장)
            NetworkSessionData.LobbyId = joinedLobby.Id;
            NetworkSessionData.IsHost = false;
            NetworkSessionData.IsHostReady = false;

            // Relay code가 로비 데이터에 있는지 확인하고 대기
            string relayCode = null;
            int maxRetries = 30; // 최대 30번 시도 (약 60초)
            int retryCount = 0;

            while (string.IsNullOrEmpty(relayCode) && retryCount < maxRetries)
            {
                try
                {
                    var lobby = await Unity.Services.Lobbies.LobbyService.Instance.GetLobbyAsync(NetworkSessionData.LobbyId);
                    
                    // Relay code 확인
                    if (lobby.Data != null && 
                        lobby.Data.ContainsKey(GameConstants.Network.LOBBY_DATA_RELAY_CODE_KEY) &&
                        !string.IsNullOrEmpty(lobby.Data[GameConstants.Network.LOBBY_DATA_RELAY_CODE_KEY].Value))
                    {
                        relayCode = lobby.Data[GameConstants.Network.LOBBY_DATA_RELAY_CODE_KEY].Value;
                        Debug.Log($"[JoinCustomRoomAsync] RelayCode 발견: {relayCode}");
                        break;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[JoinCustomRoomAsync] 로비 조회 실패 (재시도 {retryCount + 1}/{maxRetries}): {e.Message}");
                }

                retryCount++;
                if (retryCount < maxRetries)
                {
                    await Task.Delay(2000); // 2초 대기
                }
            }

            // Relay code를 찾지 못한 경우
            if (string.IsNullOrEmpty(relayCode))
            {
                Debug.LogError("Relay code를 찾을 수 없습니다. 호스트가 아직 준비되지 않았을 수 있습니다.");
                OnJoinNoRoomPanelVisibilityChanged?.Invoke(true);
                OnJoinWaitingPanelVisibilityChanged?.Invoke(false);
                return false;
            }

            // Relay 서버에 접속 시도
            var allocation = await RelayService.Instance.JoinAllocationAsync(relayCode);

            Debug.Log($"RelayCode: {relayCode}");

            // 세션 정보 설정
            NetworkSessionData.RelayCode = relayCode;

            // 인원 체크 시작
            StartLobbyPolling();

            Debug.Log($"방 접속 성공: {joinedLobby.LobbyCode}");
            OnLobbyJoined?.Invoke();
            OnJoinWaitingPanelVisibilityChanged?.Invoke(false);

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"방 접속 실패: {e}");
            OnJoinNoRoomPanelVisibilityChanged?.Invoke(true);
            OnJoinWaitingPanelVisibilityChanged?.Invoke(false);
            return false;
        }
    }

    /// <summary>
    /// 로비 삭제
    /// </summary>
    public async Task<bool> DeleteLobbyAsync()
    {
        try
        {
            await Unity.Services.Lobbies.LobbyService.Instance.DeleteLobbyAsync(NetworkSessionData.LobbyId);
            Debug.Log("로비 삭제 완료");
            _isLobbyPollingActive = false;
            OnLobbyLeft?.Invoke();
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("로비 삭제 실패: " + e);
            return false;
        }
    }

    /// <summary>
    /// 인원 조회 (Quick Match와 Custom Match 모두에서 사용)
    /// </summary>
    private async void StartLobbyPolling()
    {
        _isLobbyPollingActive = true;
        while (_isLobbyPollingActive)
        {
            try
            {
                var lobby = await Unity.Services.Lobbies.LobbyService.Instance.GetLobbyAsync(NetworkSessionData.LobbyId);
                int currentCount = lobby.Players.Count;
                int requiredCount = GameManager.Instance.requiredPlayerCount;

                OnPlayerCountChanged?.Invoke(currentCount, requiredCount);
                Debug.Log($"[LobbyService] 인원 체크 중... {currentCount}/{requiredCount}");

                if (currentCount >= requiredCount)
                {
                    Debug.Log($"[LobbyService] 인원 다 모임! Loading 씬으로 이동");
                    GameManager.Instance.StartGameSession();
                    break;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[LobbyService] 로비 조회 실패: {e}");
            }

            await Task.Delay(2000); // 2초 대기
        }
        _isLobbyPollingActive = false;
    }

    /// <summary>
    /// 로비 폴링 중지
    /// </summary>
    public void StopLobbyPolling()
    {
        _isLobbyPollingActive = false;
    }

    void OnApplicationQuit()
    {
        DeleteLobbyAsync();
        Debug.Log("[LobbyService] 어플리케이션 종료");
    }
}

