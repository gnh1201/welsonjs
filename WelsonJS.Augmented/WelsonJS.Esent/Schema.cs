// Schema.cs (WelsonJS.Esent)
// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2025 Namhyeon Go <gnh1201@catswords.re.kr>, Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
// 
using System;
using System.Collections.Generic;

namespace WelsonJS.Esent
{
    public class Schema
    {
        public string TableName { get; set; }
        public List<Column> Columns { get; set; }
        public Column PrimaryKey
        {
            get
            {
                return Columns.Find(c => c.IsPrimaryKey) ?? null;
            }
        }

        public Schema(string tableName, List<Column> columns)
        {
            TableName = tableName;
            Columns = columns ?? new List<Column>();
        }

        public void SetPrimaryKey(string columnName)
        {
            Column column = Columns.Find(c => c.Name.Equals(columnName, StringComparison.OrdinalIgnoreCase));
            if (column != null)
            {
                column.IsPrimaryKey = true;
            }
            else
            {
                throw new ArgumentException($"Column '{columnName}' does not exist in schema '{TableName}'.");
            }
        }
    }
}