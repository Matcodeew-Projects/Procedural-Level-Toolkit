using UnityEditor;
using UnityEngine;

public sealed class LevelPreviewController
{
    // =========================================================
    // Camera Settings
    // =========================================================

    private const float OrbitSensitivity =
        0.6f;

    private const float ZoomSensitivity =
        0.08f;

    private const float PanSensitivity =
        0.0025f;

    private const float MinDistance =
        0.05f;

    private const float MaxDistance =
        100000f;

    private const float MinPitch =
        5f;

    private const float MaxPitch =
        89f;


    // =========================================================
    // Preview
    // =========================================================

    private PreviewRenderUtility previewUtility;

    private GameObject levelRoot;


    // =========================================================
    // Camera State
    // =========================================================

    private Vector3 target =
        Vector3.zero;

    private float yaw =
        45f;

    private float pitch =
        45f;

    private float distance =
        20f;


    // =========================================================
    // Properties
    // =========================================================

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


    // =========================================================
    // Constructor
    // =========================================================

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


        Camera camera =
            previewUtility.camera;


        camera.clearFlags =
            CameraClearFlags.SolidColor;


        camera.backgroundColor =
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


        camera.nearClipPlane =
            0.01f;


        camera.farClipPlane =
            100000f;


        camera.fieldOfView =
            50f;


        camera.useOcclusionCulling =
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


        ConfigureLights();

        UpdateCameraTransform();
    }


    private void ConfigureLights()
    {
        Light[] lights =
            previewUtility.lights;


        if (lights == null)
        {
            return;
        }


        if (lights.Length > 0)
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


        if (lights.Length > 1)
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


        if (previewUtility == null)
        {
            return;
        }


        previewUtility.Cleanup();


        previewUtility =
            null;
    }


    // =========================================================
    // Build Preview
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
    // Frame
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


    // =========================================================
    // Bounds
    // =========================================================

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
    // Orbit
    // =========================================================

    public void Orbit(
        Vector2 delta
    )
    {
        yaw +=
            delta.x *
            OrbitSensitivity;


        /*
         * Vertical movement intentionally inverted.
         *
         * Mouse up   -> camera goes down
         * Mouse down -> camera goes up
         */

        pitch -=
            delta.y *
            OrbitSensitivity;


        pitch =
            Mathf.Clamp(
                pitch,
                MinPitch,
                MaxPitch
            );


        UpdateCameraTransform();
    }


    // =========================================================
    // Pan
    // =========================================================

    public void Pan(
        Vector2 delta
    )
    {
        if (previewUtility == null)
        {
            return;
        }


        Camera camera =
            previewUtility.camera;


        float scale =
            Mathf.Max(
                0.0001f,
                distance *
                PanSensitivity
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


    // =========================================================
    // Zoom
    // =========================================================

    public void Zoom(
        float wheelDelta
    )
    {
        distance *=
            Mathf.Exp(
                wheelDelta *
                ZoomSensitivity
            );


        distance =
            Mathf.Clamp(
                distance,
                MinDistance,
                MaxDistance
            );


        UpdateCameraTransform();
    }


    // =========================================================
    // Camera
    // =========================================================

    private void UpdateCameraTransform()
    {
        if (previewUtility == null)
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
    // Draw
    // =========================================================

    public void Draw(
        Rect rect
    )
    {
        if (previewUtility == null)
        {
            return;
        }


        if (rect.width <= 1f ||
            rect.height <= 1f)
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


        if (texture == null)
        {
            return;
        }


        GUI.DrawTexture(
            rect,
            texture,
            ScaleMode.StretchToFill,
            false
        );
    }


    // =========================================================
    // Dispose
    // =========================================================

    public void Dispose()
    {
        CleanupPreviewUtility();
    }
}