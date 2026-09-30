-- Report A: every author, with a JSON array of the Ids of the articles they wrote.
-- An author wrote an article if they wrote at least one of its contents.
-- Authors with no articles appear with [].
-- SQL Server 2016 has no STRING_AGG, so the list is built with FOR XML PATH + STUFF.

SELECT
    u.Id AS AuthorId,
    u.Username AS Author,
    '[' + ISNULL(STUFF((
        SELECT ',' + CAST(ids.ArticleId AS varchar(10))
        FROM (
            SELECT DISTINCT c.ArticleId
            FROM Contents c
            WHERE c.Author = u.Id
              AND c.ArticleId IS NOT NULL
        ) AS ids
        ORDER BY ids.ArticleId
        FOR XML PATH('')
    ), 1, 1, ''), '') + ']' AS ArticleIds
FROM Users u
ORDER BY u.Id;
