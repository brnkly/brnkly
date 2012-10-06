using System.Web.Mvc;

namespace Brnkly.Framework.Administration.Controllers
{
    public class ApplicationsController : EnvironmentConfigController
    {
        [HttpGet]
        public ActionResult Index()
        {
            var model = this.GetPendingModel();
            this.AddApplicationsAndMachinesToViewBag(getPending: true);
            return this.View(model);
        }

        [HttpPost]
        public ActionResult AddLogicalInstance(string applicationName, string logicalInstanceName)
        {
            if (!string.IsNullOrWhiteSpace(applicationName) &&
                !string.IsNullOrWhiteSpace(logicalInstanceName))
            {
                var model = this.GetPendingModel()
                    .AddLogicalInstance(applicationName, logicalInstanceName);
                this.SavePendingChanges(model);
            }

            return this.RedirectToAction("index");
        }

        [HttpGet]
        public ActionResult DeleteLogicalInstance(string applicationName, string logicalInstanceName)
        {
            if (!string.IsNullOrWhiteSpace(applicationName) &&
                !string.IsNullOrWhiteSpace(logicalInstanceName))
            {
                var model = this.GetPendingModel()
                    .DeleteLogicalInstance(applicationName, logicalInstanceName);
                this.SavePendingChanges(model);
            }

            return this.RedirectToAction("index");
        }

        [HttpPost]
        public ActionResult AddMachine(
            string applicationName,
            string logicalInstanceName,
            string machineName)
        {
            if (!string.IsNullOrWhiteSpace(applicationName) &&
                !string.IsNullOrWhiteSpace(logicalInstanceName) &&
                !string.IsNullOrWhiteSpace(machineName))
            {
                var model = this.GetPendingModel()
                    .AddMachineToLogicalInstance(applicationName, logicalInstanceName, machineName);
                this.SavePendingChanges(model);
            }

            return this.RedirectToAction("index");
        }

        [HttpGet]
        public ActionResult DeleteMachine(
            string applicationName,
            string logicalInstanceName,
            string machineName)
        {
            if (!string.IsNullOrWhiteSpace(applicationName) &&
                !string.IsNullOrWhiteSpace(logicalInstanceName) &&
                !string.IsNullOrWhiteSpace(machineName))
            {
                var model = this.GetPendingModel()
                    .DeleteMachineFromLogicalInstance(applicationName, logicalInstanceName, machineName);
                this.SavePendingChanges(model);
            }

            return this.RedirectToAction("index");
        }
    }
}
