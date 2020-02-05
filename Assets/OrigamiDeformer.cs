using UnityEngine;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using Deform;
public class OrigamiDeformer : Deformer,IFactor
{
    public float Angle
    {
        get => angle;
        set => angle = value;
    }
    public float Factor
    {
        get => factor;
        set => factor = value;
    }
    public Transform Axis
    {
        get
        {
            if (axis == null)
                axis = transform;
            return axis;
        }
        set => axis = value;
    }
    [SerializeField] private float angle;
    [SerializeField] private float factor = 0;
    [SerializeField, HideInInspector] private Transform axis;
    public override DataFlags DataFlags => DataFlags.Vertices;

    
    public override JobHandle Process(MeshData data, JobHandle dependency = default)
    {
        if (Mathf.Approximately(Factor, 0f))
            return dependency;
        var meshToAxis = DeformerUtils.GetMeshToAxisSpace(Axis, data.Target.GetTransform());
        return new OrigamiJob {
            angle = Factor * Angle,
            meshToAxis = meshToAxis,
            axisToMesh = meshToAxis.inverse,
            vertices = data.DynamicNative.VertexBuffer

        }.Schedule(data.Length, DEFAULT_BATCH_COUNT, dependency);
    }
    [BurstCompile(CompileSynchronously = COMPILE_SYNCHRONOUSLY)]
    public struct OrigamiJob : IJobParallelFor
    {
        public float angle;
        public float4x4 meshToAxis;
        public float4x4 axisToMesh;
        public NativeArray<float3> vertices;

        public void Execute(int index)
        {
            var point = mul(meshToAxis, float4(vertices[index], 1f));
            var rads = radians(angle) + (float)PI;
            if (point.x > 0) return;
            point.xy = float2
            (
                -point.x * cos(rads) - point.y * sin(rads),
                point.x * sin(rads) - point.y * cos(rads)
            );
            vertices[index] = mul(axisToMesh, point).xyz;
        }
    }

}
