using System.ComponentModel.DataAnnotations;
using ArticleApi.Entities;

namespace ArticleApi.Dtos;

public enum ArticleSortBy { CreatedAt, Title }

public enum SortOrder { Asc, Desc }

public class ArticleListQuery
{
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 50)]
    public int PageSize { get; set; } = 10;

    [EnumDataType(typeof(Status))]
    public Status? Status { get; set; }

    [EnumDataType(typeof(ArticleSortBy))]
    public ArticleSortBy SortBy { get; set; } = ArticleSortBy.CreatedAt;

    [EnumDataType(typeof(SortOrder))]
    public SortOrder? SortOrder { get; set; }
}

public record ArticleListItemDto(int Id, string? Title, string? Author, Status Status);
