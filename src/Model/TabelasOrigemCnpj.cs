using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ConsultaCnpjReceita.Model;

public class CnpjModelDTO
{
    public string CnpjOriginador { get; set; }
    public string CnpjSacado { get; set; }
    public string CnpjCedente { get; set; }
}

[PrimaryKey("Chave")]
public class FinaxisEstoque
{
    [Column("campo_chave")]
    public string Chave { get; set; }
    [Column("cpf_cnpj_originador")]
    public string CnpjOriginador { get; set; }

    [Column("cpf_cnpj_sacado")]
    public string CnpjSacado { get; set; }

    [Column("cpf_cnpj_cedente")]
    public string CnpjCedente { get; set; }
}

[PrimaryKey("Chave")]
public class HemeraEstoque
{
    [Column("campochave")]
    public string Chave { get; set; }
    [Column("originadorcpfcnpj")]
    public string CnpjOriginador { get; set; }

    [Column("sacadocnpjcpf")]
    public string CnpjSacado { get; set; }

    [Column("cedentecnpjcpf")]
    public string CnpjCedente { get; set; }
}

[PrimaryKey("Chave")]
public class SingulareEstoque
{
    [Column("seu_numero")]
    public string Chave { get; set; }
    [Column("doc_originador")]
    public string CnpjOriginador { get; set; }

    [Column("doc_sacado")]
    public string CnpjSacado { get; set; }

    [Column("doc_cedente")]
    public string CnpjCedente { get; set; }
}

[PrimaryKey("Chave")]
public class HemeraLiquidados
{
    [Column("campochave")]
    public string Chave { get; set; }

    [Column("sacadocnpjcpf")]
    public string CnpjSacado { get; set; }

    [Column("cedentecnpjcpf")]
    public string CnpjCedente { get; set; }
}

[PrimaryKey("Chave")]
public class FinaxisLiquidados
{
    [Column("campo_chave")]
    public string Chave { get; set; }

    [Column("cd_sacado")]
    public string CnpjSacado { get; set; }

    [Column("cd_cedente")]
    public string CnpjCedente { get; set; }
}

[PrimaryKey("Chave")]
public class HemeraLiquidadosRecompra
{
    [Column("campochave")]
    public string Chave { get; set; }

    [Column("sacadocnpjcpf")]
    public string CnpjSacado { get; set; }

    [Column("cedentecnpjcpf")]
    public string CnpjCedente { get; set; }
}

[PrimaryKey("Chave")]
public class SingulareLiquidados
{
    [Column("seu_numero")]
    public string Chave { get; set; }

    [Column("cpf_cnpj_sacado")]
    public string CnpjSacado { get; set; }

    [Column("cpf_cnpj_cedente")]
    public string CnpjCedente { get; set; }
}

[PrimaryKey("Chave")]
public class TituloPrivadoCarteira
{
    [Column("id_internoativo")]
    public string Chave { get; set; }

    [Column("cnpj_emissor")]
    public string CnpjEmissor { get; set; }
}