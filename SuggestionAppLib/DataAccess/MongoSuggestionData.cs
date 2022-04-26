using Microsoft.Extensions.Caching.Memory;

namespace SuggestionAppLib.DataAccess;

public class MongoSuggestionData : ISuggestionData
{
    private readonly IDbConnection _dbConnection;
    private readonly IUserData _userData;
    private readonly IMemoryCache _memoryCache;
    private readonly IMongoCollection<SuggestionModel> _suggestions;
    private const string _CACHENAME = "SuggestionsData";

    public MongoSuggestionData(IDbConnection dbConnection, IUserData userData, IMemoryCache memoryCache)
    {
        _dbConnection = dbConnection;
        _userData = userData;
        _memoryCache = memoryCache;
        _suggestions = dbConnection.SuggestionCollection;
    }
    public async Task<List<SuggestionModel>> GetAllSuggestion()
    {
        var output = _memoryCache.Get<List<SuggestionModel>>(_CACHENAME);
        if (output == null)
        {
            var result = await _suggestions.FindAsync(r => r.Archived == false);
            output = result.ToList();
            _memoryCache.Set(_CACHENAME, output, TimeSpan.FromMinutes(1));
        }
        return output;
    }

    public async Task<List<SuggestionModel>> GetUsersSuggestions(string userId)
    {
        var output = _memoryCache.Get<List<SuggestionModel>>(userId);
        if (output == null)
        {
            var userSuggestions = await _suggestions.FindAsync(s => s.Author.Id == userId);
            output = userSuggestions.ToList();
            _memoryCache.Set(userId, output, TimeSpan.FromMinutes(1));
        }
        return output;
    }

    public async Task<List<SuggestionModel>> GetAllApprovedSuggestions()
    {
        var output = await GetAllSuggestion();
        return output.Where(suggestion => suggestion.ApprovedForRelease == true).ToList();
    }
    public async Task<SuggestionModel> GetSuggestion(string id)
    {
        var result = await _suggestions.FindAsync(s => s.Id == id);
        return result.FirstOrDefault();
    }
    public async Task<List<SuggestionModel>> GetAllSuggestionWaitingForApproval()
    {
        var output = await GetAllSuggestion();
        return output.Where(s => s.ApprovedForRelease == false && s.Rejected == false).ToList();
    }
    public async Task UpdateSuggestion(SuggestionModel suggestion)
    {
        await _suggestions.ReplaceOneAsync(r => r.Id == suggestion.Id, suggestion);
        _memoryCache.Remove(_CACHENAME);
    }
    public async Task UpvoteSuggestion(string suggestionId, string userId)
    {
        var client = _dbConnection.Client;
        using var session = await client.StartSessionAsync();

        session.StartTransaction();
        try
        {
            var db = client.GetDatabase(_dbConnection.DbName);
            var suggestionsInTransaction = db.GetCollection<SuggestionModel>(_dbConnection.SuggestionCollectionName);
            var suggestion = (await suggestionsInTransaction.FindAsync(s => s.Id == suggestionId)).First();

            bool IsUpvote = suggestion.UserVotes.Add(userId);

            if (!IsUpvote)
            {
                suggestion.UserVotes.Remove(userId);
            }
            await suggestionsInTransaction.ReplaceOneAsync(session, s => s.Id == suggestionId, suggestion);
            var usersInTransaction = db.GetCollection<UserModel>(_dbConnection.UserCollectionName);
            var user = await _userData.GetUser(userId);

            if (IsUpvote)
            {
                user.VotedOnSuggestions.Add(new BasicSuggestionModel(suggestion));
            }
            else
            {
                var suggestionToRemove = user.VotedOnSuggestions.Where(s => s.Id == suggestionId).FirstOrDefault();
                user.VotedOnSuggestions.Remove(new BasicSuggestionModel(suggestionToRemove));
            }
            await usersInTransaction.ReplaceOneAsync(session, u => u.Id == user.Id, user);

            await session.CommitTransactionAsync();

            _memoryCache.Remove(_CACHENAME);

        }
        catch (Exception ex)
        {
            await session.AbortTransactionAsync();
            throw;
        }
    }
    public async Task CreateSuggestion(SuggestionModel suggestion)
    {
        var client = _dbConnection.Client;
        using var session = await client.StartSessionAsync();
        session.StartTransaction();
        try
        {
            var db = client.GetDatabase(_dbConnection.DbName);
            var suggestionsInTransaction = db.GetCollection<SuggestionModel>(_dbConnection.SuggestionCollectionName);
            await suggestionsInTransaction.InsertOneAsync(session, suggestion);

            var usersInTransaction = db.GetCollection<UserModel>(_dbConnection.UserCollectionName);
            var user = await _userData.GetUser(suggestion.Author.Id);
            user.AuthoredSuggestions.Add(new BasicSuggestionModel(suggestion));
            await usersInTransaction.ReplaceOneAsync(u => u.Id == user.Id, user);

            await session.CommitTransactionAsync();

        }
        catch (Exception)
        {
            await session.AbortTransactionAsync();
            throw;
        }
    }
}
