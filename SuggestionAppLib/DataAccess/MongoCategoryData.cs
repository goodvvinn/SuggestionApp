using Microsoft.Extensions.Caching.Memory;

namespace SuggestionAppLib.DataAccess;

public class MongoCategoryData : ICategoryData
{
    private readonly IMemoryCache _memoryCache;
    private readonly IMongoCollection<CategoryModel> _categories;
    private const string _CACHENAME = "CategoryData";

    public MongoCategoryData(IDbConnection dbConnection, IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
        _categories = dbConnection.CategoryCollection;
    }
    public async Task<List<CategoryModel>> GetAllCategories()
    {
        var output = _memoryCache.Get<List<CategoryModel>>(_CACHENAME);
        if (output == null)
        {
            var results = await _categories.FindAsync(_ => true);
            output = results.ToList();

            _memoryCache.Set(_CACHENAME, output, TimeSpan.FromDays(1));
        }

        return output;
    }

    public Task CreateCategory(CategoryModel category)
    {
        return _categories.InsertOneAsync(category);
    }
}
