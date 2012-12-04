using System;

namespace Brnkly.Raven
{
    public class Destination
    {
        public Uri Uri { get; private set; }
        public bool Replicate { get; private set; }
        public bool IsTransitive { get; private set; }

        public Destination(Uri uri, bool replicate, bool isTransitive)
        {
            this.Uri = uri;
            this.Replicate = replicate;
            this.IsTransitive = isTransitive;
        }
    }
}
