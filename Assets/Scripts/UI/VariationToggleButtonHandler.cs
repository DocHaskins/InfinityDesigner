using UnityEngine;
using UnityEngine.UI;

namespace doppelganger
{
    public class VariationToggleButtonHandler : MonoBehaviour
    {
        public VariationBuilder variationBuilder;
        public CharacterBuilder_InterfaceManager interfaceManager;

        void Start()
        {
            Button button = GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(Toggle);
            }
            else
            {
                Debug.LogError("VariationToggleButtonHandler script is not attached to a GameObject with a Button component.");
            }
        }

        void Toggle()
        {
            variationBuilder.ToggleModelInfoPanel(interfaceManager.currentSlider);
        }
    }
}