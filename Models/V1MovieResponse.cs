using System.Text.Json;
public class V1MovieResponse
{
    public JsonElement status { get; set; }
    public string msg { get; set; }
    public V1Data data { get; set; }
}

public class V1Data
{
    public List<MovieItem> items { get; set; }
    public V1Params @params { get; set; }
}

public class V1Params
{
    public V1Pagination pagination { get; set; }
}
public class V1Pagination
{
    public int totalItems { get; set; }
    public int totalItemsPerPage { get; set; }
    public int currentPage { get; set; }
    public int pageRanges { get; set; }
    
    // Computed property to match Pagination model
    public int totalPages => totalItemsPerPage > 0 ? (int)Math.Ceiling((double)totalItems / totalItemsPerPage) : 0;
}