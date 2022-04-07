using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class Water : MonoBehaviour
{
    private static HashSet<Water> _visibles = new HashSet<Water>();

    public static IReadOnlyCollection<Water> visibles
    {
        get { return _visibles; }
    }


    private void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        _visibles.Add(this);
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        _visibles.Remove(this);
    }


    void OnBeginCameraRendering(ScriptableRenderContext context,Camera camera)
    {
        var viewMatrix = camera.worldToCameraMatrix;
        var projectMatrix = GL.GetGPUProjectionMatrix(camera.projectionMatrix,false);
        var matrixVP = projectMatrix * viewMatrix;
        var invMatrixVP = matrixVP.inverse;
        var material = GetComponent<Renderer>().sharedMaterial;
        material.SetMatrix("MatrixVP",matrixVP);
        material.SetMatrix("MatrixInvVP",invMatrixVP);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
