using System.Collections.Generic;
using UnityEngine.UIElements;

public sealed class LevelLayoutUI : VisualElement
{
    private readonly LevelEditorContext context;

    private readonly DropdownField rootField;
    private readonly Button solveButton;
    private readonly Button clearButton;
    private readonly Button buildPrefabButton;
    private readonly Label stateLabel;
    private readonly ScrollView report;

    private readonly LayoutPreviewUI preview2D;
    private readonly LevelPreview3DUI preview3D;
    private readonly Button preview2DButton;
    private readonly Button preview3DButton;

    private readonly Dictionary<string, string> rootLabelToId =
        new Dictionary<string, string>();

    public LevelLayoutUI(LevelEditorContext context)
    {
        this.context = context;
        AddToClassList("level-layout");

        VisualElement toolbar = new VisualElement();
        toolbar.AddToClassList("level-layout__toolbar");

        rootField = new DropdownField("Root");
        rootField.style.minWidth = 260f;
        rootField.RegisterValueChangedCallback(evt =>
        {
            if (rootLabelToId.TryGetValue(evt.newValue, out string nodeId))
                context.SetLayoutRoot(nodeId);
        });

        solveButton = new Button(Solve) { text = "Solve Layout" };
        clearButton = new Button(context.ClearLayout) { text = "Clear" };
        buildPrefabButton = new Button(BuildPrefab) { text = "Build Level Prefab" };

        stateLabel = new Label("Not solved");
        stateLabel.AddToClassList("level-layout__state");

        toolbar.Add(rootField);
        toolbar.Add(solveButton);
        toolbar.Add(clearButton);
        toolbar.Add(buildPrefabButton);
        toolbar.Add(stateLabel);
        Add(toolbar);

        VisualElement previewToolbar = new VisualElement();
        previewToolbar.style.height = 32f;
        previewToolbar.style.flexShrink = 0f;
        previewToolbar.style.flexDirection = FlexDirection.Row;
        previewToolbar.style.alignItems = Align.Center;

        preview2DButton = new Button(Show2D) { text = "2D Layout" };
        preview3DButton = new Button(Show3D) { text = "3D Preview" };

        previewToolbar.Add(preview2DButton);
        previewToolbar.Add(preview3DButton);
        Add(previewToolbar);

        VisualElement body = new VisualElement();
        body.AddToClassList("level-layout__body");

        VisualElement previewHost = new VisualElement();
        previewHost.style.flexGrow = 1;
        previewHost.style.minWidth = 0f;
        previewHost.style.minHeight = 0f;

        preview2D = new LayoutPreviewUI(context);
        preview3D = new LevelPreview3DUI(context);

        previewHost.Add(preview2D);
        previewHost.Add(preview3D);
        body.Add(previewHost);

        report = new ScrollView();
        report.AddToClassList("level-layout__report");
        body.Add(report);

        Add(body);

        RegisterCallback<AttachToPanelEvent>(OnAttach);
        RegisterCallback<DetachFromPanelEvent>(OnDetach);

        RefreshAll();
        Show2D();
    }

    private void OnAttach(AttachToPanelEvent evt)
    {
        context.OnLevelChanged += RefreshAll;
        context.OnGraphChanged += RefreshRootChoices;
        context.OnLayoutChanged += RefreshReport;
        context.OnLayoutRootChanged += RefreshRootChoices;
    }

    private void OnDetach(DetachFromPanelEvent evt)
    {
        context.OnLevelChanged -= RefreshAll;
        context.OnGraphChanged -= RefreshRootChoices;
        context.OnLayoutChanged -= RefreshReport;
        context.OnLayoutRootChanged -= RefreshRootChoices;
    }

    private void Solve()
    {
        context.SolveLayout();
        preview2D.Fit();
        preview3D.Rebuild();
    }

    private void BuildPrefab()
    {
        LevelPrefabBuilder.BuildWithSaveDialog(context.CurrentLevel);
    }

    private void Show2D()
    {
        preview2D.style.display = DisplayStyle.Flex;
        preview3D.style.display = DisplayStyle.None;
        preview2DButton.SetEnabled(false);
        preview3DButton.SetEnabled(true);
    }

    private void Show3D()
    {
        preview2D.style.display = DisplayStyle.None;
        preview3D.style.display = DisplayStyle.Flex;
        preview2DButton.SetEnabled(true);
        preview3DButton.SetEnabled(false);
        preview3D.Rebuild();
    }

    private void RefreshAll()
    {
        RefreshRootChoices();
        RefreshReport();
    }

    private void RefreshRootChoices()
    {
        rootLabelToId.Clear();
        List<string> choices = new List<string>();

        if (context.Graph != null)
        {
            for (int i = 0; i < context.Graph.Nodes.Count; i++)
            {
                LevelNodeData node = context.Graph.Nodes[i];
                if (node == null)
                    continue;

                string moduleName = node.Module != null
                    ? node.Module.DisplayName
                    : (node.Room != null ? node.Room.name : "Missing Room");

                string shortId = node.Id != null && node.Id.Length > 6
                    ? node.Id.Substring(0, 6)
                    : node.Id;

                string label = $"{moduleName} [{shortId}]";
                choices.Add(label);
                rootLabelToId[label] = node.Id;
            }
        }

        rootField.choices = choices;

        string currentLabel = null;
        foreach (KeyValuePair<string, string> pair in rootLabelToId)
        {
            if (pair.Value == context.LayoutRootNodeId)
            {
                currentLabel = pair.Key;
                break;
            }
        }

        rootField.SetValueWithoutNotify(
            currentLabel ?? (choices.Count > 0 ? choices[0] : string.Empty));

        solveButton.SetEnabled(
            context.HasLevel &&
            context.Graph != null &&
            context.Graph.NodeCount > 0);
    }

    private void RefreshReport()
    {
        report.Clear();

        SpatialLayoutData layout = context.SpatialLayout;
        bool solved = layout != null && layout.IsSolved;
        buildPrefabButton.SetEnabled(solved && HasBuildableModules());

        if (layout == null)
        {
            stateLabel.text = "No level";
            AddReportInfo("Select or create a LevelDefinition.");
            return;
        }

        LayoutSolveResult result = context.LastSolveResult;

        if (result == null)
        {
            stateLabel.text = solved ? "Solved" : "Not solved";
            AddReportInfo($"Modules: {layout.ModuleCount}");
            AddReportInfo($"Resolved links: {layout.SocketConnectionCount}");
            AddModuleBuildValidation();
            return;
        }

        stateLabel.text = result.Success && layout.IsSolved ? "Solved" : "Failed";

        AddReportTitle("Layout Result");
        AddReportInfo($"Placed rooms: {result.PlacedModuleCount}/{result.TotalModuleCount}");
        AddReportInfo($"Resolved connections: {result.ResolvedConnectionCount}/{result.TotalConnectionCount}");

        for (int i = 0; i < result.Errors.Count; i++)
            AddReportError(result.Errors[i]);

        for (int i = 0; i < result.Warnings.Count; i++)
            AddReportWarning(result.Warnings[i]);

        if (result.Success)
            AddReportSuccess("All graph constraints were resolved.");

        AddModuleBuildValidation();
    }

    private bool HasBuildableModules()
    {
        if (context.Graph == null || context.Graph.NodeCount == 0)
            return false;

        float cellSize = -1f;

        for (int i = 0; i < context.Graph.Nodes.Count; i++)
        {
            LevelNodeData node = context.Graph.Nodes[i];
            if (node?.Module == null || !node.Module.IsBuildable)
                return false;

            if (cellSize < 0f)
                cellSize = node.Module.CellWorldSize;
            else if (UnityEngine.Mathf.Abs(cellSize - node.Module.CellWorldSize) > 0.0001f)
                return false;
        }

        return true;
    }

    private void AddModuleBuildValidation()
    {
        if (context.Graph == null)
            return;

        AddReportTitle("3D Build");

        int missingModules = 0;
        int missingPrefabs = 0;
        bool mixedCellSize = false;
        float cellSize = -1f;

        for (int i = 0; i < context.Graph.Nodes.Count; i++)
        {
            LevelNodeData node = context.Graph.Nodes[i];
            if (node == null)
                continue;

            if (node.Module == null)
            {
                missingModules++;
                continue;
            }

            if (node.Module.Prefab == null)
                missingPrefabs++;

            if (cellSize < 0f)
                cellSize = node.Module.CellWorldSize;
            else if (UnityEngine.Mathf.Abs(cellSize - node.Module.CellWorldSize) > 0.0001f)
                mixedCellSize = true;
        }

        if (missingModules == 0 && missingPrefabs == 0 && !mixedCellSize)
        {
            AddReportSuccess("All nodes have buildable Room Modules.");
            AddReportInfo($"World Units / Cell: {cellSize:0.###}");
            return;
        }

        if (missingModules > 0)
            AddReportError($"{missingModules} node(s) have no RoomModuleDefinition.");

        if (missingPrefabs > 0)
            AddReportError($"{missingPrefabs} module(s) have no prefab.");

        if (mixedCellSize)
            AddReportError("Room Modules use different World Units / Cell values.");
    }

    private void AddReportTitle(string text)
    {
        Label label = new Label(text);
        label.AddToClassList("level-layout__report-title");
        report.Add(label);
    }

    private void AddReportInfo(string text)
    {
        Label label = new Label(text);
        label.style.whiteSpace = WhiteSpace.Normal;
        report.Add(label);
    }

    private void AddReportSuccess(string text)
    {
        Label label = new Label("✓ " + text);
        label.style.whiteSpace = WhiteSpace.Normal;
        report.Add(label);
    }

    private void AddReportWarning(string text)
    {
        Label label = new Label("⚠ " + text);
        label.style.whiteSpace = WhiteSpace.Normal;
        report.Add(label);
    }

    private void AddReportError(string text)
    {
        Label label = new Label("✕ " + text);
        label.style.whiteSpace = WhiteSpace.Normal;
        report.Add(label);
    }
}
