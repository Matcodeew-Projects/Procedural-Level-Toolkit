using System.Collections.Generic;

public sealed class LevelValidationResult
{
    private readonly List<
        LevelValidationIssue
    > issues =
        new List<
            LevelValidationIssue
        >();


    public IReadOnlyList<
        LevelValidationIssue
    > Issues =>
        issues;


    public bool IsValid =>
        ErrorCount ==
        0;


    public int ErrorCount
    {
        get;
        private set;
    }


    public int WarningCount
    {
        get;
        private set;
    }


    public void AddError(
        string code,
        string message
    )
    {
        issues.Add(
            new LevelValidationIssue(
                code,
                message,
                LevelValidationSeverity.Error
            )
        );


        ErrorCount++;
    }


    public void AddWarning(
        string code,
        string message
    )
    {
        issues.Add(
            new LevelValidationIssue(
                code,
                message,
                LevelValidationSeverity.Warning
            )
        );


        WarningCount++;
    }
}
