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
        // A second English version, newer and by another author: the list has to pick one English title
        guide.Contents.Add(NewContent("EF Core for beginners", Language.English, Status.Published, bruno, now.AddDays(-12)));

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

        // Bulk data, so paging, sorting and filtering have enough rows to show
        var elena = new User { Username = "elena", CreatedAt = now.AddMonths(-3) };    // inside Report B's 4 months
        var farid = new User { Username = "farid", CreatedAt = now.AddMonths(-12) };   // outside Report B's 4 months
        var generated = GenerateArticles(now, elena, farid);

        db.Users.AddRange(alice, bruno, carmen, dmitri, elena, farid);
        db.Articles.AddRange(guide, recipes, archive, interview, empty);
        db.Articles.AddRange(generated);
        db.Contents.Add(standalone);
        db.SaveChanges();
    }

    // 36 extra articles spread over the last ~11 months, varying status and language mix
    private static List<Article> GenerateArticles(DateTime now, User elena, User farid)
    {
        (string English, string French, string Spanish)[] topics =
        [
            ("Async programming basics", "Les bases de la programmation asynchrone", "Conceptos básicos de programación asíncrona"),
            ("Designing REST APIs", "Concevoir des API REST", "Diseñar API REST"),
            ("Understanding SQL joins", "Comprendre les jointures SQL", "Entender los joins de SQL"),
            ("Dependency injection explained", "L'injection de dépendances expliquée", "La inyección de dependencias explicada"),
            ("Writing unit tests", "Écrire des tests unitaires", "Escribir pruebas unitarias"),
            ("Caching strategies", "Stratégies de mise en cache", "Estrategias de caché"),
            ("Logging in production", "La journalisation en production", "Registro de logs en producción"),
            ("Clean code habits", "Les habitudes du code propre", "Hábitos de código limpio"),
            ("Database indexing", "L'indexation des bases de données", "Indexación de bases de datos"),
            ("Handling errors gracefully", "Gérer les erreurs avec élégance", "Manejar errores con elegancia"),
            ("Version control workflows", "Les flux de travail avec Git", "Flujos de trabajo con Git"),
            ("Securing web applications", "Sécuriser les applications web", "Proteger aplicaciones web")
        ];
        Status[] statuses = [Status.Published, Status.Draft, Status.Unpublished];
        var articles = new List<Article>();

        for (var i = 0; i < 36; i++)
        {
            var created = now.AddDays(-(3 + i * 9));
            var (english, french, spanish) = topics[i % topics.Length];
            var part = $" (part {i / topics.Length + 1})";
            var variant = i + i / topics.Length;   // shifts each round, so repeated topics get different mixes

            // An author can't write before their account existed
            var author = created < elena.CreatedAt ? farid : (i % 2 == 0 ? elena : farid);
            var article = new Article { Status = statuses[variant % 3], CreatedAt = created };

            switch (variant % 4)
            {
                case 0:   // all three languages
                    article.Contents.Add(NewContent(english + part, Language.English, article.Status, author, created));
                    article.Contents.Add(NewContent(french + part, Language.French, Status.Draft, author, created.AddDays(1)));
                    article.Contents.Add(NewContent(spanish + part, Language.Spanish, Status.Draft, author, created.AddDays(2)));
                    break;
                case 1:   // English only
                    article.Contents.Add(NewContent(english + part, Language.English, article.Status, author, created));
                    break;
                case 2:   // no English: the older Spanish version should supply the title
                    article.Contents.Add(NewContent(spanish + part, Language.Spanish, article.Status, author, created));
                    article.Contents.Add(NewContent(french + part, Language.French, article.Status, author, created.AddDays(1)));
                    break;
                default:  // Spanish only
                    article.Contents.Add(NewContent(spanish + part, Language.Spanish, article.Status, author, created));
                    break;
            }

            articles.Add(article);
        }

        return articles;
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
