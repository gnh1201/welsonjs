// DataStore.cs (WelsonJS.Esent)
// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2025 Namhyeon Go, Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
// 
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Isam.Esent.Interop;

namespace WelsonJS.Esent
{
    public class DataStore : IDisposable
    {
        private const string _primaryKeyindexName = "primary";
        private const string _indexNamePrefix = "idx_";

        private static readonly object _lock = new object();
        private static bool _initialized = false;
        private static Instance _instance;
        private static string _workingDirectory;
        private static string _filePath;

        private readonly Session _session;
        private readonly JET_DBID _dbid;
        private readonly Schema _schema;
        private readonly Column _primaryKey;
        private readonly Dictionary<string, JET_COLUMNID> _columnIds;

        public DataStore(Schema schema, string workingDirectory)
        {
            _primaryKey = schema.PrimaryKey;

            if (schema == null)
                throw new ArgumentNullException(nameof(schema));

            if (_primaryKey == null)
                throw new ArgumentNullException();

            if (!schema.Columns.Exists(c => c == _primaryKey))
                throw new ArgumentException($"Primary key '{_primaryKey.Name}' is not in schema.");

            _workingDirectory = workingDirectory;
            _schema = schema;
            _columnIds = new Dictionary<string, JET_COLUMNID>(StringComparer.OrdinalIgnoreCase);

            InitializeInstance();

            _session = new Session(_instance);

            if (!File.Exists(_filePath))
            {
                Api.JetCreateDatabase(_session, _filePath, null, out _dbid, CreateDatabaseGrbit.None);
                CreateTable(_schema);
            }
            else
            {
                Api.JetAttachDatabase(_session, _filePath, AttachDatabaseGrbit.None);
                Api.JetOpenDatabase(_session, _filePath, null, out _dbid, OpenDatabaseGrbit.None);
            }

            CacheColumns();
        }

        private static void InitializeInstance()
        {
            if (_initialized) return;

            lock (_lock)
            {
                if (_initialized) return;

                // set the file path
                _filePath = Path.Combine(_workingDirectory, "metadata.edb");

                // config the instance
                _instance = new Instance("WelsonJS.Launcher.MetadataStore");
                _instance.Parameters.SystemDirectory = _workingDirectory;
                _instance.Parameters.LogFileDirectory = _workingDirectory;
                _instance.Parameters.TempDirectory = _workingDirectory;

                // initialize the instance
                _instance.Init();
                _initialized = true;
            }
        }

        private void CreateTable(Schema schema)
        {
            Api.JetBeginTransaction(_session);
            JET_TABLEID tableid;
            Api.JetCreateTable(_session, _dbid, schema.TableName, 0, 100, out tableid);

            foreach (var col in schema.Columns)
            {
                var coldef = new JET_COLUMNDEF
                {
                    coltyp = col.Type,
                    cbMax = col.MaxSize,
                    cp = col.CodePage
                };
                Api.JetAddColumn(_session, tableid, col.Name, coldef, null, 0, out _);
            }

            CreateIndex(tableid, new[] { _primaryKey }, CreateIndexGrbit.IndexPrimary | CreateIndexGrbit.IndexUnique);

            Api.JetCloseTable(_session, tableid);
            Api.JetCommitTransaction(_session, CommitTransactionGrbit.None);
        }

        public void CreateIndex(JET_TABLEID tableid, IEnumerable<Column> columns, CreateIndexGrbit grbit)
        {
            if (columns == null)
                throw new ArgumentNullException(nameof(columns));

            var columnList = columns.ToList();
            if (columnList.Count == 0)
                throw new ArgumentException("At least one column is required to create an index.", nameof(columns));

            if (tableid == JET_TABLEID.Nil)
                throw new ArgumentException("Invalid table ID.", nameof(tableid));

            bool isPrimaryKeyIndex = (columnList.Count == 1 && columnList[0].IsPrimaryKey);

            if (isPrimaryKeyIndex && (grbit & CreateIndexGrbit.IndexPrimary) == 0)
                throw new ArgumentException("Primary key index must have the CreateIndexGrbit.IndexPrimary flag set.", nameof(grbit));

            string indexName = isPrimaryKeyIndex
                ? _primaryKeyindexName
                : _indexNamePrefix + string.Join("_", columnList.Select(c => c.Name));

            string key = string.Concat(columnList.Select(c => "+" + c.Name));
            string keyDescription = key + "\0\0"; // double null-terminated
            int keyDescriptionLength = keyDescription.Length;

            Api.JetCreateIndex(
                _session,
                tableid,
                indexName,
                grbit,
                keyDescription,
                keyDescriptionLength,
                100
            );
        }

        private void CacheColumns()
        {
            using (var table = new Table(_session, _dbid, _schema.TableName, OpenTableGrbit.ReadOnly))
            {
                foreach (var col in _schema.Columns)
                {
                    try
                    {
                        JET_COLUMNID colid = Api.GetTableColumnid(_session, table, col.Name);
                        _columnIds[col.Name] = colid;
                    }
                    catch (EsentColumnNotFoundException)
                    {
                        Trace.TraceWarning($"Column '{col.Name}' not found.");
                    }
                }
            }
        }

        public bool Insert(Dictionary<string, object> values, out object key)
        {
            return TrySaveRecord(values, JET_prep.Insert, expectSeek: false, out key);
        }

        public bool Update(Dictionary<string, object> values)
        {
            return TrySaveRecord(values, JET_prep.Replace, expectSeek: true, out _);
        }

        private bool TrySaveRecord(
            Dictionary<string, object> values,
            JET_prep prepType,
            bool expectSeek,
            out object primaryKeyValue)
        {
            primaryKeyValue = null;

            if (!TryGetPrimaryKeyValue(values, out var keyValue))
                return false;

            var keyType = _primaryKey.Type;

            using (var table = new Table(_session, _dbid, _schema.TableName, OpenTableGrbit.Updatable))
            {
                try
                {
                    Api.JetBeginTransaction(_session);

                    Api.JetSetCurrentIndex(_session, table, _primaryKeyindexName);
                    MakeKeyByType(keyValue, keyType, _session, table);
                    bool found = Api.TrySeek(_session, table, SeekGrbit.SeekEQ);

                    if (expectSeek != found)
                    {
                        Trace.TraceWarning($"[ESENT] Operation skipped. Seek result = {found}, expected = {expectSeek}");
                        Api.JetRollback(_session, RollbackTransactionGrbit.None);
                        return false;
                    }

                    Api.JetPrepareUpdate(_session, table, prepType);
                    SetAllColumns(values, table);

                    Api.JetUpdate(_session, table);
                    Api.JetCommitTransaction(_session, CommitTransactionGrbit.None);

                    if (prepType == JET_prep.Insert)
                        primaryKeyValue = keyValue;

                    return true;
                }
                catch (Exception ex)
                {
                    Api.JetRollback(_session, RollbackTransactionGrbit.None);
                    throw new InvalidOperationException($"[ESENT] Operation failed: {ex.Message}");
                }
            }
        }

        public Dictionary<string, object> FindById(object keyValue)
        {
            var result = new Dictionary<string, object>();
            var keyType = _primaryKey.Type;

            using (var table = new Table(_session, _dbid, _schema.TableName, OpenTableGrbit.ReadOnly))
            {
                Api.JetSetCurrentIndex(_session, table, _primaryKeyindexName);
                MakeKeyByType(keyValue, keyType, _session, table);
                if (!Api.TrySeek(_session, table, SeekGrbit.SeekEQ))
                    return null;

                foreach (var col in _schema.Columns)
                {
                    if (!_columnIds.TryGetValue(col.Name, out var colid))
                        continue;

                    var value = RetrieveColumnByType(_session, table, colid, col.Type);
                    result[col.Name] = value;
                }
            }

            return result;
        }

        public List<Dictionary<string, object>> FindAll()
        {
            var results = new List<Dictionary<string, object>>();

            using (var table = new Table(_session, _dbid, _schema.TableName, OpenTableGrbit.ReadOnly))
            {
                Api.JetSetCurrentIndex(_session, table, _primaryKeyindexName);

                if (!Api.TryMoveFirst(_session, table))
                    return results;

                do
                {
                    var row = new Dictionary<string, object>();
                    foreach (var col in _schema.Columns)
                    {
                        if (!_columnIds.TryGetValue(col.Name, out var colid))
                            continue;

                        var value = RetrieveColumnByType(_session, table, colid, col.Type);
                        row[col.Name] = value;
                    }
                    results.Add(row);
                }
                while (Api.TryMoveNext(_session, table));
            }

            return results;
        }

        public bool DeleteById(object keyValue)
        {
            var keyType = _primaryKey.Type;

            using (var table = new Table(_session, _dbid, _schema.TableName, OpenTableGrbit.Updatable))
            {
                Api.JetSetCurrentIndex(_session, table, _primaryKeyindexName);
                MakeKeyByType(keyValue, keyType, _session, table);
                if (!Api.TrySeek(_session, table, SeekGrbit.SeekEQ))
                    return false;

                Api.JetDelete(_session, table);
                return true;
            }
        }

        private object RetrieveColumnByType(Session session, Table table, JET_COLUMNID columnId, JET_coltyp type)
        {
            switch (type)
            {
                case JET_coltyp.Text:
                    return Api.RetrieveColumnAsString(session, table, columnId, Encoding.Unicode);
                case JET_coltyp.Long:
                    return Api.RetrieveColumnAsInt32(session, table, columnId);
                case JET_coltyp.IEEEDouble:
                    return Api.RetrieveColumnAsDouble(session, table, columnId);
                case JET_coltyp.DateTime:
                    return Api.RetrieveColumnAsDateTime(session, table, columnId);
                case JET_coltyp.Binary:
                case JET_coltyp.LongBinary:
                    return Api.RetrieveColumn(session, table, columnId);
                default:
                    Trace.TraceWarning($"[ESENT] Unsupported RetrieveColumn type: {type}");
                    return null;
            }
        }

        private bool TryGetPrimaryKeyValue(Dictionary<string, object> values, out object keyValue)
        {
            keyValue = null;

            if (!values.TryGetValue(_primaryKey.Name, out keyValue))
            {
                Trace.TraceWarning($"[ESENT] Missing primary key '{_primaryKey.Name}'.");
                return false;
            }

            if (keyValue == null)
            {
                Trace.TraceWarning("[ESENT] Primary key value cannot be null.");
                return false;
            }

            return true;
        }

        private JET_coltyp GetColumnType(string columnName)
        {
            var column = _schema.Columns.FirstOrDefault(c => c.Name == columnName);
            if (column == null)
                throw new ArgumentException($"Column '{columnName}' not found in schema.");

            return column.Type;
        }

        private void SetAllColumns(Dictionary<string, object> values, Table table)
        {
            foreach (var kv in values)
            {
                if (!_columnIds.TryGetValue(kv.Key, out var colid))
                {
                    Trace.TraceWarning($"[ESENT] Column '{kv.Key}' not found in cache.");
                    continue;
                }

                var colType = GetColumnType(kv.Key);
                SetColumnByType(_session, table, colid, kv.Value, colType);
            }
        }

        private void SetColumnByType(Session session, Table table, JET_COLUMNID columnId, object value, JET_coltyp type)
        {
            if (value == null)
                return;

            switch (type)
            {
                case JET_coltyp.Text:
                    Api.SetColumn(session, table, columnId, value.ToString(), Encoding.Unicode);
                    break;
                case JET_coltyp.Long:
                    Api.SetColumn(session, table, columnId, Convert.ToInt32(value));
                    break;
                case JET_coltyp.IEEEDouble:
                    Api.SetColumn(session, table, columnId, Convert.ToDouble(value));
                    break;
                case JET_coltyp.DateTime:
                    Api.SetColumn(session, table, columnId, Convert.ToDateTime(value));
                    break;
                case JET_coltyp.Binary:
                case JET_coltyp.LongBinary:
                    Api.SetColumn(session, table, columnId, (byte[])value);
                    break;
                default:
                    Trace.TraceWarning($"[ESENT] Unsupported SetColumn type: {type}");
                    break;
            }
        }

        private void MakeKeyByType(object value, JET_coltyp type, Session session, Table table)
        {
            switch (type)
            {
                case JET_coltyp.Text:
                    Api.MakeKey(session, table, value.ToString(), Encoding.Unicode, MakeKeyGrbit.NewKey);
                    break;
                case JET_coltyp.Long:
                    Api.MakeKey(session, table, Convert.ToInt32(value), MakeKeyGrbit.NewKey);
                    break;
                case JET_coltyp.IEEEDouble:
                    Api.MakeKey(session, table, Convert.ToDouble(value), MakeKeyGrbit.NewKey);
                    break;
                case JET_coltyp.DateTime:
                    Api.MakeKey(session, table, Convert.ToDateTime(value), MakeKeyGrbit.NewKey);
                    break;
                case JET_coltyp.Binary:
                case JET_coltyp.LongBinary:
                    Api.MakeKey(session, table, (byte[])value, MakeKeyGrbit.NewKey);
                    break;
                default:
                    Trace.TraceWarning($"[ESENT] Unsupported MakeKey type: {type}");
                    break;
            }
        }

        public void Dispose()
        {
            _session?.Dispose();
        }
    }
}
