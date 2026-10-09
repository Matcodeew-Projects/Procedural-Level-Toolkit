public sealed class RoomValidationIssue
{
    public RoomValidationSeverity Severity { get; }
    public string Code { get; }
    public string Message { get; }

    public RoomValidationIssue(
        RoomValidationSeverity severity,
        string code,
        string message)
    {
        Severity = severity;
        Code = code;
        Message = message;
    }

    public override string ToString()
    {
        return $"[{Severity}] {Message}";
    }
}
