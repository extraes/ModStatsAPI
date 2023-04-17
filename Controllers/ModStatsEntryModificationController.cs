using Microsoft.AspNetCore.Mvc;

//todo: change to using ActionResult so i can return exceptions instead of throwing them, causing a 500 err to be sent back.

namespace ModStats.Controllers
{
    [ApiController]
    [Route("ModStats/Categories/{categoryName}/EntryModification/[action]")]
    public class ModStatsEntryModificationController : ControllerBase
    {
        private readonly ILogger<ModStatsAdminController> _logger;

        public ModStatsEntryModificationController(ILogger<ModStatsAdminController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        [ActionName("Create")]
        public void Create(string categoryName, string pass, string entryName)
        {
            ArgumentException.ThrowIfNullOrEmpty(categoryName);
            ArgumentException.ThrowIfNullOrEmpty(entryName);
            ArgumentException.ThrowIfNullOrEmpty(pass);
            if (!DataStore.PassMatchesCategory(categoryName, pass)) throw new UnauthorizedAccessException("Unauthorized - pass not matched");

            var category = DataStore.GetCategory(categoryName);
            category[entryName] = 0;

            DataStore.CollectionModified();
        }

        [HttpDelete]
        [ActionName("Delete")]
        public void Delete(string categoryName, string pass, string entryName)
        {
            ArgumentException.ThrowIfNullOrEmpty(categoryName);
            ArgumentException.ThrowIfNullOrEmpty(entryName);
            ArgumentException.ThrowIfNullOrEmpty(pass);
            if (!DataStore.PassMatchesCategory(categoryName, pass)) throw new UnauthorizedAccessException("Unauthorized - pass not matched");

            var category = DataStore.GetCategory(categoryName);
            category.Remove(entryName);

            DataStore.CollectionModified();
        }
    }
}