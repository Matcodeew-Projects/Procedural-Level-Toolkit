using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class RoomPrefabBuilderTests
{
    private const string TestFolder =
        "Assets/__ProceduralLevelToolkit_RoomPrefabTests";

    private const string PrefabPath =
        TestFolder + "/GeneratedRoom.prefab";


    private RoomDefinition room;

    private RoomGenerationSettings settings;


    [SetUp]
    public void SetUp()
    {
        CleanupAssets();


        AssetDatabase.CreateFolder(
            "Assets",
            "__ProceduralLevelToolkit_RoomPrefabTests"
        );


        room =
            ScriptableObject.CreateInstance<
                RoomDefinition
            >();


        room.name =
            "GeneratedRoom";


        room.Initialize(
            3,
            3
        );


        settings =
            ScriptableObject.CreateInstance<
                RoomGenerationSettings
            >();
    }


    [TearDown]
    public void TearDown()
    {
        if (room != null)
        {
            Object.DestroyImmediate(
                room
            );
        }


        if (settings != null)
        {
            Object.DestroyImmediate(
                settings
            );
        }


        CleanupAssets();
    }


    [Test]
    public void Rebuild_PreservesManualHierarchy()
    {
        GameObject firstBuild =
            RoomPrefabBuilder.Build(
                room,
                settings,
                PrefabPath
            );


        Assert.IsNotNull(
            firstBuild
        );


        GameObject contents =
            PrefabUtility.LoadPrefabContents(
                PrefabPath
            );


        try
        {
            Transform manual =
                contents.transform.Find(
                    "Manual"
                );


            Assert.IsNotNull(
                manual
            );


            GameObject keepMe =
                new GameObject(
                    "KeepMe"
                );


            keepMe.transform.SetParent(
                manual,
                false
            );


            PrefabUtility.SaveAsPrefabAsset(
                contents,
                PrefabPath
            );
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(
                contents
            );
        }


        GameObject rebuilt =
            RoomPrefabBuilder.Build(
                room,
                settings,
                PrefabPath
            );


        Assert.IsNotNull(
            rebuilt
        );


        GameObject rebuiltContents =
            PrefabUtility.LoadPrefabContents(
                PrefabPath
            );


        try
        {
            Assert.IsNotNull(
                rebuiltContents.transform.Find(
                    "Generated"
                )
            );


            Assert.IsNotNull(
                rebuiltContents.transform.Find(
                    "Manual/KeepMe"
                )
            );
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(
                rebuiltContents
            );
        }
    }


    private static void CleanupAssets()
    {
        if (AssetDatabase.IsValidFolder(
                TestFolder
            ))
        {
            AssetDatabase.DeleteAsset(
                TestFolder
            );


            AssetDatabase.Refresh();
        }
    }
}
