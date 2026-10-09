using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class LevelPersistenceTests
{
    private const string TestFolder =
        "Assets/__ProceduralLevelToolkit_LevelPersistenceTests";


    [SetUp]
    public void SetUp()
    {
        Cleanup();


        AssetDatabase.CreateFolder(
            "Assets",
            "__ProceduralLevelToolkit_LevelPersistenceTests"
        );
    }


    [TearDown]
    public void TearDown()
    {
        Cleanup();
    }


    [Test]
    public void LevelDefinition_PersistsGraphLayoutRootLayoutAndBuildOutput()
    {
        RoomDefinition roomA =
            CreateRoomAsset(
                "RoomA",
                "room-a-north",
                SocketDirection.North,
                SocketRole.Exit,
                TestFolder + "/RoomA.asset"
            );


        RoomDefinition roomB =
            CreateRoomAsset(
                "RoomB",
                "room-b-south",
                SocketDirection.South,
                SocketRole.Entry,
                TestFolder + "/RoomB.asset"
            );


        GameObject prefabA =
            CreatePrefabAsset(
                "RoomA_Prefab",
                TestFolder + "/RoomA.prefab"
            );


        GameObject prefabB =
            CreatePrefabAsset(
                "RoomB_Prefab",
                TestFolder + "/RoomB.prefab"
            );


        RoomModuleDefinition moduleA =
            CreateModuleAsset(
                "ModuleA",
                roomA,
                prefabA,
                TestFolder + "/ModuleA.asset"
            );


        RoomModuleDefinition moduleB =
            CreateModuleAsset(
                "ModuleB",
                roomB,
                prefabB,
                TestFolder + "/ModuleB.asset"
            );


        LevelDefinition level =
            ScriptableObject.CreateInstance<
                LevelDefinition
            >();


        level.name =
            "PersistentLevel";


        level.SetLevelName(
            "Persistent Level"
        );


        level.SetDescription(
            "Persistence test."
        );


        string levelPath =
            TestFolder +
            "/Level.asset";


        AssetDatabase.CreateAsset(
            level,
            levelPath
        );


        LevelNodeData nodeA =
            level.Graph.AddNode(
                moduleA,
                Vector2.zero
            );


        LevelNodeData nodeB =
            level.Graph.AddNode(
                moduleB,
                Vector2.right *
                200f
            );


        bool connected =
            level.Graph.TryAddConnection(
                nodeA.Id,
                nodeB.Id,
                roomA.Sockets[0].Id,
                roomB.Sockets[0].Id,
                out _
            );


        Assert.IsTrue(
            connected
        );


        Assert.IsTrue(
            level.SetLayoutRootNodeId(
                nodeB.Id
            )
        );


        LayoutSolver solver =
            new LayoutSolver();


        LayoutSolveResult solveResult =
            solver.Solve(
                level,
                level.LayoutRootNodeId
            );


        Assert.IsTrue(
            solveResult.Success,
            string.Join(
                "\n",
                solveResult.Errors
            )
        );


        level.SetBuildOutput(
            prefabA,
            "2026-10-06T12:00:00.0000000Z"
        );


        string expectedRootId =
            nodeB.Id;


        EditorUtility.SetDirty(
            level
        );


        AssetDatabase.SaveAssets();


        Resources.UnloadAsset(
            level
        );


        AssetDatabase.ImportAsset(
            levelPath,
            ImportAssetOptions.ForceUpdate
        );


        LevelDefinition loaded =
            AssetDatabase.LoadAssetAtPath<
                LevelDefinition
            >(
                levelPath
            );


        Assert.IsNotNull(
            loaded
        );


        Assert.AreEqual(
            2,
            loaded.Graph.NodeCount
        );


        Assert.AreEqual(
            1,
            loaded.Graph.ConnectionCount
        );


        LevelConnectionData loadedConnection =
            loaded.Graph.Connections[0];


        Assert.AreEqual(
            roomA.Sockets[0].Id,
            loadedConnection.FromSocketId
        );


        Assert.AreEqual(
            roomB.Sockets[0].Id,
            loadedConnection.ToSocketId
        );


        Assert.AreEqual(
            expectedRootId,
            loaded.LayoutRootNodeId
        );


        Assert.IsTrue(
            loaded.SpatialLayout.IsSolved
        );


        Assert.AreEqual(
            2,
            loaded.SpatialLayout.ModuleCount
        );


        Assert.AreEqual(
            1,
            loaded.SpatialLayout.SocketConnectionCount
        );


        Assert.AreEqual(
            AssetDatabase.GetAssetPath(
                prefabA
            ),
            AssetDatabase.GetAssetPath(
                loaded.GeneratedPrefab
            )
        );


        Assert.IsFalse(
            string.IsNullOrWhiteSpace(
                loaded.LastBuildUtc
            )
        );
    }


    private static RoomDefinition CreateRoomAsset(
        string name,
        string socketId,
        SocketDirection direction,
        SocketRole role,
        string path
    )
    {
        RoomDefinition room =
            ScriptableObject.CreateInstance<
                RoomDefinition
            >();


        room.name =
            name;


        room.Initialize(
            1,
            1
        );


        RoomSocketData socket =
            room.AddSocket(
                Vector2Int.zero,
                direction
            );


        Assert.IsNotNull(
            socket
        );


        socket.SetId(
            socketId
        );


        socket.SetRole(
            role
        );


        socket.SetType(
            "Door"
        );


        AssetDatabase.CreateAsset(
            room,
            path
        );


        return room;
    }


    private static GameObject CreatePrefabAsset(
        string name,
        string path
    )
    {
        GameObject source =
            new GameObject(
                name
            );


        GameObject visual =
            GameObject.CreatePrimitive(
                PrimitiveType.Cube
            );


        visual.transform.SetParent(
            source.transform,
            false
        );


        GameObject prefab =
            PrefabUtility.SaveAsPrefabAsset(
                source,
                path
            );


        Object.DestroyImmediate(
            source
        );


        return prefab;
    }


    private static RoomModuleDefinition CreateModuleAsset(
        string name,
        RoomDefinition room,
        GameObject prefab,
        string path
    )
    {
        RoomModuleDefinition module =
            ScriptableObject.CreateInstance<
                RoomModuleDefinition
            >();


        module.name =
            name;


        module.Configure(
            room,
            prefab,
            1f
        );


        AssetDatabase.CreateAsset(
            module,
            path
        );


        return module;
    }


    private static void Cleanup()
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
