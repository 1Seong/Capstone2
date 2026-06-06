using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("BGM Clips")]
    [SerializeField] private AudioClip[] bgmClips;

    [Header("SFX Clips")]
    [SerializeField] private AudioClip[] sfxClips;

    private const string MasterParam = "Master";
    private const string BGMParam    = "BGM";
    private const string SFXParam    = "SFX";

    private const string MasterVolumeKey = "Volume_Master";
    private const string BGMVolumeKey    = "Volume_BGM";
    private const string SFXVolumeKey    = "Volume_SFX";

    private const string MasterMuteKey = "Mute_Master";
    private const string BGMMuteKey    = "Mute_BGM";
    private const string SFXMuteKey    = "Mute_SFX";
    
    private Tween bgmFadeTween;
    private float defaultBgmVolume = 1f;
    
    private const float NormalCutoff = 22000f;
    private const float MuffledCutoff = 600f;
    
    private Tween masterLowpassTween;
    private const string MasterLowpassParam = "MasterLowpassCutoff";

    public enum BGMType
    {
        EditorEdit, EditorHub, Polaris, SingleHub, Title, Tutorial,
        UserMapHub, UserMapPlay, Zodiac
    }

    public enum SFXType
    {
        CamRotate, Click, Dash, Esc, GhostEnd, GhostGet, Inverter, LaserGet,
        LaserShoot, MapClear, MapRemove, Move, MoveBlocked, PopUp, Portal,
        Rotation, SceneSwitch, TileDelete, TileLoad, TileWave1, TileWave2, Undo
    }

    private Dictionary<string, BGMType> _sceneBGM =  new Dictionary<string, BGMType>()
    {
        {"SampleScene", BGMType.Title},
        {"SingleHub",  BGMType.SingleHub},
        {"SinglePuzzlePlayScene",  BGMType.Polaris},
        {"EditorMenu",  BGMType.EditorHub},
        {"PuzzleEdit",  BGMType.EditorEdit},
        {"UsermapMenu",  BGMType.UserMapHub},
        {"PuzzlePlayScene",  BGMType.UserMapPlay},
    };

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadAll();
    }

    private void LoadAll()
    {
        // 볼륨 먼저 적용 후 mute — mute가 볼륨을 덮어써야 하므로 순서 중요
        ApplyVolume(MasterParam, PlayerPrefs.GetFloat(MasterVolumeKey, 1f));
        ApplyVolume(BGMParam,    PlayerPrefs.GetFloat(BGMVolumeKey,    1f));
        ApplyVolume(SFXParam,    PlayerPrefs.GetFloat(SFXVolumeKey,    1f));

        ApplyMute(MasterParam, IsMuted(MasterMuteKey));
        ApplyMute(BGMParam,    IsMuted(BGMMuteKey));
        ApplyMute(SFXParam,    IsMuted(SFXMuteKey));
    }

    // ── 내부 헬퍼 ───────────────────────────────────────

    private float LinearToDecibel(float linear)
        => linear > 0.0001f ? Mathf.Log10(linear) * 20f : -80f;

    private void ApplyVolume(string param, float linear)
        => audioMixer.SetFloat(param, LinearToDecibel(linear));

    private void ApplyMute(string param, bool mute)
        => audioMixer.SetFloat(param, mute ? -80f : LinearToDecibel(GetRawVolume(ParamToVolumeKey(param))));

    private bool IsMuted(string muteKey)
        => PlayerPrefs.GetInt(muteKey, 0) == 1;

    private string ParamToVolumeKey(string param) => param switch
    {
        MasterParam => MasterVolumeKey,
        BGMParam    => BGMVolumeKey,
        SFXParam    => SFXVolumeKey,
        _           => MasterVolumeKey
    };

    private float GetRawVolume(string volumeKey)
        => PlayerPrefs.GetFloat(volumeKey, 1f);

    // ── 볼륨 Public API ──────────────────────────────────

    public void SetMasterVolume(float value)
    {
        PlayerPrefs.SetFloat(MasterVolumeKey, value);
        if (!IsMuted(MasterMuteKey)) ApplyVolume(MasterParam, value);
    }

    public void SetBGMVolume(float value)
    {
        PlayerPrefs.SetFloat(BGMVolumeKey, value);
        if (!IsMuted(BGMMuteKey)) ApplyVolume(BGMParam, value);
    }

    public void SetSFXVolume(float value)
    {
        PlayerPrefs.SetFloat(SFXVolumeKey, value);
        if (!IsMuted(SFXMuteKey)) ApplyVolume(SFXParam, value);
    }

    public float GetMasterVolume() => GetRawVolume(MasterVolumeKey);
    public float GetBGMVolume()    => GetRawVolume(BGMVolumeKey);
    public float GetSFXVolume()    => GetRawVolume(SFXVolumeKey);

    // ── Mute Public API ──────────────────────────────────

    public void SetMasterMute(bool mute)
    {
        PlayerPrefs.SetInt(MasterMuteKey, mute ? 1 : 0);
        ApplyMute(MasterParam, mute);
    }

    public void SetBGMMute(bool mute)
    {
        PlayerPrefs.SetInt(BGMMuteKey, mute ? 1 : 0);
        ApplyMute(BGMParam, mute);
    }

    public void SetSFXMute(bool mute)
    {
        PlayerPrefs.SetInt(SFXMuteKey, mute ? 1 : 0);
        ApplyMute(SFXParam, mute);
    }

    public bool GetMasterMute() => IsMuted(MasterMuteKey);
    public bool GetBGMMute()    => IsMuted(BGMMuteKey);
    public bool GetSFXMute()    => IsMuted(SFXMuteKey);

    // ── BGM / SFX 재생 ───────────────────────────────────

    public void PlayBGM(string sceneName, int singleMapType = 0)
    {
        int index = 0;
        if (sceneName == "SinglePuzzlePlayScene" && singleMapType != 0)
        {
            index = singleMapType switch
            {
                1 => (int)BGMType.Tutorial,
                2 => (int)BGMType.Zodiac,
                _ => index
            };
        }
        else
            index = (int)_sceneBGM[sceneName];
        if (index < 0 || index >= bgmClips.Length || bgmClips[index] == null)
        {
            Debug.LogWarning($"[AudioManager] BGM 클립 없음: {_sceneBGM[sceneName]}");
            return;
        }
        if (bgmSource.clip == bgmClips[index] && bgmSource.isPlaying) return;
        bgmSource.clip = bgmClips[index];
        bgmSource.loop = true;
        bgmSource.Play();
    }
    
    public void PlayBGM(BGMType bgm)
    {
        int index = (int)bgm;
        if (index < 0 || index >= bgmClips.Length || bgmClips[index] == null)
        {
            Debug.LogWarning($"[AudioManager] BGM 클립 없음: {bgm}");
            return;
        }
        if (bgmSource.clip == bgmClips[index] && bgmSource.isPlaying) return;
        bgmSource.clip = bgmClips[index];
        bgmSource.loop = true;
        bgmSource.Play();
    }

    public void StopBGM(float fadeDuration = 0.5f)
    {
        bgmFadeTween?.Kill();
        
        float originalVolume = bgmSource.volume;

        bgmFadeTween = bgmSource
            .DOFade(0f, fadeDuration)
            .OnComplete(() =>
            {
                bgmSource.Stop();
                bgmSource.volume = originalVolume;
            });
    }

    public void PlaySFX(SFXType type)
    {
        int index = (int)type;
        if (index < 0 || index >= sfxClips.Length || sfxClips[index] == null)
        {
            Debug.LogWarning($"[AudioManager] SFX 클립 없음: {type}");
            return;
        }
        sfxSource.PlayOneShot(sfxClips[index]);
        if(type == SFXType.GhostGet)
            SetMasterMuffled(true);
        else if(type == SFXType.GhostEnd)
            SetMasterMuffled(false);
    }
    
    public void SetMasterMuffled(bool muffled, float duration = 0.5f)
    {
        masterLowpassTween?.Kill();

        float targetCutoff = muffled ? MuffledCutoff : NormalCutoff;

        audioMixer.GetFloat(MasterLowpassParam, out float currentCutoff);

        masterLowpassTween = DOTween.To(
            () => currentCutoff,
            value =>
            {
                currentCutoff = value;
                audioMixer.SetFloat(MasterLowpassParam, value);
            },
            targetCutoff,
            duration
        );
    }
}
