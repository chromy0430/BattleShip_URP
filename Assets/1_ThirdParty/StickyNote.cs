using UnityEngine;

public enum NoteColor
{
    Yellow,
    Blue,
    Red,
    Green
}

[AddComponentMenu("Tools/Sticky Note")] // 컴포넌트 메뉴에 추가
public class StickyNote : MonoBehaviour
{
    [HideInInspector] public string noteTitle = "New Note";
    [HideInInspector] public string noteBody = "Write details here...";
    [HideInInspector] public NoteColor noteColor = NoteColor.Yellow;
}