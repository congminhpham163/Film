public class AIRecommendResponse
{
    public bool success { get; set; }

    public string message { get; set; }

    public List<MovieItem> movies { get; set; }
}