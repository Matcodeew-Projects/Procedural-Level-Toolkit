using NUnit.Framework;
using UnityEngine;

public sealed class LevelGraphTests
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
    public void RemoveNode_RemovesIncidentConnections()
    {
        RoomDefinition room =
            factory.CreateRoom(
                "Room",
                1,
                1
            );


        RoomModuleDefinition module =
            factory.CreateModule(
                "Module",
                room
            );


        LevelDefinition level =
            factory.CreateLevel();


        LevelNodeData a =
            level.Graph.AddNode(
                module,
                Vector2.zero
            );


        LevelNodeData b =
            level.Graph.AddNode(
                module,
                Vector2.right *
                200f
            );


        bool created =
            level.Graph.TryAddConnection(
                a.Id,
                b.Id,
                out _
            );


        Assert.IsTrue(
            created
        );


        Assert.AreEqual(
            2,
            level.Graph.NodeCount
        );


        Assert.AreEqual(
            1,
            level.Graph.ConnectionCount
        );


        bool removed =
            level.Graph.RemoveNode(
                a.Id
            );


        Assert.IsTrue(
            removed
        );


        Assert.AreEqual(
            1,
            level.Graph.NodeCount
        );


        Assert.AreEqual(
            0,
            level.Graph.ConnectionCount
        );
    }


    [Test]
    public void DuplicateLogicalConnection_IsRejected()
    {
        RoomDefinition room =
            factory.CreateRoom(
                "Room",
                1,
                1
            );


        RoomModuleDefinition module =
            factory.CreateModule(
                "Module",
                room
            );


        LevelDefinition level =
            factory.CreateLevel();


        LevelNodeData a =
            level.Graph.AddNode(
                module,
                Vector2.zero
            );


        LevelNodeData b =
            level.Graph.AddNode(
                module,
                Vector2.right *
                200f
            );


        bool first =
            level.Graph.TryAddConnection(
                a.Id,
                b.Id,
                out _
            );


        bool duplicate =
            level.Graph.TryAddConnection(
                b.Id,
                a.Id,
                out _
            );


        Assert.IsTrue(
            first
        );


        Assert.IsFalse(
            duplicate
        );


        Assert.AreEqual(
            1,
            level.Graph.ConnectionCount
        );
    }
}
