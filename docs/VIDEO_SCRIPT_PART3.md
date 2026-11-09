# Part 3 video script (about 7 minutes, unlisted on YouTube)

Speak in your own words. Everything below is a checklist of what to show and say, so you cover every rubric point: the full app, the MVC structure, Blob Storage, Docker and explaining the code.

## 1. Introduction (30 seconds)
- Say your name and student number and that this is RaceDay Part 3.
- Show the GitHub repository and the `Part3` branch with its commits.

## 2. MVC structure (1 minute)
- Show Solution Explorer: `RaceDay.Api`, `RaceDay.Web`, `RaceDay.Api.Tests`.
- Open `RaceDay.Web` and point at the folders: `Controllers`, `Views`, `ViewModels`, `Services`, `Filters`, `Models`.
- Open `ApiService.cs` and explain: the website has no database; every call is an HTTP call to the API and the API's session cookie is forwarded.
- Open `Program.cs` and show `AddHttpClient<ApiService>` and `AddSession`.

## 3. Authentication and navigation (1 minute)
- Register a new participant, log in, point at the participant menu.
- Open `AppController.StartSession`: the user id, name and role are stored in the session.
- Show `SessionAuthorizeAttribute`: try to open an organiser page as a participant (redirected). Log out and open a page (sent to login).

## 4. Organiser interface (1 minute 30 seconds)
- Log in as `thandi.organiser@raceday.co.za`.
- Dashboard: events, total enrolments, upcoming dates.
- Create an event with a banner image, then edit it.
- Add a category. Open Enrolments and confirm one (green Confirmed, amber Pending).
- Results: enter a finish time and position.

## 5. Participant interface (1 minute 30 seconds)
- Log in as `sipho.participant@raceday.co.za`.
- Home page: use the filters (search, type, distance).
- Open an event: details, banner and categories. Enter an event.
- My events (status), My results (position out of total finishers), Profile (update details and upload a picture).

## 6. Azure Blob Storage (1 minute)
- Show the Azure portal container with the uploaded images.
- Open `BlobStorage.cs`: explain that the MVC site posts the file to the API, the API uploads it to Azure and saves the URL in the database.
- Show the image displayed on the event detail page.

## 7. Docker (1 minute)
- Open `Dockerfile` and `docker-compose.yml`: explain the two build stages and the three containers (SQL Server, API, web).
- Run `docker compose up --build` (or show it already running) and open http://localhost:8080.
- Show the same features working inside Docker.

## 8. Wrap up (30 seconds)
- Show the green GitHub Actions build and the tests passing.
- Say what you learned and thank the viewer.

Tip: practise once. Be ready to explain any file you show, because the code is assessed as your own understanding.
