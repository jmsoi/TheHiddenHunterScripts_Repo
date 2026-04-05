using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CreateAssetMenu(fileName = "Skill Database", menuName = "HiddenHunter/Skill Database")]
public class SkillDatabase : ScriptableObject
{
    [Header("상점 아이템 목록")]
    public List<Skill> skills = new List<Skill>();
    
    
    [ContextMenu("스킬 목록 초기화")]
    public void Initialize()
    {
        // 기존 리스트를 비우고 새로 추가
        skills.Clear();
        skills.Add(new Skill(SkillType.None, 0, "테스트", 0f, ResourceType.None, 0, null, "테스트"));
        
        // 자원1 (🔵) - CC/도주/교란 스킬들
        skills.Add(new Skill(SkillType.Move, 0, "은신+이속 증가", 5f, ResourceType.Blue, 3, null, "3초간 투명 + 이동속도 30% 증가"));
        skills.Add(new Skill(SkillType.Move, 1, "암흑 시야", 5f, ResourceType.Blue, 4, null, "4초간 적 시야 제한 (원 안이 어둡게 보임)"));
        skills.Add(new Skill(SkillType.Move, 2, "그림자 귀환", 5f, ResourceType.Blue, 5, null, "표식 위치로 순간 귀환 (쿨타임 존재)"));
        
        // 자원2 (🔴) - 공격/전투 스킬들
        skills.Add(new Skill(SkillType.Attack, 0, "검", 5f, ResourceType.Red, 4, null, "근접 휘두름 - 1회 공격 가능, 빠른 속도"));
        skills.Add(new Skill(SkillType.Attack, 1, "활", 5f, ResourceType.Red, 4, null, "원거리 발사 - 맞으면 즉사 (총알 1발)"));
        skills.Add(new Skill(SkillType.Attack, 2, "지뢰", 3f, ResourceType.Red, 3, null, "설치형 - 밟으면 즉사 + 작은 범위 폭발"));
        
        // 자원3 (🟡) - 패시브/승리 조건 스킬들
        skills.Add(new Skill(SkillType.Passive, 0, "자원수집 승리", 1f, ResourceType.Yellow, 12, null, "자원1·2·3을 일정량 확보하면 즉시 승리 조건 달성"));
        skills.Add(new Skill(SkillType.Passive, 1, "NPC 전멸 승리", 1f, ResourceType.Yellow, 10, null, "맵의 NPC 모두 제거 시 즉시 승리"));
        skills.Add(new Skill(SkillType.Passive, 2, "방어막", 1f, ResourceType.Yellow, 6, null, "공격 1회 무효화 (한 번만 발동)"));
    }

    // 아이템 이름으로 찾기
    public Skill GetItemByName(string itemName)
    {
        return skills.Find(item => item._name == itemName);
    }
    
    // 인덱스로 찾기
    public Skill GetItemByIndex(int index)
    {
        if (index >= 0 && index < skills.Count)
            return skills[index];
        return null;
    }
    
    public Skill IndexAndSkillTypeToSkill(SkillType skill_type, int index)
    {
        if (skill_type == SkillType.None)
            return skills[0];
        else if (skill_type == SkillType.Move)
        {
            if (index == 0)
                return skills[1];
            else if (index == 1)
                return skills[2];
            else if (index == 2)
                return skills[3];
        }
        else if (skill_type == SkillType.Attack)
        {
            if (index == 0)
                return skills[4];
            else if (index == 1)
                return skills[5];
            else if (index == 2)
                return skills[6];
        }
        else if (skill_type == SkillType.Passive)
        {
            if (index == 0)
                return skills[7];
            else if (index == 1)
                return skills[8];
            else if (index == 2)
                return skills[9];
        }
        return null;
    }
}

#if UNITY_EDITOR

[CustomEditor(typeof(SkillDatabase))]
public class SkillDatabaseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        SkillDatabase database = (SkillDatabase)target;
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("에디터 도구", EditorStyles.boldLabel);
        
        if (GUILayout.Button("스킬 목록 초기화"))
        {
            database.Initialize();
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            Debug.Log("SkillDatabase가 초기화되었습니다!");
        }
    }
}
#endif