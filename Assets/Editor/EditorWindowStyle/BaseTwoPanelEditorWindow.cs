using UnityEngine;
using UnityEditor;

public abstract class BaseTwoPanelEditorWindow : EditorWindow
{
    protected Vector2 leftScrollPosition;
    protected Vector2 rightScrollPosition;
    protected float splitterPosition = 450f;
    protected bool isResizing = false;
    protected GUIStyle secondaryColorStyle;
    protected GUIStyle buttonStyle;
    protected Color primaryColor = new Color(0.25f, 0.25f, 0.25f);
    protected Color secondaryColor = new Color(0.12f, 0.12f, 0.12f);
    protected Color tertiaryColor = new Color(0.2f, 0.2f, 0.2f);

    protected virtual void OnGUI()
    {
        InitializeStyles();

        EditorGUILayout.BeginHorizontal();

        // Left Panel
        DrawColoredRect(primaryColor, new Rect(0, 0, splitterPosition, position.height));
        EditorGUILayout.BeginVertical(GUILayout.Width(splitterPosition));
        leftScrollPosition = EditorGUILayout.BeginScrollView(leftScrollPosition);
        DrawLeftPanel();
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        // Splitter
        ResizeSplitter();

        // Right Panel
        DrawColoredRect(tertiaryColor, new Rect(splitterPosition + 5, 0, position.width - splitterPosition - 5, position.height));
        EditorGUILayout.BeginVertical();
        rightScrollPosition = EditorGUILayout.BeginScrollView(rightScrollPosition);
        DrawRightPanel();
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    protected abstract void DrawLeftPanel();
    protected abstract void DrawRightPanel();

    private void InitializeStyles()
    {
        if (buttonStyle == null)
        {
            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.normal.background = MakeTex(2, 2, new Color(0.15f, 0.15f, 0.15f, 1.0f));
            buttonStyle.normal.textColor = Color.white;
        }
        if (secondaryColorStyle == null)
        {
            secondaryColorStyle = new GUIStyle(GUI.skin.box);
            secondaryColorStyle.normal.background = MakeTex(2, 2, secondaryColor);
        }
    }


    protected void DrawColoredRect(Color color, Rect rect)
    {
        EditorGUI.DrawRect(rect, color);
    }

    private void ResizeSplitter()
    {
        Rect splitterRect = new Rect(splitterPosition, 0, 5, position.height);
        EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.ResizeHorizontal);

        if (Event.current.type == EventType.MouseDown && splitterRect.Contains(Event.current.mousePosition))
        {
            isResizing = true;
        }

        if (isResizing)
        {
            splitterPosition = Mathf.Clamp(Event.current.mousePosition.x, 100f, position.width - 100f);
            Repaint();
        }

        if (Event.current.type == EventType.MouseUp)
        {
            isResizing = false;
        }

        EditorGUILayout.Space(5);
    }

    protected Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++)
            pix[i] = col;
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }
}