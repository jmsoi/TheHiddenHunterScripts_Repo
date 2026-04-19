using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System.Collections;
using System.Threading.Tasks;
using System;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    // 세션 관리자 참조
    private HostSessionManager _hostSessionManager;
    private ClientSessionManager _clientSessionManager;

    [Header("Game Configuration")]
    public int maxPlayerCount = GameConstants.DEFAULT_PLAYER_COUNT;
    public int maxNPCCount = GameConstants.DEFAULT_NPC_COUNT;
    public int requiredPlayerCount = GameConstants.REQUIRED_PLAYER_COUNT;
    public bool isGameStarted = false;
    public int clientReadyCount = 0;

    [Header("UI References")]
    [HideInInspector] public GameObject loadingPanel;
    [HideInInspector] public MapCreator mapCreator;
    [HideInInspector] public GameObject resultPanel;
    

#region Initialize
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // 세션 관리자 초기화
            LobbyInitialize();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LobbyInitialize()
    {
        _hostSessionManager = GetComponent<HostSessionManager>();
        _clientSessionManager = GetComponent<ClientSessionManager>();
        
        // UnityTransport 초기화 (NetworkManager가 준비될 때까지 대기)
        var transport = NetworkManager.Singleton?.GetComponent<UnityTransport>();
        if (transport != null)
        {
            _hostSessionManager.Initialize(this, transport);
            _clientSessionManager.Initialize(this, transport);
            Debug.Log("[GameManager] 세션 관리자 초기화 완료");
        }
        else
        {
            Debug.LogWarning("[GameManager] NetworkManager.Singleton이 아직 준비되지 않았습니다. 나중에 초기화됩니다.");
        }
    }
#endregion



#region start game session

    /// <summary>
    /// 게임 세션 시작 (리팩토링 버전)
    /// 호스트/클라이언트 로직을 각각의 Manager에 위임합니다.
    /// </summary>
    public async Task StartGameSession()
    {
        SceneManager.LoadScene(GameConstants.Scenes.GAME_SCENE_NAME);

        await Task.Delay((int)(GameConstants.GAME_START_DELAY * 1000)); // 게임 시작 딜레이
        InGameInitialize();

        // 역할에 따라 적절한 세션 관리자에 위임
        if (NetworkSessionData.IsHost)        
            await _hostSessionManager.StartHostSession();
        else
            await _clientSessionManager.StartClientSession();
        
    }

    /// <summary>
    /// UI 초기화
    /// </summary>
    private void InGameInitialize()
    {        
        try
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas != null)
            {
                var loadingPanelTransform = canvas.transform.Find("LoadingPanel");
                if (loadingPanelTransform != null)
                {
                    loadingPanel = loadingPanelTransform.gameObject;
                    loadingPanel.SetActive(true);
                }
                
                var resultPanelTransform = canvas.transform.Find("ResultPanel");
                if (resultPanelTransform != null)
                {
                    resultPanel = resultPanelTransform.gameObject;
                    resultPanel.SetActive(false);
                }
                mapCreator = GameObject.FindFirstObjectByType<MapCreator>();
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[GameManager] LoadingPanel 찾기 실패: " + e);
        }
        
        // 세션 관리자가 초기화되지 않았으면 다시 초기화
        if (_hostSessionManager == null || _clientSessionManager == null)
        {
            Debug.LogWarning("[GameManager] 세션 관리자가 null입니다. 다시 초기화합니다...");
            LobbyInitialize();
        }
    }

    /// <summary>
    /// MapCreator가 맵 데이터를 생성한 후 호출됨.
    /// </summary>
    public void ReadyGame()
    {
        StartCoroutine(DelayedStartGame());
    }
    private IEnumerator DelayedStartGame()
    {
        yield return new WaitForSeconds(1.5f);
        isGameStarted = true;
        loadingPanel.SetActive(false);
    }
#endregion
#region end game session

    public void EndGameSession(bool isVictory)
    {
        resultPanel.SetActive(true);
        if(isVictory)
        {
            Debug.Log("Game End - Win");
            resultPanel.transform.Find("Win").gameObject.SetActive(true);
            resultPanel.transform.Find("Win").transform.Find("Main").GetComponent<Button>().onClick.AddListener(ReturnToLobby);
        }
        else
        {
            Debug.Log("Game End - Lose");
            resultPanel.transform.Find("Lose").gameObject.SetActive(true);
            resultPanel.transform.Find("Lose").transform.Find("Main").GetComponent<Button>().onClick.AddListener(ReturnToLobby);
        }
        
        // 게임 종료 후 자동으로 로비로 이동
        StartCoroutine(AutoReturnToLobby(GameConstants.AUTO_RETURN_DELAY));
    }
    
    private System.Collections.IEnumerator AutoReturnToLobby(float delay)
    {
        yield return new WaitForSeconds(delay);
        ReturnToLobby();
    }

    public void ReturnToLobby()
    {
        // 네트워크 연결 정리
        if (NetworkManager.Singleton != null)
        {
            if (NetworkManager.Singleton.IsListening)
            {
                Debug.Log("[GameManager] 네트워크 연결 종료 중...");
                
                // 호스트인 경우
                if (NetworkManager.Singleton.IsHost)
                {
                    NetworkManager.Singleton.Shutdown();
                    Debug.Log("[GameManager] Host 종료 완료");
                }
                // 클라이언트인 경우
                else if (NetworkManager.Singleton.IsClient)
                {
                    NetworkManager.Singleton.Shutdown();
                    Debug.Log("[GameManager] Client 종료 완료");
                }
                // 서버인 경우
                else if (NetworkManager.Singleton.IsServer)
                {
                    NetworkManager.Singleton.Shutdown();
                    Debug.Log("[GameManager] Server 종료 완료");
                }
            }
        }
        
        // 로비 씬으로 이동
        SceneManager.LoadScene(GameConstants.Scenes.LOBBY_SCENE_NAME);
    }

#endregion
}
