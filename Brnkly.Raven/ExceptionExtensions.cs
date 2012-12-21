using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace Brnkly.Raven
{
    public static class ExceptionExtensions
    {
        // Copied from http://vasters.com/clemensv/2012/09/06/Are+You+Catching+Falling+Knives.aspx
        public static bool IsFatal(this Exception exception)
        {
            while (exception != null)
            {
                if (exception as OutOfMemoryException != null && exception as InsufficientMemoryException == null ||
                    exception as ThreadAbortException != null ||
                    exception as AccessViolationException != null ||
                    exception as SEHException != null ||
                    exception as StackOverflowException != null)
                {
                    return true;
                }
                else
                {
                    if (exception as TypeInitializationException == null &&
                        exception as TargetInvocationException == null)
                    {
                        break;
                    }

                    exception = exception.InnerException;
                }
            }

            return false;
        }
    }
}
