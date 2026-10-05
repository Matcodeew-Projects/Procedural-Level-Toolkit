using UnityEditor;
using UnityEngine;

public sealed class LevelPreviewController
{
    private PreviewRenderUtility previewUtility;

    private GameObject levelRoot;


    private Vector3 target =
        Vector3.zero;

    private float yaw =
        45f;

    private float pitch =
        55f;

    private float distance =
        20f;


    public string LastError
    {
        get;
        private set;
    }


    public int InstanceCount
    {
        get;
        private set;
    }


    public bool HasPreview =>
        previewUtility != null &&
        levelRoot != null;


    public LevelPreviewController()
    {
        CreatePreviewUtility();
    }


    // =========================================================
    // Preview Utility
    // =========================================================

    private void CreatePreviewUtility()
    {
        previewUtility =
            new PreviewRenderUtility();


        previewUtility.camera.clearFlags =
            CameraClearFlags.SolidColor;


        previewUtility.camera.backgroundColor =
            EditorGUIUtility.isProSkin
                ? new Color(
                    0.095f,
                    0.105f,
                    0.12f,
                    1f
                )
                : new Color(
                    0.72f,
                    0.74f,
                    0.77f,
                    1f
                );


        previewUtility.camera.nearClipPlane =
            0.01f;


        previewUtility.camera.farClipPlane =
            100000f;


        previewUtility.camera.fieldOfView =
            50f;


        previewUtility.camera.useOcclusionCulling =
            false;


        previewUtility.ambientColor =
            EditorGUIUtility.isProSkin
                ? new Color(
                    0.42f,
                    0.42f,
                    0.45f,
                    1f
                )
                : new Color(
                    0.55f,
                    0.55f,
                    0.58f,
                    1f
                );


        Light[] lights =
            previewUtility.lights;


        if (lights != null &&
            lights.Length >
            0)
        {
            lights[0].intensity =
                1.25f;


            lights[0]
                .transform
                .rotation =
                Quaternion.Euler(
                    50f,
                    -35f,
                    0f
                );


            lights[0].shadows =
                LightShadows.None;
        }


        if (lights != null &&
            lights.Length >
            1)
        {
            lights[1].intensity =
                0.55f;


            lights[1]
                .transform
                .rotation =
                Quaternion.Euler(
                    55f,
                    145f,
                    0f
                );


            lights[1].shadows =
                LightShadows.None;
        }


        UpdateCameraTransform();
    }


    private void ResetPreviewUtility()
    {
        CleanupPreviewUtility();

        CreatePreviewUtility();
    }


    private void CleanupPreviewUtility()
    {
        levelRoot =
            null;


        if (previewUtility != null)
        {
            previewUtility.Cleanup();

            previewUtility =
                null;
        }
    }


    // =========================================================
    // Build Preview Level
    // =========================================================

    public void Rebuild(
        LevelDefinition level
    )
    {
        LastError =
            string.Empty;


        InstanceCount =
            0;


        ResetPreviewUtility();


        if (level == null)
        {
            LastError =
                "No LevelDefinition selected.";


            return;
        }


        LevelBuildResult result =
            LevelBuilder.Build(
                level,
                null,
                CreateRoot,
                InstantiatePrefab
            );


        levelRoot =
            result.Root;


        InstanceCount =
            result.Instances.Count;


        if (!result.Success)
        {
            LastError =
                string.Join(
                    "\n",
                    result.Errors
                );


            return;
        }


        Frame();


        if (!TryGetRenderableBounds(
                out _
            ))
        {
            LastError =
                "The preview instantiated the level, but no enabled Renderer was found.";
        }
    }


    private GameObject CreateRoot(
        string objectName
    )
    {
        GameObject root =
            new GameObject(
                objectName
            );


        previewUtility.AddSingleGO(
            root
        );


        return root;
    }


    private GameObject InstantiatePrefab(
        GameObject prefab,
        Transform parent
    )
    {
        if (prefab == null)
        {
            return null;
        }


        GameObject instance =
            previewUtility
                .InstantiatePrefabInScene(
                    prefab
                );


        if (instance == null)
        {
            return null;
        }


        instance.transform.SetParent(
            parent,
            false
        );


        return instance;
    }


    // =========================================================
    // Framing
    // =========================================================

    public void Frame()
    {
        if (levelRoot == null)
        {
            target =
                Vector3.zero;


            distance =
                20f;


            UpdateCameraTransform();


            return;
        }


        if (!TryGetRenderableBounds(
                out Bounds bounds
            ))
        {
            target =
                levelRoot.transform.position;


            distance =
                20f;


            UpdateCameraTransform();


            return;
        }


        target =
            bounds.center;


        float radius =
            Mathf.Max(
                1f,
                bounds.extents.magnitude
            );


        distance =
            Mathf.Max(
                2f,
                radius *
                2.65f
            );


        UpdateCameraTransform();
    }


    private bool TryGetRenderableBounds(
        out Bounds bounds
    )
    {
        bounds =
            new Bounds();


        if (levelRoot == null)
        {
            return false;
        }


        Renderer[] renderers =
            levelRoot
                .GetComponentsInChildren<
                    Renderer
                >(
                    true
                );


        bool found =
            false;


        for (int i = 0;
             i < renderers.Length;
             i++)
        {
            Renderer renderer =
                renderers[i];


            if (renderer == null ||
                !renderer.enabled ||
                !renderer.gameObject.activeInHierarchy)
            {
                continue;
            }


            Bounds current =
                renderer.bounds;


            if (!IsFinite(
                    current.center
                ) ||
                !IsFinite(
                    current.size
                ))
            {
                continue;
            }


            if (current.size.sqrMagnitude >
                100000000f)
            {
                continue;
            }


            if (!found)
            {
                bounds =
                    current;


                found =
                    true;
            }
            else
            {
                bounds.Encapsulate(
                    current
                );
            }
        }


        return found;
    }


    private static bool IsFinite(
        Vector3 value
    )
    {
        return
            IsFinite(
                value.x
            )
            &&
            IsFinite(
                value.y
            )
            &&
            IsFinite(
                value.z
            );
    }


    private static bool IsFinite(
        float value
    )
    {
        return
            !float.IsNaN(
                value
            )
            &&
            !float.IsInfinity(
                value
            );
    }


    // =========================================================
    // Camera Controls
    // =========================================================

    public void Orbit(
        Vector2 delta
    )
    {
        yaw +=
            delta.x *
            0.35f;


        pitch -=
            delta.y *
            0.35f;


        pitch =
            Mathf.Clamp(
                pitch,
                8f,
                89f
            );


        UpdateCameraTransform();
    }


    public void Pan(
        Vector2 delta
    )
    {
        if (previewUtility ==
            null)
        {
            return;
        }


        Camera camera =
            previewUtility.camera;


        float scale =
            Mathf.Max(
                0.001f,
                distance *
                0.0015f
            );


        target -=
            camera.transform.right *
            delta.x *
            scale;


        target +=
            camera.transform.up *
            delta.y *
            scale;


        UpdateCameraTransform();
    }


    public void Zoom(
        float wheelDelta
    )
    {
        distance *=
            Mathf.Exp(
                wheelDelta *
                0.08f
            );


        distance =
            Mathf.Clamp(
                distance,
                0.05f,
                100000f
            );


        UpdateCameraTransform();
    }


    private void UpdateCameraTransform()
    {
        if (previewUtility ==
            null)
        {
            return;
        }


        Camera camera =
            previewUtility.camera;


        Quaternion rotation =
            Quaternion.Euler(
                pitch,
                yaw,
                0f
            );


        Vector3 backwards =
            rotation *
            Vector3.back;


        camera.transform.position =
            target +
            backwards *
            distance;


        camera.transform.rotation =
            rotation;
    }


    // =========================================================
    // Render
    // =========================================================

    public void Draw(
        Rect rect
    )
    {
        if (previewUtility ==
            null)
        {
            return;
        }


        if (rect.width <=
                1f ||
            rect.height <=
                1f)
        {
            return;
        }


        previewUtility.BeginPreview(
            rect,
            GUIStyle.none
        );


        Camera camera =
            previewUtility.camera;


        camera.aspect =
            rect.width /
            rect.height;


        camera.Render();


        Texture texture =
            previewUtility.EndPreview();


        if (texture != null)
        {
            GUI.DrawTexture(
                rect,
                texture,
                ScaleMode.StretchToFill,
                false
            );
        }
    }


    // =========================================================
    // Dispose
    // =========================================================

    public void Dispose()
    {
        CleanupPreviewUtility();
    }
}