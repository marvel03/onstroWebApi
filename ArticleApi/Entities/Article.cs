namespace ArticleApi.Entities;

public class Article
{
    public int Id { get; set; }
    public Status Status { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<Content> Contents { get; set; } = new List<Content>();
}