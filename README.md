# Student Grade Management System

UST Angelicum College - Individual Practical Examination

Faculty members manage student grade records. Students sign in and see only their own grades.
Grade remarks are always computed by the server and never trusted from the browser.

## Technology

| Layer | Technology |
|---|---|
| REST API | ASP.NET Core Web API (.NET 10, C#) |
| Web interface | ASP.NET Core MVC, Bootstrap 5, vanilla JavaScript `fetch()` with `async/await` |
| Data | SQL Server (LocalDB or Express), Entity Framework Core 10 |
| Authentication | ASP.NET Core Identity (hashed passwords, roles, lockout) + JWT bearer tokens |
| Testing | Postman (`Postman/StudentGradeManagement.postman_collection.json`) |

## Repository layout

```text
CruzDusty_WebSystemFinalProject.slnx
Database/StudentGradeManagementDB.sql          Academic schema and sample data (instructor script)
Postman/StudentGradeManagement.postman_collection.json
CruzDusty_WebSystemFinalProject.Api/           Web API
  Controllers/      AuthController, GradesController, StudentController, StudentsController
  Data/             AppDbContext, DbSeeder, DesignTimeDbContextFactory, Migrations/
  Models/           Entities (ApplicationUser, Student, Faculty, StudentGrade) and DTOs
  Services/         GradeCalculator, GradeService, AuthService, TokenService, StudentService
  Middleware/       ExceptionHandlingMiddleware (JSON errors, no stack traces)
CruzDusty_WebSystemFinalProject.Web/           MVC web interface
  Views/Faculty/    Dashboard, Grades
  Views/Student/    MyGrades
  wwwroot/js/       api.js (fetch wrapper), auth.js, faculty-*.js, student-grades.js
```

## Prerequisites

- .NET 10 SDK (`dotnet --version` prints 10.0.x)
- SQL Server Express LocalDB or SQL Server Express
- `sqlcmd` or SQL Server Management Studio
- EF Core tools: `dotnet tool install --global dotnet-ef --version 10.0.0`
- Postman

## Database setup

The database has two parts:

1. **Academic data** (`Students`, `Faculty`, `StudentGrades`, `GetGradeRemarks`) comes from the SQL script.
2. **Login accounts** (`AspNetUsers`, `AspNetRoles`, ...) come from the EF Core migration in
   `CruzDusty_WebSystemFinalProject.Api/Data/Migrations`. The migration also adds the nullable
   `StudentId` and `FacultyId` columns that link each login to its academic record.
   There is no homemade password table and no plaintext password anywhere.

Run these from the solution folder, **in this order**:

```bat
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -I -i Database\StudentGradeManagementDB.sql
```

```bat
dotnet ef database update --project CruzDusty_WebSystemFinalProject.Api
```

The API also applies pending migrations automatically when it starts.
The academic tables are marked `ExcludeFromMigrations`, so EF never creates or alters them.

## Configuration

| Setting | Where | Notes |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | `Api/appsettings.json` | LocalDB by default. For SQL Express use `Server=.\\SQLEXPRESS` |
| `Jwt:Key` | user secrets (not committed) | At least 32 characters |
| `Cors:AllowedOrigins` | `Api/appsettings.json` | Must contain the MVC site's address |
| `ApiSettings:BaseUrl` | `Web/appsettings.json` | The API address used by the browser |

Set the JWT signing key once (it is a secret, so it is not in source control):

```bat
dotnet user-secrets set "Jwt:Key" "any-random-text-of-at-least-32-characters" --project CruzDusty_WebSystemFinalProject.Api
```

If the key is not set, the API generates a temporary one and logs a warning; tokens then stop
working whenever the API restarts.

## Running

Trust the development HTTPS certificate once:

```bat
dotnet dev-certs https --trust
```

Start the API, then the web site, each in its own window:

```bat
start "API" cmd /k dotnet run --project CruzDusty_WebSystemFinalProject.Api --launch-profile https
```

```bat
start "WEB" cmd /k dotnet run --project CruzDusty_WebSystemFinalProject.Web --launch-profile https
```

- API: https://localhost:7001 (wait for `Database seeding completed.`)
- Web: https://localhost:7002

## Demonstration accounts

Created automatically in the Development environment only, linked to the script's records.
These are demonstration credentials, not real passwords.

| Username | Password | Role | Linked record |
|---|---|---|---|
| `faculty1` | `Faculty@123` | Faculty | FAC-001 Andrea Garcia |
| `faculty2` | `Faculty@123` | Faculty | FAC-002 Michael Ramos |
| `student1` | `Student@123` | Student | 2026-0001 Juan Dela Cruz |
| `student2` | `Student@123` | Student | 2026-0002 Maria Santos |
| `student3` | `Student@123` | Student | 2026-0003 Pedro Reyes |

## API endpoints

| Method | Route | Role | Success | Notes |
|---|---|---|---|---|
| POST | `/api/auth/login` | Anonymous | 200 | Returns `{ token, role, fullName, studentId, facultyId, expiresAt }` |
| GET | `/api/grades` | Faculty | 200 | Filters: `studentId`, `subjectCode`, `semester`, `schoolYear` |
| GET | `/api/grades/{id}` | Faculty, Student | 200 | A student gets 404 for a record that is not theirs |
| POST | `/api/grades` | Faculty | 201 + `Location` | Remarks and FacultyId set by the server |
| PUT | `/api/grades/{id}` | Faculty | 200 | Remarks recomputed from the new grade |
| DELETE | `/api/grades/{id}` | Faculty | 204 | |
| POST | `/api/grades/preview` | Faculty | 200 | Computes a remark without saving |
| GET | `/api/student/grades` | Student | 200 | StudentId taken only from the token |
| GET | `/api/students` | Faculty | 200 | Student list for the grade form |

Every error response has the same shape:

```json
{ "message": "Grade must be a whole number from 0 to 100.", "statusCode": 400 }
```

## Grade remarks

| Grade | Remark |
|---|---|
| 95-100 | CMS |
| 90-94 | SP |
| 85-89 | SAP |
| 80-84 | GP |
| 75-79 | EP |
| 0-74 | BEP/FAILED |

## How the security works

**Authentication flow.** `POST /api/auth/login` looks the user up with Identity's `UserManager`,
verifies the hashed password (`CheckPasswordAsync`), counts failures toward a 5-attempt lockout,
and checks the role mapping: a Faculty login must be linked to an active faculty record, a Student
login to an active student record, never both (also enforced by a database CHECK constraint).
The API then issues a signed JWT containing the role and the linked `studentId` or `facultyId`.
The browser stores it in `sessionStorage` and sends it as `Authorization: Bearer <token>`.

**Authorization.** Every grade endpoint has `[Authorize(Roles = ...)]`. A student token on a
faculty endpoint gets 403; a request without a valid token gets 401. Hiding buttons in the MVC
pages is only for convenience; the API enforces every rule.

**Ownership.** `GET /api/student/grades` has no parameters. It reads `studentId` from the signed
token, so changing the URL, query string or body cannot select another student. `GET /api/grades/{id}`
compares the record's `StudentId` with the token's `studentId` and returns 404 when they differ,
the same response as for a record that does not exist, so other students' records stay hidden.

**Validation.** `Grade` is `int?` with `[Required]` and `[Range(0, 100)]`. A missing grade, 105,
-1, 85.5 or text all return 400. `GradeCalculator` checks the range again, and the database has
`CHECK (Grade BETWEEN 0 AND 100)`. A unique constraint on
(StudentId, SubjectCode, Semester, SchoolYear) prevents duplicates (409 Conflict).
All data access goes through EF Core, which uses parameterized SQL.

**Remark computation.** The request DTO has no `Remarks`, `FacultyId` or date properties, so any
such values sent by a client are discarded during JSON binding. `GradeService` calls
`GradeCalculator.ComputeRemarks(grade)` on every create and every update and overwrites
`Remarks`, so a remark can never stay attached to an old grade. `FacultyId` comes from the token.

## Postman testing

1. Import `Postman/StudentGradeManagement.postman_collection.json`.
2. Postman settings: turn off **SSL certificate verification** (the API uses the local development certificate).
3. Make sure the API is running, then run the requests **in order**, or use **Run collection**.
   Each request has tests that check the status code and body. Later requests use values
   (tokens, ids) saved by earlier ones.
4. The collection removes its own test records at the end and before it starts, so it can be re-run.

| # | Test | Method and endpoint | Role | Expected |
|---|---|---|---|---|
| 1 | Faculty GET | GET `/api/grades` | Faculty | 200 |
| 2 | Faculty POST | POST `/api/grades` (grade 96) | Faculty | 201, CMS |
| 3 | Faculty PUT | PUT `/api/grades/{id}` (grade 74) | Faculty | 200, remark changes to BEP/FAILED |
| 4 | Faculty DELETE | DELETE `/api/grades/{id}` | Faculty | 204 |
| 5 | Validation | POST grade 105, -1, 85.5, missing, text | Faculty | 400 each |
| 6 | Tampering | POST grade 60 with `"remarks": "CMS"` | Faculty | 201, BEP/FAILED |
| 7 | Student read | GET `/api/student/grades` | Student | 200, own grades only |
| 8 | Ownership | GET `/api/grades/{other student's id}`, `?studentId=` | Student | 404 / own data only / 403 |
| 9 | Student writes | POST, PUT, DELETE `/api/grades` | Student | 403 each |
| 10 | No token | GET `/api/grades`, GET `/api/student/grades` | None | 401 |

Screenshots of each test (method, endpoint, role, status and response body) go in `Postman/screenshots/`,
named after the request number, for example `3.2-faculty-post-201.png`.

## Troubleshooting

- **"Cannot reach the server" in the web page**: the API is not running, or the certificate is not trusted.
- **"Foreign key ... references invalid table"** during `database update`: the SQL script was not run on this database yet.
- **"address already in use"**: another copy is running; stop it (`taskkill /IM CruzDusty_WebSystemFinalProject.Api.exe /F`).
- **CORS error in the browser console**: open the site at exactly https://localhost:7002.
