using NUnit.Framework;
using UnityEngine;

public sealed class LayoutSolverTests
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
    public void TwoConnectedRooms_AreSolvedWithoutOverlap()
    {
        LevelTestFactory.TwoRoomFixture fixture =
            factory.CreateSolvedTwoRoomLevel();


        SpatialLayoutData layout =
            fixture.Level.SpatialLayout;


        Assert.IsTrue(
            layout.IsSolved
        );


        Assert.AreEqual(
            2,
            layout.ModuleCount
        );


        Assert.AreEqual(
            1,
            layout.SocketConnectionCount
        );


        LevelModuleInstanceData rootPlacement =
            FindPlacement(
                layout,
                fixture.RootNode.Id
            );


        LevelModuleInstanceData childPlacement =
            FindPlacement(
                layout,
                fixture.ChildNode.Id
            );


        Assert.IsNotNull(
            rootPlacement
        );


        Assert.IsNotNull(
            childPlacement
        );


        Rect rootBounds =
            LayoutGeometryUtility
                .GetWorldBounds2D(
                    rootPlacement
                );


        Rect childBounds =
            LayoutGeometryUtility
                .GetWorldBounds2D(
                    childPlacement
                );


        Assert.IsFalse(
            LayoutGeometryUtility
                .OverlapsArea(
                    rootBounds,
                    childBounds
                )
        );


        Vector2 rootAnchor =
            LayoutGeometryUtility
                .GetWorldSocketAnchor2D(
                    rootPlacement,
                    fixture.RootSocket
                );


        Vector2 childAnchor =
            LayoutGeometryUtility
                .GetWorldSocketAnchor2D(
                    childPlacement,
                    fixture.ChildSocket
                );


        Assert.That(
            Vector2.Distance(
                rootAnchor,
                childAnchor
            ),
            Is.LessThan(
                0.001f
            )
        );
    }


    private static LevelModuleInstanceData FindPlacement(
        SpatialLayoutData layout,
        string nodeId
    )
    {
        for (int i = 0;
             i < layout.Modules.Count;
             i++)
        {
            LevelModuleInstanceData module =
                layout.Modules[i];


            if (module != null &&
                module.NodeId ==
                nodeId)
            {
                return module;
            }
        }


        return null;
    }
}
