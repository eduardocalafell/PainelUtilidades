using Microsoft.EntityFrameworkCore;
using CsvHelper;
using CsvHelper.Configuration;

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
        public string NM_FUNDO { get; set; }
        public string NU_CNPJ { get; set; }
        public string DATA_FUNDO { get; set; }
        public string NOME_ORIGINADOR { get; set; }
        public string DOC_ORIGINADOR { get; set; }
        public string NOME_CEDENTE { get; set; }
        public string DOC_CEDENTE { get; set; }
        public string NOME_SACADO { get; set; }
        public string DOC_SACADO { get; set; }
        public string SEU_NUMERO { get; set; }
        public string NU_DOCUMENTO { get; set; }
        public string TIPO_RECEBIVEL { get; set; }
        public string VALOR_NOMINAL { get; set; }
        public string VALOR_PRESENTE { get; set; }
        public string VALOR_AQUISICAO { get; set; }
        public string VALOR_PDD { get; set; }
        public string FAIXA_PDD { get; set; }
        public string DATA_REFERENCIA { get; set; }
        public string DATA_VENCIMENTO_ORIGINAL { get; set; }
        public string DATA_VENCIMENTO_AJUSTADA { get; set; }
        public string DATA_EMISSAO { get; set; }
        public string DATA_AQUISICAO { get; set; }
        public string PRAZO { get; set; }
        public string PRAZO_ATUAL { get; set; }
        public string SITUACAO_RECEBIVEL { get; set; }
        public string TAXA_CESSAO { get; set; }
        public string TX_RECEBIVEL { get; set; }
        public string COOBRIGACAO { get; set; }
        public string LINHA { get; set; }
    }


    [PrimaryKey("Id")]
    public class EstoqueModel
    {
        public Guid? Id { get; set; } = Guid.NewGuid();
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