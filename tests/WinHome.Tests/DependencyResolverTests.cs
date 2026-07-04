using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using WinHome.Models;
using WinHome.Services;
using Xunit;
using YamlDotNet.Serialization;

namespace WinHome.Tests
{
  public class DependencyResolverTests
  {
    // Minimal concrete ResourceBase for testing — no production coupling
    private class TestResource : ResourceBase
    {
      public string Name { get; set; } = string.Empty;
    }

    private static TestResource R(string name, string? resourceId = null, params string[] dependsOn)
        => new TestResource
        {
          Name = name,
          ResourceId = resourceId,
          DependsOn = dependsOn.Length > 0 ? new List<string>(dependsOn) : null
        };

    // ── Happy-path tests ─────────────────────────────────────────────────

    [Fact]
    public void Sort_ResourcesWithoutIds_ReturnUnchanged()
    {
      var resources = new List<TestResource>
            {
                R("legacy-1"),
                R("legacy-2"),
                R("legacy-3"),
            };

      var result = DependencyResolver.Sort(resources);

      Assert.Equal(3, result.Count);
      Assert.Equal("legacy-1", result[0].Name);
      Assert.Equal("legacy-2", result[1].Name);
      Assert.Equal("legacy-3", result[2].Name);
    }

    [Fact]
    public void Sort_SingleNode_NoDeps_ReturnsSameOrder()
    {
      var resources = new List<TestResource> { R("only", "a") };

      var result = DependencyResolver.Sort(resources);

      Assert.Single(result);
      Assert.Equal("a", result[0].ResourceId);
    }

    [Fact]
    public void Sort_TwoNodes_CorrectOrder_WhenDeclaredReversed()
    {
      // configure-git is declared first but depends on install-git
      var resources = new List<TestResource>
            {
                R("configure-git", "configure-git", "install-git"),
                R("install-git",   "install-git"),
            };

      var result = DependencyResolver.Sort(resources);

      Assert.Equal("install-git", result[0].ResourceId);
      Assert.Equal("configure-git", result[1].ResourceId);
    }

    [Fact]
    public void Sort_LinearChain_ABC_SortsCorrectly()
    {
      var resources = new List<TestResource>
            {
                R("C", "c", "b"),
                R("A", "a"),
                R("B", "b", "a"),
            };

      var result = DependencyResolver.Sort(resources);

      Assert.Equal("a", result[0].ResourceId);
      Assert.Equal("b", result[1].ResourceId);
      Assert.Equal("c", result[2].ResourceId);
    }

    [Fact]
    public void Sort_DiamondDependency_ResolvedCorrectly()
    {
      // A → B, A → C, B → D, C → D
      var resources = new List<TestResource>
            {
                R("D", "d", "b", "c"),
                R("B", "b", "a"),
                R("C", "c", "a"),
                R("A", "a"),
            };

      var result = DependencyResolver.Sort(resources);

      // A must be first, D must be last
      Assert.Equal("a", result[0].ResourceId);
      Assert.Equal("d", result[3].ResourceId);
      // B and C appear between A and D (order between them is not mandated)
      var middle = new[] { result[1].ResourceId, result[2].ResourceId };
      Assert.Contains("b", middle);
      Assert.Contains("c", middle);
    }

    [Fact]
    public void Sort_MixedResourcesWithAndWithoutIds_PassThroughAppendedAtEnd()
    {
      var resources = new List<TestResource>
            {
                R("no-id-1"),
                R("B", "b", "a"),
                R("no-id-2"),
                R("A", "a"),
            };

      var result = DependencyResolver.Sort(resources);

      // Sorted participants first
      Assert.Equal("a", result[0].ResourceId);
      Assert.Equal("b", result[1].ResourceId);
      // Pass-through at end, original order preserved
      Assert.Null(result[2].ResourceId);
      Assert.Equal("no-id-1", result[2].Name);
      Assert.Null(result[3].ResourceId);
      Assert.Equal("no-id-2", result[3].Name);
    }

    [Fact]
    public void Sort_EmptyList_ReturnsEmpty()
    {
      var result = DependencyResolver.Sort(new List<TestResource>());
      Assert.Empty(result);
    }

    // ── Error-path tests ─────────────────────────────────────────────────

    [Fact]
    public void Sort_DirectCycle_ThrowsWithClearMessage()
    {
      var resources = new List<TestResource>
            {
                R("A", "a", "b"),
                R("B", "b", "a"),
            };

      var ex = Assert.Throws<InvalidOperationException>(
          () => DependencyResolver.Sort(resources));

      Assert.Contains("Circular dependency", ex.Message);
      Assert.Contains("a", ex.Message);
      Assert.Contains("b", ex.Message);
    }

    [Fact]
    public void Sort_IndirectCycle_ThrowsWithClearMessage()
    {
      // A → B → C → A
      var resources = new List<TestResource>
            {
                R("A", "a", "c"),
                R("B", "b", "a"),
                R("C", "c", "b"),
            };

      var ex = Assert.Throws<InvalidOperationException>(
          () => DependencyResolver.Sort(resources));

      Assert.Contains("Circular dependency", ex.Message);
    }

    [Fact]
    public void Sort_MissingDependencyId_ThrowsWithOffendingIds()
    {
      var resources = new List<TestResource>
            {
                R("A", "a", "nonexistent"),
            };

      var ex = Assert.Throws<InvalidOperationException>(
          () => DependencyResolver.Sort(resources));

      Assert.Contains("nonexistent", ex.Message);
      Assert.Contains("a", ex.Message);
    }

    [Fact]
    public void Sort_DuplicateResourceId_Throws()
    {
      var resources = new List<TestResource>
            {
                R("First",  "duplicate-id"),
                R("Second", "duplicate-id"),
            };

      var ex = Assert.Throws<InvalidOperationException>(
          () => DependencyResolver.Sort(resources));

      Assert.Contains("duplicate-id", ex.Message);
      Assert.Contains("Duplicate", ex.Message);
    }

    [Fact]
    public void Sort_SelfReference_ThrowsCycleError()
    {
      var resources = new List<TestResource>
            {
                R("A", "a", "a"),  // depends on itself
            };

      var ex = Assert.Throws<InvalidOperationException>(
          () => DependencyResolver.Sort(resources));

      Assert.Contains("Circular dependency", ex.Message);
    }
  }
}
