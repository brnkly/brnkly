using System;

namespace Brnkly.Framework
{
    public class RefreshedEventArgs<T> : EventArgs
    {
        public T Value { get; private set; }

        public RefreshedEventArgs(T value)
        {
            this.Value = value;
        }
    }
}
