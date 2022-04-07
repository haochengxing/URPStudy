using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public abstract class PostProcessingEffect : ScriptableObject
{
    public bool active = true;

    public abstract void Render(CommandBuffer cmd, ref RenderingData renderingData, PostProcessingRenderContext context);
}
