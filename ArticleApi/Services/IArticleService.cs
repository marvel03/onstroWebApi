using ArticleApi.Dtos;

namespace ArticleApi.Services;

public interface IArticleService
{
    Task<ArticleDetailsDto?> GetByIdAsync(int id);
    Task<PagedResult<ArticleListItemDto>> GetPageAsync(ArticleListQuery query);
    Task<ArticleDetailsDto> CreateAsync(CreateArticleRequest request);
    Task<bool> UpdateAsync(int id, UpdateArticleRequest request);
    Task<bool> DeleteAsync(int id);
}
