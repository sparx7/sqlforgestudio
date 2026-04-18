using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using Npgsql;
using MySql.Data.MySqlClient;
using System.Data.SQLite;
using Oracle.ManagedDataAccess.Client;

namespace SqlForgeWpf.Services
{
    public class DatabaseService
    {
        // =========================================================================
        // 1. DYNAMIC CONNECTION FACTORY (This is the method it couldn't find!)
        // =========================================================================
        public DbConnection GetConnection(string dbType, string host, string port, string dbName, string user, string pass)
        {
            return dbType.ToLower() switch
            {
                "postgresql" => new NpgsqlConnection($"Host={host};Port={port};Database={dbName};Username={user};Password={pass}"),
                "mysql" => new MySqlConnection($"Server={host};Port={port};Database={dbName};Uid={user};Pwd={pass};"),
                "sqlite" => new SQLiteConnection($"Data Source={host};Version=3;"), // For SQLite, Host acts as the file path
                "oracle" => new OracleConnection($"Data Source={host}:{port}/{dbName};User Id={user};Password={pass};"),
                _ => throw new Exception($"Unsupported Database Type: {dbType}")
            };
        }

        // =========================================================================
        // 2. QUERY EXECUTION
        // =========================================================================
        public async Task<DataTable> ExecuteSqlAsync(string dbType, string host, string port, string dbName, string user, string pass, string sql)
        {
            var dataTable = new DataTable();

            using var connection = GetConnection(dbType, host, port, dbName, user, pass);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = sql;

            using var reader = await command.ExecuteReaderAsync();
            dataTable.Load(reader);

            return dataTable;
        }

        // =========================================================================
        // 3. MULTI-DATABASE SCHEMA EXTRACTOR
        // =========================================================================
        public async Task<Dictionary<string, Dictionary<string, List<(string Name, string Type)>>>> GetDatabaseTreeAsync(
            string dbType, string host, string port, string dbName, string user, string pass)
        {
            var tree = new Dictionary<string, Dictionary<string, List<(string Name, string Type)>>>(StringComparer.OrdinalIgnoreCase);

            using var conn = GetConnection(dbType, host, port, dbName, user, pass);
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();

            // Craft the specific schema extraction query based on the database engine
            switch (dbType.ToLower())
            {
                case "postgresql":
                    cmd.CommandText = "SELECT table_schema, table_name, column_name, data_type FROM information_schema.columns WHERE table_schema NOT IN ('pg_catalog', 'information_schema') ORDER BY table_schema, table_name, ordinal_position;";
                    break;
                case "mysql":
                    cmd.CommandText = $"SELECT table_schema, table_name, column_name, data_type FROM information_schema.columns WHERE table_schema = '{dbName}' ORDER BY table_schema, table_name, ordinal_position;";
                    break;
                case "oracle":
                    // Exclude Oracle's Recycle Bin and System tables
                    cmd.CommandText = @"
        SELECT SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA') AS table_schema, table_name, column_name, data_type 
        FROM user_tab_columns 
        WHERE table_name NOT LIKE 'BIN$%' AND table_name NOT LIKE 'SYS_%'
        ORDER BY table_name, column_id";
                    break;
                case "sqlite":
                    cmd.CommandText = "SELECT 'main' AS table_schema, m.name AS table_name, p.name AS column_name, p.type AS data_type FROM sqlite_master m JOIN pragma_table_info(m.name) p WHERE m.type='table' ORDER BY m.name, p.cid;";
                    break;
                default:
                    throw new Exception($"Unsupported database type for tree generation: {dbType}");
            }

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                string schema = reader.GetString(0);
                string table = reader.GetString(1);
                string column = reader.GetString(2);
                string type = reader.GetString(3);

                if (!tree.ContainsKey(schema))
                    tree[schema] = new Dictionary<string, List<(string Name, string Type)>>(StringComparer.OrdinalIgnoreCase);

                if (!tree[schema].ContainsKey(table))
                    tree[schema][table] = new List<(string Name, string Type)>();

                tree[schema][table].Add((column, type));
            }
            return tree;
        }
    }
}