public class MovieDetailResponse
{
    public MovieDetail movie { get; set; }
    public List<EpisodeData> episodes { get; set; }
}

public class MovieDetail
{
    public string name { get; set; }
    public string slug { get; set; }
    public string origin_name { get; set; }
    public string content { get; set; }
    public string thumb_url { get; set; }
    public string poster_url { get; set; }
    public string trailer_url { get; set; }
    public int year { get; set; }
    public string status { get; set; }
    public List<Category> category { get; set; }
    public List<Country> country { get; set; }
    public List<string> actor { get; set; }
    public List<string> director { get; set; }
    public bool chieu_rap { get; set; }
    public string type { get; set; }
}

public class EpisodeData
{
    public string server_name { get; set; }
    public List<EpisodeItem> server_data { get; set; }
}

public class EpisodeItem
{
    public string name { get; set; }
    public string link_embed { get; set; }
    public string link_m3u8 { get; set; }
}
