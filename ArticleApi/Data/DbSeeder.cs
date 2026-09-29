using ArticleApi.Entities;

namespace ArticleApi.Data;

public static class DbSeeder
{
    public static void Seed(AppDbContext db)
    {
        // Only seed an empty database, so data added through the API is never touched
        if (db.Users.Any())
            return;

        // Dates are relative to today, so Report B's time windows always have matches
        var now = DateTime.UtcNow;

        // Users: added directly to the database, since the API has no user endpoints
        var alice = new User { Username = "alice", CreatedAt = now.AddMonths(-2) };
        var bruno = new User { Username = "bruno", CreatedAt = now.AddMonths(-1) };
        var carmen = new User { Username = "carmen", CreatedAt = now.AddMonths(-8) };   // outside Report B's 4 months
        var dmitri = new User { Username = "dmitri", CreatedAt = now.AddDays(-10) };    // writes nothing

        // All three languages: the English title should win the title sort
        var guide = new Article { Status = Status.Published, CreatedAt = now.AddDays(-20) };
        guide.Contents.Add(NewContent("Getting started with EF Core", Language.English, Status.Published, alice, now.AddDays(-20)));
        guide.Contents.Add(NewContent("Débuter avec EF Core", Language.French, Status.Draft, bruno, now.AddDays(-18)));
        guide.Contents.Add(NewContent("Primeros pasos con EF Core", Language.Spanish, Status.Published, alice, now.AddDays(-15)));

        // No English content: the title has to fall back to another language
        var recipes = new Article { Status = Status.Draft, CreatedAt = now.AddMonths(-1) };
        recipes.Contents.Add(NewContent("Recettes du monde", Language.French, Status.Draft, bruno, now.AddMonths(-1)));
        recipes.Contents.Add(NewContent("Recetas del mundo", Language.Spanish, Status.Draft, alice, now.AddMonths(-1)));

        // Older than 3 months: Report B must leave it out
        var archive = new Article { Status = Status.Unpublished, CreatedAt = now.AddMonths(-5) };
        archive.Contents.Add(NewContent("Archived announcement", Language.English, Status.Unpublished, carmen, now.AddMonths(-5)));

        // Recent article, but its author's account is older than 4 months: Report B must leave it out
        var interview = new Article { Status = Status.Published, CreatedAt = now.AddMonths(-2) };
        interview.Contents.Add(NewContent("Interview with a veteran author", Language.English, Status.Published, carmen, now.AddMonths(-2)));

        // An article with no content at all
        var empty = new Article { Status = Status.Draft, CreatedAt = now.AddDays(-10) };

        // Content that belongs to no article (allowed by the diagram's 0..1)
        var standalone = NewContent("Standalone note", Language.English, Status.Draft, alice, now.AddDays(-5));

        db.Users.AddRange(alice, bruno, carmen, dmitri);
        db.Articles.AddRange(guide, recipes, archive, interview, empty);
        db.Contents.Add(standalone);
        db.SaveChanges();
    }

    private static Content NewContent(string title, Language language, Status status, User author, DateTime createdAt) =>
        new()
        {
            Title = title,
            Body = $"Sample {language} text for \"{title}\".",
            Language = language,
            Status = status,
            Author = author,
            CreatedAt = createdAt
        };
}
