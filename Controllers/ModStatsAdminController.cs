using Microsoft.AspNetCore.Mvc;

namespace ModStats.Controllers
{
    [ApiController]
    [Route("ModStats/Admin/{name}/[action]")]
    public class ModStatsAdminController : ControllerBase
    {
        private readonly ILogger<ModStatsAdminController> _logger;

        public ModStatsAdminController(ILogger<ModStatsAdminController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        [ActionName("Get")]
        public Dictionary<string, long> Get(string name, string pass)
        {
            if (!DataStore.PassMatchesCategory(name, pass)) throw new UnauthorizedAccessException("Unauthorized - pass unmatch");
            
            return DataStore.GetCategory(name);
        }

        [HttpPost]
        [ActionName("Create")]
        public void Create(string name, string pass, long bless)
        {
            DataStore.CreateCategory(name, pass, bless);
        }

        [HttpPost]
        [ActionName("CloudSave")]
        public void SaveToCloud(long bless)
        {
            DataStore.SaveToCloud(bless);
        }
    }
}