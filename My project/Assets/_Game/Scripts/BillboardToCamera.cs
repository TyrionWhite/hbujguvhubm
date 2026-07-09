using UnityEngine;

// Keeps the flat "2D sprite" visual facing the camera around the Y axis only,
// the standard trick for readable flat-card characters in a 3D isometric world.
public class BillboardToCamera : MonoBehaviour
{
    Camera cam;

    void LateUpdate()
    {
        if (cam == null)
        {
            cam = Camera.main;
            if (cam == null) return;
        }

        Vector3 fwd = transform.position - cam.transform.position;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.0001f) return;
        transform.rotation = Quaternion.LookRotation(fwd);
    }
}
