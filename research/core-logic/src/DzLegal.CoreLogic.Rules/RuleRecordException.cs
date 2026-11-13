namespace DzLegal.CoreLogic.Rules;

/// <summary>A record the evaluator cannot run as written. <c>rules check</c> refuses the same record, with the reason.</summary>
public sealed class RuleRecordException : Exception
{
    public RuleRecordException()
    {
    }

    public RuleRecordException(string message)
        : base(message)
    {
    }

    public RuleRecordException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
