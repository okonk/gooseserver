using CsvToSql.Core.Schema;

namespace CsvToSql
{
    class ClassLevelupSpellsCsvToSql : CsvToSqlBase
    {
        public override Column[] GetColumnDescriptors() => new[]
        {
            Col.Id("class_id", SqlType.Int).Ref("Classes"),
            Col.Int("level", SqlType.SmallInt),
            Col.Id("spell_id", SqlType.Int).Ref("Spells"),
        };
    }
}
