#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace doppelganger
{
    [CustomEditor(typeof(ChildSkeleton))]
    public class ChildSkeletonEditor : Editor
    {
        private bool showBindings = true;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            ChildSkeleton script = (ChildSkeleton)target;

            // Toggle for showing bindings in the scene view
            showBindings = EditorGUILayout.Toggle("Show Bindings", showBindings);

            if (showBindings)
            {
                SceneView.RepaintAll();
            }
        }

        void OnSceneGUI()
        {
            if (showBindings)
            {
                DrawBindings();
            }
        }

        private void DrawBindings()
        {
            ChildSkeleton script = (ChildSkeleton)target;
            if (script.parentSkeleton == null)
            {
                return;
            }

            foreach (var boneData in script.parentSkeleton.GetBoneData())
            {
                Transform parentBone = boneData.boneTransform;
                Transform childBone = script.transform.FindDeepChild(parentBone.name);
                if (childBone != null)
                {
                    Handles.color = Color.green;
                    Handles.DrawLine(parentBone.position, childBone.position);
                }
            }
        }
    }
}
#endif