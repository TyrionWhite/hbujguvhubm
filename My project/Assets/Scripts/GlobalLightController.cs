using UnityEngine;

// Drives the one global directional light from an inspector-exposed sun angle.
// Because every projected shadow reads this light's forward vector, changing
// sunEulerAngles re-aims every shadow in the scene at once. Runs in edit mode too
// (ExecuteAlways) so the angle can be tuned without entering play mode.
[ExecuteAlways]
[RequireComponent(typeof(Light))]
public class GlobalLightController : MonoBehaviour
{
    [Tooltip("Sun direction as euler angles. X = pitch (down-tilt), Y = compass yaw. " +
             "This single value defines the global diagonal shadow angle.")]
    public Vector3 sunEulerAngles = new Vector3(50f, 45f, 0f);

    [Tooltip("Real-time (URP) shadows are off by default; shadows are faked by BoxProjectedShadow.")]
    public LightShadows realtimeShadows = LightShadows.None;

    void OnEnable() { Apply(); }
    void Update() { Apply(); }
    void OnValidate() { Apply(); }

    void Apply()
    {
        transform.rotation = Quaternion.Euler(sunEulerAngles);
        Light light = GetComponent<Light>();
        if (light != null) light.shadows = realtimeShadows;

        // Publish the sun's travel direction to all IsoFaceShade materials so
        // vertical faces are lit/darkened on the correct sides, consistently
        // across every object in the scene.
        Shader.SetGlobalVector("_IsoSunDirWS", transform.forward);
    }
}
