using UnityEngine;
using Unity.Netcode;

/// <summary>
/// 게임에서 사용되는 확장 메서드들
/// </summary>
public static class GameExtensions
{
    // ===== Vector3 확장 =====
    
    /// <summary>
    /// Vector3의 Y축을 0으로 설정 (수평 이동용)
    /// </summary>
    /// <param name="vector">원본 벡터</param>
    /// <returns>Y축이 0인 벡터</returns>
    public static Vector3 ToHorizontal(this Vector3 vector)
    {
        return new Vector3(vector.x, 0, vector.z);
    }
    
    /// <summary>
    /// 두 Vector3 사이의 거리가 임계값보다 작은지 확인
    /// </summary>
    /// <param name="vector1">첫 번째 벡터</param>
    /// <param name="vector2">두 번째 벡터</param>
    /// <param name="threshold">임계값</param>
    /// <returns>거리가 임계값보다 작으면 true</returns>
    public static bool IsNear(this Vector3 vector1, Vector3 vector2, float threshold)
    {
        return Vector3.Distance(vector1, vector2) < threshold;
    }
    
    // ===== GameObject 확장 =====
    
    /// <summary>
    /// GameObject가 특정 태그를 가지고 있는지 확인
    /// </summary>
    /// <param name="gameObject">확인할 GameObject</param>
    /// <param name="tag">확인할 태그</param>
    /// <returns>태그가 일치하면 true</returns>
    public static bool HasTag(this GameObject gameObject, string tag)
    {
        return gameObject.CompareTag(tag);
    }
    
    /// <summary>
    /// GameObject의 자식에서 특정 이름의 오브젝트를 찾기
    /// </summary>
    /// <param name="gameObject">부모 GameObject</param>
    /// <param name="childName">찾을 자식 이름</param>
    /// <returns>찾은 자식 GameObject (없으면 null)</returns>
    public static GameObject FindChild(this GameObject gameObject, string childName)
    {
        Transform child = gameObject.transform.Find(childName);
        return child?.gameObject;
    }
    
    // ===== NetworkBehaviour 확장 =====
    
    /// <summary>
    /// NetworkBehaviour가 서버에서 실행 중인지 확인
    /// </summary>
    /// <param name="networkBehaviour">확인할 NetworkBehaviour</param>
    /// <returns>서버에서 실행 중이면 true</returns>
    public static bool IsServerOnly(this NetworkBehaviour networkBehaviour)
    {
        return networkBehaviour.IsServer && !networkBehaviour.IsClient;
    }
    
    /// <summary>
    /// NetworkBehaviour가 클라이언트에서 실행 중인지 확인
    /// </summary>
    /// <param name="networkBehaviour">확인할 NetworkBehaviour</param>
    /// <returns>클라이언트에서 실행 중이면 true</returns>
    public static bool IsClientOnly(this NetworkBehaviour networkBehaviour)
    {
        return networkBehaviour.IsClient && !networkBehaviour.IsServer;
    }
    
    // ===== String 확장 =====
    
    /// <summary>
    /// 문자열이 null이거나 비어있는지 확인
    /// </summary>
    /// <param name="str">확인할 문자열</param>
    /// <returns>null이거나 비어있으면 true</returns>
    public static bool IsNullOrEmpty(this string str)
    {
        return string.IsNullOrEmpty(str);
    }
    
    /// <summary>
    /// 문자열이 null이거나 공백만 있는지 확인
    /// </summary>
    /// <param name="str">확인할 문자열</param>
    /// <returns>null이거나 공백만 있으면 true</returns>
    public static bool IsNullOrWhiteSpace(this string str)
    {
        return string.IsNullOrWhiteSpace(str);
    }
    
    // ===== Array 확장 =====
    
    /// <summary>
    /// 배열이 null이거나 비어있는지 확인
    /// </summary>
    /// <typeparam name="T">배열 타입</typeparam>
    /// <param name="array">확인할 배열</param>
    /// <returns>null이거나 비어있으면 true</returns>
    public static bool IsNullOrEmpty<T>(this T[] array)
    {
        return array == null || array.Length == 0;
    }
    
    /// <summary>
    /// 배열에서 랜덤한 요소를 선택
    /// </summary>
    /// <typeparam name="T">배열 타입</typeparam>
    /// <param name="array">선택할 배열</param>
    /// <returns>랜덤한 요소 (배열이 비어있으면 default)</returns>
    public static T GetRandom<T>(this T[] array)
    {
        if (array.IsNullOrEmpty())
            return default(T);
        
        return array[Random.Range(0, array.Length)];
    }
    
    // ===== Transform 확장 =====
    
    /// <summary>
    /// Transform의 위치를 특정 방향으로 이동
    /// </summary>
    /// <param name="transform">이동할 Transform</param>
    /// <param name="direction">이동 방향</param>
    /// <param name="distance">이동 거리</param>
    public static void MoveInDirection(this Transform transform, Vector3 direction, float distance)
    {
        transform.position += direction.normalized * distance;
    }
    
    /// <summary>
    /// Transform이 특정 위치를 바라보도록 회전
    /// </summary>
    /// <param name="transform">회전할 Transform</param>
    /// <param name="targetPosition">바라볼 위치</param>
    /// <param name="speed">회전 속도</param>
    public static void LookAtSmooth(this Transform transform, Vector3 targetPosition, float speed)
    {
        Vector3 direction = targetPosition - transform.position;
        if (direction.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, speed * Time.deltaTime);
        }
    }
}
