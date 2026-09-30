using ArticleApi.Data;
using ArticleApi.Dtos;
using ArticleApi.Entities;
using Microsoft.EntityFrameworkCore;

namespace ArticleApi.Services;

public class ArticleService : IArticleService
{
    private readonly AppDbContext _db;

    public ArticleService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ArticleDetailsDto?> GetByIdAsync(int id)
    {
        return await _db.Articles
            .Where(a => a.Id == id)
            .Select(a => new ArticleDetailsDto(
                a.Id,
                a.Status,
                a.CreatedAt,
                a.Contents
                    .OrderBy(c => c.Language)
                    .ThenBy(c => c.CreatedAt)
                    .ThenBy(c => c.Id)
                    .Select(c => new ContentDto(
                        c.Id, c.Title, c.Body, c.AuthorId, c.Author.Username,
                        c.Status, c.Language, c.CreatedAt, c.ArticleId))
                    .ToList()))
            .FirstOrDefaultAsync();
    }

    public async Task<PagedResult<ArticleListItemDto>> GetPageAsync(ArticleListQuery query)
    {
        // 1. Filter on the article's own status
        var articles = _db.Articles.AsQueryable();
        if (query.Status is not null)
        {
            var status = query.Status.Value;
            articles = articles.Where(a => a.Status == status);
        }

        // 2. The content that represents each article: English first, then the oldest, then the lowest Id
        var rows = articles.Select(a => new
        {
            a.Id,
            a.Status,
            a.CreatedAt,
            Main = a.Contents
                .OrderBy(c => c.Language == Language.English ? 0 : 1)
                .ThenBy(c => c.CreatedAt)
                .ThenBy(c => c.Id)
                .Select(c => new { c.Title, Author = c.Author.Username })
                .FirstOrDefault()
        });

        // 3. Sort. Newest first is the default for CreatedAt, A to Z for Title.
        //    Untitled articles always go last, and Id breaks ties so pages never overlap.
        var descending = query.SortOrder == SortOrder.Desc
            || (query.SortOrder is null && query.SortBy == ArticleSortBy.CreatedAt);

        if (query.SortBy == ArticleSortBy.Title)
        {
            rows = descending
                ? rows.OrderBy(r => r.Main!.Title == null).ThenByDescending(r => r.Main!.Title).ThenBy(r => r.Id)
                : rows.OrderBy(r => r.Main!.Title == null).ThenBy(r => r.Main!.Title).ThenBy(r => r.Id);
        }
        else
        {
            rows = descending
                ? rows.OrderByDescending(r => r.CreatedAt).ThenBy(r => r.Id)
                : rows.OrderBy(r => r.CreatedAt).ThenBy(r => r.Id);
        }

        // 4. Count everything that matched the filter, then fetch only this page
        var totalCount = await rows.CountAsync();

        var items = await rows
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(r => new ArticleListItemDto(r.Id, r.Main!.Title, r.Main!.Author, r.Status))
            .ToListAsync();

        return new PagedResult<ArticleListItemDto>(items, query.Page, query.PageSize, totalCount);
    }

    public async Task<ArticleDetailsDto> CreateAsync(CreateArticleRequest request)
    {
        var article = new Article
        {
            Status = request.Status!.Value,
            CreatedAt = DateTime.UtcNow
        };

        _db.Articles.Add(article);
        await _db.SaveChangesAsync();

        return new ArticleDetailsDto(article.Id, article.Status, article.CreatedAt, []);
    }

    public async Task<bool> UpdateAsync(int id, UpdateArticleRequest request)
    {
        var article = await _db.Articles.FindAsync(id);
        if (article is null)
            return false;

        article.Status = request.Status!.Value;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var article = await _db.Articles.FindAsync(id);
        if (article is null)
            return false;

        _db.Articles.Remove(article);
        await _db.SaveChangesAsync();
        return true;
    }
}
