using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class BrushUI
    : VisualElement
{
    private readonly MazeEditorContext context;

    private readonly DropdownField typeField;
    private readonly SliderInt sizeField;
    private readonly Toggle paintOnDragToggle;


    public event Action<int>
        BrushSizeChanged;

    public event Action<bool>
        PaintOnDragChanged;

    private readonly Image typeIcon;
    private readonly Image sizeIcon;
    private readonly Image dragIcon;

    public BrushUI(
        MazeEditorContext context)
    {
        this.context = context;


        VisualTreeAsset template =
            AssetDatabase
                .LoadAssetAtPath<VisualTreeAsset>(
                    MazeUIPaths.Brush
                );

        template.CloneTree(this);


        typeField =
            this.Q<DropdownField>(
                "cell-type-field"
            );

        sizeField =
            this.Q<SliderInt>(
                "brush-size-field"
            );

        paintOnDragToggle =
            this.Q<Toggle>(
                "paint-on-drag-toggle"
            );


        typeField.choices =
            Enum
                .GetNames(
                    typeof(CellType)
                )
                .ToList();


        typeField.RegisterValueChangedCallback(
            OnTypeChanged
        );

        sizeField.RegisterValueChangedCallback(
            evt =>
                BrushSizeChanged?.Invoke(
                    evt.newValue
                )
        );

        paintOnDragToggle
            .RegisterValueChangedCallback(
                evt =>
                    PaintOnDragChanged?.Invoke(
                        evt.newValue
                    )
            );

        typeIcon =
    this.Q<Image>(
        "brush-type-icon"
    );

        sizeIcon =
            this.Q<Image>(
                "brush-size-icon"
            );

        dragIcon =
            this.Q<Image>(
                "brush-drag-icon"
            );


        typeIcon.image =
            AssetDatabase.LoadAssetAtPath<Texture2D>(
                MazeUIPaths.BrushTypeIcon
            );

        sizeIcon.image =
            AssetDatabase.LoadAssetAtPath<Texture2D>(
                MazeUIPaths.BrushSizeIcon
            );

        dragIcon.image =
            AssetDatabase.LoadAssetAtPath<Texture2D>(
                MazeUIPaths.BrushDragIcon
            );


        RegisterCallback<
            AttachToPanelEvent
        >(OnAttach);

        RegisterCallback<
            DetachFromPanelEvent
        >(OnDetach);


        Refresh();
    }


    private void OnAttach(
        AttachToPanelEvent evt)
    {
        context.OnBrushChanged += Refresh;
    }


    private void OnDetach(
        DetachFromPanelEvent evt)
    {
        context.OnBrushChanged -= Refresh;
    }


    private void OnTypeChanged(
        ChangeEvent<string> evt)
    {
        if (
            Enum.TryParse(
                evt.newValue,
                out CellType type
            ))
        {
            context.SetCurrentType(type);
        }
    }


    private void Refresh()
    {
        typeField.SetValueWithoutNotify(
            context
                .CurrentTypeSelected
                .ToString()
        );
    }
}