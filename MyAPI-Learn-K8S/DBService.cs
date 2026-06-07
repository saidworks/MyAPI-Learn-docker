using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace MyAPI_Learn_K8S;

public class DBService
{
    private static readonly object _dbLock = new object(); // Lock object for thread safety

    public static async Task CreateDbAsync(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new Exception("DefaultConnection string is missing in configuration.");
        }

        var dbName = new SqlConnectionStringBuilder(connectionString).InitialCatalog;
        if (!IsValidDatabaseName(dbName))
        {
            throw new ArgumentException($"Invalid database name: {dbName}");
        }

        await Task.Run(() =>
        {
            lock (_dbLock)
            {
                CreateDatabase(connectionString, dbName);
                CreateTable(connectionString, dbName);
                InsertSeedData(connectionString, dbName);
            }
        });
    }

    private static bool IsValidDatabaseName(string dbName)
    {
        if (string.IsNullOrEmpty(dbName) || dbName.Length > 128)
            return false;

        var validChars = System.Text.RegularExpressions.Regex.IsMatch(
            dbName,
            "^[A-Za-z][A-Za-z0-9_]*$"
        );
        return validChars;
    }

    private static void CreateDatabase(string connectionString, string dbName)
    {
        using var connection = new SqlConnection(connectionString);
        connection.Open();

        using var command = new SqlCommand(
            $@"
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = N'{EscapeSqlIdentifier(dbName)}')
BEGIN
    CREATE DATABASE [{dbName}];
END;
",
            connection
        );

        command.ExecuteNonQuery();

        VerifyDatabaseExists(connection, dbName);
    }

    private static void CreateTable(string connectionString, string dbName)
    {
        using var connection = new SqlConnection(connectionString);
        connection.Open();

        using var command = new SqlCommand(
            $@"
IF NOT EXISTS (SELECT * FROM sys.objects 
               WHERE object_id = OBJECT_ID(N'[dbo].[Product]') 
               AND type = 'U')
BEGIN
    CREATE TABLE [dbo].[Product] (
        [ProductId] INT PRIMARY KEY IDENTITY(1,1),
        [ProductName] NVARCHAR(50) NOT NULL,
        [ProductDescription] NVARCHAR(500) NULL
    );
END;
",
            connection
        );

        command.ExecuteNonQuery();

        VerifyTableExists(connection, dbName, "Product");
    }

    private static void InsertSeedData(string connectionString, string dbName)
    {
        using var connection = new SqlConnection(connectionString);

        // Enable IDENTITY_INSERT
        using var enableIdentityCmd = new SqlCommand(
            $@"
SET IDENTITY_INSERT [dbo].[Product] ON;
",
            connection
        );
        enableIdentityCmd.ExecuteNonQuery();

        // Insert seed data
        using var insertCmd = new SqlCommand(
            @"
INSERT INTO [dbo].[Product] ([ProductId], [ProductName], [ProductDescription]) 
VALUES
(1, N'Laptop', N'High performance laptop'),
(2, N'Mouse', N'Wireless optical mouse'),
(3, N'Keyboard', N'Mechanical RGB keyboard'),
(4, N'Monitor', N'27-inch 4K monitor'),
(5, N'Headphones', N'Noise cancelling headphones');
",
            connection
        );
        insertCmd.ExecuteNonQuery();

        // Disable IDENTITY_INSERT
        using var disableIdentityCmd = new SqlCommand(
            $@"
SET IDENTITY_INSERT [dbo].[Product] OFF;
",
            connection
        );
        disableIdentityCmd.ExecuteNonQuery();
    }

    private static void VerifyDatabaseExists(SqlConnection connection, string dbName)
    {
        using var command = new SqlCommand(
            $@"
SELECT COUNT(*) FROM sys.databases WHERE name = N'{EscapeSqlIdentifier(dbName)}';
",
            connection
        );

        var count = Convert.ToInt32(command.ExecuteScalar());
        if (count == 0)
        {
            throw new Exception($"Failed to create database: {dbName}");
        }
    }

    private static void VerifyTableExists(SqlConnection connection, string dbName, string tableName)
    {
        using var command = new SqlCommand(
            $@"
SELECT COUNT(*) FROM sys.objects 
WHERE object_id = OBJECT_ID(N'[dbo].[{tableName}]') 
AND type = 'U';
",
            connection
        );

        var count = Convert.ToInt32(command.ExecuteScalar());
        if (count == 0)
        {
            throw new Exception($"Failed to create table: {tableName} in database: {dbName}");
        }
    }

    private static string EscapeSqlIdentifier(string identifier)
    {
        return identifier.Replace("[", "[][]").Replace("]", "[]");
    }

    private static void Retry(Action action, int retries = 10, int delayMilliseconds = 2000)
    {
        for (int i = 0; i < retries; i++)
        {
            try
            {
                action();
                return;
            }
            catch (SqlException ex)
                when (ex.Number == 4060
                    || // Database creation failed
                    ex.Number == 42000
                    || // Object already exists
                    ex.Number == 40197
                    || // Database does not exist
                    ex.Number == 40184 // Table creation failed
                )
            {
                if (i == retries - 1)
                    throw;
                Thread.Sleep(delayMilliseconds);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
