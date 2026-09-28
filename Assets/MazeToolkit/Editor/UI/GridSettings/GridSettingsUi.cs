using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class GridSettingsUI
    : VisualElement
{
    private readonly MazeEditorContext context;

    private readonly IntegerField widthField;
    private readonly IntegerField heightField;
    private readonly FloatField cellSizeField;
    private readonly Button applyButton;


    public event Action<
        int,
        int,
        float
    > ApplyRequested;


    public GridSettingsUI(
        MazeEditorContext context)
    {
        this.context = context;


        VisualTreeAsset template =
            AssetDatabase
                .LoadAssetAtPath<VisualTreeAsset>(
                    MazeUIPaths.GridSettings
                );

        template.CloneTree(this);


        widthField =
            this.Q<IntegerField>(
                "width-field"
            );

        heightField =
            this.Q<IntegerField>(
                "height-field"
            );

        cellSizeField =
            this.Q<FloatField>(
                "cell-size-field"
            );

        applyButton =
            this.Q<Button>(
                "apply-button"
            );


        widthField.isDelayed = true;
        heightField.isDelayed = true;
        cellSizeField.isDelayed = true;


        applyButton.clicked += Apply;


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
        context.OnSettingsChanged += Refresh;
    }


    private void OnDetach(
        DetachFromPanelEvent evt)
    {
        context.OnSettingsChanged -= Refresh;
    }


    private void Refresh()
    {
        widthField
            .SetValueWithoutNotify(
                context.Width
            );

        heightField
            .SetValueWithoutNotify(
                context.Height
            );

        cellSizeField
            .SetValueWithoutNotify(
                context.CellSize
            );
    }


    private void Apply()
    {
        int width =
            Mathf.Max(
                1,
                widthField.value
            );

        int height =
            Mathf.Max(
                1,
                heightField.value
            );

        float cellSize =
            Mathf.Max(
                1f,
                cellSizeField.value
            );


        ApplyRequested?.Invoke(
            width,
            height,
            cellSize
        );
    }
}