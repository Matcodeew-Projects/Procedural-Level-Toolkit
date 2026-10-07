using System;

[Serializable]
public sealed class LevelValidationIssue
{
    public string Code
    {
        get;
    }

    public string Message
    {
        get;
    }

    public LevelValidationSeverity Severity
    {
        get;
    }


    public LevelValidationIssue(
        string code,
        string message,
        LevelValidationSeverity severity
    )
    {
        Code =
            code
            ?? string.Empty;

        Message =
            message
            ?? string.Empty;

        Severity =
            severity;
    }
}
