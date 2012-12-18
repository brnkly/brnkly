using System;

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

        public static ContractParameter<T> IsNotNull<T>(
            this ContractParameter<T> param)
        {
            if (param.Value == null)
            {
                throw new ArgumentNullException(param.Name);
            }

            return param;
        }

        public static ContractParameter<string> IsNotNullOrWhiteSpace(
            this ContractParameter<string> param)
        {
            if (string.IsNullOrWhiteSpace(param.Value))
            {
                throw new ArgumentException("Parameter cannot be null or white space.", param.Name);
            }

            return param;
        }

        public static ContractParameter<string> MinLength(
            this ContractParameter<string> param,
            int minLength)
        {
            if (param.Value.Length < minLength)
            {
                throw new ArgumentException(
                    string.Format("Parameter must be at least {0} characters long.", minLength),
                    param.Name);
            }

            return param;
        }

        public static ContractParameter<string> MaxLength(
            this ContractParameter<string> param,
            int maxLength)
        {
            if (maxLength < param.Value.Length)
            {
                throw new ArgumentException(
                    string.Format("Parameter must be no more than {0} characters long.", maxLength),
                    param.Name);
            }

            return param;
        }

        public static ContractParameter<string> StartsWith(
            this ContractParameter<string> param,
            string prefix,
            StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            if (!param.Value.StartsWith(prefix, comparison))
            {
                throw new ArgumentException(
                    string.Format("Parameter must start with '{0}'.", prefix),
                    param.Name);
            }

            return param;
        }
    }
}
