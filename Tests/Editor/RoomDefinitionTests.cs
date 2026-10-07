using NUnit.Framework;
using UnityEngine;

public sealed class RoomDefinitionTests
{
    private LevelTestFactory factory;


    [SetUp]
    public void SetUp()
    {
        factory =
            new LevelTestFactory();
    }


    [TearDown]
    public void TearDown()
    {
        factory.Dispose();
    }


    [Test]
    public void Resize_PreservesExistingCellsAndCleansOutOfBoundsData()
    {
        RoomDefinition room =
            factory.CreateRoom(
                "ResizeRoom",
                3,
                3
            );


        RoomLayerData layer =
            room.Layers[0];


        CellTypeDefinition floor =
            factory.CreateCellType(
                "Floor"
            );


        room.GetCell(
                layer,
                new Vector2Int(
                    1,
                    1
                )
            )
            .SetType(
                floor
            );


        CellGroupData group =
            room.AddGroup(
                new[]
                {
                    new Vector2Int(1, 1),
                    new Vector2Int(2, 2)
                },
                "Resize Group"
            );


        group.SetLayerId(
            layer.Id
        );


        RoomSocketData socket =
            room.AddSocket(
                new Vector2Int(
                    2,
                    1
                ),
                SocketDirection.East
            );


        Assert.IsNotNull(
            socket
        );


        room.Resize(
            2,
            2
        );


        Assert.AreEqual(
            2,
            room.Width
        );


        Assert.AreEqual(
            2,
            room.Height
        );


        Assert.AreSame(
            floor,
            room.GetCell(
                    layer,
                    new Vector2Int(
                        1,
                        1
                    )
                )
                .Type
        );


        Assert.AreEqual(
            1,
            group.Cells.Count
        );


        Assert.AreEqual(
            new Vector2Int(
                1,
                1
            ),
            group.Cells[0]
        );


        Assert.AreEqual(
            0,
            room.Sockets.Count
        );
    }


    [Test]
    public void AddSocket_RejectsInteriorCellsAndInfersBorderDirection()
    {
        RoomDefinition room =
            factory.CreateRoom(
                "SocketRoom",
                5,
                5
            );


        RoomSocketData interior =
            room.AddSocket(
                new Vector2Int(
                    2,
                    2
                )
            );


        Assert.IsNull(
            interior
        );


        RoomSocketData top =
            room.AddSocket(
                new Vector2Int(
                    2,
                    0
                )
            );


        RoomSocketData right =
            room.AddSocket(
                new Vector2Int(
                    4,
                    2
                )
            );


        RoomSocketData bottom =
            room.AddSocket(
                new Vector2Int(
                    2,
                    4
                )
            );


        RoomSocketData left =
            room.AddSocket(
                new Vector2Int(
                    0,
                    2
                )
            );


        Assert.IsNotNull(
            top
        );


        Assert.IsNotNull(
            right
        );


        Assert.IsNotNull(
            bottom
        );


        Assert.IsNotNull(
            left
        );


        Assert.AreEqual(
            SocketDirection.North,
            top.Direction
        );


        Assert.AreEqual(
            SocketDirection.East,
            right.Direction
        );


        Assert.AreEqual(
            SocketDirection.South,
            bottom.Direction
        );


        Assert.AreEqual(
            SocketDirection.West,
            left.Direction
        );
    }
}