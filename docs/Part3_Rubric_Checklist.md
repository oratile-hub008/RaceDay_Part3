# Part 3 - requirements and rubric checklist

How every Part 3 requirement and rubric line is covered. "Yours" marks the things only you can do.

## Requirements

| # | Requirement | Where it is |
|---|---|---|
| 1 | MVC app uses the Part 2 API for all data, no direct database access | `RaceDay.Web` has no `DbContext`. Every call goes through `Services/ApiService.cs` |
| 2 | Register and log in through the API | `AccountController` + `Views/Account` |
| 2 | Session stores user id and role | `AppController.StartSession` (keys in `Helpers/SessionKeys.cs`) |
| 2 | Role-specific menus, logout clears session, unauthenticated users redirected | `Views/Shared/_Layout.cshtml`, `Filters/SessionAuthorizeAttribute.cs`, `AccountController.Logout` |
| 3 | Organiser dashboard: events, enrolments per event, upcoming dates | `DashboardController` + `Views/Dashboard` |
| 3 | Create, edit, delete events with all fields and banner upload | `ManageEventsController` + `Views/ManageEvents` |
| 3 | Category management | `ManageCategoriesController` |
| 3 | Enrolments page with category and status | `ManageEnrolmentsController` |
| 3 | Results entry (finish time + position, published straight away) | `ManageResultsController` |
| 4 | Participant home with filters | `EventsController.Index` (search, type, distance) |
| 4 | Event details with categories | `EventsController.Details` |
| 4 | Enrol in a chosen category | `EventsController.Enrol` |
| 4 | My events with status | `MyEventsController` |
| 4 | My results, for example "47th of 312" | `MyResultsController` (uses `TotalFinishers` from the API) |
| 4 | Profile page with picture upload | `ProfileController` |
| 5 | Azure Blob only through the API | `RaceDay.Api/Storage/BlobStorage.cs`; the MVC app only posts the file to the API |
| 5 | Banner shown on the event detail page | `Views/Events/Details.cshtml`, `Views/Shared/_EventBanner.cshtml` |
| 6 | Runs in Docker, run command in README | `Dockerfile`, `Dockerfile.api`, `docker-compose.yml`, README |
| 7 | RaceDay brand, Bootstrap, clear menu, colour-coded statuses, graceful errors | `wwwroot/css/site.css`, Bootstrap 5 in the layout, `_StatusBadge.cshtml`, `Views/Shared/ApiError.cshtml` |
| 8 | Part 3 branch, 20+ commits, workflow builds the MVC project | `.github/workflows/ci.yml`; commit list in `Part3_Guide1_Checklist.md` |

## Rubric

| Criteria (marks) | Top band asks for | Status |
|---|---|---|
| Correct submission (5) | Clear folders, README with Docker command, CI screenshot, video link | Folders and README done. **Yours:** CI screenshot and video link |
| GitHub and CI/CD (10) | 20+ meaningful commits, workflow validates structure with a green build | Workflow done. **Yours:** the commits and the green build |
| Authentication and navigation (10) | Register and login, session with id and role, distinct menus, logout, redirects | Done |
| Organiser interface (15) | Dashboard, events, categories, enrolments, results | Done |
| Participant interface (15) | Home, event details, enrol, my events, my results, profile | Done |
| Azure Blob Storage (10) | File goes MVC to API, URL stored, shown in MVC | Done. **Yours:** your own Azure connection string |
| Docker (10) | Builds and runs cleanly with a run command in the README | Done. **Yours:** run it once to confirm |
| UI design (10) | Polished RaceDay look, colour-coded statuses, imagery, intuitive navigation | Done |
| Video (15) | Full demo of MVC pages, Blob Storage, Docker, code explanation | Script in `VIDEO_SCRIPT_PART3.md`. **Yours:** record it |

GitHub not used or incorrectly used is a **-5%** penalty, so push to the `Part3` branch regularly.

## Before you submit

- [ ] `dotnet test RaceDay.sln` passes
- [ ] `docker compose up --build` works and the site opens on http://localhost:8080
- [ ] An image uploads to Azure and shows on the event page
- [ ] 20+ commits on the `Part3` branch, green build on GitHub Actions, screenshot saved as `docs/ci-green-build.png`
- [ ] Unlisted YouTube video recorded and the link pasted in the README
- [ ] The "AI usage" section in the README is filled in honestly (the brief asks for it)
- [ ] `.env` and your Azure key are not on GitHub
