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
    /// Ações de SFTP
    /// </summary>
    [Route("[controller]")]
    [ApiController]
    public class SftpController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly SftpService _sftpService;
        public SftpController(AppDbContext context, ProcessamentoBackgroundService processamentoBackgroundService, IServiceScopeFactory serviceScopeFactory)
        {
            _context = context;
            _scopeFactory = serviceScopeFactory;
            _sftpService = new SftpService(_context, _scopeFactory);
        }

        /// <summary>
        /// Integração com os arquivos de SFTP.
        /// </summary>
        /// <returns></returns>
        [HttpPost("RecuperarDadosSftpSingulareEstoque")]
        [ProducesResponseType(202), ProducesResponseType(400)]
        public IActionResult RecuperarDadosSftpSingulareEstoque()
        {
            Task.Run(async () =>
            {
                await _sftpService.RecuperarDadosSftpSingulareEstoque();
            });

            // Retorne 202 Accepted imediatamente sem caracteres especiais
            return Accepted(new { message = "Executando ação de integração com os arquivos de SFTP." });
        }
    }
}