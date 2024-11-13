using TSQLTookit.Utils;

namespace TSQLTookit.Models;

public partial class Join : Table
{
    public SQLJoinType JoinType { get; set; }

    public string MatchColumn { get; set; }

    public Table PrimaryTable { get; set; }

    public string PrimaryMatchColumn { get; set; }

    public Join(SQLJoinType joinType, string tableInQuery, string matchColumn, Table primaryTable, string primaryMatchColumn, bool forceHaveAlias = false) : base(tableInQuery, forceHaveAlias)
    {
        JoinType = joinType;
        MatchColumn = matchColumn;
        PrimaryTable = primaryTable;
        PrimaryMatchColumn = primaryMatchColumn;
    }

    public override string ToString()
    {
        if (HasAlias)
        {
            return $"{JoinType} JOIN {Name} {Alias} ON {Alias}.{MatchColumn} = {PrimaryTable.Identifier}.{PrimaryMatchColumn}";
        }

        return $"{JoinType} JOIN {Name} ON {Name}.{MatchColumn} = {PrimaryTable.Name}.{PrimaryMatchColumn}";
    }
}