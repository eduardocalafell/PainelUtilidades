using System.Linq;
using Data.AppDbContext;
using Newtonsoft.Json;
using ConsultaCnpjReceita.Model;
using System.Text.RegularExpressions;
using System.Net.Http.Headers;
using System.Text;
using System.Xml.Serialization;
using System.Globalization;
using System.Xml;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace ConsultaCnpjReceita.Service;

public class UtilidadesService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private static readonly object _logLock = new object();
    private IConfiguration Configuration { get; set; }
    private readonly List<DateTime> feriadosAnbima = new List<DateTime>
    {
        new DateTime(2024, 1, 1),  // Confraternização Universal
        new DateTime(2024, 2, 12), // Carnaval
        new DateTime(2024, 2, 13), // Carnaval
        new DateTime(2024, 3, 29), // Sexta-feira Santa
        new DateTime(2024, 4, 21), // Tiradentes
        new DateTime(2024, 5, 1),  // Dia do Trabalho
        new DateTime(2024, 9, 7),  // Independência do Brasil
        new DateTime(2024, 10, 12), // Nossa Senhora Aparecida
        new DateTime(2024, 11, 2), // Finados
        new DateTime(2024, 11, 15), // Proclamação da República
        new DateTime(2024, 12, 25) // Natal
    };


    public UtilidadesService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;

        Configuration = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();
    }

    public Task ConsultarListaCnpj()
    {
        var scope = _scopeFactory.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var url = Configuration.GetSection("ReceitaWs:Url").Value;
        var token = Configuration.GetSection("ReceitaWs:Token").Value;
        HttpClient client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var listaCnpjExistente = context.tb_aux_Retorno_Receita.Select(x => FormatarCnpj(x.cnpj)).ToList();

        var cnpjEstoque = new List<string>();
        var take = 10000;
        var skip = 0;

        do
        {
            cnpjEstoque = context.tb_stg_estoque_singulare_full.AsNoTracking()
                                                                .Skip(skip).Take(take).ToList()
                                                                .SelectMany(s => new[] { s.doc_cedente.Replace("/", "").Replace(".", "").Replace("-", ""),
                                                                    s.doc_sacado.Replace("/", "").Replace(".", "").Replace("-", "") })
                                                                .Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();

            foreach (var item in cnpjEstoque)
            {
                var response = client.GetAsync($"{url}{item}/days/180").Result;
                if (response.IsSuccessStatusCode)
                {
                    var json = response.Content.ReadAsStringAsync().Result;
                    var retorno = JsonConvert.DeserializeObject<Root>(json);
                    if (retorno.cnpj is not null)
                    {
                        if (listaCnpjExistente.Contains(item))
                        {
                            if (retorno.atividades_secundarias is not null && retorno?.atividades_secundarias?.Count > 0)
                            {
                                var attCnpj = context.tb_aux_Retorno_Receita.FirstOrDefault(x => x.cnpj == item);
                                retorno.atividades_secundarias.ForEach(f => attCnpj.cnae_secundario += $"{f.code}: {f.text.ToUpperInvariant()} | ");
                                attCnpj.cnae_secundario = attCnpj.cnae_secundario.Remove(attCnpj.cnae_secundario.Length - 3, 3);

                                context.tb_aux_Retorno_Receita.Update(attCnpj);
                                context.SaveChanges();
                            }
                        }
                        else
                        {
                            if (retorno.atividade_principal is not null && retorno?.atividade_principal?.Count > 0)
                            {
                                retorno.atividade_principal.ForEach(f => retorno.cnae_primario += $"{f.code}: {f.text.ToUpperInvariant()} | ");
                                retorno.cnae_primario = retorno.cnae_primario.Remove(retorno.cnae_primario.Length - 3, 3);
                            }

                            if (retorno.atividades_secundarias is not null && retorno?.atividades_secundarias?.Count > 0)
                            {
                                retorno.atividades_secundarias.ForEach(f => retorno.cnae_secundario += $"{f.code}: {f.text.ToUpperInvariant()} | ");
                                retorno.cnae_secundario = retorno.cnae_secundario.Remove(retorno.cnae_secundario.Length - 3, 3);
                            }

                            retorno.cnpj = retorno.cnpj.Replace("/", "").Replace(".", "").Replace("-", "");

                            listaCnpjExistente.Add(item);
                            context.tb_aux_Retorno_Receita.Add(retorno);
                            context.SaveChanges();
                        }
                    }
                }
            }

            take += 10000;
            skip += 10000;

        } while (cnpjEstoque.Count > 0);

        return Task.CompletedTask;
    }

    /*  public async Task<string> IniciarRecuperacaoXmlAnbimaAsync()
     {
         await Task.Run(() => RecuperarXmlAnbima());
         return "A operação de recuperação do XML foi iniciada com sucesso!";
     }

     private async Task RecuperarXmlAnbima()
     {
         using (var scope = _serviceProvider.CreateScope())
         {
             var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

             var url = Configuration.GetSection("Singulare:Url").Value;
             var user = Configuration.GetSection("Singulare:Usuario").Value;
             var pswr = Configuration.GetSection("Singulare:Senha").Value;
             var xmlExistentes = context.tb_aux_retorno_xml_anbima.ToList();
             var listaFidcs = RecuperarFidcs();

             using HttpClient client = new HttpClient();
             client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{user}:{pswr}")));

             var response = await client.PostAsync(url + "painel/token/api", null);
             var authToken = JsonConvert.DeserializeObject<SingulareApiAuthResponse>(await response.Content.ReadAsStringAsync()).Token;

             client.DefaultRequestHeaders.Authorization = null;
             client.DefaultRequestHeaders.Add("x-api-key", authToken);

             DateTime dataInicial = DateTime.Today;
             //DateTime dataFinal = new DateTime(2024, 10, 1);
             DateTime dataFinal = DateTime.Today.AddDays(-1);

             while (dataInicial > dataFinal)
             {
                 var dataPesquisa = ObterDataUtilD2(feriadosAnbima, dataInicial);
                 Debug.WriteLine($"Começando execução para a data {dataPesquisa}", "Aviso");

                 foreach (var f in listaFidcs)
                 {
                     var retry = 0;
                     var registroExistente = xmlExistentes.FirstOrDefault(x => x.NomeFundo == f && x.DataCarteira == dataPesquisa);
                     if (registroExistente is null)
                     {
                         do
                         {
                             response = await client.GetAsync(url + $"netreport/report/xml-anbima/{f}/{dataPesquisa}");
                             if (response.IsSuccessStatusCode)
                             {
                                 var ret = await response.Content.ReadAsStringAsync();
                                 var novoXml = new NovoXmlAnbima
                                 {
                                     Id = Guid.NewGuid(),
                                     NomeFundo = f,
                                     DataCarteira = dataPesquisa,
                                     Xml = ret
                                 };

                                 try
                                 {
                                     context.tb_aux_retorno_xml_anbima.Add(novoXml);
                                     xmlExistentes.Add(novoXml);
                                     context.SaveChanges();
                                     Debug.WriteLine($"Adicionado {f} para a data {dataPesquisa}", "Aviso");
                                 }
                                 catch (Exception ex)
                                 {
                                     Debug.WriteLine(ex.InnerException);
                                 }
                             }
                             else
                             {
                                 retry++;
                                 Debug.WriteLine($"{f} não encontrado para a data {dataPesquisa}", "Aviso");
                                 await Task.Delay(500);
                             }
                         } while (!response.IsSuccessStatusCode && retry <= 0);
                     }
                     else
                     {
                         Debug.WriteLine($"{f} já inserido para a data {dataPesquisa}", "Aviso");
                     }
                 }

                 dataInicial = dataInicial.AddDays(-1);
             }
         }

     } */

    private static string FormatarCnpj(string cnpj)
    {
        var r = new Regex("[^0-9a-zA-Z]+");
        return r.Replace(cnpj, "");
    }

    private static void EscreverLog(string mensagem)
    {
        lock (_logLock)
        {
            using (StreamWriter log = new StreamWriter($"{Environment.CurrentDirectory}/.log", append: true))
            {
                log.WriteLine($"[{DateTime.Now:s}] {mensagem}");
                log.Close();
            }
        }
    }

    private static string ObterDataUtilD2(List<DateTime> feriados, DateTime? dataInicial = null)
    {
        var hoje = dataInicial?.Date ?? DateTime.Today;
        var diasUteis = 0;
        var dataCalculada = hoje;

        while (diasUteis < 2)
        {
            dataCalculada = dataCalculada.AddDays(-1); // Subtraindo 1 dia por vez

            // Verifica se é dia útil (não final de semana e não feriado)
            if (dataCalculada.DayOfWeek != DayOfWeek.Saturday &&
                dataCalculada.DayOfWeek != DayOfWeek.Sunday &&
                !feriados.Contains(dataCalculada))
            {
                diasUteis++;
            }
        }

        // Retorna a data no formato "yyyy-MM-dd"
        return dataCalculada.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private List<string> RecuperarFidcs()
    {
        return [
                "AURUM FIDC",
                "BRAVA FIDC",
                "BRAVA FIDC MZ1",
                "BRAVA FIDC SR1",
                "CUPERTINO FIDC",
                "CUPERTINO MEZ1",
                "CUPERTINO MEZ2",
                "CUPERTINO SEN",
                "CUPERTINO SEN2",
                "CUPERTINO SEN4",
                "CUPERTINO SEN5",
                "CUPERTINO SEN6",
                "CUPERTINO SEN7",
                "CUPERTINO SEN8",
                "DIP FINAN11 FIC",
                "DIP FINANCI MEZ",
                "DIP FINANCI SEN",
                "DIP FINANCING11",
                "EMUNAH FIDC",
                "EMUNAH FIDC MZ1",
                "EMUNAH FIDC MZ2",
                "EMUNAH FIDC SR1",
                "EMUNAH FIDC SR2",
                "F18 FIDC",
                "F18 FIDC MEZ",
                "F18 FIDC SEN",
                "FIDC AURUM MEZA",
                "FIDC AURUM MEZB",
                "FIDC AURUM MEZE",
                "FIDC AURUM MEZF",
                "FIDC AURUM MEZG",
                "FIDC AURUM MEZH",
                "FIDC AURUM MEZI",
                "FIDC AURUM MEZJ",
                "FIDC AURUM MEZK",
                "FIDC AURUM MEZL",
                "FIDC AURUM MEZM",
                "FIDC AURUM MK1",
                "FIDC AURUM MZG2",
                "FIDC AURUM SR14",
                "FIDC AURUM SR15",
                "FIDC AURUM SR16",
                "FIDC AURUM SR17",
                "FIDC AURUM SR19",
                "FIDC AURUM SR20",
                "FIDC AURUM SR21",
                "FIDC AURUM SR22",
                "FIDC AURUM SR23",
                "FIDC AURUM SR24",
                "FIDC AURUM SR4",
                "FIDC ML BANK II",
                "FIDC MLB",
                "FIDC MLB MEZ",
                "FIDC MLB SR1",
                "FIDCMLBANKII M2",
                "FIDCMLBANKII M3",
                "FIDCMLBANKII MZ",
                "FIDCMLBANKII S2",
                "FIDCMLBANKII S3",
                "FIDCMLBANKII S4",
                "FIDCMLBANKII S5",
                "FIDCMLBANKII S6",
                "FIDCMLBANKII SN",
                "GAVEA OPEN FIDC",
                "GAVEAOPENFIDCSR",
                "GAVEAOPENMEZ1",
                "GAVEA REAL FIDC",
                "GAVEA REAL SEN1",
                "GAVEA REAL SEN2",
                "GAVEA SUL FIDC",
                "GAVEA SUL MEZA2",
                "GAVEA SUL MEZA3",
                "GAVEA SUL MEZA5",
                "GAVEA SUL MEZAN",
                "GAVEA SUL SEN10",
                "GAVEA SUL SEN11",
                "GAVEA SUL SEN12",
                "GAVEA SUL SEN13",
                "GAVEA SUL SEN2",
                "GAVEA SUL SEN3",
                "GAVEA SUL SEN4",
                "GAVEA SUL SEN5",
                "GAVEA SUL SEN6",
                "GAVEA SUL SEN7",
                "GAVEA SUL SEN8",
                "GAVEA SUL SEN9",
                "GAVEA SUL SENIO",
                "GPR FIDC",
                "GPR FIDC MEZA",
                "GPR FIDC MEZ2",
                "GPR FIDC SEN",
                "GPR FIDC SEN 2",
                "GPR FIDC SEN 3",
                "ONE7 CRED FIDC",
                "ONE7LB SUBJR",
                "ONE7LP MEZ CI",
                "ONE7LP MEZ DI",
                "ONE7LP MEZ E",
                "ONE7LP MEZ F",
                "ONE7LP MEZ G",
                "ONE7LP SEN 14",
                "ONE7LP SEN 15",
                "PHD FIDC MEZ",
                "PHD FIDC SEN",
                "PHD FIDC SUB",
                "PUMA FIDC SUB",
                "SC FIDC",
                "SC FIDC SEN1",
                "SC FIDC SEN2",
                "SFT CI FIDC",
                "SFT CI FIDC SR1",
                "SIGMA CRED FIDC",
                "SIGMA CRED MEZ",
                "SIGMA CRED SEN",
                "SP ADGM FIDC",
                "SP ADGM MEZ1",
                "SP ADGM MEZ2",
                "SP ADGM MEZ3",
                "SP ADGM SEN1",
                "SP ADGM SEN2",
                "SP ADGM SEN3",
                "SP ADGM SEN4",
                "SUPER BOX FIDC",
                "SUPER BOX MEZ",
                "SUPER BOX SEN",
                "TMOV FIDC",
                "TMOV FIDC SR"
        ];

    }
}