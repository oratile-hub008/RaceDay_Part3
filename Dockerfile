# RaceDay MVC web application (Part 3)
# Build: docker build -t raceday-web .
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first so Docker can cache the packages between builds
COPY RaceDay.Web/RaceDay.Web.csproj RaceDay.Web/
RUN dotnet restore RaceDay.Web/RaceDay.Web.csproj

COPY RaceDay.Web/ RaceDay.Web/
RUN dotnet publish RaceDay.Web/RaceDay.Web.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
# Where the Part 2 API lives. Override with: -e ApiSettings__BaseUrl=http://your-api:port/
ENV ApiSettings__BaseUrl=http://host.docker.internal:5100/

EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "RaceDay.Web.dll"]
