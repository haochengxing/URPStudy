using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CopyColorWithAlphaFeature : ScriptableRendererFeature
{
    [SerializeField]
    private Material _material;

    private CopyColorWithAlphaPass _pass;

    RenderTargetHandle m_OpaqueColor;


    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.renderType!=CameraRenderType.Base)
        {
            return;
        }
        if (_pass==null)
        {
            if (_material)
            {
                _pass = new CopyColorWithAlphaPass(RenderPassEvent.AfterRenderingSkybox, _material);
            }
            else
            {
                return;
            }
        }
        _pass.Setup(renderer.cameraColorTarget,m_OpaqueColor,Downsampling._4xBox);
        renderer.EnqueuePass(_pass);
    }

    private void EnsureMaterialInEditor()
    {
#if UNITY_EDITOR
        if (!_material)
        {
            _material = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/URPLearn/CustomCopyColorPass/Materials/CopyColorMat.mat");
        }
#endif
    }

    public override void Create()
    {
        this.EnsureMaterialInEditor();
        m_OpaqueColor.Init("_CameraOpaqueTexture");
        if (_material)
        {
            _pass = new CopyColorWithAlphaPass(RenderPassEvent.AfterRenderingSkybox,_material);
        }
    }

    public class CopyColorWithAlphaPass : ScriptableRenderPass
    {

        int m_SampleOffsetShaderHandle;
        Material m_SamplingMaterial;
        Downsampling m_DownsamplingMethod;

        private RenderTargetIdentifier source { get; set; }

        private RenderTargetHandle destination { get; set; }

        const string m_ProfilerTag = "Copy Color";


        public CopyColorWithAlphaPass(RenderPassEvent evt,Material samplingMaterial)
        {
            m_SamplingMaterial = samplingMaterial;
            m_SampleOffsetShaderHandle = Shader.PropertyToID("_SampleOffset");
            renderPassEvent = evt;
            m_DownsamplingMethod = Downsampling.None;
        }

        public void Setup(RenderTargetIdentifier source, RenderTargetHandle destination, Downsampling downsampling) {
            this.source = source;
            this.destination = destination;
            m_DownsamplingMethod = downsampling;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            RenderTextureDescriptor descriptor = cameraTextureDescriptor;
            descriptor.msaaSamples = 1;
            descriptor.depthBufferBits = 0;
            if (m_DownsamplingMethod == Downsampling._2xBilinear)
            {
                descriptor.width /= 2;
                descriptor.height /= 2;
            }
            else if (m_DownsamplingMethod == Downsampling._4xBox || m_DownsamplingMethod == Downsampling._4xBilinear)
            {
                descriptor.width /= 4;
                descriptor.height /= 4;
            }
            cmd.GetTemporaryRT(destination.id,descriptor,m_DownsamplingMethod==Downsampling.None?FilterMode.Point:FilterMode.Bilinear);
        }


        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_SamplingMaterial==null)
            {
                Debug.LogErrorFormat("Missing {0}.{1} render pass will not execute. Check for missing reference in the renderer resources.", m_SamplingMaterial,GetType().Name);
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);
            RenderTargetIdentifier opaqueColorRT = destination.Identifier();

            switch (m_DownsamplingMethod)
            {
                case Downsampling.None:
                    Blit(cmd,source,opaqueColorRT);
                    break;
                case Downsampling._2xBilinear:
                    Blit(cmd,source,opaqueColorRT);
                    break;
                case Downsampling._4xBox:
                    m_SamplingMaterial.SetFloat(m_SampleOffsetShaderHandle,2);
                    Blit(cmd,source,opaqueColorRT,m_SamplingMaterial);
                    break;
                case Downsampling._4xBilinear:
                    Blit(cmd,source,opaqueColorRT);
                    break;
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            if (cmd==null)
            {
                return;
            }
            if (destination!=RenderTargetHandle.CameraTarget)
            {
                cmd.ReleaseTemporaryRT(destination.id);
                destination = RenderTargetHandle.CameraTarget;
            }
        }
    }
}
