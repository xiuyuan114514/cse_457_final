using UnityEngine;

/// <summary>
/// Scales the legacy IMGUI (OnGUI) layer to a virtual reference resolution.
///
/// All of the in-game panels use hard-coded pixel font sizes and rect sizes
/// that were tuned for a roughly 960px-tall view. On a high-resolution
/// standalone build (e.g. a fullscreen 1900px+ display) those fixed pixels
/// occupy a much smaller fraction of the screen, so every label looks tiny.
///
/// Calling <see cref="Begin"/> at the top of an OnGUI method applies a uniform
/// <see cref="GUI.matrix"/> scale; laying out with <see cref="Width"/> /
/// <see cref="Height"/> instead of Screen.width/Screen.height then keeps the UI
/// at the same on-screen proportion across resolutions. Unity transforms the
/// IMGUI event mouse position by the same matrix, so GUI.Button and manual
/// Rect.Contains hit-tests keep working.
/// </summary>
public static class ImguiScale
{
    /// <summary>Screen height the UI layouts were authored against.</summary>
    public const float ReferenceHeight = 960f;

    /// <summary>
    /// Uniform scale factor. Only scales up on taller-than-reference displays so
    /// small windows keep the authored pixel sizes instead of shrinking further.
    /// </summary>
    public static float Factor => Mathf.Max(1f, Screen.height / ReferenceHeight);

    /// <summary>Virtual screen width to use for OnGUI layout math.</summary>
    public static float Width => Screen.width / Factor;

    /// <summary>Virtual screen height to use for OnGUI layout math.</summary>
    public static float Height => Screen.height / Factor;

    /// <summary>Call once at the top of an OnGUI method, after style init.</summary>
    public static void Begin()
    {
        GUI.matrix = Matrix4x4.Scale(new Vector3(Factor, Factor, 1f));
    }
}
