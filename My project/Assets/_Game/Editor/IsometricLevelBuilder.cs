using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Builds the whole isometric demo level as real scene GameObjects.
// Run via Tools > Isometric Demo > Build Level. Re-running replaces the
// previously built root so it's safe to run multiple times while tuning.
public static class IsometricLevelBuilder
{
    const string RootName = "Isometric Demo Level";

    // Layout constants (world units). See the accompanying summary for the
    // axis convention: +X/-X = screen right/left, +Z/-Z = screen up/down
    // (depth), +Y = world height (jump).
    const float GroundHalfExtent = 16f;
    const float GroundThickness = 0.2f;

    const float PitHalfX = 1.5f;
    const float PitHalfZ = 1.5f;
    static readonly Vector3 PitCenter = new Vector3(0f, 0f, -8f);
    const float PitDepth = 1.2f;
    const float PitWallThickness = 0.2f;

    const float BoxSize = 1f;
    static readonly Vector3 BoxCenter = new Vector3(4f, BoxSize * 0.5f, 0f);

    static readonly Vector3 FenceCenter = new Vector3(7.5f, 0.7f, 0f);
    static readonly Vector3 FenceSize = new Vector3(0.2f, 1.4f, 4f);

    const float PlayerHeight = 2f;
    const float PlayerRadius = 0.4f;

    // Riser must stay under the player's stepHeight so each step is climbable.
    // Tread can be narrower than the cylinder - proper swept step-up handles it.
    const int StairSteps = 8;
    const float StairRiser = 0.3f;
    const float StairTread = 0.6f;
    const float StairWidth = 2.4f;
    const float StairStartX = -4f;
    const float StairCenterZ = 3f;

    [MenuItem("Tools/Isometric Demo/Build Level")]
    public static void BuildLevel()
    {
        GameObject existingRoot = GameObject.Find(RootName);
        if (existingRoot != null)
        {
            if (!EditorUtility.DisplayDialog("Rebuild Isometric Demo Level",
                "An \"" + RootName + "\" already exists in the scene. Replace it?",
                "Replace", "Cancel"))
            {
                return;
            }
            Undo.DestroyObjectImmediate(existingRoot);
        }

        // Stylized face-shading shader (top faces lighter, sides darker).
        Shader shader = Shader.Find("Custom/IsoFaceShade");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        // Always-transparent flat shader for the projected ground shadows.
        Shader shadowShader = Shader.Find("Custom/BlobShadow");
        if (shadowShader == null) shadowShader = Shader.Find("Universal Render Pipeline/Unlit");

        GameObject root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Build Isometric Demo Level");

        Light globalLight = SetupGlobalLight();

        BuildGround(root.transform, shader);
        GameObject player = BuildPlayer(root.transform, shader);
        GameObject redBox = BuildRedBox(root.transform, shader);
        GameObject fence = BuildFence(root.transform, shader);
        GameObject staircase = BuildStaircase(root.transform, shader, player.transform);
        GameObject pitRoot = BuildPit(root.transform, shader);
        SetupCamera(player.transform);

        // Sharp box-projected drop shadows, all sharing the one global light angle.
        // The player uses a rounded (cylindrical) shadow and drops it onto whatever
        // surface is below it (so it lands on the pit floor inside the pit).
        AddShadow(player, globalLight, shadowShader, true, true, true);
        AddShadow(redBox, globalLight, shadowShader, false, false, false);
        AddShadow(fence, globalLight, shadowShader, false, false, false);
        foreach (Transform step in staircase.transform)
            AddShadow(step.gameObject, globalLight, shadowShader, false, false, false);

        // Pit walls cast onto the pit floor (top of the floor slab = -PitDepth), so
        // the sun-side rim correctly shades the inside of the pit. Portions of these
        // shadows that fall outside the pit are hidden under the ground slabs.
        foreach (Transform wall in pitRoot.transform)
            if (wall.name.StartsWith("PitWall"))
                AddShadow(wall.gameObject, globalLight, shadowShader, false, false, false, -PitDepth);

        Selection.activeGameObject = root;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    static GameObject CreateBox(Transform parent, string name, Vector3 center, Vector3 size, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        Undo.RegisterCreatedObjectUndo(go, "Build Isometric Demo Level");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = center;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    static Material Flat(Shader shader, Color color)
    {
        return URPTransparencyUtil.CreateOpaque(shader, color);
    }

    static GameObject BuildGround(Transform parent, Shader shader)
    {
        GameObject ground = new GameObject("Ground");
        Undo.RegisterCreatedObjectUndo(ground, "Build Isometric Demo Level");
        ground.transform.SetParent(parent, false);

        Material white = Flat(shader, Color.white);
        float y = -GroundThickness * 0.5f;

        float pitXMin = PitCenter.x - PitHalfX;
        float pitXMax = PitCenter.x + PitHalfX;
        float pitZMin = PitCenter.z - PitHalfZ;
        float pitZMax = PitCenter.z + PitHalfZ;
        float g = GroundHalfExtent;

        // Four slabs tiling the ground area around the pit rectangle (exact, no gaps/overlaps).
        CreateBox(ground.transform, "Ground_West",
            new Vector3((-g + pitXMin) * 0.5f, y, PitCenter.z),
            new Vector3(pitXMin - (-g), GroundThickness, pitZMax - pitZMin), white);

        CreateBox(ground.transform, "Ground_East",
            new Vector3((pitXMax + g) * 0.5f, y, PitCenter.z),
            new Vector3(g - pitXMax, GroundThickness, pitZMax - pitZMin), white);

        CreateBox(ground.transform, "Ground_Far",
            new Vector3(0f, y, (-g + pitZMin) * 0.5f),
            new Vector3(2f * g, GroundThickness, pitZMin - (-g)), white);

        CreateBox(ground.transform, "Ground_Near",
            new Vector3(0f, y, (pitZMax + g) * 0.5f),
            new Vector3(2f * g, GroundThickness, g - pitZMax), white);

        return ground;
    }

    static GameObject BuildPlayer(Transform parent, Shader shader)
    {
        GameObject player = new GameObject("Player");
        Undo.RegisterCreatedObjectUndo(player, "Build Isometric Demo Level");
        player.transform.SetParent(parent, false);
        // Spawn a hair above the ground plane so the first physics frame starts
        // clear of the floor instead of intersecting it.
        player.transform.localPosition = new Vector3(0f, 0.02f, 0f);
        // Ignore Raycast layer: the controller's own downward casts / overlap tests
        // use Physics.DefaultRaycastLayers, which excludes this layer, so the player
        // never collides with itself.
        player.layer = 2;

        // True cylinder hitbox: kinematic Rigidbody + convex cylinder MeshCollider.
        // The cylinder mesh is generated at runtime by PlayerController (Awake).
        Rigidbody rb = player.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        MeshCollider mc = player.AddComponent<MeshCollider>();
        mc.convex = true;

        PlayerController pc = player.AddComponent<PlayerController>();
        pc.radius = PlayerRadius;
        pc.standingHeight = PlayerHeight;
        pc.stepHeight = StairRiser + 0.15f; // must exceed the stair riser so steps are climbable

        // Blue "vertical rectangle" visual - a flat card that always faces the camera,
        // standing in for the eventual 2D character sprite.
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = "Visual";
        Undo.RegisterCreatedObjectUndo(visual, "Build Isometric Demo Level");
        Object.DestroyImmediate(visual.GetComponent<Collider>());
        visual.transform.SetParent(player.transform, false);
        visual.transform.localPosition = new Vector3(0f, PlayerHeight * 0.5f, 0f);
        visual.transform.localScale = new Vector3(0.6f, PlayerHeight, 0.1f);
        visual.GetComponent<Renderer>().sharedMaterial = Flat(shader, Color.blue);
        visual.AddComponent<BillboardToCamera>();

        pc.visual = visual.transform; // resized when crouching

        return player;
    }

    static GameObject BuildRedBox(Transform parent, Shader shader)
    {
        GameObject box = CreateBox(parent, "RedBox", BoxCenter, new Vector3(BoxSize, BoxSize, BoxSize), Flat(shader, Color.red));
        return box;
    }

    static GameObject BuildFence(Transform parent, Shader shader)
    {
        // A thin cube: its BoxCollider is scaled down along with the mesh, so the
        // hitbox is naturally as narrow as the fence itself (no oversized default collider).
        GameObject fence = CreateBox(parent, "GreenFence", FenceCenter, FenceSize, Flat(shader, Color.green));
        return fence;
    }

    static GameObject BuildStaircase(Transform parent, Shader shader, Transform player)
    {
        GameObject staircase = new GameObject("Staircase");
        Undo.RegisterCreatedObjectUndo(staircase, "Build Isometric Demo Level");
        staircase.transform.SetParent(parent, false);

        Material grey = Flat(shader, Color.gray);

        for (int k = 0; k < StairSteps; k++)
        {
            float stepHeight = StairRiser * (k + 1);
            float centerX = StairStartX - k * StairTread - StairTread * 0.5f;
            GameObject step = CreateBox(staircase.transform, "Step_" + k,
                new Vector3(centerX, stepHeight * 0.5f, StairCenterZ),
                new Vector3(StairTread, stepHeight, StairWidth), grey);
            step.isStatic = false; // must stay non-static; its material is swapped at runtime
        }

        StaircaseOcclusionFade fade = staircase.AddComponent<StaircaseOcclusionFade>();
        fade.player = player;
        fade.shader = shader;
        fade.baseColor = Color.gray;
        fade.transparentAlpha = 0.25f;

        return staircase;
    }

    static GameObject BuildPit(Transform parent, Shader shader)
    {
        GameObject pit = new GameObject("Pit");
        Undo.RegisterCreatedObjectUndo(pit, "Build Isometric Demo Level");
        pit.transform.SetParent(parent, false);

        // Everything is the same white as the ground, so the pit reads as a hole
        // in the same floor with no painted outline. The IsoFaceShade shader
        // shades the walls' vertical faces lighter/darker on the correct sides
        // (relative to the global sun), which is what visually separates them.
        Material white = Flat(shader, Color.white);

        // Floor
        CreateBox(pit.transform, "PitFloor",
            new Vector3(PitCenter.x, -PitDepth - GroundThickness * 0.5f, PitCenter.z),
            new Vector3(PitHalfX * 2f, GroundThickness, PitHalfZ * 2f), white);

        float wallHeight = PitDepth;
        float wallCenterY = -PitDepth * 0.5f;

        // Walls sit flush against the inside of the hole edge (inset by half their
        // own thickness) so they don't overlap/z-fight with the ground slabs above.
        float halfWall = PitWallThickness * 0.5f;

        CreateBox(pit.transform, "PitWall_West",
            new Vector3(PitCenter.x - PitHalfX + halfWall, wallCenterY, PitCenter.z),
            new Vector3(PitWallThickness, wallHeight, PitHalfZ * 2f), white);

        CreateBox(pit.transform, "PitWall_East",
            new Vector3(PitCenter.x + PitHalfX - halfWall, wallCenterY, PitCenter.z),
            new Vector3(PitWallThickness, wallHeight, PitHalfZ * 2f), white);

        CreateBox(pit.transform, "PitWall_Near",
            new Vector3(PitCenter.x, wallCenterY, PitCenter.z + PitHalfZ - halfWall),
            new Vector3(PitHalfX * 2f, wallHeight, PitWallThickness), white);

        CreateBox(pit.transform, "PitWall_Far",
            new Vector3(PitCenter.x, wallCenterY, PitCenter.z - PitHalfZ + halfWall),
            new Vector3(PitHalfX * 2f, wallHeight, PitWallThickness), white);

        return pit;
    }

    // A single directional light whose forward vector defines the one, global
    // diagonal angle every projected shadow uses. Real-time shadows are turned
    // off - our objects are unlit and shadows are faked by BoxProjectedShadow.
    static Light SetupGlobalLight()
    {
        Light light = null;
        foreach (Light l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (l.type == LightType.Directional) { light = l; break; }
        }

        if (light == null)
        {
            GameObject go = new GameObject("Global Light");
            Undo.RegisterCreatedObjectUndo(go, "Build Isometric Demo Level");
            light = go.AddComponent<Light>();
            light.type = LightType.Directional;
        }
        else
        {
            Undo.RecordObject(light, "Build Isometric Demo Level");
            Undo.RecordObject(light.transform, "Build Isometric Demo Level");
        }

        light.intensity = 1f;

        // The sun angle is exposed (and applied) via GlobalLightController, so it can
        // be tuned from the inspector and re-aims every shadow at once.
        GlobalLightController ctrl = light.GetComponent<GlobalLightController>();
        if (ctrl == null) ctrl = Undo.AddComponent<GlobalLightController>(light.gameObject);
        ctrl.sunEulerAngles = new Vector3(50f, 45f, 0f);
        ctrl.realtimeShadows = LightShadows.None;
        light.transform.rotation = Quaternion.Euler(ctrl.sunEulerAngles);
        light.shadows = LightShadows.None;
        return light;
    }

    static void AddShadow(GameObject caster, Light light, Shader shadowShader,
        bool dynamic, bool cylindrical, bool projectOntoSurfaceBelow, float groundY = 0f)
    {
        if (caster == null) return;
        if (caster.GetComponentInChildren<Collider>() == null) return;

        BoxProjectedShadow shadow = caster.GetComponent<BoxProjectedShadow>();
        if (shadow == null) shadow = Undo.AddComponent<BoxProjectedShadow>(caster);
        shadow.globalLight = light;
        shadow.shadowShader = shadowShader;
        shadow.groundY = groundY;
        shadow.dynamic = dynamic;
        shadow.cylindrical = cylindrical;
        shadow.projectOntoSurfaceBelow = projectOntoSurfaceBelow;
    }

    static void SetupCamera(Transform player)
    {
        Camera cam = Camera.main;
        GameObject camGo;
        if (cam == null)
        {
            camGo = new GameObject("Main Camera");
            Undo.RegisterCreatedObjectUndo(camGo, "Build Isometric Demo Level");
            camGo.tag = "MainCamera";
            cam = camGo.AddComponent<Camera>();
        }
        else
        {
            camGo = cam.gameObject;
            Undo.RecordObject(camGo.transform, "Build Isometric Demo Level");
        }

        cam.orthographic = true;
        cam.orthographicSize = 8f;

        Vector3 offset = new Vector3(0f, 12f, -12f);
        camGo.transform.rotation = Quaternion.Euler(50f, 0f, 0f);
        camGo.transform.position = player.position + offset;

        CameraFollow follow = camGo.GetComponent<CameraFollow>();
        if (follow == null) follow = camGo.AddComponent<CameraFollow>();
        follow.target = player;
        follow.offset = offset;
    }
}
