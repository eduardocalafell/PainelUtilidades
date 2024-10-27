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
        private readonly UtilidadesService _utilidadesService;

        public UtilidadesController(AppDbContext context)
        {
            _context = context;
            _utilidadesService = new UtilidadesService(context);
        }

        /// <summary>
        /// Consulta a lista de CNPJs armazenados no banco de dados.
        /// </summary>
        /// <returns></returns>
        [HttpGet("ConsultarListaCnpj")]
        [ProducesResponseType(200), ProducesResponseType(500)]
        public IActionResult ConsultarListaCnpj()
        {
            var ret = _utilidadesService.ConsultarListaCnpj();
            return Ok(ret);
        }

        /// <summary>
        /// Recupera os XMLs de todos os fundos cadastrados.
        /// </summary>
        /// <returns></returns>
        [HttpGet("RecuperarXmlAnbima")]
        [ProducesResponseType(200), ProducesResponseType(500)]
        public IActionResult RecuperarXmlAnbima()
        {
            var ret = _utilidadesService.RecuperarXmlAnbima();
            return Ok(ret);
        }
    }
}