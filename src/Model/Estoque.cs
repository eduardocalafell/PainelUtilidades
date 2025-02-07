using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ConsultaCnpjReceita.Model
{
    public class tb_stg_estoque_full
    {
        public tb_stg_estoque_full() { }

        public tb_stg_estoque_full(string docCedente, string docSacado)
        {
            Id = Guid.NewGuid();
            DocCedente = docCedente;
            DocSacado = docSacado;
        }

        public Guid Id { get; set; }
        public string DocCedente { get; set; }
        public string DocSacado { get; set; }
    }

    public class tb_stg_estoque_full_hemera
    {
        public tb_stg_estoque_full_hemera() { }

        public tb_stg_estoque_full_hemera(string docCedente, string docSacado)
        {
            Id = Guid.NewGuid();
            DocCedente = docCedente;
            DocSacado = docSacado;
        }

        public Guid Id { get; set; }
        public string DocCedente { get; set; }
        public string DocSacado { get; set; }
    }

    public class EstoqueCsv
    {
        public string nomeFundo { get; set; }
        public string docFundo { get; set; }
        public string dataFundo { get; set; }
        public string nomeGestor { get; set; }
        public string docGestor { get; set; }
        public string nomeOriginador { get; set; }
        public string docOriginador { get; set; }
        public string nomeCedente { get; set; }
        public string docCedente { get; set; }
        public string nomeSacado { get; set; }
        public string docSacado { get; set; }
        public string seuNumero { get; set; }
        public string numeroDocumento { get; set; }
        public string tipoRecebivel { get; set; }
        public string valorNominal { get; set; }
        public string valorPresente { get; set; }
        public string valorAquisicao { get; set; }
        public string valorPdd { get; set; }
        public string faixaPdd { get; set; }
        public string dataReferencia { get; set; }
        public string dataVencimentoOriginal { get; set; }
        public string dataVencimentoAjustada { get; set; }
        public string dataEmissao { get; set; }
        public string dataAquisicao { get; set; }
        public string prazo { get; set; }
        public string prazoAnual { get; set; }
        public string situacaoRecebivel { get; set; }
        public string taxaCessao { get; set; }
        public string taxaRecebivel { get; set; }
        public string coobrigacao { get; set; }
    }

    [PrimaryKey("seu_numero")]
    public class EstoqueModel
    {
        public string nome_fundo { get; set; }
        public string doc_fundo { get; set; }
        public string data_fundo { get; set; }
        public string nome_originador { get; set; }
        public string doc_originador { get; set; }
        public string nome_cedente { get; set; }
        public string doc_cedente { get; set; }
        public string nome_sacado { get; set; }
        public string doc_sacado { get; set; }
        public string seu_numero { get; set; }
        public string nu_documento { get; set; }
        public string tipo_recebivel { get; set; }
        public string valor_nominal { get; set; }
        public string valor_presente { get; set; }
        public string valor_aquisicao { get; set; }
        public string valor_pdd { get; set; }
        public string faixa_pdd { get; set; }
        public string data_referencia { get; set; }
        public string data_vencimento_original { get; set; }
        public string data_vencimento_ajustada { get; set; }
        public string data_emissao { get; set; }
        public string data_aquisicao { get; set; }
        public string prazo { get; set; }
        public string prazo_atual { get; set; }
        public string situacao_recebivel { get; set; }
        public string taxa_cessao { get; set; }
        public string tx_recebivel { get; set; }
        public string coobrigacao { get; set; }
    }
}