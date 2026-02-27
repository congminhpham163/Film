using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using MovieWeb.Models;

namespace MovieWeb.Controllers
{
    public class ReelController : Controller
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly FilmService _filmService;
        private const string API_KEY = "c2957d98f14f4f508ecd995ec7c66b0e";

        public ReelController(IHttpClientFactory httpFactory, FilmService filmService)
        {
            _httpFactory = httpFactory;
            _filmService = filmService;
        }

        public async Task<IActionResult> ThuocPhim()
        {
            var folder = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot/videos"
            );

            var files = Directory.GetFiles(folder, "*.mp4");

            var http = _httpFactory.CreateClient("tmdb");

            var list = new List<ReelVideoVM>();

            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);

                string actorImage = null;
                string actorName = null;
                string movieName = null;
                string posterUrl = null;

                // ✅ dùng ReelMap
                if (ReelMap.TryGetValue(fileName, out var info))
                {
                    actorName = info.ActorName;
                    movieName = info.MovieName;

                    // ========================
                    // ACTOR IMAGE (TMDB)
                    // ========================
                    var res = await http.GetAsync(
                        $"search/person?api_key={API_KEY}&query={Uri.EscapeDataString(actorName)}"
                    );

                    if (res.IsSuccessStatusCode)
                    {
                        dynamic data =
                            JsonConvert.DeserializeObject(await res.Content.ReadAsStringAsync());

                        if (data.results.Count > 0 &&
                            data.results[0].profile_path != null)
                        {
                            actorImage =
                                "https://image.tmdb.org/t/p/w185" +
                                data.results[0].profile_path;
                        }
                    }

                    // ========================
                    // ⭐ MOVIE POSTER (OPHIM)
                    // ========================
                    if (!string.IsNullOrEmpty(info.MovieSlug))
                    {
                        var movieDetail =
                            await _filmService.GetMovieDetail(info.MovieSlug);

                        if (movieDetail != null)
                        {
                            posterUrl =
                                movieDetail.movie.poster_url;
                        }
                    }
                }
                
                list.Add(new ReelVideoVM
                {
                    VideoUrl = "/videos/" + fileName,
                    ActorImage = actorImage,
                    ActorName = actorName,
                    MovieName = movieName,
                    MovieSlug = info?.MovieSlug,
                    PosterUrl = posterUrl
                });
            }

            return View(list);
        }

        private static Dictionary<string, ReelInfo> ReelMap =
            new Dictionary<string, ReelInfo>()
        {
            ["fixed_bachloc.mp4"] = new ReelInfo
            {
                ActorName = "Bạch Lộc"
            },

            ["fixed_kimjiwon.mp4"] = new ReelInfo
            {
                ActorName = "Kim Ji Won",
                MovieName = "Nữ Hoàng Nước Mắt",
                MovieSlug = "nu-hoang-nuoc-mat"
            },

            ["fixed_trieulotu.mp4"] = new ReelInfo
            {
                ActorName = "Triệu Lộ Tư"
            },

            ["fixed_dichlenhatba.mp4"] = new ReelInfo
            {
                ActorName = "Địch Lệ Nhiệt Ba"
            },

            ["fixed_goyounjung.mp4"] = new ReelInfo
            {
                ActorName = "Go Youn Jung",
                MovieName = "Tiếng Yêu Này Anh Dịch Được Không?",
                MovieSlug = "tieng-yeu-nay-anh-dich-duoc-khong"
            }
        };
        
    }
}