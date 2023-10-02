using Microsoft.Data.Sqlite;

namespace InvisibleManXRay.Handlers.Databases
{
    using Models.Databases;
    using Values;

    public class SqliteDatabase
    {
        private const string CONNECTION_STRING = $"Data Source = {Path.CONFIGS_DB}";

        private SqliteConnection connection;
        private SqliteCommand command;

        public SqliteDatabase()
        {
            this.connection = new SqliteConnection(CONNECTION_STRING);
        }

        public void CreateTable(string tableName, Column[] columns)
        {
            string query = $"CREATE TABLE IF NOT EXISTS {tableName} (";
            for (int i = 0; i < columns.Length; i++)
            {
                query += $"{columns[i].Name} {columns[i].Type} {columns[i].Properties}";

                if (!IsLastElement())
                    query += ", ";

                bool IsLastElement() => i == columns.Length - 1;
            }
            query += ")";
            Execute(query);
        }

        public void InsertIntoTable(string tableName, ColumnValue[] columnValues)
        {
            string columns = "";
            string values = "";

            for (int i = 0; i < columnValues.Length; i++)
            {
                columns += $"'{columnValues[i].Column}'";
                values += $"'{columnValues[i].Value}'";

                if (!IsLastElement())
                {
                    columns += ", ";
                    values += ", ";
                }

                bool IsLastElement() => i == columnValues.Length - 1;
            }

            string query = $"INSERT INTO {tableName} ({columns}) VALUES ({values})";
            Execute(query);
        }

        private void Execute(string query)
        {
            OpenConnection();
            command = new SqliteCommand(query);
            command.Connection = connection;
            command.ExecuteNonQuery();
        }

        private void OpenConnection() => connection.Open();
    }
}