using System.Collections.Concurrent;
using System.Diagnostics;

namespace Brnkly.Framework.Instrumentation
{
    public class MultiInstanceCounter
    {
        private string categoryName;
        private ConcurrentDictionary<string, PerformanceCounter> instances;

        public CounterCreationData Data { get; private set; }

        public MultiInstanceCounter(string categoryName, CounterCreationData data)
        {
            this.categoryName = categoryName;
            this.Data = data;
            this.instances = new ConcurrentDictionary<string, PerformanceCounter>();
        }

        public PerformanceCounter GetInstance(string instanceName)
        {
            return this.instances.GetOrAdd(
                instanceName,
                name => new PerformanceCounter(this.categoryName, this.Data.CounterName, name, false));
        }
    }
}
