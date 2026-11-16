using System;
using System.Collections.Generic;
using Game.Runtime;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

internal static class S004TransformProjectionReadback
{
    static JArray Vector(float4 v) => new JArray(v.x, v.y, v.z, v.w);
    static JArray Matrix(float4x4 m) => new JArray(Vector(m.c0), Vector(m.c1), Vector(m.c2), Vector(m.c3));
    internal static JObject Read(EntityManager em)
    {
        var nodes = new Dictionary<Entity, LocalTransform>();
        var parents = new Dictionary<Entity, Entity>();
        var post = new Dictionary<Entity, float4x4>();
        using (var query = em.CreateEntityQuery(typeof(LocalTransform)))
        using (var entities = query.ToEntityArray(Allocator.Temp))
        foreach (var entity in entities)
        {
            nodes.Add(entity, em.GetComponentData<LocalTransform>(entity));
            if (em.HasComponent<Parent>(entity)) parents.Add(entity, em.GetComponentData<Parent>(entity).Value);
            if (em.HasComponent<PostTransformMatrix>(entity)) post.Add(entity, em.GetComponentData<PostTransformMatrix>(entity).Value);
        }
        uint before = em.EntityOrderVersion;
        var matrices = S004ExistingTransformProjection.ComputeMatrices(nodes, parents, post);
        if (before != em.EntityOrderVersion || matrices.Count != nodes.Count)
            throw new InvalidOperationException("Read-only actual graph projection changed entity structure");
        int compared = 0, different = 0, missing = 0;
        var samples = new JArray();
        foreach (var row in matrices)
        {
            if (!em.HasComponent<LocalToWorld>(row.Key)) { missing++; continue; }
            compared++;
            var actual = em.GetComponentData<LocalToWorld>(row.Key).Value;
            if (!row.Value.Equals(actual))
            {
                different++;
                if (samples.Count < 3) samples.Add(new JObject { ["entity"] = row.Key.ToString(),
                    ["parent"] = parents.TryGetValue(row.Key, out var parent) ? parent.ToString() : null,
                    ["local_position"] = new JArray(nodes[row.Key].Position.x, nodes[row.Key].Position.y, nodes[row.Key].Position.z),
                    ["computed"] = Matrix(row.Value), ["existing"] = Matrix(actual) });
            }
        }
        return new JObject { ["scope"] = "ActualS004Graph_ExistingBridgePureMatrixProjection_ReadOnly",
            ["nodes"] = nodes.Count, ["parent_edges"] = parents.Count, ["post_transform_matrices"] = post.Count,
            ["computed_matrices"] = matrices.Count, ["existing_local_to_world_compared"] = compared,
            ["exact_matrix_differences"] = different, ["missing_local_to_world"] = missing,
            ["difference_samples"] = samples,
            ["bridge_step_executed"] = false, ["native_parity"] = false, ["mission_ready"] = false };
    }
}
