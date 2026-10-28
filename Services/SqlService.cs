using Microsoft.Data.SqlClient;
using System.Data;

namespace ukg_wfm_sync.Services;

public class SqlService
{
    private readonly string _connectionString = Environment.GetEnvironmentVariable("SQL_CONNECTION_STRING")!;

    public async Task ExecuteAsync(string sql)
    {
        await using SqlConnection conn = new(_connectionString);
        await conn.OpenAsync();

        await using SqlCommand cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        _ = await cmd.ExecuteNonQueryAsync();
    }

    public async Task<DataTable> QueryAsync(string sql)
    {
        DataTable table = new();

        await using SqlConnection conn = new(_connectionString);
        await conn.OpenAsync();

        await using SqlCommand cmd = conn.CreateCommand();
        cmd.CommandText = sql;

        await using SqlDataReader reader = await cmd.ExecuteReaderAsync();
        table.Load(reader);

        return table;
    }

    public async Task BulkInsertAsync(string tableName, DataTable table)
    {
        await using SqlConnection conn = new(_connectionString);
        await conn.OpenAsync();

        using SqlBulkCopy bulk = new(conn);
        bulk.DestinationTableName = tableName;

        await bulk.WriteToServerAsync(table);
    }
}
