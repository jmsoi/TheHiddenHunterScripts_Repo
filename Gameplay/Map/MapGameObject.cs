using System.Collections.Generic;
using UnityEngine;

public class MapGameObject : MonoBehaviour
{
    public int idx;
    public MapObject mapObject;
}

// MapObject를 ResourceType으로 변환하는 확장 메서드
public static class MapObjectExtensions
{
    public static ResourceType ToResourceType(this MapObject mapObject)
    {
        switch (mapObject)
        {
            case MapObject.Blue:
                return ResourceType.Blue;
            case MapObject.Red:
                return ResourceType.Red;
            case MapObject.Yellow:
                return ResourceType.Yellow;
            default:
                return ResourceType.None;
        }
    }
    
    public static bool IsResource(this MapObject mapObject)
    {
        return mapObject == MapObject.Blue || 
               mapObject == MapObject.Red || 
               mapObject == MapObject.Yellow;
    }
}