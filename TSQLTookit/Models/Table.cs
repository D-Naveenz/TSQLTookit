using System.Text.RegularExpressions;

namespace TSQLTookit.Models;

public class Table
{
    public string Name { get; set; }

    public string? Alias { get; set; }

    public HashSet<Selector> Selectors { get; } = [];

    public bool HasAlias => Alias != null;

    public string Identifier => Alias ?? Name;

    public Table(string name, string? alias)
    {
        Name = name;
        Alias = alias;
    }

    public Table(string nameInQuery, bool forceHaveAlias = false)
    {
        var names = nameInQuery.Trim().Split(" ");
        if (names.Length > 1)
        {
            Name = names[0];
            Alias = names[1];
        }
        else if (forceHaveAlias)
        {
            Name = nameInQuery;
            Alias = CreateAlias(nameInQuery);
        }
        else
        {
            Name = nameInQuery;
        }
    }

    public override string ToString()
    {
        return Alias != null ? $"FROM {Name} {Alias}" : $"FROM {Name}";
    }

    public override bool Equals(object? obj)
    {
        if (obj == null) return false;
        if (obj is string str)
        {
            return str == Name || str == Alias;
        }

        return base.Equals(obj);
    }

    public override int GetHashCode()
    {
        return Name.GetHashCode();
    }

    private static string CreateAlias(string tableName)
    {
        // Grab all Capital letters and numbers to create an alias in simple case
        string pattern = @"[A-Z0-9]";
        string alias = string.Concat(Regex.Matches(tableName, pattern).Select(m => m.Value)).ToLower();

        return alias;
    }
}