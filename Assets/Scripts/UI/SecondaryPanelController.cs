using UnityEngine;
using UnityEngine.UI;

namespace doppelganger
{
    public class SecondaryPanelController : MonoBehaviour
    {
        public Animator animator;
        public bool isPresets = false;
        public bool isSaves = false;
        public bool isVariations = false;

        public Button presetButton;
        public Button saveButton;
        public Button variationButton;

        void Start()
        {
            // Initialize button listeners
            presetButton.onClick.AddListener(SetPreset);
            saveButton.onClick.AddListener(SetSave);
            variationButton.onClick.AddListener(SetVariation);
            SetPreset();
        }

        public void SetPreset()
        {
            UpdateState(ref isPresets, ref isSaves, ref isVariations);
            animator.SetBool("isPresets", isPresets);
        }

        public void SetSave()
        {
            UpdateState(ref isSaves, ref isPresets, ref isVariations);
            animator.SetBool("isSaves", isSaves);
        }

        public void SetVariation()
        {
            UpdateState(ref isVariations, ref isPresets, ref isSaves);
            animator.SetBool("isVariations", isVariations);
        }

        // Helper method to update state bools and ensure only one is true at a time
        private void UpdateState(ref bool toTrue, ref bool toFalse1, ref bool toFalse2)
        {
            // Set the bools for the state
            toTrue = true;
            toFalse1 = false;
            toFalse2 = false;

            // Update the Animator's parameters to reflect the new state
            animator.SetBool("isPresets", isPresets);
            animator.SetBool("isSaves", isSaves);
            animator.SetBool("isVariations", isVariations);
        }
    }
}