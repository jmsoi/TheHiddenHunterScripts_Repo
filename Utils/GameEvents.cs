using System;

/// <summary>
/// 게임 전반에서 사용되는 이벤트 시스템
/// </summary>
public static class GameEvents
{
    // ===== 게임 상태 이벤트 =====
    public static event Action OnGameStarted;
    public static event Action<bool> OnGameEnded;
    public static event Action<GameConstants.GameState> OnGameStateChanged;
    
    // ===== 네트워크 이벤트 =====
    public static event Action<string> OnPlayerJoined;
    public static event Action<string> OnPlayerLeft;
    public static event Action OnHostReady;
    public static event Action OnClientConnected;
    
    // ===== 로비 이벤트 =====
    public static event Action OnLobbyCreated;
    public static event Action OnLobbyJoined;
    public static event Action OnLobbyLeft;
    public static event Action OnMatchmakingStarted;
    public static event Action OnMatchmakingCompleted;
    
    // ===== 플레이어 이벤트 =====
    public static event Action<ulong> OnPlayerSpawned;
    public static event Action<ulong> OnPlayerDespawned;
    public static event Action<ulong, GameConstants.ResourceType, int> OnResourceCollected;
    public static event Action<ulong, int> OnSkillUsed;
    
    // ===== NPC 이벤트 =====
    public static event Action<ulong, string> OnNPCStateChanged;
    public static event Action<ulong> OnNPCMined;
    
    // ===== 맵 이벤트 =====
    public static event Action OnMapGenerated;
    public static event Action<int> OnMapObjectDestroyed;
    
    // ===== UI 이벤트 =====
    public static event Action OnUIPanelOpened;
    public static event Action OnUIPanelClosed;
    
    // ===== 이벤트 트리거 메서드들 =====
    
    public static void TriggerGameStarted() => OnGameStarted?.Invoke();
    public static void TriggerGameEnded(bool isVictory) => OnGameEnded?.Invoke(isVictory);
    public static void TriggerGameStateChanged(GameConstants.GameState newState) => OnGameStateChanged?.Invoke(newState);
    
    public static void TriggerPlayerJoined(string playerId) => OnPlayerJoined?.Invoke(playerId);
    public static void TriggerPlayerLeft(string playerId) => OnPlayerLeft?.Invoke(playerId);
    public static void TriggerHostReady() => OnHostReady?.Invoke();
    public static void TriggerClientConnected() => OnClientConnected?.Invoke();
    
    public static void TriggerLobbyCreated() => OnLobbyCreated?.Invoke();
    public static void TriggerLobbyJoined() => OnLobbyJoined?.Invoke();
    public static void TriggerLobbyLeft() => OnLobbyLeft?.Invoke();
    public static void TriggerMatchmakingStarted() => OnMatchmakingStarted?.Invoke();
    public static void TriggerMatchmakingCompleted() => OnMatchmakingCompleted?.Invoke();
    
    public static void TriggerPlayerSpawned(ulong clientId) => OnPlayerSpawned?.Invoke(clientId);
    public static void TriggerPlayerDespawned(ulong clientId) => OnPlayerDespawned?.Invoke(clientId);
    public static void TriggerResourceCollected(ulong clientId, GameConstants.ResourceType resourceType, int amount) => 
        OnResourceCollected?.Invoke(clientId, resourceType, amount);
    public static void TriggerSkillUsed(ulong clientId, int skillIndex) => OnSkillUsed?.Invoke(clientId, skillIndex);
    
    public static void TriggerNPCStateChanged(ulong clientId, string newState) => OnNPCStateChanged?.Invoke(clientId, newState);
    public static void TriggerNPCMined(ulong clientId) => OnNPCMined?.Invoke(clientId);
    
    public static void TriggerMapGenerated() => OnMapGenerated?.Invoke();
    public static void TriggerMapObjectDestroyed(int objectIndex) => OnMapObjectDestroyed?.Invoke(objectIndex);
    
    public static void TriggerUIPanelOpened() => OnUIPanelOpened?.Invoke();
    public static void TriggerUIPanelClosed() => OnUIPanelClosed?.Invoke();
    
    // ===== 이벤트 정리 메서드 =====
    
    /// <summary>
    /// 모든 이벤트 구독 해제 (게임 종료 시 호출)
    /// </summary>
    public static void ClearAllEvents()
    {
        OnGameStarted = null;
        OnGameEnded = null;
        OnGameStateChanged = null;
        OnPlayerJoined = null;
        OnPlayerLeft = null;
        OnHostReady = null;
        OnClientConnected = null;
        OnLobbyCreated = null;
        OnLobbyJoined = null;
        OnLobbyLeft = null;
        OnMatchmakingStarted = null;
        OnMatchmakingCompleted = null;
        OnPlayerSpawned = null;
        OnPlayerDespawned = null;
        OnResourceCollected = null;
        OnSkillUsed = null;
        OnNPCStateChanged = null;
        OnNPCMined = null;
        OnMapGenerated = null;
        OnMapObjectDestroyed = null;
        OnUIPanelOpened = null;
        OnUIPanelClosed = null;
    }
}
