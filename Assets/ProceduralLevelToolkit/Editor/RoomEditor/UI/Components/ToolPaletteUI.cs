using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class ToolPaletteUI
    : IDisposable
{
    private readonly RoomEditorContext context;

    private readonly Label activeToolLabel;

    private readonly VisualElement cellTypeList;
    private readonly Label cellTypePlaceholder;

    private readonly SliderInt brushSizeSlider;
    private readonly IntegerField brushSizeIntegerField;

    private readonly Toggle continuousPaintToggle;

    private readonly Dictionary<RoomEditorTool, Button>
        toolButtons = new();

    private readonly List<Button>
        cellTypeButtons = new();


    // =========================================================
    // Constructor
    // =========================================================

    public ToolPaletteUI(
        VisualElement root,
        RoomEditorContext context)
    {
        if (root == null)
            throw new ArgumentNullException(nameof(root));

        if (context == null)
            throw new ArgumentNullException(nameof(context));


        this.context = context;


        // =====================================================
        // General UI
        // =====================================================

        activeToolLabel =
            FindOptional<Label>(
                root,
                "active-tool-label"
            );


        cellTypeList =
            RequireAny<VisualElement>(
                root,
                "cell-type-list"
            );


        cellTypePlaceholder =
            FindOptional<Label>(
                root,
                "cell-type-list-placeholder"
            );


        continuousPaintToggle =
            RequireAny<Toggle>(
                root,
                "continuous-paint-toggle",
                "paint-on-drag-toggle"
            );


        // =====================================================
        // Brush size
        // =====================================================

        VisualElement brushSizeElement =
            RequireAny<VisualElement>(
                root,
                "brush-size-field"
            );


        brushSizeSlider =
            brushSizeElement as SliderInt;


        brushSizeIntegerField =
            brushSizeElement as IntegerField;


        if (
            brushSizeSlider == null &&
            brushSizeIntegerField == null)
        {
            throw new InvalidOperationException(
                "[RoomEditor] 'brush-size-field' " +
                "must be either a SliderInt or IntegerField."
            );
        }


        // =====================================================
        // Tool buttons
        // =====================================================

        RegisterTool(
            root,
            RoomEditorTool.Select,
            "select-tool-button"
        );


        RegisterTool(
            root,
            RoomEditorTool.Paint,
            "paint-tool-button",
            "brush-tool"
        );


        RegisterTool(
            root,
            RoomEditorTool.Erase,
            "erase-tool-button",
            "eraser-tool"
        );


        RegisterTool(
            root,
            RoomEditorTool.Fill,
            "fill-tool-button",
            "fill-tool"
        );


        RegisterTool(
            root,
            RoomEditorTool.Group,
            "group-tool-button"
        );


        RegisterTool(
            root,
            RoomEditorTool.Socket,
            "socket-tool-button"
        );


        // =====================================================
        // Brush events
        // =====================================================

        if (brushSizeSlider != null)
        {
            brushSizeSlider.lowValue = 1;
            brushSizeSlider.highValue = 16;

            brushSizeSlider
                .SetValueWithoutNotify(
                    context.BrushSize
                );


            brushSizeSlider
                .RegisterValueChangedCallback(
                    OnBrushSliderChanged
                );
        }


        if (brushSizeIntegerField != null)
        {
            brushSizeIntegerField
                .SetValueWithoutNotify(
                    context.BrushSize
                );


            brushSizeIntegerField
                .RegisterValueChangedCallback(
                    OnBrushIntegerChanged
                );
        }


        continuousPaintToggle
            .SetValueWithoutNotify(
                context.ContinuousPaint
            );


        continuousPaintToggle
            .RegisterValueChangedCallback(
                OnContinuousPaintChanged
            );


        // =====================================================
        // Context events
        // =====================================================

        context.ToolChanged +=
            RefreshToolState;

        context.BrushChanged +=
            RefreshBrushState;

        context.CellTypeChanged +=
            RefreshCellTypeState;


        RefreshToolState();
        RefreshBrushState();
        RefreshCellTypeState();
    }


    // =========================================================
    // Tools
    // =========================================================

    private void RegisterTool(
        VisualElement root,
        RoomEditorTool tool,
        params string[] names)
    {
        Button button =
            RequireAny<Button>(
                root,
                names
            );


        toolButtons.Add(
            tool,
            button
        );


        button.clicked +=
            () =>
                context.SetTool(
                    tool
                );
    }


    private void RefreshToolState()
    {
        if (activeToolLabel != null)
        {
            activeToolLabel.text =
                context
                    .CurrentTool
                    .ToString();
        }


        foreach (
            KeyValuePair<RoomEditorTool, Button>
            pair
            in toolButtons)
        {
            bool selected =
                pair.Key ==
                context.CurrentTool;


            /*
             * On applique les deux noms.
             *
             * Si ton USS n'en possède qu'un,
             * l'autre ne fait simplement rien.
             */
            pair.Value.EnableInClassList(
                "tool-button--active",
                selected
            );


            pair.Value.EnableInClassList(
                "tool-button--selected",
                selected
            );
        }
    }


    // =========================================================
    // Cell Types
    // =========================================================

    public void SetCellTypes(
        IEnumerable<CellTypeDefinition> definitions)
    {
        foreach (
            Button button
            in cellTypeButtons)
        {
            button.RemoveFromHierarchy();
        }


        cellTypeButtons.Clear();


        bool hasCellTypes =
            false;


        if (definitions != null)
        {
            foreach (
                CellTypeDefinition definition
                in definitions)
            {
                if (definition == null)
                    continue;


                hasCellTypes =
                    true;


                Button button =
                    new Button
                    {
                        text =
                            definition.DisplayName,

                        userData =
                            definition
                    };


                button.AddToClassList(
                    "button"
                );


                button.AddToClassList(
                    "button--compact"
                );


                button.clicked +=
                    () =>
                    {
                        context.SetCurrentCellType(
                            definition
                        );


                        /*
                         * Choisir un type de cellule
                         * passe automatiquement au Paint.
                         */
                        context.SetTool(
                            RoomEditorTool.Paint
                        );
                    };


                cellTypeList.Add(
                    button
                );


                cellTypeButtons.Add(
                    button
                );
            }
        }


        if (cellTypePlaceholder != null)
        {
            cellTypePlaceholder
                .EnableInClassList(
                    "hidden",
                    hasCellTypes
                );
        }


        RefreshCellTypeState();
    }


    private void RefreshCellTypeState()
    {
        foreach (
            Button button
            in cellTypeButtons)
        {
            bool selected =
                ReferenceEquals(
                    button.userData,
                    context.CurrentCellType
                );


            button.EnableInClassList(
                "tool-button--active",
                selected
            );


            button.EnableInClassList(
                "tool-button--selected",
                selected
            );
        }
    }


    // =========================================================
    // Brush
    // =========================================================

    private void OnBrushSliderChanged(
        ChangeEvent<int> evt)
    {
        context.SetBrushSize(
            evt.newValue
        );
    }


    private void OnBrushIntegerChanged(
        ChangeEvent<int> evt)
    {
        context.SetBrushSize(
            evt.newValue
        );
    }


    private void OnContinuousPaintChanged(
        ChangeEvent<bool> evt)
    {
        context.SetContinuousPaint(
            evt.newValue
        );
    }


    private void RefreshBrushState()
    {
        if (brushSizeSlider != null)
        {
            brushSizeSlider
                .SetValueWithoutNotify(
                    context.BrushSize
                );
        }


        if (brushSizeIntegerField != null)
        {
            brushSizeIntegerField
                .SetValueWithoutNotify(
                    context.BrushSize
                );
        }


        continuousPaintToggle
            .SetValueWithoutNotify(
                context.ContinuousPaint
            );
    }


    // =========================================================
    // Queries
    // =========================================================

    private static T RequireAny<T>(
        VisualElement root,
        params string[] names)
        where T : VisualElement
    {
        foreach (string name in names)
        {
            T element =
                root.Q<T>(
                    name
                );


            if (element != null)
                return element;
        }


        throw new InvalidOperationException(
            $"[RoomEditor] Missing UI element of type " +
            $"'{typeof(T).Name}'. Expected one of: " +
            $"{string.Join(", ", names)}"
        );
    }


    private static T FindOptional<T>(
        VisualElement root,
        string name)
        where T : VisualElement
    {
        return root.Q<T>(
            name
        );
    }


    // =========================================================
    // Dispose
    // =========================================================

    public void Dispose()
    {
        context.ToolChanged -=
            RefreshToolState;

        context.BrushChanged -=
            RefreshBrushState;

        context.CellTypeChanged -=
            RefreshCellTypeState;


        if (brushSizeSlider != null)
        {
            brushSizeSlider
                .UnregisterValueChangedCallback(
                    OnBrushSliderChanged
                );
        }


        if (brushSizeIntegerField != null)
        {
            brushSizeIntegerField
                .UnregisterValueChangedCallback(
                    OnBrushIntegerChanged
                );
        }


        continuousPaintToggle
            .UnregisterValueChangedCallback(
                OnContinuousPaintChanged
            );
    }
}