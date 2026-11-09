using Microsoft.EntityFrameworkCore;

namespace RaceDay.Api.Data;

// EnsureCreated() only builds the database when it does not exist yet. It never adds new columns
// to a database that was created by an older version of the project. That caused the error
// "Invalid column name 'ProfilePictureUrl'". This class adds the columns that are missing,
// so an old RaceDayDb keeps working without being deleted.
public static class DbPatcher
{
    public static void AddMissingColumns(AppDbContext db)
    {
        // Only SQL Server needs this (the tests use an in-memory database).
        if (!db.Database.IsSqlServer()) return;

        // COL_LENGTH returns NULL when the column does not exist.
        db.Database.ExecuteSqlRaw(
            "IF COL_LENGTH('Users', 'ProfilePictureUrl') IS NULL ALTER TABLE Users ADD ProfilePictureUrl NVARCHAR(MAX) NULL;");

        db.Database.ExecuteSqlRaw(
            "IF COL_LENGTH('Events', 'BannerUrl') IS NULL ALTER TABLE Events ADD BannerUrl NVARCHAR(MAX) NULL;");
    }
}
