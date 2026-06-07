using Microsoft.Data.SqlClient;
using System.Threading;

namespace MyAPI_Learn_K8S;

public class DBService
{
    public static void CreateDb(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new Exception("DefaultConnection string is missing in configuration.");
        }

        // To create the database, we need to connect to 'master' first.
        var builder = new SqlConnectionStringBuilder(connectionString)
        {
            InitialCatalog = "master"
        };
        var masterConnectionString = builder.ConnectionString;

        var dbName = new SqlConnectionStringBuilder(connectionString).InitialCatalog;

        Retry(() =>
        {
            using (var connection = new SqlConnection(masterConnectionString))
            {
                connection.Open();
                using var command = new SqlCommand($@"
IF DB_ID(N'{dbName}') IS NULL
BEGIN
    CREATE DATABASE [{dbName}];
END;", connection);
                command.ExecuteNonQuery();
            }
        });

        Retry(() =>
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using var command = new SqlCommand(@"
IF OBJECT_ID(N'dbo.Product', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Product
    (
        ProductId INT PRIMARY KEY,
        ProductName VARCHAR(50),
        ProductDescription VARCHAR(500)
    );
END;", connection);
                command.ExecuteNonQuery();
            }
        });

        Retry(() =>
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using var command = new SqlCommand(@"
IF NOT EXISTS (SELECT 1 FROM dbo.Product)
BEGIN
    INSERT INTO dbo.Product (ProductId, ProductName, ProductDescription) VALUES
    (1, 'Laptop', 'High performance laptop'),
    (2, 'Mouse', 'Wireless optical mouse'),
    (3, 'Keyboard', 'Mechanical RGB keyboard'),
    (4, 'Monitor', '27-inch 4K monitor'),
    (5, 'Headphones', 'Noise cancelling headphones');
END;", connection);
                command.ExecuteNonQuery();
            }
        });
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
            catch (SqlException)
            {
                if (i == retries - 1) throw;
                Thread.Sleep(delayMilliseconds);
            }
        }
    }
}