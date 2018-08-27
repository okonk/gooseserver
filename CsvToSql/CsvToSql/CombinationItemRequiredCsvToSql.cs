﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CsvHelper;
using System.IO;

namespace CsvToSql
{
    class CombinationItemRequiredCsvToSql : CsvToSqlBase
    {
        protected override string[] GetColumns()
        {
            return new[] {
               "combination_id", "item_template_id"
            };
        }

        protected override string TransformValue(string columnName, string value)
        {
            switch (columnName)
            {
                default:
                    return value;
            }
        }
    }
}