using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

// Reference: https://catlikecoding.com/unity/tutorials/basics/jobs/

public class Fractal : MonoBehaviour
{
    [SerializeField, Range(1, 8)]
    int depth = 4;

    [SerializeField]
    Mesh mesh;

    private int subMeshIndex = 0;

    [SerializeField]
    Material material;

    struct FractalPart
    {
        // public fields in non-public struct make it accessible just in Fractal class
        public Vector3 direction, worldPosition;
        public Quaternion rotation, worldRotation;
        public float spinAngle;
    }

    // Use generic NativeArrays for Jobs
    // > pointer to native machine memory; sidesteps default memory management overhead in C# memory heap
    NativeArray<FractalPart>[] parts;

    NativeArray<Matrix4x4>[] matrices;

    static Vector3[] directions = {
        Vector3.up, Vector3.right, Vector3.left, Vector3.forward, Vector3.back
    };

    static Quaternion[] rotations = {
        Quaternion.identity,
        Quaternion.Euler(0f, 0f, -90f), Quaternion.Euler(0f, 0f, 90f),
        Quaternion.Euler(90f, 0f, 0f), Quaternion.Euler(-90f, 0f, 0f)
    };

    ComputeBuffer[] matricesBuffers;
    ComputeBuffer argsBuffer;
    uint[] args = new uint[5] { 0, 0, 0, 0, 0 };

    static readonly int matricesId = Shader.PropertyToID("_Matrices");

    static MaterialPropertyBlock propertyBlock;

    // Change from Awake() to OnEnable() to work with hot reloads
    private void OnEnable()
    {
        parts = new NativeArray<FractalPart>[depth];
        matrices = new NativeArray<Matrix4x4>[depth];

        matricesBuffers = new ComputeBuffer[depth];

        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);

        // A 4×4 matrix has sixteen float values, so the stride of the buffers is 16 * 4 bytes
        int stride = 16 * 4;

        for (int i = 0, length = 1; i < parts.Length; i++, length *= 5)
        {
            parts[i] = new NativeArray<FractalPart>(length, Allocator.Persistent);
            matrices[i] = new NativeArray<Matrix4x4>(length, Allocator.Persistent);

            matricesBuffers[i] = new ComputeBuffer(length, stride);
        }

        parts[0][0] = CreatePart(0);

        for (int li = 1; li < parts.Length; li++)
        {
            NativeArray<FractalPart> levelParts = parts[li];
            for (int fpi = 0; fpi < levelParts.Length; fpi += 5)
            {
                for (int ci = 0; ci < 5; ci++)
                {
                   levelParts[fpi + ci] = CreatePart(ci);
                }
            }
        }

        // last buffer set is what is used by all draw commands,
        // so need to link each buffer to a specific draw command
        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }
    }

    private void OnDisable()
    {
        for (int i = 0; i < matricesBuffers.Length; i++)
        {
            matricesBuffers[i].Release();
            parts[i].Dispose();
            matrices[i].Dispose();
        }

        // these are created in OnEnable(), so we can get rid of them here as well
        parts = null;
        matrices = null;
        matricesBuffers = null;

        argsBuffer.Release();
        argsBuffer = null;
    }

    // invoked after a change has been made to the component via the inspector or an undo/redo action
    // this will ensure a "reset" of the program
    private void OnValidate()
    {
        // ensure we are in play mode and the fractal is active
        if (parts != null && enabled)
        {
            OnDisable();
            OnEnable();
        }
        
    }

    private void Update()
    {

        float spinAngleDelta = 22.5f * Time.deltaTime;

        FractalPart rootPart = parts[0][0];
        rootPart.spinAngle += spinAngleDelta;
        rootPart.worldRotation = transform.rotation *
            (rootPart.rotation * Quaternion.Euler(0f, rootPart.spinAngle, 0f));
        rootPart.worldPosition = transform.position;
        parts[0][0] = rootPart;

        float objectScale = transform.lossyScale.x;
        matrices[0][0] = Matrix4x4.TRS(
            rootPart.worldPosition,
            rootPart.worldRotation,
            objectScale * Vector3.one
        );

        float scale = objectScale;

        for (int li = 1; li < parts.Length; li++)
        {
            scale *= 0.5f;

            NativeArray<FractalPart> parentParts = parts[li - 1];
            NativeArray<FractalPart> levelParts = parts[li];
            NativeArray<Matrix4x4> levelMatrices = matrices[li];

            for (int fpi = 0; fpi < levelParts.Length; fpi++)
            {
                FractalPart parent = parentParts[fpi / 5];
                FractalPart part = levelParts[fpi];

                part.spinAngle += spinAngleDelta;

                part.worldRotation = parent.worldRotation *
                    (part.rotation * Quaternion.Euler(0f, part.spinAngle, 0f)); ;

                part.worldPosition =
                    parent.worldPosition +
                    parent.worldRotation * (1.5f * scale * part.direction);

                levelParts[fpi] = part;

                levelMatrices[fpi] = Matrix4x4.TRS(
                    part.worldPosition, part.worldRotation, scale * Vector3.one
                );
            }
        }

        var bounds = new Bounds(rootPart.worldPosition, 3f * objectScale * Vector3.one);

        // upload matrices to GPU
        for (int i = 0; i < matricesBuffers.Length; i++)
        {
            ComputeBuffer buffer = matricesBuffers[i];
            buffer.SetData(matrices[i]);

            // args are index count per instance, instance count, start index location, base vertex location, start instance location
            // Indirect args
            if (mesh != null)
            {
                args[0] = (uint)mesh.GetIndexCount(subMeshIndex);
                args[1] = (uint)buffer.count;
                args[2] = (uint)mesh.GetIndexStart(subMeshIndex);
                args[3] = (uint)mesh.GetBaseVertex(subMeshIndex);
            }
            else
            {
                args[0] = args[1] = args[2] = args[3] = 0;
            }
            argsBuffer.SetData(args);

            propertyBlock.SetBuffer(matricesId, buffer);
            Graphics.DrawMeshInstancedIndirect(
                mesh, subMeshIndex, material, bounds, argsBuffer, 0, propertyBlock
            );
        }
    }

    FractalPart CreatePart(int childIndex) 
    {
        return new FractalPart {
            direction = directions[childIndex],
            rotation = rotations[childIndex]
        };
    }
}
