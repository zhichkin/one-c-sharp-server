﻿using OneCSharp.Metadata;
using System;
using System.Linq;
using System.Text;

namespace OneCSharp.OQL.UI.Services
{
    public static class DbUtilities
    {
        public static int GetInt32(byte[] bytes)
        {
            byte[] value = new byte[4];
            bytes.CopyTo(value, 0);
            // If the system architecture is little-endian (that is, little end first), reverse the byte array.
            if (BitConverter.IsLittleEndian) Array.Reverse(value);
            return BitConverter.ToInt32(value, 0);
        }
        public static byte[] GetByteArray(int value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
            return bytes;
        }
        public static DbFieldPurpose ParseFieldPurpose(string fieldName)
        {
            char L = char.Parse("L");
            char N = char.Parse("N");
            char T = char.Parse("T");
            char S = char.Parse("S");
            char B = char.Parse("B");

            char test = fieldName[fieldName.Count() - 1];

            if (char.IsDigit(test)) return DbFieldPurpose.Value;

            if (test == L)
            {
                return DbFieldPurpose.Boolean;
            }
            else if (test == N)
            {
                return DbFieldPurpose.Numeric;
            }
            else if (test == T)
            {
                return DbFieldPurpose.DateTime;
            }
            else if (test == S)
            {
                return DbFieldPurpose.String;
            }
            else if (test == B)
            {
                return DbFieldPurpose.Binary;
            }

            string TYPE = "TYPE";
            string TRef = "TRef";
            string RRef = "RRef";

            string postfix = fieldName.Substring(fieldName.Count() - 4);

            if (postfix == TYPE)
            {
                return DbFieldPurpose.Discriminator;
            }
            else if (postfix == TRef)
            {
                return DbFieldPurpose.TypeCode;
            }
            else if (postfix == RRef)
            {
                return DbFieldPurpose.Object;
            }

            return DbFieldPurpose.Value;
        }
        public static bool IsAutoGeneratedValue(DbField field)
        {
            return (field.TypeName == "timestamp" || field.TypeName == "rowversion");
        }
        public static object GetDefaultValueAsObject(DbField field)
        {
            if (field.IsNullable)
            {
                return null;
            }
            else if (field.TypeName == "numeric"
                || field.TypeName == "decimal"
                || field.TypeName == "smallmoney"
                || field.TypeName == "money")
            {
                return 0;
            }
            else if (field.TypeName == "bit")
            {
                return false;
            }
            else if (field.TypeName == "tinyint")
            {
                return (byte)0;
            }
            else if (field.TypeName == "smallint")
            {
                return (short)0;
            }
            else if (field.TypeName == "int")
            {
                return 0;
            }
            else if (field.TypeName == "bigint")
            {
                return (long)0;
            }
            else if (field.TypeName == "float"
                || field.TypeName == "real")
            {
                return 0D;
            }
            else if (field.TypeName == "datetime"
                || field.TypeName == "date"
                || field.TypeName == "time"
                || field.TypeName == "datetime2"
                || field.TypeName == "datetimeoffset")
            {
                return new DateTime(1753, 1, 1);
            }
            else if (field.TypeName == "smalldatetime")
            {
                return new DateTime(1900, 1, 1);
            }
            else if (field.TypeName == "char"
                || field.TypeName == "varchar"
                || field.TypeName == "nchar"
                || field.TypeName == "nvarchar"
                || field.TypeName == "text"
                || field.TypeName == "ntext")
            {
                return string.Empty;
            }
            else if (field.TypeName == "binary")
            {
                return new byte[field.Length];
            }
            else if (field.TypeName == "varbinary"
                || field.TypeName == "image")
            {
                return Guid.Empty.ToByteArray();
            }
            else if (field.TypeName == "timestamp"
                || field.TypeName == "rowversion")
            {
                return null; // the value is auto generated by database engine
            }
            else if (field.TypeName == "uniqueidentifier")
            {
                return Guid.Empty;
            }
            else
            {
                return null;
            }
        }
        public static string GetDefaultValueAsString(DbField field)
        {
            if (field.IsNullable)
            {
                return null;
            }
            else if (field.TypeName == "numeric"
                || field.TypeName == "decimal"
                || field.TypeName == "smallmoney"
                || field.TypeName == "money"
                || field.TypeName == "float"
                || field.TypeName == "real"
                || field.TypeName == "tinyint"
                || field.TypeName == "smallint"
                || field.TypeName == "int"
                || field.TypeName == "bigint")
            {
                return "0";
            }
            else if (field.TypeName == "datetime"
                || field.TypeName == "date"
                || field.TypeName == "time"
                || field.TypeName == "datetime2"
                || field.TypeName == "smalldatetime"
                || field.TypeName == "datetimeoffset")
            {
                return "1753-01-01T00:00:00"; // SELECT CONVERT(nvarchar(19), @value, 126);
            }
            else if (field.TypeName == "nchar"
                || field.TypeName == "nvarchar"
                || field.TypeName == "ntext"
                || field.TypeName == "char"
                || field.TypeName == "varchar"
                || field.TypeName == "text")
            {
                return string.Empty;
            }
            else if (field.TypeName == "binary" && field.Purpose == DbFieldPurpose.Discriminator)
            {
                return "01"; // byte
            }
            else if (field.TypeName == "binary" && field.Length == 1)
            {
                return "00"; // boolean
            }
            else if (field.TypeName == "binary" && field.Length == 4)
            {
                return "00000000"; // int
            }
            else if (field.TypeName == "binary" && field.Length == 16)
            {
                return Guid.Empty.ToString().Replace("-", string.Empty); // GUID
            }
            else if (field.TypeName == "bit")
            {
                return "false";
            }
            else if (field.TypeName == "binary"
                || field.TypeName == "varbinary"
                || field.TypeName == "image")
            {
                return "00";
            }
            else if (field.TypeName == "timestamp"
                || field.TypeName == "rowversion")
            {
                return null; // the value is auto generated by database engine
            }
            else if (field.TypeName == "uniqueidentifier")
            {
                return Guid.Empty.ToString();
            }
            else
            {
                return null;
            }
        }
        public static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
            {
                hex.AppendFormat("{0:x2}", b);
            }
            return hex.ToString();
        }
        public static byte[] StringToByteArray(string hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return bytes;
        }
        public static string GetValueAsString(DbField field, object value)
        {
            if (value == null || value == DBNull.Value)
            {
                return null;
            }
            else if (value is DateTime)
            {
                DateTime d = (DateTime)value;
                StringBuilder sb = new StringBuilder();
                sb.Append(d.Year.ToString("D4"));
                sb.Append("-");
                sb.Append(d.Month.ToString("D2"));
                sb.Append("-");
                sb.Append(d.Day.ToString("D2"));
                sb.Append("T");
                sb.Append(d.Hour.ToString("D2"));
                sb.Append(":");
                sb.Append(d.Minute.ToString("D2"));
                sb.Append(":");
                sb.Append(d.Second.ToString("D2"));
                return sb.ToString(); // ISO 8601
            }
            else if (value is bool)
            {
                return (bool)value ? "true" : "false";
            }
            else if (value is byte[])
            {
                return ByteArrayToString((byte[])value);
            }
            else
            {
                return value.ToString();
            }
        }
        public static object GetValueFromString(DbField field, string value)
        {
            if (field.TypeName == "numeric"
                || field.TypeName == "decimal"
                || field.TypeName == "smallmoney"
                || field.TypeName == "money")
            {
                return decimal.Parse(value);
            }
            else if (field.TypeName == "bit")
            {
                return (value == "true");
            }
            else if (field.TypeName == "tinyint")
            {
                return byte.Parse(value);
            }
            else if (field.TypeName == "smallint")
            {
                return short.Parse(value);
            }
            else if (field.TypeName == "int")
            {
                return int.Parse(value);
            }
            else if (field.TypeName == "bigint")
            {
                return long.Parse(value);
            }
            else if (field.TypeName == "float")
            {
                return double.Parse(value);
            }
            else if (field.TypeName == "real")
            {
                return float.Parse(value);
            }
            else if (field.TypeName == "datetime"
                || field.TypeName == "date"
                || field.TypeName == "time"
                || field.TypeName == "datetime2"
                || field.TypeName == "smalldatetime"
                || field.TypeName == "datetimeoffset")
            {
                return DateTime.Parse(value);
            }
            else if (field.TypeName == "char"
                || field.TypeName == "varchar"
                || field.TypeName == "nchar"
                || field.TypeName == "nvarchar"
                || field.TypeName == "text"
                || field.TypeName == "ntext")
            {
                return value;
            }
            else if (field.TypeName == "binary"
                || field.TypeName == "varbinary"
                || field.TypeName == "timestamp"
                || field.TypeName == "rowversion"
                || field.TypeName == "image")
            {
                return StringToByteArray(value);
            }
            else if (field.TypeName == "uniqueidentifier")
            {
                return new Guid(value);
            }
            else
            {
                return null; // DBNull.Value;
            }
        }
    }
}
