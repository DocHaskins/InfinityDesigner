using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace doppelganger
{
    public class TextureLoader : MonoBehaviour
    {
        public Transform contentPanel;
        public ScrollRect scrollRect;
        public GameObject buttonPrefab;
        private List<Texture2D> allTextures;
        private int totalTextures;
        public string searchTerm = "_dif";

        void Start()
        {
            allTextures = Resources.LoadAll<Texture2D>("Textures").Where(t => t.name.EndsWith(searchTerm)).ToList();
            totalTextures = allTextures.Count;
            Debug.Log($"Total Textures: {totalTextures}");
        }

        void LoadTextures(string filter)
        {
            // Load all textures, then filter them based on the search term
            Texture2D[] allTextures = Resources.LoadAll<Texture2D>("Textures");
            Texture2D[] filteredTextures = allTextures.Where(t => t.name.EndsWith(filter)).ToArray();

            foreach (var texture in filteredTextures)
            {
                GameObject btn = ObjectPool.SharedInstance.GetPooledObject();
                if (btn != null)
                {
                    btn.transform.SetParent(contentPanel, false);
                    btn.SetActive(true);

                    Texture2D thumbnail = GenerateThumbnail(texture);
                    btn.GetComponent<Image>().sprite = Sprite.Create(thumbnail, new Rect(0.0f, 0.0f, thumbnail.width, thumbnail.height), new Vector2(0.5f, 0.5f), 100.0f);
                    btn.GetComponent<Button>().onClick.AddListener(() => SelectTexture(texture));
                    btn.transform.localScale = Vector3.one;
                }
            }
        }


        Texture2D GenerateThumbnail(Texture2D sourceTexture)
        {
            RenderTexture rt = RenderTexture.GetTemporary(48, 48);
            RenderTexture.active = rt;
            Graphics.Blit(sourceTexture, rt);
            Texture2D thumbnail = new Texture2D(48, 48, TextureFormat.RGB24, false);
            thumbnail.ReadPixels(new Rect(0, 0, 48, 48), 0, 0);
            thumbnail.Apply();

            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);

            return thumbnail;
        }

        void AdjustScale(float scale)
        {
            foreach (GameObject btn in ObjectPool.SharedInstance.pooledObjects)
            {
                if (btn.activeInHierarchy)
                {
                    btn.transform.localScale = Vector3.one * scale;
                }
            }
        }

        void SelectTexture(Texture2D texture)
        {
            Debug.Log($"Selected texture: {texture.name}");
            // Apply the texture to your object here
        }
    }
}