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
    private readonly AppDbContext _context;
    private readonly IServiceProvider _serviceProvider;
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


    public UtilidadesService(AppDbContext context, IServiceProvider serviceProvider)
    {
        _context = context;
        _serviceProvider = serviceProvider;

        Configuration = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();
    }

    public async Task<string> IniciarConsultarListaCnpj()
    {
        await Task.Run(() => ConsultarListaCnpj());
        return "Consulta à receita sendo executada...";
    }

    public async Task ConsultarListaCnpj()
    {
        var listaCnpjConsulta = new List<string>();

        using (var scope = _serviceProvider.CreateScope())
        {
            var scopedContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Titulos Privados
            var ListaTitulosPrivados = await scopedContext.tb_ods_titulo_privado_carteira.Select(x => new CnpjModelDTO
            {
                CnpjOriginador = FormatarCnpj(x.CnpjEmissor)
            }).Distinct().ToListAsync();

            ListaTitulosPrivados.ForEach(f =>
            {
                listaCnpjConsulta.Add(f.CnpjOriginador);
            });
            // Fim Titulos Privados

            /*             // Finaxis Estoque
                        var ListaEstoqueFinaxis = await scopedContext.tb_stg_estoque_finaxis_full.Select(x => new CnpjModelDTO
                        {
                            CnpjOriginador = FormatarCnpj(x.CnpjOriginador),
                            CnpjCedente = FormatarCnpj(x.CnpjCedente),
                            CnpjSacado = FormatarCnpj(x.CnpjSacado),
                        }).ToListAsync();

                        ListaEstoqueFinaxis.ForEach(f =>
                        {
                            listaCnpjConsulta.Add(f.CnpjOriginador);
                            listaCnpjConsulta.Add(f.CnpjCedente);
                            listaCnpjConsulta.Add(f.CnpjSacado);
                        });
                        // Fim Finaxis Estoque

                        // Hemera Estoque
                        var ListaEstoqueHemera = await scopedContext.tb_stg_estoque_hemera_full.Select(x => new CnpjModelDTO
                        {
                            CnpjOriginador = FormatarCnpj(x.CnpjOriginador),
                            CnpjCedente = FormatarCnpj(x.CnpjCedente),
                            CnpjSacado = FormatarCnpj(x.CnpjSacado),
                        }).ToListAsync();

                        ListaEstoqueHemera.ForEach(f =>
                        {
                            listaCnpjConsulta.Add(f.CnpjOriginador);
                            listaCnpjConsulta.Add(f.CnpjCedente);
                            listaCnpjConsulta.Add(f.CnpjSacado);
                        });
                        // Fim Hemera Estoque

                        // Singulare Estoque
                        var ListaEstoqueSingulare = await scopedContext.tb_stg_estoque_singulare_full.Select(x => new CnpjModelDTO
                        {
                            CnpjOriginador = FormatarCnpj(x.CnpjOriginador),
                            CnpjCedente = FormatarCnpj(x.CnpjCedente),
                            CnpjSacado = FormatarCnpj(x.CnpjSacado),
                        }).ToListAsync();

                        ListaEstoqueSingulare.ForEach(f =>
                        {
                            listaCnpjConsulta.Add(f.CnpjOriginador);
                            listaCnpjConsulta.Add(f.CnpjCedente);
                            listaCnpjConsulta.Add(f.CnpjSacado);
                        });
                        // Fim Singulare Estoque

                        // Hemera Liquidados
                        var ListaLiquidadosHemera = await scopedContext.tb_stg_estoque_singulare_full.Select(x => new CnpjModelDTO
                        {
                            CnpjCedente = FormatarCnpj(x.CnpjCedente),
                            CnpjSacado = FormatarCnpj(x.CnpjSacado),
                        }).ToListAsync();

                        ListaLiquidadosHemera.ForEach(f =>
                        {
                            listaCnpjConsulta.Add(f.CnpjCedente);
                            listaCnpjConsulta.Add(f.CnpjSacado);
                        });
                        // Fim Hemera Liquidados

                        // Finaxis Liquidados
                        var ListaLiquidadosFinaxis = await scopedContext.tb_stg_liquidados_finaxis_full.Select(x => new CnpjModelDTO
                        {
                            CnpjCedente = FormatarCnpj(x.CnpjCedente),
                            CnpjSacado = FormatarCnpj(x.CnpjSacado),
                        }).ToListAsync();

                        ListaLiquidadosFinaxis.ForEach(f =>
                        {
                            listaCnpjConsulta.Add(f.CnpjCedente);
                            listaCnpjConsulta.Add(f.CnpjSacado);
                        });
                        // Fim Finaxis Liquidados

                        // Finaxis Liquidados Recompra Hemera
                        var ListaLiquidadosRecompraHemera = await scopedContext.tb_stg_liquidados_recompra_hemera_full.Select(x => new CnpjModelDTO
                        {
                            CnpjCedente = FormatarCnpj(x.CnpjCedente),
                            CnpjSacado = FormatarCnpj(x.CnpjSacado),
                        }).ToListAsync();

                        ListaLiquidadosRecompraHemera.ForEach(f =>
                        {
                            listaCnpjConsulta.Add(f.CnpjCedente);
                            listaCnpjConsulta.Add(f.CnpjSacado);
                        });
                        // Fim Hemera Liquidados Recompra

                        // Singulare Liquidados
                        var ListaLiquidadosSingulare = await scopedContext.tb_stg_liquidados_singulare_full.Select(x => new CnpjModelDTO
                        {
                            CnpjCedente = FormatarCnpj(x.CnpjCedente),
                            CnpjSacado = FormatarCnpj(x.CnpjSacado),
                        }).ToListAsync();

                        ListaLiquidadosSingulare.ForEach(f =>
                        {
                            listaCnpjConsulta.Add(f.CnpjCedente);
                            listaCnpjConsulta.Add(f.CnpjSacado);
                        });
                        // Fim Singulare Liquidados */

            var url = Configuration.GetSection("ReceitaWs:Url").Value;
            HttpClient client = new HttpClient();

            var listaCnpjExistente = await scopedContext.tb_aux_Retorno_Receita.Select(x => FormatarCnpj(x.cnpj)).ToListAsync();

            foreach (var item in listaCnpjConsulta)
            {
                var response = await client.GetAsync($"{url}{item}");
                if (response.IsSuccessStatusCode && !listaCnpjExistente.Contains(item))
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var retorno = JsonConvert.DeserializeObject<Root>(json);
                    if (retorno.cnpj is not null)
                    {
                        listaCnpjExistente.Add(item);
                        scopedContext.tb_aux_Retorno_Receita.Add(retorno);
                        scopedContext.SaveChanges();
                    }
                }

                await Task.Delay(20500);
            }
        }
    }

    public async Task<string> IniciarRecuperacaoXmlAnbimaAsync()
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

    }


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