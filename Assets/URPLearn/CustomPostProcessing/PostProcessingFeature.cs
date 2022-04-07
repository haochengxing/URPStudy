using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PostProcessingFeature : ScriptableRendererFeature
{
    private PostProcessingPass _pass;

    [SerializeField]
    private List<PostProcessingEffect> _effects = new List<PostProcessingEffect>();

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.renderType!=CameraRenderType.Base)
        {
            return;
        }
        _pass.ConfigureTarget(renderer.cameraColorTarget,renderer.cameraDepth);
        renderer.EnqueuePass(_pass);
    }

    public override void Create()
    {
        _pass = new PostProcessingPass();
        _pass.Setup(_effects);
    }


    public class PostProcessingPass : ScriptableRenderPass
    {
        private const string CommandBufferTag = "CustomPostProcessing";

        private List<PostProcessingEffect> _effects;

        private PostProcessingRenderContext _postContext = new PostProcessingRenderContext();

        public PostProcessingPass()
        {
            this.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        }

        public void Setup(List<PostProcessingEffect> effects)
        {
            _effects = effects;
        }


        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get(CommandBufferTag);
            try
            {
                cmd.Clear();

                Render(cmd, ref renderingData, context);


            }
            finally
            {
                CommandBufferPool.Release(cmd);
            }
        }

        void Render(CommandBuffer cmd, ref RenderingData renderingData, ScriptableRenderContext context)
        {
            var cameraDes = renderingData.cameraData.cameraTargetDescriptor;
            var colorAttachment = this.colorAttachment;
            try
            {
                _postContext.Prepare(ref renderingData,colorAttachment);
                foreach (var e in _effects)
                {
                    if (e && e.active)
                    {
                        e.Render(cmd, ref renderingData, _postContext);
                    }
                }
                _postContext.BlitBackToSource(cmd);
                context.ExecuteCommandBuffer(cmd);
            }
            finally
            {
                _postContext.Release(cmd);
            }
        }
    }
}
