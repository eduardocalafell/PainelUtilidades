using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace ConsultaCnpjReceita.Model
{
    public class SingulareApiAuthResponse(string token)
    {
        [JsonProperty("apiToken")]
        public string Token { get; set; } = token;

    }

    [PrimaryKey("Id")]
    public class XmlAmbimaModel
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string? Isin { get; set; }
        public string? Cnpj { get; set; }
        public string? Nome { get; set; }
        public string? DataPosicao { get; set; }
        public string? NomeAdm { get; set; }
        public string? CnpjAdm { get; set; }
        public string? NomeGestor { get; set; }
        public string? CnpjGestor { get; set; }
        public string? NomeCustodiante { get; set; }
        public string? CnpjCustodiante { get; set; }
        public decimal? ValorCota { get; set; }
        public decimal? Quantidade { get; set; }
        public decimal? PatrimonioLiquido { get; set; }
        public decimal? ValorAtivos { get; set; }
        public decimal? ValorReceber { get; set; }
        public decimal? ValorPagar { get; set; }
        public decimal? VlCotasEmitir { get; set; }
        public decimal? VlCotasResgatar { get; set; }
        public int? CodAnbid { get; set; }
        public int? TipoFundo { get; set; }
        public string? NivelRsc { get; set; }
        public string? TipoConta { get; set; }
        public decimal? Saldo { get; set; }
        public decimal? ValorFinanceiro { get; set; }
        public int? CodProv { get; set; }
        public string? CreDebProv { get; set; }
        public string? DataProv { get; set; }
        public decimal? ValorProv { get; set; }
    }


    [XmlRoot(ElementName = "arquivoposicao_4_01")]
    public class ArquivoPosicao
    {
        [XmlElement(ElementName = "fundo")]
        public Fundo Fundo { get; set; }
    }

    public class Fundo
    {
        [XmlElement(ElementName = "header")]
        public Header Header { get; set; }

        [XmlElement(ElementName = "outrasdespesas")]
        public List<OutrasDespesas> OutrasDespesas { get; set; }

        [XmlElement(ElementName = "caixa")]
        public Caixa Caixa { get; set; }

        [XmlElement(ElementName = "fidc")]
        public FIDC FIDC { get; set; }

        [XmlElement(ElementName = "provisao")]
        public Provisao Provisao { get; set; }
    }

    public class Header
    {
        [XmlElement(ElementName = "isin")]
        public string Isin { get; set; }

        [XmlElement(ElementName = "cnpj")]
        public string Cnpj { get; set; }

        [XmlElement(ElementName = "nome")]
        public string Nome { get; set; }

        [XmlElement(ElementName = "dtposicao")]
        public string DataPosicao { get; set; }

        [XmlElement(ElementName = "nomeadm")]
        public string NomeAdm { get; set; }

        [XmlElement(ElementName = "cnpjadm")]
        public string CnpjAdm { get; set; }

        [XmlElement(ElementName = "nomegestor")]
        public string NomeGestor { get; set; }

        [XmlElement(ElementName = "cnpjgestor")]
        public string CnpjGestor { get; set; }

        [XmlElement(ElementName = "nomecustodiante")]
        public string NomeCustodiante { get; set; }

        [XmlElement(ElementName = "cnpjcustodiante")]
        public string CnpjCustodiante { get; set; }

        [XmlElement(ElementName = "valorcota")]
        public decimal ValorCota { get; set; }

        [XmlElement(ElementName = "quantidade")]
        public decimal Quantidade { get; set; }

        [XmlElement(ElementName = "patliq")]
        public decimal PatrimonioLiquido { get; set; }

        [XmlElement(ElementName = "valorativos")]
        public decimal ValorAtivos { get; set; }

        [XmlElement(ElementName = "valorreceber")]
        public decimal ValorReceber { get; set; }

        [XmlElement(ElementName = "valorpagar")]
        public decimal ValorPagar { get; set; }

        [XmlElement(ElementName = "vlcotasemitir")]
        public decimal VlCotasEmitir { get; set; }

        [XmlElement(ElementName = "vlcotasresgatar")]
        public decimal VlCotasResgatar { get; set; }

        [XmlElement(ElementName = "codanbid")]
        public int CodAnbid { get; set; }

        [XmlElement(ElementName = "tipofundo")]
        public int TipoFundo { get; set; }

        [XmlElement(ElementName = "nivelrsc")]
        public string NivelRsc { get; set; }
    }

    public class OutrasDespesas
    {
        [XmlElement(ElementName = "coddesp")]
        public int CodDespesa { get; set; }

        [XmlElement(ElementName = "valor")]
        public decimal Valor { get; set; }
    }

    public class Caixa
    {
        [XmlElement(ElementName = "isininstituicao")]
        public string IsinInstituicao { get; set; }

        [XmlElement(ElementName = "tpconta")]
        public string TipoConta { get; set; }

        [XmlElement(ElementName = "saldo")]
        public decimal Saldo { get; set; }

        [XmlElement(ElementName = "nivelrsc")]
        public string NivelRsc { get; set; }
    }

    public class FIDC
    {
        [XmlElement(ElementName = "valorfinanceiro")]
        public decimal ValorFinanceiro { get; set; }
    }

    public class Provisao
    {
        [XmlElement(ElementName = "codprov")]
        public int CodProv { get; set; }

        [XmlElement(ElementName = "credeb")]
        public string CreDeb { get; set; }

        [XmlElement(ElementName = "dt")]
        public string Data { get; set; }

        [XmlElement(ElementName = "valor")]
        public decimal Valor { get; set; }
    }
}