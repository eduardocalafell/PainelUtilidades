using System.Linq;
using Data.AppDbContext;
using Newtonsoft.Json;
using ConsultaCnpjReceita.Model;
using System.Text.RegularExpressions;

namespace ConsultaCnpjReceita.Service;

public class UtilidadesService
{
    private readonly AppDbContext _context;
    private IConfiguration Configuration { get; set; }

    public UtilidadesService(AppDbContext context)
    {
        _context = context;

        Configuration = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();
    }

    public object ConsultarListaCnpj()
    {
        List<Root> listaRetorno = new List<Root>();
        var url = Configuration.GetSection("ReceitaWs:Url").Value;
        HttpClient client = new HttpClient();

        var listaCnpjEstoque = _context.CnpjEstoque.ToList();
        var listaCnpjExistente = _context.RetornoReceita.Select(x => FormatarCnpj(x.cnpj)).ToList();
        var listaCnpjPesquisa = new List<string>();
        bool houveInclusao = false;

        foreach (var item in listaCnpjEstoque)
        {
            var cnpjCedente = FormatarCnpj(item.DocCedente);
            var cnpjSacado = FormatarCnpj(item.DocSacado);
            houveInclusao = false;

            var response = client.GetAsync($"{url}{cnpjCedente}").Result;
            if (response.IsSuccessStatusCode && !listaCnpjExistente.Contains(cnpjCedente) && !listaCnpjPesquisa.Contains(cnpjCedente))
            {
                var json = response.Content.ReadAsStringAsync().Result;
                var retorno = JsonConvert.DeserializeObject<Root>(json);
                if (retorno.cnpj is not null)
                {
                    listaCnpjPesquisa.Add(cnpjCedente);
                    _context.RetornoReceita.Add(retorno);
                    houveInclusao = true;
                }
            }

            response = client.GetAsync($"{url}{cnpjSacado}").Result;
            if (response.IsSuccessStatusCode && !listaCnpjExistente.Contains(cnpjSacado) && !listaCnpjPesquisa.Contains(cnpjSacado))
            {
                var json = response.Content.ReadAsStringAsync().Result;
                var retorno = JsonConvert.DeserializeObject<Root>(json);
                if (retorno.cnpj is not null)
                {
                    listaCnpjPesquisa.Add(cnpjCedente);
                    _context.RetornoReceita.Add(retorno);
                    houveInclusao = true;
                }
            }

            if (houveInclusao)
            {
                _context.SaveChanges();
                Thread.Sleep(60000);
            }
        }

        return listaRetorno;
    }

    private static string FormatarCnpj(string cnpj)
    {
        var r = new Regex("[^0-9a-zA-Z]+");
        return r.Replace(cnpj, "");
    }
}