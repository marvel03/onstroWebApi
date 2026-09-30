-- Report A: every author, with a JSON array of the Ids of the articles they wrote.
-- An author wrote an article if they wrote at least one of its contents.
-- Authors with no articles appear with [].
SELECT
    u.Id       AS AuthorId,
    u.Username AS Author,
    '[' + ISNULL(
            STRING_AGG(CAST(p.ArticleId AS varchar(10)), ',')
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
