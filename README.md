# RaceDay - PROG6212 Portfolio of Evidence

RaceDay is an event management platform for the South African running, walking and cycling community.

| Part | What it contains |
|---|---|
| Part 1 | Planning: ERD, endpoint plan and SQL script in `/docs` |
| Part 2 | `RaceDay.Api` - ASP.NET Core 8 REST API, SQL Server, session login, roles, xUnit tests |
| Part 3 | `RaceDay.Web` - ASP.NET Core MVC front end, Azure Blob Storage image uploads, Docker |

- **YouTube video (unlisted):** `PASTE LINK HERE`
- **CI build:** `PASTE GREEN BUILD SCREENSHOT HERE` (for example `docs/ci-green-build.png`)

---

## How the system fits together

```text
Browser  ->  RaceDay.Web (MVC)  ->  RaceDay.Api (REST)  ->  SQL Server
                                          |
                                          +-->  Azure Blob Storage (banner + profile images)
```

- The MVC application has **no database access and no business logic**. Every read and write is an HTTP call to the API through one class: `RaceDay.Web/Services/ApiService.cs`.
- The MVC application **never talks to Azure**. Images are posted to the API, the API stores them in Azure Blob Storage and saves the URL in the database.
- Login: MVC posts to `/api/auth/login`, keeps the API's session cookie, the user's id, name and role in its own server-side session, and forwards that cookie on every API call.
- Roles are enforced twice: by the API (`[Authorize(Roles = ...)]`) and by the MVC `SessionAuthorize` filter, which also drives the role-specific menus.

---

## Features

**Everyone**
- Register (first name, last name, email, phone, password + confirm, role) and log in (with "remember me")
- Log out and be redirected; unauthenticated visitors are sent to the login page
- Profile page: update details and upload a profile picture (Azure Blob Storage)

**Organiser** (green accent navigation)
- Dashboard: events created, total enrolments, pending confirmations, upcoming dates
- Manage events: create, edit and delete with name, description, date, location, distance, type and **banner image upload**
- Categories: add and delete age or distance categories per event
- Enrolments: see every participant, their category and status; confirm or set back to pending
- Results entry: capture and publish finish time and finishing position

**Participant** (orange accent navigation)
- Home: upcoming events with search, type and distance filters
- Event detail: banner, full information, categories and **Enter event**
- My events: every enrolment with colour-coded status (green = Confirmed, amber = Pending) and withdraw
- My results: event, date, category, finish time and position (for example "47 out of 312")

---

## Run it locally (Visual Studio)

Requirements: .NET 8 SDK, SQL Server (LocalDB is fine) and an Azure Storage account.

1. **Azure Blob Storage** - create a Storage account, enable *Allow Blob anonymous access*, copy a connection string from *Security + networking > Access keys*.
2. **API secret** (kept out of Git):
   ```bash
   dotnet user-secrets set "AzureBlob:ConnectionString" "<your connection string>" --project RaceDay.Api
   ```
   The container `raceday-images` is created automatically.
3. Check the SQL connection string in `RaceDay.Api/appsettings.json`.
   If you ran an earlier version of the API, **delete the old `RaceDayDb` database first** so the new columns are created.
4. In Visual Studio choose *Configure Startup Projects > Multiple startup projects* and start both **RaceDay.Api** (https://localhost:7100) and **RaceDay.Web** (https://localhost:7200).
5. Open https://localhost:7200.

The API URL used by the web app is `ApiSettings:BaseUrl` in `RaceDay.Web/appsettings.json`.

### Demo logins (password `Password123!`)

| Role | Email |
|---|---|
| Organiser | `thandi.organiser@raceday.co.za` |
| Organiser | `pieter.organiser@raceday.co.za` |
| Participant | `sipho.participant@raceday.co.za` |
| Participant | `aisha.participant@raceday.co.za` |

---

## Run it with Docker

### Everything with one command (web + API + SQL Server)

```bash
cp .env.example .env        # then paste your Azure connection string into .env
docker compose up --build
```

- Web app: http://localhost:8080
- API / Swagger: http://localhost:5100

### Only the MVC container (API already running on your machine)

```bash
docker build -t raceday-web .
docker run -p 8080:8080 --add-host=host.docker.internal:host-gateway -e ApiSettings__BaseUrl=http://host.docker.internal:5100/ raceday-web
```

Start the API first (it also listens on `http://localhost:5100`), then open http://localhost:8080.

The `Dockerfile` is a multi-stage build (SDK image to build, slim ASP.NET runtime image to run, runs as a non-root user).

---

## Swagger and the API filters

Open `http://localhost:5100/swagger` (or `https://localhost:7100/swagger` in Visual Studio). Every endpoint has a description, every parameter has an explanation and an example, and every response shows its class and status codes. The descriptions come from the `///` comments in the code.

To try the endpoints, call `POST /api/auth/login` first. Swagger keeps the session cookie for the next calls.

When you click **Try it out**, the request body box is already filled with an example (for example a full event or a login). Change the values and press **Execute**.

`GET /api/events` accepts these optional filters (they can be combined):

| Parameter | Meaning | Example |
|---|---|---|
| `type` | Run, Walk or Cycle | `?type=Run` |
| `search` | Text in the event name or location | `?search=soweto` |
| `upcomingOnly` | `true` hides events that already happened | `?upcomingOnly=true` |
| `minDistanceKm` | At least this many km | `?minDistanceKm=5` |
| `maxDistanceKm` | At most this many km | `?maxDistanceKm=21.1` |

`GET /api/events/{eventId}/enrolments` accepts `?status=Pending` or `?status=Confirmed`.

## AI usage disclosure

The module brief asks students to disclose any use of AI tools. Complete this section honestly before you submit, for example:

> I used an AI assistant (Claude) to help plan the project structure, to explain errors, and to help write and comment parts of the code. I reviewed, tested and can explain the code I submitted.

---

## Tests and CI

```bash
dotnet test RaceDay.sln
```

The API tests (xUnit, in-memory database, fake blob storage) cover login, roles, events, enrolment, results, banner/profile uploads and enrolment status.

The GitHub Actions workflow (`.github/workflows/ci.yml`) validates the repository structure, restores and builds the **whole solution including the MVC project**, runs the tests and builds the Docker image.

---

## Project structure

```text
RaceDay/
├── RaceDay.Api/            REST API (Part 2 + Azure Blob Storage)
│   ├── Controllers/  Data/  Dtos/  Models/  Storage/  Auth/
├── RaceDay.Api.Tests/      xUnit tests
├── RaceDay.Web/            MVC front end (Part 3)
│   ├── Controllers/        Account, Events, MyEvents, MyResults, Profile + Organiser controllers
│   ├── Services/           ApiService (the only code that calls the API)
│   ├── Filters/            SessionAuthorize (login + role check)
│   ├── Models/             Classes that match the API JSON
│   ├── ViewModels/         Models used by the Razor pages
│   ├── Views/              Razor views and shared partials
│   └── wwwroot/            site.css (RaceDay theme) and site.js
├── docs/                   ERD, endpoint plan, SQL script
├── Dockerfile              MVC application image
├── Dockerfile.api          API image (used by docker compose)
├── docker-compose.yml      web + api + SQL Server
└── RaceDay.sln
```
