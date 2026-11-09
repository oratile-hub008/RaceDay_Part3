-- RaceDay schema (reference script - matches the EF Core model and the Part 1 ERD).
IF DB_ID('RaceDayDb') IS NULL CREATE DATABASE RaceDayDb;
GO
USE RaceDayDb;
GO
CREATE TABLE Roles (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(20) NOT NULL UNIQUE
);
CREATE TABLE Users (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(256) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(MAX) NOT NULL,
    PhoneNumber NVARCHAR(30) NULL,
    ProfilePictureUrl NVARCHAR(500) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    RoleId INT NOT NULL REFERENCES Roles(Id)
);
CREATE TABLE Events (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(150) NOT NULL,
    Description NVARCHAR(2000) NOT NULL DEFAULT '',
    EventDate DATETIME2 NOT NULL,
    Location NVARCHAR(200) NOT NULL,
    DistanceKm DECIMAL(7,2) NOT NULL,
    EventType NVARCHAR(10) NOT NULL CHECK (EventType IN ('Run','Walk','Cycle')),
    BannerUrl NVARCHAR(500) NULL,
    OrganiserId INT NOT NULL REFERENCES Users(Id)
);
CREATE TABLE Categories (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500) NULL,
    EventId INT NOT NULL REFERENCES Events(Id) ON DELETE CASCADE
);
CREATE TABLE Enrolments (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    EnrolledAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    Status NVARCHAR(10) NOT NULL DEFAULT 'Pending' CHECK (Status IN ('Pending','Confirmed')),
    EventId INT NOT NULL REFERENCES Events(Id) ON DELETE CASCADE,
    CategoryId INT NOT NULL REFERENCES Categories(Id),
    ParticipantId INT NOT NULL REFERENCES Users(Id),
    CONSTRAINT UQ_Enrolment_Event_Participant UNIQUE (EventId, ParticipantId)
);
CREATE TABLE Results (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    FinishTime TIME NOT NULL,
    Position INT NOT NULL CHECK (Position > 0),
    RecordedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    EnrolmentId INT NOT NULL UNIQUE REFERENCES Enrolments(Id) ON DELETE CASCADE
);
GO
INSERT INTO Roles (Name) VALUES ('Organiser'), ('Participant');
-- NOTE: sample users are created by the API's start-up seeder (SeedDemoData) because
-- passwords must be hashed by ASP.NET Core; do not insert plain-text passwords.
