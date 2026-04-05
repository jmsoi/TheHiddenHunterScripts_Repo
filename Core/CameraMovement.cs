using UnityEngine;
using Unity.Netcode;

public class CameraMovement : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 0.125f;
    public Vector3 offset;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    // public void SetTarget()
    // {
    //     // 예시: PlayerController가 플레이어 스크립트라고 가정
    //     var players = FindObjectsOfType<PlayerController>();
    //     foreach (var player in players)
    //     {
    //         if (player.IsOwner) // 호스트의 플레이어 오브젝트
    //         {
    //             target = player.transform;
    //             break;
    //         }
    //     }
    // }

    // Update is called once per frame
    // void Update()
    // {
        
    // }

    void LateUpdate()
    {
        if (target != null)
        {
            Vector3 desiredPosition = target.position + offset;
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
            transform.position = smoothedPosition;
        }
    }
}
