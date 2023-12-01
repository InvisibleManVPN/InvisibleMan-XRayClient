using Microsoft.Data.Sqlite;

namespace InvisibleManXRay.Handlers.Databases
{
    using Models.Databases;
    using Values;

    public class SqliteDatabase
    {
        private const string CONNECTION_STRING = $"Data Source = {Path.CONFIGS_DB}; foreign keys=true";

        private SqliteConnection connection;
        private SqliteCommand command;

        public SqliteDatabase()
        {
            this.connection = new SqliteConnection(CONNECTION_STRING);
        }

        public void CreateTable(string tableName, Column[] columns, ForeignKey foreignKey = null)
        {
            string query = $"CREATE TABLE IF NOT EXISTS {tableName} (";
            for (int i = 0; i < columns.Length; i++)
            {
                query += $"{columns[i].Name} {columns[i].Type} {columns[i].Properties}";

                if (!IsLastElement())
                    query += ", ";

                bool IsLastElement() => i == columns.Length - 1;
            }

            if (IsForeignKeyExists())
                query += $", CONSTRAINT {foreignKey.KeyName} FOREIGN KEY ({foreignKey.FirstColumn}) " +
                    $"REFERENCES {foreignKey.SecondColumn} {foreignKey.Properties}";

            query += $")";

            ExecuteNonQuery(query);

            bool IsForeignKeyExists() => foreignKey != null;
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
            ExecuteNonQuery(query);
        }

        public void UpdateTable(string tableName, ColumnValue[] columnValues, string condition = "")
        {
            string query = $"UPDATE {tableName} SET ";

            for (int i = 0; i < columnValues.Length; i++)
            {
                query += $"'{columnValues[i].Column}' = '{columnValues[i].Value}'";

                if (!IsLastElement())
                    query += ", ";

                bool IsLastElement() => i == columnValues.Length - 1;
            }

            if (IsAnyConditionExists())
                query += $" WHERE ({condition})";

            ExecuteNonQuery(query);
            
            bool IsAnyConditionExists() => !string.IsNullOrEmpty(condition);
        }

        public void DeleteFromTable(string tableName, string condition = "")
        {
            string query = $"DELETE FROM {tableName}";

            if (IsAnyConditionExists())
                query += $" WHERE ({condition})";

            ExecuteNonQuery(query);

            bool IsAnyConditionExists() => !string.IsNullOrEmpty(condition);
        }

        public SqliteDataReader SelectFromTable(string tableName, string condition = "", string extraClauses = "")
        {
            string query = $"SELECT * FROM {tableName}";

            if (IsAnyConditionExists())
                query += $" WHERE ({condition})";
            
            if (IsAnyExtraClausesExists())
                query += $" {extraClauses}";
            
            System.Windows.MessageBox.Show(query);
            return ExecuteReader(query);
            
            bool IsAnyConditionExists() => !string.IsNullOrEmpty(condition);

            bool IsAnyExtraClausesExists() => !string.IsNullOrEmpty(extraClauses);
        }

        private void ExecuteNonQuery(string query)
        {
            OpenConnection();
            command = new SqliteCommand(query);
            command.Connection = connection;
            command.ExecuteNonQuery();
        }

        private SqliteDataReader ExecuteReader(string query)
        {
            OpenConnection();
            command = new SqliteCommand(query);
            command.Connection = connection;
            return command.ExecuteReader();
        }

        private void OpenConnection() => connection.Open();
    }
}