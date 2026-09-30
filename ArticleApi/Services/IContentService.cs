using ArticleApi.Dtos;

namespace ArticleApi.Services;

public interface IContentService
{
    Task<ContentDto?> GetByIdAsync(int id);
    Task<(ContentDto? Content, string? Error)> CreateAsync(CreateContentRequest request);
    Task<(bool Found, string? Error)> UpdateAsync(int id, UpdateContentRequest request);
    Task<bool> DeleteAsync(int id);
}
