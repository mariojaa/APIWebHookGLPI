using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace APIChatAgent.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WebhookController : ControllerBase
    {
        /// <summary>
        /// Salva os dados do WebHook GLPI em txt
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> ReceiveWebhook()
        {
            try
            {
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                string body = await reader.ReadToEndAsync();

                string filePath = Path.Combine(Directory.GetCurrentDirectory(), "webhook_data.txt");

                await System.IO.File.AppendAllTextAsync(filePath, body + "\n");

                return Ok(new { message = "Dados recebidos e salvos com sucesso!" });
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { message = "Erro ao processar o webhook", error = ex.Message });
            }
        }
    }
}
