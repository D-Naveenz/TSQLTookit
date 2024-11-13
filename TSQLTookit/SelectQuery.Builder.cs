using System.Text;
using System.Text.RegularExpressions;

namespace TSQLTookit;

public partial class SelectQuery
{
    public override string ToString()
    {
        StringBuilder sb = new();

        // Build the inner query
        sb.Append(BuildInnerQuery());

        // Append the order by and pagination
        if (OrderBy is not null)
        {
            sb.Insert(0, $"WITH InnerResults AS (");
            sb.Append($") SELECT * FROM InnerResults ORDER BY {OrderBy}");

            if (HasPagination)
            {
                sb.Append(" OFFSET @offsetRows ROWS FETCH NEXT @rowCount ROWS ONLY");
            }
        }

        sb.Append(';');

        return sb.ToString();
    }

    public string BuildInnerQuery()
    {
        StringBuilder sb = new("SELECT");

        // Append columns
        foreach (var column in Columns)
        {
            // Try to build subquery if the column is a subquery
            if (column.IsExpression && TryBuildSubQuery(column.ToString(), out var subQuery))
            {
                sb.Append($" {subQuery},");
                continue;
            }

            sb.Append($" {column},");
        }

        // Remove the last comma
        sb.Remove(sb.Length - 1, 1);

        // Append tables
        foreach (var table in Tables)
        {
            sb.Append($" {table}");
        }

        // Append conditions
        if (Conditions != null)
        {
            sb.Append(" WHERE");

            foreach (var condition in Conditions)
            {
                // Try to build subquery if the condition is a subquery
                if (condition.IsExpression && TryBuildSubQuery(condition.ToString(), out var subQuery))
                {
                    sb.Append($" {subQuery}");
                    continue;
                }

                sb.Append($" {condition}");
            }
        }

        // Append group by
        if (GroupBy.Count > 0)
        {
            sb.Append(" GROUP BY");
            foreach (var column in GroupBy)
            {
                sb.Append($" {column},");
            }

            // Remove the last comma
            sb.Remove(sb.Length - 1, 1);
        }

        return sb.ToString();
    }

    private bool TryBuildSubQuery(string line, out string subQuery)
    {
        subQuery = line;

        var subQueryMatch = SubqueryMatcher().Match(line);
        if (subQueryMatch.Success)
        {
            // Get the start and end indexes of the subquery
            var subQueryIndex = int.Parse(subQueryMatch.Value.Replace($"{{{SUBQUERY_PREFIX}_", "").Replace("}", ""));
            var innerquery = _subQueries[subQueryIndex].BuildInnerQuery();

            // Replace the subquery placeholder in the line with the actual subquery
            subQuery = line.Replace(subQueryMatch.Value, innerquery);

            return true;
        }

        return false;
    }

    [GeneratedRegex(@$"{{{SUBQUERY_PREFIX}_\d+}}")]
    private static partial Regex SubqueryMatcher();
}