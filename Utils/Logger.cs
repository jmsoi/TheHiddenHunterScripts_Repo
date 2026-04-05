using UnityEngine;

/// <summary>
/// 게임 전반에서 사용되는 로깅 시스템
/// </summary>
public static class GameLogger
{
    private const string LOG_FORMAT = "[{0}] {1}: {2}";
    
    /// <summary>
    /// 일반 정보 로그
    /// </summary>
    /// <param name="message">로그 메시지</param>
    /// <param name="context">컨텍스트 (클래스명 등)</param>
    public static void LogInfo(string message, string context = "")
    {
        Debug.Log(FormatLog("INFO", context, message));
    }
    
    /// <summary>
    /// 경고 로그
    /// </summary>
    /// <param name="message">로그 메시지</param>
    /// <param name="context">컨텍스트 (클래스명 등)</param>
    public static void LogWarning(string message, string context = "")
    {
        Debug.LogWarning(FormatLog("WARNING", context, message));
    }
    
    /// <summary>
    /// 에러 로그
    /// </summary>
    /// <param name="message">로그 메시지</param>
    /// <param name="context">컨텍스트 (클래스명 등)</param>
    public static void LogError(string message, string context = "")
    {
        Debug.LogError(FormatLog("ERROR", context, message));
    }
    
    /// <summary>
    /// 네트워크 관련 로그
    /// </summary>
    /// <param name="message">로그 메시지</param>
    /// <param name="context">컨텍스트 (클래스명 등)</param>
    public static void LogNetwork(string message, string context = "")
    {
        Debug.Log(FormatLog("NETWORK", context, message));
    }
    
    /// <summary>
    /// 게임플레이 관련 로그
    /// </summary>
    /// <param name="message">로그 메시지</param>
    /// <param name="context">컨텍스트 (클래스명 등)</param>
    public static void LogGameplay(string message, string context = "")
    {
        Debug.Log(FormatLog("GAMEPLAY", context, message));
    }
    
    /// <summary>
    /// UI 관련 로그
    /// </summary>
    /// <param name="message">로그 메시지</param>
    /// <param name="context">컨텍스트 (클래스명 등)</param>
    public static void LogUI(string message, string context = "")
    {
        Debug.Log(FormatLog("UI", context, message));
    }
    
    /// <summary>
    /// 로그 포맷팅
    /// </summary>
    /// <param name="level">로그 레벨</param>
    /// <param name="context">컨텍스트</param>
    /// <param name="message">메시지</param>
    /// <returns>포맷된 로그 문자열</returns>
    private static string FormatLog(string level, string context, string message)
    {
        if (string.IsNullOrEmpty(context))
        {
            return string.Format(LOG_FORMAT, level, "GAME", message);
        }
        return string.Format(LOG_FORMAT, level, context, message);
    }
}
