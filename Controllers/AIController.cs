using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using WebMovie.Models;

namespace WebMovie.Controllers
{
    [Route("AI")]
    public class AIController : Controller
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _config;
        
        public AIController(HttpClient http,IConfiguration config)
        {
            _http = http;
            _config = config;
        }

        [HttpGet("Chatbot")]
        public IActionResult Chatbot()
        {
            return View();
        }

        [HttpPost("Recommend")]
        public async Task<IActionResult> Recommend(
            [FromBody] ChatRequest request)
        {
            try
            {
                var apiKey = _config["OpenRouter:ApiKey"];

                var body = new
                {
                    model = "deepseek/deepseek-chat",

                    messages = new[]
                    {
                        new
                        {
                            role = "system",

                            content = @"
You are an expert cinematic recommendation AI.

Your task:
- recommend movies with the SAME vibe
- same mood
- same atmosphere
- same storytelling style

Rules:
- highly relevant
- avoid random popular movies
- return ONLY movie titles
- one movie per line
- no numbering
- no explanation
- recommend 10 movies
"
                        },

                        new
                        {
                            role = "user",
                            content = request.Message
                        }
                    }
                };

                var json =
                    JsonSerializer.Serialize(body);

                var requestMessage =
                    new HttpRequestMessage(
                        HttpMethod.Post,
                        "https://openrouter.ai/api/v1/chat/completions"
                    );

                requestMessage.Headers.Add(
                    "Authorization",
                    $"Bearer {apiKey}"
                );

                requestMessage.Headers.Add(
                    "HTTP-Referer",
                    "http://localhost:5000"
                );

                requestMessage.Headers.Add(
                    "X-Title",
                    "Cine AI"
                );

                requestMessage.Content =
                    new StringContent(
                        json,
                        Encoding.UTF8,
                        "application/json"
                    );

                var response =
                    await _http.SendAsync(requestMessage);

                if (!response.IsSuccessStatusCode)
                {
                    var error =
                        await response.Content
                            .ReadAsStringAsync();

                    return Json(new
                    {
                        success = false,
                        message = error
                    });
                }

                var result =
                    await response.Content
                        .ReadAsStringAsync();

                using JsonDocument doc =
                    JsonDocument.Parse(result);

                var text =
                    doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                if (string.IsNullOrWhiteSpace(text))
                {
                    return Json(new
                    {
                        success = false,
                        message = "AI không phản hồi."
                    });
                }

                var movieNames = text

                    .Split('\n',
                        StringSplitOptions.RemoveEmptyEntries)

                    .Select(x => x.Trim())

                    // remove numbering
                    .Select(x => x.TrimStart(
                        '0','1','2','3','4',
                        '5','6','7','8','9',
                        '.','-',' ')
                    )

                    // remove year
                    .Select(x =>
                    {
                        var index = x.IndexOf('(');

                        return index > 0
                            ? x[..index].Trim()
                            : x;
                    })

                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x))

                    .Distinct()

                    .Take(10)

                    .ToList();

                var tasks =
                    movieNames.Select(SearchMovie);

                var results =
                    await Task.WhenAll(tasks);

                var movies = results

                    .Where(x => x != null)

                    .Cast<MovieItem>()

                    .GroupBy(x => x.slug)

                    .Select(x => x.First())

                    .Take(10)

                    .ToList();

                var replies = new[]
                {
                    "Đây là những bộ phim phù hợp với mô tả của bạn 🎬",

                    "Mình nghĩ bạn sẽ thích các phim này 🍿",

                    "Các phim dưới đây khá giống vibe bạn muốn 👇",

                    "Danh sách phim dành cho bạn đây 😎",

                    "Mấy phim này khá hợp gu bạn đó 🎥"
                };

                var randomReply =
                    replies[new Random()
                        .Next(replies.Length)];

                return Json(new
                {
                    success = true,

                    reply = randomReply,

                    movies = movies
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        public class ChatRequest
        {
            public string Message { get; set; }
        }

        private async Task<MovieItem?> SearchMovie(
            string keyword)
        {
            try
            {
                var url =
                    $"https://ophim1.com/v1/api/tim-kiem?keyword={Uri.EscapeDataString(keyword)}";

                var json =
                    await _http.GetStringAsync(url);

                using JsonDocument doc =
                    JsonDocument.Parse(json);

                var items =
                    doc.RootElement
                    .GetProperty("data")
                    .GetProperty("items");

                if (items.GetArrayLength() == 0)
                    return null;

                foreach (var item in items.EnumerateArray())
                {
                    var movie =
                        JsonSerializer.Deserialize<MovieItem>(
                            item.GetRawText()
                        );

                    if (movie != null
                        && !string.IsNullOrWhiteSpace(movie.poster_url))
                    {
                        return movie;
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}