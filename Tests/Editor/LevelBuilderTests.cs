using NUnit.Framework;
using UnityEngine;

public sealed class LevelBuilderTests
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
    public void Build_UsesSolvedModuleCountAndTransforms()
    {
        LevelTestFactory.TwoRoomFixture fixture =
            factory.CreateSolvedTwoRoomLevel();


        LevelBuildResult result =
            LevelBuilder.Build(
                fixture.Level
            );


        try
        {
            Assert.IsTrue(
                result.Success,
                string.Join(
                    "\n",
                    result.Errors
                )
            );


            Assert.IsNotNull(
                result.Root
            );


            Assert.AreEqual(
                fixture.Level
                    .SpatialLayout
                    .ModuleCount,
                result.Instances.Count
            );


            for (int i = 0;
                 i < fixture.Level
                     .SpatialLayout
                     .Modules
                     .Count;
                 i++)
            {
                LevelModuleInstanceData placement =
                    fixture.Level
                        .SpatialLayout
                        .Modules[i];


                GameObject instance =
                    result.Instances[i];


                LevelNodeData node =
                    fixture.Level
                        .Graph
                        .FindNode(
                            placement.NodeId
                        );


                float cellWorldSize =
                    node.Module
                        .CellWorldSize;


                Vector3 expectedPosition =
                    placement.Position *
                    cellWorldSize;


                Assert.That(
                    Vector3.Distance(
                        expectedPosition,
                        instance.transform
                            .localPosition
                    ),
                    Is.LessThan(
                        0.001f
                    )
                );


                float expectedYaw =
                    LayoutGeometryUtility
                        .NormalizeQuarterTurns(
                            placement.QuarterTurns
                        )
                    *
                    90f;


                float actualYaw =
                    instance.transform
                        .localEulerAngles
                        .y;


                Assert.That(
                    Mathf.Abs(
                        Mathf.DeltaAngle(
                            expectedYaw,
                            actualYaw
                        )
                    ),
                    Is.LessThan(
                        0.001f
                    )
                );
            }
        }
        finally
        {
            if (result.Root != null)
            {
                Object.DestroyImmediate(
                    result.Root
                );
            }
        }
    }
}
