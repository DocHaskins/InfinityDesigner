using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace doppelganger
{
    public class DocumentationManager : MonoBehaviour
    {
        public Camera captureCamera;
        private string savePath = "Assets/Resources/Prefabs";

        void Start()
        {
            StartCoroutine(CapturePrefabThumbnails());
        }

        IEnumerator CapturePrefabThumbnails()
        {
            GameObject[] prefabs = Resources.LoadAll<GameObject>("Prefabs"); // Replace "Prefabs" with the path inside Resources
            foreach (GameObject prefab in prefabs)
            {
                GameObject instance = Instantiate(prefab, Vector3.zero, Quaternion.identity);
                yield return new WaitForEndOfFrame(); // Ensure the prefab is rendered

                CaptureThumbnail(instance, prefab.name);
                Destroy(instance); // Cleanup the instantiated prefab
                yield return new WaitForEndOfFrame(); // Wait for the prefab to be destroyed
            }
            Debug.Log("All thumbnails captured.");
        }

        void CaptureThumbnail(GameObject gameObject, string prefabName)
        {
            // Ensure the capture camera is setup correctly
            RenderTexture renderTexture = captureCamera.targetTexture ?? new RenderTexture(256, 256, 24); // Use the camera's targetTexture or create a new one
            captureCamera.targetTexture = renderTexture;
            Texture2D screenShot = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);

            captureCamera.Render();
            RenderTexture.active = renderTexture;
            screenShot.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            screenShot.Apply();

            byte[] bytes = screenShot.EncodeToPNG();
            string filename = $"{savePath}/{prefabName}.png";
            File.WriteAllBytes(filename, bytes);

            Debug.Log($"Saved {filename}");

            // Clean up
            RenderTexture.active = null; // Remove the static reference to the renderTexture
            if (captureCamera.targetTexture != renderTexture)
            {
                Destroy(renderTexture); // Destroy the renderTexture if it was created just for this capture
            }
        }
    }
}