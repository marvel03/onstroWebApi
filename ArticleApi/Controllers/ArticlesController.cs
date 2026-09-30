using ArticleApi.Dtos;
using ArticleApi.Services;
using Microsoft.AspNetCore.Mvc;
using ArticleApi.Entities;
namespace ArticleApi.Controllers;

[ApiController]
[Route("api/articles")]
public class ArticlesController : ControllerBase
{
    private readonly IArticleService _articles;

    public ArticlesController(IArticleService articles)
    {
        _articles = articles;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ArticleListItemDto>>> GetPage([FromQuery] ArticleListQuery query)
    {
        // Enum values must be exactly one name (?status=Published). This also rejects numbers
        // (?status=1) and comma lists (?status=Draft,Published), which .NET would otherwise combine.
        if (!IsEnumName<Status>("status") || !IsEnumName<ArticleSortBy>("sortBy") || !IsEnumName<SortOrder>("sortOrder"))
        {
            return Problem(detail: "status, sortBy and sortOrder must each be a single name, for example status=Published.",
                           statusCode: StatusCodes.Status400BadRequest);
        }

        return Ok(await _articles.GetPageAsync(query));
    }
    // True if the parameter is missing, or exactly one of the enum's names (ignoring case)
    private bool IsEnumName<TEnum>(string key) where TEnum : struct, Enum
    {
        var raw = Request.Query[key].ToString().Trim();
        return raw.Length == 0 || Enum.GetNames<TEnum>().Contains(raw, StringComparer.OrdinalIgnoreCase);
    }
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ArticleDetailsDto>> GetById(int id)
    {
        var article = await _articles.GetByIdAsync(id);
        return article is null ? NotFound() : Ok(article);
    }

    [HttpPost]
    public async Task<ActionResult<ArticleDetailsDto>> Create(CreateArticleRequest request)
    {
        var article = await _articles.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = article.Id }, article);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateArticleRequest request)
    {
        return await _articles.UpdateAsync(id, request) ? NoContent() : NotFound();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        return await _articles.DeleteAsync(id) ? NoContent() : NotFound();
    }
}
