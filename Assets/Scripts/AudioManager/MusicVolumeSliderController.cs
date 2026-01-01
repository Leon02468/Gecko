using UnityEngine;
using UnityEngine.UI;

public class MusicVolumeSliderController : MonoBehaviour
{
    [SerializeField] private Slider slider;

    private void Start()
    {
        if (slider == null)
            slider = GetComponentInChildren<Slider>();

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = AudioManager.GlobalMusicVolume;

        slider.onValueChanged.AddListener(OnSliderValueChanged);
    }

    private void OnSliderValueChanged(float value)
    {
        if (GameManager.Instance.AudioInstance != null)
            GameManager.Instance.AudioInstance.SetGlobalMusicVolume(value);
    }
}
