using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// World axes convention (fixed, non-rotating isometric camera):
//   +X / -X = screen right / left   (horizontal, "screenwise")
//   +Z / -Z = screen up / down      (depth, the other "screenwise" axis)
//   +Y      = world height          ("world vertical" - jumping, falling)
//
// The player is a TRUE CYLINDER: a kinematic Rigidbody + a convex cylinder
// MeshCollider generated at runtime (NOT a capsule CharacterController).
//
// Movement model:
//  - Force-based with momentum: input applies an acceleration force that
//    integrates into a persistent velocity, bled by per-medium drag.
//  - Horizontal speed is clamped to the active maximum (walk / run / crouch;
//    run in the air) ONLY while there is horizontal input. Vertical speed is
//    never clamped.
//  - All movement uses swept collision (Rigidbody.SweepTest), so the cylinder
//    can never tunnel through thin geometry or fall below the ground.
//  - Step-up: walking into a ledge no taller than stepHeight teleports the
//    cylinder's bottom to the ledge top (classic up -> forward -> down sweep),
//    preserving horizontal momentum exactly like a standard game controller.
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(MeshCollider))]
public class PlayerController : MonoBehaviour
{
    // Per-environment momentum feel. Lower drag = more momentum preserved.
    [System.Serializable]
    public class Medium
    {
        public string name = "Medium";
        [Tooltip("Acceleration force per unit of input. Divided by mass to get acceleration.")]
        public float accelerationForce = 140f;
        [Tooltip("Linear drag (1/s). Higher = momentum bleeds faster.")]
        public float drag = 16f;
        [Tooltip("Input authority in this medium (0 = none, 1 = full).")]
        [Range(0f, 1f)] public float control = 1f;
        [Tooltip("Multiplier on gravity while in this medium.")]
        public float gravityScale = 1f;
    }

    [Header("Cylinder body")]
    public float radius = 0.4f;
    public float standingHeight = 2f;
    [Range(0.1f, 1f)]
    [Tooltip("Crouch (sit) height as a fraction of standing height.")]
    public float crouchHeightFraction = 0.5f;
    [Range(3, 60)] public int colliderSegments = 20;
    [Tooltip("The flat sprite card, resized when crouching. Assigned by the level builder.")]
    public Transform visual;

    [Header("Speeds")]
    [Tooltip("Max running speed (hold Shift), m/s. Derived as the old ground walking speed (~6.8) + 25%.")]
    public float runSpeed = 8.5f;
    [Range(0f, 1f)]
    [Tooltip("Walking speed as a fraction of run speed.")]
    public float walkSpeedFraction = 0.4f;
    [Range(0f, 1f)]
    [Tooltip("Crouch speed as a fraction of walking speed.")]
    public float crouchSpeedFraction = 0.5f;

    [Header("Jump / gravity")]
    public float mass = 1f;
    public float gravity = -30f;
    [Tooltip("Peak jump height (m) on flat ground. Converted to an upward impulse using current gravity.")]
    public float jumpHeight = 1.8f;
    [Tooltip("Extra gravity multiplier while falling, for a snappier arc.")]
    public float fallGravityMultiplier = 1.0f;
    public float coyoteTime = 0.1f;
    public float jumpBufferTime = 0.1f;
 
    [Header("Stepping")]
    [Tooltip("Tallest ledge the cylinder automatically steps up onto: its bottom is " +
             "teleported to the ledge top, preserving horizontal momentum. Never snaps down.")]
    public float stepHeight = 0.45f;

    [Header("Media (momentum feel per environment)")]
    public Medium ground = new Medium { name = "Ground", accelerationForce = 140f, drag = 16f, control = 1f, gravityScale = 1f };
    public Medium air = new Medium { name = "Air", accelerationForce = 45f, drag = 1.2f, control = 0.6f, gravityScale = 1f };
    public Medium pit = new Medium { name = "Pit", accelerationForce = 90f, drag = 8f, control = 0.85f, gravityScale = 1f };
    [Tooltip("Character is 'in the pit' when its feet drop below this world Y.")]
    public float pitDetectHeight = -0.5f;

    const float Skin = 0.02f;             // gap kept between the cylinder and surfaces
    const float MinStepForward = 0.05f;   // minimum forward probe when stepping up
    const float StickVelocity = -2f;      // small downward velocity that keeps us planted
    const float MaxDeltaTime = 0.05f;     // clamp frame spikes so physics stays stable

    Rigidbody rb;
    MeshCollider col;
    int castMask;

    Vector3 horizontalVelocity;
    float verticalVelocity;
    float coyoteTimer;
    float jumpBufferTimer;
    bool grounded;

    float currentHeight;
    Vector3 visualBaseScale;

    readonly Collider[] overlapBuf = new Collider[16];

    // --- Derived speeds ---
    public float WalkSpeed => runSpeed * walkSpeedFraction;
    public float CrouchSpeed => WalkSpeed * crouchSpeedFraction;
    float CrouchHeight => standingHeight * crouchHeightFraction;

    // --- Cylinder AABB from the transform (independent of PhysX sync lag) ---
    Vector3 BoxCenter => transform.position + Vector3.up * (currentHeight * 0.5f);
    Vector3 BoxHalfExtents => new Vector3(radius, currentHeight * 0.5f, radius);

    public bool IsCrouching { get; private set; }
    public bool IsRunning { get; private set; }
    public Vector3 Velocity => horizontalVelocity + Vector3.up * verticalVelocity;
    public Medium CurrentMedium { get; private set; }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        col = GetComponent<MeshCollider>();
        col.convex = true;

        // Exclude the player's own layer (Ignore Raycast, set by the builder) so
        // raycast/overlap queries never hit the cylinder itself. Swept movement
        // uses Rigidbody.SweepTest, which already excludes the body's own collider.
        castMask = Physics.DefaultRaycastLayers;

        if (visual != null) visualBaseScale = visual.localScale;
        currentHeight = standingHeight;
        ApplyHeight();
    }

    void Update()
    {
        float dt = Mathf.Min(Time.deltaTime, MaxDeltaTime);
        Keyboard kb = Keyboard.current;

        HandleCrouchInput(kb);

        grounded = GroundProbe();
        IsRunning = grounded && !IsCrouching && kb != null && kb.leftShiftKey.isPressed;

        Medium medium = !grounded ? air
            : (transform.position.y < pitDetectHeight ? pit : ground);
        CurrentMedium = medium;

        // --- Forgiving jump timers ---
        if (grounded) coyoteTimer = coyoteTime;
        else coyoteTimer -= dt;

        if (kb != null && kb.spaceKey.wasPressedThisFrame) jumpBufferTimer = jumpBufferTime;
        else jumpBufferTimer -= dt;

        // --- Input direction ---
        Vector3 inputDir = ReadInput(kb);

        // --- Force -> velocity (momentum) ---
        Vector3 force = inputDir * medium.accelerationForce * medium.control;
        horizontalVelocity += force / Mathf.Max(mass, 0.0001f) * dt;
        horizontalVelocity *= Mathf.Clamp01(1f - medium.drag * dt);

        // Clamp the horizontal plane speed to the active max ONLY while there is
        // horizontal input; passive momentum is left to drag. Vertical speed
        // (falling, jumping) is never clamped.
        if (inputDir.sqrMagnitude > 1e-4f)
        {
            float max = ActiveMaxSpeed();
            if (horizontalVelocity.sqrMagnitude > max * max)
                horizontalVelocity = horizontalVelocity.normalized * max;
        }

        // --- Vertical: gravity force + jump impulse ---
        if (grounded && verticalVelocity < 0f) verticalVelocity = StickVelocity;

        if (jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            float g = Mathf.Abs(gravity * medium.gravityScale);
            verticalVelocity = Mathf.Sqrt(2f * g * jumpHeight);
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            grounded = false;
        }

        float gScale = medium.gravityScale * (verticalVelocity < 0f ? fallGravityMultiplier : 1f);
        verticalVelocity += gravity * gScale * dt;

        // --- Swept movement ---
        MoveVertical(verticalVelocity * dt);
        MoveHorizontal(new Vector3(horizontalVelocity.x, 0f, horizontalVelocity.z) * dt);
        Depenetrate(); // safety net for any residual overlap (e.g. crouch/stand)
    }

    Vector3 ReadInput(Keyboard kb)
    {
        if (kb == null) return Vector3.zero;
        Vector3 dir = Vector3.zero;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) dir.x -= 1f;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) dir.x += 1f;
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed) dir.z -= 1f;
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed) dir.z += 1f;
        if (dir.sqrMagnitude > 1f) dir.Normalize();
        return dir;
    }

    float ActiveMaxSpeed()
    {
        if (IsCrouching) return CrouchSpeed;
        if (!grounded) return runSpeed; // air momentum may stay at full running speed
        return IsRunning ? runSpeed : WalkSpeed;
    }

    // ---- Crouch / stand -----------------------------------------------------

    void HandleCrouchInput(Keyboard kb)
    {
        if (kb == null || !kb.cKey.wasPressedThisFrame) return;
        if (IsCrouching)
        {
            if (HeadroomClear(standingHeight)) SetHeight(standingHeight, false);
        }
        else
        {
            SetHeight(CrouchHeight, true);
        }
    }

    void SetHeight(float h, bool crouching)
    {
        IsCrouching = crouching;
        currentHeight = h;
        ApplyHeight();
    }

    void ApplyHeight()
    {
        Mesh previous = col.sharedMesh;
        col.sharedMesh = BuildCylinderMesh(radius, currentHeight, colliderSegments);
        if (previous != null) Destroy(previous); // avoid leaking a mesh per crouch toggle

        if (visual != null)
        {
            visual.localScale = new Vector3(visualBaseScale.x, currentHeight, visualBaseScale.z);
            visual.localPosition = new Vector3(0f, currentHeight * 0.5f, 0f);
        }
    }

    bool HeadroomClear(float targetHeight)
    {
        Vector3 bottom = transform.position + Vector3.up * radius;
        Vector3 top = transform.position + Vector3.up * (targetHeight - radius);
        return !Physics.CheckCapsule(bottom, top, radius * 0.95f, castMask, QueryTriggerInteraction.Ignore);
    }

    // ---- Swept movement / collision -----------------------------------------

    // Sweep the cylinder's own collider along dir. SweepTest excludes the body's
    // own collider and respects triggers per the parameter.
    bool Sweep(Vector3 dir, float dist, out RaycastHit hit)
    {
        Physics.SyncTransforms(); // keep PhysX in sync with our manual transform moves
        return rb.SweepTest(dir, out hit, dist, QueryTriggerInteraction.Ignore);
    }

    void MoveVertical(float dy)
    {
        if (Mathf.Abs(dy) < 1e-8f) return;
        Vector3 dir = dy > 0f ? Vector3.up : Vector3.down;
        float dist = Mathf.Abs(dy);

        if (Sweep(dir, dist + Skin, out RaycastHit hit))
        {
            transform.position += dir * Mathf.Max(hit.distance - Skin, 0f);
            if (dy < 0f) verticalVelocity = StickVelocity;       // landed
            else if (verticalVelocity > 0f) verticalVelocity = 0f; // hit ceiling
        }
        else
        {
            transform.position += dir * dist;
        }
    }

    void MoveHorizontal(Vector3 horiz)
    {
        float dist = horiz.magnitude;
        if (dist < 1e-8f) return;
        Vector3 dir = horiz / dist;

        if (!Sweep(dir, dist + Skin, out RaycastHit hit))
        {
            transform.position += horiz;
            return;
        }

        // Advance to the obstacle, then either step up onto it or slide along it.
        float allowed = Mathf.Max(hit.distance - Skin, 0f);
        transform.position += dir * allowed;
        float remaining = dist - allowed;

        if (grounded && TryStepUp(dir, remaining)) return;

        // Cancel only the momentum component pushing into the wall.
        Vector3 n = hit.normal;
        n.y = 0f;
        if (n.sqrMagnitude > 1e-6f)
        {
            n.Normalize();
            float into = Vector3.Dot(horizontalVelocity, n);
            if (into < 0f) horizontalVelocity -= n * into;
        }

        // Slide the remaining distance along the wall.
        Vector3 slide = Vector3.ProjectOnPlane(dir, hit.normal);
        slide.y = 0f;
        if (slide.sqrMagnitude > 1e-6f)
        {
            slide.Normalize();
            float slideDist = remaining;
            if (Sweep(slide, slideDist + Skin, out RaycastHit slideHit))
                slideDist = Mathf.Max(slideHit.distance - Skin, 0f);
            transform.position += slide * slideDist;
        }
    }

    // Classic game step-up (up -> forward -> down sweeps): if the obstacle ahead
    // is a ledge no taller than stepHeight, teleport the cylinder's bottom onto
    // its top. Horizontal momentum is never modified, so speed carries across
    // the step exactly.
    bool TryStepUp(Vector3 dir, float remaining)
    {
        Vector3 start = transform.position;
        float forward = Mathf.Max(remaining, MinStepForward) + Skin;

        // 1) Up: as far as stepHeight allows (a ceiling may shorten it).
        float up = stepHeight;
        if (Sweep(Vector3.up, stepHeight + Skin, out RaycastHit upHit))
            up = Mathf.Max(upHit.distance - Skin, 0f);
        if (up < 0.01f) return false;
        transform.position = start + Vector3.up * up;

        // 2) Forward: if we can't clear the riser at all, it's a real wall.
        float fwd = forward;
        if (Sweep(dir, forward + Skin, out RaycastHit fwdHit))
            fwd = Mathf.Max(fwdHit.distance - Skin, 0f);
        if (fwd < MinStepForward * 0.5f)
        {
            transform.position = start;
            return false;
        }
        transform.position += dir * fwd;

        // 3) Down: land on the step. Commit only if we actually ended up higher
        //    on a walkable top - never snap down below where we started.
        if (Sweep(Vector3.down, up + Skin, out RaycastHit downHit))
        {
            float drop = Mathf.Max(downHit.distance - Skin, 0f);
            float rise = up - drop;
            if (rise > 0.01f && downHit.normal.y >= 0.7f)
            {
                transform.position += Vector3.down * drop;
                verticalVelocity = StickVelocity;
                return true;
            }
        }

        transform.position = start;
        return false;
    }

    void Depenetrate()
    {
        for (int iter = 0; iter < 4; iter++)
        {
            int count = Physics.OverlapBoxNonAlloc(BoxCenter, BoxHalfExtents + Vector3.one * Skin,
                overlapBuf, Quaternion.identity, castMask, QueryTriggerInteraction.Ignore);

            bool any = false;
            for (int i = 0; i < count; i++)
            {
                Collider other = overlapBuf[i];
                if (other == col || other.isTrigger) continue;

                if (Physics.ComputePenetration(
                        col, transform.position, transform.rotation,
                        other, other.transform.position, other.transform.rotation,
                        out Vector3 dir, out float dist) && dist > 0f)
                {
                    transform.position += dir * dist;

                    Vector3 hn = new Vector3(dir.x, 0f, dir.z);
                    if (hn.sqrMagnitude > 1e-6f)
                    {
                        hn.Normalize();
                        float into = Vector3.Dot(horizontalVelocity, hn);
                        if (into < 0f) horizontalVelocity -= hn * into;
                    }
                    if (dir.y > 0.5f && verticalVelocity < 0f) verticalVelocity = StickVelocity;
                    if (dir.y < -0.5f && verticalVelocity > 0f) verticalVelocity = 0f;
                    any = true;
                }
            }
            if (!any) break;
        }
    }

    bool GroundProbe()
    {
        if (verticalVelocity > 0.1f) return false;
        Vector3 origin = transform.position + Vector3.up * (radius + 0.05f);
        if (Physics.SphereCast(origin, radius * 0.9f, Vector3.down, out RaycastHit hit,
                (radius + 0.05f) + 0.15f, castMask, QueryTriggerInteraction.Ignore))
            return hit.normal.y > 0.5f;
        return false;
    }

    // ---- Cylinder mesh (base at local y=0, top at y=height) -----------------

    static Mesh BuildCylinderMesh(float r, float h, int seg)
    {
        seg = Mathf.Clamp(seg, 3, 60);
        List<Vector3> verts = new List<Vector3>(2 + seg * 2);
        List<int> tris = new List<int>(seg * 12);

        verts.Add(new Vector3(0f, 0f, 0f)); // 0 bottom center
        verts.Add(new Vector3(0f, h, 0f));  // 1 top center

        for (int i = 0; i < seg; i++)
        {
            float a = (float)i / seg * Mathf.PI * 2f;
            float x = Mathf.Cos(a) * r;
            float z = Mathf.Sin(a) * r;
            verts.Add(new Vector3(x, 0f, z));
            verts.Add(new Vector3(x, h, z));
        }

        for (int i = 0; i < seg; i++)
        {
            int b0 = 2 + i * 2;
            int t0 = b0 + 1;
            int ni = (i + 1) % seg;
            int b1 = 2 + ni * 2;
            int t1 = b1 + 1;

            tris.Add(b0); tris.Add(t0); tris.Add(t1);
            tris.Add(b0); tris.Add(t1); tris.Add(b1);
            tris.Add(0); tris.Add(b1); tris.Add(b0);
            tris.Add(1); tris.Add(t0); tris.Add(t1);
        }

        Mesh m = new Mesh { name = "PlayerCylinder" };
        m.SetVertices(verts);
        m.SetTriangles(tris, 0);
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }
}
