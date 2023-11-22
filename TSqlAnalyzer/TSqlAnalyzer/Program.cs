using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using System.Runtime.InteropServices;

public static class Program
{
    public static void Main()
    {
        var parser = new TSql160Parser(true);
        var sqlScript = System.IO.File.ReadAllText(@"..\..\..\CreateTableSample.sql");
        var fragment = parser.Parse(new System.IO.StringReader(sqlScript), out var errors);
        var analyzer = new TSqlAnalyzer();
        fragment.Accept(analyzer);

        sqlScript = System.IO.File.ReadAllText(@"..\..\..\InsertSample.sql");
        fragment = parser.Parse(new System.IO.StringReader(sqlScript), out errors);
        fragment.Accept(analyzer);
    }
}
public class TSqlAnalyzer : TSqlFragmentVisitor
{
    public sealed class TSqlAnalyzeInfo
    {
        public Dictionary<string /* ColumnName */, ScalarExpression /* DefaultValue */> columnNameToDefaultValue = new();

        public Dictionary<string /* ColumnName */, bool /* Nullable */> columnNameToNullable = new();

        public bool HasDefaultValue(string columnName) => columnNameToDefaultValue.ContainsKey(columnName);

        public bool IsNotNullColumn(string columnName) =>
            columnNameToNullable.GetValueOrDefault(columnName);

        public IEnumerable<string> NotNullableColumns
        {
            get
            {
                foreach (var pair in columnNameToNullable.Where(pair => pair.Value == false))
                {
                    yield return pair.Key;
                }
            }
        }
    }
    public override void ExplicitVisit(CreateTableStatement node)
    {
        TableDefinition tableDefinition = node.Definition;
        Analyze(node.SchemaObjectName, tableDefinition);
    }

    public override void ExplicitVisit(AlterTableAddTableElementStatement node)
    {
        // ALTER TABLE [dbo].[User] ADD CONSTRAINT [DF_User] DEFAULT '' FOR [Name];
        TableDefinition tableDefinition = node.Definition;
        Analyze(node.SchemaObjectName, tableDefinition);
    }

    private Dictionary<string /* TableName */, TSqlAnalyzeInfo> tableToAnalyzeInfos = new();

    private void Analyze(SchemaObjectName schemaObjectName, TableDefinition tableDefinition)
    {
        var tableName = GetTableName(schemaObjectName);
        foreach (ColumnDefinition columnDefinition in tableDefinition.ColumnDefinitions)
        {
            var columnName = GetColumnName(columnDefinition);

            Analyze(columnDefinition.DefaultConstraint, tableName: tableName, columnName: columnName);

            foreach (var constraint in columnDefinition.Constraints)
            {
                Analyze(constraint, tableName: tableName, columnName: columnName);
            }
        }

        foreach (var constraint in tableDefinition.TableConstraints)
        {
            Analyze(constraint, tableName: tableName, columnName: null);
        }
    }

    private void Analyze(ConstraintDefinition constraint, string tableName, string? columnName)
    {
        if (!tableToAnalyzeInfos.TryGetValue(tableName, out var analyzeInfo))
        {
            analyzeInfo = new();
            tableToAnalyzeInfos.Add(tableName, analyzeInfo);
        }

        if (constraint is NullableConstraintDefinition nullableConstraint)
        {
            if (columnName is null)
            {
                Console.WriteLine($"{nullableConstraint}'s column cannot find");
                return;
            }

            if (nullableConstraint.Nullable)
            {
                Console.WriteLine($"{columnName} is null");
            }
            else
            {
                Console.WriteLine($"{columnName} is not null");
            }

            analyzeInfo.columnNameToNullable[columnName] = nullableConstraint.Nullable;

        }

        if (constraint is DefaultConstraintDefinition defaultConstraint)
        {
            var defaultValue = defaultConstraint.Expression;
            columnName ??= defaultConstraint.Column?.Value;
            if (columnName is null)
            {
                Console.WriteLine($"{defaultConstraint}'s column cannot find");
                return;
            }

            Console.WriteLine($"{columnName} is default {defaultValue}");
            analyzeInfo.columnNameToDefaultValue[columnName] = defaultValue;
        }
    }

    private string GetColumnName(ColumnDefinition columnDefinition) =>
        columnDefinition.ColumnIdentifier.Value;

    private string GetTableName(SchemaObjectName schemaObjectName) =>
        schemaObjectName.BaseIdentifier.Value;

    public override void ExplicitVisit(InsertStatement node)
    {
        // 컬럼을 명시적으로 지정한 경우
        var schemaObject = node.InsertSpecification.Target switch
        {
            NamedTableReference namedTableReference => namedTableReference.SchemaObject,
            _ => null
        };

        var insertColumns = new HashSet<string>();
        foreach (ColumnReferenceExpression column in node.InsertSpecification.Columns)
        {
            foreach (Identifier identifier in column.MultiPartIdentifier.Identifiers)
            {
                var columnName = identifier.Value;
                insertColumns.Add(columnName);
            }
        }

        var tableName = schemaObject is not null
            ? GetTableName(schemaObject)
            : string.Empty;
        if (tableToAnalyzeInfos.TryGetValue(tableName, out var analyzeInfo))
        {
            foreach (var columnName in analyzeInfo.NotNullableColumns)
            {
                var hasDefault = analyzeInfo.HasDefaultValue(columnName);
                if (hasDefault)
                {
                    continue;
                }

                if (!insertColumns.Contains(columnName))
                {
                    Console.WriteLine($"Warning!! {columnName} need to Insert Value");
                }
            }
        }


        // https://learn.microsoft.com/ko-kr/dotnet/api/microsoft.sqlserver.transactsql.scriptdom.insertsource?view=sql-transactsql-161&devlangs=csharp&f1url=%3FappId%3DDev16IDEF1%26l%3DKO-KR%26k%3Dk(InsertSource)%3Bk(DevLang-csharp)%26rd%3Dtrue
        if (node.InsertSpecification.InsertSource is SelectInsertSource selectInsertSource)
        {
            // INSERT INTO ~~ SELECT 같은 경우
            if (!node.InsertSpecification.Columns.Any())
            {

            }
        }
        else if (node.InsertSpecification.InsertSource is ValuesInsertSource valuesInsertSource)
        {

        }
        else if (node.InsertSpecification.InsertSource is ExecuteInsertSource executeInsertSource)
        {

        }
        else
        {
            Console.WriteLine("Error");
        }
    }

    public override void ExplicitVisit(UpdateStatement node)
    {
        Console.WriteLine(nameof(UpdateStatement));
    }

    public override void ExplicitVisit(DeleteStatement node)
    {
        Console.WriteLine(nameof(DeleteStatement));
    }
}

