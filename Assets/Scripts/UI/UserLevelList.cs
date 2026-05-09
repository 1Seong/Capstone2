using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using com.example;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Postgrest.Exceptions;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class UserLevelList : MonoBehaviour
{
    [SerializeField] private UserLevelCell[] levelCells;
    [SerializeField] private RawImage[] rawImages;
    [SerializeField] private Button[] buttons;
    [SerializeField] private Texture _defaultThumbnail;
    
    [SerializeField] private GameObject cam;
    [SerializeField] private GameObject canvas;
    [SerializeField] private GameObject rightPageObject;
    [SerializeField] private RawImage rightPageRawImage;
    [SerializeField] private TMP_InputField rightPageName;
    [SerializeField] private TMP_InputField rightPageDescription;
    [SerializeField] private TextMeshProUGUI rightPageLikesTMP;
    [SerializeField] private TextMeshProUGUI rightPageIdTMP;
    [SerializeField] private TextMeshProUGUI rightPageBestMovesTMP;
    [SerializeField] private TextMeshProUGUI rightPageNicknameTMP;
    [SerializeField] private GameObject rightPageClearObject;
    [SerializeField] private Button rightPageReportButton;
    //[SerializeField] private Image rightPageLikeImage;
    [SerializeField] private Transform rightPageLikeFilledImage;
    [SerializeField] private GameObject reportPanel;
    [SerializeField] private Scrollbar scrollBar;
    
    private Tuple<MapDetailResult, Texture, Button> _selectedMap;
    public Tuple<MapDetailResult, Texture, Button> SelectedMap
    {
        set
        {
            _selectedMap = value;
            UpdateRightPage();
        }
    }
    private int _currentPage = 0;
    private int CurrentPage
    {
        set
        {
            _currentPage = value;
            pageText.text = _currentPage.ToString();
        }
    }

    [SerializeField] private TMP_Dropdown sortDropdown;
    [SerializeField] private TMP_Dropdown orderDropdown;
    [SerializeField] private TMP_Dropdown filterDropdown;
    [SerializeField] private TMP_InputField searchInput;
    [SerializeField] private TextMeshProUGUI pageText;
    [SerializeField] private TMP_InputField reportInput;

    private string _currentSearch;
    private string _currentSort = "created_at";
    private SortOrder _currentSortOrder = SortOrder.Descending;
    private ClearFilter _currentFilter = ClearFilter.All;
    private const int PageNum = 18;

    private async void Start()
    {
        List<MapDetailResult> maps = new();
        try
        {
            maps = await DBManager.Instance.FetchPageWithDetailsAsync();
        }
        catch (Exception e)
        {
            Debug.LogWarning(e.Message);
            PopUpManager.Instance.Show("맵을 가져올 수 없습니다.");
            await SceneChange.Instance.ManualEndFade();
            return;
        }
        
        await UpdateCells(maps);
        await SceneChange.Instance.ManualEndFade();
    }

    private async UniTask UpdateCells(List<MapDetailResult> maps)
    {
        scrollBar.value = 1;
        foreach (var i in levelCells)
        {
            i.gameObject.SetActive(false);
            var tex = i.GetComponentInChildren<RawImage>().texture;
            if(tex != _defaultThumbnail)
                Destroy(tex);
        }

        // 1. 버튼 오브젝트 먼저 다 생성
        var entries = new List<Tuple<MapDetailResult, RawImage, Button>>();
        for (int i = 0; i < maps.Count; i++)
        {
            levelCells[i].UpdateInfo(maps[i].IsCleared, maps[i].Map.Name, maps[i].Nickname, maps[i].Map.NumLikes, maps[i].Map.BestMoves);
            var o = levelCells[i].gameObject;
            entries.Add(new Tuple<MapDetailResult, RawImage, Button>(
                maps[i],
                rawImages[i],
                buttons[i]
            ));
        }

        // 2. 썸네일 로딩 전부 동시에
        await UniTask.WhenAll(entries.Select(e =>
            string.IsNullOrEmpty(e.Item1.Map.ThumbnailUrl)
                ? UniTask.CompletedTask
                : LoadThumbnailAsync(e.Item2, e.Item1.Map.ThumbnailUrl)
        ));

        // 3. 현재 항목 등록
        for(int i = 0; i != entries.Count; ++i)
        {
            var e = entries[i];
            var map = e.Item1;
            var tex = e.Item2.texture;
            var btn = e.Item3;
            var m = new Tuple<MapDetailResult, Texture, Button>(map, tex, btn);
            btn.onClick.AddListener(()=> SelectedMap = m);
            levelCells[i].gameObject.SetActive(true);
        }
    }
    
    private void UpdateRightPage()
    {
        var m = _selectedMap.Item1;
        rightPageRawImage.texture = _selectedMap.Item2;
        rightPageName.text = m.Map.Name;
        rightPageName.placeholder.GetComponent<TextMeshProUGUI>().text = m.Map.Name;
        rightPageDescription.text = m.Map.Desc;
        rightPageDescription.placeholder.GetComponent<TextMeshProUGUI>().text = m.Map.Desc;
        rightPageIdTMP.text = m.Map.MapId.ToString();
        rightPageLikesTMP.text = m.Map.NumLikes.ToString();
        rightPageBestMovesTMP.text = m.Map.BestMoves == null ? "없음" : m.Map.BestMoves.Value.ToString() + " 회";
        rightPageNicknameTMP.text = m.Nickname;
        rightPageClearObject.SetActive(m.IsCleared);
        rightPageReportButton.interactable = !m.IsReported;
        if(_selectedMap.Item1.IsLiked)
            rightPageLikeFilledImage.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
        else
            rightPageLikeFilledImage.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack);
        rightPageObject.SetActive(true);
    }
    
    private async UniTask LoadThumbnailAsync(RawImage rawImage, string url)
    {
        using var request = UnityWebRequestTexture.GetTexture(url);
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

    public async void PlayMap()
    {
        var id = _selectedMap.Item1.Map.MapId;
        var best = _selectedMap.Item1.Map.BestMoves;
        var user = _selectedMap.Item1.Map.UserId;
        var d = StringHelper.DecodeCube(_selectedMap.Item1.Map.Data);
        Dictionary<Vector3Int, Vector3Int> pDic = null;
        if (_selectedMap.Item1.Map.PortalPairs != null)
        {
            var pList = PortalPairHelper.Decode(_selectedMap.Item1.Map.PortalPairs);
            pDic = PortalPairHelper.ToDict(pList);
        }

        RotateInfo rotInfo = new RotateInfo() { Axis = 0, Layers = null };
        if (_selectedMap.Item1.Map.RotInfo != null)
        {
            rotInfo = RotateHelper.Decode(_selectedMap.Item1.Map.RotInfo);
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
                var t1 = DBManager.Instance.GetNewBestMoves(id, user);
                var t2 = DBManager.Instance.GetIsCleared(id, user);
                await Task.WhenAll(t1, t2);

                var cell = _selectedMap.Item3.GetComponent<UserLevelCell>();
                if (t1.Result != null)
                {
                    _selectedMap.Item1.Map.BestMoves = t1.Result.Value;
                    rightPageBestMovesTMP.text = t1.Result.Value.ToString();
                    cell.UpdateBest(t1.Result.Value);
                }
                if (t2.Result)
                {
                    _selectedMap.Item1.IsCleared = true;
                    rightPageClearObject.SetActive(false);
                    cell.UpdateCleared(true);
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
    
    public async void LoadNextPage()
    {
        if (!GameManager.Instance.CheckNetworkAndLogIn())
            return;
        
        SceneChange.Instance.LightLoading(true);
        List<MapDetailResult> result = new();
        try
        {
            result = await DBManager.Instance.FetchPageWithDetailsAsync(_currentPage + 1, _currentSort,
                _currentSortOrder, _currentSearch, _currentFilter);
        }
        catch (Exception e)
        {
            Debug.LogWarning(e.Message);
            PopUpManager.Instance.Show("맵을 가져올 수 없습니다.");
            SceneChange.Instance.LightLoading(false);
            return;
        }
        if (result.Count == 0)
        {
            PopUpManager.Instance.Show("마지막 페이지입니다.");
            SceneChange.Instance.LightLoading(false);
            return;
        }

        CurrentPage = _currentPage + 1;
        await UpdateCells(result);
        SceneChange.Instance.LightLoading(false);
    }
    
    public async void LoadPrevPage()
    {
        if (!GameManager.Instance.CheckNetworkAndLogIn())
            return;
        if (_currentPage == 0)
        {
            PopUpManager.Instance.Show("첫 페이지입니다.");
            return;
        }
        
        SceneChange.Instance.LightLoading(true);
        List<MapDetailResult> result = new();
        try
        {
            result = await DBManager.Instance.FetchPageWithDetailsAsync(_currentPage-1, _currentSort,
                _currentSortOrder, _currentSearch, _currentFilter);
        }
        catch (Exception e)
        {
            Debug.LogWarning(e.Message);
            PopUpManager.Instance.Show("맵을 가져올 수 없습니다.");
            SceneChange.Instance.LightLoading(false);
            return;
        }
        
        CurrentPage = _currentPage-1;
        await UpdateCells(result);
        SceneChange.Instance.LightLoading(false);
    }
    
    public async void SearchPageAsync()
    {
        if (!GameManager.Instance.CheckNetworkAndLogIn())
            return;
        
        SceneChange.Instance.LightLoading(true);
        List<MapDetailResult> result = new();
        
        var sort = sortDropdown.value switch
        {
            0 => "created_at",
            1 => "name",
            2 => "num_likes",
            _ => "created_at"
        };
        var order = orderDropdown.value switch
        {
            0 => SortOrder.Descending,
            1 => SortOrder.Ascending,
            _ => SortOrder.Descending
        };
        var filter = filterDropdown.value switch
        {
            0 => ClearFilter.All,
            1 => ClearFilter.NotClearedOnly,
            2 => ClearFilter.ClearedOnly,
            _ => ClearFilter.All
        };
        var search = searchInput.text;

        try
        {
            result = await DBManager.Instance.FetchPageWithDetailsAsync(0, sort,  order, search, filter);
        }
        catch (Exception e)
        {
            Debug.LogWarning(e.Message);
            PopUpManager.Instance.Show("맵을 가져올 수 없습니다.");
            SceneChange.Instance.LightLoading(false);
            return;
        }
        
        await UpdateCells(result);
        _currentSort = sort;
        _currentSortOrder = order;
        _currentSearch = search;
        _currentFilter = filter;
        CurrentPage = 0;
        
        SceneChange.Instance.LightLoading(false);
    }

    public async void ReportMap()
    {
        if (!GameManager.Instance.CheckNetworkAndLogIn())
            return;
        if (string.IsNullOrEmpty(reportInput.text.Trim()))
        {
            PopUpManager.Instance.Show("내용은 최소 한 자 이상이어야 합니다.");
            return;
        }
        // rightpage 버튼 비활시키고 맵 정보 수정
        SceneChange.Instance.LightLoading(true);
        var rep = new Report()
        {
            UserId = Guid.Parse(SupabaseManager.Instance.Supabase().Auth.CurrentUser.Id),
            MapId = _selectedMap.Item1.Map.MapId,
            MapUserId = _selectedMap.Item1.Map.UserId,
            Desc = reportInput.text.Trim()
        };
        
        try
        {
            await DBManager.Instance.InsertReportAsync(rep);
        }
        catch (PostgrestException e) when (e.Message.Contains("inappropriate_content"))
        {
            PopUpManager.Instance.Show("부적절한 단어가 포함되어 있습니다.");
            SceneChange.Instance.LightLoading(false);
            return;
        }
        catch (Exception e)
        {
            Debug.LogWarning(e.Message);
            PopUpManager.Instance.Show("나중에 다시 시도해주세요.");
            SceneChange.Instance.LightLoading(false);
            return;
        }

        rightPageReportButton.interactable = false;
        _selectedMap.Item1.IsReported = true;
        reportPanel.SetActive(false);
        PopUpManager.Instance.Show("신고가 접수되었습니다.");
        SceneChange.Instance.LightLoading(false);
    }

    public async void ToggleLikeMap() // 측정해보고 결정
    {
        var mapId = _selectedMap.Item1.Map.MapId;
        var mapUserId = _selectedMap.Item1.Map.UserId;
        var originalIsLiked = _selectedMap.Item1.IsLiked;

        _selectedMap.Item1.IsLiked = !originalIsLiked;
        string likes;
        if (originalIsLiked)
        {
            rightPageLikeFilledImage.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack);
            likes = (--_selectedMap.Item1.Map.NumLikes).ToString();
        }
        else
        {
            rightPageLikeFilledImage.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
            likes = (++_selectedMap.Item1.Map.NumLikes).ToString();
        }
        rightPageLikesTMP.text = likes;
        _selectedMap.Item3.GetComponent<UserLevelCell>().UpdateLikes(likes);
        
        //SceneChange.Instance.LightLoading(true);
        try
        {
            await DBManager.Instance.ToggleMapLikesAsync(mapId, mapUserId);
        }
        catch (Exception e)
        {
            Debug.LogWarning(e.Message);
            /*
            _selectedMap.Item1.IsLiked = originalIsLiked;
            if (originalIsLiked)
            {
                rightPageLikeImage.sprite = rightPageLikeSprite;
                likes = (++_selectedMap.Item1.Map.NumLikes).ToString();
            }
            else
            {
                rightPageLikeImage.sprite = rightPageUnlikeSprite;
                likes = (--_selectedMap.Item1.Map.NumLikes).ToString();
            }
            rightPageLikesTMP.text = likes;
            _selectedMap.Item3.GetComponent<UserLevelCell>().UpdateLikes(likes);
            */
        }
        //SceneChange.Instance.LightLoading(false);
    }
}
