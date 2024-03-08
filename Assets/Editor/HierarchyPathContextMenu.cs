using UnityEngine;
using UnityEditor;

public class HierarchyPathContextMenu
{
    [MenuItem("GameObject/Copy Hierarchy Path", false, 0)]
    private static void CopyHierarchyPath()
    {
        if (Selection.activeTransform != null)
        {
            // Build the path
            string path = Selection.activeTransform.name;
            Transform currentParent = Selection.activeTransform.parent;

            while (currentParent != null)
            {
                path = currentParent.name + "/" + path;
                currentParent = currentParent.parent;
            }

            // Copy to clipboard
            EditorGUIUtility.systemCopyBuffer = path;
            Debug.Log("Copied Hierarchy Path: " + path);
        }
    }

    [MenuItem("GameObject/Copy Hierarchy Path", true)]
    private static bool CopyHierarchyPathValidation()
    {
        return Selection.activeTransform != null;
    }
}