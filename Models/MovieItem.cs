public class MovieItem
{
    public string _id { get; set; }
    public string name { get; set; }
    public string slug { get; set; }
    public string thumb_url { get; set; }
    public string poster_url { get; set; }
    public int year { get; set; }

    public List<Category> category { get; set; }
    public List<Country> country { get; set; }
}

public class Category
{
    public string name { get; set; }
    public string slug { get; set; }
}

public class Country
{
    public string name { get; set; }
    public string slug { get; set; }
}
