using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 로비 UI 프레젠터
/// UI 업데이트만 담당하며, 비즈니스 로직은 LobbyService에 위임
/// </summary>
public class LobbyUIPresenter : MonoBehaviour
{
    [Header("Quick Match UI")]
    public GameObject quickMatchmakingPanel;
    public TextMeshProUGUI quickStatusText;

    [Header("Custom Match UI")]
    public GameObject customMatchmakingPanel;
    public Sprite orangeSprite;
    public Sprite greenSprite;

    [Header("Create Room UI")]
    public TextMeshProUGUI createCodeText;
    public Image createAndCancelButtonImage;
    public TextMeshProUGUI createButtonText;
    public GameObject createWaitingPanel;

    [Header("Join Room UI")]
    public TMP_InputField joinInputField;
    public GameObject joinPanel;
    public GameObject joinNoRoomPanel;
    public GameObject joinWaitingPanel;

    private LobbyNetworkService _lobbyService;
    private bool _isCustomRoomCreated = false;

    private void Awake()
    {
        _lobbyService = FindFirstObjectByType<LobbyNetworkService>();
        if (_lobbyService == null)
        {
            Debug.LogError("[LobbyUIPresenter] LobbyNetworkService를 찾을 수 없습니다.");
            return;
        }

        // 이벤트 구독
        SubscribeToEvents();
    }

    public void Start()
    {
        quickMatchmakingPanel.SetActive(false);
        customMatchmakingPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        UnsubscribeFromEvents();
    }

    private void SubscribeToEvents()
    {
        _lobbyService.OnQuickMatchStatusChanged += UpdateQuickMatchStatus;
        _lobbyService.OnQuickMatchPanelVisibilityChanged += SetQuickMatchPanelVisibility;
        _lobbyService.OnRoomCodeChanged += UpdateRoomCode;
        _lobbyService.OnCreateButtonInteractableChanged += SetCreateButtonInteractable;
        _lobbyService.OnCreateButtonTextChanged += UpdateCreateButtonText;
        _lobbyService.OnWaitingPanelVisibilityChanged += SetWaitingPanelVisibility;
        _lobbyService.OnJoinPanelVisibilityChanged += SetJoinPanelVisibility;
        _lobbyService.OnJoinNoRoomPanelVisibilityChanged += SetJoinNoRoomPanelVisibility;
        _lobbyService.OnJoinWaitingPanelVisibilityChanged += SetJoinWaitingPanelVisibility;
        _lobbyService.OnLobbyCreated += OnLobbyCreatedHandler;
        _lobbyService.OnLobbyJoined += OnLobbyJoinedHandler;
        _lobbyService.OnLobbyLeft += OnLobbyLeftHandler;
        _lobbyService.OnPlayerCountChanged += OnPlayerCountChangedHandler;
    }

    private void UnsubscribeFromEvents()
    {
        if (_lobbyService == null) return;

        _lobbyService.OnQuickMatchStatusChanged -= UpdateQuickMatchStatus;
        _lobbyService.OnQuickMatchPanelVisibilityChanged -= SetQuickMatchPanelVisibility;
        _lobbyService.OnRoomCodeChanged -= UpdateRoomCode;
        _lobbyService.OnCreateButtonInteractableChanged -= SetCreateButtonInteractable;
        _lobbyService.OnCreateButtonTextChanged -= UpdateCreateButtonText;
        _lobbyService.OnWaitingPanelVisibilityChanged -= SetWaitingPanelVisibility;
        _lobbyService.OnJoinPanelVisibilityChanged -= SetJoinPanelVisibility;
        _lobbyService.OnJoinNoRoomPanelVisibilityChanged -= SetJoinNoRoomPanelVisibility;
        _lobbyService.OnJoinWaitingPanelVisibilityChanged -= SetJoinWaitingPanelVisibility;
        _lobbyService.OnLobbyCreated -= OnLobbyCreatedHandler;
        _lobbyService.OnLobbyJoined -= OnLobbyJoinedHandler;
        _lobbyService.OnLobbyLeft -= OnLobbyLeftHandler;
        _lobbyService.OnPlayerCountChanged -= OnPlayerCountChangedHandler;
    }

    // ===== UI 업데이트 메서드 =====

    private void UpdateQuickMatchStatus(string status)
    {
        if (quickStatusText != null)
            quickStatusText.text = status;
    }

    private void SetQuickMatchPanelVisibility(bool isVisible)
    {
        if (quickMatchmakingPanel != null)
            quickMatchmakingPanel.SetActive(isVisible);
    }

    private void UpdateRoomCode(string roomCode)
    {
        if (createCodeText != null)
            createCodeText.text = roomCode;
    }

    private void SetCreateButtonInteractable(bool isInteractable)
    {
        if (createAndCancelButtonImage != null)
        {
            var button = createAndCancelButtonImage.GetComponent<Button>();
            if (button != null)
                button.interactable = isInteractable;
        }
    }

    private void UpdateCreateButtonText(string text)
    {
        if (createButtonText != null)
            createButtonText.text = text;
    }

    private void SetWaitingPanelVisibility(bool isVisible)
    {
        if (createWaitingPanel != null)
            createWaitingPanel.SetActive(isVisible);
    }

    private void SetJoinPanelVisibility(bool isVisible)
    {
        if (joinPanel != null)
            joinPanel.SetActive(isVisible);
    }

    private void SetJoinNoRoomPanelVisibility(bool isVisible)
    {
        if (joinNoRoomPanel != null)
            joinNoRoomPanel.SetActive(isVisible);
    }

    private void SetJoinWaitingPanelVisibility(bool isVisible)
    {
        if (joinWaitingPanel != null)
            joinWaitingPanel.SetActive(isVisible);
    }

    private void OnLobbyCreatedHandler()
    {
        _isCustomRoomCreated = true;
        if (createAndCancelButtonImage != null)
            createAndCancelButtonImage.sprite = greenSprite;
    }

    private void OnLobbyJoinedHandler()
    {
        // 로비 참가 시 UI 업데이트
    }

    private void OnLobbyLeftHandler()
    {
        ResetCreateRoomButton();
    }

    private void OnPlayerCountChangedHandler(int current, int required)
    {
        // 인원 수 변경 시 UI 업데이트 (필요시)
        // 예: 인원 표시 텍스트 업데이트
    }

    // ===== 버튼 이벤트 핸들러 =====

    /// <summary>
    /// 빠른 매칭 버튼 클릭
    /// </summary>
    public async void OnQuickMatchButtonClicked()
    {
        if (_lobbyService != null)
        {
            await _lobbyService.RequestQuickMatchAsync();
        }
    }

    /// <summary>
    /// 커스텀 매칭 패널 열기
    /// </summary>
    public void OnCustomMatchButtonClicked()
    {
        if (customMatchmakingPanel != null)
            customMatchmakingPanel.SetActive(true);

        if (createAndCancelButtonImage != null)
            createAndCancelButtonImage.sprite = orangeSprite;

        if (createCodeText != null)
            createCodeText.text = "";

        if (createButtonText != null)
            createButtonText.text = "생성";

        if (createWaitingPanel != null)
            createWaitingPanel.SetActive(false);

        if (joinPanel != null)
            joinPanel.SetActive(true);
    }

    /// <summary>
    /// 커스텀 방 생성/취소 버튼 클릭
    /// </summary>
    public async void OnCustomCreateRoomButtonClicked()
    {
        if (_lobbyService == null) return;

        if (!_isCustomRoomCreated)
        {
            // 방 생성
            await _lobbyService.CreateCustomRoomAsync();
        }
        else
        {
            // 방 삭제
            await _lobbyService.DeleteLobbyAsync();
            ResetCreateRoomButton();
        }
    }

    /// <summary>
    /// 커스텀 방 참가 버튼 클릭
    /// </summary>
    public async void OnCustomJoinRoomButtonClicked()
    {
        if (_lobbyService == null) return;

        string lobbyCode = joinInputField != null ? joinInputField.text : "";
        await _lobbyService.JoinCustomRoomAsync(lobbyCode);
    }

    /// <summary>
    /// 취소 버튼 클릭
    /// </summary>
    public async void OnCancelButtonClicked()
    {
        if (quickMatchmakingPanel != null)
            quickMatchmakingPanel.SetActive(false);

        if (customMatchmakingPanel != null)
            customMatchmakingPanel.SetActive(false);

        if (_lobbyService != null)
        {
            await _lobbyService.DeleteLobbyAsync();
        }
    }

    /// <summary>
    /// 방 생성 버튼 초기화
    /// </summary>
    private void ResetCreateRoomButton()
    {
        _isCustomRoomCreated = false;

        if (createWaitingPanel != null)
            createWaitingPanel.SetActive(false);

        if (joinPanel != null)
            joinPanel.SetActive(true);

        if (createCodeText != null)
            createCodeText.text = "";

        if (createButtonText != null)
            createButtonText.text = "생성";

        if (createAndCancelButtonImage != null)
        {
            createAndCancelButtonImage.sprite = orangeSprite;
            var button = createAndCancelButtonImage.GetComponent<Button>();
            if (button != null)
                button.interactable = true;
        }
    }
}

