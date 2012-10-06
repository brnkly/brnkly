using System;

namespace Brnkly.Framework.Administration.Models
{
    public class TrackedChange<T> where T : IComparable
    {
        public PendingChangeType PendingChange { get; set; }
        public T Value { get; set; }

        public TrackedChange()
            : this(default(T))
        {
        }

        public TrackedChange(T value)
        {
            this.Value = value;
        }

        public void MarkPendingChange(T compareTo)
        {
            if ((this.Value == null) != (compareTo == null) ||
                this.Value.CompareTo(compareTo) != 0)
            {
                this.PendingChange = PendingChangeType.Changed;
            }
        }
    }
}
