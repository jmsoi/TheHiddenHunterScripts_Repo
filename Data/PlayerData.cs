using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlayerData
{
    public bool isGameStarted = false;
    
    // 순수 데이터만 포함, 로직은 Manager로 이동
    public Dictionary<ResourceType, int> resources = new Dictionary<ResourceType, int>();
    public Skill[] skills = new Skill[2];
    
    public PlayerData()
    {
        resources = new Dictionary<ResourceType, int>();
        skills = new Skill[2];
        for (int i = 0; i < skills.Length; i++)
        {
            skills[i] = new Skill();
        }
    }
}





