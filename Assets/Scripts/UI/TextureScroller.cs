using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace UnlimitedScrollUI.Example
{
    public class TextureScroller : MonoBehaviour
    {
        public Transform contentPanel;
        public GameObject cellPrefab; // Assuming this is your cell prefab
        public bool autoGenerate; // Control the generation process
        private List<Texture2D> allTextures;
        public string searchTerm = "_dif";
        private IUnlimitedScroller unlimitedScroller;

        private void Start()
        {
            unlimitedScroller = GetComponent<IUnlimitedScroller>();
            //LoadTextures(searchTerm);
            if (autoGenerate)
            {
                StartCoroutine(DelayGenerate());
            }
        }

        private void LoadTextures(string filter)
        {
            // Load and filter textures based on the search term
            allTextures = Resources.LoadAll<Texture2D>("Textures").Where(t => t.name.EndsWith(filter)).ToList();
            Debug.Log($"Total Textures: {allTextures.Count}");
        }

        private IEnumerator DelayGenerate()
        {
            // Wait for the end of the frame to ensure all setups are done
            yield return new WaitForEndOfFrame();
            GenerateTextures();
        }

        private void GenerateTextures()
        {
            unlimitedScroller.Generate(cellPrefab, allTextures.Count, (index, iCell) =>
            {
                var cell = iCell as RegularCell; // Assuming RegularCell is your cell class
                if (cell != null)
                {
                    // Generate and assign thumbnail to the cell
                    Texture2D texture = allTextures[index];
                    Texture2D thumbnail = GenerateThumbnail(texture);
                    cell.GetComponent<Image>().sprite = Sprite.Create(thumbnail, new Rect(0.0f, 0.0f, thumbnail.width, thumbnail.height), new Vector2(0.5f, 0.5f), 100.0f);
                    cell.GetComponent<Button>().onClick.AddListener(() => SelectTexture(texture));
                    cell.transform.localScale = Vector3.one;
                    cell.onGenerated?.Invoke(index);
                }
            });
        }

        private Texture2D GenerateThumbnail(Texture2D sourceTexture)
        {
            // Generate a thumbnail from the source texture
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

        private void SelectTexture(Texture2D texture)
        {
            // Log the selected texture (could be extended to apply the texture)
            Debug.Log($"Selected texture: {texture.name}");
        }

        // Optionally, add a public method to trigger texture generation manually
        public void TriggerGenerateTextures()
        {
            if (!autoGenerate)
            {
                StartCoroutine(DelayGenerate());
            }
        }
    }
}