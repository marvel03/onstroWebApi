using System.ComponentModel.DataAnnotations;
using ArticleApi.Entities;

namespace ArticleApi.Dtos;

public record CreateArticleRequest([Required] Status? Status);

public record UpdateArticleRequest([Required] Status? Status);

public record ArticleDetailsDto(
    int Id,
    Status Status,
    DateTime CreatedAt,
    IReadOnlyList<ContentDto> Contents);
