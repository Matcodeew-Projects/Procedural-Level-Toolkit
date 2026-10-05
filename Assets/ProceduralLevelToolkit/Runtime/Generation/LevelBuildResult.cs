using System.Collections.Generic;
using UnityEngine;

public sealed class LevelBuildResult
{
    private readonly List<string> errors = new List<string>();
    private readonly List<GameObject> instances = new List<GameObject>();

    public GameObject Root { get; internal set; }
    public IReadOnlyList<string> Errors => errors;
    public IReadOnlyList<GameObject> Instances => instances;
    public bool Success => Root != null && errors.Count == 0;

    internal void AddError(string message)
    {
        if (!string.IsNullOrWhiteSpace(message))
            errors.Add(message);
    }

    internal void AddInstance(GameObject instance)
    {
        if (instance != null)
            instances.Add(instance);
    }
}
