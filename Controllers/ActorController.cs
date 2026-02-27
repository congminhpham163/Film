using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

public class ActorController : Controller
{
    private readonly IHttpClientFactory _httpFactory;
    private const string API_KEY = "c2957d98f14f4f508ecd995ec7c66b0e";

    public ActorController(IHttpClientFactory httpFactory)
    {
        _httpFactory = httpFactory;
    }

    public async Task<IActionResult> Actor(string id)
    {
        if (string.IsNullOrEmpty(id))
            return NotFound();

        var actorName = id.Replace("-", " ");
        var http = _httpFactory.CreateClient("tmdb");

        try
        {
            // =============================
            // 1. SEARCH ACTOR
            // =============================
            var searchRes = await http.GetAsync(
                $"search/person?api_key={API_KEY}&query={Uri.EscapeDataString(actorName)}&language=vi-VN"
            );

            if (!searchRes.IsSuccessStatusCode)
                return NotFound();

            dynamic searchData =
                JsonConvert.DeserializeObject(await searchRes.Content.ReadAsStringAsync());

            var results = ((IEnumerable<dynamic>)searchData.results);

            if (!results.Any())
                return NotFound();

            var actor = results
                .OrderByDescending(x => (double)x.popularity)
                .First();

            int actorId = actor.id;

            // =============================
            // 2. ACTOR DETAIL
            // =============================
            var detailRes = await http.GetAsync(
                $"person/{actorId}?api_key={API_KEY}&language=vi-VN"
            );

            if (!detailRes.IsSuccessStatusCode)
                return NotFound();

            dynamic actorDetail =
                JsonConvert.DeserializeObject(await detailRes.Content.ReadAsStringAsync());
                
            // =============================
            // ƯU TIÊN TÊN QUỐC TẾ
            // =============================
            string displayName = actorDetail.name;

            foreach (var aka in actorDetail.also_known_as)
            {
                string n = (string)aka;

                // nếu chứa chữ Latin
                if (!string.IsNullOrEmpty(n) &&
                    n.Any(c => c >= 'A' && c <= 'z'))
                {
                    displayName = n;
                    break;
                }
            }

            // override name
            actorDetail.name = displayName;

            // =============================
            // 3. FULL FILMOGRAPHY ⭐⭐⭐
            // KHÔNG dùng language
            // =============================
            var creditRes = await http.GetAsync(
                $"person/{actorId}/combined_credits?api_key={API_KEY}&language=vi-VN"
            );

            if (!creditRes.IsSuccessStatusCode)
                return NotFound();

            dynamic creditData =
                JsonConvert.DeserializeObject(await creditRes.Content.ReadAsStringAsync());

            var allCredits = ((IEnumerable<dynamic>)creditData.cast);

            // =============================
            // LẤY TẤT CẢ MOVIE + TV
            // =============================
            var allWorks = allCredits
                .Where(x =>
                    x.media_type == "movie" ||
                    x.media_type == "tv")
                .OrderByDescending(x => (double?)x.popularity ?? 0)
                .ToList();

            ViewBag.Actor = actorDetail;
            ViewBag.Works = allWorks;

            return View();
        }
        catch
        {
            return NotFound();
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetActorImage(string name)
    {
        if (string.IsNullOrEmpty(name))
            return Json(null);

        var http = _httpFactory.CreateClient("tmdb");

        var res = await http.GetAsync(
            $"search/person?api_key={API_KEY}&query={Uri.EscapeDataString(name)}"
        );

        if (!res.IsSuccessStatusCode)
            return Json(null);

        dynamic data =
            JsonConvert.DeserializeObject(await res.Content.ReadAsStringAsync());

        var results = ((IEnumerable<dynamic>)data.results);

        var actor = results
            .OrderByDescending(x => (double)x.popularity)
            .FirstOrDefault();

        if (actor == null)
            return Json(null);

        int actorId = actor.id;

        // ⭐ LẤY DETAIL ĐỂ CÓ ALSO_KNOWN_AS
        var detailRes = await http.GetAsync(
            $"person/{actorId}?api_key={API_KEY}"
        );

        if (!detailRes.IsSuccessStatusCode)
            return Json(null);

        dynamic detail =
            JsonConvert.DeserializeObject(await detailRes.Content.ReadAsStringAsync());

        string displayName = detail.name;

        // =============================
        // ƯU TIÊN TÊN LATIN
        // =============================
        foreach (var aka in detail.also_known_as)
        {
            string n = (string)aka;

            // nếu chứa chữ latin
            if (n.Any(c => c >= 'A' && c <= 'z'))
            {
                displayName = n;
                break;
            }
        }

        string image = detail.profile_path != null
            ? "https://image.tmdb.org/t/p/w185" + (string)detail.profile_path
            : "/images/no-actor.png";

        return Json(new
        {
            name = displayName,
            image = image
        });
    }
}