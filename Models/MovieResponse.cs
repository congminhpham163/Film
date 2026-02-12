using System.Text.Json;
public class MovieResponse
{
    public JsonElement status { get; set; }
    public string msg { get; set; }
    public List<MovieItem> items { get; set; }
    public Pagination pagination { get; set; }
}
public class Pagination
{
    public int currentPage { get; set; }
    public int totalPages { get; set; }
}