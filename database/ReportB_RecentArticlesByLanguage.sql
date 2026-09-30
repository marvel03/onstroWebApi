-- Report B: articles created in the last @ArticleMonths months that have content in @Language
-- written by a user whose account was created in the last @UserMonths months.
-- The language and the recent author must be on the same content.
-- Dates are stored in UTC, so "now" is taken in UTC too.

DECLARE @Language      nvarchar(20) = N'English';   -- English, French or Spanish
DECLARE @ArticleMonths int          = 3;
DECLARE @UserMonths    int          = 4;

IF @Language NOT IN (N'English', N'French', N'Spanish')
    THROW 50000, 'Language must be English, French or Spanish.', 1;

DECLARE @Now           datetime2 = SYSUTCDATETIME();
DECLARE @ArticlesSince datetime2 = DATEADD(MONTH, -@ArticleMonths, @Now);
DECLARE @UsersSince    datetime2 = DATEADD(MONTH, -@UserMonths, @Now);

SELECT
    a.Id        AS ArticleId,
    a.Status    AS ArticleStatus,
    a.CreatedAt AS ArticleCreatedAt,
    m.Title,
    m.Author,
    m.AuthorCreatedAt
FROM Articles a
CROSS APPLY (
    SELECT TOP (1) c.Title, u.Username AS Author, u.CreatedAt AS AuthorCreatedAt
    FROM Contents c
    JOIN Users u ON u.Id = c.Author
    WHERE c.ArticleId = a.Id
      AND c.Language  = @Language
      AND u.CreatedAt >= @UsersSince
    ORDER BY c.CreatedAt, c.Id
) AS m
WHERE a.CreatedAt >= @ArticlesSince
ORDER BY a.CreatedAt DESC, a.Id;
