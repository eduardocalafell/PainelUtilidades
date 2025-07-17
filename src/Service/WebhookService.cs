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
using PainelUtilidades.Utils.Extensions;
using Microsoft.EntityFrameworkCore;

public class WebhookService
{
    private readonly AppDbContext _context;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly HttpClient _client;
    private readonly IConfiguration Configuration;
    private readonly ILoggerFactory _loggerFactory;
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

    public WebhookService(AppDbContext context, IServiceScopeFactory serviceScopeFactory, ILoggerFactory loggerFactory)
    {
        _context = context;
        _scopeFactory = serviceScopeFactory;
        _loggerFactory = loggerFactory;
        _client = new HttpClient();
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

            Task.Run(() => ProcessarUnicoArquivoEstoqueSingulare(payloadModel));

            payloadModel.IsProcessado = true;
            _context.tb_aux_callback_estoque_singulare.Add(payloadModel);
            _context.SaveChanges();
        }
        else
        {
            throw new Exception("Ocorreu um erro durante a execução do processo!");
        }

        return Task.CompletedTask;
    }

    public Task RecuperarRelatorioEstoqueSingulare()
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        context.Database.SetCommandTimeout(600); // timeout de 10 minutos para recuperar a tabela inteira...
        var listaFundos = context.tb_stg_estoque_singulare_full.AsNoTracking().Select(s => s.doc_fundo.FormatarCnpj()).Distinct().ToList();
        var listaProcessados = context.tb_aux_relatorios_processados.ToList();

        var url = Configuration.GetSection("Singulare:Url").Value;
        var user = Configuration.GetSection("Singulare:Usuario").Value;
        var pswr = Configuration.GetSection("Singulare:Senha").Value;

        using HttpClient client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{user}:{pswr}")));

        var response = client.PostAsync(url + "painel/token/api", null).Result;
        var authToken = JsonConvert.DeserializeObject<SingulareApiAuthResponse>(response.Content.ReadAsStringAsync().Result).Token;

        client.DefaultRequestHeaders.Authorization = null;
        client.DefaultRequestHeaders.Add("x-api-key", authToken);

        DateTime dataInicial = new(2025, 1, 1);
        DateTime dataFinal = DateTime.Today;

        if (listaFundos.Count > 0)
        {
            do
            {
                foreach (var fundo in listaFundos)
                {
                    var dataPesquisa = ObterDataUtilD2(feriadosAnbima, dataInicial);

                    if (listaProcessados.FirstOrDefault(x => x.DataSolicitada == dataPesquisa && x.Fundo == fundo) is null)
                    {
                        Console.WriteLine($"Começando execução para a data {dataPesquisa} do fundo {fundo}", "Aviso");

                        var obj = new
                        {
                            callbackUrl = "https://m8-core-api.azurewebsites.net/Utilidades/CallbackEstoqueSingulare/",
                            cnpjFundo = fundo,
                            date = dataPesquisa,
                        };

                        var json = JsonConvert.SerializeObject(obj);
                        var payload = new StringContent(json, Encoding.UTF8, "application/json");

                        response = client.PostAsync(url + $"queue/scheduler/report/fidc-estoque", payload).Result;
                        if (response.IsSuccessStatusCode)
                        {
                            context.tb_aux_relatorios_processados.Add(new RelatoriosProcessados
                            {
                                DataSolicitada = dataPesquisa,
                                Fundo = fundo,
                            });

                            context.SaveChanges();

                            Console.WriteLine($"Inserido registo para a data {dataPesquisa} do fundo {fundo}. Aguardando 3s...", "Aviso");

                            Thread.Sleep(3000); // Aguarda um pouco antes de processar o próximo
                        }
                        else
                        {
                            var error = response.Content.ReadAsStringAsync().Result;
                            Console.WriteLine($"Erro ao processar fundo {fundo} - mensagem da singulare: {error}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Relatório para a data {dataPesquisa} e fundo {fundo} já recuperado.", "Aviso");
                    }
                }

                dataInicial = dataInicial.AddDays(1);
            } while (dataInicial <= dataFinal);
        }

        return Task.CompletedTask;
    }

    public Task ProcessarArquivosEstoqueSingulare()
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var arquivosProcessar = context.tb_aux_callback_estoque_singulare
            .Where(x => !x.IsProcessado)
            .ToList();

        if (arquivosProcessar.Count == 0)
            return Task.FromException(new Exception("Não há arquivos a serem processados."));

        foreach (var arquivo in arquivosProcessar)
        {
            try
            {
                using var ms = new MemoryStream();

                var response = _client.GetAsync(arquivo.FileLink).Result;
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Erro ao baixar arquivo: {arquivo.FileLink}");
                    continue;
                }

                using var contentStream = response.Content.ReadAsStreamAsync().Result;
                contentStream.CopyTo(ms);
                ms.Position = 0;

                using var reader = new StreamReader(ms);
                using var csv = new CsvReader(reader, new CsvConfiguration
                {
                    Delimiter = ";",
                    HasHeaderRecord = true
                });

                var estoque = csv.GetRecords<EstoqueCsv>().ToList();

                foreach (var titulo in estoque)
                {
                    var linhaEstoque = new EstoqueModel
                    {
                        nome_fundo = titulo.NM_FUNDO,
                        doc_fundo = titulo.NU_CNPJ,
                        data_fundo = titulo.DATA_FUNDO,
                        nome_originador = titulo.NOME_ORIGINADOR,
                        doc_originador = titulo.DOC_ORIGINADOR,
                        nome_cedente = titulo.NOME_CEDENTE,
                        doc_cedente = titulo.DOC_CEDENTE,
                        nome_sacado = titulo.NOME_SACADO,
                        doc_sacado = titulo.DOC_SACADO,
                        seu_numero = titulo.SEU_NUMERO,
                        nu_documento = titulo.NU_DOCUMENTO,
                        tipo_recebivel = titulo.TIPO_RECEBIVEL,
                        valor_nominal = titulo.VALOR_NOMINAL,
                        valor_presente = titulo.VALOR_PRESENTE,
                        valor_aquisicao = titulo.VALOR_AQUISICAO,
                        valor_pdd = titulo.VALOR_PDD,
                        faixa_pdd = titulo.FAIXA_PDD,
                        data_referencia = titulo.DATA_REFERENCIA,
                        data_vencimento_original = titulo.DATA_VENCIMENTO_ORIGINAL,
                        data_vencimento_ajustada = titulo.DATA_VENCIMENTO_AJUSTADA,
                        data_emissao = titulo.DATA_EMISSAO,
                        data_aquisicao = titulo.DATA_AQUISICAO,
                        prazo = titulo.PRAZO,
                        prazo_atual = titulo.PRAZO_ATUAL,
                        situacao_recebivel = titulo.SITUACAO_RECEBIVEL,
                        taxa_cessao = titulo.TAXA_CESSAO,
                        tx_recebivel = titulo.TX_RECEBIVEL,
                        coobrigacao = titulo.COOBRIGACAO
                    };

                    var updateEstoque = context.tb_stg_estoque_singulare_full
                        .FirstOrDefault(x => x.seu_numero == linhaEstoque.seu_numero);

                    if (updateEstoque == null)
                    {
                        context.tb_stg_estoque_singulare_full.Add(linhaEstoque);
                    }
                    else
                    {
                        context.Entry(updateEstoque).CurrentValues.SetValues(linhaEstoque);
                        context.tb_stg_estoque_singulare_full.Update(updateEstoque);
                    }
                }

                arquivo.IsProcessado = true;
                context.tb_aux_callback_estoque_singulare.Update(arquivo);
                context.SaveChanges();

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao processar arquivo {arquivo.FileLink}: {ex.Message}");
                return Task.FromException(ex);
            }
        }

        return Task.CompletedTask;
    }

    public Task ProcessarUnicoArquivoEstoqueSingulare(WebhookModel retornoWebhook)
    {
        using var scope = _scopeFactory.CreateScope();
        var log = _loggerFactory.CreateLogger("ProcessarUnicoArquivoEstoqueSingulare");
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        try
        {
            using var ms = new MemoryStream();

            var response = _client.GetAsync(retornoWebhook.FileLink).Result;
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Erro ao baixar arquivo: {retornoWebhook.FileLink}");
                throw new Exception("Erro ao baixar o arquivo solicitado!");
            }

            using var contentStream = response.Content.ReadAsStreamAsync().Result;
            contentStream.CopyTo(ms);
            ms.Position = 0;

            using var reader = new StreamReader(ms);
            using var csv = new CsvReader(reader, new CsvConfiguration
            {
                Delimiter = ";",
                HasHeaderRecord = true
            });

            var estoque = csv.GetRecords<EstoqueCsv>().ToList();
            log.LogInformation("Começando integração estoque...");

            foreach (var titulo in estoque)
            {
                var linhaEstoque = new EstoqueModel
                {
                    nome_fundo = titulo.NM_FUNDO,
                    doc_fundo = titulo.NU_CNPJ,
                    data_fundo = titulo.DATA_FUNDO,
                    nome_originador = titulo.NOME_ORIGINADOR,
                    doc_originador = titulo.DOC_ORIGINADOR,
                    nome_cedente = titulo.NOME_CEDENTE,
                    doc_cedente = titulo.DOC_CEDENTE,
                    nome_sacado = titulo.NOME_SACADO,
                    doc_sacado = titulo.DOC_SACADO,
                    seu_numero = titulo.SEU_NUMERO,
                    nu_documento = titulo.NU_DOCUMENTO,
                    tipo_recebivel = titulo.TIPO_RECEBIVEL,
                    valor_nominal = titulo.VALOR_NOMINAL,
                    valor_presente = titulo.VALOR_PRESENTE,
                    valor_aquisicao = titulo.VALOR_AQUISICAO,
                    valor_pdd = titulo.VALOR_PDD,
                    faixa_pdd = titulo.FAIXA_PDD,
                    data_referencia = titulo.DATA_REFERENCIA,
                    data_vencimento_original = titulo.DATA_VENCIMENTO_ORIGINAL,
                    data_vencimento_ajustada = titulo.DATA_VENCIMENTO_AJUSTADA,
                    data_emissao = titulo.DATA_EMISSAO,
                    data_aquisicao = titulo.DATA_AQUISICAO,
                    prazo = titulo.PRAZO,
                    prazo_atual = titulo.PRAZO_ATUAL,
                    situacao_recebivel = titulo.SITUACAO_RECEBIVEL,
                    taxa_cessao = titulo.TAXA_CESSAO,
                    tx_recebivel = titulo.TX_RECEBIVEL,
                    coobrigacao = titulo.COOBRIGACAO
                };

                context.tb_stg_estoque_singulare_full.Add(linhaEstoque);
                context.SaveChanges();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao processar arquivo {retornoWebhook.FileLink}: {ex.Message}");
            return Task.FromException(ex);
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