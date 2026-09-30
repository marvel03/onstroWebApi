# Article API

A Web API for a small multilingual article system, built with ASP.NET Core (.NET 10), Entity Framework Core 10 and SQL Server LocalDB.

An **article** is a container with a status. Its text lives in **contents**: each content is one language version of the article (English, French or Spanish), with a title, a body and an author. **Users** are the authors.

## Requirements

- **Windows.** The database is SQL Server Express LocalDB, which only runs on Windows.
- **[.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)**
- **SQL Server Express LocalDB**, with the default instance `(localdb)\MSSQLLocalDB`. It is installed with Visual Studio, or from the SQL Server Express installer. The project was tested on LocalDB 2016 (version 13.0).
- **Optional: the EF Core command-line tool**, needed only to reset the database:

  ```powershell
  dotnet tool install --global dotnet-ef
  ```

## Getting started

```powershell
git clone https://github.com/marvel03/onstroWebApi.git
cd onstroWebApi
dotnet run --project ArticleApi
```

The API starts on **http://localhost:5229**. There is nothing to configure. On the first run the app:

1. creates the database, storing its files in `ArticleApi/App_Data/`
2. creates the tables by applying the EF Core migrations
3. fills them with sample data

This happens in the Development environment, which `dotnet run` selects through `ArticleApi/Properties/launchSettings.json`. Running the compiled DLL directly uses the Production environment and skips it.

Each copy of the project gets its own database, named `ArticleDb_` followed by a code derived from the folder path, so two clones on the same machine never clash.

**To run with HTTPS**, use the `https` launch profile. It listens on **https://localhost:7010** as well as http://localhost:5229:

```powershell
dotnet run --project ArticleApi --launch-profile https
```

If the browser or client rejects the certificate, trust the .NET development certificate once:

```powershell
dotnet dev-certs https --trust
```

Under the default `http` profile the log shows the warning "Failed to determine the https port for redirect". It is harmless: requests are simply served over HTTP.

**To look at the data**, connect SQL Server Management Studio to `(localdb)\MSSQLLocalDB` with Windows Authentication, ticking **Trust server certificate**, and open the `ArticleDb_…` database.

## Sample data

The sample data is created relative to the day the database is created, so the date-based queries always have matching rows.

**Users** (6). The API has no user endpoints: as the task specifies, users are added directly in the database.

| Id | Username | Account created |
|---|---|---|
| 1 | alice | 2 months ago |
| 2 | bruno | 1 month ago |
| 3 | carmen | 8 months ago |
| 4 | dmitri | 10 days ago (has written nothing) |
| 5 | elena | 3 months ago |
| 6 | farid | 12 months ago |

To add another user, insert it with SQL, for example:

```sql
INSERT INTO Users (Username, CreatedAt) VALUES ('newuser', SYSUTCDATETIME());
```

**Articles** (42) and **contents** (74). Articles 1 to 5 and 42 were written by hand to cover specific cases:

| Id | Case it covers |
|---|---|
| 1 | Contents in all three languages, including two English versions by different authors |
| 2 | No English content |
| 3 | Created 5 months ago |
| 4 | Recent, but written by a user whose account is older than 4 months |
| 5 | No contents at all |
| 42 | Recent, with English by carmen (account older than 4 months) and French by alice (recent account): see [Report B](#report-b-recent-articles-in-a-language) |

Articles 6 to 41 are generated: one every 9 days going back about 10 months, with varied statuses and language combinations. There is also one content that belongs to no article.

## Resetting the database

To delete the database and start again from fresh sample data, stop the app and run:

```powershell
dotnet ef database drop --project ArticleApi
dotnet run --project ArticleApi
```

The first command deletes the database, including its files in `ArticleApi/App_Data/`. The next run creates it again and reloads the sample data.

Use this command rather than deleting the `App_Data` folder by hand. SQL Server LocalDB keeps the database files open and remembers the database, so deleting the folder either fails or leaves the next run unable to start. Drop the database this way before moving or renaming the project folder, too.

The command needs the EF Core command-line tool (see [Requirements](#requirements)).

## Using the API

All requests and responses are JSON. In the examples below, a **request body** is the JSON you send with a POST or PUT; a **response** is the JSON the API sends back. GET and DELETE requests have no request body.

- **Enum values are sent and returned as text**, not numbers:
  - `status`: `Draft`, `Published` or `Unpublished`
  - `language`: `English`, `French` or `Spanish`
- **Dates are UTC**, in ISO 8601 format ending in `Z`, for example `2026-09-09T00:44:38.5892938Z`. `createdAt` is always set by the server.
- **Errors** use the standard ProblemDetails format: **400** for invalid input, with a message saying what is wrong, and **404** for an Id that does not exist.

### Endpoints

| Method | URL | What it does | Success |
|---|---|---|---|
| GET | `/api/articles` | Paginated list of articles | 200 |
| GET | `/api/articles/{id}` | One article with all its contents | 200 |
| POST | `/api/articles` | Create an article | 201 |
| PUT | `/api/articles/{id}` | Update an article | 204 |
| DELETE | `/api/articles/{id}` | Delete an article **and all its contents** | 204 |
| GET | `/api/contents/{id}` | One content | 200 |
| POST | `/api/contents` | Create a content | 201 |
| PUT | `/api/contents/{id}` | Update a content | 204 |
| DELETE | `/api/contents/{id}` | Delete a content | 204 |

### List articles

```
GET /api/articles?page=1&pageSize=10&status=Published&sortBy=title&sortOrder=asc
```

Every query parameter is optional:

| Parameter | Values | Default |
|---|---|---|
| `page` | 1 or more | `1` |
| `pageSize` | 1 to 50 | `10` |
| `status` | `Draft`, `Published`, `Unpublished` — filters on the article's own status | all statuses |
| `sortBy` | `createdAt` or `title` | `createdAt` |
| `sortOrder` | `asc` or `desc` | newest first for `createdAt`, A to Z for `title` |

Values are not case-sensitive, so `status=published` also works. Anything else returns 400.

Each row shows the article's **title, author and status**. An article can have several contents, so one of them is chosen to represent it, and it supplies both the title and the author:

1. an **English** content if there is one, the oldest if there are several;
2. otherwise the **oldest content in any language**;
3. if two contents are equally old, the one with the lower Id.

An article with no contents has a `null` title and author, and always appears **last** when sorting by title, in either direction. Filtering, sorting and paging all happen in the database query.

Example request: `GET /api/articles?pageSize=2` (no request body)

Response: **200 OK**

```json
{
  "items": [
    {
      "id": 6,
      "title": "Async programming basics (part 1)",
      "author": "elena",
      "status": "Published"
    },
    {
      "id": 5,
      "title": null,
      "author": null,
      "status": "Draft"
    }
  ],
  "page": 1,
  "pageSize": 2,
  "totalCount": 42,
  "totalPages": 21
}
```

`totalCount` counts every article matching the filter, across all pages.

### Get one article, with its contents

Example request: `GET /api/articles/8` (no request body)

Response: **200 OK**, or **404 Not Found** if the article does not exist

```json
{
  "id": 8,
  "status": "Unpublished",
  "createdAt": "2026-09-09T00:44:38.5892938Z",
  "contents": [
    {
      "id": 14,
      "title": "Comprendre les jointures SQL (part 1)",
      "body": "Sample French text for \"Comprendre les jointures SQL (part 1)\".",
      "authorId": 5,
      "authorName": "elena",
      "status": "Unpublished",
      "language": "French",
      "createdAt": "2026-09-10T00:44:38.5892938Z",
      "articleId": 8
    },
    {
      "id": 13,
      "title": "Entender los joins de SQL (part 1)",
      "body": "Sample Spanish text for \"Entender los joins de SQL (part 1)\".",
      "authorId": 5,
      "authorName": "elena",
      "status": "Unpublished",
      "language": "Spanish",
      "createdAt": "2026-09-09T00:44:38.5892938Z",
      "articleId": 8
    }
  ]
}
```

Here the contents are listed by language, then by date. In the article list, article 8 shows the Spanish title, because it has no English content and the Spanish version is the older one.

The exact dates depend on when the database was created.

### Create, update and delete an article

**Create:** `POST /api/articles`

Request body (`status` is required):

```json
{
  "status": "Draft"
}
```

Response: **201 Created**, with a `Location` header giving the new article's address, and the new article in the body:

```json
{
  "id": 43,
  "status": "Draft",
  "createdAt": "2026-09-30T10:15:00.0000000Z",
  "contents": []
}
```

**Update:** `PUT /api/articles/{id}`, for example `PUT /api/articles/43`

Request body:

```json
{
  "status": "Published"
}
```

Response: **204 No Content**, with no response body, or **404 Not Found** if the article does not exist.

**Delete:** `DELETE /api/articles/{id}` (no request body)

Response: **204 No Content**, with no response body, or **404 Not Found**. **The article's contents are deleted with it.**

### Get one content

Example request: `GET /api/contents/1` (no request body)

Response: **200 OK**, or **404 Not Found** if the content does not exist:

```json
{
  "id": 1,
  "title": "Getting started with EF Core",
  "body": "Sample English text for \"Getting started with EF Core\".",
  "authorId": 1,
  "authorName": "alice",
  "status": "Published",
  "language": "English",
  "createdAt": "2026-09-10T00:44:38.5892938Z",
  "articleId": 1
}
```

### Create, update and delete a content

**Create:** `POST /api/contents`

Request body:

```json
{
  "title": "My first content",
  "body": "Some text",
  "authorId": 1,
  "status": "Draft",
  "language": "English",
  "articleId": 1
}
```

The fields of the request body:

| Field | Rules |
|---|---|
| `title` | required, not empty |
| `body` | required, not empty |
| `authorId` | required; the Id of an existing user (see [Sample data](#sample-data)) |
| `status` | required |
| `language` | required |
| `articleId` | optional; the Id of an existing article. Leave it out, or send `null`, for a content that belongs to no article |

An article may have several contents in the same language.

Response: **201 Created**, with a `Location` header giving the new content's address, and the new content in the body, including the author's name:

```json
{
  "id": 75,
  "title": "My first content",
  "body": "Some text",
  "authorId": 1,
  "authorName": "alice",
  "status": "Draft",
  "language": "English",
  "createdAt": "2026-09-30T10:20:00.0000000Z",
  "articleId": 1
}
```

**Update:** `PUT /api/contents/{id}`, for example `PUT /api/contents/75`

Request body. PUT replaces every field, so send all of them, including any you are not changing:

```json
{
  "title": "My first content, edited",
  "body": "Updated text",
  "authorId": 5,
  "status": "Published",
  "language": "English",
  "articleId": 1
}
```

This can also move a content to another article, or detach it from its article with `"articleId": null`.

Response: **204 No Content**, with no response body, or **404 Not Found** if the content does not exist.

**Delete:** `DELETE /api/contents/{id}` (no request body)

Response: **204 No Content**, with no response body, or **404 Not Found**.

**Example error.** Request body sent to `POST /api/contents`, with an author that does not exist:

```json
{
  "title": "x",
  "body": "y",
  "authorId": 999,
  "status": "Draft",
  "language": "English"
}
```

Response: **400 Bad Request**

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "Author 999 does not exist."
}
```

The Ids and dates in these examples will differ from yours.

### Sending requests

Any HTTP client works: Postman, curl, or the VS Code REST Client extension with `ArticleApi/ArticleApi.http`. With Postman, choose the method from the dropdown and put only the URL in the address box, for example `http://localhost:5229/api/articles`. For POST and PUT, put the request body under **Body → raw → JSON**. The response appears in the lower panel.

## Reports

The two reports are SQL scripts in the `database/` folder. They are written for SQL Server 2016, which has no `STRING_AGG`.

**To run one in SQL Server Management Studio:** open the file, select the `ArticleDb_…` database in the toolbar's database dropdown, and press F5. The scripts contain no `USE` statement, because the database name depends on the folder the project is in.

**To run one from a terminal**, find the database name first, then pass the file with `-i`:

```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -Q "SELECT name FROM sys.databases WHERE name LIKE 'ArticleDb_%'"
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -d ArticleDb_XXXXXXXX -i database\ReportA_AuthorArticleIds.sql
```

### Report A: authors and their articles

`database/ReportA_AuthorArticleIds.sql` lists every user with a JSON array of the Ids of the articles they have written content for. Each article appears once per author, in ascending order. Users who have written no articles appear with `[]`. The list is built with `FOR XML PATH` and `STUFF`.

With the sample data:

| AuthorId | Author | ArticleIds |
|---|---|---|
| 1 | alice | `[1,2,42]` |
| 2 | bruno | `[1,2]` |
| 3 | carmen | `[3,4,42]` |
| 4 | dmitri | `[]` |
| 5 | elena | `[6,8,10,12,14]` |
| 6 | farid | `[7,9,11,13,15,16,…,41]` |

### Report B: recent articles in a language

`database/ReportB_RecentArticlesByLanguage.sql` lists the articles created in the last 3 months that have content in a chosen language written by a user whose account was created in the last 4 months. **The language and the recent author must be on the same content.** Each article appears once, with the title and author of its oldest matching content.

Set the variables at the top of the script:

| Variable | Meaning | Default |
|---|---|---|
| `@Language` | `N'English'`, `N'French'` or `N'Spanish'`; anything else stops the script with an error | `N'English'` |
| `@ArticleMonths` | how many months back the article may have been created | `3` |
| `@UserMonths` | how many months back the author's account may have been created | `4` |

Months are calendar months counted back from the current UTC time.

With the sample data, English returns articles 6, 1, 10 and 14, French returns 8 articles and Spanish 7. Article 42 shows the rule at work: it is in the French report, because its French content is by alice, but not the English one, because its English content is by carmen, whose account is too old.

## Assumptions

The task leaves the following points open. These are the choices made, and why.

**Data model**

- **Naming.** The task's table definitions and its diagram disagree on naming (plural PascalCase against singular snake_case). The tables follow the table definitions: `Users`, `Articles`, `Contents`, with PascalCase columns. The columns `Contents.Content` and `Contents.Author` keep their names in the database. In C# they are the properties `Body` and `AuthorId`, because a class cannot have a member with its own name, and `Author` is the navigation to the user.
- **A content can exist without an article.** `ArticleId` is not marked `not null`, and the diagram shows the relationship as `0..1`.
- **An article can have several contents in the same language.** The task says an article may have many contents and does not limit them per language.
- **Statuses and languages are stored as text** (`'Published'`, `'French'`), as the diagram's `varchar` status suggests. This also keeps the report scripts readable. The API accepts and returns the names only, not numbers.
- **Deleting an article deletes its contents.** They are its language versions and have no purpose without it. **Deleting a user who has written content is refused,** since every content must have an author.
- **Title, body and username are required,** as well as the author. A content without a title could not be shown in the article list.
- **Text columns are `nvarchar`,** so French and Spanish accents are stored correctly. No lengths are given, so free text has no length limit.
- **Dates are stored in UTC as `datetime2`.** The task's `timestamp` is taken to mean a date and time; SQL Server's own `timestamp` type is an unrelated row-version counter. `CreatedAt` is always set by the server.
- **Users have no API.** As the task says, they are added directly in the database: by the seeder, or with SQL.

**API**

- **An article's status and its contents' statuses are independent.** The list's status filter uses the article's own status.
- **The title and author shown for an article** come from one content: an English one if it exists (the oldest, if several); otherwise the oldest content in any language; ties go to the lower Id. An article with no contents has no title or author and is sorted last in a title sort.
- **Default sorting is newest first.** Sorting by title defaults to A to Z.
- **Pages** hold 10 articles by default and at most 50.
- **PUT replaces every field.** Updating a content can therefore move it to another article, or detach it from its article.
- **References are checked.** Creating or updating a content with an author or article that does not exist returns 400, not a database error.

**Reports**

- **Report A** includes every user, even those who have written no article, since the task says "all authors". The array is JSON text. Content that belongs to no article is not counted.
- **Report B** requires the language and the recent author to be on the same content. The statuses of the article and its contents are not considered.

**Environment**

- Built and tested with SQL Server 2016 LocalDB. EF Core is set to SQL Server 2016's compatibility level (130), so it only generates SQL that version supports.
- The database is created, migrated and filled with sample data automatically, in the Development environment only. The sample dates are relative to the day the database is created, so the time-based report always has matching data.

## Project structure

```
database/           the two report scripts
ArticleApi/
├── Controllers/    HTTP endpoints: routes and status codes only
├── Services/       the logic: queries, validation, the title rule
├── Dtos/           the shapes the API accepts and returns
├── Entities/       one class per table, plus the Status and Language enums
├── Data/           AppDbContext, the sample-data seeder, the UTC date converter
├── Migrations/     EF Core migrations that create the tables
└── Program.cs      startup: services, database location, migration and seeding
```
