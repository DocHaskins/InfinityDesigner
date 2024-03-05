using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections;
using UnityEngine.Networking;

namespace doppelganger
{
    public class BundleVersionChecker : MonoBehaviour
    {
        public string versionFileUrl = "https://raw.githubusercontent.com/yourUsername/yourRepo/main/scenes_version.txt";
        public string bundleUrl = "https://github.com/yourUsername/yourRepo/raw/main/scenes_assets_all_b4d01f51d4448c76c1e00416ba83b8f1.bundle";
        private string localVersionPath;
        private string localBundlePath;

        void Start()
        {
            localVersionPath = Application.persistentDataPath + "/scenes_version.txt";
            localBundlePath = Application.persistentDataPath + "/scenes_assets_all.bundle";
            StartCoroutine(CheckAndUpdateBundle());
        }

        IEnumerator CheckAndUpdateBundle()
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                Debug.Log("No internet connection available.");
                yield break;
            }

            string localVersion = "0"; // Default if not found
            if (System.IO.File.Exists(localVersionPath))
            {
                localVersion = System.IO.File.ReadAllText(localVersionPath);
            }

            // Get the version from GitHub
            UnityWebRequest versionRequest = UnityWebRequest.Get(versionFileUrl);
            yield return versionRequest.SendWebRequest();
            if (versionRequest.result == UnityWebRequest.Result.ConnectionError || versionRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Error getting version file: " + versionRequest.error);
                yield break;
            }
            string githubVersion = versionRequest.downloadHandler.text;

            // Compare versions and update if GitHub has a newer version
            if (githubVersion != localVersion)
            {
                Debug.Log("Newer bundle version available, updating...");
                UnityWebRequest bundleRequest = UnityWebRequest.Get(bundleUrl);
                yield return bundleRequest.SendWebRequest();
                if (bundleRequest.result == UnityWebRequest.Result.ConnectionError || bundleRequest.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError("Error downloading new bundle: " + bundleRequest.error);
                    yield break;
                }

                // Save the updated bundle and version locally
                System.IO.File.WriteAllBytes(localBundlePath, bundleRequest.downloadHandler.data);
                System.IO.File.WriteAllText(localVersionPath, githubVersion);
                Debug.Log("Bundle updated successfully!");

                // Reload Addressables
                yield return ReloadAddressables();
            }
            else
            {
                Debug.Log("Bundle is up-to-date.");
            }
        }

        IEnumerator ReloadAddressables()
        {
            Debug.Log("Reloading Addressables...");
            Addressables.ClearResourceLocators();
            var handle = Addressables.InitializeAsync();
            yield return handle;
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                Debug.Log("Addressables reloaded successfully.");
                // Optionally, you can load something here to verify
            }
            else
            {
                Debug.LogError("Failed to reload Addressables.");
            }
        }
    }
}