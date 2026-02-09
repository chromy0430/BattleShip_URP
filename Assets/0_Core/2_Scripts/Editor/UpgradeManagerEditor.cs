using UnityEngine;
using UnityEditor;
using BattleShip;
using System.Collections.Generic;

[CustomEditor(typeof(UpgradeManager))]
public class UpgradeManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EditorGUILayout.Space(20);
        EditorGUILayout.LabelField("Auto Data Loader", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("프로젝트 내의 ScriptableObject 데이터를 자동으로 찾아 리스트에 등록합니다.", MessageType.Info);

        // 1. 전체 로드 버튼 (가장 많이 씀)
        if (GUILayout.Button("Load ALL Data (Everything)", GUILayout.Height(40)))
        {
            LoadAllUpgradeData();
            LoadAllWeaponData();
            LoadAllEquipmentData();
            LoadAllCargoData();
            Debug.Log("<color=green>[UpgradeManagerEditor] 모든 데이터 로드 완료!</color>");
        }

        EditorGUILayout.Space(10);

        // 2. 개별 로드 버튼들 (필요할 때만)
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Load Upgrades")) LoadAllUpgradeData();
        if (GUILayout.Button("Load Weapons")) LoadAllWeaponData();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Load Equipments")) LoadAllEquipmentData();
        if (GUILayout.Button("Load Cargos")) LoadAllCargoData();
        GUILayout.EndHorizontal();
    }

    // -----------------------------------------------------------------------
    // 1. UpgradeData 로드
    // -----------------------------------------------------------------------
    private void LoadAllUpgradeData()
    {
        List<UpgradeData> dataList = FindAssetsByType<UpgradeData>();
        UpdateSerializedList("allUpgrades", dataList);
    }

    // -----------------------------------------------------------------------
    // 2. WeaponData 로드
    // -----------------------------------------------------------------------
    private void LoadAllWeaponData()
    {
        List<BaseWeaponData> dataList = FindAssetsByType<BaseWeaponData>();
        UpdateSerializedList("allWeapons", dataList);
    }

    // -----------------------------------------------------------------------
    // 3. EquipmentData 로드
    // -----------------------------------------------------------------------
    private void LoadAllEquipmentData()
    {
        List<BaseEquipmentData> dataList = FindAssetsByType<BaseEquipmentData>();
        UpdateSerializedList("allEquipments", dataList);
    }

    // -----------------------------------------------------------------------
    // 4. CargoData 로드
    // -----------------------------------------------------------------------
    private void LoadAllCargoData()
    {
        List<BaseCargoData> dataList = FindAssetsByType<BaseCargoData>();
        UpdateSerializedList("allCargos", dataList);
    }

    // -----------------------------------------------------------------------
    // [공통 유틸리티] 제네릭 에셋 검색 함수
    // -----------------------------------------------------------------------
    private List<T> FindAssetsByType<T>() where T : ScriptableObject
    {
        string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
        List<T> list = new List<T>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) list.Add(asset);
        }
        return list;
    }

    // -----------------------------------------------------------------------
    // [공통 유틸리티] 리스트 갱신 함수
    // -----------------------------------------------------------------------
    private void UpdateSerializedList<T>(string propertyName, List<T> newData) where T : Object
    {
        SerializedProperty listProperty = serializedObject.FindProperty(propertyName);
        if (listProperty == null)
        {
            Debug.LogError($"[UpgradeManagerEditor] '{propertyName}' 리스트를 찾을 수 없습니다. 변수명을 확인하세요.");
            return;
        }

        listProperty.ClearArray();

        for (int i = 0; i < newData.Count; i++)
        {
            listProperty.InsertArrayElementAtIndex(i);
            SerializedProperty element = listProperty.GetArrayElementAtIndex(i);
            element.objectReferenceValue = newData[i];
        }

        serializedObject.ApplyModifiedProperties(); // 저장
        Debug.Log($"[{propertyName}] 갱신 완료: {newData.Count}개 항목");
    }
}