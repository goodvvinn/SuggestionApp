
namespace SuggestionAppLib.DataAccess;

public class MongoUserData : IUserData
{
    private readonly IMongoCollection<UserModel> _users;

    public MongoUserData(IDbConnection dbConnection)
    {
        _users = dbConnection.UserCollection;
    }

    public async Task<List<UserModel>> GetUsersAsync()
    {
        var result = await _users.FindAsync(_ => true);
        return result.ToList();
    }

    public async Task<UserModel> GetUser(string id)
    {
        var result = await _users.FindAsync(r => r.Id == id);
        return result.FirstOrDefault();
    }
    public async Task<UserModel> GetUserFromAuthentication(string objectId)
    {
        var result = await _users.FindAsync(r => r.ObjectIdentifier == objectId);
        return result.FirstOrDefault();
    }
    public Task CreateUser(UserModel user)
    {
        return _users.InsertOneAsync(user);
    }
    public Task UpdateUser(UserModel user)
    {
        var filter = Builders<UserModel>.Filter.Eq("Id", user.Id);
        return _users.ReplaceOneAsync(filter, user, new ReplaceOptions { IsUpsert = true });
    }

}

