namespace ServiceDiscovery.Domain.Entities;

/// <summary>
/// Represents a catalog of all registered services
/// </summary>
public class ServiceCatalog
{
    /// <summary>
    /// Dictionary of service name to list of instances
    /// </summary>
    private readonly Dictionary<string, List<ServiceInstance>> _services = new();

    /// <summary>
    /// Gets all services in the catalog
    /// </summary>
    public IReadOnlyDictionary<string, List<ServiceInstance>> Services => _services;

    /// <summary>
    /// Registers a new service instance
    /// </summary>
    public void RegisterService(ServiceInstance instance)
    {
        if (!instance.IsValid())
        {
            throw new ArgumentException("Invalid service instance", nameof(instance));
        }

        if (!_services.ContainsKey(instance.ServiceName))
        {
            _services[instance.ServiceName] = new List<ServiceInstance>();
        }

        // Remove existing instance with same ID if it exists
        _services[instance.ServiceName].RemoveAll(s => s.Id == instance.Id);

        // Add the new instance
        _services[instance.ServiceName].Add(instance);
    }

    /// <summary>
    /// Deregisters a service instance by ID
    /// </summary>
    public bool DeregisterService(string instanceId)
    {
        foreach (var serviceList in _services.Values)
        {
            var removed = serviceList.RemoveAll(s => s.Id == instanceId);
            if (removed > 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets all instances of a specific service
    /// </summary>
    public List<ServiceInstance> GetServiceInstances(string serviceName)
    {
        return _services.TryGetValue(serviceName, out var instances)
            ? instances.ToList()
            : new List<ServiceInstance>();
    }

    /// <summary>
    /// Gets only healthy instances of a service
    /// </summary>
    public List<ServiceInstance> GetHealthyInstances(string serviceName)
    {
        return GetServiceInstances(serviceName)
            .Where(s => s.IsHealthy())
            .ToList();
    }

    /// <summary>
    /// Gets a specific instance by ID
    /// </summary>
    public ServiceInstance? GetInstance(string instanceId)
    {
        foreach (var serviceList in _services.Values)
        {
            var instance = serviceList.FirstOrDefault(s => s.Id == instanceId);
            if (instance != null)
            {
                return instance;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets all service names
    /// </summary>
    public List<string> GetServiceNames()
    {
        return _services.Keys.ToList();
    }

    /// <summary>
    /// Gets total count of all service instances
    /// </summary>
    public int GetTotalInstanceCount()
    {
        return _services.Values.Sum(list => list.Count);
    }

    /// <summary>
    /// Gets count of healthy instances across all services
    /// </summary>
    public int GetHealthyInstanceCount()
    {
        return _services.Values.Sum(list => list.Count(s => s.IsHealthy()));
    }
}
