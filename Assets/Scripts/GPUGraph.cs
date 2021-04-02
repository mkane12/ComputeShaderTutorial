using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// practice from Catlike Coding Unity Tutorial: 
// https://catlikecoding.com/unity/tutorials/basics/

public class GPUGraph : MonoBehaviour
{

    [SerializeField, Range(10, 200)] // enforces range on below variable
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

    [SerializeField]
    ComputeShader computeShader;

    [SerializeField]
    Material material;

    [SerializeField]
    Mesh mesh;

    // accesss computeshader properties via text identifiers - will never be changed, so readonly used
    static readonly int
        positionsId = Shader.PropertyToID("_Positions"),
        resolutionId = Shader.PropertyToID("_Resolution"),
        stepId = Shader.PropertyToID("_Step"),
        timeId = Shader.PropertyToID("_Time");

    // use OnEnable instead of Awake so these objects survive hot reloads (let us change code while in play mode)
    // activates right after Awake and after a hot reload is completed
    private void OnEnable() 
    {
        // storing resolution * resolution Vector3 positions, each Vector3 is 3 floats 4 bytes each
        positionsBuffer = new ComputeBuffer(1, 3 * 4, ComputeBufferType.IndirectArguments);
    }

    // invoked when component is disabled (if graph destroyed and right before hot reload)
    private void OnDisable()
    {
        positionsBuffer.Release(); // free GPU memory allocated for the buffer
        positionsBuffer = null; // possible for object to be reclaimed by Unity's memory garbage collection process
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
        float step = 2f / resolution;
        computeShader.SetInt(resolutionId, resolution);
        computeShader.SetFloat(stepId, step);
        computeShader.SetFloat(timeId, Time.time);

        computeShader.SetBuffer(0, positionsId, positionsBuffer); // link buffer to kernel

        // run all groups (default 8x8 group size, resolution * resolution groups)
        int groups = Mathf.CeilToInt(resolution / 8f);
        computeShader.Dispatch(0, groups, groups, 1);

        material.SetBuffer(positionsId, positionsBuffer);
        material.SetFloat(stepId, step);

        // create bounds box so unity knows where to draw the graph
        var bounds = new Bounds(Vector3.zero, Vector3.one * (2f + 2f / resolution));

        Graphics.DrawMeshInstancedIndirect(mesh, 0, material, bounds, positionsBuffer);
    }

    void PickNextFunction()
    {
        function = transitionMode == TransitionMode.Cycle ?
            FunctionLibrary.GetNextFunctionName(function) :
            FunctionLibrary.GetRandomFunctionNameOtherThan(function);
    }
}
