using System;
using Newtonsoft.Json;
using UnityEngine;

public class StarController : MonoBehaviour
{
    public LevelData levelData;

    [SerializeField] private GameObject clearedStar;
    [SerializeField] private GameObject unlockedStar;
    [SerializeField] private GameObject lockedStar;
    [SerializeField] private Collider2D coll;
    private int _moves;
    
    [SerializeField] private StarController[] unlockTargets;
    [SerializeField] private LineRenderer[] edges;
    [SerializeField] private Color edgeColor;
    [SerializeField] private LineRenderer[] edges1;
    [SerializeField] private Color edgeColor1;

    [SerializeField] private GameObject cam;

    [SerializeField] private StarController[] otherClears;

    private bool _isCleared;
    public bool IsCleared
    {
        get =>  _isCleared;
        set
        {
            _isCleared = value;
            if (value)
            {
                clearedStar.SetActive(true);
                unlockedStar.SetActive(false);
                lockedStar.SetActive(false);
                _isUnlocked = true;
                coll.enabled = true;
            }
        }
    }
    private bool _isUnlocked;
    public bool IsUnlocked
    {
        get =>  _isUnlocked;
        set
        {
            _isUnlocked = value;
            if (value)
            {
                clearedStar.SetActive(false);
                unlockedStar.SetActive(true);
                lockedStar.SetActive(false);
                coll.enabled = true;
            }
        }
    }

    void Awake()
    {
        _moves = SaveManager.Instance.LoadClear(levelData.mapId.ToString());
        if (_moves < 0)
        {
            if (levelData.mapId == 0)
            {
                IsUnlocked = true;
                return;
            }
            clearedStar.SetActive(false);
            unlockedStar.SetActive(false);
            lockedStar.SetActive(true);
            coll.enabled = false;
            return;
        }
        IsCleared = true;
    }

    private bool CheckUnlock()
    {
        if (!IsCleared) return false;
        foreach (var i in otherClears)
        {
            if (!i.IsCleared) return false;
        }

        return true;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (!CheckUnlock()) return;
        
        foreach (var i in unlockTargets)
        {
            if (i.IsCleared) continue;

            i.IsUnlocked = true;
        }
        
        foreach (var i in edges)
        {
            i.startColor = edgeColor;
            i.endColor = edgeColor;
        }

        foreach (var i in edges1)
        {
            i.startColor = edgeColor1;
            i.endColor = edgeColor1;
        }
    }
    
    private void OnMouseEnter()
    {
        StageTooltipUI.Instance.Show(levelData, Input.mousePosition);
    }

    private async void OnMouseDown()
    {
        GameManager.Instance.SingleEnterEvent += () => { cam.SetActive(false); };
        var p = NormalizeJsonString(levelData.portalPairs);
        var r = NormalizeJsonString(levelData.rotationInfo);
        AudioManager.Instance.PlaySFX(AudioManager.SFXType.Click);
        await GameManager.Instance.EnterGameSingle(levelData.mapId, levelData.data, p, r);
    }
    
    private string NormalizeJsonString(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return raw;

        raw = raw.Trim();

        if (raw.Length >= 2 && raw[0] == '"' && raw[^1] == '"')
        {
            try
            {
                return JsonConvert.DeserializeObject<string>(raw);
            }
            catch
            {
                // 실패하면 원본 그대로 사용
                return raw;
            }
        }

        return raw;
    }

    private void OnMouseExit()
    {
        StageTooltipUI.Instance.Hide();
    }
}
