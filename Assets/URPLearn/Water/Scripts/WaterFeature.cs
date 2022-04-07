using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class WaterFeature : ScriptableRendererFeature
{

    private RefractionMaskPass _pass;
    private SSPRGeneratePass _ssprPass;

    public override void Create()
    {
        _pass = new RefractionMaskPass();
        _ssprPass = new SSPRGeneratePass();
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.renderType != CameraRenderType.Base)
        {
            return;
        }
        renderer.EnqueuePass(_pass);
        _ssprPass.ConfigureTarget(renderer.cameraColorTarget,renderer.cameraDepth);
        renderer.EnqueuePass(_ssprPass);
    }

    class RefractionMaskPass : ScriptableRenderPass
    {
        private const string COMMAND_BUFFER_TAG = "RefractionMaskPass";

        public RefractionMaskPass()
        {
            this.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get(COMMAND_BUFFER_TAG);
            try
            {
                foreach (var water in Water.visibles)
                {
                    var render = water.GetComponent<Renderer>();
                    cmd.DrawRenderer(render,render.sharedMaterial,0,0);
                }
                context.ExecuteCommandBuffer(cmd);
            }
            finally
            {
                CommandBufferPool.Release(cmd);
            }
        }

        
    }

    class SSPRGeneratePass : ScriptableRenderPass
    {
        private const string COMMAND_BUFFER_TAG = "WaterSSPRPass";

        private SSPRTexGenerator _generator;

        public SSPRGeneratePass()
        {
            this.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
            _generator = new SSPRTexGenerator("WaterReflectionTex");
            _generator.excludeBackground = true;
            _generator.enableBlur = false;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (Water.visibles.Count==0)
            {
                return;
            }

            var cmd = CommandBufferPool.Get(COMMAND_BUFFER_TAG);
            try
            {
                foreach (var w in Water.visibles)
                {
                    var descriptor = new PlanarDescriptor()
                    {
                        position = w.transform.position,
                        normal = w.transform.up,
                    };

                    _generator.Render(cmd,ref renderingData,ref descriptor);
                    context.ExecuteCommandBuffer(cmd);
                    break;
                }
            }
            finally
            {
                CommandBufferPool.Release(cmd);
            }
        }

        
        
    }




}


