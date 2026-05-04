using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using com.example;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Postgrest;
using Postgrest.Models;
using Postgrest.Attributes;
using Postgrest.Responses;
using BaseResponse = Postgrest.Responses.BaseResponse;
using Client = Supabase.Client;

#region Models
// ReSharper disable ExplicitCallerInfoArgument
[Table("map")]
public class Map : BaseModel
{
    [JsonProperty("map_id")]
    [PrimaryKey("map_id", shouldInsert: true)]
    public long MapId { get; set; }
    
    [JsonProperty("name")]
    [Column("name")]
    public string Name { get; set; } = default!;

    [JsonProperty("created_at")]
    [Column("created_at", ignoreOnInsert: true, ignoreOnUpdate: true)]
    public DateTime CreatedAt { get; set; } // 현재 시간 기본값

    [JsonProperty("data")]
    [Column("data")]
    public string Data { get; set; }

    [JsonProperty("num_likes")]
    [Column("num_likes", ignoreOnInsert: true, ignoreOnUpdate: true)]
    public long NumLikes { get; set; } // 0 기본값

    [JsonProperty("user_id")]
    [PrimaryKey("user_id", shouldInsert: true)]
    public Guid UserId { get; set; }

    [JsonProperty("is_private")]
    [Column("is_private", ignoreOnInsert: true, ignoreOnUpdate: true)]
    public bool IsPrivate { get; set; }
    
    [JsonProperty("desc")]
    [Column("desc")]
    public string Desc { get; set; } // NULL 가능
    
    [JsonProperty("best_moves")]
    [Column("best_moves",  ignoreOnInsert: true, ignoreOnUpdate: true)]
    public short? BestMoves { get; set; } // NULL 기본값
    
    [JsonProperty("portal_pairs")]
    [Column("portal_pairs")]
    public string PortalPairs { get; set; }
    
    [JsonProperty("rotation_info")]
    [Column("rotation_info")]
    public string RotInfo { get; set; }
    
    [JsonProperty("thumbnail_url")]
    [Column("thumbnail_url")]
    public string ThumbnailUrl { get; set; }
}

[Table("map_clears")]
public class MapClears : BaseModel
{
    [PrimaryKey("user_id", shouldInsert: true)]
    public Guid UserId { get; set; }

    [PrimaryKey("map_id", shouldInsert: true)]
    public long MapId { get; set; }
    
    [PrimaryKey("map_user_id", shouldInsert: true)]
    public Guid MapUserId { get; set; }
    
    [Column("moves")]
    public short Moves { get; set; }
}

[Table("map_creating")]
public class MapCreating : BaseModel
{
    [PrimaryKey("map_id", shouldInsert: false)]
    public long MapId { get; set; } // identity
    
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("created_at", ignoreOnInsert: true, ignoreOnUpdate: true)]
    public DateTimeOffset CreatedAt { get; set; } // 현재 시간 기본값

    [Column("data", ignoreOnInsert: true)] 
    public string Data { get; set; }

    [Column("name")] 
    public string Name { get; set; } = "New Puzzle";
    
    [Column("desc", ignoreOnInsert: true)]
    public string Desc { get; set; } // NULL 가능
    
    [JsonProperty("portal_pairs", NullValueHandling = NullValueHandling.Ignore)]
    [Column("portal_pairs", ignoreOnInsert: true)]
    public string PortalPairs { get; set; }
    
    [JsonProperty("rotation_info", NullValueHandling = NullValueHandling.Ignore)]
    [Column("rotation_info", ignoreOnInsert: true)]
    public string RotInfo { get; set; }
    
    [Column("thumbnail_url", ignoreOnInsert: true)]
    public string ThumbnailUrl { get; set; }
}

[Table("map_likes")]
public class MapLikes : BaseModel
{
    [PrimaryKey("user_id", shouldInsert: true)]
    public Guid UserId { get; set; } // auth.id() 기본값

    [PrimaryKey("map_id", shouldInsert: true)]
    public long MapId { get; set; }
    
    [PrimaryKey("map_user_id", shouldInsert: true)]
    public Guid MapUserId { get; set; }
}

[Table("story_saves")]
public class StorySaves : BaseModel
{
    [PrimaryKey("user_id", shouldInsert: true)]
    public Guid UserId { get; set; } // auth.id() 기본값

    [PrimaryKey("map_id", shouldInsert: true)]
    public short MapId { get; set; }
    
    [Column("moves")]
    public short Moves { get; set; }
}

[Table("report")]
public class Report : BaseModel
{
    [PrimaryKey("user_id", shouldInsert: true)]
    public Guid UserId { get; set; } // auth.id() 기본값

    [PrimaryKey("map_id", shouldInsert: true)]
    public long MapId { get; set; }
    
    [PrimaryKey("map_user_id", shouldInsert: true)]
    public Guid MapUserId { get; set; }
    
    [Column("desc")]
    public string Desc { get; set; }
}

[Table("nickname")]
public class Nickname : BaseModel
{
    [PrimaryKey("user_id", shouldInsert: true)]
    public Guid UserId { get; set; } // auth.id() 기본값
    
    [Column("name")]
    public string Name { get; set; }
}

public class MapInteractionResult
{
    [JsonProperty("map_id")]
    public long MapId { get; set; }

    [JsonProperty("map_user_id")]
    public Guid MapUserId { get; set; }

    [JsonProperty("is_liked")]
    public bool IsLiked { get; set; }

    [JsonProperty("is_cleared")]
    public bool IsCleared { get; set; }

    [JsonProperty("is_reported")]
    public bool IsReported { get; set; }
}

// ReSharper restore ExplicitCallerInfoArgument
#endregion

public class MapDetailResult
{
    public Map Map       { get; set; }
    public bool IsLiked  { get; set; }
    public bool IsCleared { get; set; }
    public bool IsReported { get; set; }
    public string Nickname { get; set; }
}

public enum SortOrder { Ascending, Descending }

public enum ClearFilter
{
    All,
    ClearedOnly,
    NotClearedOnly
}

public class DBManager : MonoBehaviour
{
    public static DBManager Instance;
    
    private const int PageSize = 6;
    
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

    }

    #region Map
    
    private async Task<List<Map>> FetchPageAsync(
        int page,
        string sortColumn = "created_at",
        SortOrder sortOrder = SortOrder.Descending,
        string search = null,
        ClearFilter clearFilter = ClearFilter.All)
    {
        int from = page * PageSize;
        int to   = from + PageSize - 1;
        var _client = SupabaseManager.Instance.Supabase();
        var userId = _client.Auth.CurrentUser.Id;

        var orderStr = sortOrder == SortOrder.Ascending ? "ASC" : "DESC";
        var clearStr = clearFilter switch
        {
            ClearFilter.ClearedOnly    => "cleared",
            ClearFilter.NotClearedOnly => "not_cleared",
            _                          => "all"
        };

        var rpcParams = new Dictionary<string, object>
        {
            { "p_user_id",      userId },
            { "p_sort_col",     sortColumn },
            { "p_sort_order",   orderStr },
            { "p_from_idx",     from.ToString() },
            { "p_to_idx",       to.ToString() },
            { "p_clear_filter", clearStr }
        };
        if (!string.IsNullOrEmpty(search))
            rpcParams.Add("p_search", search);

        var response = await _client.Rpc("fetch_maps_page", rpcParams);
        //Debug.Log(response.Content);
        return string.IsNullOrEmpty(response.Content)
            ? new List<Map>()
            : JsonConvert.DeserializeObject<List<Map>>(response.Content) ?? new List<Map>();
    }

    public async Task<List<MapDetailResult>> FetchPageWithDetailsAsync(
        int page = 0,
        string sortColumn = "created_at",
        SortOrder sortOrder = SortOrder.Descending,
        string search = null,
        ClearFilter clearFilter = ClearFilter.All)
    {
        var _client = SupabaseManager.Instance.Supabase();
        List<Map> maps;
        try
        {
            maps = await FetchPageAsync(page, sortColumn, sortOrder, search, clearFilter);
        }
        catch (Exception e)
        {
            Debug.LogWarning(e.Message);
            throw;
        }
        //Debug.Log(maps[0].MapId.ToString());

        if (maps.Count == 0) return new List<MapDetailResult>();
        
        var creatorIds = maps.Select(m => (object)m.UserId.ToString()).Distinct().ToList();
        var userId = _client.Auth.CurrentUser.Id;

        var mapKeys = maps.Select(m => new { map_id = m.MapId, map_user_id = m.UserId }).ToList();

        var rpcParams = new Dictionary<string, object>
        {
            { "p_user_id", userId },
            { "p_map_keys", JArray.FromObject(mapKeys) }
        };

        Task<BaseResponse> interactionsTask;
        Task<ModeledResponse<Nickname>> nicknameTask;
        try
        {
            interactionsTask = _client.Rpc("get_user_map_interactions", rpcParams);
            nicknameTask = _client.From<Nickname>()
                .Filter("user_id", Constants.Operator.In, creatorIds)
                .Get();

            await Task.WhenAll(interactionsTask, nicknameTask);
        }
        catch (Exception e)
        {
            Debug.LogWarning(e.Message);
            throw;
        }
        //Debug.Log(interactionsTask.Result.Content);
        var interactions = string.IsNullOrEmpty(interactionsTask.Result.Content)
            ? new List<MapInteractionResult>()
            : JsonConvert.DeserializeObject<List<MapInteractionResult>>(interactionsTask.Result.Content) 
              ?? new List<MapInteractionResult>();
        var interactionMap = interactions.ToDictionary(x => (x.MapId, x.MapUserId));
        var nicknameMap = nicknameTask.Result.Models.ToDictionary(x => x.UserId, x => x.Name);
        
        return maps.Select(map => new MapDetailResult
        {
            Map        = map,
            IsLiked    = interactionMap.TryGetValue((map.MapId, map.UserId), out var i) && i.IsLiked,
            IsCleared  = interactionMap.TryGetValue((map.MapId, map.UserId), out var j) && j.IsCleared,
            IsReported = interactionMap.TryGetValue((map.MapId, map.UserId), out var k) && k.IsReported,
            Nickname   = nicknameMap.GetValueOrDefault(map.UserId, "익명"),
        }).ToList();
    }
    
    // 유저맵 업로드 함수(RLS에 의해 본인 맵만 삽입 가능)
    public async Task UpsertMapAsync(Map map)
    {
        await SupabaseManager.Instance.Supabase().From<Map>().Upsert(map, new QueryOptions(){Returning = QueryOptions.ReturnType.Minimal});
    }
    
    // 맵 id를 사용한 유저맵 삭제(RLS에 의해 본인 맵만 삭제 가능)
    public async Task DeleteMapAsync(long id)
    {
        var client = SupabaseManager.Instance.Supabase();
        
        try
        {
            var userId = SupabaseManager.Instance.Supabase().Auth.CurrentUser.Id;
            await client.Storage
                .From("uploaded-map-thumbnails")
                .Remove(new List<string> { $"{userId}/{id}.jpg" });
        }
        catch (Exception e)
        {
            // Storage 실패해도 DB 삭제는 진행
            Debug.LogWarning($"썸네일 삭제 실패 (무시): {e.Message}");
        }
        
        await client.From<Map>()
            .Where(x => x.MapId == id)
            .Delete();
    }
    
    public async Task<List<Map>> FetchMyMapAsync()
    {
        var userId = Guid.Parse(SupabaseManager.Instance.Supabase().Auth.CurrentUser.Id);
        var response = await SupabaseManager.Instance.Supabase().From<Map>()
            .Where(x => x.UserId == userId).Order("map_id", Constants.Ordering.Ascending).Get();
        return response.Models;
    }
    
    public async Task<short?> GetNewBestMoves(long mapId, Guid userId)
    {
        var response = await SupabaseManager.Instance.Supabase().From<Map>()
            .Select("best_moves")
            .Where(x => x.UserId == userId && x.MapId == mapId)
            .Limit(1)
            .Get();

        return response.Models.FirstOrDefault()?.BestMoves;
    }
    
    #endregion
    
    #region Map_Likes
    
    // 좋아요 삽입
    public async Task ToggleMapLikesAsync(long mapId, Guid mapUserId)
    {
        var rpcParams = new Dictionary<string, object>
        {
            { "p_map_id", mapId.ToString() },
            { "p_map_user_id", mapUserId.ToString() }
        };

        await SupabaseManager.Instance.Supabase().Rpc("toggle_map_like", rpcParams);
    }
    
    #endregion
    
    #region Map_Clears

    public async Task UpsertMapClearsAsync(MapClears mapClear)
    {
        await SupabaseManager.Instance.Supabase().From<MapClears>().Insert(mapClear); // upsert를 하지 않은 이유는 Trigger에 의해 자동 업데이트를 설정해놨기 때문
    }
    
    public async Task<bool> GetIsCleared(long mapId, Guid mapUserId)
    {
        var userId = SupabaseManager.Instance.Supabase().Auth.CurrentUser.Id;
        var result = await SupabaseManager.Instance.Supabase()
            .From<MapClears>()
            .Select("user_id")
            .Filter("user_id", Constants.Operator.Equals, userId)
            .Filter("map_user_id", Constants.Operator.Equals, mapUserId.ToString())
            .Filter("map_id", Constants.Operator.Equals, mapId.ToString())
            .Limit(1)
            .Get();

        return result.Models.Count > 0;
    }
    
    #endregion

    #region Map_Creating

    public async Task InsertMapCreatingAsync(MapCreating map)
    {
        await SupabaseManager.Instance.Supabase().From<MapCreating>().Insert(map);
    }
    
    public async Task UpdateMapCreatingAsync(MapCreating map)
    {
        await SupabaseManager.Instance.Supabase().From<MapCreating>().Update(map);
    }

    public async Task DeleteMapCreatingAsync(long id)
    {
        // 1. Storage 먼저
        try
        {
            await SupabaseManager.Instance.Supabase().Storage
                .From("map-thumbnails")
                .Remove(new List<string> { $"{id}.jpg" });
        }
        catch (Exception e)
        {
            // Storage 실패해도 DB 삭제는 진행
            Debug.LogWarning($"썸네일 삭제 실패 (무시): {e.Message}");
        }

        // 2. DB 삭제
        await SupabaseManager.Instance.Supabase()
            .From<MapCreating>()
            .Where(x => x.MapId == id)
            .Delete();
    }

    public async Task<List<MapCreating>> FetchMapCreatingAsync()
    {
        var response = await SupabaseManager.Instance.Supabase().From<MapCreating>().Order("map_id", Constants.Ordering.Ascending).Get();
        return response.Models;
    }

    public async Task<MapCreating> FetchRecentMapCreatingSingleAsync()
    {
        var response = await SupabaseManager.Instance.Supabase()
            .From<MapCreating>()
            .Order("created_at", Constants.Ordering.Descending)
            .Limit(1)
            .Get();
        return response.Models.FirstOrDefault();
    }

    #endregion

    #region Story_Saves

    public async Task UpsertStorySavesAsync(StorySaves save)
    {
        await SupabaseManager.Instance.Supabase().From<StorySaves>().Insert(save); // upsert를 하지 않은 이유는 Trigger에 의해 자동 업데이트를 설정해놨기 때문
    }

    public async Task<List<StorySaves>> FetchStorySavesAsync()
    {
        var response = await SupabaseManager.Instance.Supabase().From<StorySaves>().Get();

        return response.Models;
    }

    #endregion
    
    #region Nickname

    public async Task UpsertNicknameAsync(string text)
    {
        var userId = Guid.Parse(SupabaseManager.Instance.Supabase().Auth.CurrentUser.Id);
        await SupabaseManager.Instance.Supabase().From<Nickname>().Upsert(new  Nickname { UserId = userId, Name = text });
    }
    
    public async Task<bool> HasNicknameAsync()
    {
        var userId = SupabaseManager.Instance.Supabase().Auth.CurrentUser.Id;
        var result = await SupabaseManager.Instance.Supabase()
            .From<Nickname>()
            .Select("user_id")
            .Filter("user_id", Constants.Operator.Equals, userId)
            .Count(Constants.CountType.Exact);

        return result > 0;
    }

    public async Task<bool> IsNicknameAvailableAsync(string nickname)
    {
        var result = await SupabaseManager.Instance.Supabase()
            .From<Nickname>()
            .Select("user_id")
            .Filter("name", Constants.Operator.Equals, nickname)
            .Count(Constants.CountType.Exact);

        return result == 0;
    }
    
    #endregion
    
    #region Report

    public async Task InsertReportAsync(Report report)
    {
        await SupabaseManager.Instance.Supabase().From<Report>().Insert(report);
    }
    
    #endregion
}
