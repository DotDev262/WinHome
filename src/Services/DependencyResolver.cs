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
    /// <param name="globalIds">
    /// Optional set of all resource IDs declared across ALL collections in
    /// the config. When provided, <c>dependsOn</c> entries are validated
    /// against this global set so cross-type references (e.g. a service
    /// depending on an app) are recognised as valid rather than causing a
    /// missing-ID error. Dependencies on IDs outside the local collection
    /// are ignored during local sorting because the engine's fixed pipeline
    /// order already guarantees they run first.
    /// </param>
    /// <returns>A new list in dependency order.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a duplicate <c>resourceId</c> is found, when a
    /// <c>dependsOn</c> entry references an ID that exists in neither the
    /// local collection nor <paramref name="globalIds"/>, or when a
    /// circular dependency is detected within the local collection.
    /// </exception>
    public static List<T> Sort<T>(List<T> resources, HashSet<string>? globalIds = null)
        where T : ResourceBase
    {
      // Split into participants (have a ResourceId) and pass-through (no ResourceId)
      var participants = resources.Where(r => r.ResourceId is not null).ToList();
      var passThrough = resources.Where(r => r.ResourceId is null).ToList();

      // Nothing to sort — fast path (also handles backward-compat configs)
      if (participants.Count == 0)
        return resources;

      // Build id → resource map for the LOCAL collection, catching duplicates
      var idMap = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
      foreach (var resource in participants)
      {
        if (!idMap.TryAdd(resource.ResourceId!, resource))
          throw new InvalidOperationException(
              $"[DependencyResolver] Duplicate resourceId '{resource.ResourceId}'. " +
              $"Each resourceId must be unique across the entire config.");
      }

      // Validate all dependsOn entries:
      // - Must exist in the local idMap OR in the global set (cross-type reference)
      // - If neither, it is a typo/missing id — throw a clear error
      foreach (var resource in participants)
      {
        if (resource.DependsOn is null) continue;
        foreach (var dep in resource.DependsOn)
        {
          var existsLocally = idMap.ContainsKey(dep);
          var existsGlobally = globalIds?.Contains(dep) ?? false;
          if (!existsLocally && !existsGlobally)
            throw new InvalidOperationException(
                $"[DependencyResolver] Resource '{resource.ResourceId}' declares " +
                $"dependsOn: '{dep}', but no resource with that resourceId exists " +
                $"in the configuration.");
        }
      }

      // ── Kahn's algorithm ─────────────────────────────────────────────
      // Only add graph edges for dependencies that exist in the LOCAL
      // collection. Cross-type deps (in globalIds but not idMap) are
      // already satisfied by the engine's fixed pipeline execution order.
      var inDegree = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
      var adjacency = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

      foreach (var id in idMap.Keys)
      {
        inDegree[id] = 0;
        adjacency[id] = new List<string>();
      }

      foreach (var resource in participants)
      {
        if (resource.DependsOn is null) continue;
        foreach (var dep in resource.DependsOn)
        {
          // Only wire local edges — cross-type deps are skipped here
          if (!idMap.ContainsKey(dep)) continue;

          adjacency[dep].Add(resource.ResourceId!);
          inDegree[resource.ResourceId!]++;
        }
      }

      // Start with every node that has no unresolved local dependencies
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
