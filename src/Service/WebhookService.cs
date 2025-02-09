namespace ConsultaCnpjReceita.Service;

using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using ConsultaCnpjReceita.Model;
using Data.AppDbContext;
using Newtonsoft.Json;
using WebConsultaCnpjReceita.Models;
using CsvHelper;
using CsvHelper.Configuration;

public class WebhookService
{
    private readonly AppDbContext _context;
    private readonly IServiceProvider _provider;
    private readonly IConfiguration Configuration;
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

    public WebhookService(AppDbContext context, IServiceProvider provider)
    {
        _context = context;
        _provider = provider;
        Configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json")
            .Build();
    }

    public Task CallbackEstoqueSingulare(WebhookPayload payload)
    {
        if (payload is not null)
        {
            var payloadModel = new WebhookModel
            {
                EventType = payload.EventType,
                FileLink = payload.Data.FileLink,
                JobId = payload.JobId.ToString(),
                WebhookId = payload.WebhookId.ToString(),
                IsProcessado = false,
            };

            _context.tb_aux_callback_estoque_singulare.Add(payloadModel);
            _context.SaveChanges();
        }
        else
        {
            throw new Exception("Ocorreu um erro durante a execução do processo!");
        }

        return Task.CompletedTask;
    }

    public async Task RecuperarRelatorioEstoqueSingulare()
    {
        using var scope = _provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var listaFundos = context.tb_fato_carteira.GroupBy(g => g.cnpj_fundo).Select(s => s.Key).ToList();
        var listaProcessados = context.tb_aux_relatorios_processados.ToList();

        var url = Configuration.GetSection("Singulare:Url").Value;
        var user = Configuration.GetSection("Singulare:Usuario").Value;
        var pswr = Configuration.GetSection("Singulare:Senha").Value;

        using HttpClient client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{user}:{pswr}")));

        var response = await client.PostAsync(url + "painel/token/api", null);
        var authToken = JsonConvert.DeserializeObject<SingulareApiAuthResponse>(await response.Content.ReadAsStringAsync()).Token;

        client.DefaultRequestHeaders.Authorization = null;
        client.DefaultRequestHeaders.Add("x-api-key", authToken);

        DateTime dataInicial = DateTime.Today;
        DateTime dataFinal = new DateTime(2024, 1, 1);

        if (listaFundos.Count > 0)
        {
            do
            {
                foreach (var fundo in listaFundos)
                {
                    var dataPesquisa = ObterDataUtilD2(feriadosAnbima, dataFinal);

                    if (!listaProcessados.Any(x => x.DataSolicitada == dataPesquisa && x.Fundo == fundo))
                    {
                        Debug.WriteLine($"Começando execução para a data {dataPesquisa}", "Aviso");

                        var obj = new
                        {
                            callbackUrl = "https://m8-core-api.azurewebsites.net/Utilidades/CallbackEstoqueSingulare/",
                            cnpjFundo = fundo,
                            date = dataPesquisa,
                        };

                        var json = JsonConvert.SerializeObject(obj);
                        var payload = new StringContent(json, Encoding.UTF8, "application/json");

                        response = await client.PostAsync(url + $"queue/scheduler/report/fidc-estoque", payload);
                        if (response.IsSuccessStatusCode)
                        {
                            context.tb_aux_relatorios_processados.Add(new RelatoriosProcessados
                            {
                                DataSolicitada = dataPesquisa,
                                Fundo = fundo,
                            });

                            await context.SaveChangesAsync();

                            await Task.Delay(3000); // Aguarda um pouco antes de processar o próximo
                        }
                        else
                        {
                            var error = await response.Content.ReadAsStringAsync();
                            Console.WriteLine($"Erro ao processar fundo {fundo}: {error}");
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"Relatório para a data {dataPesquisa} e fundo {fundo} já recuperado.", "Aviso");
                    }
                }

                dataFinal = dataFinal.AddDays(1);
            } while (dataFinal < dataInicial);
        }
    }

    public Task ProcessarArquivosEstoqueSingulare()
    {
        using (var scope = _provider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            HttpClient httpClient = new HttpClient();

            var arquivosProcessar = context.tb_aux_callback_estoque_singulare.Where(x => !x.IsProcessado).ToList();

            if (arquivosProcessar.Count > 0)
            {
                MemoryStream ms = new MemoryStream();

                foreach (var arquivo in arquivosProcessar)
                {
                    var content = httpClient.GetAsync(arquivo.FileLink).Result.Content.ReadAsStream();
                    content.CopyTo(ms);
                    ms.Position = 0;

                    using var reader = new StreamReader(ms);
                    using var csv = new CsvReader(reader, new CsvConfiguration
                    {
                        Delimiter = ";",
                        HasHeaderRecord = true,
                    });

                    var estoque = csv.GetRecords<EstoqueCsv>().ToList();

                    foreach (var titulo in estoque)
                    {
                        var linhaEstoque = new EstoqueModel
                        {
                            nome_fundo = titulo.nomeFundo,
                            doc_fundo = titulo.docFundo,
                            data_fundo = titulo.dataFundo,
                            nome_originador = titulo.nomeOriginador,
                            doc_originador = titulo.docOriginador,
                            nome_cedente = titulo.nomeCedente,
                            doc_cedente = titulo.docCedente,
                            nome_sacado = titulo.nomeSacado,
                            doc_sacado = titulo.docSacado,
                            seu_numero = titulo.seuNumero,
                            nu_documento = titulo.numeroDocumento,
                            tipo_recebivel = titulo.tipoRecebivel,
                            valor_nominal = titulo.valorNominal,
                            valor_presente = titulo.valorPresente,
                            valor_aquisicao = titulo.valorAquisicao,
                            valor_pdd = titulo.valorPdd,
                            faixa_pdd = titulo.faixaPdd,
                            data_referencia = titulo.dataReferencia,
                            data_vencimento_original = titulo.dataVencimentoOriginal,
                            data_vencimento_ajustada = titulo.dataVencimentoAjustada,
                            data_emissao = titulo.dataEmissao,
                            data_aquisicao = titulo.dataAquisicao,
                            prazo = titulo.prazo,
                            prazo_atual = titulo.prazoAnual,
                            situacao_recebivel = titulo.situacaoRecebivel,
                            taxa_cessao = titulo.taxaCessao,
                            tx_recebivel = titulo.taxaRecebivel,
                            coobrigacao = titulo.coobrigacao
                        };

                        if (!context.tb_stg_estoque_singulare_full.Select(x => x.seu_numero).Contains(linhaEstoque.seu_numero))
                        {
                            context.tb_stg_estoque_singulare_full.Add(linhaEstoque);
                        }
                        else
                        {
                            var updateEstoque = context.tb_stg_estoque_singulare_full.FirstOrDefault(x => x.seu_numero == linhaEstoque.seu_numero);

                            if (updateEstoque != null)
                            {
                                // Atualiza as propriedades desejadas
                                updateEstoque.nome_fundo = linhaEstoque.nome_fundo;
                                updateEstoque.doc_fundo = linhaEstoque.doc_fundo;
                                updateEstoque.data_fundo = linhaEstoque.data_fundo;
                                updateEstoque.nome_originador = linhaEstoque.nome_originador;
                                updateEstoque.doc_originador = linhaEstoque.doc_originador;
                                updateEstoque.nome_cedente = linhaEstoque.nome_cedente;
                                updateEstoque.doc_cedente = linhaEstoque.doc_cedente;
                                updateEstoque.nome_sacado = linhaEstoque.nome_sacado;
                                updateEstoque.doc_sacado = linhaEstoque.doc_sacado;
                                updateEstoque.nu_documento = linhaEstoque.nu_documento;
                                updateEstoque.tipo_recebivel = linhaEstoque.tipo_recebivel;
                                updateEstoque.valor_nominal = linhaEstoque.valor_nominal;
                                updateEstoque.valor_presente = linhaEstoque.valor_presente;
                                updateEstoque.valor_aquisicao = linhaEstoque.valor_aquisicao;
                                updateEstoque.valor_pdd = linhaEstoque.valor_pdd;
                                updateEstoque.faixa_pdd = linhaEstoque.faixa_pdd;
                                updateEstoque.data_referencia = linhaEstoque.data_referencia;
                                updateEstoque.data_vencimento_original = linhaEstoque.data_vencimento_original;
                                updateEstoque.data_vencimento_ajustada = linhaEstoque.data_vencimento_ajustada;
                                updateEstoque.data_emissao = linhaEstoque.data_emissao;
                                updateEstoque.data_aquisicao = linhaEstoque.data_aquisicao;
                                updateEstoque.prazo = linhaEstoque.prazo;
                                updateEstoque.prazo_atual = linhaEstoque.prazo_atual;
                                updateEstoque.situacao_recebivel = linhaEstoque.situacao_recebivel;
                                updateEstoque.taxa_cessao = linhaEstoque.taxa_cessao;
                                updateEstoque.tx_recebivel = linhaEstoque.tx_recebivel;
                                updateEstoque.coobrigacao = linhaEstoque.coobrigacao;
                            }

                            context.tb_stg_estoque_singulare_full.Update(updateEstoque);
                        }
                    }

                    reader.Close();

                    arquivo.IsProcessado = true;

                    context.tb_aux_callback_estoque_singulare.Update(arquivo);
                    context.SaveChanges();
                }
            }
        }

        return Task.CompletedTask;
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

}