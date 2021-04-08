using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// practice from Catlike Coding Unity Tutorial: 
// https://catlikecoding.com/unity/tutorials/basics/

public class GPUGraph : MonoBehaviour
{

    const int maxResolution = 1000;

    [SerializeField, Range(10, maxResolution)] // enforces range on below variable
    private int resolution = 10;

    [SerializeField]
    FunctionLibrary.FunctionName function = (FunctionLibrary.FunctionName) 0;

    FunctionLibrary.FunctionName transitionFunction;

    public enum TransitionMode { Cycle, Random }

    [SerializeField]
    TransitionMode transitionMode = TransitionMode.Cycle;

    [SerializeField, Range(0f, 60f)]
    float functionDuration = 1f, transitionDuration = 1f;

    float duration;

    bool transitioning; // bool to determine if graph is transitioning or not

    ComputeBuffer positionsBuffer; // allocated memory to store point positions on GPU
    ComputeBuffer argsBuffer; // buffer for 5 arguments for Graphics.Draw function
    uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
    // args are index count per instance, instance count, start index location, base vertex location, start instance location

    [SerializeField]
    ComputeShader computeShader;

    [SerializeField]
    Material material;

    [SerializeField]
    Mesh mesh;

    private int subMeshIndex = 0;

    // accesss computeshader properties via text identifiers - will never be changed, so readonly used
    static readonly int
        positionsId = Shader.PropertyToID("_Positions"),
        resolutionId = Shader.PropertyToID("_Resolution"),
        stepId = Shader.PropertyToID("_Step"),
        timeId = Shader.PropertyToID("_Time"),
        transitionProgressId = Shader.PropertyToID("_TransitionProgress");

    // use OnEnable instead of Awake so these objects survive hot reloads (let us change code while in play mode)
    // activates right after Awake and after a hot reload is completed
    private void OnEnable() 
    {
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);

        // storing resolution * resolution Vector3 positions, each Vector3 is 3 floats 4 bytes each
        positionsBuffer = new ComputeBuffer(maxResolution * maxResolution, 3 * 4);
    }

    // invoked when component is disabled (if graph destroyed and right before hot reload)
    private void OnDisable()
    {
        if (positionsBuffer != null)
        {
            positionsBuffer.Release(); // free GPU memory allocated for the buffer
        }
        positionsBuffer = null; // possible for object to be reclaimed by Unity's memory garbage collection process

        if (argsBuffer != null)
        {
            argsBuffer.Release();
        }
        argsBuffer = null;
    }

    private void Update()
    {
        duration += Time.deltaTime;
        if (transitioning)
        {
            if (duration >= transitionDuration)
            {
                duration -= transitionDuration;
                transitioning = false;
            }

        }
        else if (duration >= functionDuration)
        {
            duration -= functionDuration;
            transitioning = true;
            transitionFunction = function;
            PickNextFunction();
        }

        UpdateFunctionOnGPU();
    }

    void UpdateFunctionOnGPU()
    {
        // Indirect args
        if (mesh != null)
        {
            args[0] = (uint)mesh.GetIndexCount(subMeshIndex);
            args[1] = (uint)(resolution * resolution);
            args[2] = (uint)mesh.GetIndexStart(subMeshIndex);
            args[3] = (uint)mesh.GetBaseVertex(subMeshIndex);
        }
        else
        {
            args[0] = args[1] = args[2] = args[3] = 0;
        }
        argsBuffer.SetData(args);

        float step = 2f / resolution;
        computeShader.SetInt(resolutionId, resolution);
        computeShader.SetFloat(stepId, step);
        computeShader.SetFloat(timeId, Time.time);

        if (transitioning)
        {
            computeShader.SetFloat(
                transitionProgressId,
                Mathf.SmoothStep(0f, 1f, duration / transitionDuration)
                );
        }

        var kernelIndex = (int)function +
            (int)(transitioning ? transitionFunction : function) * 
            FunctionLibrary.FunctionCount;
        computeShader.SetBuffer(kernelIndex, positionsId, positionsBuffer); // link buffer to kernel

        // run all groups (default 8x8 group size, resolution * resolution groups)
        int groups = Mathf.CeilToInt(resolution / 8f);
        computeShader.Dispatch(kernelIndex, groups, groups, 1);

        material.SetBuffer(positionsId, positionsBuffer);
        material.SetFloat(stepId, step);

        // create bounds box so unity knows where to draw the graph
        var bounds = new Bounds(Vector3.zero, new Vector3(2f, 2f + 2f / resolution, 2f));

        // render
        Graphics.DrawMeshInstancedIndirect(
            mesh, subMeshIndex, material, bounds, argsBuffer
        );

        
    }

    void PickNextFunction()
    {
        function = transitionMode == TransitionMode.Cycle ?
            FunctionLibrary.GetNextFunctionName(function) :
            FunctionLibrary.GetRandomFunctionNameOtherThan(function);
    }
}
