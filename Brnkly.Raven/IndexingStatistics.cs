using System.Collections.ObjectModel;

namespace Brnkly.Raven
{
    public class IndexingStatistics
    {
        public string Name { get; set; }
        public Collection<InstanceIndexStats> Instances { get; set; }

        public IndexingStatistics()
        {
            this.Instances = new Collection<InstanceIndexStats>();
        }
    }
}
