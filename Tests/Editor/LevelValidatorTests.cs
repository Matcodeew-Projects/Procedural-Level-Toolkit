using NUnit.Framework;
using UnityEngine;

public sealed class LevelValidatorTests
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
    public void SolvedTwoRoomLevel_IsValid()
    {
        LevelTestFactory.TwoRoomFixture fixture =
            factory.CreateSolvedTwoRoomLevel();


        LevelValidationResult result =
            LevelValidator.Validate(
                fixture.Level
            );


        Assert.IsTrue(
            result.IsValid,
            FormatIssues(
                result
            )
        );


        Assert.AreEqual(
            0,
            result.ErrorCount
        );
    }


    [Test]
    public void DisconnectedGraph_IsRejected()
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


        level.Graph.AddNode(
            module,
            Vector2.zero
        );


        level.Graph.AddNode(
            module,
            Vector2.right *
            200f
        );


        LevelValidationResult result =
            LevelValidator.Validate(
                level
            );


        Assert.IsFalse(
            result.IsValid
        );


        AssertIssueExists(
            result,
            "GRAPH_DISCONNECTED"
        );
    }


    [Test]
    public void UnassignedConnectionSockets_AreRejected()
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


        LevelValidationResult result =
            LevelValidator.Validate(
                level
            );


        Assert.IsFalse(
            result.IsValid
        );


        AssertIssueExists(
            result,
            "CONNECTION_SOCKET_UNASSIGNED"
        );
    }


    private static void AssertIssueExists(
        LevelValidationResult result,
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
                return;
            }
        }


        Assert.Fail(
            $"Expected validation issue '{code}'.\n{FormatIssues(result)}"
        );
    }


    private static string FormatIssues(
        LevelValidationResult result
    )
    {
        System.Text.StringBuilder builder =
            new System.Text.StringBuilder();


        for (int i = 0;
             i < result.Issues.Count;
             i++)
        {
            LevelValidationIssue issue =
                result.Issues[i];


            builder.AppendLine(
                $"[{issue.Severity}] {issue.Code}: {issue.Message}"
            );
        }


        return builder.ToString();
    }
}
