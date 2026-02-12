using System.Text.Json;

public class SearchResponse
{
    public JsonElement status { get; set; }
    public string msg { get; set; }
    public SearchData data { get; set; }
}

public class SearchData
{
    public List<MovieItem> items { get; set; }
}
