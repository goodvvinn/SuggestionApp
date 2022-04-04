namespace SuggestionAppLib.Models;

public class BasicUserModel
{
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }
    public string DisplayName { get; set; }

    public BasicUserModel()
    {
    }
    public BasicUserModel(UserModel userModel)
    {
        Id = userModel.Id;
        DisplayName = userModel.DisplayName;
    }

}
