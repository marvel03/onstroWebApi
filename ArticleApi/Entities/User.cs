namespace ArticleApi.Entities;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public ICollection<Content> Contents { get; set; } = new List<Content>();
}
