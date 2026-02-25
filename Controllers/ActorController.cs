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
                $"search/person?api_key={API_KEY}&query={Uri.EscapeDataString(actorName)}"
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
}