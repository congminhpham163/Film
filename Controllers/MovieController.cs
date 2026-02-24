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
    string? year = null,
    string? type = null,
    string? quality = null,
    string? lang = null,
    bool showAll = false)
    {
        MovieResponse? result = null;

        ViewBag.Category = category;
        ViewBag.Country = country;
        ViewBag.Year = year;
        ViewBag.Type = type;
        ViewBag.Quality = quality;
        ViewBag.Lang = lang;

        // Hoạt Hình là type trong API, không phải category - chuyển đổi sang đúng param
        if (category == "hoathinh")
        {
            type = "hoathinh";
            category = null;
            ViewBag.Category = "hoathinh"; // giữ để dropdown hiện đúng trạng thái selected
            ViewBag.Type = null;            // đừng để type dropdown cũng selected
        }

        bool isHomePage = !showAll &&
                          string.IsNullOrEmpty(keyword) && 
                          string.IsNullOrEmpty(category) && 
                          string.IsNullOrEmpty(country) && 
                          string.IsNullOrEmpty(year) &&
                          string.IsNullOrEmpty(quality) &&
                          string.IsNullOrEmpty(lang) &&
                          type != "hoathinh" &&
                          page == 1;
        
        ViewBag.IsHomePage = isHomePage;

        if (!string.IsNullOrEmpty(keyword))
        {
            result = await _filmService.SearchMovies(keyword, page);
            ViewBag.Keyword = keyword;
        }
        else
        {
            bool hasFilter = !string.IsNullOrEmpty(category) ||
                             !string.IsNullOrEmpty(country) ||
                             !string.IsNullOrEmpty(year) ||
                             !string.IsNullOrEmpty(type) ||
                             !string.IsNullOrEmpty(quality) ||
                             !string.IsNullOrEmpty(lang);

            if (hasFilter)
            {
                result = await _filmService.GetMoviesWithFilter(page, category, country, year, type, quality, lang);
            }
            else
            {
                result = await _filmService.GetMoviesByPage(page);
                
                if (isHomePage)
                {
                    ViewBag.LatestMovies = result?.items ?? new List<MovieItem>();
                    ViewBag.ActionMovies = await _filmService.GetActionMovies();
                    ViewBag.HorrorMovies = await _filmService.GetHorrorMovies();
                    ViewBag.AnimationMovies = await _filmService.GetAnimationMovies();
                }
            }
        }

        ViewBag.CurrentPage = result?.pagination?.currentPage ?? 1;
        ViewBag.TotalPages = result?.pagination?.totalPages ?? 1;

        var categories = await _filmService.GetCategories();
        ViewBag.Categories = categories.OrderBy(c => c.name).ToList();
        
        ViewBag.Countries = await _filmService.GetCountries();
        ViewBag.Years = Enumerable.Range(2000, DateTime.Now.Year - 1999)    
                                .Reverse()
                                .ToList();

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
