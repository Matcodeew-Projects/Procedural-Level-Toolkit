using System.Collections.Generic;
using UnityEngine;

public sealed class RoomSocketComponent
    : MonoBehaviour
{
    [SerializeField]
    private string socketId;

    [SerializeField]
    private Vector2Int gridPosition;

    [SerializeField]
    private SocketDirection direction;

    [SerializeField]
    private SocketRole role;

    [SerializeField]
    private string socketType;

    [SerializeField, Min(1)]
    private int width = 1;

    [SerializeField]
    private List<string>
        tags = new();


    public string SocketId =>
        socketId;


    public Vector2Int GridPosition =>
        gridPosition;


    public SocketDirection Direction =>
        direction;


    public SocketRole Role =>
        role;


    public string SocketType =>
        socketType;


    public int Width =>
        width;


    public IReadOnlyList<string> Tags =>
        tags;


    public void Configure(
        RoomSocketData socket)
    {
        if (socket == null)
            return;


        socketId =
            socket.Id;


        gridPosition =
            socket.Position;


        direction =
            socket.Direction;


        role =
            socket.Role;


        socketType =
            socket.Type;


        width =
            Mathf.Max(
                1,
                socket.Width
            );


        tags.Clear();


        if (socket.Tags == null)
            return;


        foreach (
            string tag
            in socket.Tags)
        {
            if (
                string.IsNullOrWhiteSpace(
                    tag
                ))
            {
                continue;
            }


            tags.Add(
                tag
            );
        }
    }
}