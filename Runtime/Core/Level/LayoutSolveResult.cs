using System;
using System.Collections.Generic;

[Serializable]
public sealed class LayoutSolveResult
{
    private readonly List<string> errors = new();
    private readonly List<string> warnings = new();

    public IReadOnlyList<string> Errors => errors;
    public IReadOnlyList<string> Warnings => warnings;
    public bool Success => errors.Count == 0;
    public int PlacedModuleCount { get; internal set; }
    public int TotalModuleCount { get; internal set; }
    public int ResolvedConnectionCount { get; internal set; }
    public int TotalConnectionCount { get; internal set; }

    public void AddError(string message)
    {
        if (!string.IsNullOrWhiteSpace(message)) errors.Add(message);
    }

    public void AddWarning(string message)
    {
        if (!string.IsNullOrWhiteSpace(message)) warnings.Add(message);
    }
}
