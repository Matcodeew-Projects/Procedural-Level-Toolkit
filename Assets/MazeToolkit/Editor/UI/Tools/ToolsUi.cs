using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class ToolsUI
    : VisualElement
{
    public event Action<MazeEditorTool>
        ToolChanged;

    public event Action
        ClearRequested;


    private readonly Button brushButton;
    private readonly Button eraserButton;
    private readonly Button fillButton;
    private readonly Button clearButton;


    private readonly Image brushIcon;
    private readonly Image eraserIcon;
    private readonly Image fillIcon;
    private readonly Image clearIcon;


    public MazeEditorTool CurrentTool
    {
        get;
        private set;
    } = MazeEditorTool.Brush;


    public ToolsUI()
    {
        VisualTreeAsset template =
            AssetDatabase
                .LoadAssetAtPath<VisualTreeAsset>(
                    MazeUIPaths.Tools
                );

        template.CloneTree(this);


        // ==============================
        // Buttons
        // ==============================

        brushButton =
            this.Q<Button>(
                "brush-tool"
            );

        eraserButton =
            this.Q<Button>(
                "eraser-tool"
            );

        fillButton =
            this.Q<Button>(
                "fill-tool"
            );

        clearButton =
            this.Q<Button>(
                "clear-button"
            );


        // ==============================
        // Images
        // ==============================

        brushIcon =
            this.Q<Image>(
                "brush-tool-icon"
            );

        eraserIcon =
            this.Q<Image>(
                "eraser-tool-icon"
            );

        fillIcon =
            this.Q<Image>(
                "fill-tool-icon"
            );

        clearIcon =
            this.Q<Image>(
                "clear-tool-icon"
            );


        LoadIcons();


        // ==============================
        // Events
        // ==============================

        brushButton.clicked +=
            () => SelectTool(
                MazeEditorTool.Brush
            );

        eraserButton.clicked +=
            () => SelectTool(
                MazeEditorTool.Eraser
            );

        fillButton.clicked +=
            () => SelectTool(
                MazeEditorTool.Fill
            );

        clearButton.clicked +=
            () => ClearRequested?.Invoke();


        RefreshButtons();
    }


    private void LoadIcons()
    {
        brushIcon.image =
            LoadIcon(
                MazeUIPaths.BrushToolIcon
            );

        eraserIcon.image =
            LoadIcon(
                MazeUIPaths.EraserToolIcon
            );

        fillIcon.image =
            LoadIcon(
                MazeUIPaths.FillToolIcon
            );

        clearIcon.image =
            LoadIcon(
                MazeUIPaths.ClearToolIcon
            );
    }


    private Texture2D LoadIcon(
        string path)
    {
        return AssetDatabase
            .LoadAssetAtPath<Texture2D>(
                path
            );
    }


    private void SelectTool(
        MazeEditorTool tool)
    {
        if (CurrentTool == tool)
            return;

        CurrentTool = tool;

        RefreshButtons();

        ToolChanged?.Invoke(tool);
    }


    private void RefreshButtons()
    {
        brushButton.EnableInClassList(
            "maze-tool--selected",
            CurrentTool ==
            MazeEditorTool.Brush
        );

        eraserButton.EnableInClassList(
            "maze-tool--selected",
            CurrentTool ==
            MazeEditorTool.Eraser
        );

        fillButton.EnableInClassList(
            "maze-tool--selected",
            CurrentTool ==
            MazeEditorTool.Fill
        );
    }
}