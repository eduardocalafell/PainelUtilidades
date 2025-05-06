using Microsoft.EntityFrameworkCore;

namespace ConsultaCnpjReceita.Model;

public class RelatoriosProcessados
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Fundo { get; set; }
    public string DataSolicitada { get; set; }
    public string? NomeArquivo { get; set; }
}