using System.Data.Common;
using System.Globalization;

namespace EasyBill.DataAccess.StoredProcedures
{
    public static class SqlDataReaderExtensions
    {
        public static bool HasColumn(this DbDataReader reader, string columnName)
        {
            for (var i = 0; i < reader.FieldCount; i++)
            {
                if (string.Equals(reader.GetName(i), columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public static string? ReadNullableString(this DbDataReader reader, string columnName)
        {
            var ordinal = GetOrdinal(reader, columnName);
            return ordinal >= 0 && !reader.IsDBNull(ordinal) ? reader.GetString(ordinal) : null;
        }

        public static int ReadInt32(this DbDataReader reader, string columnName)
        {
            var ordinal = GetOrdinal(reader, columnName);
            return ordinal >= 0 && !reader.IsDBNull(ordinal) ? reader.GetInt32(ordinal) : 0;
        }

        public static int? ReadNullableInt32(this DbDataReader reader, string columnName)
        {
            var ordinal = GetOrdinal(reader, columnName);
            return ordinal >= 0 && !reader.IsDBNull(ordinal) ? reader.GetInt32(ordinal) : null;
        }

        public static decimal ReadDecimal(this DbDataReader reader, string columnName)
        {
            var ordinal = GetOrdinal(reader, columnName);
            if (ordinal < 0 || reader.IsDBNull(ordinal))
            {
                return 0M;
            }

            // Previous direct decimal cast kept commented for reference.
            // return reader.GetDecimal(ordinal);

            var value = reader.GetValue(ordinal);

            return value switch
            {
                decimal decimalValue => decimalValue,
                int intValue => intValue,
                long longValue => longValue,
                short shortValue => shortValue,
                byte byteValue => byteValue,
                float floatValue => Convert.ToDecimal(floatValue, CultureInfo.InvariantCulture),
                double doubleValue => Convert.ToDecimal(doubleValue, CultureInfo.InvariantCulture),
                _ => Convert.ToDecimal(value, CultureInfo.InvariantCulture)
            };
        }

        public static DateTime? ReadNullableDateTime(this DbDataReader reader, string columnName)
        {
            var ordinal = GetOrdinal(reader, columnName);
            return ordinal >= 0 && !reader.IsDBNull(ordinal) ? reader.GetDateTime(ordinal) : null;
        }

        private static int GetOrdinal(DbDataReader reader, string columnName)
        {
            for (var i = 0; i < reader.FieldCount; i++)
            {
                if (string.Equals(reader.GetName(i), columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
