using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

public class FilmService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;

    public FilmService(HttpClient httpClient, IMemoryCache cache)
    {
        _httpClient = httpClient;
        _cache = cache;
        _httpClient.BaseAddress = new Uri("https://ophim1.com/");
    }

    public async Task<MovieResponse?> GetMoviesByPage(int page)
    {
        string cacheKey = $"movies_page_{page}";

        if (_cache.TryGetValue(cacheKey, out MovieResponse cachedData))
        {
            return cachedData;
        }

        var response = await _httpClient.GetAsync($"danh-sach/phim-moi-cap-nhat?page={page}");

        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();

        var result = JsonSerializer.Deserialize<MovieResponse>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(10));

        return result;
    }


    public async Task<List<MovieItem>> GetLatestMovies()
    {
        var response = await _httpClient.GetAsync("danh-sach/phim-moi-cap-nhat?page=1");

        if (!response.IsSuccessStatusCode)
            return new List<MovieItem>();

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<MovieResponse>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return result?.items ?? new List<MovieItem>();
    }

    public async Task<MovieResponse?> SearchMovies(string keyword, int page = 1)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return null;

        var encodedKeyword = Uri.EscapeDataString(keyword);

        var response = await _httpClient.GetAsync(
            $"v1/api/tim-kiem?keyword={encodedKeyword}&page={page}");

        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();

        var result = JsonSerializer.Deserialize<V1MovieResponse>(json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        if (result?.data == null)
            return null;

        return new MovieResponse
        {
            items = result.data.items ?? new List<MovieItem>(),
            pagination = new Pagination
            {
                currentPage = result.data.@params.pagination.currentPage,
                totalPages = result.data.@params.pagination.totalPages
            }
        };
    }


    public async Task<MovieDetailResponse?> GetMovieDetail(string slug)
    {
        var response = await _httpClient.GetAsync($"phim/{slug}");

        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<MovieDetailResponse>(json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
    }

    
    public async Task<MovieResponse?> GetMoviesWithFilter(
    int page,
    string? category,
    string? country,
    string? year)
    {
        // USER REQUEST: "Show all" - we fetch 5 pages at a time to simulate a larger list
        int batchSize = 5; 
        int startApiPage = (page - 1) * batchSize + 1;
        
        var tasks = new List<Task<V1MovieResponse?>>();

        for (int i = 0; i < batchSize; i++)
        {
            int currentApiPage = startApiPage + i;
            tasks.Add(FetchSinglePage(currentApiPage, category, country, year));
        }

        var responses = await Task.WhenAll(tasks);

        var aggregatedItems = new List<MovieItem>();
        V1Pagination? lastPagination = null;

        foreach (var response in responses)
        {
            if (response?.data?.items != null)
            {
                aggregatedItems.AddRange(response.data.items);
                lastPagination = response.data.@params?.pagination;
            }
        }

        if (aggregatedItems.Count == 0 || lastPagination == null)
            return null;

        // Recalculate pagination for the UI
        // API TotalPages = 355 (for example)
        // Batch Size = 5
        // New TotalPages = Ceiling(355 / 5) = 71
        
        int newTotalPages = (int)Math.Ceiling((double)lastPagination.totalPages / batchSize);

        // Use status/msg from the last successful response
        var lastResponse = responses.LastOrDefault(r => r?.data?.items != null);
        
        return new MovieResponse
        {
            status = lastResponse?.status ?? default,
            msg = lastResponse?.msg ?? "",
            items = aggregatedItems,
            pagination = new Pagination
            {
                currentPage = page,
                totalPages = newTotalPages
            }
        };
    }

    private async Task<V1MovieResponse?> FetchSinglePage(int page, string? category, string? country, string? year)
    {
        var queryParams = new List<string>();
        queryParams.Add($"page={page}");

        if (!string.IsNullOrEmpty(category))
            queryParams.Add($"category={category}");

        if (!string.IsNullOrEmpty(country))
            queryParams.Add($"country={country}");

        if (!string.IsNullOrEmpty(year))
            queryParams.Add($"year={year}");

        var queryString = string.Join("&", queryParams);
        var endpoint = $"v1/api/danh-sach/phim-moi-cap-nhat?{queryString}";

        try 
        {
            var response = await _httpClient.GetAsync(endpoint);
            if (!response.IsSuccessStatusCode) return null;
            
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<V1MovieResponse>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch 
        {
            return null;
        }
    }


    public async Task<List<CategoryItem>> GetCategories()
    {
        var response = await _httpClient.GetAsync("the-loai");

        if (!response.IsSuccessStatusCode)
            return new List<CategoryItem>();

        var json = await response.Content.ReadAsStringAsync();

        var result = JsonSerializer.Deserialize<List<CategoryItem>>(json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        return result ?? new List<CategoryItem>();
    }

    public async Task<List<CategoryItem>> GetCountries()
    {
        var response = await _httpClient.GetAsync("quoc-gia");

        if (!response.IsSuccessStatusCode)
            return new List<CategoryItem>();

        var json = await response.Content.ReadAsStringAsync();

        var result = JsonSerializer.Deserialize<List<CategoryItem>>(json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        return result ?? new List<CategoryItem>();
    }

}


