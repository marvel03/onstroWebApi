using ArticleApi.Dtos;
using ArticleApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace ArticleApi.Controllers;

[ApiController]
[Route("api/contents")]
public class ContentsController : ControllerBase
{
    private readonly IContentService _contents;

    public ContentsController(IContentService contents)
    {
        _contents = contents;
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ContentDto>> GetById(int id)
    {
        var content = await _contents.GetByIdAsync(id);
        return content is null ? NotFound() : Ok(content);
    }

    [HttpPost]
    public async Task<ActionResult<ContentDto>> Create(CreateContentRequest request)
    {
        var (content, error) = await _contents.CreateAsync(request);
        if (error is not null)
            return Problem(detail: error, statusCode: StatusCodes.Status400BadRequest);

        return CreatedAtAction(nameof(GetById), new { id = content!.Id }, content);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateContentRequest request)
    {
        var (found, error) = await _contents.UpdateAsync(id, request);
        if (!found)
            return NotFound();
        if (error is not null)
            return Problem(detail: error, statusCode: StatusCodes.Status400BadRequest);

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        return await _contents.DeleteAsync(id) ? NoContent() : NotFound();
    }
}
