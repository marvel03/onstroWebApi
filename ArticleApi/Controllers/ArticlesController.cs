using ArticleApi.Dtos;
using ArticleApi.Services;
using Microsoft.AspNetCore.Mvc;

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
        return Ok(await _articles.GetPageAsync(query));
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
