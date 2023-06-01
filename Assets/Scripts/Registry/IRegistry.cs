using System.Collections.Generic;
using UnityEngine;
// Registry interface
public interface IRegistry {
    void Load();
    void Destroy();
}

// Composite Registry
public class CompositeRegistry : IRegistry {
    private List<IRegistry> registries = new List<IRegistry>();

    public void AddRegistry(IRegistry registry) {
        registries.Add(registry);
    }

    public void RemoveRegistry(IRegistry registry) {
        registries.Remove(registry);
    }

    public void Load() {
        foreach (var registry in registries) {
            registry.Load();
        }
    }

    public void Destroy() {
        foreach (var registry in registries) {
            registry.Destroy();
        }
    }

    public void Initialize() {

	}
}