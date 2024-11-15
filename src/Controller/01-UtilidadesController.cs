using Microsoft.AspNetCore.Mvc;
using Data.AppDbContext;
using ConsultaCnpjReceita.Service;
using System.Linq;
using System.Data;

namespace WebApi.Controllers
{
    /// <summary>
    /// Controller responsável por disponibilizar ações de utilidades
    /// </summary>
    [Route("[controller]")]
    [ApiController]
    public class UtilidadesController : ControllerBase
    {

        private readonly AppDbContext _context;
        private readonly IServiceProvider _serviceProvider;
        private readonly UtilidadesService _utilidadesService;

        public UtilidadesController(AppDbContext context, IServiceProvider serviceProvider)
        {
            _context = context;
            _serviceProvider = serviceProvider;
            _utilidadesService = new UtilidadesService(_context, _serviceProvider);
        }

        /// <summary>
        /// Consulta a lista de CNPJs armazenados no banco de dados.
        /// </summary>
        /// <returns></returns>
        [HttpPost("ConsultarListaCnpj")]
        [ProducesResponseType(202), ProducesResponseType(400)]
        public IActionResult ConsultarListaCnpj()
        {
            Task.Run(async () =>
            {
                // Execute a lógica longa aqui
                await _utilidadesService.IniciarConsultarListaCnpj();
            });

            // Retorne 202 Accepted imediatamente sem caracteres especiais
            return Accepted(new { message = "Request accepted and is being processed in the background." });
        }

        /// <summary>
        /// Recupera os XMLs de todos os fundos cadastrados.
        /// </summary>
        /// <returns></returns>
        [HttpPost("RecuperarXmlAnbima")]
        [ProducesResponseType(202), ProducesResponseType(400)]
        public IActionResult RecuperarXmlAnbima()
        {
            Task.Run(async () =>
            {
                // Execute a lógica longa aqui
                await _utilidadesService.IniciarRecuperacaoXmlAnbimaAsync();
            });

            // Retorne 202 Accepted imediatamente sem caracteres especiais
            return Accepted(new { message = "Request accepted and is being processed in the background." });
        }
    }
}