using System.Collections.Generic;
using System.Linq;

public sealed class RoomValidationResult
{
    private readonly List<RoomValidationIssue> issues = new();

    public IReadOnlyList<RoomValidationIssue> Issues => issues;

    public bool HasErrors =>
        issues.Any(
            issue =>
                issue.Severity ==
                RoomValidationSeverity.Error
        );

    public bool HasWarnings =>
        issues.Any(
            issue =>
                issue.Severity ==
                RoomValidationSeverity.Warning
        );

    public int ErrorCount =>
        issues.Count(
            issue =>
                issue.Severity ==
                RoomValidationSeverity.Error
        );

    public int WarningCount =>
        issues.Count(
            issue =>
                issue.Severity ==
                RoomValidationSeverity.Warning
        );

    public bool IsValid => !HasErrors;

    public void Add(
        RoomValidationSeverity severity,
        string code,
        string message)
    {
        issues.Add(
            new RoomValidationIssue(
                severity,
                code,
                message
            )
        );
    }

    public string GetSummary()
    {
        if (!HasErrors && !HasWarnings)
            return "Room is valid.";

        if (HasErrors)
            return $"{ErrorCount} error(s), {WarningCount} warning(s).";

        return $"{WarningCount} warning(s).";
    }
}
