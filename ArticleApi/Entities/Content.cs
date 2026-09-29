namespace ArticleApi.Entities;

public class Content
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;   // maps to column "Content"
    public int AuthorId { get; set; }                   // maps to column "Author"
    public User Author { get; set; } = null!;
    public Status Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public Language Language { get; set; }
    public int? ArticleId { get; set; }
    public Article? Article { get; set; }
}
