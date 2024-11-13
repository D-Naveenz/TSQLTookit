using System.Text.RegularExpressions;
using TSQLTookit.Models;
using TSQLTookit.Utils;

namespace TSQLTookit;

public sealed partial class SelectQuery
{
    private const string SUBQUERY_PREFIX = "SUBQUERY";
    private readonly List<SelectQuery> _subQueries = [];

    public List<Selector> Columns { get; } = [];

    public List<Table> Tables { get; } = [];

    public List<Selector> Conditions { get; } = [];

    public Table PrimaryTable
    {
        get
        {
            return Tables.FirstOrDefault(table => table is not Join) ??
                throw new IndexOutOfRangeException("The primary table was not found.");
        }
    }

    public List<Selector> GroupBy { get; } = [];

    public string? OrderBy { get; set; }

    public bool HasPagination { get; set; }

    public SelectQuery(string query)
    {
        query = ConvertToOneLine(query);

        // Subqueries can be cause of issues, so we have to extract them first
        ExtractSubQueries(ref query);

        ParseTables(query);
        ParseColumns(query);
        ParseConditions(query);
        ParseGroupBy(query);

        if (Tables.Count == 0)
        {
            throw new IndexOutOfRangeException("The query must have a section with 'FROM' clause.");
        }
    }

    public Table GetTable(string indentifier)
    {
        var result = Tables.FirstOrDefault(table => table.Equals(indentifier));
        return result is null ? throw new IndexOutOfRangeException($"The table with identifier '{indentifier}' was not found.") : result;
    }

    #region Add Methods

    public SelectQuery AddColumns(params string[] columns)
    {
        foreach (var column in columns)
        {
            Columns.Add(new(column, this));
        }

        return this;
    }

    public SelectQuery AddSubQueryAsColumn(string subQuery, string columnName)
    {
        subQuery = ConvertToOneLine(subQuery);

        Columns.Add(new($"{subQuery.Trim()} AS {columnName}", this, true));

        return this;
    }

    public SelectQuery AddCondition(string query, string joinOperator = "AND", bool isExpression = false)
    {
        if (isExpression) query = ConvertToOneLine(query);

        // Check if the conditions array is empty. If so, add the query without a join operator
        if (Conditions.Count == 0)
        {
            Conditions.Add(new(query, this, isExpression));
        }
        else
        {
            Conditions.Add(new(string.Concat(joinOperator, " ", query), this, isExpression));
        }

        return this;
    }

    public SelectQuery AddGroupBy(params string[] columns)
    {
        foreach (var column in columns)
        {
            GroupBy.Add(new(column, this));
        }

        return this;
    }

    public SelectQuery AddJoin(SQLJoinType joinType, string joinTable, string matchingColumn)
    {
        // create the join
        var join = new Join(joinType, joinTable, matchingColumn, PrimaryTable, matchingColumn, PrimaryTable.HasAlias);

        // Add the join to the tables list
        Tables.Add(join);

        return this;
    }

    public SelectQuery AddJoin(SQLJoinType joinType, string joinTable, string matchingColumn, string primaryTable, string? primaryColumn = null)
    {
        primaryColumn ??= matchingColumn;

        // create the join
        var join = new Join(joinType, joinTable, matchingColumn, GetTable(primaryTable), primaryColumn, PrimaryTable.HasAlias);

        // Add the join to the tables list
        Tables.Add(join);

        return this;
    }

    #endregion Add Methods

    /// <summary>
    /// Remove all newlines and extra spaces from the query
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    private static string ConvertToOneLine(string query)
    {
        return QuerySpaceMatcher().Replace(query, " ").Trim();
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex QuerySpaceMatcher();
}