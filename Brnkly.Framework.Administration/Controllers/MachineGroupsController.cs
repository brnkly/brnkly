using System.Web.Mvc;

namespace Brnkly.Framework.Administration.Controllers
{
    public class MachineGroupsController : EnvironmentConfigController
    {
        [HttpGet]
        public ActionResult Index()
        {
            var model = this.GetPendingModel();
            return this.View(model);
        }

        [HttpPost]
        public ActionResult AddGroup(string groupName)
        {
            if (!string.IsNullOrWhiteSpace(groupName))
            {
                var model = this.GetPendingModel()
                    .AddMachineGroup(groupName);
                this.SavePendingChanges(model);
            }

            return this.RedirectToAction("index");
        }

        [HttpGet]
        public ActionResult DeleteGroup(string groupName)
        {
            if (!string.IsNullOrWhiteSpace(groupName))
            {
                var model = this.GetPendingModel()
                    .DeleteMachineGroup(groupName);
                this.SavePendingChanges(model);
            }

            return this.RedirectToAction("index");
        }

        [HttpPost]
        public ActionResult AddMachine(string groupName, string machineName)
        {
            if (!string.IsNullOrWhiteSpace(groupName) &&
                !string.IsNullOrWhiteSpace(machineName))
            {
                var model = this.GetPendingModel()
                    .AddMachineToGroup(groupName, machineName);
                this.SavePendingChanges(model);
            }

            return this.RedirectToAction("index");
        }

        [HttpGet]
        public ActionResult DeleteMachine(string groupName, string machineName)
        {
            if (!string.IsNullOrWhiteSpace(groupName) &&
                !string.IsNullOrWhiteSpace(machineName))
            {
                var model = this.GetPendingModel()
                    .DeleteMachineFromGroup(groupName, machineName);
                this.SavePendingChanges(model);
            }

            return this.RedirectToAction("index");
        }
    }
}
