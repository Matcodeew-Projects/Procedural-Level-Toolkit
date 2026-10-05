using UnityEditor;
using UnityEngine.UIElements;

public sealed class ToolkitShellUI
    : VisualElement
{
    private readonly VisualElement roomEditorPage;

    private readonly VisualElement levelEditorPage;


    private readonly Button roomButton;

    private readonly Button levelButton;


    public ToolkitShellUI(
        VisualElement existingRoomEditorRoot,
        VisualElement levelEditorRoot
    )
    {
        style.flexGrow =
            1;


        AddToClassList(
            "toolkit-shell"
        );


        StyleSheet stylesheet =
            AssetDatabase
                .LoadAssetAtPath<
                    StyleSheet
                >(
                    LevelEditorUIPaths
                        .ShellStyle
                );


        if (stylesheet != null)
        {
            styleSheets.Add(
                stylesheet
            );
        }


        // =====================================================
        // Navigation
        // =====================================================

        VisualElement navigation =
            new VisualElement();

        navigation.AddToClassList(
            "toolkit-shell__navigation"
        );


        roomButton =
            new Button(
                ShowRoomEditor
            )
            {
                text =
                    "Room Editor"
            };


        levelButton =
            new Button(
                ShowLevelEditor
            )
            {
                text =
                    "Level Editor"
            };


        roomButton.AddToClassList(
            "toolkit-shell__tab"
        );


        levelButton.AddToClassList(
            "toolkit-shell__tab"
        );


        navigation.Add(
            roomButton
        );


        navigation.Add(
            levelButton
        );


        Add(
            navigation
        );


        // =====================================================
        // Content
        // =====================================================

        VisualElement content =
            new VisualElement();

        content.AddToClassList(
            "toolkit-shell__content"
        );


        Add(
            content
        );


        roomEditorPage =
            new VisualElement();

        roomEditorPage.style.flexGrow =
            1;


        levelEditorPage =
            new VisualElement();

        levelEditorPage.style.flexGrow =
            1;


        existingRoomEditorRoot?
            .RemoveFromHierarchy();


        if (existingRoomEditorRoot != null)
        {
            roomEditorPage.Add(
                existingRoomEditorRoot
            );
        }


        if (levelEditorRoot != null)
        {
            levelEditorPage.Add(
                levelEditorRoot
            );
        }


        content.Add(
            roomEditorPage
        );


        content.Add(
            levelEditorPage
        );


        ShowRoomEditor();
    }


    // =========================================================
    // Navigation
    // =========================================================

    private void ShowRoomEditor()
    {
        roomEditorPage.style.display =
            DisplayStyle.Flex;


        levelEditorPage.style.display =
            DisplayStyle.None;


        roomButton.EnableInClassList(
            "toolkit-shell__tab--active",
            true
        );


        levelButton.EnableInClassList(
            "toolkit-shell__tab--active",
            false
        );
    }


    private void ShowLevelEditor()
    {
        roomEditorPage.style.display =
            DisplayStyle.None;


        levelEditorPage.style.display =
            DisplayStyle.Flex;


        roomButton.EnableInClassList(
            "toolkit-shell__tab--active",
            false
        );


        levelButton.EnableInClassList(
            "toolkit-shell__tab--active",
            true
        );
    }
}
