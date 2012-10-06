using System.Diagnostics;

namespace Brnkly.Framework.Instrumentation
{
    public class SingleInstanceCounter
    {
        private string categoryName;
        private PerformanceCounter instance;

        public CounterCreationData Data { get; private set; }

        public SingleInstanceCounter(string categoryName, CounterCreationData data)
        {
            this.categoryName = categoryName;
            this.Data = data;
        }

        public PerformanceCounter GetInstance()
        {
            if (this.instance == null)
            {
                this.instance = new PerformanceCounter(categoryName, this.Data.CounterName, false);
            }

            return this.instance;
        }
    }
}
