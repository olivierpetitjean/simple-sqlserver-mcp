namespace SimpleSqlServerMcp.Safety;

internal interface IReadOnlyQueryValidator
{
    void Validate(string sql);
}
