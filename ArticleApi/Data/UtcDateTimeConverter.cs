using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ArticleApi.Data;

// datetime2 stores no time zone, so dates read back from the database are marked as UTC
public class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter()
        : base(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
    {
    }
}
