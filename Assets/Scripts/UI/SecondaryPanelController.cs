using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace doppelganger
{
    public class SecondaryPanelController : MonoBehaviour
    {
        public PresetScroller presetScroller;
        public CharacterBuilder_InterfaceManager interfaceManager;
        public VariationBuilder variationBuilder;

        public TMP_Text currentPresetLabel;
        
        public Animator animator;
        public bool isPresets = false;
        public bool isSaves = false;
        public bool isVariations = false;
        public bool isShare = false;

        public Button presetButton;
        public Button saveButton;
        public Button variationButton;
        public Button shareButton;

        void Start()
        {
            // Initialize button listeners
            presetButton.onClick.AddListener(SetPreset);
            saveButton.onClick.AddListener(SetSave);
            variationButton.onClick.AddListener(SetVariation);
            shareButton.onClick.AddListener(SetShare);
        }

        public void SetPreset()
        {
            currentPresetLabel.gameObject.SetActive(true);
            UpdateState(ref isPresets, ref isSaves, ref isVariations, ref isShare);
            animator.SetBool("isPresets", isPresets);
            presetScroller.LoadPresets();
        }

        public void SetSave()
        {
            currentPresetLabel.gameObject.SetActive(true);
            UpdateState(ref isSaves, ref isPresets, ref isVariations, ref isShare);
            animator.SetBool("isSaves", isSaves);
        }

        public void SetVariation()
        {
            currentPresetLabel.gameObject.SetActive(false);
            UpdateState(ref isVariations, ref isPresets, ref isSaves, ref isShare);
            animator.SetBool("isVariations", isVariations);
            variationBuilder.UpdateModelInfoPanel(interfaceManager.currentSlider);
        }

        public void SetShare()
        {
            currentPresetLabel.gameObject.SetActive(false);
            UpdateState(ref isShare, ref isPresets, ref isSaves, ref isVariations);
            animator.SetBool("isShare", isShare);
        }

        private void UpdateState(ref bool toTrue, ref bool toFalse1, ref bool toFalse2, ref bool toFalse3)
        {
            toTrue = true;
            toFalse1 = false;
            toFalse2 = false;
            toFalse3 = false;

            animator.SetBool("isPresets", isPresets);
            animator.SetBool("isSaves", isSaves);
            animator.SetBool("isVariations", isVariations);
            animator.SetBool("isShare", isShare);
        }
    }
}