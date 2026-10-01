using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class RoomSocketData
{
    [SerializeField]
    private string id;

    [SerializeField]
    private Vector2Int position;

    [SerializeField]
    private SocketDirection direction =
        SocketDirection.North;

    [SerializeField]
    private SocketRole role =
        SocketRole.Any;

    [SerializeField]
    private string type = "Default";

    [SerializeField, Min(1)]
    private int width = 1;

    [SerializeField]
    private List<string> tags = new();


    public string Id => id;

    public Vector2Int Position =>
        position;

    public SocketDirection Direction =>
        direction;

    public SocketRole Role =>
        role;

    public string Type =>
        type;

    public int Width =>
        width;

    public IReadOnlyList<string> Tags =>
        tags;


    public RoomSocketData()
    {
        EnsureIntegrity();
    }


    public RoomSocketData(
        Vector2Int position)
    {
        this.position =
            position;

        EnsureIntegrity();
    }


    public void EnsureIntegrity()
    {
        if (
            string.IsNullOrWhiteSpace(
                id
            ))
        {
            id =
                Guid.NewGuid()
                    .ToString("N");
        }


        if (
            string.IsNullOrWhiteSpace(
                type
            ))
        {
            type =
                "Default";
        }


        width =
            Mathf.Max(
                1,
                width
            );


        tags ??=
            new List<string>();
    }


    public void SetId(
        string value)
    {
        if (
            string.IsNullOrWhiteSpace(
                value
            ))
        {
            return;
        }


        id =
            value.Trim();
    }


    public void SetPosition(
        Vector2Int value)
    {
        position =
            value;
    }


    public void SetDirection(
        SocketDirection value)
    {
        direction =
            value;
    }


    public void SetRole(
        SocketRole value)
    {
        role =
            value;
    }


    public void SetType(
        string value)
    {
        type =
            string.IsNullOrWhiteSpace(
                value
            )
                ? "Default"
                : value.Trim();
    }


    public void SetWidth(
        int value)
    {
        width =
            Mathf.Max(
                1,
                value
            );
    }


    public void SetTags(
        IEnumerable<string> values)
    {
        tags.Clear();


        if (values == null)
            return;


        foreach (
            string value
            in values)
        {
            if (
                string.IsNullOrWhiteSpace(
                    value
                ))
            {
                continue;
            }


            string clean =
                value.Trim();


            if (
                !tags.Contains(
                    clean
                ))
            {
                tags.Add(
                    clean
                );
            }
        }
    }
}