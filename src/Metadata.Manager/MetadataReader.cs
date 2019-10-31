﻿using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace OneCSharp.Metadata
{
    internal sealed class DBName
    {
        internal string Token;
        internal int TypeCode;
    }
    
    internal sealed class MetadataReader
    {
        private ILogger _logger;
        internal string ConnectionString { get; set; }
        private void WriteBinaryDataToFile(Stream binaryData, string fileName)
        {
            string filePath = Path.Combine(_logger.CatalogPath, fileName);
            using (FileStream output = File.Create(filePath))
            {
                binaryData.CopyTo(output);
            }
        }
        internal void UseLogger(ILogger logger) { _logger = logger; }
        internal List<InfoBase> GetInfoBases()
        {
            List<InfoBase> list = new List<InfoBase>();

            { // limited scope for variables declared in it - using statement does like that - used here to get control over catch block
                SqlConnection connection = new SqlConnection(ConnectionString);
                SqlCommand command = connection.CreateCommand();
                SqlDataReader reader = null;
                command.CommandType = CommandType.Text;
                command.CommandText = "SELECT database_id, name FROM sys.databases WHERE name NOT IN ('master', 'model', 'msdb', 'tempdb', 'Resource', 'distribution', 'reportserver', 'reportservertempdb');";
                try
                {
                    connection.Open();
                    reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        list.Add(new InfoBase()
                        {
                            Name = string.Empty,
                            Database = reader.GetString(1)
                        });
                    }
                }
                catch (Exception error)
                {
                    // TODO: log error
                }
                finally
                {
                    if (reader != null)
                    {
                        if (reader.HasRows) command.Cancel();
                        reader.Dispose();
                    }
                    if (command != null) command.Dispose();
                    if (connection != null) connection.Dispose();
                }
            } // end of limited scope

            return list;
        }

        # region " Read DBNames "
        internal void ReadDBNames(Dictionary<string, List<DBName>> DBNames)
        {
            SqlBytes binaryData = GetDBNamesFromDatabase();
            if (binaryData == null) return;

            DeflateStream stream = new DeflateStream(binaryData.Stream, CompressionMode.Decompress);

            if (_logger == null)
            {
                ParseDBNames(stream, DBNames);
            }
            else
            {
                MemoryStream memory = new MemoryStream();
                stream.CopyTo(memory);
                memory.Seek(0, SeekOrigin.Begin);
                WriteBinaryDataToFile(memory, "DBNames.txt");
                memory.Seek(0, SeekOrigin.Begin);
                ParseDBNames(memory, DBNames);
            }
        }
        private SqlBytes GetDBNamesFromDatabase()
        {
            SqlBytes binaryData = null;

            { // limited scope for variables declared in it - using statement does like that - used here to get control over catch block
                SqlConnection connection = new SqlConnection(ConnectionString);
                SqlCommand command = connection.CreateCommand();
                SqlDataReader reader = null;
                command.CommandType = CommandType.Text;
                command.CommandText = "SELECT BinaryData FROM Params WHERE FileName = N'DBNames'";
                try
                {
                    connection.Open();
                    reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        binaryData = reader.GetSqlBytes(0);
                    }
                }
                catch (Exception error)
                {
                    // TODO: log error
                }
                finally
                {
                    if (reader != null)
                    {
                        if (reader.HasRows) command.Cancel();
                        reader.Dispose();
                    }
                    if (command != null) command.Dispose();
                    if (connection != null) connection.Dispose();
                }
            } // end of limited scope

            return binaryData;
        }
        private void ParseDBNames(Stream stream, Dictionary<string, List<DBName>> DBNames)
        {
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                string line = reader.ReadLine();
                if (line != null)
                {
                    int capacity = GetDBNamesCapacity(line);
                    while ((line = reader.ReadLine()) != null)
                    {
                        ParseDBNameLine(line, DBNames);
                    }
                }
            }
        }
        private int GetDBNamesCapacity(string line)
        {
            return int.Parse(line.Replace("{", string.Empty).Replace(",", string.Empty));
        }
        private void ParseDBNameLine(string line, Dictionary<string, List<DBName>> DBNames)
        {
            string[] items = line.Split(',');
            if (items.Length < 3) return;

            string FileName = items[0].Replace("{", string.Empty);
            DBName dbname = new DBName()
            {
                Token = items[1].Replace("\"", string.Empty),
                TypeCode = int.Parse(items[2].Replace("}", string.Empty))
            };
            
            if (DBNames.TryGetValue(FileName, out List<DBName> dbnames))
            {
                dbnames.Add(dbname);
            }
            else
            {
                DBNames.Add(FileName, new List<DBName>() { dbname });
            }
        }
        # endregion

        #region " Read DBSchema "
        //internal sealed class DBObject // Regex("^{\"\\w+\",\"[NI]\",\\d+,\"\\w*\","); // Example: {"Reference42","N",42,"", | {"VT5798","I",0,"Reference228",
        //{
        //    internal int ID;
        //    internal string Name;
        //    internal string Token;
        //    internal string FileName;
        //    internal string Owner; // for table parts (I token)
        //    internal List<DBProperty> Properties = new List<DBProperty>();
        //    internal List<DBObject> NestedObjects = new List<DBObject>(); // Nested tables
        //}
        //internal sealed class DBProperty // Regex("^{\"\\w+\",0,"); // Example: {"Period",0,
        //{
        //    internal int ID;
        //    internal string Name;
        //    internal string Token;
        //    internal string FileName;
        //    internal List<DBType> Types = new List<DBType>();
        //}
        //internal sealed class DBType // Regex("^{\"[ENLVBSTR]\",\\d+,\\d+,\"\\w*\",\\d}"); // Example: {"N",5,0,"",0} | {"R",0,0,"Reference188",3}
        //{
        //    internal string Token; // [BENLVSTR]
        //    internal int Size;
        //    internal int Tail;
        //    internal string Name; // reference types only & if not compound (E token)
        //    internal int Kind; // ??? used usually with references
        //}
        //public List<DBObject> ReadDBSchema(string connectionString, DBName[] dbnames)
        //{
        //    SqlBytes binaryData = GetDBSchemaFromDatabase(connectionString);
        //    if (binaryData == null) return null;

        //    WriteBinaryDataToFile(binaryData.Stream, "DBSchema.txt");

        //    binaryData.Stream.Seek(0, SeekOrigin.Begin);
        //    return ParseDBSchema(binaryData.Stream, dbnames);
        //}
        //private SqlBytes GetDBSchemaFromDatabase(string connectionString)
        //{
        //    SqlBytes binaryData = null;

        //    { // limited scope for variables declared in it - using statement does like that - used here to get control over catch block
        //        SqlConnection connection = new SqlConnection(connectionString);
        //        SqlCommand command = connection.CreateCommand();
        //        SqlDataReader reader = null;
        //        command.CommandType = CommandType.Text;
        //        command.CommandText = "SELECT TOP 1 SerializedData FROM DBSchema";
        //        try
        //        {
        //            connection.Open();
        //            reader = command.ExecuteReader();
        //            if (reader.Read())
        //            {
        //                binaryData = reader.GetSqlBytes(0);
        //            }
        //        }
        //        catch (Exception error)
        //        {
        //            // TODO: log error
        //        }
        //        finally
        //        {
        //            if (reader != null)
        //            {
        //                if (reader.HasRows) command.Cancel();
        //                reader.Dispose();
        //            }
        //            if (command != null) command.Dispose();
        //            if (connection != null) connection.Dispose();
        //        }
        //    } // end of limited scope

        //    return binaryData;
        //}
        //private List<DBObject> ParseDBSchema(Stream stream, DBName[] dbnames)
        //{
        //    List<DBObject> result = new List<DBObject>();

        //    Match match = null;
        //    Regex DBTypeRegex = new Regex("^{\"[ENLVBSTR]\",\\d+,\\d+,\"\\w*\",\\d(?:,\\d)?}"); // Example: {"N",5,0,"",0} | {"N",10,0,"",0,1} | {"R",0,0,"Reference188",3}
        //    Regex DBObjectRegex = new Regex("^{\"\\w+\",\"[NI]\",\\d+,\"\\w*\","); // Example: {"Reference42","N",42,"", | {"VT5798","I",0,"Reference228",
        //    Regex DBPropertyRegex = new Regex("^{\"\\w{2,}\",\\d,$"); // Example: {"Period",0, | {"Fld1795",1,

        //    using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
        //    {
        //        string line = null;
        //        int state = 0;
        //        int typesCount = 0;
        //        int propertiesCount = 0;
        //        DBObject currentObject = null;
        //        DBObject currentTable = null;
        //        DBProperty currentProperty = null;
        //        while ((line = reader.ReadLine()) != null)
        //        {
        //            if (state == 0) // waiting for the next DBObject
        //            {
        //                match = DBObjectRegex.Match(line);
        //                if (match.Success)
        //                {
        //                    DBObject dbo = ParseDBObject(line, dbnames);
        //                    if (dbo.Owner == string.Empty)
        //                    {
        //                        currentObject = dbo;
        //                        result.Add(currentObject);
        //                        state = 1;
        //                    }
        //                    else // Nested object - table part
        //                    {
        //                        currentTable = dbo;
        //                        currentObject.NestedObjects.Add(currentTable);
        //                        state = 3;
        //                    }
        //                }
        //            }
        //            else if (state == 1) // reading DBObject's properties
        //            {
        //                propertiesCount = int.Parse(line.Replace("{", string.Empty).Replace(",", string.Empty));
        //                state = 2;
        //            }
        //            else if (state == 2) // waiting for the next DBProperty
        //            {
        //                if (propertiesCount == 0)
        //                {
        //                    state = 0; // waiting for the next DBObject
        //                }
        //                else
        //                {
        //                    match = DBPropertyRegex.Match(line);
        //                    if (match.Success)
        //                    {
        //                        currentProperty = ParseDBProperty(line, dbnames);
        //                        currentObject.Properties.Add(currentProperty);
        //                        propertiesCount--;
        //                        state = 5;
        //                    }
        //                }
        //            }
        //            else if (state == 3) // reading DBObject's properties
        //            {
        //                propertiesCount = int.Parse(line.Replace("{", string.Empty).Replace(",", string.Empty));
        //                state = 4;
        //            }
        //            else if (state == 4) // waiting for the next DBProperty
        //            {
        //                if (propertiesCount == 0)
        //                {
        //                    state = 0; // waiting for the next DBObject
        //                    currentTable = null;
        //                }
        //                else
        //                {
        //                    match = DBPropertyRegex.Match(line);
        //                    if (match.Success)
        //                    {
        //                        currentProperty = ParseDBProperty(line, dbnames);
        //                        currentTable.Properties.Add(currentProperty);
        //                        propertiesCount--;
        //                        state = 5;
        //                    }
        //                }
        //            }
        //            else if (state == 5) // reading DBProperty's types
        //            {
        //                typesCount = int.Parse(line.Replace("{", string.Empty).Replace(",", string.Empty));
        //                state = 6;
        //            }
        //            else if (state == 6) // waiting for the next DBType
        //            {
        //                match = DBTypeRegex.Match(line);
        //                if (match.Success)
        //                {
        //                    DBType propertyType = ParseDBType(line);
        //                    currentProperty.Types.Add(propertyType);
        //                    typesCount--;
        //                    if (typesCount == 0)
        //                    {
        //                        if (currentTable == null)
        //                        {
        //                            state = 2;
        //                        }
        //                        else
        //                        {
        //                            state = 4;
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    return result;
        //}
        //private DBObject ParseDBObject(string line, DBName[] dbnames)
        //{
        //    string[] items = line.Split(',');
        //    DBObject dbo = new DBObject()
        //    {
        //        Name = items[0].Replace("{", string.Empty).Replace("\"", string.Empty),
        //        ID = int.Parse(items[2]),
        //        Owner = items[3].Replace("\"", string.Empty),
        //    };
        //    if (dbo.Owner == string.Empty) 
        //    {
        //        dbo.Token = dbo.Name.Replace(dbo.ID.ToString(), string.Empty);
        //        DBName dbname = dbnames[dbo.ID];
        //        if (dbo.Token == dbname.Token)
        //        {
        //            dbo.FileName = dbname.FileName;
        //        }
        //    }
        //    else // nested object table part
        //    {
        //        Match match = Regex.Match(dbo.Name, "\\d+");
        //        if (match.Success)
        //        {
        //            dbo.ID = int.Parse(match.Value);
        //            dbo.Token = dbo.Name.Replace(dbo.ID.ToString(), string.Empty);
        //            DBName dbname = dbnames[dbo.ID];
        //            if (dbo.Token == dbname.Token)
        //            {
        //                dbo.FileName = dbname.FileName;
        //            }
        //        }
        //        else // system property
        //        {
        //            dbo.ID = 0;
        //            dbo.FileName = string.Empty;
        //        }
        //    }
        //    return dbo;
        //}
        //private DBProperty ParseDBProperty(string line, DBName[] dbnames)
        //{
        //    string[] items = line.Split(',');
        //    DBProperty dbp = new DBProperty()
        //    {
        //        Name = items[0].Replace("{", string.Empty).Replace("\"", string.Empty)
        //    };
        //    Match match = Regex.Match(dbp.Name, "\\d+");
        //    if (match.Success && !dbp.Name.Contains("DimUse"))
        //    {
        //        dbp.ID = int.Parse(match.Value);
        //        dbp.Token = dbp.Name.Replace(dbp.ID.ToString(), string.Empty);
        //        DBName dbname = dbnames[dbp.ID];
        //        if (dbp.Token == dbname.Token)
        //        {
        //            dbp.FileName = dbname.FileName;
        //        }
        //    }
        //    else // system property
        //    {
        //        dbp.ID = 0;
        //        dbp.FileName = string.Empty;
        //    }
        //    return dbp;
        //}
        //private DBType ParseDBType(string line)
        //{
        //    string[] items = line.Split(',');
        //    DBType dbt = new DBType()
        //    {
        //        Token = items[0].Replace("{", string.Empty).Replace("\"", string.Empty),
        //        Size = (items[1].Length == 10) ? int.MaxValue : int.Parse(items[1]),
        //        Tail = int.Parse(items[2]),
        //        Name = items[3].Replace("\"", string.Empty),
        //        Kind = int.Parse(items[4].Replace("}",string.Empty))
        //    };
        //    return dbt;
        //}
        #endregion

        #region " Read Config "
        //public List<MetadataObject> ReadConfig(string connectionString, List<DBObject> dbobjects)
        //{
        //    List<MetadataObject> result = new List<MetadataObject>();
        //    foreach (DBObject dbo in dbobjects)
        //    {
        //        SqlBytes binaryData = ReadConfigFromDatabase(connectionString, dbo.FileName);
        //        if (binaryData == null)
        //        {
        //            Logger.WriteEntry($"{dbo.Name} file not found {dbo.FileName}");
        //            continue;
        //        }

        //        DeflateStream stream = new DeflateStream(binaryData.Stream, CompressionMode.Decompress);
        //        MemoryStream memory = new MemoryStream();
        //        stream.CopyTo(memory);

        //        memory.Seek(0, SeekOrigin.Begin);
        //        WriteBinaryDataToFile(memory, $"config\\{dbo.FileName}.txt");

        //        memory.Seek(0, SeekOrigin.Begin);
        //        MetadataObject mo = ParseMetadataObject(memory, dbo);
        //        if (mo == null)
        //        {
        //            Logger.WriteEntry($"{dbo.Name} could not create MetadataObject {dbo.FileName}");
        //            continue;
        //        }
        //        result.Add(mo);
        //    }
        //    return result;
        //}
        //private SqlBytes ReadConfigFromDatabase(string connectionString, string fileName)
        //{
        //    SqlBytes binaryData = null;

        //    { // limited scope for variables declared in it - using statement does like that - used here to get control over catch block
        //        SqlConnection connection = new SqlConnection(connectionString);
        //        SqlCommand command = connection.CreateCommand();
        //        SqlDataReader reader = null;
        //        command.CommandType = CommandType.Text;
        //        command.CommandText = "SELECT BinaryData FROM Config WHERE FileName = @FileName ORDER BY PartNo ASC";
        //        command.Parameters.AddWithValue("FileName", fileName);
        //        try
        //        {
        //            connection.Open();
        //            reader = command.ExecuteReader();
        //            while (reader.Read())
        //            {
        //                binaryData = reader.GetSqlBytes(0);
        //            }
        //        }
        //        catch (Exception error)
        //        {
        //            // TODO: log error
        //        }
        //        finally
        //        {
        //            if (reader != null)
        //            {
        //                if (reader.HasRows) command.Cancel();
        //                reader.Dispose();
        //            }
        //            if (command != null) command.Dispose();
        //            if (connection != null) connection.Dispose();
        //        }
        //    } // end of limited scope

        //    return binaryData;
        //}
        //private MetadataObject ParseMetadataObject(Stream stream, DBObject dbo)
        //{
        //    MetadataObject mo = new MetadataObject()
        //    {
        //        Token = dbo.Token,
        //        Table = $"_{dbo.Name}"
        //    };

        //    string UUID = null;
        //    string name = null;
        //    Regex regex = new Regex("^{\\d,\\d,[a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12}},\"\\w+\",$"); // Example: {0,0,eb3dfdc7-58b8-4b1f-b079-368c262364c9},"ВерсииФайлов",
        //    using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
        //    {
        //        string line = null;

        //        Match match = null;
        //        while ((line = reader.ReadLine()) != null)
        //        {
        //            match = regex.Match(line);
        //            if (!match.Success) continue;

        //            string[] lines = line.Split(',');
        //            UUID = lines[2].Replace("}", string.Empty);
        //            name = lines[3].Replace("\"", string.Empty);
        //            SetNameByUUID(mo, dbo, UUID, name);
        //        }
        //    }
        //    return mo;
        //}
        //private void SetNameByUUID(MetadataObject metaObject, DBObject dbo, string UUID, string name)
        //{
        //    if (string.IsNullOrEmpty(metaObject.Name))
        //    {
        //        if (dbo.FileName == UUID)
        //        {
        //            metaObject.Name = name;
        //            return;
        //        }
        //    }

        //    foreach (DBProperty property in dbo.Properties)
        //    {
        //        if (property.FileName == UUID)
        //        {
        //            MetadataProperty p = new MetadataProperty()
        //            {
        //                Name = name,
        //                SDBL = property.Name
        //            };
        //            //p.Fields.Add(new MetadataField() { Name = $"_{property.Name}" });
        //            metaObject.Properties.Add(p);
        //            return;
        //        }
        //    }

        //    foreach (DBObject table in dbo.NestedObjects)
        //    {
        //        if (table.FileName == UUID)
        //        {
        //            MetadataObject t = new MetadataObject()
        //            {
        //                Name = name,
        //                Token = table.Token,
        //                Table = $"{metaObject.Table}_{table.Name}"
        //            };
        //            metaObject.NestedObjects.Add(t);
        //            return;
        //        }

        //        foreach (DBProperty property in table.Properties)
        //        {
        //            if (property.FileName == UUID)
        //            {
        //                MetadataProperty p = new MetadataProperty()
        //                {
        //                    Name = name,
        //                    SDBL = property.Name
        //                };
        //                //p.Fields.Add(new MetadataField() { Name = $"_{property.Name}" });
        //                MetadataObject nested = metaObject.NestedObjects.Find(i => i.Table == table.Name);
        //                if (nested == null)
        //                {
        //                    continue;
        //                }
        //                else
        //                {
        //                    nested.Properties.Add(p);
        //                }
        //                return;
        //            }
        //        }
        //    }
        //}
        #endregion

        //public void ReadSQLMetadata(string connectionString, List<MetadataObject> metaObjects)
        //{
        //    SQLHelper SQL = new SQLHelper();
        //    SQL.Load(connectionString, metaObjects);
        //}
    }
}
