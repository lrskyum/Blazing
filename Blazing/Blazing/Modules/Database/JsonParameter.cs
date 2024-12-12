using System.Data;
using Npgsql;
using NpgsqlTypes;
using static Dapper.SqlMapper;

namespace Blazing.Modules.Database;

/// <summary>
/// Dapper does not natively support jsonb. Instead use this class to add a jsonb parameter to a query.
/// See https://stackoverflow.com/a/57534990
/// </summary>
public class JsonParameter : ICustomQueryParameter
{
    private readonly string _value;

    public JsonParameter(string value)
    {
        _value = value;
    }

    public void AddParameter(IDbCommand command, string name)
    {
        var parameter = new NpgsqlParameter(name, NpgsqlDbType.Json);
        parameter.Value = _value;

        command.Parameters.Add(parameter);
    }
}
