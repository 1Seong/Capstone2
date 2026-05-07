using UnityEngine;
using UnityEngine.UI;

public class VolumeSliderController : MonoBehaviour
{
    public enum VolumeType { Master, BGM, SFX }

    [SerializeField] private VolumeType volumeType;
    [SerializeField] private Slider slider;

    private void Start()
    {
        InitSlider();
        slider.onValueChanged.AddListener(OnSliderValueChanged);
    }

    // AudioManager에서 mute 상태가 바뀔 때 호출 — 뮤트 토글 버튼이 이걸 불러줌
    public void RefreshMuteState()
    {
        if (AudioManager.Instance == null) return;

        bool muted = volumeType switch
        {
            VolumeType.Master => AudioManager.Instance.GetMasterMute(),
            VolumeType.BGM    => AudioManager.Instance.GetBGMMute(),
            VolumeType.SFX    => AudioManager.Instance.GetSFXMute(),
            _                 => false
        };

        slider.interactable = !muted;
    }

    private void InitSlider()
    {
        if (AudioManager.Instance == null) return;

        slider.SetValueWithoutNotify(volumeType switch
        {
            VolumeType.Master => AudioManager.Instance.GetMasterVolume(),
            VolumeType.BGM    => AudioManager.Instance.GetBGMVolume(),
            VolumeType.SFX    => AudioManager.Instance.GetSFXVolume(),
            _                 => 1f
        });

        RefreshMuteState();
    }

    private void OnSliderValueChanged(float value)
    {
        if (AudioManager.Instance == null) return;

        switch (volumeType)
        {
            case VolumeType.Master: AudioManager.Instance.SetMasterVolume(value); break;
            case VolumeType.BGM:    AudioManager.Instance.SetBGMVolume(value);    break;
            case VolumeType.SFX:    AudioManager.Instance.SetSFXVolume(value);    break;
        }
    }

    private void OnDestroy()
    {
        if (slider != null)
            slider.onValueChanged.RemoveListener(OnSliderValueChanged);
    }
}
