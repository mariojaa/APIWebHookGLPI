using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using System.Runtime.Versioning;

namespace APIChatAgent.Controllers
{
    [Route("v1/status")]
    [ApiController]
    public class StatusController : Controller
    {
        private readonly IHostEnvironment _env;
        public StatusController(IHostEnvironment env)
        {
            _env = env;
        }

        /// <summary>
        /// Retorna informações sobre o status e o ambiente do servidor.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult Index()
        {
            var executionInfo = new
            {
                StatusCode = 200,
                FrameworkVersion = Assembly.GetEntryAssembly()?.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName,
                Environment = _env.EnvironmentName,
                MachineName = Environment.MachineName,
                OSVersion = Environment.OSVersion.VersionString,
                CLRVersion = Environment.Version.ToString(),
                CurrentTime = DateTime.Now.ToString("dd-MM-yyy HH:mm:ss")
            };

            return Ok(executionInfo);
        }
    }
}
