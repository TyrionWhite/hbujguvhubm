using UnityEngine;
using UnityEngine.Rendering;

// Runtime opaque/transparent toggling for URP materials (Lit or Unlit shaders
// that expose the standard Surface Options block). Swapping between two
// pre-built materials is far more reliable at runtime than flipping shader
// keywords on a single shared material every frame.
public static class URPTransparencyUtil
{
    public static Material CreateOpaque(Shader shader, Color color)
    {
        Material mat = new Material(shader);
        SetOpaque(mat);
        mat.color = color;
        return mat;
    }

    public static Material CreateTransparent(Shader shader, Color color, float alpha)
    {
        Material mat = new Material(shader);
        SetTransparent(mat);
        Color c = color;
        c.a = alpha;
        mat.color = c;
        return mat;
    }

    public static void SetOpaque(Material mat)
    {
        mat.SetFloat("_Surface", 0f);
        mat.SetOverrideTag("RenderType", "Opaque");
        mat.SetInt("_SrcBlend", (int)BlendMode.One);
        mat.SetInt("_DstBlend", (int)BlendMode.Zero);
        mat.SetInt("_ZWrite", 1);
        mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = (int)RenderQueue.Geometry;
    }

    public static void SetTransparent(Material mat)
    {
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", 0f);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = (int)RenderQueue.Transparent;
    }
}
