using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using com.example;
using Newtonsoft.Json;
using Postgrest;
using Postgrest.Models;
using Postgrest.Attributes;
using Postgrest.Responses;
using Client = Supabase.Client;

#region Models
// ReSharper disable ExplicitCallerInfoArgument
[Table("map")]
public class Map : BaseModel
{
    [PrimaryKey("id", shouldInsert: true)]
    public long Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = default!;

    [Column("created_at", ignoreOnInsert: true, ignoreOnUpdate: true)]
    public DateTime CreatedAt { get; set; } // 현재 시간 기본값

    [Column("data")]
    public string Data { get; set; }

    [Column("num_likes", ignoreOnInsert: true, ignoreOnUpdate: true)]
    public long NumLikes { get; set; } // 0 기본값

    [Column("user_id", ignoreOnInsert: true, ignoreOnUpdate: true)]
    public Guid UserId { get; set; } // auth.uid 기본값

    [Column("is_private")]
    public bool IsPrivate { get; set; }
    
    [Column("desc")]
    public string Desc { get; set; } // NULL 가능

    [Column("played_count", ignoreOnInsert: true, ignoreOnUpdate: true)]
    public long PlayedCount { get; set; } // 0 기본값

    [Column("best_moves",  ignoreOnInsert: true, ignoreOnUpdate: true)]
    public short? BestMoves { get; set; } // NULL 기본값
    
    [JsonProperty("portal_pairs")]
    [Column("portal_pairs")]
    public string PortalPairs { get; set; }
    
    [JsonProperty("rotation_info")]
    [Column("rotation_info")]
    public string RotInfo { get; set; }
}

[Table("map_clears")]
public class MapClears : BaseModel
{
    [PrimaryKey("user_id", shouldInsert: false)]
    public Guid UserId { get; set; } // auth.id() 기본값

    [PrimaryKey("map_id", shouldInsert: true)]
    public long MapId { get; set; }
    
    [Column("moves")]
    public short Moves { get; set; }
}

[Table("map_creating")]
public class MapCreating : BaseModel
{
    [PrimaryKey("map_id", shouldInsert: false)]
    public long MapId { get; set; } // identity
    
    [Column("user_id", ignoreOnInsert: true, ignoreOnUpdate: true)]
    public Guid UserId { get; set; } // auth.id() 기본값

    [Column("created_at", ignoreOnInsert: true, ignoreOnUpdate: true)]
    public DateTimeOffset CreatedAt { get; set; } // 현재 시간 기본값

    [Column("data", ignoreOnInsert: true)] 
    public string Data { get; set; } = default!;

    [Column("name")] 
    public string Name { get; set; } = "my map";
    
    [JsonProperty("portal_pairs")]
    [Column("portal_pairs")]
    public string PortalPairs { get; set; }
    
    [JsonProperty("rotation_info")]
    [Column("rotation_info")]
    public string RotInfo { get; set; }
}

[Table("map_likes")]
public class MapLikes : BaseModel
{
    [PrimaryKey("user_id", shouldInsert: false)]
    public Guid UserId { get; set; } // auth.id() 기본값

    [PrimaryKey("map_id", shouldInsert: true)]
    public long MapId { get; set; }
}

[Table("story_saves")]
public class StorySaves : BaseModel
{
    [PrimaryKey("user_id", shouldInsert: false)]
    public Guid UserId { get; set; } // auth.id() 기본값

    [PrimaryKey("map_id", shouldInsert: true)]
    public short MapId { get; set; }
    
    [Column("moves")]
    public short Moves { get; set; }
}

// ReSharper restore ExplicitCallerInfoArgument
#endregion

public class MapDetailResult
{
    public Map Map       { get; set; }
    public bool IsLiked  { get; set; }
    public bool IsCleared { get; set; }
    public bool IsOwner { get; set; }
}

public enum SortOrder { Ascending, Descending }

public class DBManager : MonoBehaviour
{
    public static DBManager Instance;
    private Client _client;
    
    private const int PageSize = 10;
    private int _currentPage = 0;
    public int CurrentPage() => _currentPage;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        _client = SupabaseManager.Instance.Supabase();
    }
    
    #region Map
    
    // 유저맵 가져오기 함수들 (RLS에 의해 public과, 본인의 private 맵들을 조회)
    private async Task<List<Map>> FetchPageAsync(
        int page,
        string sortColumn = "created_at",
        SortOrder sortOrder = SortOrder.Descending,
        string nameSearch = null, string idSearch = null)
    {
        int from = page * PageSize;
        int to   = from + PageSize - 1;

        var ordering = sortOrder == SortOrder.Ascending ? Constants.Ordering.Ascending : Constants.Ordering.Descending;

        var table = _client.From<Map>();

        ModeledResponse<Map> response;

        if (!string.IsNullOrEmpty(idSearch))
        {
            var m = await table.Where(x => x.Id.ToString().Contains(idSearch))
                .Single();
            
            var l = new List<Map>();
            l.Add(m);
            return l;
        }
        else if (!string.IsNullOrEmpty(nameSearch))
        {
            response = await table.Filter("name", Constants.Operator.ILike, $"%{nameSearch}%")
                .Order(sortColumn, ordering)
                .Range(from, to)
                .Get();
        }
        else
        {
            response = await table.Order(sortColumn, ordering).Range(from, to).Get();
        }

        return response.Models;
    }
    
    // 내 좋아요 여부, 내 클리어 여부, 내 맵 여부까지 조회해서 가져옴
    public async Task<List<MapDetailResult>> FetchPageWithDetailsAsync(int page, string sortColumn = "created_at", 
        SortOrder sortOrder = SortOrder.Descending,
        string nameSearch = null, string idSearch = null)
    {
        // 1. 맵 목록 먼저 조회
        var maps = await FetchPageAsync(page, sortColumn, sortOrder, nameSearch, idSearch);
    
        if (maps.Count == 0) return new List<MapDetailResult>();
    
        var mapIds = maps.Select(m => m.Id).ToList();

        // 2. 좋아요/클리어를 map id 목록으로 한번에 조회 (쿼리 2개)
        var likedTask = _client.From<MapLikes>()
            .Filter("map_id", Constants.Operator.In, mapIds)
            .Get();

        var clearedTask = _client.From<MapClears>()
            .Filter("map_id", Constants.Operator.In, mapIds)
            .Get();

        await Task.WhenAll(likedTask, clearedTask);

        // 3. HashSet으로 빠르게 룩업
        var likedMapIds   = likedTask.Result.Models.Select(x => x.MapId).ToHashSet();
        var clearedMapIds = clearedTask.Result.Models.Select(x => x.MapId).ToHashSet();

        var currentUid = _client.Auth.CurrentUser.Id;

        // 4. 조합
        return maps.Select(map => new MapDetailResult
        {
            Map       = map,
            IsLiked   = likedMapIds.Contains(map.Id),
            IsCleared = clearedMapIds.Contains(map.Id),
            IsOwner   = map.UserId.ToString() == currentUid
        }).ToList();
    }
    
    public async Task<List<MapDetailResult>> FetchNextPageAsync(string sortColumn = "created_at", SortOrder sortOrder = SortOrder.Descending,
        string nameSearch = null, string idSearch = null)
    {
        var result = await FetchPageWithDetailsAsync(_currentPage + 1, sortColumn, sortOrder, nameSearch, idSearch);

        if (result.Count > 0)
        {
            _currentPage++;
            return result;
        }
        else
        {
            PopUpManager.Instance.Show("마지막 페이지입니다.");
            return null;
        }
    }
    
    public async Task<List<MapDetailResult>> FetchPrevPageAsync(string sortColumn = "created_at", SortOrder sortOrder = SortOrder.Descending,
        string nameSearch = null, string idSearch = null)
    {
        if (_currentPage == 0)
        {
            PopUpManager.Instance.Show("첫 페이지입니다.");
            return null;
        }
        
        return await FetchPageWithDetailsAsync(--_currentPage, sortColumn, sortOrder, nameSearch, idSearch);
    }
    
    public async Task<List<MapDetailResult>> RefreshPageAsync(string sortColumn = "created_at", SortOrder sortOrder = SortOrder.Descending,
        string nameSearch = null, string idSearch = null)
    {
        return await FetchPageWithDetailsAsync(_currentPage, sortColumn, sortOrder, nameSearch, idSearch);
    }
    
    // 유저맵 업로드 함수(RLS에 의해 본인 맵만 삽입 가능)
    public async Task UpsertMapAsync(Map map)
    {
        await _client.From<Map>().Upsert(map, new QueryOptions(){Returning = QueryOptions.ReturnType.Minimal});
    }
    
    // 맵 id를 사용한 유저맵 삭제(RLS에 의해 본인 맵만 삭제 가능)
    public async Task DeleteMapAsync(long id)
    {
        await _client.From<Map>()
            .Where(x => x.Id == id)
            .Delete();
    }

    public async Task IncreaseMapPlayCount()
    {
        await _client.Rpc("increment_map_play_count", null);
    }
    
    #endregion
    
    #region Map_Likes
    
    // 좋아요 삽입
    public async Task InsertMapLikesAsync(long mapId)
    {
        await _client.From<MapLikes>().Insert(new MapLikes{MapId = mapId});
    }
    
    #endregion
    
    #region Map_Clears

    public async Task UpsertMapClearsAsync(MapClears clear)
    {
        await _client.From<MapClears>().Insert(clear); // upsert를 하지 않은 이유는 Trigger에 의해 자동 업데이트를 설정해놨기 때문
    }
    
    #endregion

    #region Map_Creating

    public async Task InsertMapCreatingAsync(MapCreating map)
    {
        await _client.From<MapCreating>().Insert(map);
    }
    
    public async Task UpdateMapCreatingAsync(MapCreating map)
    {
        await _client.From<MapCreating>().Update(map);
    }

    public async Task DeleteMapCreatingAsync(MapCreating map)
    {
        await _client.From<MapCreating>().Delete(map);
    }

    public async Task<List<MapCreating>> FetchMapCreatingAsync()
    {
        var response = await _client.From<MapCreating>().Get();

        return response.Models;
    }

    #endregion

    #region Story_Saves

    public async Task UpsertStorySavesAsync(StorySaves save)
    {
        await _client.From<StorySaves>().Insert(save); // upsert를 하지 않은 이유는 Trigger에 의해 자동 업데이트를 설정해놨기 때문
    }

    public async Task<List<StorySaves>> FetchStorySavesAsync()
    {
        var response = await _client.From<StorySaves>().Get();

        return response.Models;
    }

    #endregion
}
