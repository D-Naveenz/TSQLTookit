using System.Text.RegularExpressions;
using TSQLTookit.Models;
using TSQLTookit.Utils;

namespace TSQLTookit;

public partial class SelectQuery
{
    private void ParseColumns(string query)
    {
        // Extract columns section
        var columnsMatch = ColumnClauseMatcher().Match(query);
        if (!columnsMatch.Success) return;
        var columnSection = columnsMatch.Groups[1].Value;

        // Extract the columns
        var columnMatches = ColumnsMatcher().Matches(columnSection)
            .Where(match => !string.IsNullOrWhiteSpace(match.Value));
        foreach (Match match in columnMatches)
        {
            bool isExpression = match.Value.Contains('(') && match.Value.Contains(')');
            Columns.Add(new Selector(match.Value, this, isExpression));
        }
    }

    private void ParseTables(string query)
    {
        // Extract tables section
        var tablesMatch = TableClauseMatcher().Match(query);
        if (!tablesMatch.Success) return;

        var tableSection = tablesMatch.Value;

        // Extract the primary table
        var primaryTableMatch = FromClauseMatcher().Match(tableSection);
        if (primaryTableMatch.Success)
        {
            Tables.Add(new Table(primaryTableMatch.Groups[1].Value));

            tableSection = tableSection.Replace(primaryTableMatch.Value, ""); // Remove the primary table from the section
        }

        // Extract the join tables
        var joinMatches = JoinClauseMatcher().Matches(tableSection);
        if (joinMatches.Count > 0)
        {
            foreach (Match match in joinMatches)
            {
                var joinType = match.Groups[1].Success ? match.Groups[1].Value : "INNER";
                var table = match.Groups[2].Value;
                var matchColumn = match.Groups[3].Value.Split('.')[1];
                var primaryClause = match.Groups[4].Value.Split('.');

                Tables.Add(new Join(
                    (SQLJoinType)Enum.Parse(typeof(SQLJoinType), joinType),
                    table,
                    matchColumn,
                    GetTable(primaryClause[0]),
                    primaryClause[1]
                    ));
            }
        }
    }

    private void ParseConditions(string query)
    {
        // Extract conditions section
        var conditionsMatch = ConditionClauseMatcher().Match(query);
        if (!conditionsMatch.Success) return;

        var conditionSection = conditionsMatch.Value;

        // Extract the conditions
        var conditionMatches = ConditionsMatcher().Matches(conditionSection)
            .Where(match => !string.IsNullOrWhiteSpace(match.Value));
        foreach (Match match in conditionMatches)
        {
            bool isExpression = match.Value.Contains('(') && match.Value.Contains(')');
            Conditions.Add(new Selector(match.Value, this, isExpression));
        }
    }

    private void ParseGroupBy(string query)
    {
        // Extract group by section
        var groupByMatch = GroupByMatcher().Match(query);
        if (!groupByMatch.Success) return;

        var groupBySection = groupByMatch.Value;

        // Split by newlines, trim each line, and filter out empty lines
        var groupByList = groupBySection
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .ToList();

        foreach (var groupBy in groupByList)
        {
            GroupBy.Add(new Selector(groupBy, this));
        }
    }

    private void ExtractSubQueries(ref string query)
    {
        // Pattern to locate the beginning of a subquery (SELECT preceded by '(')
        var matches = SubQueriesMatcher().Matches(query);

        int offset = 0; // Tracks position adjustments after replacing with placeholders

        foreach (Match match in matches)
        {
            int startIndex = match.Index + offset;
            int bracketCount = 1;
            int endIndex = startIndex + match.Length;

            // Increment through the query to find the matching closing parenthesis
            for (int i = endIndex; i < query.Length; i++)
            {
                if (query[i] == '(')
                    bracketCount++;
                else if (query[i] == ')')
                    bracketCount--;

                // When bracketCount returns to zero, we've reached the end of the subquery
                if (bracketCount == 0)
                {
                    endIndex = i - 1; // Exclude the closing parenthesis
                    break;
                }
            }

            // Extract the subquery and replace with a placeholder
            string subQuery = query.Substring(startIndex, endIndex - startIndex + 1);
            string placeholder = $"{{{SUBQUERY_PREFIX}_{_subQueries.Count}}}";

            _subQueries.Add(new SelectQuery(subQuery)); // Store the subquery
            query = string.Concat(query.AsSpan(0, startIndex), placeholder, query.AsSpan(endIndex + 1));

            offset += placeholder.Length - subQuery.Length; // Adjust offset for replaced content length
        }
    }

    [GeneratedRegex(@"(?<=SELECT)\s+([\S\s]*?)\s+(?=FROM)", RegexOptions.Singleline)]
    private static partial Regex ColumnClauseMatcher();

    [GeneratedRegex(@"\s*(\((?>[^()]+|(?<open>\()|(?<-open>\)))+(?(open)(?!))\)|[^,])+", RegexOptions.Compiled)]
    private static partial Regex ColumnsMatcher();

    [GeneratedRegex(@"FROM\s+([\S\s]*?)\s+(?=WHERE|GROUP BY|ORDER BY|LIMIT|$)", RegexOptions.Singleline)]
    private static partial Regex TableClauseMatcher();

    [GeneratedRegex(@"FROM\s+(\S+(?:\s+\S+)?)", RegexOptions.Singleline)]
    private static partial Regex FromClauseMatcher();

    [GeneratedRegex(@"(?:(LEFT|RIGHT|INNER|OUTER)\s+)?JOIN\s+(\S+(?:\s+\S+)?)\s+ON\s+(\S+)\s+=\s+(\S+)", RegexOptions.IgnoreCase)]
    private static partial Regex JoinClauseMatcher();

    [GeneratedRegex(@"(?<=WHERE)(.*?)(?=GROUP BY|ORDER BY|LIMIT|$)", RegexOptions.Singleline)]
    private static partial Regex ConditionClauseMatcher();

    [GeneratedRegex(@"(?:(\((?>[^()]+|(?<open>\()|(?<-open>\)))+(?(open)(?!))\))|[^()])+?(?=\s+(AND|OR|$)|$)", RegexOptions.Compiled)]
    private static partial Regex ConditionsMatcher();

    [GeneratedRegex(@"(?<=GROUP BY)(.*?)(?=ORDER BY|LIMIT|$)", RegexOptions.Singleline)]
    private static partial Regex GroupByMatcher();

    [GeneratedRegex(@"(?<=\()SELECT", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex SubQueriesMatcher();
}