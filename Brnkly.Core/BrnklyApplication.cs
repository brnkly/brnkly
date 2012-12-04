using System;

namespace Brnkly
{
    public class BrnklyApplication
    {
        internal static object GlobalContainer;

        public static void RegisterContainer(object container)
        {
            container.Ensure("container").IsNotNull();

            if (GlobalContainer != null)
            {
                throw new InvalidOperationException("A container has already been registered");
            }

            GlobalContainer = container;
        }
    }
}
