using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class LevelBuildUI
    : VisualElement
{
    private readonly LevelEditorContext context;


    private readonly Label statusTitle;

    private readonly Label statusSubtitle;

    private readonly Label errorCountLabel;

    private readonly Label warningCountLabel;


    private ObjectField outputField;

    private Label outputPathLabel;

    private Label lastBuildLabel;


    private readonly Button validateButton;

    private readonly Button buildButton;

    private readonly Button buildAsButton;

    private readonly Button pingButton;


    private ScrollView issuesList;


    private LevelValidationResult validation;


    public LevelBuildUI(
        LevelEditorContext context
    )
    {
        this.context =
            context;


        AddToClassList(
            "level-build"
        );


        // =====================================================
        // Header
        // =====================================================

        VisualElement header =
            new VisualElement();


        header.AddToClassList(
            "level-build__header"
        );


        VisualElement headerText =
            new VisualElement();


        headerText.style.flexGrow =
            1;


        statusTitle =
            new Label(
                "BUILD & VALIDATION"
            );


        statusTitle.AddToClassList(
            "level-build__title"
        );


        statusSubtitle =
            new Label(
                "Select a LevelDefinition."
            );


        statusSubtitle.AddToClassList(
            "level-build__subtitle"
        );


        headerText.Add(
            statusTitle
        );


        headerText.Add(
            statusSubtitle
        );


        VisualElement counters =
            new VisualElement();


        counters.AddToClassList(
            "level-build__counters"
        );


        errorCountLabel =
            CreateCounter(
                "Errors"
            );


        warningCountLabel =
            CreateCounter(
                "Warnings"
            );


        counters.Add(
            errorCountLabel
        );


        counters.Add(
            warningCountLabel
        );


        header.Add(
            headerText
        );


        header.Add(
            counters
        );


        Add(
            header
        );


        // =====================================================
        // Actions
        // =====================================================

        VisualElement actions =
            new VisualElement();


        actions.AddToClassList(
            "level-build__actions"
        );


        validateButton =
            new Button(
                RunValidation
            )
            {
                text =
                    "Validate Level"
            };


        buildButton =
            new Button(
                BuildOrRebuild
            )
            {
                text =
                    "Build Level Prefab"
            };


        buildButton.AddToClassList(
            "level-build__primary-button"
        );


        buildAsButton =
            new Button(
                BuildAs
            )
            {
                text =
                    "Build As..."
            };


        pingButton =
            new Button(
                PingOutput
            )
            {
                text =
                    "Ping Output"
            };


        actions.Add(
            validateButton
        );


        actions.Add(
            buildButton
        );


        actions.Add(
            buildAsButton
        );


        actions.Add(
            pingButton
        );


        Add(
            actions
        );


        // =====================================================
        // Body
        // =====================================================

        TwoPaneSplitView bodySplit =
            new TwoPaneSplitView(
                0,
                360f,
                TwoPaneSplitViewOrientation
                    .Horizontal
            );


        bodySplit.AddToClassList(
            "level-build__body"
        );


        VisualElement outputPanel =
            BuildOutputPanel();


        VisualElement validationPanel =
            BuildValidationPanel();


        bodySplit.Add(
            outputPanel
        );


        bodySplit.Add(
            validationPanel
        );


        Add(
            bodySplit
        );


        // =====================================================
        // Events
        // =====================================================

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


        Refresh();
    }


    // =========================================================
    // Panels
    // =========================================================

    private VisualElement BuildOutputPanel()
    {
        VisualElement panel =
            new VisualElement();


        panel.AddToClassList(
            "level-build-card"
        );


        AddCardTitle(
            panel,
            "BUILD OUTPUT",
            "The generated prefab associated with this LevelDefinition."
        );


        outputField =
            new ObjectField(
                "Generated Prefab"
            )
            {
                objectType =
                    typeof(
                        GameObject
                    ),

                allowSceneObjects =
                    false
            };


        outputField.SetEnabled(
            false
        );


        outputPathLabel =
            new Label();


        outputPathLabel.AddToClassList(
            "level-build-card__detail"
        );


        outputPathLabel.style.whiteSpace =
            WhiteSpace.Normal;


        lastBuildLabel =
            new Label();


        lastBuildLabel.AddToClassList(
            "level-build-card__detail"
        );


        lastBuildLabel.style.whiteSpace =
            WhiteSpace.Normal;


        Label behavior =
            new Label(
                "Build validates the complete level first. If an output already exists, Build overwrites that prefab. Build As... creates or selects another prefab output."
            );


        behavior.AddToClassList(
            "level-build-card__hint"
        );


        behavior.style.whiteSpace =
            WhiteSpace.Normal;


        panel.Add(
            outputField
        );


        panel.Add(
            outputPathLabel
        );


        panel.Add(
            lastBuildLabel
        );


        panel.Add(
            behavior
        );


        return panel;
    }


    private VisualElement BuildValidationPanel()
    {
        VisualElement panel =
            new VisualElement();


        panel.AddToClassList(
            "level-build-card"
        );


        AddCardTitle(
            panel,
            "VALIDATION",
            "Errors block the build. Warnings do not."
        );


        issuesList =
            new ScrollView();


        issuesList.AddToClassList(
            "level-build__issues"
        );


        panel.Add(
            issuesList
        );


        return panel;
    }


    private static void AddCardTitle(
        VisualElement parent,
        string title,
        string subtitle
    )
    {
        Label titleLabel =
            new Label(
                title
            );


        titleLabel.AddToClassList(
            "level-build-card__title"
        );


        Label subtitleLabel =
            new Label(
                subtitle
            );


        subtitleLabel.AddToClassList(
            "level-build-card__subtitle"
        );


        subtitleLabel.style.whiteSpace =
            WhiteSpace.Normal;


        parent.Add(
            titleLabel
        );


        parent.Add(
            subtitleLabel
        );
    }


    private static Label CreateCounter(
        string label
    )
    {
        Label counter =
            new Label(
                $"{label}: 0"
            );


        counter.AddToClassList(
            "level-build__counter"
        );


        return counter;
    }


    // =========================================================
    // Context Events
    // =========================================================

    private void OnAttach(
        AttachToPanelEvent evt
    )
    {
        context.OnLevelChanged +=
            Refresh;

        context.OnGraphChanged +=
            Refresh;

        context.OnLayoutChanged +=
            Refresh;
    }


    private void OnDetach(
        DetachFromPanelEvent evt
    )
    {
        context.OnLevelChanged -=
            Refresh;

        context.OnGraphChanged -=
            Refresh;

        context.OnLayoutChanged -=
            Refresh;
    }


    // =========================================================
    // Validation
    // =========================================================

    private void RunValidation()
    {
        validation =
            LevelValidator.Validate(
                context.CurrentLevel
            );


        RefreshUI();
    }


    private void Refresh()
    {
        validation =
            LevelValidator.Validate(
                context.CurrentLevel
            );


        RefreshUI();
    }


    private void RefreshUI()
    {
        LevelDefinition level =
            context.CurrentLevel;


        bool hasLevel =
            level != null;


        validateButton.SetEnabled(
            hasLevel
        );


        buildAsButton.SetEnabled(
            hasLevel &&
            validation != null &&
            validation.IsValid
        );


        pingButton.SetEnabled(
            hasLevel &&
            level.GeneratedPrefab != null
        );


        buildButton.SetEnabled(
            hasLevel &&
            validation != null &&
            validation.IsValid
        );


        if (!hasLevel)
        {
            statusTitle.text =
                "NO LEVEL SELECTED";


            statusSubtitle.text =
                "Select or create a LevelDefinition before validating or building.";


            errorCountLabel.text =
                "Errors: —";


            warningCountLabel.text =
                "Warnings: —";


            outputField.SetValueWithoutNotify(
                null
            );


            outputPathLabel.text =
                "Output: —";


            lastBuildLabel.text =
                "Last build: —";


            issuesList.Clear();


            return;
        }


        errorCountLabel.text =
            $"Errors: {validation.ErrorCount}";


        warningCountLabel.text =
            $"Warnings: {validation.WarningCount}";


        if (validation.IsValid)
        {
            statusTitle.text =
                "READY TO BUILD";


            statusSubtitle.text =
                validation.WarningCount >
                0
                    ? $"Validation passed with {validation.WarningCount} warning(s)."
                    : "Validation passed. The level is ready to build.";
        }
        else
        {
            statusTitle.text =
                "BUILD BLOCKED";


            statusSubtitle.text =
                $"Fix {validation.ErrorCount} validation error(s) before building.";
        }


        buildButton.text =
            level.GeneratedPrefab != null
                ? "Rebuild Level Prefab"
                : "Build Level Prefab";


        outputField.SetValueWithoutNotify(
            level.GeneratedPrefab
        );


        string outputPath =
            level.GeneratedPrefab != null
                ? AssetDatabase.GetAssetPath(
                    level.GeneratedPrefab
                )
                : string.Empty;


        outputPathLabel.text =
            string.IsNullOrWhiteSpace(
                outputPath
            )
                ? "Output: not generated yet"
                : $"Output: {outputPath}";


        lastBuildLabel.text =
            FormatBuildDate(
                level.LastBuildUtc
            );


        RefreshIssues();
    }


    private void RefreshIssues()
    {
        issuesList.Clear();


        if (validation == null)
        {
            return;
        }


        if (validation.Issues.Count ==
            0)
        {
            Label valid =
                new Label(
                    "✓ No validation issues."
                );


            valid.AddToClassList(
                "level-build-issue--success"
            );


            issuesList.Add(
                valid
            );


            return;
        }


        /*
         * Errors first.
         */
        for (int pass = 0;
             pass < 2;
             pass++)
        {
            LevelValidationSeverity severity =
                pass ==
                0
                    ? LevelValidationSeverity.Error
                    : LevelValidationSeverity.Warning;


            for (int i = 0;
                 i < validation.Issues.Count;
                 i++)
            {
                LevelValidationIssue issue =
                    validation.Issues[i];


                if (issue.Severity !=
                    severity)
                {
                    continue;
                }


                VisualElement row =
                    new VisualElement();


                row.AddToClassList(
                    "level-build-issue"
                );


                row.AddToClassList(
                    severity ==
                    LevelValidationSeverity.Error
                        ? "level-build-issue--error"
                        : "level-build-issue--warning"
                );


                Label symbol =
                    new Label(
                        severity ==
                        LevelValidationSeverity.Error
                            ? "✕"
                            : "⚠"
                    );


                symbol.AddToClassList(
                    "level-build-issue__symbol"
                );


                VisualElement text =
                    new VisualElement();


                text.style.flexGrow =
                    1;


                Label message =
                    new Label(
                        issue.Message
                    );


                message.AddToClassList(
                    "level-build-issue__message"
                );


                message.style.whiteSpace =
                    WhiteSpace.Normal;


                Label code =
                    new Label(
                        issue.Code
                    );


                code.AddToClassList(
                    "level-build-issue__code"
                );


                text.Add(
                    message
                );


                text.Add(
                    code
                );


                row.Add(
                    symbol
                );


                row.Add(
                    text
                );


                issuesList.Add(
                    row
                );
            }
        }
    }


    // =========================================================
    // Build
    // =========================================================

    private void BuildOrRebuild()
    {
        RunValidation();


        if (validation == null ||
            !validation.IsValid)
        {
            return;
        }


        GameObject output =
            LevelPrefabBuilder
                .BuildOrRebuild(
                    context.CurrentLevel
                );


        if (output != null)
        {
            Refresh();
        }
    }


    private void BuildAs()
    {
        RunValidation();


        if (validation == null ||
            !validation.IsValid)
        {
            return;
        }


        GameObject output =
            LevelPrefabBuilder
                .BuildWithSaveDialog(
                    context.CurrentLevel
                );


        if (output != null)
        {
            Refresh();
        }
    }


    private void PingOutput()
    {
        GameObject prefab =
            context.CurrentLevel?
                .GeneratedPrefab;


        if (prefab == null)
        {
            return;
        }


        Selection.activeObject =
            prefab;


        EditorGUIUtility.PingObject(
            prefab
        );
    }


    // =========================================================
    // Helpers
    // =========================================================

    private static string FormatBuildDate(
        string buildUtc
    )
    {
        if (string.IsNullOrWhiteSpace(
                buildUtc
            ))
        {
            return "Last build: never";
        }


        if (!DateTime.TryParse(
                buildUtc,
                null,
                System.Globalization
                    .DateTimeStyles
                    .RoundtripKind,
                out DateTime parsed
            ))
        {
            return
                $"Last build: {buildUtc}";
        }


        return
            $"Last build: " +
            $"{parsed.ToLocalTime():yyyy-MM-dd HH:mm:ss}";
    }
}
