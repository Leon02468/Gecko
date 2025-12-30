using UnityEngine;
using UnityEngine.UI;

public class VolumeSliderController : MonoBehaviour
{
    [SerializeField] private Slider slider;

    private void Start()
    {
        // Initialize slider value from AudioManager
        if (slider == null)
            slider = GetComponentInChildren<Slider>();

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = AudioManager.GlobalSFXVolume;

        slider.onValueChanged.AddListener(OnSliderValueChanged);
    }

    private void OnSliderValueChanged(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetGlobalSFXVolume(value);
    }
}
