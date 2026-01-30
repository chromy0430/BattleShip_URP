using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Battleship_Enum;
using BattleShip;

public class DataManagerWindow : EditorWindow
{
    private enum TabType { Weapon, Equipment, Cargo, Upgrade, Ship, Enemy }
    private TabType currentTab = TabType.Weapon;

    // 리스트 관리 (좌측 패널용 스크롤)
    private Vector2 leftPanelScrollPos;
    private List<ScriptableObject> currentList = new List<ScriptableObject>();
    private ScriptableObject selectedData;
    private Editor cachedEditor;

    // [수정] 오른쪽 패널용 스크롤 변수 추가
    private Vector2 inspectorScrollPos;

    // 생성용 임시 데이터 & 에디터
    private ScriptableObject tempCreationData;
    private Editor tempCreationEditor;
    private string newFileName = "";

    // [설정] 아이콘 크기
    private float iconSize = 32f;

    // [설정] 기본 경로
    private string BasePath => "Assets/0_Core/2_Scripts/Data";

    [MenuItem("BattleShip/Data Manager")]
    public static void ShowWindow()
    {
        GetWindow<DataManagerWindow>("Data Manager");
    }

    private void OnEnable()
    {
        RefreshList();
    }

    private void OnDisable()
    {
        if (tempCreationData != null)
        {
            DestroyImmediate(tempCreationData);
        }
    }

    private void OnGUI()
    {
        // 1. 상단 탭
        GUILayout.BeginHorizontal();
        string[] tabNames = { "무기", "장비", "화물", "강화", "함선", "적" };
        EditorGUI.BeginChangeCheck();
        currentTab = (TabType)GUILayout.Toolbar((int)currentTab, tabNames, GUILayout.Height(30));
        if (EditorGUI.EndChangeCheck())
        {
            // 탭 변경 시 초기화
            selectedData = null;
            cachedEditor = null;
            newFileName = "";
            inspectorScrollPos = Vector2.zero; // 탭 바꿀때 스크롤 초기화

            if (tempCreationData != null) DestroyImmediate(tempCreationData);
            tempCreationData = null;
            tempCreationEditor = null;

            RefreshList();
        }
        GUILayout.EndHorizontal();

        // 2. 패널 분할
        EditorGUILayout.BeginHorizontal();
        DrawLeftPanel();
        DrawRightPanel();
        EditorGUILayout.EndHorizontal();
    }

    // 좌측: 리스트
    private void DrawLeftPanel()
    {
        float panelWidth = 300f;
        EditorGUILayout.BeginVertical("box", GUILayout.Width(panelWidth), GUILayout.ExpandHeight(true));

        if (GUILayout.Button("Refresh List", GUILayout.Height(25))) RefreshList();
        GUILayout.Space(5);

        // [수정] 좌측 패널 전용 변수 사용
        leftPanelScrollPos = EditorGUILayout.BeginScrollView(leftPanelScrollPos);

        if (currentList != null)
        {
            for (int i = 0; i < currentList.Count; i++)
            {
                var data = currentList[i];
                if (data == null) continue;

                bool isSelected = (selectedData == data);
                if (isSelected) GUI.backgroundColor = Color.cyan;
                else GUI.backgroundColor = Color.white;

                EditorGUILayout.BeginHorizontal("box", GUILayout.Height(iconSize + 4));

                // 1. 아이콘
                Texture2D iconTex = GetIconTexture(data);
                if (iconTex != null)
                {
                    GUI.DrawTexture(GUILayoutUtility.GetRect(iconSize, iconSize, GUILayout.Width(iconSize)), iconTex, ScaleMode.ScaleToFit);
                }
                else
                {
                    GUILayout.Space(iconSize);
                }

                // 2. 이름 버튼
                GUILayout.BeginVertical();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(data.name, EditorStyles.label, GUILayout.Height(iconSize)))
                {
                    selectedData = data;
                    cachedEditor = Editor.CreateEditor(data);
                    inspectorScrollPos = Vector2.zero; // 선택 변경 시 스크롤 맨 위로

                    if (tempCreationData != null) DestroyImmediate(tempCreationData);
                    tempCreationData = null;

                    GUI.FocusControl(null);
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndVertical();

                // 3. 삭제 버튼
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("X", EditorStyles.miniButton, GUILayout.Width(20), GUILayout.Height(20)))
                {
                    if (EditorUtility.DisplayDialog("데이터 삭제", $"{data.name} 삭제하시겠습니까?", "삭제", "취소"))
                    {
                        DeleteData(data);
                        RefreshList();
                        GUIUtility.ExitGUI();
                    }
                }
                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    // 우측: 상세 설정 or 생성
    private void DrawRightPanel()
    {
        EditorGUILayout.BeginVertical("box", GUILayout.ExpandHeight(true));

        if (selectedData != null)
        {
            // --- [편집 모드] ---
            GUILayout.Label($"[Edit] {selectedData.name}", EditorStyles.boldLabel);
            GUILayout.Space(10);

            if (cachedEditor == null) cachedEditor = Editor.CreateEditor(selectedData);

            // [수정] Vector2.zero 대신 inspectorScrollPos 변수 사용 -> 스크롤 기억됨
            inspectorScrollPos = EditorGUILayout.BeginScrollView(inspectorScrollPos);
            cachedEditor.OnInspectorGUI();
            EditorGUILayout.EndScrollView();

            GUILayout.Space(10);
            if (GUILayout.Button("Save Changes", GUILayout.Height(30)))
            {
                EditorUtility.SetDirty(selectedData);
                AssetDatabase.SaveAssets();
                Debug.Log($"{selectedData.name} 저장 완료!");
            }
        }
        else
        {
            // --- [생성 모드] ---
            GUILayout.Label($"Create New {currentTab}", EditorStyles.boldLabel);
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.Label("File Name:", GUILayout.Width(80));
            newFileName = EditorGUILayout.TextField(newFileName);
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            if (tempCreationData == null)
            {
                InitTempCreationData();
            }

            if (tempCreationData != null)
            {
                if (tempCreationEditor == null) tempCreationEditor = Editor.CreateEditor(tempCreationData);

                GUILayout.Label("Initial Values (설정 후 Create 누르세요):", EditorStyles.helpBox);

                // [수정] 여기도 inspectorScrollPos 사용
                inspectorScrollPos = EditorGUILayout.BeginScrollView(inspectorScrollPos);
                tempCreationEditor.OnInspectorGUI();
                EditorGUILayout.EndScrollView();
            }

            GUILayout.Space(10);

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Create Data File", GUILayout.Height(40)))
            {
                CreateNewData();
            }
            GUI.backgroundColor = Color.white;
        }

        EditorGUILayout.EndVertical();
    }

    // 아이콘 텍스처 추출
    private Texture2D GetIconTexture(ScriptableObject data)
    {
        if (data == null) return null;

        Sprite sprite = null;

        if (data is BaseItemData itemData)
        {
            sprite = itemData.Icon;
        }
        else
        {
            var type = data.GetType();
            var field = type.GetField("Icon") ?? type.GetField("icon");
            if (field != null)
            {
                var val = field.GetValue(data);
                if (val is Sprite s) sprite = s;
            }
        }

        return sprite != null ? sprite.texture : null;
    }

    private void InitTempCreationData()
    {
        switch (currentTab)
        {
            case TabType.Weapon: tempCreationData = CreateInstance<BaseWeaponData>(); break;
            case TabType.Equipment: tempCreationData = CreateInstance<BaseEquipmentData>(); break;
            case TabType.Cargo: tempCreationData = CreateInstance<BaseCargoData>(); break;
            case TabType.Upgrade: tempCreationData = CreateInstance<UpgradeData>(); break;
            case TabType.Ship: tempCreationData = CreateInstance<BaseShipData>(); break;
            case TabType.Enemy: tempCreationData = CreateInstance<EnemyData>(); break;
        }
    }

    private void CreateNewData()
    {
        if (string.IsNullOrEmpty(newFileName))
        {
            EditorUtility.DisplayDialog("Error", "파일 이름을 입력해주세요.", "OK");
            return;
        }

        if (tempCreationData == null) InitTempCreationData();

        string folderName = "";
        switch (currentTab)
        {
            case TabType.Weapon: folderName = "WeaponData"; break;
            case TabType.Equipment: folderName = "EquipmentData"; break;
            case TabType.Cargo: folderName = "CargoData"; break;
            case TabType.Upgrade: folderName = "UpgradeData"; break;
            case TabType.Ship: folderName = "ShipData"; break;
            case TabType.Enemy: folderName = "EnemyData"; break;
        }

        string fullFolderPath = $"{BasePath}/{folderName}";

        if (!Directory.Exists(fullFolderPath))
        {
            Directory.CreateDirectory(fullFolderPath);
            AssetDatabase.Refresh();
        }

        string fullPath = $"{fullFolderPath}/{newFileName}.asset";
        fullPath = AssetDatabase.GenerateUniqueAssetPath(fullPath);

        AssetDatabase.CreateAsset(tempCreationData, fullPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"생성 완료: {fullPath}");

        tempCreationData = null;
        tempCreationEditor = null;
        newFileName = "";

        RefreshList();
        selectedData = AssetDatabase.LoadAssetAtPath<ScriptableObject>(fullPath);
    }

    private void RefreshList()
    {
        currentList.Clear();
        string[] guids = new string[0];
        string typeFilter = "";

        switch (currentTab)
        {
            case TabType.Weapon: typeFilter = "t:BaseWeaponData"; break;
            case TabType.Equipment: typeFilter = "t:BaseEquipmentData"; break;
            case TabType.Cargo: typeFilter = "t:BaseCargoData"; break;
            case TabType.Upgrade: typeFilter = "t:UpgradeData"; break;
            case TabType.Ship: typeFilter = "t:BaseShipData"; break;
            case TabType.Enemy: typeFilter = "t:EnemyData"; break;
        }

        guids = AssetDatabase.FindAssets(typeFilter);

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ScriptableObject asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (asset != null) currentList.Add(asset);
        }
    }

    private void DeleteData(ScriptableObject data)
    {
        string path = AssetDatabase.GetAssetPath(data);
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.SaveAssets();
        selectedData = null;
        cachedEditor = null;
    }
}