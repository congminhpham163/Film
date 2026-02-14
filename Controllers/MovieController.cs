using Microsoft.AspNetCore.Mvc;

public class MovieController : Controller
{
    private readonly FilmService _filmService;

    public MovieController(FilmService filmService)
    {
        _filmService = filmService;
    }

    public async Task<IActionResult> Index(
    int page = 1,
    string? keyword = null,
    string? category = null,
    string? country = null,
    string? year = null)
    {
        MovieResponse? result = null;

        ViewBag.Category = category;
        ViewBag.Country = country;
        ViewBag.Year = year;

        if (!string.IsNullOrEmpty(keyword))
        {
            result = await _filmService.SearchMovies(keyword, page);
            ViewBag.Keyword = keyword;
        }
        else
        {
            if (!string.IsNullOrEmpty(category) ||
                !string.IsNullOrEmpty(country) ||
                !string.IsNullOrEmpty(year))
            {
                result = await _filmService.GetMoviesWithFilter(page, category, country, year);
            }
            else
            {
                result = await _filmService.GetMoviesByPage(page);
            }
        }

        ViewBag.CurrentPage = result?.pagination?.currentPage ?? 1;
        ViewBag.TotalPages = result?.pagination?.totalPages ?? 1;

        ViewBag.Categories = await _filmService.GetCategories();
        ViewBag.Countries = await _filmService.GetCountries();
        ViewBag.Years = Enumerable.Range(2000, DateTime.Now.Year - 1999)    
                                .Reverse()
                                .ToList();


        // ⚠ QUAN TRỌNG: luôn trả List<MovieItem>
        return View(result?.items ?? new List<MovieItem>());
    }



    public async Task<IActionResult> Detail(string id)
    {
        var movie = await _filmService.GetMovieDetail(id);

        if (movie == null)
            return NotFound();

        ViewBag.MovieId = id;

        // Fetch related movies from same category
        if (movie.movie.category != null && movie.movie.category.Any())
        {
            var firstCategory = movie.movie.category.First().slug;
            var relatedMoviesResult = await _filmService.GetMoviesWithFilter(1, firstCategory, null, null);
            
            // Filter out current movie and take first 12
            var relatedMovies = relatedMoviesResult?.items?
                .Where(m => m.slug != id)
                .Take(12)
                .ToList() ?? new List<MovieItem>();
            
            ViewBag.RelatedMovies = relatedMovies;
        }
        else
        {
            ViewBag.RelatedMovies = new List<MovieItem>();
        }

        return View(movie);
    }
    
    public IActionResult MyList()
    {
        return View();
    }

}
