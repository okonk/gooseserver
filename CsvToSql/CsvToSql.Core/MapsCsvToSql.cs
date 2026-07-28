using CsvToSql.Core.Schema;

namespace CsvToSql
{
    public class MapsCsvToSql : CsvToSqlBase
    {
        public override Column[] GetColumnDescriptors() => new[]
        {
            Col.Id("map_id", SqlType.Integer).PrimaryKey(),
            Col.Text("map_name"),
            Col.Text("map_filename"),
            Col.Int("min_level", SqlType.SmallInt, def: 0),
            Col.Int("max_level", SqlType.SmallInt, def: 0),
            Col.Int("min_experience", SqlType.BigInt, def: 0),
            Col.Int("max_experience", SqlType.BigInt, def: 0),
            Col.Bool("pvp_enabled", def: false),
            Col.Bool("chat_enabled", def: true),
            Col.Bool("auction_enabled", def: true),
            Col.Bool("shout_enabled", def: true),
            Col.Bool("spells_enabled", def: true),
            Col.Bool("bind_enabled", def: false),
            Col.Bool("items_enabled", def: true),
            Col.Bool("pets_enabled", def: true),
            Col.Text("script_path", def: "''"),
            Col.Text("script_params", def: "''"),
        };
    }
}
