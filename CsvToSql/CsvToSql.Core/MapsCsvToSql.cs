using CsvToSql.Core.Schema;

namespace CsvToSql
{
    public class MapsCsvToSql : CsvToSqlBase
    {
        public override Column[] GetColumnDescriptors() => new[]
        {
            Col.Id("map_id", SqlType.Integer).PrimaryKey().HeaderText("id"),
            Col.Text("map_name").HeaderText("name"),
            Col.Text("map_filename").HeaderText("filename"),
            Col.Int("min_level", SqlType.SmallInt, def: 0).HeaderText("min_level (0)"),
            Col.Int("max_level", SqlType.SmallInt, def: 0).HeaderText("max_level (0)"),
            Col.Int("min_experience", SqlType.BigInt, def: 0).HeaderText("min_experience (0)"),
            Col.Int("max_experience", SqlType.BigInt, def: 0).HeaderText("max_experience (0)"),
            Col.Bool("pvp_enabled", def: false).HeaderText("pvp_enabled (0)"),
            Col.Bool("chat_enabled", def: true).HeaderText("chat_enabled (1)"),
            Col.Bool("auction_enabled", def: true).HeaderText("auction_enabled (1)"),
            Col.Bool("shout_enabled", def: true).HeaderText("shout_enabled (1)"),
            Col.Bool("spells_enabled", def: true).HeaderText("spells_enabled (1)"),
            Col.Bool("bind_enabled", def: false).HeaderText("bind_enabled (0)"),
            Col.Bool("items_enabled", def: true).HeaderText("items_enabled (1)"),
            Col.Bool("pets_enabled", def: true).HeaderText("pets_enabled (1)"),
            Col.Text("script_path", def: "''").HeaderText("script path"),
            Col.Text("script_params", def: "''").HeaderText("script params"),
        };
    }
}
