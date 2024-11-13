using System.Text.RegularExpressions;

namespace TSQLTookit.Models;

public partial class Selector
{
    public string Content { get; set; }

    public bool IsExpression { get; set; }

    public Selector(string content, SelectQuery selectQuery, bool isExpression = false)
    {
        content = content.Trim();

        IsExpression = isExpression;
        Content = content;

        if (!isExpression) ParseIdentifiers(selectQuery);
    }

    public override string ToString()
    {
        return Content;
    }

    private void ParseIdentifiers(SelectQuery selectQuery)
    {
        // Match the columns and tables
        var identifierMatch = IdentifierMatcher().Matches(Content);
        foreach (Match match in identifierMatch)
        {
            var column = match.Value.Split('.');
            var table = selectQuery.Tables.FirstOrDefault(t => t.Equals(column[0]));
            if (table is not null)
            {
                // Replace the column with the table identifier
                if (selectQuery.PrimaryTable.HasAlias)
                {
                    Content = Content.Replace(match.Value, $"{table.Alias}.{column[1]}");
                }

                table.Selectors.Add(this);
                continue;
            }
        }

        // If identifier is not found, then the columns are from the primary table
        if (selectQuery.Tables.Count == 0)
        {
            selectQuery.PrimaryTable.Selectors.Add(this);
        }
    }

    [GeneratedRegex(@"\b\w+\.\w+\b")]
    private static partial Regex IdentifierMatcher();
}