using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace doppelganger
{
    public class SliderKeyboardControl : MonoBehaviour
    {
        public CharacterBuilder_InterfaceManager characterBuilder;

        private List<Slider> sliders = new List<Slider>();
        private int currentSliderIndex = 0;

        void Start()
        {
            // Initialize sliders list
            InitializeSliders();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                MoveToPreviousSlider();
                Debug.Log("Up Arrow Pressed");
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                MoveToNextSlider();
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                AdjustCurrentSlider(-1);
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                AdjustCurrentSlider(1);
            }
        }

        public void RefreshSliders()
        {
            StopAllCoroutines();
            StartCoroutine(RefreshSlidersCoroutine());
        }

        private IEnumerator RefreshSlidersCoroutine()
        {
            yield return new WaitForSeconds(2.0f);
            sliders.Clear();
            InitializeSliders();
            HighlightCurrentSlider();
        }

        private void InitializeSliders()
        {
            // Assuming slidersPanel is a parent object containing all slider objects
            foreach (Transform child in characterBuilder.slidersPanel.transform)
            {
                Slider slider = child.GetComponentInChildren<Slider>();
                if (slider != null)
                {
                    Debug.Log($"Added slider inside {characterBuilder.slidersPanel.transform} for: " + child.name);
                    sliders.Add(slider);
                }

            }
        }

        private void MoveToNextSlider()
        {
            Debug.Log("Moving to the next slider.");

            if (sliders.Count == 0)
            {
                Debug.LogWarning("No sliders found. Cannot proceed.");
                return;
            }

            currentSliderIndex++;
            if (currentSliderIndex >= sliders.Count)
            {
                currentSliderIndex = 0; // Loop back to the first slider
                Debug.Log("Reached the end of the sliders. Looping back to the first slider.");
            }

            HighlightCurrentSlider();
        }


        private void MoveToPreviousSlider()
        {
            if (sliders.Count == 0) return;

            currentSliderIndex--;
            if (currentSliderIndex < 0) currentSliderIndex = sliders.Count - 1; // Loop to the last slider

            HighlightCurrentSlider();
        }

        private void AdjustCurrentSlider(int direction)
        {
            if (sliders.Count == 0 || currentSliderIndex < 0 || currentSliderIndex >= sliders.Count)
            {
                Debug.LogWarning("No sliders found or invalid current slider index. Cannot proceed with adjustment.");
                return;
            }

            Debug.Log($"Adjusting current slider ({currentSliderIndex}) by direction: {direction}");

            Slider currentSlider = sliders[currentSliderIndex];
            currentSlider.value += direction; // Adjust the slider value by direction (-1 for left, 1 for right)
            Debug.Log($"Slider '{currentSlider.name}' value adjusted to: {currentSlider.value}");
        }

        private void HighlightCurrentSlider()
        {
            foreach (var slider in sliders)
            {
                // Reset all sliders to a default state
                var background = slider.transform.Find("Background");
                if (background != null) background.GetComponent<Image>().color = Color.white;
            }

            // Highlight the current slider
            var currentBackground = sliders[currentSliderIndex].transform.Find("Background");
            if (currentBackground != null) currentBackground.GetComponent<Image>().color = Color.yellow; // Example color
        }
    }
}