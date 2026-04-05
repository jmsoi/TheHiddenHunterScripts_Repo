/// <summary>
/// 네트워크 세션 정보를 저장하는 정적 클래스
/// </summary>
public static class NetworkSessionData
{
    public static string RelayCode { get; set; }
    public static bool IsHost { get; set; }
    public static string LobbyId { get; set; }
    public static bool IsHostReady { get; set; }
    public static string PlayerId { get; set; }
    
    /// <summary>
    /// 세션 데이터 초기화
    /// </summary>
    public static void Reset()
    {
        RelayCode = string.Empty;
        IsHost = false;
        LobbyId = string.Empty;
        IsHostReady = false;
        PlayerId = string.Empty;
    }
} 