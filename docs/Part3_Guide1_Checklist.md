# Part 3 - Guide 1 (Basic Setup) checklist

| Guide part | What it asks | Where it is in this project |
|---|---|---|
| A | Add the MVC project (.NET 8, no authentication, no container support) | `RaceDay.Web` |
| B | Multiple startup projects: `RaceDay.Api` + `RaceDay.Web` = Start, tests = None | Solution > Configure Startup Projects |
| C | Note your own http/https addresses | API: http://localhost:5100, https://localhost:7100. Web: see its console |
| D | Folders `Services`, `Filters`, `ViewModels` | all three exist in `RaceDay.Web` |
| E | Branch `Part3`, commit and push, 20+ commits | see the commit plan below |
| F | `ApiSettings:BaseUrl` = API **http** address ending in `/` | `RaceDay.Web/appsettings.json` = `http://localhost:5100/` |
| G | `Services/ApiService.cs` and the HttpClient + session in `Program.cs` | `ApiService.cs` is filled in; `Program.cs` registers it |

If your API runs on a different port, change `BaseUrl` (keep the `/` at the end).

## Commit plan (20+ commits)
1. Add RaceDay.Web MVC project
2. Configure multiple startup projects
3. Add Services, Filters and ViewModels folders
4. Add ApiSettings BaseUrl to appsettings.json
5. Add ApiResult and ApiService
6. Register HttpClient, session and anti-forgery in Program.cs
7. Add ApiModels
8. Add SessionKeys and UiHelper
9. Add SessionAuthorize filter
10. Add AppController base class
11. Add layout, site.css and shared partials
12. Add Account controller and views (login, register, logout)
13. Add Home controller and views
14. Add Profile controller and views
15. Add Events controller and views (browse, details, enter)
16. Add MyEvents controller and views
17. Add MyResults controller and views
18. Add Dashboard controller and view
19. Add ManageEvents controller and views
20. Add ManageCategories controller and views
21. Add ManageEnrolments controller and views
22. Add ManageResults controller and views
23. Return typed results so Swagger shows response classes
24. Add Dockerfiles and docker-compose.yml
25. Add GitHub Actions workflow
26. Update README
