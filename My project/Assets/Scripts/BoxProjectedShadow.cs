using System.Collections.Generic;
using UnityEngine;

// Casts a SHARP, hard-edged drop shadow by projecting this object's 3D collision
// box onto the ground plane along the global light direction. Every shadow in the
// scene uses the same directional light, so all shadows fall at one consistent
// diagonal angle. The shadow silhouette is the convex hull of the collider's
// silhouette points projected onto the ground - computed from the real collider,
// not a generic blob. Boxes use their 8 corners; cylindrical casters (the player)
// sample two rings so the shadow reads as a rounded oval, not a boxy hexagon.
public class BoxProjectedShadow : MonoBehaviour
{
    public Light globalLight;
    public Shader shadowShader;
    public float groundY = 0f;
    public float yOffset = 0.02f;               // lift above the receiving surface to avoid z-fighting
    public Color shadowColor = new Color(0f, 0f, 0f, 0.33f);
    [Tooltip("Recompute every frame (needed for moving casters like the player).")]
    public bool dynamic = false;
    [Tooltip("Sample the collider as a cylinder (rounded oval shadow) instead of a box.")]
    public bool cylindrical = false;
    [Tooltip("Cast down and drop the shadow onto whatever surface is directly below " +
             "(e.g. the pit floor when the player is inside it) instead of a fixed ground plane.")]
    public bool projectOntoSurfaceBelow = false;

    const int RingSegments = 16;

    Collider col;
    Mesh mesh;
    Vector3 lastDir;

    readonly List<Vector3> sourcePoints = new List<Vector3>(32);
    readonly List<Vector2> hullInput = new List<Vector2>(32);

    void Start()
    {
        col = GetComponentInChildren<Collider>();
        if (col == null)
        {
            enabled = false;
            return;
        }

        if (globalLight == null)
        {
            foreach (Light l in FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (l.type == LightType.Directional) { globalLight = l; break; }
            }
        }

        GameObject go = new GameObject(name + "_Shadow");
        // Parent to the scene root (identity) so mesh vertices can be authored in
        // world space and are not dragged around when the caster moves.
        go.transform.SetParent(transform.root, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        mesh = new Mesh { name = "ProjectedShadow" };
        mesh.MarkDynamic();
        go.AddComponent<MeshFilter>().sharedMesh = mesh;

        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = CreateShadowMaterial();
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;

        Rebuild();
    }

    // The shadow material must NEVER render opaque - an opaque projected hull
    // reads as a solid black shape (e.g. a fake black "cube" on the ground).
    // Prefer the dedicated always-transparent blob shader, re-resolved at
    // runtime so a stale serialized fallback can't sneak in; if it is somehow
    // unavailable, force the URP Unlit fallback into its transparent surface mode.
    Material CreateShadowMaterial()
    {
        Shader blob = Shader.Find("Custom/BlobShadow");
        if (blob != null)
        {
            Material mat = new Material(blob);
            mat.SetColor("_BaseColor", shadowColor);
            return mat;
        }

        Shader fallback = shadowShader != null ? shadowShader : Shader.Find("Universal Render Pipeline/Unlit");
        if (fallback == null)
        {
            Debug.LogError($"{name}: no shadow shader available, disabling projected shadow.", this);
            enabled = false;
            return null;
        }

        Debug.LogWarning($"{name}: Custom/BlobShadow not found, forcing transparent fallback material.", this);
        return URPTransparencyUtil.CreateTransparent(fallback, shadowColor, shadowColor.a);
    }

    void LateUpdate()
    {
        // Rebuild every frame for moving casters; for static ones, only when the
        // global sun angle has changed (so re-aiming the light updates them too).
        if (dynamic || (LightDir() - lastDir).sqrMagnitude > 1e-8f) Rebuild();
    }

    Vector3 LightDir()
    {
        Vector3 dir = globalLight != null ? globalLight.transform.forward : new Vector3(0.3f, -1f, 0.3f);
        dir.Normalize();
        if (dir.y > -0.05f) dir.y = -0.05f; // keep it pointing down so it hits the ground
        return dir;
    }

    // World Y of the surface the shadow lands on.
    float ResolveGroundY(Bounds b)
    {
        if (!projectOntoSurfaceBelow) return groundY;
        // Cast from just under the caster's top down to the surface below. The
        // caster's own collider is on the Ignore Raycast layer (excluded here).
        Vector3 origin = new Vector3(b.center.x, b.max.y - 0.01f, b.center.z);
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 100f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            return hit.point.y;
        return groundY;
    }

    void Rebuild()
    {
        if (col == null || mesh == null) return;

        lastDir = LightDir();

        Bounds b = col.bounds;
        Vector3 c = b.center;
        Vector3 e = b.extents;
        float receiverY = ResolveGroundY(b);

        // Collect the silhouette-defining source points in world space.
        sourcePoints.Clear();
        if (cylindrical)
        {
            // Two rings (bottom and top) approximating the cylinder outline.
            for (int i = 0; i < RingSegments; i++)
            {
                float a = (float)i / RingSegments * Mathf.PI * 2f;
                float x = Mathf.Cos(a) * e.x;
                float z = Mathf.Sin(a) * e.z;
                sourcePoints.Add(new Vector3(c.x + x, c.y - e.y, c.z + z));
                sourcePoints.Add(new Vector3(c.x + x, c.y + e.y, c.z + z));
            }
        }
        else
        {
            sourcePoints.Add(c + new Vector3(-e.x, -e.y, -e.z));
            sourcePoints.Add(c + new Vector3( e.x, -e.y, -e.z));
            sourcePoints.Add(c + new Vector3(-e.x, -e.y,  e.z));
            sourcePoints.Add(c + new Vector3( e.x, -e.y,  e.z));
            sourcePoints.Add(c + new Vector3(-e.x,  e.y, -e.z));
            sourcePoints.Add(c + new Vector3( e.x,  e.y, -e.z));
            sourcePoints.Add(c + new Vector3(-e.x,  e.y,  e.z));
            sourcePoints.Add(c + new Vector3( e.x,  e.y,  e.z));
        }

        Vector3 dir = LightDir();
        float planeY = receiverY + yOffset;

        hullInput.Clear();
        for (int i = 0; i < sourcePoints.Count; i++)
        {
            Vector3 p = sourcePoints[i];
            float t = (receiverY - p.y) / dir.y; // distance along light dir to reach the receiving surface
            if (t < 0f) t = 0f;
            Vector3 g = p + dir * t;
            hullInput.Add(new Vector2(g.x, g.z));
        }

        List<Vector2> hull = ConvexHull(hullInput);
        if (hull.Count < 3)
        {
            mesh.Clear();
            return;
        }

        int n = hull.Count;
        Vector3[] verts = new Vector3[n];
        for (int i = 0; i < n; i++)
            verts[i] = new Vector3(hull[i].x, planeY, hull[i].y);

        int[] tris = new int[(n - 2) * 3];
        int ti = 0;
        for (int i = 1; i < n - 1; i++)
        {
            tris[ti++] = 0;
            tris[ti++] = i;
            tris[ti++] = i + 1;
        }

        mesh.Clear();
        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.RecalculateBounds();
    }

    // Andrew's monotone chain convex hull. Returns hull points in CCW order.
    static List<Vector2> ConvexHull(List<Vector2> pts)
    {
        List<Vector2> p = new List<Vector2>(pts);
        p.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));

        // Drop exact duplicates so the cross-product test stays well-behaved.
        for (int i = p.Count - 1; i > 0; i--)
            if ((p[i] - p[i - 1]).sqrMagnitude < 1e-8f) p.RemoveAt(i);

        int count = p.Count;
        if (count < 3) return p;

        Vector2[] hull = new Vector2[2 * count];
        int k = 0;
        for (int i = 0; i < count; i++)
        {
            while (k >= 2 && Cross(hull[k - 2], hull[k - 1], p[i]) <= 0f) k--;
            hull[k++] = p[i];
        }
        for (int i = count - 2, t = k + 1; i >= 0; i--)
        {
            while (k >= t && Cross(hull[k - 2], hull[k - 1], p[i]) <= 0f) k--;
            hull[k++] = p[i];
        }

        List<Vector2> result = new List<Vector2>(k - 1);
        for (int i = 0; i < k - 1; i++) result.Add(hull[i]);
        return result;
    }

    static float Cross(Vector2 o, Vector2 a, Vector2 b)
    {
        return (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);
    }
}
