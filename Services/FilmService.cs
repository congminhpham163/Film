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

        // Fetch 2 API pages to get ~30 movies (each API page has ~24 movies)
        // UI page 1 → API pages 1-2
        // UI page 2 → API pages 3-4
        // UI page 3 → API pages 5-6
        int startApiPage = (page - 1) * 2 + 1;
        
        var task1 = _httpClient.GetAsync($"danh-sach/phim-moi-cap-nhat?page={startApiPage}");
        var task2 = _httpClient.GetAsync($"danh-sach/phim-moi-cap-nhat?page={startApiPage + 1}");

        await Task.WhenAll(task1, task2);

        var response1 = await task1;
        var response2 = await task2;

        if (!response1.IsSuccessStatusCode)
            return null;

        var json1 = await response1.Content.ReadAsStringAsync();
        var result1 = JsonSerializer.Deserialize<MovieResponse>(json1,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (result1 == null)
            return null;

        // Merge second page if available
        if (response2.IsSuccessStatusCode)
        {
            var json2 = await response2.Content.ReadAsStringAsync();
            var result2 = JsonSerializer.Deserialize<MovieResponse>(json2,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result2?.items != null)
            {
                result1.items.AddRange(result2.items);
            }
        }

        // Recalculate pagination: divide total pages by 2
        if (result1.pagination != null)
        {
            result1.pagination.currentPage = page;
            result1.pagination.totalPages = (int)Math.Ceiling((double)result1.pagination.totalPages / 2);
        }

        _cache.Set(cacheKey, result1, TimeSpan.FromMinutes(10));

        return result1;
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

    public async Task<List<MovieItem>> GetActionMovies()
    {
        var response = await _httpClient.GetAsync("v1/api/the-loai/hanh-dong?page=1");

        if (!response.IsSuccessStatusCode)
            return new List<MovieItem>();

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<V1MovieResponse>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return result?.data?.items ?? new List<MovieItem>();
    }

    public async Task<List<MovieItem>> GetHorrorMovies()
    {
        var response = await _httpClient.GetAsync("v1/api/the-loai/kinh-di?page=1");

        if (!response.IsSuccessStatusCode)
            return new List<MovieItem>();

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<V1MovieResponse>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return result?.data?.items ?? new List<MovieItem>();
    }

    public async Task<List<MovieItem>> GetAnimationMovies()
    {
        var response = await _httpClient.GetAsync("v1/api/danh-sach/hoat-hinh?page=1");

        if (!response.IsSuccessStatusCode)
            return new List<MovieItem>();

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<V1MovieResponse>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return result?.data?.items ?? new List<MovieItem>();
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
    string? year,
    string? type = null,
    string? quality = null,
    string? lang = null)
    {
        int batchSize = 6; 
        int startApiPage = (page - 1) * batchSize + 1;
        
        var tasks = new List<Task<V1MovieResponse?>>();

        for (int i = 0; i < batchSize; i++)
        {
            int currentApiPage = startApiPage + i;
            tasks.Add(FetchSinglePage(currentApiPage, category, country, year, type, quality, lang));
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

        int newTotalPages = (int)Math.Ceiling((double)lastPagination.totalPages / batchSize);

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

    private async Task<V1MovieResponse?> FetchSinglePage(
        int page, string? category, string? country, string? year,
        string? type = null, string? quality = null, string? lang = null)
    {
        var queryParams = new List<string>();
        queryParams.Add($"page={page}");

        if (!string.IsNullOrEmpty(category))
            queryParams.Add($"category={category}");

        if (!string.IsNullOrEmpty(country))
            queryParams.Add($"country={country}");

        if (!string.IsNullOrEmpty(year))
            queryParams.Add($"year={year}");

        if (!string.IsNullOrEmpty(type))
            queryParams.Add($"type={type}");

        if (!string.IsNullOrEmpty(quality))
            queryParams.Add($"quality={quality}");

        if (!string.IsNullOrEmpty(lang))
            queryParams.Add($"lang={lang}");

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


