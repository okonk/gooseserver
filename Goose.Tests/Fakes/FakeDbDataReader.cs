using System.Collections;
using System.Data.Common;

namespace Goose.Tests.Fakes;

/// <summary>Drives the various FromReader methods, which only ever index by column name.
/// The extension helpers (GetInt32/GetString etc.) route through the name indexer and do not throw;
/// only direct typed/ordinal accessor calls fail loudly rather than silently return a default.</summary>
public sealed class FakeDbDataReader : DbDataReader
{
    private readonly Dictionary<string, object> values;

    public FakeDbDataReader(Dictionary<string, object> values) => this.values = values;

    public override object this[string name] => values[name];

    public override int FieldCount => values.Count;
    public override bool HasRows => true;
    public override bool IsClosed => false;
    public override int Depth => 0;
    public override int RecordsAffected => 0;
    public override bool NextResult() => false;
    public override bool Read() => false;

    public override object this[int ordinal] => throw new NotSupportedException();
    public override bool GetBoolean(int i) => throw new NotSupportedException();
    public override byte GetByte(int i) => throw new NotSupportedException();
    public override long GetBytes(int i, long o, byte[]? b, int bo, int l) => throw new NotSupportedException();
    public override char GetChar(int i) => throw new NotSupportedException();
    public override long GetChars(int i, long o, char[]? b, int bo, int l) => throw new NotSupportedException();
    public override string GetDataTypeName(int i) => throw new NotSupportedException();
    public override DateTime GetDateTime(int i) => throw new NotSupportedException();
    public override decimal GetDecimal(int i) => throw new NotSupportedException();
    public override double GetDouble(int i) => throw new NotSupportedException();
    public override Type GetFieldType(int i) => throw new NotSupportedException();
    public override float GetFloat(int i) => throw new NotSupportedException();
    public override Guid GetGuid(int i) => throw new NotSupportedException();
    public override short GetInt16(int i) => throw new NotSupportedException();
    public override int GetInt32(int i) => throw new NotSupportedException();
    public override long GetInt64(int i) => throw new NotSupportedException();
    public override string GetName(int i) => throw new NotSupportedException();
    public override int GetOrdinal(string name) => throw new NotSupportedException();
    public override string GetString(int i) => throw new NotSupportedException();
    public override object GetValue(int i) => throw new NotSupportedException();
    public override int GetValues(object[] values) => throw new NotSupportedException();
    public override bool IsDBNull(int i) => throw new NotSupportedException();
    public override IEnumerator GetEnumerator() => throw new NotSupportedException();
}
