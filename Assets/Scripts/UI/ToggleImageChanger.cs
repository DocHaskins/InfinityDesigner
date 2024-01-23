using UnityEngine;
using UnityEngine.UI;

public class ToggleImageChanger : MonoBehaviour
{
    public Toggle toggle;
    public Image image;

    public Sprite imageWhenTrue;
    public Sprite imageWhenFalse;

    void Start()
    {
        toggle.onValueChanged.AddListener(OnToggleValueChanged);
        UpdateImage(toggle.isOn);
    }

    private void OnToggleValueChanged(bool isOn)
    {
        UpdateImage(isOn);
    }

    private void UpdateImage(bool isOn)
    {
        image.sprite = isOn ? imageWhenTrue : imageWhenFalse;
    }
}