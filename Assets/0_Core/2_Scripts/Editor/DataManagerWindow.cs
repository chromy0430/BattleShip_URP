using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Battleship_Enum;
using BattleShip; // BaseEquipmentData, BaseWeaponData 등 네임스페이스 참조

public class DataManagerWindow : EditorWindow
{
    private enum TabType { Weapon, Equipment, Cargo, Upgrade, Ship, Enemy }
    private TabType currentTab = TabType.Weapon;

    // 리스트 관리
    private Vector2 leftPanelScrollPos;
    private Dictionary<string, List<ScriptableObject>> groupedData = new Dictionary<string, List<ScriptableObject>>();
    private Dictionary<string, bool> groupFoldouts = new Dictionary<string, bool>();

    private ScriptableObject selectedData;
    private Editor cachedEditor;
    private Vector2 inspectorScrollPos;

    // 생성용 변수
    private ScriptableObject tempCreationData;
    private Editor tempCreationEditor;
    private string newFileName = "";

    // [설정] 아이콘 크기 (이제 사용됨)
    private float iconSize = 32f;
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
        if (cachedEditor != null) DestroyImmediate(cachedEditor);
        if (tempCreationEditor != null) DestroyImmediate(tempCreationEditor);
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();
        DrawLeftPanel();
        DrawRightPanel();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawLeftPanel()
    {
        EditorGUILayout.BeginVertical("box", GUILayout.Width(300), GUILayout.ExpandHeight(true));

        // 상단 탭 버튼
        EditorGUI.BeginChangeCheck();
        currentTab = (TabType)GUILayout.Toolbar((int)currentTab, System.Enum.GetNames(typeof(TabType)));

        // [수정] 탭이 변경되었을 때 실행되는 블록
        if (EditorGUI.EndChangeCheck())
        {
            // 1. 선택된 데이터 초기화
            selectedData = null;
            if (cachedEditor != null) DestroyImmediate(cachedEditor);

            // 2. [추가] 생성 모드 초기화 (이게 빠져서 문제가 발생했습니다)
            tempCreationData = null;
            if (tempCreationEditor != null) DestroyImmediate(tempCreationEditor);
            newFileName = ""; // 파일 이름 입력창도 초기화

            // 3. 리스트 갱신
            RefreshList();
        }

        GUILayout.Space(10);

        // 스크롤 뷰 시작
        leftPanelScrollPos = EditorGUILayout.BeginScrollView(leftPanelScrollPos);

        foreach (var group in groupedData)
        {
            string groupName = group.Key;
            List<ScriptableObject> items = group.Value;
            int count = items.Count;

            // 그룹 헤더
            if (!groupFoldouts.ContainsKey(groupName)) groupFoldouts[groupName] = true;

            GUI.backgroundColor = new Color(0.8f, 0.8f, 0.8f);
            EditorGUILayout.BeginHorizontal("HelpBox");
            groupFoldouts[groupName] = EditorGUILayout.Foldout(groupFoldouts[groupName], $"{groupName} ({count})", true);
            EditorGUILayout.EndHorizontal();
            GUI.backgroundColor = Color.white;

            if (groupFoldouts[groupName])
            {
                EditorGUI.indentLevel++;
                foreach (var data in items)
                {
                    if (data == null) continue;

                    EditorGUILayout.BeginHorizontal();

                    // [추가됨] 아이콘 그리기
                    Texture2D icon = GetIconForData(data);
                    if (icon != null)
                    {
                        // 아이콘을 그립니다.
                        GUILayout.Label(icon, GUILayout.Width(iconSize), GUILayout.Height(iconSize));
                    }
                    else
                    {
                        // 아이콘이 없으면 빈 공간만 둡니다.
                        GUILayout.Space(iconSize + 4);
                    }

                    // 선택 버튼 (높이를 아이콘에 맞춤)
                    GUI.backgroundColor = (selectedData == data) ? Color.cyan : Color.white;
                    if (GUILayout.Button(data.name, EditorStyles.miniButtonLeft, GUILayout.Height(iconSize))) // 높이 수정
                    {
                        selectedData = data;
                        if (cachedEditor != null) DestroyImmediate(cachedEditor);
                        cachedEditor = Editor.CreateEditor(selectedData);
                        GUI.FocusControl(null);
                    }
                    GUI.backgroundColor = Color.white;

                    // 삭제 버튼
                    GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                    if (GUILayout.Button("X", EditorStyles.miniButtonRight, GUILayout.Width(20), GUILayout.Height(iconSize))) // 높이 수정
                    {
                        if (EditorUtility.DisplayDialog("Delete Data", $"Delete '{data.name}'?", "Yes", "No"))
                        {
                            DeleteData(data);
                            return;
                        }
                    }
                    GUI.backgroundColor = Color.white;

                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.indentLevel--;
                GUILayout.Space(5);
            }
        }

        EditorGUILayout.EndScrollView();

        GUILayout.Space(5);
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Create New Data", GUILayout.Height(30)))
        {
            PrepareCreation();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndVertical();
    }

    // [추가됨] 데이터 타입별 아이콘 가져오기 헬퍼 함수
    private Texture2D GetIconForData(ScriptableObject data)
    {
        if (data == null) return null;

        // 1. ShipData
        if (data is BaseShipData ship && ship.shipIcon != null)
            return ship.shipIcon.texture;

        // 2. EnemyData
        if (data is EnemyData enemy && enemy.MinimapIcon != null)
            return enemy.MinimapIcon.texture;

        // 3. ItemData (Weapon, Equipment, Cargo) - BaseItemData에 Icon 필드가 있다고 가정
        // 사용자가 업로드한 파일에는 BaseItemData 내용이 없지만, 보통 Icon 필드가 존재합니다.
        // 만약 필드명이 다르다면 아래 코드를 수정해야 합니다.
        if (data is BaseItemData item)
        {
            // 리플렉션이나 캐스팅으로 가져올 수 있습니다. BaseItemData에 public Sprite Icon;이 있다고 가정.
            // 여기서는 안전하게 타입 캐스팅 시도
            System.Reflection.FieldInfo iconField = data.GetType().GetField("Icon"); // 혹은 "itemIcon" 등 실제 변수명
            if (iconField != null)
            {
                Sprite sprite = iconField.GetValue(data) as Sprite;
                if (sprite != null) return sprite.texture;
            }
        }

        return null;
    }

    // ... (DrawRightPanel, DrawInspectorMode, DrawCreationMode는 기존과 동일) ...
    private void DrawRightPanel()
    {
        EditorGUILayout.BeginVertical("box", GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        inspectorScrollPos = EditorGUILayout.BeginScrollView(inspectorScrollPos);

        if (tempCreationData != null) DrawCreationMode();
        else if (selectedData != null) DrawInspectorMode();
        else EditorGUILayout.HelpBox("Select an item from the left list or click 'Create New Data'.", MessageType.Info);

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawInspectorMode()
    {
        EditorGUILayout.LabelField($"Editing: {selectedData.name}", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        if (cachedEditor == null) cachedEditor = Editor.CreateEditor(selectedData);
        EditorGUI.BeginChangeCheck();
        cachedEditor.OnInspectorGUI();
        if (EditorGUI.EndChangeCheck()) EditorUtility.SetDirty(selectedData);
    }

    private void DrawCreationMode()
    {
        EditorGUILayout.LabelField($"Create New {currentTab}", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        newFileName = EditorGUILayout.TextField("File Name", newFileName);
        if (tempCreationEditor == null) tempCreationEditor = Editor.CreateEditor(tempCreationData);
        tempCreationEditor.OnInspectorGUI();
        GUILayout.Space(10);
        GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
        if (GUILayout.Button("Save & Create", GUILayout.Height(40))) CreateDataFile();
        GUI.backgroundColor = Color.white;
        if (GUILayout.Button("Cancel")) { tempCreationData = null; selectedData = null; }
    }

    private void RefreshList()
    {
        groupedData.Clear();
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

        string[] guids = AssetDatabase.FindAssets(typeFilter);

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ScriptableObject asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (asset != null)
            {
                string groupKey = GetGroupKey(asset);
                if (!groupedData.ContainsKey(groupKey)) groupedData[groupKey] = new List<ScriptableObject>();
                groupedData[groupKey].Add(asset);
            }
        }

        // 정렬: 이름순
        foreach (var key in groupedData.Keys.ToList())
        {
            groupedData[key] = groupedData[key].OrderBy(x => x.name).ToList();
        }
    }

    private string GetGroupKey(ScriptableObject asset)
    {
        switch (currentTab)
        {
            case TabType.Ship:
                // [수정 1] ShipSize (대문자 S) -> shipSize (소문자 s)
                if (asset is BaseShipData ship) return ship.shipSize.ToString();
                break;

            case TabType.Enemy:
                if (asset is EnemyData enemy) return enemy.AttackType.ToString();
                break;

            case TabType.Upgrade:
                if (asset is UpgradeData upgrade)
                    return string.IsNullOrEmpty(upgrade.relatedWeaponID) ? "Common (Stat)" : $"Weapon: {upgrade.relatedWeaponID}";
                break;

            case TabType.Equipment:
                // [수정 2] EquipSlot (대문자 E) -> equipSlot (소문자 e)
                if (asset is BaseEquipmentData equip) return equip.equipSlot.ToString();
                break;

            case TabType.Weapon:
                if (asset is BaseWeaponData weapon) return weapon.IsHoming ? "Homing" : "Non-Homing";
                break;

            case TabType.Cargo:
                return "Default";
        }

        return "Uncategorized";
    }

    private void PrepareCreation()
    {
        selectedData = null;
        if (cachedEditor != null) DestroyImmediate(cachedEditor);
        newFileName = $"New_{currentTab}";

        switch (currentTab)
        {
            case TabType.Weapon: tempCreationData = CreateInstance<BaseWeaponData>(); break;
            case TabType.Equipment: tempCreationData = CreateInstance<BaseEquipmentData>(); break;
            case TabType.Cargo: tempCreationData = CreateInstance<BaseCargoData>(); break;
            case TabType.Upgrade: tempCreationData = CreateInstance<UpgradeData>(); break;
            case TabType.Ship: tempCreationData = CreateInstance<BaseShipData>(); break;
            case TabType.Enemy: tempCreationData = CreateInstance<EnemyData>(); break;
        }

        if (tempCreationEditor != null) DestroyImmediate(tempCreationEditor);
        tempCreationEditor = Editor.CreateEditor(tempCreationData);
    }

    private void CreateDataFile()
    {
        if (string.IsNullOrEmpty(newFileName))
        {
            EditorUtility.DisplayDialog("Error", "Please enter a file name.", "OK");
            return;
        }

        string folderPath = $"{BasePath}/{currentTab}Data";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            if (!AssetDatabase.IsValidFolder(BasePath)) AssetDatabase.CreateFolder("Assets/0_Core/2_Scripts", "Data");
            AssetDatabase.CreateFolder(BasePath, $"{currentTab}Data");
        }

        string fullPath = $"{folderPath}/{newFileName}.asset";
        fullPath = AssetDatabase.GenerateUniqueAssetPath(fullPath);

        AssetDatabase.CreateAsset(tempCreationData, fullPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Created: {fullPath}");
        tempCreationData = null;
        tempCreationEditor = null;
        newFileName = "";
        RefreshList();
        selectedData = AssetDatabase.LoadAssetAtPath<ScriptableObject>(fullPath);
    }

    private void DeleteData(ScriptableObject data)
    {
        string path = AssetDatabase.GetAssetPath(data);
        AssetDatabase.DeleteAsset(path);
        RefreshList();
        selectedData = null;
    }
}

/*using UnityEngine;
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
}*/