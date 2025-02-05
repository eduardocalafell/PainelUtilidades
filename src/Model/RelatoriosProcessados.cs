using Microsoft.EntityFrameworkCore;

namespace ConsultaCnpjReceita.Model;

public class RelatoriosProcessados
{
    public Guid Id { get; set; }
    public string Fundo { get; set; }
    public string DataSolicitada { get; set; }
}