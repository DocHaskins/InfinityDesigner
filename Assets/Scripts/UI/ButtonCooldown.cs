using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ButtonCooldown : MonoBehaviour
{
    public Button buttonToDisable;
    public float disableTime = 5f;
    public GameObject countdownPrefab;

    private GameObject countdownInstance;

    public void DisableButton()
    {
        if (buttonToDisable.interactable)
        {
            StartCoroutine(DisableButtonCoroutine());
        }
    }

    private IEnumerator DisableButtonCoroutine()
    {
        buttonToDisable.interactable = false;

        countdownInstance = Instantiate(countdownPrefab, buttonToDisable.transform.position, Quaternion.identity, buttonToDisable.transform);
        countdownInstance.transform.localPosition = Vector3.zero;
        countdownInstance.transform.localRotation = Quaternion.identity;
        countdownInstance.transform.localScale = Vector3.one;

        TMP_Text countdownText = countdownInstance.transform.Find("Timer")?.GetComponent<TMP_Text>();

        if (countdownText == null)
        {
            Debug.LogError("The 'Timer' child or TMP_Text component is missing in the countdown prefab.");
            yield break; // Stop the coroutine if there is no 'Timer' text.
        }

        // Start the countdown.
        for (float time = disableTime; time >= 0; time -= Time.deltaTime)
        {
            countdownText.text = time.ToString("F2"); // Update the timer text.
            yield return null; // Wait for the next frame.
        }

        buttonToDisable.interactable = true;
        Destroy(countdownInstance); // Remove the countdown display once finished.
    }
}