using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class LevelListController : MonoBehaviour
{
    [SerializeField] private Transform levelButtonParent;
    [SerializeField] private GameObject levelButtonPrefab;
    [SerializeField] private GameObject rightPageObject;
    [SerializeField] private RawImage rightPageRawImage;
    [SerializeField] private TMP_InputField rightPageName;
    [SerializeField] private TMP_InputField rightPageDescription;
    private Tuple<MapCreating, Texture, Button> _selectedMapCreating;
    // 토글 버튼과 연결

    public Tuple<MapCreating, Texture, Button> SelectedMapCreating
    {
        set
        {
            _selectedMapCreating = value;
            UpdateRightPage();
        }
    }

    private async void Start()
    {
        List<MapCreating> maps;
        try
        {
            maps = await DBManager.Instance.FetchMapCreatingAsync();
        }
        catch (Exception e)
        {
            Debug.LogWarning(e.Message);
            PopUpManager.Instance.Show("맵을 가져올 수 없습니다.");
            return;
        }
        
        for (int i = 0; i != maps.Count; ++i)
        {
            var o = Instantiate(levelButtonPrefab, levelButtonParent);
            var m = new Tuple<MapCreating, Texture, Button>(maps[i], null, o.GetComponent<Button>());
            if (!string.IsNullOrEmpty(maps[i].ThumbnailUrl))
            {
                var ri = o.GetComponent<RawImage>();
                await LoadThumbnailAsync(ri, maps[i].ThumbnailUrl);
                m = new Tuple<MapCreating, Texture, Button>(maps[i], ri.texture, o.GetComponent<Button>());
            }
            m.Item3.onClick.AddListener(() => SelectedMapCreating = m);
            o.GetComponentInChildren<TextMeshProUGUI>().text = maps[i].Name;
        }
    }

    private void UpdateRightPage()
    {
        var m = _selectedMapCreating.Item1;
        rightPageRawImage.texture = _selectedMapCreating.Item2;
        rightPageName.text = m.Name;
        rightPageName.placeholder.GetComponent<TextMeshProUGUI>().text = m.Name;
        rightPageDescription.text = m.Desc;
        rightPageDescription.placeholder.GetComponent<TextMeshProUGUI>().text = m.Desc;
        rightPageObject.SetActive(true);
    }
    
    private async UniTask LoadThumbnailAsync(RawImage rawImage, string url)
    {
        using var request = UnityWebRequestTexture.GetTexture(url);
        await request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"썸네일 로드 실패: {request.error}");
            return;
        }
        
        if (rawImage.texture != null)
            Destroy(rawImage.texture);

        rawImage.texture = DownloadHandlerTexture.GetContent(request);
    }
    
    // 온라인 체크 + 예외처리 필요
    private async void CreateMap()
    {
        if (!GameManager.Instance.CheckNetwork())
            return;
        
        try
        {
            await DBManager.Instance.InsertMapCreatingAsync(new MapCreating());
        }
        catch (Exception e)
        {
            Debug.LogWarning(e.Message);
            PopUpManager.Instance.Show("나중에 다시 시도해주세요.");
            return;
        }

        MapCreating recent;
        try
        {
            recent = await DBManager.Instance.FetchRecentMapCreatingSingleAsync();
        }
        catch (Exception e)
        {
            Debug.LogWarning(e.Message);
            PopUpManager.Instance.Show("정보 가져오기 실패");
            return;
        }
        var o = Instantiate(levelButtonPrefab, levelButtonParent);
        var newMap = new Tuple<MapCreating, Texture, Button>(recent, null, o.GetComponent<Button>());
        newMap.Item3.onClick.AddListener(() => SelectedMapCreating = newMap);
    }
    
    // 온라인 체크 + 예외처리 필요
    private async void DeleteMap()
    {
        if (!GameManager.Instance.CheckNetwork())
            return;
        try
        {
            await DBManager.Instance.DeleteMapCreatingAsync(_selectedMapCreating.Item1.MapId);
        }
        catch (Exception e)
        {
            Debug.LogWarning(e.Message);
            PopUpManager.Instance.Show("나중에 다시 시도해주세요.");
            return;
        }
        
        Destroy(_selectedMapCreating.Item2);
        Destroy(_selectedMapCreating.Item3);
        _selectedMapCreating = null;
        
        rightPageObject.SetActive(false);
    }
    
    private void EditMap()
    {
        GameManager.Instance.EnterEditor(_selectedMapCreating.Item1);
    }
    
    // 온라인 체크 + 예외처리 필요
    private async void UpdateMap()
    {
        if (!GameManager.Instance.CheckNetwork())
            return;

        var originalName = _selectedMapCreating.Item1.Name;
        var originalDesc = _selectedMapCreating.Item1.Desc;
        var newName = rightPageName.text;
        var newDesc = rightPageDescription.text;
        _selectedMapCreating.Item1.Name = newName;
        _selectedMapCreating.Item1.Desc = newDesc;
        
        try
        {
            await DBManager.Instance.UpdateMapCreatingAsync(_selectedMapCreating.Item1);
        }
        catch (Exception e)
        {
            Debug.LogWarning(e.Message);
            PopUpManager.Instance.Show("나중에 다시 시도해주세요.");
            _selectedMapCreating.Item1.Name = originalName;
            _selectedMapCreating.Item1.Desc = originalDesc;
            return;
        }
        
        rightPageName.text = newName;
        rightPageName.placeholder.GetComponent<TextMeshProUGUI>().text = newName;
        rightPageDescription.text = newDesc;
        rightPageDescription.placeholder.GetComponent<TextMeshProUGUI>().text = newDesc;
        var b = _selectedMapCreating.Item3;
        b.GetComponent<TextMeshProUGUI>().text = newName;
    }
}
