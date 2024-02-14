using UnityEngine;
using UnityEditor;
using System.IO;

namespace doppelganger
{
    public class PrefabThumbnailGenerator : EditorWindow
    {
        [MenuItem("Tools/Prefab Thumbnail Generator")]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(PrefabThumbnailGenerator));
        }

        void OnGUI()
        {
            if (GUILayout.Button("Generate Prefab Thumbnails"))
            {
                GeneratePrefabThumbnails();
            }
        }

        private static void GeneratePrefabThumbnails()
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/PathToYourPrefabs" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    string thumbnailPath = Path.ChangeExtension(path, ".png");
                    GenerateThumbnail(prefab, thumbnailPath);
                }
            }
        }

        private static void GenerateThumbnail(GameObject prefab, string filePath)
        {
            // Setup a temporary scene and camera
            GameObject tempPrefab = PrefabUtility.InstantiatePrefab(prefab) as GameObject;

            // Create a new camera for capturing the thumbnail
            GameObject cameraGameObject = new GameObject("TemporaryCamera");
            Camera camera = cameraGameObject.AddComponent<Camera>();
            camera.backgroundColor = Color.clear; // Set background color to clear or any preferred color
            camera.clearFlags = CameraClearFlags.SolidColor;

            // Position the camera (this is a simple setup, adjust as needed)
            camera.transform.position = tempPrefab.transform.position + new Vector3(0, 0, -10);
            camera.transform.LookAt(tempPrefab.transform);

            // Setup and capture the image
            RenderTexture rt = new RenderTexture(256, 256, 24);
            camera.targetTexture = rt;
            Texture2D screenShot = new Texture2D(256, 256, TextureFormat.RGB24, false);
            camera.Render();
            RenderTexture.active = rt;
            screenShot.ReadPixels(new Rect(0, 0, 256, 256), 0, 0);
            camera.targetTexture = null;
            RenderTexture.active = null;
            DestroyImmediate(rt);
            byte[] bytes = screenShot.EncodeToPNG();
            File.WriteAllBytes(filePath, bytes);

            // Clean up
            DestroyImmediate(tempPrefab);
            DestroyImmediate(cameraGameObject); // Destroy the camera after capturing the thumbnail
        }
    }
}