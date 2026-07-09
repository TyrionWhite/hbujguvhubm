using UnityEngine;

// Fades every renderer under this object to translucent whenever the camera's
// line of sight to the player passes through one of this object's own colliders
// (i.e. the player has walked behind the staircase).
public class StaircaseOcclusionFade : MonoBehaviour
{
    public Transform player;
    public Shader shader;
    public Color baseColor = Color.gray;
    public float transparentAlpha = 0.25f;

    Renderer[] renderers;
    Material[] opaqueMats;
    Material[] transparentMats;
    bool isTransparent;
    Camera cam;

    void Start()
    {
        cam = Camera.main;
        renderers = GetComponentsInChildren<Renderer>();
        opaqueMats = new Material[renderers.Length];
        transparentMats = new Material[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            opaqueMats[i] = URPTransparencyUtil.CreateOpaque(shader, baseColor);
            transparentMats[i] = URPTransparencyUtil.CreateTransparent(shader, baseColor, transparentAlpha);
            renderers[i].sharedMaterial = opaqueMats[i];
        }
    }

    void Update()
    {
        if (player == null || cam == null) return;

        Vector3 origin = cam.transform.position;
        Vector3 targetPoint = player.position + Vector3.up * 1f;
        Vector3 delta = targetPoint - origin;
        float distance = delta.magnitude;
        if (distance < 0.01f) return;
        Vector3 dir = delta / distance;

        bool blocked = false;
        RaycastHit[] hits = Physics.RaycastAll(origin, dir, distance - 0.1f);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].transform.IsChildOf(transform))
            {
                blocked = true;
                break;
            }
        }

        if (blocked != isTransparent)
        {
            isTransparent = blocked;
            Material[] set = isTransparent ? transparentMats : opaqueMats;
            for (int i = 0; i < renderers.Length; i++)
                renderers[i].sharedMaterial = set[i];
        }
    }
}
