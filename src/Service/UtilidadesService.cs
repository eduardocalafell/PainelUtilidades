using System.Linq;
using Data.AppDbContext;
using Newtonsoft.Json;
using ConsultaCnpjReceita.Model;

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

        var listaCnpjEstoque = _context.Estoque.ToList();

        foreach (var item in listaCnpjEstoque)
        {
            var cnpj = item.DocCedente;
            var response = client.GetAsync($"{url}{cnpj}").Result;
            if (response.IsSuccessStatusCode)
            {
                var json = response.Content.ReadAsStringAsync().Result;
                var retorno = JsonConvert.DeserializeObject<Root>(json);
                listaRetorno.Add(retorno);
            }
        }

        return listaRetorno;
    }


}