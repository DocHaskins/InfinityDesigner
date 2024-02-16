using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnlimitedScrollUI.Example;

namespace doppelganger
{
    public class TextureScroller : MonoBehaviour
    {
        public Transform contentPanel;
        public GameObject cellPrefab; // Assuming this is your cell prefab
        public bool autoGenerate; // Control the generation process
        private List<Texture2D> allTextures;
        public string searchTerm = "_msk";
        private string additionalFilterTerm = ""; // New field for the additional filter term
        public TMP_InputField filterInputField; // Reference to the TMP Input Field
        private UnlimitedScrollUI.IUnlimitedScroller unlimitedScroller;
        public GameObject currentModel;
        public string currentSlotName;
        public event Action<Texture2D> onTextureSelected;
        public event Action<Texture2D, GameObject, string> TextureSelected;

        private void Start()
        {
            unlimitedScroller = GetComponent<UnlimitedScrollUI.IUnlimitedScroller>();
            LoadTextures(searchTerm);
            if (autoGenerate)
            {
                StartCoroutine(DelayGenerate());
            }
        }

        public void LoadTextures(string filter)
        {
            // Retrieve the additional filter term from the TMP Input Field
            additionalFilterTerm = filterInputField.text.Trim().ToLower();

            // Load and filter textures based on both the search term and the additional filter term
            allTextures = Resources.LoadAll<Texture2D>("Textures").Where(t => t.name.EndsWith(filter)).ToList();
            Debug.Log($"Total Textures: {allTextures.Count}");
            GenerateTextures();
        }

        private IEnumerator DelayGenerate()
        {
            // Wait for the end of the frame to ensure all setups are done
            yield return new WaitForEndOfFrame();
            GenerateTextures();
        }

        private void GenerateTextures()
        {
            //ClearExistingCells();

            unlimitedScroller.Generate(cellPrefab, allTextures.Count, (index, iCell) =>
            {
                var cell = iCell as UnlimitedScrollUI.RegularCell; // Assuming RegularCell is your cell class
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

        public void SetCurrentSlotName(string slotName)
        {
            currentSlotName = slotName;
            Debug.Log($"TextureScroller currentSlotName set to: {currentSlotName}");
        }

        private void SelectTexture(Texture2D texture)
        {
            Debug.Log($"SelectTexture: currentModel is {(currentModel == null ? "null" : currentModel.name)}, slotName is {currentSlotName}");
            TextureSelected?.Invoke(texture, currentModel, currentSlotName);
        }

        // Optionally, add a public method to trigger texture generation manually
        public void TriggerGenerateTextures()
        {
            if (!autoGenerate)
            {
                StartCoroutine(DelayGenerate());
            }
        }

        public void ClearOnTextureSelectedSubscriptions()
        {
            onTextureSelected = null;
        }
        public void RefreshTexturesWithAdditionalFilter()
        {
            LoadTextures(searchTerm);
        }

        public void RefreshTextures(string newSearchTerm)
        {
            // Update the searchTerm
            searchTerm = newSearchTerm;

            // Clear existing textures and UI cells
            ClearExistingCells();

            // Load and display new textures based on the updated searchTerm
            LoadTextures(searchTerm);
        }

        private void ClearExistingCells()
        {
            allTextures.Clear();

            if (unlimitedScroller != null)
            {
                unlimitedScroller.Clear();
            }
            else
            {
                Debug.LogError("unlimitedScroller is null.");
            }
        }
    }
}