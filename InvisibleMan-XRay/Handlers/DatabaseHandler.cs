using System;

namespace InvisibleManXRay.Handlers
{
    using Models.Databases;
    using Databases;
    using Utilities;
    using Values;

    public class DatabaseHandler : Handler
    {
        private SqliteDatabase sqliteDatabase;

        public DatabaseHandler()
        {
            this.sqliteDatabase = new SqliteDatabase();

            if (!IsDatabaseExists())
                InitializeDatabase();
        }

        bool IsDatabaseExists() => FileUtility.IsFileExists(Path.CONFIGS_DB);

        private void InitializeDatabase()
        {
            CreateConfigsTable();
            CreateGroupsTable();
            InsertGeneralGroupIntoGroupsTable();

            void CreateConfigsTable()
            {
                sqliteDatabase.CreateTable(
                    tableName: "Configs",
                    columns: new[] {
                        new Column("id", "INTEGER", "PRIMARY KEY AUTOINCREMENT"),
                        new Column("group_id", "INTEGER", "NOT NULL"),
                        new Column("name", "TEXT", "NOT NULL"),
                        new Column("type", "TEXT", "NOT NULL"),
                        new Column("value", "TEXT", "NOT NULL"),
                        new Column("created_at", "TEXT", "NOT NULL")
                    }
                );
            }

            void CreateGroupsTable()
            {
                sqliteDatabase.CreateTable(
                    tableName: "Groups",
                    columns: new[] {
                        new Column("id", "INTEGER", "PRIMARY KEY AUTOINCREMENT"),
                        new Column("name", "TEXT", "NOT NULL"),
                        new Column("url", "TEXT"),
                        new Column("created_at", "TEXT", "NOT NULL")
                    }
                );
            }

            void InsertGeneralGroupIntoGroupsTable()
            {
                sqliteDatabase.InsertIntoTable(
                    tableName: "Groups",
                    columnValues: new[] {
                        new ColumnValue("name", "general"),
                        new ColumnValue("created_at", GetCurrentTime())
                    }
                );
            }
        }

        private string GetCurrentTime() => DateTime.Now.ToShortDateString();
    }
}