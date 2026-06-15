using System;
using System.Collections.Generic;
using System.Linq;
using WinHome.Models;

namespace WinHome.Services
{
    /// <summary>
    /// Topologically sorts a list of <see cref="ResourceBase"/> items using
    /// Kahn's algorithm, respecting <see cref="ResourceBase.DependsOn"/> edges.
    /// Resources without a <see cref="ResourceBase.ResourceId"/> are fully
    /// backward-compatible and are appended after the sorted group unchanged.
    /// </summary>
    public static class DependencyResolver
    {
        /// <summary>
        /// Returns <paramref name="resources"/> sorted so that every resource
        /// appears after all resources it declares in
        /// <see cref="ResourceBase.DependsOn"/>.
        /// </summary>
        /// <typeparam name="T">Any type that extends <see cref="ResourceBase"/>.</typeparam>
        /// <param name="resources">The flat list to sort.</param>
        /// <returns>A new list in dependency order.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when a duplicate <c>resourceId</c> is found, when a
        /// <c>dependsOn</c> entry references a non-existent id, or when a
        /// circular dependency is detected.
        /// </exception>
        public static List<T> Sort<T>(List<T> resources) where T : ResourceBase
        {
            // Split into participants (have a ResourceId) and pass-through (no ResourceId)
            var participants = resources.Where(r => r.ResourceId is not null).ToList();
            var passThrough  = resources.Where(r => r.ResourceId is null).ToList();

            // Nothing to sort — fast path
            if (participants.Count == 0)
                return resources;

            // Build id → resource map, catching duplicates
            var idMap = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
            foreach (var resource in participants)
            {
                if (!idMap.TryAdd(resource.ResourceId!, resource))
                    throw new InvalidOperationException(
                        $"[DependencyResolver] Duplicate resourceId '{resource.ResourceId}'. " +
                        $"Each resourceId must be unique across the entire config.");
            }

            // Validate all dependsOn entries reference real ids
            foreach (var resource in participants)
            {
                if (resource.DependsOn is null) continue;
                foreach (var dep in resource.DependsOn)
                {
                    if (!idMap.ContainsKey(dep))
                        throw new InvalidOperationException(
                            $"[DependencyResolver] Resource '{resource.ResourceId}' declares " +
                            $"dependsOn: '{dep}', but no resource with that resourceId exists.");
                }
            }

            // ── Kahn's algorithm ─────────────────────────────────────────────
            // inDegree[id]  = number of unresolved dependencies for that node
            // adjacency[id] = list of ids that depend ON id (outgoing edges)
            var inDegree  = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var adjacency = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var id in idMap.Keys)
            {
                inDegree[id]  = 0;
                adjacency[id] = new List<string>();
            }

            foreach (var resource in participants)
            {
                if (resource.DependsOn is null) continue;
                foreach (var dep in resource.DependsOn)
                {
                    // dep must finish before resource → dep → resource edge
                    adjacency[dep].Add(resource.ResourceId!);
                    inDegree[resource.ResourceId!]++;
                }
            }

            // Start with every node that has no unresolved dependencies
            var queue = new Queue<string>(
                inDegree.Where(kv => kv.Value == 0).Select(kv => kv.Key));

            var sorted = new List<T>();

            while (queue.Count > 0)
            {
                var currentId = queue.Dequeue();
                sorted.Add(idMap[currentId]);

                foreach (var neighborId in adjacency[currentId])
                {
                    inDegree[neighborId]--;
                    if (inDegree[neighborId] == 0)
                        queue.Enqueue(neighborId);
                }
            }

            // If not all participants were sorted, a cycle exists
            if (sorted.Count != participants.Count)
            {
                var cycleIds = inDegree
                    .Where(kv => kv.Value > 0)
                    .Select(kv => kv.Key);
                throw new InvalidOperationException(
                    $"[DependencyResolver] Circular dependency detected among resources: " +
                    $"{string.Join(", ", cycleIds)}. " +
                    $"Check the 'dependsOn' fields for these resourceIds.");
            }

            // Append pass-through resources at the end (backward-compatible)
            sorted.AddRange(passThrough);
            return sorted;
        }
    }
}