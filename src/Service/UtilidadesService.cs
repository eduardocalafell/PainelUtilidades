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

namespace ConsultaCnpjReceita.Service;

public class UtilidadesService
{
    private readonly AppDbContext _context;
    private readonly IServiceProvider _serviceProvider;
    private static readonly object _logLock = new object();
    private IConfiguration Configuration { get; set; }

    public UtilidadesService(AppDbContext context, IServiceProvider serviceProvider)
    {
        _context = context;
        _serviceProvider = serviceProvider;

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

        var listaCnpjEstoque = new List<object>();
        var listaCnpjEstoqueCast = new List<tb_stg_estoque_full>();
        listaCnpjEstoque.AddRange(_context.tb_stg_estoque_full.ToList());
        listaCnpjEstoque.AddRange(_context.tb_stg_estoque_full_hemera.ToList());
        listaCnpjEstoqueCast = listaCnpjEstoque.Cast<tb_stg_estoque_full>().ToList();
        var listaCnpjExistente = _context.tb_aux_Retorno_Receita.Select(x => FormatarCnpj(x.cnpj)).ToList();
        var listaCnpjPesquisa = new List<string>();
        bool houveInclusao = false;

        foreach (var item in listaCnpjEstoqueCast)
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
                    _context.tb_aux_Retorno_Receita.Add(retorno);
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
                    _context.tb_aux_Retorno_Receita.Add(retorno);
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
            var xmlExistentes = context.tb_aux_Xml_Anbima.ToList();
            var listaFidcs = RecuperarFidcs();

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{user}:{pswr}")));

            var response = await client.PostAsync(url + "painel/token/api", null);
            var authToken = JsonConvert.DeserializeObject<SingulareApiAuthResponse>(response.Content.ReadAsStringAsync().Result).Token;

            client.DefaultRequestHeaders.Authorization = null;
            client.DefaultRequestHeaders.Add("x-api-key", authToken);

            listaFidcs.ForEach(async f =>
            {
                var retry = 0;
                do
                {
                    response = await client.GetAsync(url + $"netreport/report/xml-anbima/{f}");
                    if (response.IsSuccessStatusCode)
                    {
                        var ret = response.Content.ReadAsStringAsync().Result;
                        XmlSerializer serializer = new XmlSerializer(typeof(ArquivoPosicao));
                        using StringReader reader = new StringReader(ret);
                        try
                        {
                            ArquivoPosicao arquivoPosicao = (ArquivoPosicao)serializer.Deserialize(reader);

                            if (arquivoPosicao.Fundo is not null)
                            {
                                XmlAmbimaModel xmlAmbimaModel = new XmlAmbimaModel
                                {
                                    Isin = arquivoPosicao.Fundo?.Header?.Isin ?? string.Empty,
                                    Cnpj = arquivoPosicao.Fundo?.Header?.Cnpj ?? string.Empty,
                                    Nome = arquivoPosicao.Fundo?.Header?.Nome ?? string.Empty,
                                    DataPosicao = arquivoPosicao.Fundo?.Header?.DataPosicao ?? string.Empty,
                                    NomeAdm = arquivoPosicao.Fundo?.Header?.NomeAdm ?? string.Empty,
                                    CnpjAdm = arquivoPosicao.Fundo?.Header?.CnpjAdm ?? string.Empty,
                                    NomeGestor = arquivoPosicao.Fundo?.Header?.NomeGestor ?? string.Empty,
                                    CnpjGestor = arquivoPosicao.Fundo?.Header?.CnpjGestor ?? string.Empty,
                                    NomeCustodiante = arquivoPosicao.Fundo?.Header?.NomeCustodiante ?? string.Empty,
                                    CnpjCustodiante = arquivoPosicao.Fundo?.Header?.CnpjCustodiante ?? string.Empty,
                                    ValorCota = arquivoPosicao.Fundo?.Header?.ValorCota ?? 0,
                                    Quantidade = arquivoPosicao.Fundo?.Header?.Quantidade ?? 0,
                                    PatrimonioLiquido = arquivoPosicao.Fundo?.Header?.PatrimonioLiquido ?? 0,
                                    ValorAtivos = arquivoPosicao.Fundo?.Header?.ValorAtivos ?? 0,
                                    ValorReceber = arquivoPosicao.Fundo?.Header?.ValorReceber ?? 0,
                                    ValorPagar = arquivoPosicao.Fundo?.Header?.ValorPagar ?? 0,
                                    VlCotasEmitir = arquivoPosicao.Fundo?.Header?.VlCotasEmitir ?? 0,
                                    VlCotasResgatar = arquivoPosicao.Fundo?.Header?.VlCotasResgatar ?? 0,
                                    CodAnbid = arquivoPosicao.Fundo?.Header?.CodAnbid ?? 0,
                                    TipoFundo = arquivoPosicao.Fundo?.Header?.TipoFundo ?? 0,
                                    NivelRsc = arquivoPosicao.Fundo?.Header?.NivelRsc ?? string.Empty,
                                    TipoConta = arquivoPosicao.Fundo?.Caixa?.TipoConta ?? string.Empty,
                                    Saldo = arquivoPosicao.Fundo?.Caixa?.Saldo ?? 0,
                                    ValorFinanceiro = arquivoPosicao.Fundo?.FIDC?.ValorFinanceiro ?? 0,
                                    CodProv = arquivoPosicao.Fundo?.Provisao?.CodProv ?? 0,
                                    CreDebProv = arquivoPosicao.Fundo?.Provisao?.CreDeb ?? string.Empty,
                                    DataProv = arquivoPosicao.Fundo?.Provisao?.Data ?? string.Empty,
                                    ValorProv = arquivoPosicao.Fundo?.Provisao?.Valor ?? 0
                                };


                                if (xmlExistentes.FirstOrDefault(x => x.DataPosicao == xmlAmbimaModel.DataPosicao && x.Nome == xmlAmbimaModel.Nome) == null)
                                {
                                    EscreverLog($"Posição do fundo {f} recuperado e salvo com sucesso!");
                                    //File.WriteAllText($"{Directory.GetCurrentDirectory()}/src/XML/{f}.xml", ret);
                                    await context.tb_aux_Xml_Anbima.AddAsync(xmlAmbimaModel);
                                    await context.SaveChangesAsync();
                                }
                                else
                                {
                                    EscreverLog($"Posição do dia {DateTime.Parse(xmlAmbimaModel.DataPosicao.Substring(0, 4) + "-" + xmlAmbimaModel.DataPosicao.Substring(4, 2) + "-" + xmlAmbimaModel.DataPosicao.Substring(6, 2), CultureInfo.GetCultureInfo("pt-BR")):dd/MM/yyyy} para o CNPJ {xmlAmbimaModel.Cnpj} - {xmlAmbimaModel.Nome} já recuperado.");
                                }
                            }
                            else
                            {
                                EscreverLog($"Posição do fundo {f} não encontrado.");
                            }

                        }
                        catch
                        {
                        }
                        break;
                    }
                    else
                    {
                        retry++;
                        await Task.Delay(3000);
                        if (retry <= 5)
                        {
                            EscreverLog($"Erro ao recuperar XML do fundo {f}. Tentativa {retry}");
                        }
                        else
                        {
                            EscreverLog($"Tentativas excedidas ao recuperar XML do fundo {f}.");
                        }
                    }
                } while (!response.IsSuccessStatusCode && retry <= 5);
            });
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

    private List<string> RecuperarFidcs()
    {
        return
        [
            "AURUM FIDC",
            "AURUM FIDC MZ L",
            "BRAVA FIDC",
            "BRAVA FIDC MZ1",
            "BRAVA FIDC SR1",
            "CUPERTINO FIDC",
            "DIP FINAN11 FIC",
            "DIP FINANCI MEZ",
            "DIP FINANCI SEN",
            "DIP FINANCING11",
            "DIPFINANCING11",
            "EMUNAH FIDC",
            "EMUNAH FIDC MZ1",
            "EMUNAH FIDC MZ2",
            "EMUNAH FIDC SR1",
            "EMUNAH FIDC SR2",
            "F18 FIDC",
            "F18 FIDC MEZ",
            "F18 FIDC SEN",
            "FIDC ML BANK II",
            "FIDC MLB",
            "FIDC MLB MEZ",
            "FIDC MLB SR1",
            "GAVEA OPEN FIDC",
            "GAVEA REAL FIDC",
            "GAVEA REAL SEN1",
            "GAVEA SUL FIDC",
            "GAVEA SUL MEZA2",
            "GAVEA SUL MEZA3",
            "GAVEA SUL MEZA5",
            "GAVEA SUL MEZAN",
            "GAVEA SUL SEN10",
            "GAVEA SUL SEN11",
            "GAVEA SUL SEN12",
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
            "GPR FIDC MEZ2",
            "GPR FIDC MEZA",
            "GPR FIDC SEN",
            "GPR FIDC SEN 2",
            "GPR FIDC SEN 3",
            "ONE7LB SUBJR",
            "ONE7LP MEZ CI",
            "ONE7LP MEZ DI",
            "ONE7LP MEZ E",
            "ONE7LP MEZ F",
            "ONE7LP MEZ G",
            "ONE7LP SEN 14",
            "ONE7LP SEN 15",
            "PHD FIDC",
            "PHD FIDC MEZ",
            "PHD FIDC SEN",
            "PHD FIDC SUB",
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
            "TMOV FIDC SR",
            "PUMA FIDC SUB",
        ];
    }
}