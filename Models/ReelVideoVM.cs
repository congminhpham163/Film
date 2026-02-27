namespace MovieWeb.Models;

public class ReelVideoVM
{
    public string VideoUrl { get; set; }

    public string ActorImage { get; set; }
    public string ActorName { get; set; }
    public int? ActorId { get; set; }

    public string MovieName { get; set; }
    public int? MovieId { get; set; }

    public string MovieSlug { get; set; }

    public string PosterUrl { get; set; }
}

public class ReelInfo
{
    public string ActorName { get; set; }
    public string MovieName { get; set; }
    public string MovieSlug { get; set; }
}