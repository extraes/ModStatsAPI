
using Microsoft.AspNetCore.Mvc;

namespace ModStats.Controllers
{
    [ApiController]
    [Route("ModStats/Categories/{categoryName}/Entries/{entryName}/[action]")]
    public class ModStatsEntryController : ControllerBase
    {
        private readonly ILogger<ModStatsAdminController> _logger;

        public ModStatsEntryController(ILogger<ModStatsAdminController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        [ActionName("Increment")]
        public long Increment(string categoryName, string entryName)
        {
            ArgumentException.ThrowIfNullOrEmpty(categoryName);
            ArgumentException.ThrowIfNullOrEmpty(entryName);

            var category = DataStore.GetCategory(categoryName);
            long newVal = category[entryName]++;

            DataStore.CollectionModified();
            return newVal;
        }

        [HttpPost]
        [ActionName("Set")]
        public void Set(string categoryName, string entryName, long value)
        {
            ArgumentException.ThrowIfNullOrEmpty(categoryName);
            ArgumentException.ThrowIfNullOrEmpty(entryName);

            var category = DataStore.GetCategory(categoryName);
            category[entryName] = value;

            DataStore.CollectionModified();
        }

        [HttpGet]
        [ActionName("Get")]
        public long Get(string categoryName, string entryName)
        {
            ArgumentException.ThrowIfNullOrEmpty(categoryName);
            ArgumentException.ThrowIfNullOrEmpty(entryName);

            var category = DataStore.GetCategory(categoryName);

            return category[entryName];
        }
    }
}