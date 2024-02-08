using UnityEngine;
using UnityEngine.UI;

namespace doppelganger
{
    public class VariationToggleButtonHandler : MonoBehaviour
    {
        public VariationBuilder variationBuilder;
        public CharacterBuilder_InterfaceManager interfaceManager;

        private bool isOn = false;

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
            if (isOn)
            {
                ToggleOff();
            }
            else
            {
                ToggleOn();
            }
            isOn = !isOn;
        }

        void ToggleOn()
        {
            Debug.Log("is on");
            variationBuilder.OpenModelInfoPanel(interfaceManager.currentSlider);
        }

        void ToggleOff()
        {
            Debug.Log("is off");
            if (variationBuilder.currentModelInfoPanel != null)
            {
                Destroy(variationBuilder.currentModelInfoPanel);
                variationBuilder.currentModelInfoPanel = null;
            }
        }
    }
}