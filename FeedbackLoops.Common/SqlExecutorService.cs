using Npgsql;
using Pgvector;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;

public class SqlExecutorService
{
    private readonly string _connectionString;

    public SqlExecutorService(IConfiguration configuration)
    {
        _connectionString = configuration["NeonDatabaseConnectionString"];
    }

    public async Task<List<Dictionary<string, object>>> ExecuteQueryAsync(string sqlQuery, object parameters = null)
    {
        var result = new List<Dictionary<string, object>>();

        using (var connection = new NpgsqlConnection(_connectionString))
        {
            await connection.OpenAsync();

            using var command = new NpgsqlCommand(sqlQuery, connection);
            if (parameters != null)
            {
                foreach (var prop in parameters.GetType().GetProperties())
                {
                    command.Parameters.AddWithValue($"@{prop.Name}", prop.GetValue(parameters));
                }
            }

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var columnName = reader.GetName(i);
                    var columnType = reader.GetDataTypeName(i);

                    if (columnType == "public.vector") // Correctly handle pgvector
                    {
                        // var vector = reader.GetFieldValue<Vector>(i); // Read as Pgvector.Vector
                        row[columnName] = reader.GetFieldValue<Vector>(i); // Convert to float[]
                    }
                    else
                    {
                        row[columnName] = reader.GetValue(i);
                    }
                }
                result.Add(row);
            }
        }

        return result;
    }
}
