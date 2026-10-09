using NUnit.Framework;
using UnityEngine;

public sealed class RoomValidatorTests
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
    public void InteriorSocket_IsRejected()
    {
        RoomSocketData socket =
            factory.CreateSocket(
                "interior",
                new Vector2Int(
                    1,
                    1
                ),
                SocketDirection.North,
                SocketRole.Any
            );


        RoomDefinition room =
            factory.CreateRoom(
                "InvalidSocketRoom",
                3,
                3,
                socket
            );


        RoomValidationResult result =
            RoomValidator.Validate(
                room
            );


        Assert.IsTrue(
            result.HasErrors
        );


        AssertIssueExists(
            result,
            "SOCKET_BOUNDARY"
        );
    }


    [Test]
    public void BoundarySockets_WithMatchingDirections_AreValid()
    {
        RoomSocketData north =
            factory.CreateSocket(
                "north",
                new Vector2Int(
                    1,
                    0
                ),
                SocketDirection.North,
                SocketRole.Any
            );


        RoomSocketData east =
            factory.CreateSocket(
                "east",
                new Vector2Int(
                    2,
                    1
                ),
                SocketDirection.East,
                SocketRole.Any
            );


        RoomSocketData south =
            factory.CreateSocket(
                "south",
                new Vector2Int(
                    1,
                    2
                ),
                SocketDirection.South,
                SocketRole.Any
            );


        RoomSocketData west =
            factory.CreateSocket(
                "west",
                new Vector2Int(
                    0,
                    1
                ),
                SocketDirection.West,
                SocketRole.Any
            );


        RoomDefinition room =
            factory.CreateRoom(
                "ValidSocketRoom",
                3,
                3,
                north,
                east,
                south,
                west
            );


        RoomValidationResult result =
            RoomValidator.Validate(
                room
            );


        Assert.IsFalse(
            HasIssue(
                result,
                "SOCKET_BOUNDARY"
            )
        );
    }


    private static void AssertIssueExists(
        RoomValidationResult result,
        string code
    )
    {
        if (HasIssue(
                result,
                code
            ))
        {
            return;
        }


        Assert.Fail(
            $"Expected Room validation issue '{code}'."
        );
    }


    private static bool HasIssue(
        RoomValidationResult result,
        string code
    )
    {
        for (int i = 0;
             i < result.Issues.Count;
             i++)
        {
            if (result.Issues[i].Code ==
                code)
            {
                return true;
            }
        }


        return false;
    }
}