namespace ConsultaCnpjReceita.Service;

using Data.AppDbContext;
using System;
using System.IO;
using Renci.SshNet;
using CsvHelper;
using CsvHelper.Configuration;
using ConsultaCnpjReceita.Model;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Collections.Concurrent;

public class SftpService
{
    const int BATCH_SIZE = 5;
    private readonly AppDbContext _context;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly HttpClient _client;
    private readonly IConfiguration Configuration;

    public SftpService(AppDbContext context, IServiceScopeFactory serviceScopeFactory)
    {
        _context = context;
        _scopeFactory = serviceScopeFactory;
        _client = new HttpClient();
        Configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json")
            .Build();
    }

    public async Task RecuperarDadosSftpSingulareEstoque()
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var arquivosProcessar = new List<(string file, long fileSize, string fundo, string data)>();

        string host = "sftp.singulare.com.br"; // Endereço do servidor SFTP
        string username = "usr_m8"; // Usuário do SFTP
        string privateKeyPath = @$"{Environment.CurrentDirectory}/usr_m8"; // Caminho da chave privada

        try
        {
            // Carrega a chave privada
            using var keyFile = new PrivateKeyFile(privateKeyPath);
            var keyFiles = new[] { keyFile };
            var authMethod = new PrivateKeyAuthenticationMethod(username, keyFiles);

            // Cria a conexão SFTP
            var connectionInfo = new ConnectionInfo(host, 22, username, authMethod);
            connectionInfo.Timeout = new TimeSpan(1, 0, 0, 0);

            using var sftp = new SftpClient(connectionInfo);
            sftp.Connect();
            Console.WriteLine("Conectado ao SFTP!");

            // Lista arquivos na pasta raiz do SFTP
            var pastasFundo = sftp.ListDirectory("/IN/Relatorios_Estoque").Where(x => x.IsDirectory).Skip(2).ToList();
            var arquivosIntegrados = context.tb_aux_relatorios_processados.ToList();

            foreach (var pastaFundo in pastasFundo)
            {
                Console.WriteLine($"Processando pasta: {pastaFundo.Name}");
                var pastasData = sftp.ListDirectory(pastaFundo.FullName).Where(x => x.IsDirectory).Skip(2).ToList();

                foreach (var pastaData in pastasData)
                {
                    Console.WriteLine($"Processando data: {pastaData.Name}");
                    var arquivosEstoque = sftp.ListDirectory(pastaData.FullName).Where(x => !x.IsDirectory).ToList();

                    foreach (var arquivoEstoque in arquivosEstoque)
                    {
                        if (arquivosIntegrados.FirstOrDefault(x => x.NomeArquivo == arquivoEstoque.FullName) is null)
                        {
                            arquivosProcessar.Add((arquivoEstoque.FullName, arquivoEstoque.Length, pastaFundo.Name, pastaData.Name));
                            Console.WriteLine($"Arquivo na fila: {arquivoEstoque.FullName}");
                        }
                        else Console.WriteLine($"Arquivo já integrado: {arquivoEstoque.FullName} | Procurando próximo arquivo!");
                    }
                }
            }

            // Processando arquivos na lista
            foreach (var (file, fileSize, fundo, data) in arquivosProcessar.OrderBy(o => o.fileSize))
            {
                var connectionInfoDownload = new ConnectionInfo(host, 22, username, authMethod);
                connectionInfoDownload.Timeout = new TimeSpan(1, 0, 0, 0);

                using var sftpDownload = new SftpClient(connectionInfoDownload);

                Console.WriteLine($"Processando arquivo: {file}");

                if (!sftpDownload.IsConnected) sftpDownload.Connect();
                else Console.WriteLine($"sftpDownload já está conectado! Baixando arquivo: {file}");

                using MemoryStream ms = new MemoryStream();
                sftpDownload.OperationTimeout = new TimeSpan(1, 0, 0, 0);
                sftpDownload.DownloadFile(file, ms);
                ms.Position = 0;

                Console.WriteLine("Arquivo baixado com sucesso!");

                using var reader = new StreamReader(ms);
                using var csv = new CsvReader(reader, new CsvConfiguration
                {
                    Delimiter = ";",
                    HasHeaderRecord = true
                });

                // File.WriteAllBytes(Environment.CurrentDirectory + "/src/arquivo.csv", ms.ToArray());
                var estoque = csv.GetRecords<EstoqueCsv>().ToList();
                var listaEstoqueModel = estoque.Select(titulo => new EstoqueModel
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
                    seu_numero = titulo.NU_DOCUMENTO,
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
                });

                context.tb_stg_estoque_singulare_full.AddRange(listaEstoqueModel);

                context.tb_aux_relatorios_processados.Add(new RelatoriosProcessados
                {
                    NomeArquivo = file,
                    TamanhoArquivo = (fileSize / 1000000).ToString(),
                    DataSolicitada = data,
                    Fundo = fundo
                });

                context.SaveChanges();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro: {ex.Message}");
            await Task.FromException(ex);
        }
    }


    public async Task RecuperarDadosSftpSingulareEstoqueParalelo()
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        string host = "sftp.singulare.com.br"; // Endereço do servidor SFTP
        string username = "usr_m8"; // Usuário do SFTP
        string privateKeyPath = @$"{Environment.CurrentDirectory}/usr_m8"; // Caminho da chave privada

        try
        {
            // Carrega a chave privada
            using var keyFile = new PrivateKeyFile(privateKeyPath);
            var keyFiles = new[] { keyFile };
            var authMethod = new PrivateKeyAuthenticationMethod(username, keyFiles);

            // Cria a conexão SFTP
            var connectionInfo = new ConnectionInfo(host, 22, username, authMethod);

            connectionInfo.Timeout = new TimeSpan(1, 0, 0, 0);
            connectionInfo.MaxSessions = 5;

            var arquivosParaProcessar = new List<(string Fundo, string Data, string CaminhoArquivo, string TamanhoArquivo)>();

            using (var sftp = new SftpClient(connectionInfo))
            {
                sftp.Connect();
                Console.WriteLine("Conectado ao SFTP!");

                var pastasFundo = sftp.ListDirectory("/IN/Relatorios_Estoque").Where(x => x.IsDirectory).Skip(2).ToList();
                var arquivosIntegrados = context.tb_aux_relatorios_processados.ToList();

                foreach (var pastaFundo in pastasFundo)
                {
                    var pastasData = sftp.ListDirectory(pastaFundo.FullName).Where(x => x.IsDirectory).Skip(2).ToList();

                    foreach (var pastaData in pastasData)
                    {
                        var arquivosEstoque = sftp.ListDirectory(pastaData.FullName).Where(x => !x.IsDirectory).ToList();

                        foreach (var arquivoEstoque in arquivosEstoque)
                        {
                            if (arquivosIntegrados.FirstOrDefault(x => x.NomeArquivo == arquivoEstoque.FullName) == null)
                            {
                                Console.WriteLine($"Arquivo adicionado a fila: {arquivoEstoque.FullName}");
                                arquivosParaProcessar.Add((pastaFundo.Name, pastaData.Name, arquivoEstoque.FullName, (arquivoEstoque.Length / 1000000).ToString()));
                            }
                        }
                    }
                }

                sftp.Disconnect();
            }

            // Processamento paralelo
            for (int i = 0; i < arquivosParaProcessar.Count; i += BATCH_SIZE)
            {
                var batch = arquivosParaProcessar.Skip(i).Take(BATCH_SIZE).ToList();

                var batchResults = new ConcurrentBag<(List<EstoqueModel> Estoques, RelatoriosProcessados MetaInfo)>();
                var semaphore = new SemaphoreSlim(BATCH_SIZE);
                var tasks = new List<Task>();

                foreach (var file in batch)
                {
                    await semaphore.WaitAsync();

                    var task = Task.Run(async () =>
                    {
                        try
                        {
                            using var keyFile = new PrivateKeyFile(privateKeyPath);
                            var keyFiles = new[] { keyFile };
                            var authMethod = new PrivateKeyAuthenticationMethod(username, keyFiles);
                            var connectionInfo = new ConnectionInfo(host, 22, username, authMethod)
                            {
                                Timeout = new TimeSpan(1, 0, 0, 0)
                            };

                            using var client = new SftpClient(connectionInfo);
                            client.Connect();

                            using var ms = new MemoryStream();
                            Console.WriteLine($"Baixando arquivo: {file.CaminhoArquivo}");
                            client.DownloadFile(file.CaminhoArquivo, ms);
                            client.Disconnect();
                            Console.WriteLine($"Arquivo baixado com sucesso: {file.CaminhoArquivo}");

                            ms.Position = 0;
                            using var reader = new StreamReader(ms);
                            using var csv = new CsvReader(reader, new CsvConfiguration
                            {
                                Delimiter = ";",
                                HasHeaderRecord = true
                            });

                            var estoque = csv.GetRecords<EstoqueCsv>().Select(titulo => new EstoqueModel
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
                                seu_numero = titulo.NU_DOCUMENTO,
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
                            }).ToList();

                            var metainfo = new RelatoriosProcessados
                            {
                                NomeArquivo = file.CaminhoArquivo,
                                TamanhoArquivo = file.TamanhoArquivo,
                                DataSolicitada = file.Data,
                                Fundo = file.Fundo
                            };

                            batchResults.Add((estoque, metainfo));
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Erro ao processar {file.CaminhoArquivo}: {ex.Message}");
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    });

                    tasks.Add(task);
                }

                await Task.WhenAll(tasks);

                // Persistência no banco de dados — após os downloads deste lote
                foreach (var (estoques, metainfo) in batchResults)
                {
                    context.tb_stg_estoque_singulare_full.AddRange(estoques);
                    context.tb_aux_relatorios_processados.Add(metainfo);
                }

                await context.SaveChangesAsync();
                Console.WriteLine($"Lote de {batchResults.Count} arquivos salvo com sucesso no banco.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro: {ex.Message}");
            await Task.FromException(ex);
        }
    }
}