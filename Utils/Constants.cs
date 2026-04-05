using UnityEngine;

/// <summary>
/// 게임 전반에서 사용되는 상수들을 정의하는 클래스
/// </summary>
public static class GameConstants
{
    // ===== 게임 설정 =====
    public const int DEFAULT_PLAYER_COUNT = 2;
    public const int DEFAULT_NPC_COUNT = 1;
    public const int REQUIRED_PLAYER_COUNT = 2;
    
    // ===== 타이밍 설정 =====
    public const float LOBBY_POLLING_INTERVAL = 2f;
    public const float GAME_START_DELAY = 0.5f;
    public const float AUTO_RETURN_DELAY = 5f;
    public const float HOST_READY_CHECK_INTERVAL = 1f;
    public const float CLIENT_CONNECTION_DELAY = 0.5f;
    
    // ===== 네트워크 설정 =====
    public static class Network
    {
        public const int MAX_LOBBY_QUERY_COUNT = 25;
        public const string RELAY_PROTOCOL = "dtls";
        public const string LOBBY_DATA_RELAY_CODE_KEY = "RelayCode";
        public const string LOBBY_DATA_HOST_READY_KEY = "HostReady";
    }
    
    // ===== 씬 이름 =====
    public static class Scenes
    {
        public const string LOBBY_SCENE_NAME = "Lobby";
        public const string GAME_SCENE_NAME = "InGame";
    }
    
    // ===== UI 설정 =====
    public static class UI
    {
        public const string CANVAS_NAME = "Canvas";
        public const string LOADING_PANEL_NAME = "LoadingPanel";
        public const string RESULT_PANEL_NAME = "ResultPanel";
        public const string WIN_PANEL_NAME = "Win";
        public const string LOSE_PANEL_NAME = "Lose";
        public const string MAIN_BUTTON_NAME = "Main";
        public const string MINING_BUTTON_NAME = "MiningBtn";
        public const string JOYSTICK_NAME = "Variable Joystick";
    }
    
    // ===== NPC 설정 =====
    public static class NPC
    {
        public const float DEFAULT_SPEED = 1f;
        public const float DEFAULT_IDLE_TIME = 2f;
        public const float DEFAULT_MINING_TIME = 3f;
        public const float DEFAULT_TRACKING_TIME = 4f;
        public const float DEFAULT_WANDERING_TIME = 5f;
        public const float AI_UPDATE_INTERVAL = 0.5f;
    }
    
    // ===== 플레이어 설정 =====
    public static class Player
    {
        public const float DEFAULT_SPEED = 5f;
        public const float COLLISION_CHECK_DISTANCE = 1f;
        public const float MIN_MOVEMENT_THRESHOLD = 0.001f;
    }
    
    // ===== 맵 설정 =====
    public static class Map
    {
        public const int DEFAULT_WIDTH = 20;
        public const int DEFAULT_HEIGHT = 20;
        public const float MINERAL_POSITION_RANDOM_RANGE = 1f;
    }
    
    // ===== 리소스 타입 =====
    public enum ResourceType
    {
        None = 0,
        Stone = 1,
        Blue = 2,
        Red = 3,
        Yellow = 4
    }
    
    // ===== 게임 상태 =====
    public enum GameState
    {
        Lobby,
        Loading,
        InGame,
        Paused,
        GameOver
    }
}
