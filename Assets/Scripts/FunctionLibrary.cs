using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// a collection of publicly accessible functions
public static class FunctionLibrary
{
    // delegate defines what kind of method something can reference
    public delegate Vector3 Function (float u, float v, float t);

    public enum FunctionName { Wave, MultiWave, Ripple, Sphere, Torus }

    static Function[] functions = { Wave, MultiWave, Ripple, Sphere, Torus };

    public static Function GetFunction (FunctionName name)
    {
        return functions[(int)name];
    }

    public static FunctionName GetNextFunctionName (FunctionName name)
    {
        return (int)name < functions.Length - 1 ? name + 1 : 0;
    }

    public static FunctionName GetRandomFunctionNameOtherThan (FunctionName name)
    {
        var choice = (FunctionName)Random.Range(0, functions.Length);
        return choice == name ? 0 : choice;
    }

    // make public to make it accessible, make static to make it work on class level
    // returns sin x-position x over time t
    public static Vector3 Wave (float u, float v, float t)
    {
        Vector3 p;
        p.x = u;
        p.y = Mathf.Sin(Mathf.PI * (u + v + t));
        p.z = v;

        return p;
    }

    // add another sin wave with double frequency
    public static Vector3 MultiWave (float u, float v, float t)
    {
        Vector3 p;
        p.x = u;

        p.y = Mathf.Sin(Mathf.PI * (u + 0.5f * t));
        p.y += 0.5f * Mathf.Sin(2f * Mathf.PI * (v + t));
        p.y += Mathf.Sin(Mathf.PI * (u + v + 0.25f * t)) * (1f / 2.5f);

        p.z = v;

        return p;
    }

    public static Vector3 Ripple (float u, float v, float t)
    {
        Vector3 p;
        p.x = u;

        float d = Mathf.Sqrt(u * u + v * v);
        p.y = Mathf.Sin(Mathf.PI * (4f * d - t)) / (1f + 10f * d);

        p.z = v;

        return p;
    }

    public static Vector3 Sphere (float u, float v, float t)
    {
        float r = 0.9f + 0.1f * Mathf.Sin(Mathf.PI * (6f * u + 4f * v + t));
        float s = r * Mathf.Cos(0.5f * Mathf.PI * v);

        Vector3 p;
        p.x = s * Mathf.Sin(Mathf.PI * u);
        p.y = r * Mathf.Sin(-.5f * Mathf.PI * v);
        p.z = s * Mathf.Cos(Mathf.PI * u);

        return p;
    }

    public static Vector3 Torus (float u, float v, float t)
    {
        float r1 = 0.7f + 0.1f * Mathf.Sin(Mathf.PI * (6f * u + 0.5f * t));
        float r2 = 0.15f + 0.05f * Mathf.Sin(Mathf.PI * (8f * u + 4f * v + 2f * t));


        float s = r1 + r2 * Mathf.Cos(Mathf.PI * v);

        Vector3 p;
        p.x = s * Mathf.Sin(Mathf.PI * u);
        p.y = r2 * Mathf.Sin(Mathf.PI * v);
        p.z = s * Mathf.Cos(Mathf.PI * u);

        return p;
    }

    public static Vector3 Morph(float u, float v, float t, Function from, Function to, float progress)
    {
        return Vector3.LerpUnclamped(from(u, v, t), to(u, v, t), Mathf.SmoothStep(0f, 1f, progress));
    }
}
