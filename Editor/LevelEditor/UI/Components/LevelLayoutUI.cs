using System.Collections.Generic;
using UnityEngine.UIElements;

public sealed class LevelLayoutUI
    : VisualElement
{
    private readonly LevelEditorContext context;


    private readonly DropdownField rootField;

    private readonly Button solveButton;

    private readonly Button clearButton;

    private readonly Label stateLabel;

    private readonly ScrollView report;


    private readonly LayoutPreviewUI preview2D;

    private readonly LevelPreview3DUI preview3D;


    private readonly Dictionary<
        string,
        string
    > rootLabelToId =
        new Dictionary<
            string,
            string
        >();


    public LevelLayoutUI(
        LevelEditorContext context
    )
    {
        this.context =
            context;


        AddToClassList(
            "level-layout"
        );


        // =====================================================
        // Toolbar
        // =====================================================

        VisualElement toolbar =
            new VisualElement();


        toolbar.AddToClassList(
            "level-layout__toolbar"
        );


        rootField =
            new DropdownField(
                "Root"
            );


        rootField.style.minWidth =
            250f;


        rootField.RegisterValueChangedCallback(
            evt =>
            {
                if (rootLabelToId.TryGetValue(
                        evt.newValue,
                        out string nodeId
                    ))
                {
                    context.SetLayoutRoot(
                        nodeId
                    );
                }
            }
        );


        solveButton =
            new Button(
                Solve
            )
            {
                text =
                    "Solve Layout"
            };


        clearButton =
            new Button(
                context.ClearLayout
            )
            {
                text =
                    "Clear"
            };


        stateLabel =
            new Label(
                "Not solved"
            );


        stateLabel.AddToClassList(
            "level-layout__state"
        );


        toolbar.Add(
            rootField
        );


        toolbar.Add(
            solveButton
        );


        toolbar.Add(
            clearButton
        );


        toolbar.Add(
            stateLabel
        );


        Add(
            toolbar
        );


        // =====================================================
        // Body
        // =====================================================

        TwoPaneSplitView reportSplit =
            new TwoPaneSplitView(
                1,
                300f,
                TwoPaneSplitViewOrientation
                    .Horizontal
            );


        reportSplit.AddToClassList(
            "level-layout__main-split"
        );


        TwoPaneSplitView previewSplit =
            new TwoPaneSplitView(
                0,
                620f,
                TwoPaneSplitViewOrientation
                    .Horizontal
            );


        previewSplit.AddToClassList(
            "level-layout__preview-split"
        );


        VisualElement preview2DPanel =
            CreatePreviewPanel(
                "2D LAYOUT",
                "Logical spatial result"
            );


        VisualElement preview3DPanel =
            CreatePreviewPanel(
                "3D PREVIEW",
                "Final module assembly"
            );


        preview2D =
            new LayoutPreviewUI(
                context
            );


        preview3D =
            new LevelPreview3DUI(
                context
            );


        preview2DPanel.Add(
            preview2D
        );


        preview3DPanel.Add(
            preview3D
        );


        previewSplit.Add(
            preview2DPanel
        );


        previewSplit.Add(
            preview3DPanel
        );


        report =
            new ScrollView();


        report.AddToClassList(
            "level-layout__report"
        );


        reportSplit.Add(
            previewSplit
        );


        reportSplit.Add(
            report
        );


        Add(
            reportSplit
        );


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


        RefreshAll();
    }


    private static VisualElement CreatePreviewPanel(
        string title,
        string subtitle
    )
    {
        VisualElement panel =
            new VisualElement();


        panel.AddToClassList(
            "level-layout-preview-panel"
        );


        VisualElement header =
            new VisualElement();


        header.AddToClassList(
            "level-layout-preview-panel__header"
        );


        Label titleLabel =
            new Label(
                title
            );


        titleLabel.AddToClassList(
            "level-layout-preview-panel__title"
        );


        Label subtitleLabel =
            new Label(
                subtitle
            );


        subtitleLabel.AddToClassList(
            "level-layout-preview-panel__subtitle"
        );


        header.Add(
            titleLabel
        );


        header.Add(
            subtitleLabel
        );


        panel.Add(
            header
        );


        return panel;
    }


    // =========================================================
    // Events
    // =========================================================

    private void OnAttach(
        AttachToPanelEvent evt
    )
    {
        context.OnLevelChanged +=
            RefreshAll;

        context.OnGraphChanged +=
            RefreshRootChoices;

        context.OnLayoutChanged +=
            RefreshReport;

        context.OnLayoutRootChanged +=
            RefreshRootChoices;
    }


    private void OnDetach(
        DetachFromPanelEvent evt
    )
    {
        context.OnLevelChanged -=
            RefreshAll;

        context.OnGraphChanged -=
            RefreshRootChoices;

        context.OnLayoutChanged -=
            RefreshReport;

        context.OnLayoutRootChanged -=
            RefreshRootChoices;
    }


    // =========================================================
    // Actions
    // =========================================================

    private void Solve()
    {
        context.SolveLayout();


        preview2D.Fit();


        preview3D.Rebuild();
    }


    // =========================================================
    // Refresh
    // =========================================================

    private void RefreshAll()
    {
        RefreshRootChoices();

        RefreshReport();
    }


    private void RefreshRootChoices()
    {
        rootLabelToId.Clear();


        List<string> choices =
            new List<string>();


        if (context.Graph != null)
        {
            for (int i = 0;
                 i < context.Graph.Nodes.Count;
                 i++)
            {
                LevelNodeData node =
                    context.Graph.Nodes[i];


                if (node == null)
                {
                    continue;
                }


                string moduleName =
                    node.Module != null
                        ? node.Module.DisplayName
                        : (
                            node.Room != null
                                ? node.Room.name
                                : "Missing Room"
                        );


                string shortId =
                    node.Id != null &&
                    node.Id.Length >
                    6
                        ? node.Id.Substring(
                            0,
                            6
                        )
                        : node.Id;


                string label =
                    $"{moduleName} [{shortId}]";


                choices.Add(
                    label
                );


                rootLabelToId[
                    label
                ] =
                    node.Id;
            }
        }


        rootField.choices =
            choices;


        string currentLabel =
            null;


        foreach (
            KeyValuePair<
                string,
                string
            > pair
            in rootLabelToId
        )
        {
            if (pair.Value ==
                context.LayoutRootNodeId)
            {
                currentLabel =
                    pair.Key;


                break;
            }
        }


        rootField.SetValueWithoutNotify(
            currentLabel
            ??
            (
                choices.Count >
                0
                    ? choices[0]
                    : string.Empty
            )
        );


        solveButton.SetEnabled(
            context.HasLevel &&
            context.Graph != null &&
            context.Graph.NodeCount >
            0
        );
    }


    private void RefreshReport()
    {
        report.Clear();


        SpatialLayoutData layout =
            context.SpatialLayout;


        if (layout == null)
        {
            stateLabel.text =
                "No level";


            AddReportInfo(
                "Select or create a LevelDefinition."
            );


            return;
        }


        LayoutSolveResult result =
            context.LastSolveResult;


        if (result == null)
        {
            stateLabel.text =
                layout.IsSolved
                    ? "Solved"
                    : "Not solved";


            AddReportInfo(
                $"Modules: {layout.ModuleCount}"
            );


            AddReportInfo(
                $"Resolved links: {layout.SocketConnectionCount}"
            );


            AddReportInfo(
                "Use the Build tab for full validation and prefab generation."
            );


            return;
        }


        stateLabel.text =
            result.Success &&
            layout.IsSolved
                ? "Solved"
                : "Failed";


        AddReportTitle(
            "Layout Result"
        );


        AddReportInfo(
            $"Placed rooms: " +
            $"{result.PlacedModuleCount}/" +
            $"{result.TotalModuleCount}"
        );


        AddReportInfo(
            $"Resolved connections: " +
            $"{result.ResolvedConnectionCount}/" +
            $"{result.TotalConnectionCount}"
        );


        for (int i = 0;
             i < result.Errors.Count;
             i++)
        {
            AddReportError(
                result.Errors[i]
            );
        }


        for (int i = 0;
             i < result.Warnings.Count;
             i++)
        {
            AddReportWarning(
                result.Warnings[i]
            );
        }


        if (result.Success &&
            layout.IsSolved)
        {
            AddReportSuccess(
                "Spatial layout solved successfully."
            );


            AddReportInfo(
                "Open the Build tab for complete V1 validation."
            );
        }
    }


    // =========================================================
    // Report
    // =========================================================

    private void AddReportTitle(
        string text
    )
    {
        Label label =
            new Label(
                text
            );


        label.AddToClassList(
            "level-layout__report-title"
        );


        report.Add(
            label
        );
    }


    private void AddReportInfo(
        string text
    )
    {
        Label label =
            new Label(
                text
            );


        label.style.whiteSpace =
            WhiteSpace.Normal;


        report.Add(
            label
        );
    }


    private void AddReportSuccess(
        string text
    )
    {
        Label label =
            new Label(
                "✓ " +
                text
            );


        label.style.whiteSpace =
            WhiteSpace.Normal;


        report.Add(
            label
        );
    }


    private void AddReportWarning(
        string text
    )
    {
        Label label =
            new Label(
                "⚠ " +
                text
            );


        label.style.whiteSpace =
            WhiteSpace.Normal;


        report.Add(
            label
        );
    }


    private void AddReportError(
        string text
    )
    {
        Label label =
            new Label(
                "✕ " +
                text
            );


        label.style.whiteSpace =
            WhiteSpace.Normal;


        report.Add(
            label
        );
    }
}
