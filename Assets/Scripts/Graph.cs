using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// practice from Catlike Coding Unity Tutorial: 
// https://catlikecoding.com/unity/tutorials/basics/

public class Graph : MonoBehaviour
{

    [SerializeField]
    private Transform pointPrefab;

    [SerializeField, Range(10,100)] // enforces range on below variable
    private int resolution = 10;

    [SerializeField]
    FunctionLibrary.FunctionName function = (FunctionLibrary.FunctionName) 0;

    FunctionLibrary.FunctionName transitionFunction;

    public enum TransitionMode { Cycle, Random }

    [SerializeField]
    TransitionMode transitionMode = TransitionMode.Cycle;

    [SerializeField, Range(0f, 60f)]
    float functionDuration = 1f, transitionDuration = 1f;

    private Transform[] points;

    float duration;

    bool transitioning; // bool to determine if graph is transitioning or not

    private void Awake()
    {
        float step = 2f / resolution;
        var scale = Vector3.one * step;

        points = new Transform[resolution * resolution];

        for (int i = 0, x = 0, z = 0; i < points.Length; i++, x++)
        {
            Transform point = Instantiate(pointPrefab);
            point.SetParent(transform, false);

            point.localScale = scale;

            points[i] = point;
        }
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

        if (transitioning)
        {
            UpdateFunctionTransition();
        }
        else
        {
            UpdateFunction();
        }

    }

    void PickNextFunction()
    {
        function = transitionMode == TransitionMode.Cycle ?
            FunctionLibrary.GetNextFunctionName(function) :
            FunctionLibrary.GetRandomFunctionNameOtherThan(function);
    }

    void UpdateFunction() { 
        FunctionLibrary.Function f = FunctionLibrary.GetFunction(function);

        float time = Time.time;
        float step = 2f / resolution;

        float v = 0.5f * step - 1f;
        
        for (int i = 0, x = 0, z = 0; i < points.Length; i++, x++)
        {
            if (x == resolution)
            {
                x = 0;
                z += 1;
                v = (z + 0.5f) * step - 1f;
            }

            float u = (x + 0.5f) * step - 1f;

            points[i].localPosition = f(u, v, time);
        }
    }

    void UpdateFunctionTransition()
    {
        FunctionLibrary.Function
            from = FunctionLibrary.GetFunction(transitionFunction),
            to = FunctionLibrary.GetFunction(function);

        float progress = duration / transitionDuration;

        float time = Time.time;
        float step = 2f / resolution;

        float v = 0.5f * step - 1f;

        for (int i = 0, x = 0, z = 0; i < points.Length; i++, x++)
        {
            if (x == resolution)
            {
                x = 0;
                z += 1;
                v = (z + 0.5f) * step - 1f;
            }

            float u = (x + 0.5f) * step - 1f;

            points[i].localPosition = FunctionLibrary.Morph(u, v, time, from, to, progress);
        }
    }
}
