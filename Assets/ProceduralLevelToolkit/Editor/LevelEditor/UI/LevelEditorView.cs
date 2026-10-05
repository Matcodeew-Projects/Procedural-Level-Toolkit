using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class LevelEditorView : VisualElement
{
    private readonly LevelEditorContext context;

    private ObjectField levelField;
    private Button pingButton;

    private RoomLibraryUI roomLibrary;
    private LevelGraphUI graphUI;
    private LevelInspectorUI inspectorUI;
    private LevelLayoutUI layoutUI;

    private VisualElement structurePage;
    private VisualElement layoutPage;
    private Button structureTab;
    private Button layoutTab;

    public LevelEditorContext Context => context;

    public LevelEditorView() : this(new LevelEditorContext()) { }

    public LevelEditorView(LevelEditorContext context)
    {
        this.context = context;
        style.flexGrow = 1;

        LoadVisualTree();
        BuildToolbar();
        BuildModeNavigation();
        BuildStructurePage();
        BuildLayoutPage();

        RegisterCallback<AttachToPanelEvent>(OnAttach);
        RegisterCallback<DetachFromPanelEvent>(OnDetach);

        RefreshLevelField();
        ShowStructure();
    }

    private void LoadVisualTree()
    {
        VisualTreeAsset template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
            LevelEditorUIPaths.LevelEditor);

        if (template == null)
        {
            Debug.LogError("Missing LevelEditor UXML at: " + LevelEditorUIPaths.LevelEditor);
            return;
        }

        template.CloneTree(this);

        StyleSheet stylesheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
            LevelEditorUIPaths.LevelEditorStyle);

        if (stylesheet != null)
            styleSheets.Add(stylesheet);
    }

    private void BuildToolbar()
    {
        VisualElement host = this.Q<VisualElement>("level-toolbar-host");
        if (host == null)
            return;

        levelField = new ObjectField("Level")
        {
            objectType = typeof(LevelDefinition),
            allowSceneObjects = false
        };
        levelField.style.minWidth = 320f;

        levelField.RegisterValueChangedCallback(evt =>
        {
            context.SetLevel(evt.newValue as LevelDefinition);
            roomLibrary?.Refresh();
        });

        Button createButton = new Button(CreateLevelAsset) { text = "New Level" };
        pingButton = new Button(PingCurrentLevel) { text = "Ping" };

        host.Add(levelField);
        host.Add(createButton);
        host.Add(pingButton);
    }

    private void BuildModeNavigation()
    {
        VisualElement host = this.Q<VisualElement>("level-mode-toolbar-host");
        if (host == null)
            return;

        structureTab = new Button(ShowStructure) { text = "Structure" };
        layoutTab = new Button(ShowLayout) { text = "Layout" };

        structureTab.AddToClassList("level-mode-tab");
        layoutTab.AddToClassList("level-mode-tab");

        host.Add(structureTab);
        host.Add(layoutTab);
    }

    private void BuildStructurePage()
    {
        structurePage = this.Q<VisualElement>("level-structure-page");
        VisualElement libraryHost = this.Q<VisualElement>("room-library-host");
        VisualElement graphHost = this.Q<VisualElement>("level-graph-host");
        VisualElement inspectorHost = this.Q<VisualElement>("level-inspector-host");

        if (structurePage == null || libraryHost == null || graphHost == null || inspectorHost == null)
        {
            Debug.LogError("LevelEditor.uxml is missing Structure hosts.");
            return;
        }

        roomLibrary = new RoomLibraryUI();
        graphUI = new LevelGraphUI(context);
        inspectorUI = new LevelInspectorUI(context);

        roomLibrary.AddModuleRequested += OnAddModuleRequested;

        libraryHost.Add(roomLibrary);
        graphHost.Add(graphUI);
        inspectorHost.Add(inspectorUI);
    }

    private void BuildLayoutPage()
    {
        layoutPage = this.Q<VisualElement>("level-layout-page");
        VisualElement layoutHost = this.Q<VisualElement>("level-layout-host");

        if (layoutPage == null || layoutHost == null)
        {
            Debug.LogError("LevelEditor.uxml is missing Layout hosts.");
            return;
        }

        layoutUI = new LevelLayoutUI(context);
        layoutHost.Add(layoutUI);
    }

    private void ShowStructure()
    {
        if (structurePage == null || layoutPage == null)
            return;

        structurePage.style.display = DisplayStyle.Flex;
        layoutPage.style.display = DisplayStyle.None;
        structureTab?.EnableInClassList("level-mode-tab--active", true);
        layoutTab?.EnableInClassList("level-mode-tab--active", false);
    }

    private void ShowLayout()
    {
        if (structurePage == null || layoutPage == null)
            return;

        structurePage.style.display = DisplayStyle.None;
        layoutPage.style.display = DisplayStyle.Flex;
        structureTab?.EnableInClassList("level-mode-tab--active", false);
        layoutTab?.EnableInClassList("level-mode-tab--active", true);
    }

    private void OnAttach(AttachToPanelEvent evt)
    {
        context.OnLevelChanged += RefreshLevelField;
    }

    private void OnDetach(DetachFromPanelEvent evt)
    {
        context.OnLevelChanged -= RefreshLevelField;
    }

    private void RefreshLevelField()
    {
        if (levelField == null)
            return;

        levelField.SetValueWithoutNotify(context.CurrentLevel);
        pingButton?.SetEnabled(context.CurrentLevel != null);
    }

    private void OnAddModuleRequested(RoomModuleDefinition module)
    {
        if (!context.HasLevel)
        {
            Debug.LogWarning("Select or create a LevelDefinition first.");
            return;
        }

        Vector2 position = graphUI != null
            ? graphUI.GetSuggestedNodePosition()
            : Vector2.zero;

        context.AddModule(module, position);
    }

    private void CreateLevelAsset()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Create Level Definition",
            "NewLevel",
            "asset",
            "Choose where the LevelDefinition should be saved.");

        if (string.IsNullOrWhiteSpace(path))
            return;

        LevelDefinition level = ScriptableObject.CreateInstance<LevelDefinition>();
        level.SetLevelName(Path.GetFileNameWithoutExtension(path));

        AssetDatabase.CreateAsset(level, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        context.SetLevel(level);
        Selection.activeObject = level;
        EditorGUIUtility.PingObject(level);
    }

    private void PingCurrentLevel()
    {
        if (context.CurrentLevel == null)
            return;

        Selection.activeObject = context.CurrentLevel;
        EditorGUIUtility.PingObject(context.CurrentLevel);
    }
}
