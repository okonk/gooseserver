using CsvToSql.Core.Schema;

namespace CsvToSql
{
    public class QuestsCsvToSql : CsvToSqlBase
    {
        public override Column[] GetColumnDescriptors() => new[]
        {
            Col.Id("id", SqlType.Integer).PrimaryKey(),
            Col.Text("name"),
            Col.Text("description", def: "''"),
            // Worksheet order. The pre-descriptor schema listed fail_text before pass_text — do not
            // "fix" this to match it: cells are read positionally, so swapping these two
            // would write each column's value into the other.
            Col.Text("pass_text", def: "''"),
            Col.Text("fail_text", def: "''"),
            Col.Int("class_restrictions", SqlType.BigInt, def: 0).Nullable(),
            Col.Int("min_experience", SqlType.BigInt, def: 0).Nullable(),
            Col.Int("max_experience", SqlType.BigInt, def: 0).Nullable(),
            Col.Int("min_level", SqlType.Int, def: 0).Nullable(),
            Col.Int("max_level", SqlType.Int, def: 0).Nullable(),
            Col.Bool("repeatable", def: false).Nullable(),
            Col.Bool("show_progress", def: false).Nullable(),
            Col.Bool("only_one_player_can_complete", def: false).Nullable(),
            Col.Text("prerequisite_quests", def: "''"),
        };

        public override Composite[] GetComposites() => new[]
        {
            // Same column, same convention, same control as Items/Spells/Combinations: a set bit
            // means the class is ALLOWED (Goose/Class.cs:CanUse), the bit index is the class id,
            // and an all-zero mask means every class. Without this the editor renders it as a
            // bare number box and a designer has to hand-compute the mask.
            Composite.Bitmask("class_restrictions", from: "Classes"),
        };
    }
}
