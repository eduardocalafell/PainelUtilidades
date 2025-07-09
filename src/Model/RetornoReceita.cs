using System.ComponentModel.DataAnnotations.Schema;

namespace ConsultaCnpjReceita.Model;

public class Root
{
    public Guid Id { get; set; }
    public DateTime ultima_atualizacao { get; set; }
    public string cnpj { get; set; }
    public string tipo { get; set; }
    public string porte { get; set; }
    public string nome { get; set; }
    public string fantasia { get; set; }
    public string abertura { get; set; }
    [NotMapped]
    public List<Atividade> atividade_principal { get; set; }
    [NotMapped]
    public List<Atividade> atividade_secundaria { get; set; }
    public string? cnae_primario { get; set; }
    public string? cnae_secundario { get; set; }
    public string natureza_juridica { get; set; }
    public string logradouro { get; set; }
    public string numero { get; set; }
    public string complemento { get; set; }
    public string cep { get; set; }
    public string bairro { get; set; }
    public string municipio { get; set; }
    public string uf { get; set; }
    public string email { get; set; }
    public string telefone { get; set; }
    public string efr { get; set; }
    public string situacao { get; set; }
}

public class Atividade
{
    public string code { get; set; }
    public string text { get; set; }
}