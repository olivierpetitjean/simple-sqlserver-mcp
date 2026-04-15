namespace SimpleSqlServerMcp.Safety;

internal interface IMutableQueryValidator
{
    string Validate(string sql);
}
