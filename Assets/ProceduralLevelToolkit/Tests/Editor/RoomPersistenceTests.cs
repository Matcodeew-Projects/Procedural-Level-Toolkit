using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class RoomPersistenceTests
{
    private const string TestFolder =
        "Assets/__ProceduralLevelToolkit_RoomPersistenceTests";

    private const string RoomPath =
        TestFolder + "/PersistenceRoom.asset";

    private const string CellTypePath =
        TestFolder + "/Floor.asset";


    [SetUp]
    public void SetUp()
    {
        Cleanup();

        AssetDatabase.CreateFolder(
            "Assets",
            "__ProceduralLevelToolkit_RoomPersistenceTests"
        );
    }


    [TearDown]
    public void TearDown()
    {
        Cleanup();
    }


    [Test]
    public void RoomDefinition_SurvivesSaveUnloadAndReload()
    {
        CellTypeDefinition floor =
            ScriptableObject.CreateInstance<
                CellTypeDefinition
            >();


        floor.name =
            "Floor";


        AssetDatabase.CreateAsset(
            floor,
            CellTypePath
        );


        RoomDefinition room =
            ScriptableObject.CreateInstance<
                RoomDefinition
            >();


        room.name =
            "PersistenceRoom";


        room.Initialize(
            4,
            3
        );


        RoomLayerData layer =
            room.Layers[0];


        layer.SetDisplayName(
            "Geometry"
        );


        layer.SetVisible(
            false
        );


        layer.SetLocked(
            true
        );


        room.GetCell(
                layer,
                new Vector2Int(
                    2,
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
                    new Vector2Int(2, 1)
                },
                "Persistent Group"
            );


        group.SetLayerId(
            layer.Id
        );


        group.SetGenerationMode(
            CellGroupGenerationMode.PerCell
        );


        RoomSocketData socket =
            room.AddSocket(
                new Vector2Int(
                    3,
                    1
                ),
                SocketDirection.East
            );


        Assert.IsNotNull(
            socket
        );


        socket.SetDisplayName(
            "East Door"
        );


        socket.SetRole(
            SocketRole.Exit
        );


        socket.SetType(
            "Door"
        );


        socket.SetWidth(
            2
        );


        AssetDatabase.CreateAsset(
            room,
            RoomPath
        );


        EditorUtility.SetDirty(
            room
        );


        AssetDatabase.SaveAssets();


        Resources.UnloadAsset(
            room
        );


        AssetDatabase.ImportAsset(
            RoomPath,
            ImportAssetOptions.ForceUpdate
        );


        RoomDefinition loaded =
            AssetDatabase.LoadAssetAtPath<
                RoomDefinition
            >(
                RoomPath
            );


        Assert.IsNotNull(
            loaded
        );


        Assert.AreEqual(
            4,
            loaded.Width
        );


        Assert.AreEqual(
            3,
            loaded.Height
        );


        Assert.AreEqual(
            1,
            loaded.Layers.Count
        );


        RoomLayerData loadedLayer =
            loaded.Layers[0];


        Assert.AreEqual(
            "Geometry",
            loadedLayer.DisplayName
        );


        Assert.IsFalse(
            loadedLayer.Visible
        );


        Assert.IsTrue(
            loadedLayer.Locked
        );


        CellData loadedCell =
            loaded.GetCell(
                loadedLayer,
                new Vector2Int(
                    2,
                    1
                )
            );


        Assert.IsNotNull(
            loadedCell.Type
        );


        Assert.AreEqual(
            "Floor",
            loadedCell.Type.name
        );


        Assert.AreEqual(
            1,
            loaded.Groups.Count
        );


        Assert.AreEqual(
            2,
            loaded.Groups[0].Cells.Count
        );


        Assert.AreEqual(
            loadedLayer.Id,
            loaded.Groups[0].LayerId
        );


        Assert.AreEqual(
            1,
            loaded.Sockets.Count
        );


        RoomSocketData loadedSocket =
            loaded.Sockets[0];


        Assert.AreEqual(
            new Vector2Int(
                3,
                1
            ),
            loadedSocket.Position
        );


        Assert.AreEqual(
            SocketDirection.East,
            loadedSocket.Direction
        );


        Assert.AreEqual(
            SocketRole.Exit,
            loadedSocket.Role
        );


        Assert.AreEqual(
            "East Door",
            loadedSocket.DisplayName
        );


        Assert.AreEqual(
            2,
            loadedSocket.Width
        );
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
