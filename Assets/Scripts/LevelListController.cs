using System;
using System.Collections.Generic;
using com.example;
using Cysharp.Threading.Tasks;
using Postgrest.Exceptions;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class LevelListController : MonoBehaviour
{
    [SerializeField] private GameObject levelButtonPrefab;
    [SerializeField] private Texture _defaultThumbnail;
    
    [Header("Tab1")]
    [SerializeField] private Transform levelButtonParent;
    [SerializeField] private GameObject rightPageObject;
    [SerializeField] private RawImage rightPageRawImage;
    [SerializeField] private TMP_InputField rightPageName;
    [SerializeField] private TMP_InputField rightPageDescription;
    private Tuple<MapCreating, Texture, Button> _selectedMapCreating;

    [Header("Tab2")] 
    [SerializeField] private GameObject cam;
    [SerializeField] private GameObject canvas;
    [SerializeField] private Transform mapLevelButtonParent;
    [SerializeField] private GameObject mapRightPageObject;
    [SerializeField] private RawImage mapRightPageRawImage;
    [SerializeField] private TMP_InputField mapRightPageName;
    [SerializeField] private TMP_InputField mapRightPageDescription;
    [SerializeField] private TextMeshProUGUI mapRightPageLikesTMP;
    [SerializeField] private TextMeshProUGUI mapRightPageIdTMP;
    [SerializeField] private TextMeshProUGUI mapRightPageBestMovesTMP;
    private Tuple<Map, Texture, Button> _selectedMap;

    public Tuple<MapCreating, Texture, Button> SelectedMapCreating
    {
        set
        {
            _selectedMapCreating = value;
            UpdateRightPage();
        }
    }
    public Tuple<Map, Texture, Button> SelectedMap
    {
        set
        {
            _selectedMap = value;
            MapUpdateRightPage();
        }
    }

    private async void Start()
    {
        List<MapCreating> maps = new();
        List<Map> uploadedMaps;
        try
        {
            maps = await DBManager.Instance.FetchMapCreatingAsync();
        }
        catch (Exception e)
        {
            Debug.LogWarning(e.Message);
            PopUpManager.Instance.Show("제작 중인 맵을 가져올 수 없습니다.");
        }
        
        // 1. 버튼 오브젝트 먼저 다 생성
        var entries = new List<Tuple<MapCreating, RawImage, Button>>();
        for (int i = 0; i < maps.Count; i++)
        {
            var o = Instantiate(levelButtonPrefab, levelButtonParent);
            o.GetComponentInChildren<TextMeshProUGUI>().text = maps[i].Name;
            entries.Add(new Tuple<MapCreating, RawImage, Button>(
                maps[i],
                o.GetComponent<RawImage>(),
                o.GetComponent<Button>()
            ));
        }

        // 2. 썸네일 로딩 전부 동시에
        await UniTask.WhenAll(entries.Select(e =>
            string.IsNullOrEmpty(e.Item1.ThumbnailUrl)
                ? UniTask.CompletedTask
                : LoadThumbnailAsync(e.Item2, e.Item1.ThumbnailUrl)
        ));

        // 3. 버튼 이벤트 등록
        foreach (var e in entries)
        {
            var map = e.Item1;
            var tex = e.Item2.texture;
            var btn = e.Item3;
            var m = new Tuple<MapCreating, Texture, Button>(map, tex, btn);
            btn.onClick.AddListener(() => SelectedMapCreating = m);
        }
        
        try
        {
            uploadedMaps = await DBManager.Instance.FetchMyMapAsync();
        }
        catch (Exception e)
        {
            Debug.LogWarning(e.Message);
            PopUpManager.Instance.Show("업로드한 맵을 가져올 수 없습니다.");
            await SceneChange.Instance.ManualEndFade();
            return;
        }
        // 1. 버튼 오브젝트 먼저 다 생성
        var mapEntries = new List<Tuple<Map, RawImage, Button>>();
        for (int i = 0; i < uploadedMaps.Count; i++)
        {
            var o = Instantiate(levelButtonPrefab, mapLevelButtonParent);
            o.GetComponentInChildren<TextMeshProUGUI>().text = uploadedMaps[i].Name;
            mapEntries.Add(new Tuple<Map, RawImage, Button>(
                uploadedMaps[i],
                o.GetComponent<RawImage>(),
                o.GetComponent<Button>()
            ));
        }

        // 2. 썸네일 로딩 전부 동시에
        await UniTask.WhenAll(mapEntries.Select(e =>
            string.IsNullOrEmpty(e.Item1.ThumbnailUrl)
                ? UniTask.CompletedTask
                : LoadThumbnailAsync(e.Item2, e.Item1.ThumbnailUrl)
        ));

        // 3. 버튼 이벤트 등록
        foreach (var e in mapEntries)
        {
            var map = e.Item1;
            var tex = e.Item2.texture;
            var btn = e.Item3;
            var m = new Tuple<Map, Texture, Button>(map, tex, btn);
            btn.onClick.AddListener(() => SelectedMap = m);
        }

        await SceneChange.Instance.ManualEndFade();
    }

    private void UpdateRightPage()
    {
        var m = _selectedMapCreating.Item1;
        rightPageRawImage.texture = _selectedMapCreating.Item2 == null ? _defaultThumbnail : _selectedMapCreating.Item2;
        rightPageName.text = m.Name;
        rightPageName.placeholder.GetComponent<TextMeshProUGUI>().text = m.Name;
        rightPageDescription.text = m.Desc;
        rightPageDescription.placeholder.GetComponent<TextMeshProUGUI>().text = m.Desc;
        rightPageObject.SetActive(true);
    }
    
    private void MapUpdateRightPage()
    {
        var m = _selectedMap.Item1;
        mapRightPageRawImage.texture = _selectedMap.Item2;
        mapRightPageName.text = m.Name;
        mapRightPageName.placeholder.GetComponent<TextMeshProUGUI>().text = m.Name;
        mapRightPageDescription.text = m.Desc;
        mapRightPageDescription.placeholder.GetComponent<TextMeshProUGUI>().text = m.Desc;
        mapRightPageIdTMP.text = m.MapId.ToString();
        mapRightPageLikesTMP.text = m.NumLikes.ToString();
        mapRightPageBestMovesTMP.text = m.BestMoves == null ? "none" : m.BestMoves.Value.ToString();
        mapRightPageObject.SetActive(true);
    }
    
    private async UniTask LoadThumbnailAsync(RawImage rawImage, string url)
    {
        var bustUrl = $"{url}?t={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        using var request = UnityWebRequestTexture.GetTexture(bustUrl);
        await request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"썸네일 로드 실패: {request.error}");
            //PopUpManager.Instance.Show("썸네일 로드 실패");
            return;
        }
        /*
        if (rawImage.texture != null)
            Destroy(rawImage.texture);
        */
        rawImage.texture = DownloadHandlerTexture.GetContent(request);
    }
    
    // 온라인 체크 + 예외처리 필요
    public async void CreateMap()
    {
        if (!GameManager.Instance.CheckNetworkAndLogIn())
            return;
        
        SceneChange.Instance.LightLoading(true);
        try
        {
            await DBManager.Instance.InsertMapCreatingAsync(new MapCreating(){UserId = Guid.Parse(SupabaseManager.Instance.Supabase().Auth.CurrentUser.Id)});
        }
        catch (Exception e)
        {
            Debug.LogWarning(e.Message);
            PopUpManager.Instance.Show("나중에 다시 시도해주세요.");
            SceneChange.Instance.LightLoading(false);
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
            SceneChange.Instance.LightLoading(false);
            return;
        }
        var o = Instantiate(levelButtonPrefab, levelButtonParent);
        var newMap = new Tuple<MapCreating, Texture, Button>(recent, null, o.GetComponent<Button>());
        newMap.Item3.onClick.AddListener(() => SelectedMapCreating = newMap);
        SceneChange.Instance.LightLoading(false);
    }
    
    // 온라인 체크 + 예외처리 필요
    public async void DeleteMap()
    {
        if (!GameManager.Instance.CheckNetworkAndLogIn())
            return;
        
        SceneChange.Instance.LightLoading(true);
        try
        {
            await DBManager.Instance.DeleteMapCreatingAsync(_selectedMapCreating.Item1.MapId);
        }
        catch (Exception e)
        {
            Debug.LogWarning(e.Message);
            PopUpManager.Instance.Show("나중에 다시 시도해주세요.");
            SceneChange.Instance.LightLoading(false);
            return;
        }
        
        if(_selectedMapCreating.Item2 != _defaultThumbnail)
            Destroy(_selectedMapCreating.Item2);
        Destroy(_selectedMapCreating.Item3.gameObject);
        _selectedMapCreating = null;
        
        rightPageObject.SetActive(false);
        SceneChange.Instance.LightLoading(false);
        PopUpManager.Instance.Show("맵이 삭제되었습니다.");
    }

    public async void DeleteUploadedMap()
    {
        if (!GameManager.Instance.CheckNetworkAndLogIn())
            return;
        
        SceneChange.Instance.LightLoading(true);
        try
        {
            await DBManager.Instance.DeleteMapAsync(_selectedMap.Item1.MapId);
        }
        catch (Exception e)
        {
            Debug.LogWarning(e.Message);
            PopUpManager.Instance.Show("나중에 다시 시도해주세요.");
            SceneChange.Instance.LightLoading(false);
            return;
        }
        
        if(_selectedMap.Item2 != _defaultThumbnail)
            Destroy(_selectedMap.Item2);
        Destroy(_selectedMap.Item3.gameObject);
        _selectedMap = null;
        
        mapRightPageObject.SetActive(false);
        SceneChange.Instance.LightLoading(false);
        PopUpManager.Instance.Show("맵이 삭제되었습니다.");
    }
    
    public void EditMap()
    {
        GameManager.Instance.EnterEditor(_selectedMapCreating.Item1);
    }

    public async void PlayMap()
    {
        var id = _selectedMap.Item1.MapId;
        var best = _selectedMap.Item1.BestMoves;
        var user = _selectedMap.Item1.UserId;
        var d = StringHelper.DecodeCube(_selectedMap.Item1.Data);
        Dictionary<Vector3Int, Vector3Int> pDic = null;
        if (_selectedMap.Item1.PortalPairs != null)
        {
            var pList = PortalPairHelper.Decode(_selectedMap.Item1.PortalPairs);
            pDic = PortalPairHelper.ToDict(pList);
        }

        RotateInfo rotInfo = new RotateInfo() { Axis = 0, Layers = null };
        if (_selectedMap.Item1.RotInfo != null)
        {
            rotInfo = RotateHelper.Decode(_selectedMap.Item1.RotInfo);
        }

        GameManager.Instance.UserClearEvent += () =>
        {
            canvas.SetActive(true);
            cam.SetActive(true);
        };
        GameManager.Instance.UserClearEvent += async () =>
        {
            try
            {
                var r = await DBManager.Instance.GetNewBestMoves(id, user);
                if (r != null)
                {
                    _selectedMap.Item1.BestMoves = r.Value;
                    mapRightPageBestMovesTMP.text = r.Value.ToString();
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning(e.Message);
            }
        };
        GameManager.Instance.UserEnterEvent += ()=>
        {
            canvas.SetActive(false);
            cam.SetActive(false);
        };
        await GameManager.Instance.EnterGameUser(id, user, best, d, pDic, rotInfo.Axis, rotInfo.Layers);
    }
    
    // 온라인 체크 + 예외처리 필요
    public async void UpdateMap()
    {
        if (!GameManager.Instance.CheckNetworkAndLogIn())
            return;
        
        SceneChange.Instance.LightLoading(true);
        var originalName = _selectedMapCreating.Item1.Name;
        var originalDesc = _selectedMapCreating.Item1.Desc;
        var newName = rightPageName.text.Trim();
        var newDesc = rightPageDescription.text.Trim();
        _selectedMapCreating.Item1.Name = newName;
        _selectedMapCreating.Item1.Desc = newDesc;

        if (string.IsNullOrEmpty(newName))
        {
            PopUpManager.Instance.Show("제목은 최소 한 자 이상이어야 합니다.");
            _selectedMapCreating.Item1.Name = originalName;
            _selectedMapCreating.Item1.Desc = originalDesc;
            rightPageName.text = originalName;
            rightPageName.placeholder.GetComponent<TextMeshProUGUI>().text = originalName;
            rightPageDescription.text = originalDesc;
            rightPageDescription.placeholder.GetComponent<TextMeshProUGUI>().text = originalDesc;
            SceneChange.Instance.LightLoading(false);
            return;
        }
        
        try
        {
            await DBManager.Instance.UpdateMapCreatingAsync(_selectedMapCreating.Item1);
        }
        catch (PostgrestException e) when (e.Message.Contains("inappropriate_content"))
        {
            PopUpManager.Instance.Show("부적절한 단어가 포함되어 있습니다.");
            _selectedMapCreating.Item1.Name = originalName;
            _selectedMapCreating.Item1.Desc = originalDesc;
            rightPageName.text = originalName;
            rightPageName.placeholder.GetComponent<TextMeshProUGUI>().text = originalName;
            rightPageDescription.text = originalDesc;
            rightPageDescription.placeholder.GetComponent<TextMeshProUGUI>().text = originalDesc;
            SceneChange.Instance.LightLoading(false);
            return;
        }
        catch (Exception e)
        {
            Debug.LogWarning(e.Message);
            PopUpManager.Instance.Show("나중에 다시 시도해주세요.");
            _selectedMapCreating.Item1.Name = originalName;
            _selectedMapCreating.Item1.Desc = originalDesc;
            rightPageName.text = originalName;
            rightPageName.placeholder.GetComponent<TextMeshProUGUI>().text = originalName;
            rightPageDescription.text = originalDesc;
            rightPageDescription.placeholder.GetComponent<TextMeshProUGUI>().text = originalDesc;
            SceneChange.Instance.LightLoading(false);
            return;
        }
        
        rightPageName.text = newName;
        rightPageName.placeholder.GetComponent<TextMeshProUGUI>().text = newName;
        rightPageDescription.text = newDesc;
        rightPageDescription.placeholder.GetComponent<TextMeshProUGUI>().text = newDesc;
        var b = _selectedMapCreating.Item3;
        b.GetComponentInChildren<TextMeshProUGUI>().text = newName;
        PopUpManager.Instance.Show("정보 업데이트 완료");
        SceneChange.Instance.LightLoading(false);
    }

    public void CancelEdit()
    {
        rightPageName.text = _selectedMapCreating.Item1.Name;
        rightPageName.placeholder.GetComponent<TextMeshProUGUI>().text = _selectedMapCreating.Item1.Name;
        rightPageDescription.text = _selectedMapCreating.Item1.Desc;
        rightPageDescription.placeholder.GetComponent<TextMeshProUGUI>().text = _selectedMapCreating.Item1.Desc;
    }
}
