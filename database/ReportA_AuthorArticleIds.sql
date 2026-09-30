-- Report A: every author, with a JSON array of the Ids of the articles they wrote.
-- An author wrote an article if they wrote at least one of its contents.
-- Authors with no articles appear with [].
-- Needs SQL Server 2017 or later (STRING_AGG). The ids are cast to varchar(max) so the result is
-- varchar(max): with a shorter type, STRING_AGG fails once one author's list passes 8000 bytes.
SELECT
    u.Id       AS AuthorId,
    u.Username AS Author,
    '[' + ISNULL(
            STRING_AGG(CAST(p.ArticleId AS varchar(max)), ',')
                WITHIN GROUP (ORDER BY p.ArticleId),
          '') + ']' AS ArticleIds
FROM Users u
LEFT JOIN (
    SELECT DISTINCT Author, ArticleId
    FROM Contents
    WHERE ArticleId IS NOT NULL
) AS p ON p.Author = u.Id
GROUP BY u.Id, u.Username
ORDER BY u.Id;
