using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIToggleAnimation : MonoBehaviour
{
    public Animator animationController;
    public string booleanName = "isActive";
    private bool isActive = false;
    private bool isUIVisible = false;

    [Header("Interface")]
    public AudioSource audioSource;

    [Header("Audio")]
    public AudioClip onClip;
    public AudioClip offClip;

    public void ToggleAnimatorBoolean()
    {
        if (animationController != null)
        {
            isActive = !isActive;
            animationController.SetBool(booleanName, isActive);
            PlaySound(isUIVisible);
        }
        else
        {
            Debug.LogWarning("Animator reference is not set!");
        }
    }

    void PlaySound(bool isVisible)
    {
        audioSource.clip = isVisible ? onClip : offClip;
        audioSource.Play();
    }
}
