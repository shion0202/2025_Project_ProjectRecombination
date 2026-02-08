using TMPro;
using UnityEngine;
using UnityEngine.Audio;

public class UI_Option : MonoBehaviour
{
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private TextMeshProUGUI bgmVolumeText;
    [SerializeField] private TextMeshProUGUI seVolumeText;

    private void Start()
    {
        float bgmVolume = PlayerPrefs.GetFloat("BGMParam", 0.8f);
        float seVolume = PlayerPrefs.GetFloat("SEParam", 0.8f);

        SetBGMVolume(bgmVolume);
        SetSEVolume(seVolume);
    }

    public void SetBGMVolume(float value)
    {
        bgmVolumeText.text = $"{Mathf.RoundToInt(value * 100)}%";

        value = Mathf.Clamp(value, 0.0001f, 1.0f);
        audioMixer.SetFloat("BGMParam", Mathf.Log10(value) * 20);
        PlayerPrefs.SetFloat("BGMParam", value);
    }

    public void SetSEVolume(float value)
    {
        seVolumeText.text = $"{Mathf.RoundToInt(value * 100)}%";

        value = Mathf.Clamp(value, 0.0001f, 1.0f);
        audioMixer.SetFloat("SEParam", Mathf.Log10(value) * 20);
        PlayerPrefs.SetFloat("SEParam", value);
    }
}
