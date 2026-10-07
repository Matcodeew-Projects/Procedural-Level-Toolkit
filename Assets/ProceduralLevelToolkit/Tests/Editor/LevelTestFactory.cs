using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using NUnit.Framework;
using UnityEngine;

internal sealed class LevelTestFactory
{
    private readonly List<UnityEngine.Object> unityObjects =
        new List<UnityEngine.Object>();


    public void Dispose()
    {
        for (int i =
                 unityObjects.Count - 1;
             i >= 0;
             i--)
        {
            UnityEngine.Object obj =
                unityObjects[i];


            if (obj != null)
            {
                UnityEngine.Object
                    .DestroyImmediate(
                        obj
                    );
            }
        }


        unityObjects.Clear();
    }


    public LevelDefinition CreateLevel(
        string levelName =
            "Test Level"
    )
    {
        LevelDefinition level =
            Track(
                ScriptableObject
                    .CreateInstance<
                        LevelDefinition
                    >()
            );


        level.SetLevelName(
            levelName
        );


        level.SetDescription(
            "Automated EditMode test level."
        );


        level.EnsureIntegrity();


        return level;
    }


    public RoomDefinition CreateRoom(
        string name,
        int width,
        int height,
        params RoomSocketData[] sockets
    )
    {
        RoomDefinition room =
            Track(
                ScriptableObject
                    .CreateInstance<
                        RoomDefinition
                    >()
            );


        room.name =
            name;


        SetRequiredField(
            room,
            "width",
            width
        );


        SetRequiredField(
            room,
            "height",
            height
        );


        SetRequiredField(
            room,
            "sockets",
            new List<RoomSocketData>(
                sockets
                ??
                Array.Empty<RoomSocketData>()
            )
        );


        room.EnsureIntegrity();


        return room;
    }


    public CellTypeDefinition CreateCellType(
        string name
    )
    {
        CellTypeDefinition cellType =
            Track(
                ScriptableObject
                    .CreateInstance<
                        CellTypeDefinition
                    >()
            );


        cellType.name =
            name;


        return cellType;
    }


    public RoomModuleDefinition CreateModule(
        string name,
        RoomDefinition room,
        float cellWorldSize =
            1f
    )
    {
        RoomModuleDefinition module =
            Track(
                ScriptableObject
                    .CreateInstance<
                        RoomModuleDefinition
                    >()
            );


        module.name =
            name;


        GameObject prefab =
            Track(
                new GameObject(
                    name +
                    "_Prefab"
                )
            );


        GameObject visual =
            GameObject.CreatePrimitive(
                PrimitiveType.Cube
            );


        unityObjects.Add(
            visual
        );


        visual.name =
            "Visual";


        visual.transform.SetParent(
            prefab.transform,
            false
        );


        visual.transform.localPosition =
            Vector3.zero;


        module.Configure(
            room,
            prefab,
            cellWorldSize
        );


        return module;
    }


    public RoomSocketData CreateSocket(
        string id,
        Vector2Int position,
        SocketDirection direction,
        SocketRole role,
        string type =
            "Door",
        int width =
            1,
        string displayName =
            null
    )
    {
#pragma warning disable SYSLIB0050
        RoomSocketData socket =
            (RoomSocketData)
            FormatterServices
                .GetUninitializedObject(
                    typeof(
                        RoomSocketData
                    )
                );
#pragma warning restore SYSLIB0050


        SetRequiredField(
            socket,
            "id",
            id
        );


        SetRequiredField(
            socket,
            "position",
            position
        );


        SetRequiredField(
            socket,
            "direction",
            direction
        );


        SetRequiredField(
            socket,
            "role",
            role
        );


        SetRequiredField(
            socket,
            "type",
            type
        );


        SetRequiredField(
            socket,
            "width",
            width
        );


        SetOptionalField(
            socket,
            "displayName",
            displayName
            ?? string.Empty
        );


        return socket;
    }


    public TwoRoomFixture CreateSolvedTwoRoomLevel()
    {
        RoomSocketData rootSocket =
            CreateSocket(
                "root-north",
                new Vector2Int(
                    0,
                    0
                ),
                SocketDirection.North,
                SocketRole.Exit
            );


        RoomSocketData childSocket =
            CreateSocket(
                "child-south",
                new Vector2Int(
                    0,
                    0
                ),
                SocketDirection.South,
                SocketRole.Entry
            );


        RoomDefinition rootRoom =
            CreateRoom(
                "RootRoom",
                1,
                1,
                rootSocket
            );


        RoomDefinition childRoom =
            CreateRoom(
                "ChildRoom",
                1,
                1,
                childSocket
            );


        RoomModuleDefinition rootModule =
            CreateModule(
                "RootModule",
                rootRoom
            );


        RoomModuleDefinition childModule =
            CreateModule(
                "ChildModule",
                childRoom
            );


        LevelDefinition level =
            CreateLevel();


        LevelNodeData rootNode =
            level.Graph.AddNode(
                rootModule,
                Vector2.zero
            );


        LevelNodeData childNode =
            level.Graph.AddNode(
                childModule,
                new Vector2(
                    250f,
                    0f
                )
            );


        bool connected =
            level.Graph.TryAddConnection(
                rootNode.Id,
                childNode.Id,
                rootSocket.Id,
                childSocket.Id,
                out LevelConnectionData connection
            );


        Assert.IsTrue(
            connected
        );


        Assert.IsNotNull(
            connection
        );


        LayoutSolver solver =
            new LayoutSolver();


        LayoutSolveResult solveResult =
            solver.Solve(
                level,
                rootNode.Id
            );


        Assert.IsTrue(
            solveResult.Success,
            string.Join(
                "\n",
                solveResult.Errors
            )
        );


        return new TwoRoomFixture(
            level,
            rootNode,
            childNode,
            connection,
            rootSocket,
            childSocket
        );
    }


    // =========================================================
    // Reflection Helpers
    // =========================================================

    private static void SetRequiredField(
        object target,
        string fieldName,
        object value
    )
    {
        FieldInfo field =
            FindField(
                target.GetType(),
                fieldName
            );


        Assert.IsNotNull(
            field,
            $"Expected serialized field '{fieldName}' on {target.GetType().Name}."
        );


        field.SetValue(
            target,
            ConvertValue(
                value,
                field.FieldType
            )
        );
    }


    private static void SetOptionalField(
        object target,
        string fieldName,
        object value
    )
    {
        FieldInfo field =
            FindField(
                target.GetType(),
                fieldName
            );


        if (field == null)
        {
            return;
        }


        field.SetValue(
            target,
            ConvertValue(
                value,
                field.FieldType
            )
        );
    }


    private static FieldInfo FindField(
        Type type,
        string fieldName
    )
    {
        while (type !=
               null)
        {
            FieldInfo field =
                type.GetField(
                    fieldName,
                    BindingFlags.Instance |
                    BindingFlags.NonPublic |
                    BindingFlags.Public
                );


            if (field !=
                null)
            {
                return field;
            }


            type =
                type.BaseType;
        }


        return null;
    }


    private static object ConvertValue(
        object value,
        Type targetType
    )
    {
        if (value == null)
        {
            return null;
        }


        Type valueType =
            value.GetType();


        if (targetType.IsAssignableFrom(
                valueType
            ))
        {
            return value;
        }


        if (targetType.IsEnum)
        {
            if (value is string stringValue)
            {
                return Enum.Parse(
                    targetType,
                    stringValue
                );
            }


            return Enum.ToObject(
                targetType,
                value
            );
        }


        return Convert.ChangeType(
            value,
            targetType
        );
    }


    private T Track<T>(
        T obj
    )
        where T : UnityEngine.Object
    {
        unityObjects.Add(
            obj
        );


        return obj;
    }


    // =========================================================
    // Fixture
    // =========================================================

    public readonly struct TwoRoomFixture
    {
        public LevelDefinition Level
        {
            get;
        }

        public LevelNodeData RootNode
        {
            get;
        }

        public LevelNodeData ChildNode
        {
            get;
        }

        public LevelConnectionData Connection
        {
            get;
        }

        public RoomSocketData RootSocket
        {
            get;
        }

        public RoomSocketData ChildSocket
        {
            get;
        }


        public TwoRoomFixture(
            LevelDefinition level,
            LevelNodeData rootNode,
            LevelNodeData childNode,
            LevelConnectionData connection,
            RoomSocketData rootSocket,
            RoomSocketData childSocket
        )
        {
            Level =
                level;

            RootNode =
                rootNode;

            ChildNode =
                childNode;

            Connection =
                connection;

            RootSocket =
                rootSocket;

            ChildSocket =
                childSocket;
        }
    }
}
