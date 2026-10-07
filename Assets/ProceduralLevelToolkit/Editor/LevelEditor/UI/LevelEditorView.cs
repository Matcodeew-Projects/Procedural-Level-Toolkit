using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class LevelEditorView
    : VisualElement
{
    private readonly LevelEditorContext context;


    private ObjectField levelField;

    private Button saveButton;

    private Button pingButton;


    private RoomLibraryUI roomLibrary;

    private LevelGraphUI graphUI;

    private LevelInspectorUI inspectorUI;

    private LevelLayoutUI layoutUI;

    private LevelBuildUI buildUI;


    private VisualElement structurePage;

    private VisualElement layoutPage;

    private VisualElement buildPage;


    private Button structureTab;

    private Button layoutTab;

    private Button buildTab;


    public LevelEditorContext Context =>
        context;


    public LevelEditorView()
        : this(
            new LevelEditorContext()
        )
    {
    }


    public LevelEditorView(
        LevelEditorContext context
    )
    {
        this.context =
            context;


        style.flexGrow =
            1;


        LoadVisualTree();

        BuildToolbar();

        BuildModeNavigation();

        BuildStructurePage();

        BuildLayoutPage();

        BuildBuildPage();


        RegisterCallback<
            AttachToPanelEvent
        >(
            OnAttach
        );


        RegisterCallback<
            DetachFromPanelEvent
        >(
            OnDetach
        );


        RefreshLevelField();

        ShowStructure();
    }


    // =========================================================
    // Setup
    // =========================================================

    private void LoadVisualTree()
    {
        VisualTreeAsset template =
            AssetDatabase
                .LoadAssetAtPath<
                    VisualTreeAsset
                >(
                    LevelEditorUIPaths
                        .LevelEditor
                );


        if (template == null)
        {
            Debug.LogError(
                "Missing LevelEditor UXML at: " +
                LevelEditorUIPaths.LevelEditor
            );


            return;
        }


        template.CloneTree(
            this
        );


        StyleSheet stylesheet =
            AssetDatabase
                .LoadAssetAtPath<
                    StyleSheet
                >(
                    LevelEditorUIPaths
                        .LevelEditorStyle
                );


        if (stylesheet != null)
        {
            styleSheets.Add(
                stylesheet
            );
        }
    }


    // =========================================================
    // Top Toolbar
    // =========================================================

    private void BuildToolbar()
    {
        VisualElement host =
            this.Q<VisualElement>(
                "level-toolbar-host"
            );


        if (host == null)
        {
            return;
        }


        levelField =
            new ObjectField(
                "Level"
            )
            {
                objectType =
                    typeof(
                        LevelDefinition
                    ),

                allowSceneObjects =
                    false
            };


        levelField.style.minWidth =
            320f;


        levelField.RegisterValueChangedCallback(
            evt =>
            {
                context.SetLevel(
                    evt.newValue
                        as LevelDefinition
                );


                roomLibrary?
                    .Refresh();
            }
        );


        Button createButton =
            new Button(
                CreateLevelAsset
            )
            {
                text =
                    "New Level"
            };


        saveButton =
            new Button(
                SaveCurrentLevel
            )
            {
                text =
                    "Save"
            };


        pingButton =
            new Button(
                PingCurrentLevel
            )
            {
                text =
                    "Ping"
            };


        host.Add(
            levelField
        );


        host.Add(
            createButton
        );


        host.Add(
            saveButton
        );


        host.Add(
            pingButton
        );
    }


    // =========================================================
    // Mode Navigation
    // =========================================================

    private void BuildModeNavigation()
    {
        VisualElement host =
            this.Q<VisualElement>(
                "level-mode-toolbar-host"
            );


        if (host == null)
        {
            return;
        }


        structureTab =
            CreateTab(
                "Structure",
                ShowStructure
            );


        layoutTab =
            CreateTab(
                "Layout",
                ShowLayout
            );


        buildTab =
            CreateTab(
                "Build",
                ShowBuild
            );


        host.Add(
            structureTab
        );


        host.Add(
            layoutTab
        );


        host.Add(
            buildTab
        );
    }


    private static Button CreateTab(
        string text,
        System.Action action
    )
    {
        Button button =
            new Button(
                action
            )
            {
                text =
                    text
            };


        button.AddToClassList(
            "level-mode-tab"
        );


        return button;
    }


    // =========================================================
    // Structure Page
    // =========================================================

    private void BuildStructurePage()
    {
        structurePage =
            this.Q<VisualElement>(
                "level-structure-page"
            );


        if (structurePage == null)
        {
            Debug.LogError(
                "LevelEditor.uxml is missing level-structure-page."
            );


            return;
        }


        VisualElement oldBody =
            structurePage.Q<VisualElement>(
                "level-structure-body"
            );


        if (oldBody != null)
        {
            oldBody.RemoveFromHierarchy();
        }


        TwoPaneSplitView librarySplit =
            new TwoPaneSplitView(
                0,
                300f,
                TwoPaneSplitViewOrientation
                    .Horizontal
            );


        librarySplit.name =
            "level-library-split";


        librarySplit.AddToClassList(
            "level-structure-split"
        );


        VisualElement libraryHost =
            new VisualElement();


        libraryHost.name =
            "room-library-host";


        libraryHost.AddToClassList(
            "level-editor__library"
        );


        TwoPaneSplitView inspectorSplit =
            new TwoPaneSplitView(
                1,
                320f,
                TwoPaneSplitViewOrientation
                    .Horizontal
            );


        inspectorSplit.name =
            "level-inspector-split";


        inspectorSplit.AddToClassList(
            "level-structure-split"
        );


        VisualElement graphHost =
            new VisualElement();


        graphHost.name =
            "level-graph-host";


        graphHost.AddToClassList(
            "level-editor__graph"
        );


        VisualElement inspectorHost =
            new VisualElement();


        inspectorHost.name =
            "level-inspector-host";


        inspectorHost.AddToClassList(
            "level-editor__inspector"
        );


        inspectorSplit.Add(
            graphHost
        );


        inspectorSplit.Add(
            inspectorHost
        );


        librarySplit.Add(
            libraryHost
        );


        librarySplit.Add(
            inspectorSplit
        );


        structurePage.Add(
            librarySplit
        );


        roomLibrary =
            new RoomLibraryUI();


        graphUI =
            new LevelGraphUI(
                context
            );


        inspectorUI =
            new LevelInspectorUI(
                context
            );


        roomLibrary.AddModuleRequested +=
            OnAddModuleRequested;


        libraryHost.Add(
            roomLibrary
        );


        graphHost.Add(
            graphUI
        );


        inspectorHost.Add(
            inspectorUI
        );
    }


    // =========================================================
    // Layout Page
    // =========================================================

    private void BuildLayoutPage()
    {
        layoutPage =
            this.Q<VisualElement>(
                "level-layout-page"
            );


        VisualElement layoutHost =
            this.Q<VisualElement>(
                "level-layout-host"
            );


        if (layoutPage == null ||
            layoutHost == null)
        {
            Debug.LogError(
                "LevelEditor.uxml is missing Layout hosts."
            );


            return;
        }


        layoutUI =
            new LevelLayoutUI(
                context
            );


        layoutHost.Add(
            layoutUI
        );
    }


    // =========================================================
    // Build Page
    // =========================================================

    private void BuildBuildPage()
    {
        VisualElement pagesHost =
            this.Q<VisualElement>(
                "level-pages"
            );


        if (pagesHost == null)
        {
            Debug.LogError(
                "LevelEditor.uxml is missing level-pages."
            );


            return;
        }


        buildPage =
            new VisualElement();


        buildPage.name =
            "level-build-page";


        buildPage.AddToClassList(
            "level-page"
        );


        buildPage.style.display =
            DisplayStyle.None;


        buildUI =
            new LevelBuildUI(
                context
            );


        buildPage.Add(
            buildUI
        );


        pagesHost.Add(
            buildPage
        );
    }


    // =========================================================
    // Tabs
    // =========================================================

    private void ShowStructure()
    {
        SetPageState(
            structurePage,
            true
        );


        SetPageState(
            layoutPage,
            false
        );


        SetPageState(
            buildPage,
            false
        );


        SetTabState(
            structureTab,
            true
        );


        SetTabState(
            layoutTab,
            false
        );


        SetTabState(
            buildTab,
            false
        );


        graphUI?
            .FocusGraph();
    }


    private void ShowLayout()
    {
        SetPageState(
            structurePage,
            false
        );


        SetPageState(
            layoutPage,
            true
        );


        SetPageState(
            buildPage,
            false
        );


        SetTabState(
            structureTab,
            false
        );


        SetTabState(
            layoutTab,
            true
        );


        SetTabState(
            buildTab,
            false
        );
    }


    private void ShowBuild()
    {
        SetPageState(
            structurePage,
            false
        );


        SetPageState(
            layoutPage,
            false
        );


        SetPageState(
            buildPage,
            true
        );


        SetTabState(
            structureTab,
            false
        );


        SetTabState(
            layoutTab,
            false
        );


        SetTabState(
            buildTab,
            true
        );
    }


    private static void SetPageState(
        VisualElement page,
        bool visible
    )
    {
        if (page == null)
        {
            return;
        }


        page.style.display =
            visible
                ? DisplayStyle.Flex
                : DisplayStyle.None;
    }


    private static void SetTabState(
        Button button,
        bool active
    )
    {
        button?
            .EnableInClassList(
                "level-mode-tab--active",
                active
            );
    }


    // =========================================================
    // Context
    // =========================================================

    private void OnAttach(
        AttachToPanelEvent evt
    )
    {
        context.OnLevelChanged +=
            RefreshLevelField;
    }


    private void OnDetach(
        DetachFromPanelEvent evt
    )
    {
        context.OnLevelChanged -=
            RefreshLevelField;
    }


    private void RefreshLevelField()
    {
        if (levelField == null)
        {
            return;
        }


        levelField.SetValueWithoutNotify(
            context.CurrentLevel
        );


        bool hasLevel =
            context.CurrentLevel != null;


        saveButton?
            .SetEnabled(
                hasLevel
            );


        pingButton?
            .SetEnabled(
                hasLevel
            );
    }


    // =========================================================
    // Module Add
    // =========================================================

    private void OnAddModuleRequested(
        RoomModuleDefinition module
    )
    {
        if (!context.HasLevel)
        {
            Debug.LogWarning(
                "Select or create a LevelDefinition first."
            );


            return;
        }


        Vector2 position =
            graphUI != null
                ? graphUI.GetSuggestedNodePosition()
                : Vector2.zero;


        context.AddModule(
            module,
            position
        );
    }


    // =========================================================
    // Level Asset
    // =========================================================

    private void CreateLevelAsset()
    {
        string path =
            EditorUtility
                .SaveFilePanelInProject(
                    "Create Level Definition",
                    "NewLevel",
                    "asset",
                    "Choose where the LevelDefinition should be saved."
                );


        if (string.IsNullOrWhiteSpace(
                path
            ))
        {
            return;
        }


        LevelDefinition level =
            ScriptableObject
                .CreateInstance<
                    LevelDefinition
                >();


        level.SetLevelName(
            Path.GetFileNameWithoutExtension(
                path
            )
        );


        AssetDatabase.CreateAsset(
            level,
            path
        );


        AssetDatabase.SaveAssets();

        AssetDatabase.Refresh();


        context.SetLevel(
            level
        );


        Selection.activeObject =
            level;


        EditorGUIUtility.PingObject(
            level
        );
    }


    private void SaveCurrentLevel()
    {
        if (!context.SaveCurrentLevel())
        {
            return;
        }


        Debug.Log(
            $"Saved LevelDefinition '{context.CurrentLevel.LevelName}'.",
            context.CurrentLevel
        );
    }


    private void PingCurrentLevel()
    {
        if (context.CurrentLevel == null)
        {
            return;
        }


        Selection.activeObject =
            context.CurrentLevel;


        EditorGUIUtility.PingObject(
            context.CurrentLevel
        );
    }
}
