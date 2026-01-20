using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
[CustomEditor(typeof(StickyNote))]
public class StickyNoteEditor : Editor
{
    private GUIStyle _noteStyle;
    private GUIStyle _titleStyle;
    private GUIStyle _bodyStyle;
    private bool _stylesInitialized = false;

    private SerializedProperty _titleProp;
    private SerializedProperty _bodyProp;
    private SerializedProperty _colorProp;

    // [추가됨] 스크롤의 현재 위치(x, y)를 기억하는 변수
    private Vector2 _scrollPosition;

    private void OnEnable()
    {
        _titleProp = serializedObject.FindProperty("noteTitle");
        _bodyProp = serializedObject.FindProperty("noteBody");
        _colorProp = serializedObject.FindProperty("noteColor");
    }

    private void InitStyles()
    {
        if (_stylesInitialized) return;

        _noteStyle = new GUIStyle(EditorStyles.helpBox);
        _noteStyle.padding = new RectOffset(15, 15, 15, 15);
        _noteStyle.margin = new RectOffset(10, 10, 10, 10);

        _titleStyle = new GUIStyle(EditorStyles.boldLabel);
        _titleStyle.fontSize = 14;
        _titleStyle.alignment = TextAnchor.MiddleLeft;
        _titleStyle.normal.textColor = Color.black;

        _bodyStyle = new GUIStyle(EditorStyles.textArea);
        _bodyStyle.wordWrap = true;
        _bodyStyle.fontSize = 12;
        // [중요] 텍스트가 길어지면 영역도 같이 늘어나게 설정 (스크롤뷰 내부에서 작동)
        _bodyStyle.stretchHeight = true;

        _stylesInitialized = true;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        InitStyles();

        Color backgroundColor = GetColorFromEnum((NoteColor)_colorProp.enumValueIndex);
        GUI.backgroundColor = backgroundColor;

        EditorGUILayout.BeginVertical(_noteStyle);
        {
            // 상단: 타이틀 및 색상
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.PrefixLabel("Title");
                _titleProp.stringValue = EditorGUILayout.TextField(_titleProp.stringValue, _titleStyle);
                EditorGUILayout.PropertyField(_colorProp, GUIContent.none, GUILayout.Width(60));
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Content", EditorStyles.miniLabel);

            // [핵심 변경] 스크롤 뷰 시작
            // GUILayout.Height(150): 노트의 높이를 150픽셀로 고정하고 그 안에서 스크롤하게 함
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(150));
            {
                // 내용은 길어질 수 있으므로 ExpandHeight를 true로 설정
                _bodyProp.stringValue = EditorGUILayout.TextArea(
                    _bodyProp.stringValue,
                    _bodyStyle,
                    GUILayout.ExpandHeight(true)
                );
            }
            EditorGUILayout.EndScrollView(); // 스크롤 뷰 끝
        }
        EditorGUILayout.EndVertical();

        serializedObject.ApplyModifiedProperties();
        GUI.backgroundColor = Color.white;
    }

    private Color GetColorFromEnum(NoteColor color)
    {
        switch (color)
        {
            case NoteColor.Yellow: return new Color(1f, 0.95f, 0.6f);
            case NoteColor.Blue: return new Color(0.6f, 0.8f, 1f);
            case NoteColor.Red: return new Color(1f, 0.6f, 0.6f);
            case NoteColor.Green: return new Color(0.7f, 1f, 0.7f);
            default: return Color.white;
        }
    }
}
#endif