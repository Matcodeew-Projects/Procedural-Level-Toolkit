using System;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class RoomPrefabBuildWindow
    : EditorWindow
{
    // =========================================================
    // Initial Room
    // =========================================================

    [SerializeField]
    private RoomDefinition initialRoom;


    // =========================================================
    // Main UI
    // =========================================================

    private ObjectField roomField;

    private ObjectField settingsField;

    private TextField outputPathField;

    private Label statusLabel;

    private Button buildButton;


    // =========================================================
    // Generation Settings UI
    // =========================================================

    private VisualElement
        settingsInspectorContainer;


    // =========================================================
    // Groups UI
    // =========================================================

    private VisualElement
        groupsContainer;


    private Label
        groupsSummaryLabel;


    // =========================================================
    // Open
    // =========================================================

    [MenuItem(
        "Procedural Level Toolkit/Build Room Prefab"
    )]
    public static void Open()
    {
        Open(
            null
        );
    }


    public static void Open(
        RoomDefinition room)
    {
        RoomPrefabBuildWindow window =
            GetWindow<
                RoomPrefabBuildWindow
            >();


        window.titleContent =
            new GUIContent(
                "Build Room Prefab"
            );


        window.minSize =
            new Vector2(
                540f,
                620f
            );


        window.initialRoom =
            room;


        window.Show();


        window.ApplyInitialRoom();
    }


    // =========================================================
    // GUI
    // =========================================================

    public void CreateGUI()
    {
        rootVisualElement.Clear();


        rootVisualElement.style.paddingLeft =
            12f;

        rootVisualElement.style.paddingRight =
            12f;

        rootVisualElement.style.paddingTop =
            12f;

        rootVisualElement.style.paddingBottom =
            12f;


        ScrollView mainScroll =
            new ScrollView();


        mainScroll.style.flexGrow =
            1f;


        rootVisualElement.Add(
            mainScroll
        );


        // =====================================================
        // Header
        // =====================================================

        AddHeader(
            mainScroll,
            "ROOM PREFAB BUILDER",
            14
        );


        // =====================================================
        // Room Section
        // =====================================================

        AddSectionTitle(
            mainScroll,
            "ROOM"
        );


        roomField =
            new ObjectField(
                "Room Definition"
            );


        roomField.objectType =
            typeof(RoomDefinition);


        roomField.allowSceneObjects =
            false;


        roomField.RegisterValueChangedCallback(
            OnRoomChanged
        );


        mainScroll.Add(
            roomField
        );


        // =====================================================
        // Output
        // =====================================================

        VisualElement outputRow =
            new VisualElement();


        outputRow.style.flexDirection =
            FlexDirection.Row;


        outputRow.style.marginTop =
            4f;


        outputPathField =
            new TextField(
                "Output"
            );


        outputPathField.style.flexGrow =
            1f;


        Button browseButton =
            new Button(
                BrowseOutput
            );


        browseButton.text =
            "...";


        browseButton.style.width =
            32f;


        browseButton.style.marginLeft =
            4f;


        outputRow.Add(
            outputPathField
        );


        outputRow.Add(
            browseButton
        );


        mainScroll.Add(
            outputRow
        );


        AddSeparator(
            mainScroll
        );


        // =====================================================
        // Generation Settings
        // =====================================================

        AddSectionTitle(
            mainScroll,
            "GENERATION PROFILE"
        );


        settingsField =
            new ObjectField(
                "Generation Settings"
            );


        settingsField.objectType =
            typeof(RoomGenerationSettings);


        settingsField.allowSceneObjects =
            false;


        settingsField.RegisterValueChangedCallback(
            OnSettingsChanged
        );


        mainScroll.Add(
            settingsField
        );


        settingsInspectorContainer =
            new VisualElement();


        settingsInspectorContainer.style.marginTop =
            8f;


        settingsInspectorContainer.style.marginLeft =
            8f;


        settingsInspectorContainer.style.paddingLeft =
            8f;


        settingsInspectorContainer.style.borderLeftWidth =
            2f;


        settingsInspectorContainer.style.borderLeftColor =
            new Color(
                0.25f,
                0.4f,
                0.65f,
                1f
            );


        mainScroll.Add(
            settingsInspectorContainer
        );


        AddSeparator(
            mainScroll
        );


        // =====================================================
        // Groups
        // =====================================================

        AddSectionTitle(
            mainScroll,
            "CELL GROUPS"
        );


        groupsSummaryLabel =
            new Label();


        groupsSummaryLabel.style.opacity =
            0.65f;


        groupsSummaryLabel.style.marginBottom =
            8f;


        mainScroll.Add(
            groupsSummaryLabel
        );


        groupsContainer =
            new VisualElement();


        mainScroll.Add(
            groupsContainer
        );


        AddSeparator(
            mainScroll
        );


        // =====================================================
        // Status
        // =====================================================

        statusLabel =
            new Label();


        statusLabel.style.whiteSpace =
            WhiteSpace.Normal;


        statusLabel.style.marginBottom =
            8f;


        mainScroll.Add(
            statusLabel
        );


        // =====================================================
        // Build
        // =====================================================

        buildButton =
            new Button(
                BuildPrefab
            );


        buildButton.text =
            "BUILD PREFAB";


        buildButton.style.height =
            36f;


        buildButton.style.unityFontStyleAndWeight =
            FontStyle.Bold;


        mainScroll.Add(
            buildButton
        );


        // =====================================================
        // Initialize
        // =====================================================

        ApplyInitialRoom();


        TryFindDefaultSettings();


        RebuildSettingsInspector();


        RebuildGroups();


        UpdateOutputPath();


        UpdateStatus();
    }


    // =========================================================
    // Initial Room
    // =========================================================

    private void ApplyInitialRoom()
    {
        if (roomField == null)
            return;


        RoomDefinition room =
            initialRoom;


        if (
            room == null &&
            Selection.activeObject
            is RoomDefinition selectedRoom)
        {
            room =
                selectedRoom;
        }


        if (room == null)
            return;


        initialRoom =
            room;


        roomField
            .SetValueWithoutNotify(
                room
            );


        UpdateOutputPath();


        RebuildGroups();


        UpdateStatus();
    }


    // =========================================================
    // Room Changed
    // =========================================================

    private void OnRoomChanged(
        ChangeEvent<UnityEngine.Object> evt)
    {
        initialRoom =
            evt.newValue
            as RoomDefinition;


        UpdateOutputPath();


        RebuildGroups();


        UpdateStatus();
    }


    // =========================================================
    // Settings Changed
    // =========================================================

    private void OnSettingsChanged(
        ChangeEvent<UnityEngine.Object> evt)
    {
        RebuildSettingsInspector();


        UpdateStatus();
    }


    // =========================================================
    // Default Settings
    // =========================================================

    private void TryFindDefaultSettings()
    {
        if (
            settingsField == null ||
            settingsField.value !=
            null)
        {
            return;
        }


        string[] guids =
            AssetDatabase.FindAssets(
                "t:RoomGenerationSettings"
            );


        if (
            guids.Length !=
            1)
        {
            return;
        }


        string path =
            AssetDatabase
                .GUIDToAssetPath(
                    guids[0]
                );


        RoomGenerationSettings settings =
            AssetDatabase
                .LoadAssetAtPath<
                    RoomGenerationSettings
                >(
                    path
                );


        settingsField
            .SetValueWithoutNotify(
                settings
            );
    }


    // =========================================================
    // Generation Settings Inspector
    // =========================================================

    private void RebuildSettingsInspector()
    {
        if (
            settingsInspectorContainer ==
            null)
        {
            return;
        }


        settingsInspectorContainer.Clear();


        RoomGenerationSettings settings =
            settingsField?.value
            as RoomGenerationSettings;


        if (settings == null)
        {
            Label empty =
                new Label(
                    "Select a RoomGenerationSettings asset."
                );


            empty.style.opacity =
                0.55f;


            settingsInspectorContainer.Add(
                empty
            );


            return;
        }


        SerializedObject serializedSettings =
            new SerializedObject(
                settings
            );


        InspectorElement inspector =
            new InspectorElement(
                serializedSettings
            );


        settingsInspectorContainer.Add(
            inspector
        );
    }


    // =========================================================
    // Groups
    // =========================================================

    private void RebuildGroups()
    {
        if (
            groupsContainer ==
            null)
        {
            return;
        }


        groupsContainer.Clear();


        RoomDefinition room =
            roomField?.value
            as RoomDefinition;


        if (room == null)
        {
            groupsSummaryLabel.text =
                "No Room selected.";


            return;
        }


        int groupCount =
            room.Groups.Count;


        int singlePrefabCount =
            0;


        foreach (
            CellGroupData group
            in room.Groups)
        {
            if (
                group != null &&
                group.GenerationMode ==
                CellGroupGenerationMode.SinglePrefab)
            {
                singlePrefabCount++;
            }
        }


        groupsSummaryLabel.text =
            $"{groupCount} group(s) • " +
            $"{singlePrefabCount} SinglePrefab";


        if (groupCount == 0)
        {
            Label empty =
                new Label(
                    "This Room contains no CellGroup."
                );


            empty.style.opacity =
                0.55f;


            groupsContainer.Add(
                empty
            );


            return;
        }


        for (
            int index = 0;
            index < room.Groups.Count;
            index++)
        {
            CellGroupData group =
                room.Groups[index];


            if (group == null)
                continue;


            AddGroupEditor(
                room,
                group,
                index
            );
        }
    }


    // =========================================================
    // Group Editor
    // =========================================================

    private void AddGroupEditor(
        RoomDefinition room,
        CellGroupData group,
        int index)
    {
        string footprint =
            GetGroupFootprintLabel(
                group
            );


        string name =
            string.IsNullOrWhiteSpace(
                group.Label
            )
                ? $"Group {index + 1}"
                : group.Label;


        Foldout foldout =
            new Foldout();


        foldout.text =
            $"{name}    {footprint}";


        foldout.value =
            group.GenerationMode ==
            CellGroupGenerationMode.SinglePrefab;


        foldout.style.marginBottom =
            6f;


        foldout.style.paddingLeft =
            6f;


        foldout.style.paddingRight =
            6f;


        foldout.style.paddingBottom =
            6f;


        foldout.style.backgroundColor =
            new Color(
                0.16f,
                0.16f,
                0.16f,
                1f
            );


        // =====================================================
        // Mode
        // =====================================================

        EnumField modeField =
            new EnumField(
                "Generation Mode",
                group.GenerationMode
            );


        modeField.RegisterValueChangedCallback(
            evt =>
            {
                ApplyGroupChange(
                    room,
                    "Change Group Generation Mode",
                    () =>
                        group.SetGenerationMode(
                            (CellGroupGenerationMode)
                            evt.newValue
                        )
                );


                rootVisualElement
                    .schedule
                    .Execute(
                        RebuildGroups
                    );
            }
        );


        foldout.Add(
            modeField
        );


        // =====================================================
        // Enabled
        // =====================================================

        Toggle enabledField =
            new Toggle(
                "Enabled"
            );


        enabledField
            .SetValueWithoutNotify(
                group.Enabled
            );


        enabledField.RegisterValueChangedCallback(
            evt =>
            {
                ApplyGroupChange(
                    room,
                    "Change Group Enabled",
                    () =>
                        group.SetEnabled(
                            evt.newValue
                        )
                );
            }
        );


        foldout.Add(
            enabledField
        );


        // =====================================================
        // Single Prefab Options
        // =====================================================

        if (
            group.GenerationMode ==
            CellGroupGenerationMode.SinglePrefab)
        {
            AddSinglePrefabGroupEditor(
                room,
                group,
                foldout
            );
        }
        else
        {
            Label perCellInfo =
                new Label(
                    "PerCell uses the Cell Type mappings from the Generation Profile."
                );


            perCellInfo.style.opacity =
                0.55f;


            perCellInfo.style.whiteSpace =
                WhiteSpace.Normal;


            perCellInfo.style.marginTop =
                4f;


            foldout.Add(
                perCellInfo
            );
        }


        groupsContainer.Add(
            foldout
        );
    }


    // =========================================================
    // Single Prefab Group
    // =========================================================

    private void AddSinglePrefabGroupEditor(
        RoomDefinition room,
        CellGroupData group,
        VisualElement parent)
    {
        // =====================================================
        // Prefab
        // =====================================================

        ObjectField prefabField =
            new ObjectField(
                "Prefab"
            );


        prefabField.objectType =
            typeof(GameObject);


        prefabField.allowSceneObjects =
            false;


        prefabField
            .SetValueWithoutNotify(
                group.Prefab
            );


        prefabField.RegisterValueChangedCallback(
            evt =>
            {
                ApplyGroupChange(
                    room,
                    "Change Group Prefab",
                    () =>
                        group.SetPrefab(
                            evt.newValue
                            as GameObject
                        )
                );
            }
        );


        parent.Add(
            prefabField
        );


        // =====================================================
        // Fit To Footprint
        // =====================================================

        Toggle fitField =
            new Toggle(
                "Fit To Footprint"
            );


        fitField
            .SetValueWithoutNotify(
                group.FitPrefabToFootprint
            );


        fitField.tooltip =
            "Automatically scale X/Z to match the Group footprint.";


        fitField.RegisterValueChangedCallback(
            evt =>
            {
                ApplyGroupChange(
                    room,
                    "Change Group Fit Mode",
                    () =>
                        group.SetFitPrefabToFootprint(
                            evt.newValue
                        )
                );
            }
        );


        parent.Add(
            fitField
        );


        // =====================================================
        // Position Offset
        // =====================================================

        Vector3Field positionField =
            new Vector3Field(
                "Position Offset"
            );


        positionField
            .SetValueWithoutNotify(
                group.PositionOffset
            );


        positionField.RegisterValueChangedCallback(
            evt =>
            {
                ApplyGroupChange(
                    room,
                    "Change Group Position Offset",
                    () =>
                        group.SetPositionOffset(
                            evt.newValue
                        )
                );
            }
        );


        parent.Add(
            positionField
        );


        // =====================================================
        // Rotation
        // =====================================================

        Vector3Field rotationField =
            new Vector3Field(
                "Rotation"
            );


        rotationField
            .SetValueWithoutNotify(
                group.Rotation
            );


        rotationField.RegisterValueChangedCallback(
            evt =>
            {
                ApplyGroupChange(
                    room,
                    "Change Group Rotation",
                    () =>
                        group.SetRotation(
                            evt.newValue
                        )
                );
            }
        );


        parent.Add(
            rotationField
        );


        // =====================================================
        // Scale
        // =====================================================

        Vector3Field scaleField =
            new Vector3Field(
                "Scale Multiplier"
            );


        scaleField
            .SetValueWithoutNotify(
                group.ScaleMultiplier
            );


        scaleField.RegisterValueChangedCallback(
            evt =>
            {
                ApplyGroupChange(
                    room,
                    "Change Group Scale",
                    () =>
                        group.SetScaleMultiplier(
                            evt.newValue
                        )
                );
            }
        );


        parent.Add(
            scaleField
        );
    }


    // =========================================================
    // Apply Group Modification
    // =========================================================

    private static void ApplyGroupChange(
        RoomDefinition room,
        string undoName,
        Action change)
    {
        if (
            room == null ||
            change == null)
        {
            return;
        }


        Undo.RegisterCompleteObjectUndo(
            room,
            undoName
        );


        change.Invoke();


        EditorUtility.SetDirty(
            room
        );
    }


    // =========================================================
    // Group Footprint
    // =========================================================

    private static string GetGroupFootprintLabel(
        CellGroupData group)
    {
        if (
            group == null ||
            group.Cells == null ||
            group.Cells.Count ==
            0)
        {
            return "0×0";
        }


        int minX =
            group.Cells[0].x;


        int maxX =
            group.Cells[0].x;


        int minY =
            group.Cells[0].y;


        int maxY =
            group.Cells[0].y;


        for (
            int i = 1;
            i < group.Cells.Count;
            i++)
        {
            Vector2Int cell =
                group.Cells[i];


            minX =
                Mathf.Min(
                    minX,
                    cell.x
                );


            maxX =
                Mathf.Max(
                    maxX,
                    cell.x
                );


            minY =
                Mathf.Min(
                    minY,
                    cell.y
                );


            maxY =
                Mathf.Max(
                    maxY,
                    cell.y
                );
        }


        return
            $"{maxX - minX + 1}×" +
            $"{maxY - minY + 1}";
    }


    // =========================================================
    // Output
    // =========================================================

    private void UpdateOutputPath()
    {
        if (
            roomField == null ||
            outputPathField == null)
        {
            return;
        }


        RoomDefinition room =
            roomField.value
            as RoomDefinition;


        if (room == null)
            return;


        string roomPath =
            AssetDatabase.GetAssetPath(
                room
            );


        if (
            string.IsNullOrWhiteSpace(
                roomPath
            ))
        {
            return;
        }


        string directory =
            Path.GetDirectoryName(
                roomPath
            );


        if (
            string.IsNullOrWhiteSpace(
                directory
            ))
        {
            directory =
                "Assets";
        }


        directory =
            directory.Replace(
                "\\",
                "/"
            );


        outputPathField
            .SetValueWithoutNotify(
                $"{directory}/{room.name}.prefab"
            );
    }


    // =========================================================
    // Browse
    // =========================================================

    private void BrowseOutput()
    {
        RoomDefinition room =
            roomField.value
            as RoomDefinition;


        string name =
            room != null
                ? room.name
                : "Room";


        string path =
            EditorUtility
                .SaveFilePanelInProject(
                    "Build Room Prefab",
                    name,
                    "prefab",
                    "Choose where to save the Room prefab."
                );


        if (
            string.IsNullOrWhiteSpace(
                path
            ))
        {
            return;
        }


        outputPathField
            .SetValueWithoutNotify(
                path
            );
    }


    // =========================================================
    // Build
    // =========================================================

    private void BuildPrefab()
    {
        RoomDefinition room =
            roomField.value
            as RoomDefinition;


        RoomGenerationSettings settings =
            settingsField.value
            as RoomGenerationSettings;


        if (room == null)
        {
            SetStatus(
                "RoomDefinition is missing."
            );


            return;
        }


        if (settings == null)
        {
            SetStatus(
                "RoomGenerationSettings are missing."
            );


            return;
        }


        if (
            !ValidateGroups(
                room
            ))
        {
            return;
        }


        string output =
            outputPathField.value;


        if (
            string.IsNullOrWhiteSpace(
                output
            ))
        {
            SetStatus(
                "Output path is missing."
            );


            return;
        }


        try
        {
            AssetDatabase.SaveAssets();


            GameObject prefab =
                RoomPrefabBuilder.Build(
                    room,
                    settings,
                    output
                );


            Selection.activeObject =
                prefab;


            EditorGUIUtility.PingObject(
                prefab
            );


            SetStatus(
                $"Build successful:\n{output}"
            );
        }
        catch (Exception exception)
        {
            Debug.LogException(
                exception
            );


            SetStatus(
                $"Build failed:\n{exception.Message}"
            );
        }
    }


    // =========================================================
    // Group Validation
    // =========================================================

    private bool ValidateGroups(
        RoomDefinition room)
    {
        foreach (
            CellGroupData group
            in room.Groups)
        {
            if (
                group == null ||
                !group.Enabled)
            {
                continue;
            }


            if (
                group.GenerationMode !=
                CellGroupGenerationMode.SinglePrefab)
            {
                continue;
            }


            if (
                group.Prefab !=
                null)
            {
                continue;
            }


            SetStatus(
                $"Group '{group.Label}' requires a prefab."
            );


            return false;
        }


        return true;
    }


    // =========================================================
    // Status
    // =========================================================

    private void UpdateStatus()
    {
        RoomDefinition room =
            roomField?.value
            as RoomDefinition;


        if (room == null)
        {
            SetStatus(
                "Select a RoomDefinition."
            );


            return;
        }


        SetStatus(
            $"Ready to build '{room.name}'."
        );
    }


    private void SetStatus(
        string text)
    {
        if (statusLabel != null)
        {
            statusLabel.text =
                text;
        }
    }


    // =========================================================
    // Utility UI
    // =========================================================

    private static void AddHeader(
        VisualElement root,
        string text,
        int fontSize)
    {
        Label label =
            new Label(
                text
            );


        label.style.unityFontStyleAndWeight =
            FontStyle.Bold;


        label.style.fontSize =
            fontSize;


        label.style.marginBottom =
            12f;


        root.Add(
            label
        );
    }


    private static void AddSectionTitle(
        VisualElement root,
        string text)
    {
        Label label =
            new Label(
                text
            );


        label.style.unityFontStyleAndWeight =
            FontStyle.Bold;


        label.style.fontSize =
            11f;


        label.style.marginTop =
            4f;


        label.style.marginBottom =
            6f;


        root.Add(
            label
        );
    }


    private static void AddSeparator(
        VisualElement root)
    {
        VisualElement separator =
            new VisualElement();


        separator.style.height =
            1f;


        separator.style.marginTop =
            12f;


        separator.style.marginBottom =
            12f;


        separator.style.backgroundColor =
            new Color(
                0.25f,
                0.25f,
                0.25f,
                1f
            );


        root.Add(
            separator
        );
    }
}