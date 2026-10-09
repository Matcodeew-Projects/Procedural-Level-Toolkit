using System.Collections.Generic;
using UnityEngine;

public sealed class RoomGenerationResult
{
    private readonly List<GameObject>
        generatedObjects =
            new();

    private readonly List<string>
        warnings =
            new();


    public GameObject Root
    {
        get;
    }


    public Transform GeneratedRoot
    {
        get;
    }


    public Transform ManualRoot
    {
        get;
    }


    public IReadOnlyList<GameObject>
        GeneratedObjects =>
            generatedObjects;


    public IReadOnlyList<string>
        Warnings =>
            warnings;


    public bool HasWarnings =>
        warnings.Count >
        0;


    public RoomGenerationResult(
        GameObject root,
        Transform generatedRoot,
        Transform manualRoot)
    {
        Root =
            root;

        GeneratedRoot =
            generatedRoot;

        ManualRoot =
            manualRoot;
    }


    internal void AddGeneratedObject(
        GameObject value)
    {
        if (value == null)
            return;


        generatedObjects.Add(
            value
        );
    }


    internal void AddWarning(
        string warning)
    {
        if (
            string.IsNullOrWhiteSpace(
                warning
            ))
        {
            return;
        }


        warnings.Add(
            warning
        );
    }
}