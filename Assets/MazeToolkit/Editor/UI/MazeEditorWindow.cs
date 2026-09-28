using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class MazeEditorWindow
    : EditorWindow
{
    private const float CompactWidth =
        760f;


    private MazeEditorContext context;

    private InputHandler inputHandler;


    private GridPreviewUI gridPreviewUI;

    private GridSettingsUI gridSettingsUI;

    private ToolsUI toolsUI;

    private BrushUI brushUI;


    private VisualElement editorRoot;


    [MenuItem(
        "Maze Toolkit/Maze Editor"
    )]
    public static void Open()
    {
        MazeEditorWindow window =
            GetWindow<MazeEditorWindow>();

        window.titleContent =
            new GUIContent(
                "Maze Toolkit"
            );

        window.minSize =
            new Vector2(
                480,
                420
            );
    }


    public void CreateGUI()
    {
        rootVisualElement.Clear();


        // ==========================================
        // Context
        // ==========================================

        context =
            new MazeEditorContext(
                20,
                20,
                24f
            );


        inputHandler =
            new InputHandler(
                context
            );


        // ==========================================
        // Main UXML
        // ==========================================

        VisualTreeAsset mainTemplate =
            AssetDatabase
                .LoadAssetAtPath<VisualTreeAsset>(
                    MazeUIPaths.MainWindow
                );


        mainTemplate.CloneTree(
            rootVisualElement
        );


        StyleSheet styleSheet =
            AssetDatabase
                .LoadAssetAtPath<StyleSheet>(
                    MazeUIPaths.Style
                );


        rootVisualElement
            .styleSheets
            .Add(styleSheet);


        editorRoot =
            rootVisualElement
                .Q<VisualElement>(
                    "maze-editor-root"
                );


        // ==========================================
        // Components
        // ==========================================

        gridSettingsUI =
            new GridSettingsUI(
                context
            );

        toolsUI =
            new ToolsUI();

        brushUI =
            new BrushUI(
                context
            );

        gridPreviewUI =
            new GridPreviewUI(
                context
            );


        rootVisualElement
            .Q<VisualElement>(
                "grid-settings-host"
            )
            .Add(gridSettingsUI);


        rootVisualElement
            .Q<VisualElement>(
                "tools-host"
            )
            .Add(toolsUI);


        rootVisualElement
            .Q<VisualElement>(
                "brush-host"
            )
            .Add(brushUI);


        rootVisualElement
            .Q<VisualElement>(
                "grid-preview-host"
            )
            .Add(gridPreviewUI);


        // ==========================================
        // Input
        // ==========================================

        inputHandler.Bind(
            gridPreviewUI
        );


        // ==========================================
        // Events
        // ==========================================

        gridSettingsUI.ApplyRequested +=
            OnGridSettingsChanged;


        toolsUI.ToolChanged +=
            inputHandler.SetTool;


        toolsUI.ClearRequested +=
            () => context.Clear();


        brushUI.BrushSizeChanged +=
            inputHandler.SetBrushSize;


        brushUI.PaintOnDragChanged +=
            inputHandler.SetPaintOnDrag;


        // ==========================================
        // Responsive
        // ==========================================

        editorRoot.RegisterCallback<
            GeometryChangedEvent
        >(OnGeometryChanged);
    }


    private void OnGridSettingsChanged(
        int width,
        int height,
        float cellSize)
    {
        context.Resize(
            width,
            height,
            cellSize
        );
    }


    private void OnGeometryChanged(
        GeometryChangedEvent evt)
    {
        bool compact =
            evt.newRect.width <
            CompactWidth;


        editorRoot.EnableInClassList(
            "maze-editor--compact",
            compact
        );
    }


    private void OnDisable()
    {
        inputHandler?.Unbind();
    }
}