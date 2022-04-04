using Microsoft.Extensions.Caching.Memory;

namespace SuggestionAppLib.DataAccess;

public class MongoStatusData : IStatusData
{
    private readonly IMongoCollection<StatusModel> _statuses;
    private readonly IMemoryCache _cache;
    private const string _CACHENAME = "StatusData";

    public MongoStatusData(IDbConnection dbConnection, IMemoryCache cache)
    {
        _cache = cache;
        _statuses = dbConnection.StatusCollection;
    }
    public async Task<List<StatusModel>> GetAllStatuses()
    {
        var output = _cache.Get<List<StatusModel>>(_CACHENAME);
        if (output == null)
        {
            var results = await _statuses.FindAsync(_ => true);
            output = results.ToList();
            _cache.Set(_CACHENAME, output, TimeSpan.FromDays(1));
        }
        return output;
    }

    public Task CreateStatus(StatusModel status)
    {
        return _statuses.InsertOneAsync(status);
    }
}
