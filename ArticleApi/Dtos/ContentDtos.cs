using System.ComponentModel.DataAnnotations;
using ArticleApi.Entities;

namespace ArticleApi.Dtos;

public record ContentDto(
    int Id,
    string Title,
    string Body,
    int AuthorId,
    string AuthorName,
    Status Status,
    Language Language,
    DateTime CreatedAt,
    int? ArticleId);

public record CreateContentRequest(
    [Required] string Title,
    [Required] string Body,
    [Required] int? AuthorId,
    [Required] Status? Status,
    [Required] Language? Language,
    int? ArticleId);

public record UpdateContentRequest(
    [Required] string Title,
    [Required] string Body,
    [Required] int? AuthorId,
    [Required] Status? Status,
    [Required] Language? Language,
    int? ArticleId);
