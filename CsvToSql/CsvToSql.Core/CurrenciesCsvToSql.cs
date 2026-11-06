using CsvToSql.Core.Schema;

namespace CsvToSql
{
    public class CurrenciesCsvToSql : CsvToSqlBase
    {
        public override Column[] GetColumnDescriptors() => new[]
        {
            Col.Text("currency_id").PrimaryKey(),
            Col.Text("name"),
            Col.Text("short_name", def: "''"),
            Col.Int("icon_sheet", SqlType.Int, def: 0),
            Col.Int("icon_graphic", SqlType.Int, def: 0),
            Col.Bool("vendor_buyback", def: false),
        };
    }
}
