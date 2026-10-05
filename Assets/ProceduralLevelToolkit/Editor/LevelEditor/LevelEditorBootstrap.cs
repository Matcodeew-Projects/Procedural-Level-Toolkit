using UnityEngine;
using UnityEngine.UIElements;

public static class LevelEditorBootstrap
{
    public static void Attach(
        VisualElement windowRoot,
        VisualElement roomEditorRoot
    )
    {
        // =====================================================
        // Validation
        // =====================================================

        if (windowRoot == null)
        {
            Debug.LogError(
                "LevelEditorBootstrap: windowRoot is null."
            );

            return;
        }


        if (roomEditorRoot == null)
        {
            Debug.LogError(
                "LevelEditorBootstrap: roomEditorRoot is null."
            );

            return;
        }


        // =====================================================
        // Prevent hierarchy cycle
        // =====================================================
        //
        // Never allow:
        //
        // Attach(
        //     rootVisualElement,
        //     rootVisualElement
        // );
        //
        // This would create a cyclic UI hierarchy.
        // =====================================================

        if (ReferenceEquals(
                windowRoot,
                roomEditorRoot
            ))
        {
            Debug.LogError(
                "LevelEditorBootstrap: " +
                "windowRoot and roomEditorRoot cannot be the same VisualElement."
            );

            return;
        }


        // =====================================================
        // Level Editor
        // =====================================================

        LevelEditorView levelEditorView =
            new LevelEditorView();


        // =====================================================
        // Shell
        // =====================================================

        ToolkitShellUI shell =
            new ToolkitShellUI(
                roomEditorRoot,
                levelEditorView
            );


        // =====================================================
        // Install
        // =====================================================

        windowRoot.Clear();


        windowRoot.Add(
            shell
        );
    }
}