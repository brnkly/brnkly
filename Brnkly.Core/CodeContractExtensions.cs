using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Brnkly
{
    public struct ContractParameter<T>
    {
        public string Name { get; internal set; }
        public T Value { get; internal set; }
    }

    public static class CodeContractExtensions
    {
        public static ContractParameter<T> Ensure<T>(this T obj, string paramName)
        {
            return new ContractParameter<T> { Name = paramName, Value = obj };
        }

        public static ContractParameter<T> IsNotNull<T>(this ContractParameter<T> param)
        {
            if (param.Value == null)
            {
                throw new ArgumentNullException(param.Name);
            }

            return param;
        }

        public static ContractParameter<string> IsNotNullOrWhiteSpace(this ContractParameter<string> param)
        {
            if (string.IsNullOrWhiteSpace(param.Value))
            {
                throw new ArgumentException("Parameter cannot be null or white space.", param.Name);
            }

            return param;
        }
    }
}
