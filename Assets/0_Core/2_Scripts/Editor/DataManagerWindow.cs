using UnityEngine;
using UnityEditor;
using System.IO;
using Battleship_Enum;

public class DataManagerWindow : EditorWindow
{
    // ---------------------------------------------------------
    // 1. 경로 상수
    // ---------------------------------------------------------
    private const string ROOT_PATH = "Assets/0_Core/2_Scripts/Data/";
    private const string PATH_UPGRADE = "UpgradeData/";
    private const string PATH_SHIP = "ShipData/";
    private const string PATH_ENEMY = "EnemyData/";
    private const string PATH_WEAPON = "WeaponData/";

    // ---------------------------------------------------------
    // 2. 내부 변수
    // ---------------------------------------------------------
    private int _selectedTab = 0;
    private string[] _tabs = { "Upgrade", "Ship", "Enemy", "Weapon" };

    private Vector2 _scrollPosLeft;
    private Vector2 _scrollPosRight;
    private float _sidebarWidth = 280f; // 버튼 추가로 너비 살짝 늘림

    // [Edit Mode] 선택된 에셋
    private ScriptableObject _selectedAsset;

    // [Create Mode] 임시 에셋
    private UpgradeData _tempUpgrade;
    private BaseShipData _tempShip;
    private EnemyData _tempEnemy;
    private BaseWeaponData _tempWeapon;

    // [Create Mode] 파일 이름
    private string _newFileName = "New_Data";

    // [Common] 에디터 캐싱
    private Editor _cachedEditor;

    // [Weapon Type Selection]
    private enum WeaponTypeSelection { Gun, Missile }
    private WeaponTypeSelection _weaponType = WeaponTypeSelection.Gun;


    // ---------------------------------------------------------
    // 3. 초기화
    // ---------------------------------------------------------
    [MenuItem("Tools/JJ Chronicles/Battleship Data Manager")]
    public static void ShowWindow()
    {
        DataManagerWindow window = GetWindow<DataManagerWindow>("Data Manager");
        window.minSize = new Vector2(1000, 600);
    }

    private void OnEnable()
    {
        RefreshAssetList();
        InitTempData();
    }

    private void InitTempData()
    {
        if (_tempUpgrade == null) _tempUpgrade = CreateInstance<UpgradeData>();
        if (_tempShip == null) _tempShip = CreateInstance<BaseShipData>();
        if (_tempEnemy == null) _tempEnemy = CreateInstance<EnemyData>();
        if (_tempWeapon == null) _tempWeapon = CreateInstance<GunData>();
    }

    private void OnGUI()
    {
        ProcessEnterKey();

        // 1. 상단 탭바
        GUILayout.BeginArea(new Rect(0, 0, position.width, 30));
        int oldTab = _selectedTab;
        _selectedTab = GUILayout.Toolbar(_selectedTab, _tabs, GUILayout.Height(25));

        if (oldTab != _selectedTab)
        {
            _selectedAsset = null;
            _cachedEditor = null;
            _newFileName = "New_Data";
            GUI.FocusControl(null);
        }
        GUILayout.EndArea();

        // 2. 메인 패널
        GUILayout.BeginArea(new Rect(0, 30, position.width, position.height - 30));
        EditorGUILayout.BeginHorizontal();

        DrawLeftPanel();  // 리스트
        DrawRightPanel(); // 폼

        EditorGUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    // ---------------------------------------------------------
    // 4. 왼쪽 패널 (리스트 + 삭제 버튼)
    // ---------------------------------------------------------
    private void DrawLeftPanel()
    {
        GUIStyle sidebarStyle = new GUIStyle(GUI.skin.box);
        sidebarStyle.normal.background = MakeTex(2, 2, new Color(0.25f, 0.25f, 0.25f, 1f));

        EditorGUILayout.BeginVertical(sidebarStyle, GUILayout.Width(_sidebarWidth), GUILayout.ExpandHeight(true));

        // 상단 버튼
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+ Create New", GUILayout.Height(30)))
        {
            _selectedAsset = null;
            _cachedEditor = null;
            _newFileName = "New_Data";
            GUI.FocusControl(null);
        }
        if (GUILayout.Button("↻", GUILayout.Width(30), GUILayout.Height(30)))
        {
            RefreshAssetList();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Existing Data List", EditorStyles.boldLabel);

        _scrollPosLeft = EditorGUILayout.BeginScrollView(_scrollPosLeft, GUIStyle.none, GUI.skin.verticalScrollbar);

        string[] guids = FindAssetsByTab();
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ScriptableObject asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (asset == null) continue;

            // [변경점] 가로 정렬 시작 (이름 버튼 + 삭제 버튼)
            EditorGUILayout.BeginHorizontal();

            // 1. 에셋 선택 버튼
            GUIStyle btnStyle = new GUIStyle(GUI.skin.button);
            btnStyle.alignment = TextAnchor.MiddleLeft;
            if (_selectedAsset == asset)
            {
                btnStyle.normal.textColor = Color.cyan;
                btnStyle.fontStyle = FontStyle.Bold;
            }

            // ExpandWidth(true)를 써서 남은 공간을 다 차지하게 함
            if (GUILayout.Button($" {asset.name}", btnStyle, GUILayout.Height(25), GUILayout.ExpandWidth(true)))
            {
                _selectedAsset = asset;
                _cachedEditor = null;
                GUI.FocusControl(null);
            }

            // 2. 삭제(X) 버튼
            GUI.backgroundColor = new Color(1f, 0.6f, 0.6f); // 연한 빨강
            if (GUILayout.Button("X", GUILayout.Width(25), GUILayout.Height(25)))
            {
                // 삭제 로직 실행
                if (DeleteAssetDirectly(asset))
                {
                    // 삭제되었다면 루프를 멈추고 리턴 (리스트 갱신을 위해)
                    GUI.backgroundColor = Color.white;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndScrollView();
                    EditorGUILayout.EndVertical();
                    return;
                }
            }
            GUI.backgroundColor = Color.white; // 색상 복구

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    // ---------------------------------------------------------
    // 5. 오른쪽 패널 (통합 뷰어)
    // ---------------------------------------------------------
    private void DrawRightPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(true));
        _scrollPosRight = EditorGUILayout.BeginScrollView(_scrollPosRight);

        EditorGUILayout.Space(10);

        if (_selectedAsset != null)
        {
            // [EDIT MODE]
            DrawInspectorFor(_selectedAsset, "Edit Mode");
            DrawEditModeButtons();
        }
        else
        {
            // [CREATE MODE]
            DrawCreateHeader();

            ScriptableObject targetTemp = GetCurrentTempObject();
            if (targetTemp != null)
            {
                DrawInspectorFor(targetTemp, "Input Form");
                DrawCreateButton(targetTemp);
            }
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawInspectorFor(ScriptableObject obj, string title)
    {
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        if (_cachedEditor == null || _cachedEditor.target != obj)
        {
            if (_cachedEditor != null) DestroyImmediate(_cachedEditor);
            _cachedEditor = Editor.CreateEditor(obj);
        }

        if (_cachedEditor != null)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUI.BeginChangeCheck();
            _cachedEditor.OnInspectorGUI();
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(obj);
            }
            EditorGUILayout.EndVertical();
        }
    }

    private void DrawCreateHeader()
    {
        EditorGUILayout.LabelField("Create New Data", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("새 데이터를 생성합니다.", MessageType.Info);
        EditorGUILayout.Space(10);

        if (_selectedTab == 3) // Weapon
        {
            WeaponTypeSelection oldType = _weaponType;
            _weaponType = (WeaponTypeSelection)EditorGUILayout.EnumPopup("Weapon Type", _weaponType);
            if (oldType != _weaponType) UpdateWeaponTempObject();
            EditorGUILayout.Space(5);
        }

        _newFileName = EditorGUILayout.TextField("File Name (English)", _newFileName);
        EditorGUILayout.Space(10);
    }

    private void DrawCreateButton(ScriptableObject sourceTemp)
    {
        EditorGUILayout.Space(20);
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Create Asset", GUILayout.Height(40)))
        {
            CreateAssetFromTemp(sourceTemp);
        }
        GUI.backgroundColor = Color.white;
    }

    private void DrawEditModeButtons()
    {
        EditorGUILayout.Space(20);

        EditorGUILayout.BeginHorizontal();

        GUI.backgroundColor = new Color(0.5f, 1f, 0.5f);
        if (GUILayout.Button("Update Data (Save)", GUILayout.Height(40)))
        {
            SaveSelectedAsset();
        }

        GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
        if (GUILayout.Button("Delete Data", GUILayout.Height(40)))
        {
            if (DeleteAssetDirectly(_selectedAsset)) return;
        }

        EditorGUILayout.EndHorizontal();
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(5);
        EditorGUILayout.HelpBox("Tip: 값을 수정하고 'Enter' 키를 누르면 자동 저장됩니다.", MessageType.None);
    }

    // ---------------------------------------------------------
    // 기능 로직 (삭제, 저장, 엔터키)
    // ---------------------------------------------------------
    private bool DeleteAssetDirectly(ScriptableObject asset)
    {
        if (asset == null) return false;

        if (EditorUtility.DisplayDialog("Delete Asset", $"정말 '{asset.name}'을 삭제하시겠습니까?\n(이 작업은 되돌릴 수 없습니다)", "Yes", "No"))
        {
            string path = AssetDatabase.GetAssetPath(asset);

            // 만약 삭제하려는게 현재 보고 있던거면 선택 해제
            if (_selectedAsset == asset)
            {
                _selectedAsset = null;
                _cachedEditor = null;
            }

            AssetDatabase.DeleteAsset(path);
            RefreshAssetList();
            Debug.Log($"[Data Manager] '{asset.name}' 삭제 완료.");
            return true; // 삭제됨
        }
        return false; // 취소됨
    }

    private void ProcessEnterKey()
    {
        Event e = Event.current;
        if (e.type == EventType.KeyDown)
        {
            if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
            {
                if (_selectedAsset != null)
                {
                    SaveSelectedAsset();
                    e.Use();
                    Repaint();
                }
            }
        }
    }

    private void SaveSelectedAsset()
    {
        if (_selectedAsset == null) return;
        GUI.FocusControl(null);
        EditorUtility.SetDirty(_selectedAsset);
        AssetDatabase.SaveAssets();
        Debug.Log($"[Data Manager] '{_selectedAsset.name}' 저장(Update) 완료!");
    }

    private ScriptableObject GetCurrentTempObject()
    {
        switch (_selectedTab)
        {
            case 0: return _tempUpgrade;
            case 1: return _tempShip;
            case 2: return _tempEnemy;
            case 3: return _tempWeapon;
        }
        return null;
    }

    private void UpdateWeaponTempObject()
    {
        if (_tempWeapon != null) DestroyImmediate(_tempWeapon);
        if (_weaponType == WeaponTypeSelection.Gun) _tempWeapon = CreateInstance<GunData>();
        else _tempWeapon = CreateInstance<HomingWeaponData>();
        _cachedEditor = null;
    }

    private void CreateAssetFromTemp(ScriptableObject sourceTemp)
    {
        GUI.FocusControl(null);

        string relativePath = "";
        switch (_selectedTab)
        {
            case 0: relativePath = PATH_UPGRADE; break;
            case 1: relativePath = PATH_SHIP; break;
            case 2: relativePath = PATH_ENEMY; break;
            case 3: relativePath = PATH_WEAPON + (_weaponType == WeaponTypeSelection.Gun ? "Gun/" : "HormingWeapon/"); break;
        }

        string fullPath = ROOT_PATH + relativePath;
        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
            AssetDatabase.Refresh();
        }

        ScriptableObject newData = CreateInstance(sourceTemp.GetType());
        EditorUtility.CopySerialized(sourceTemp, newData);

        if (string.IsNullOrEmpty(_newFileName)) _newFileName = "New_Data";
        string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(fullPath + _newFileName + ".asset");

        AssetDatabase.CreateAsset(newData, assetPathAndName);
        AssetDatabase.SaveAssets();

        RefreshAssetList();
        _selectedAsset = newData;
        GUI.FocusControl(null);
        Debug.Log($"[Data Manager] {_newFileName} 생성 완료!");
    }

    private string[] FindAssetsByTab()
    {
        string typeFilter = "";
        switch (_selectedTab)
        {
            case 0: typeFilter = "t:UpgradeData"; break;
            case 1: typeFilter = "t:BaseShipData"; break;
            case 2: typeFilter = "t:EnemyData"; break;
            case 3: typeFilter = "t:BaseWeaponData"; break;
        }
        return AssetDatabase.FindAssets(typeFilter);
    }

    private void RefreshAssetList()
    {
        AssetDatabase.Refresh();
        Repaint();
    }

    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; ++i) pix[i] = col;
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }
}