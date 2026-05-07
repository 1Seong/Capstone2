using UnityEngine;
using UnityEngine.UI;

public class MuteToggleController : MonoBehaviour
{
    public enum VolumeType { Master, BGM, SFX }

    [SerializeField] private VolumeType volumeType;
    [SerializeField] private VolumeSliderController sliderController;

    // 뮤트 상태에 따라 버튼 아이콘 등을 바꾸고 싶을 때 선택적으로 연결
    [SerializeField] private Image buttonImage;
    [SerializeField] private Sprite mutedSprite;
    [SerializeField] private Sprite unmutedSprite;

    private void Start()
    {
        RefreshVisual();
    }

    // 버튼 OnClick에 연결
    public void OnClick()
    {
        bool current = volumeType switch
        {
            VolumeType.Master => AudioManager.Instance.GetMasterMute(),
            VolumeType.BGM    => AudioManager.Instance.GetBGMMute(),
            VolumeType.SFX    => AudioManager.Instance.GetSFXMute(),
            _                 => false
        };

        bool next = !current;

        switch (volumeType)
        {
            case VolumeType.Master: AudioManager.Instance.SetMasterMute(next); break;
            case VolumeType.BGM:    AudioManager.Instance.SetBGMMute(next);    break;
            case VolumeType.SFX:    AudioManager.Instance.SetSFXMute(next);    break;
        }

        sliderController.RefreshMuteState();
        RefreshVisual();
    }

    private void RefreshVisual()
    {
        if (buttonImage == null || mutedSprite == null || unmutedSprite == null) return;

        bool muted = volumeType switch
        {
            VolumeType.Master => AudioManager.Instance.GetMasterMute(),
            VolumeType.BGM    => AudioManager.Instance.GetBGMMute(),
            VolumeType.SFX    => AudioManager.Instance.GetSFXMute(),
            _                 => false
        };

        buttonImage.sprite = muted ? mutedSprite : unmutedSprite;
    }
}
