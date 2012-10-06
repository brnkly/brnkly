
namespace Brnkly.Framework.Administration.Models
{
    public class SettingEditModel<T>
    {
        public string ApplicationName { get; set; }
        public string MachineName { get; set; }
        public T Value { get; set; }

        internal bool IsEmpty()
        {
            return
                string.IsNullOrWhiteSpace(this.ApplicationName) &&
                string.IsNullOrWhiteSpace(this.MachineName);
        }
    }
}
