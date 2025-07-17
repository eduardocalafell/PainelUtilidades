using Microsoft.AspNetCore.Mvc;
using Data.AppDbContext;
using ConsultaCnpjReceita.Service;
using System.Linq;
using System.Data;
using WebConsultaCnpjReceita.Models;
using ConsultaCnpjReceita.Model;

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
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly UtilidadesService _utilidadesService;
        private readonly ILoggerFactory _loggerFactory;
        private readonly WebhookService _webhookService;
        private readonly ProcessamentoBackgroundService _backgroundService;

        public UtilidadesController(AppDbContext context, IServiceProvider serviceProvider, ProcessamentoBackgroundService processamentoBackgroundService, IServiceScopeFactory serviceScopeFactory, ILoggerFactory loggerFactory)
        {
            _context = context;
            _serviceProvider = serviceProvider;
            _backgroundService = processamentoBackgroundService;
            _scopeFactory = serviceScopeFactory;
            _utilidadesService = new UtilidadesService(_scopeFactory);
            _loggerFactory = loggerFactory;
            _webhookService = new WebhookService(_context, _scopeFactory, _loggerFactory);
        }

        /// <summary>
        /// Consulta a lista de CNPJs armazenados no banco de dados.
        /// </summary>
        /// <returns></returns>
        [HttpPost("ConsultarListaCnpj")]
        [ProducesResponseType(202), ProducesResponseType(400)]
        public IActionResult ConsultarListaCnpj()
        {
            Task.Run(_utilidadesService.ConsultarListaCnpj);

            return Accepted(new { message = "Solicitação recebida, processando CNPJs na base de dados." });
        }

        /// <summary>
        /// Recupera os XMLs de todos os fundos cadastrados.
        /// </summary>
        /// <returns></returns>
        [HttpPost("RecuperarXmlAnbima")]
        [ProducesResponseType(202), ProducesResponseType(400)]
        public IActionResult RecuperarXmlAnbima()
        {
            Task.Run(_utilidadesService.RecuperarXmlAnbima);

            return Accepted(new { message = "Integração do XML AMBIMA executado com sucesso!" });
        }

        /// <summary>
        /// Retorno para callback de solicitação do relatório de Estoque.
        /// </summary>
        /// <returns></returns>
        [HttpPost("CallbackEstoqueSingulare")]
        [ProducesResponseType(202), ProducesResponseType(400)]
        public IActionResult CallbackEstoqueSingulare(WebhookPayload payload)
        {
            _webhookService.CallbackEstoqueSingulare(payload);
            return Accepted(new { message = "Request accepted and is being processed in the background." });
        }

        /// <summary>
        /// Iniciar recuperação do estoque diário de cada fundo.
        /// </summary>
        /// <returns></returns>
        [HttpPost("RecuperarRelatorioEstoqueSingulare")]
        [ProducesResponseType(202), ProducesResponseType(400)]
        public IActionResult RecuperarRelatorioEstoqueSingulare()
        {
            Task.Run(_webhookService.RecuperarRelatorioEstoqueSingulare);

            return Accepted(new { message = "Recuperando relatórios de estoque..." });
        }

        /// <summary>
        /// Processa os arquivos de estoque recebidos através do callback.
        /// </summary>
        /// <returns></returns>
        [HttpPost("ProcessarArquivosEstoqueSingulare")]
        [ProducesResponseType(202), ProducesResponseType(400)]
        public IActionResult ProcessarArquivosEstoqueSingulare()
        {
            Task.Run(_webhookService.ProcessarArquivosEstoqueSingulare);

            return Accepted(new { message = "Integrando arquivos de estoque..." });
        }
    }
}