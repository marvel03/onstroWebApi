using ArticleApi.Data;
using ArticleApi.Dtos;
using ArticleApi.Entities;
using Microsoft.EntityFrameworkCore;

namespace ArticleApi.Services;

public class ContentService : IContentService
{
    private readonly AppDbContext _db;

    public ContentService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ContentDto?> GetByIdAsync(int id)
    {
        return await _db.Contents
            .Where(c => c.Id == id)
            .Select(c => new ContentDto(
                c.Id, c.Title, c.Body, c.AuthorId, c.Author.Username,
                c.Status, c.Language, c.CreatedAt, c.ArticleId))
            .FirstOrDefaultAsync();
    }

    public async Task<(ContentDto? Content, string? Error)> CreateAsync(CreateContentRequest request)
    {
        var error = await CheckReferencesAsync(request.AuthorId!.Value, request.ArticleId);
        if (error is not null)
            return (null, error);

        var content = new Content
        {
            Title = request.Title,
            Body = request.Body,
            AuthorId = request.AuthorId.Value,
            Status = request.Status!.Value,
            Language = request.Language!.Value,
            ArticleId = request.ArticleId,
            CreatedAt = DateTime.UtcNow
        };

        _db.Contents.Add(content);
        await _db.SaveChangesAsync();

        return (await GetByIdAsync(content.Id), null);
    }

    public async Task<(bool Found, string? Error)> UpdateAsync(int id, UpdateContentRequest request)
    {
        var content = await _db.Contents.FindAsync(id);
        if (content is null)
            return (false, null);

        var error = await CheckReferencesAsync(request.AuthorId!.Value, request.ArticleId);
        if (error is not null)
            return (true, error);

        content.Title = request.Title;
        content.Body = request.Body;
        content.AuthorId = request.AuthorId.Value;
        content.Status = request.Status!.Value;
        content.Language = request.Language!.Value;
        content.ArticleId = request.ArticleId;

        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var content = await _db.Contents.FindAsync(id);
        if (content is null)
            return false;

        _db.Contents.Remove(content);
        await _db.SaveChangesAsync();
        return true;
    }

    // Returns an error message if the author or article doesn't exist, otherwise null
    private async Task<string?> CheckReferencesAsync(int authorId, int? articleId)
    {
        if (!await _db.Users.AnyAsync(u => u.Id == authorId))
            return $"Author {authorId} does not exist.";

        if (articleId is not null && !await _db.Articles.AnyAsync(a => a.Id == articleId))
            return $"Article {articleId} does not exist.";

        return null;
    }
}
