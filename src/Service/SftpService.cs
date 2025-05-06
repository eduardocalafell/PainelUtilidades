namespace ConsultaCnpjReceita.Service;

using Data.AppDbContext;
using System;
using System.IO;
using Renci.SshNet;
using Renci.SshNet.Common;
using CsvHelper;
using CsvHelper.Configuration;
using ConsultaCnpjReceita.Model;

public class SftpService
{
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

        string host = "sftp.singulare.com.br"; // Endereço do servidor SFTP
        string username = "usr_m8"; // Usuário do SFTP
        string privateKeyPath = @$"{Environment.CurrentDirectory}\usr_m8"; // Caminho da chave privada

        try
        {
            // Carrega a chave privada
            using (var keyFile = new PrivateKeyFile(privateKeyPath))
            {
                var keyFiles = new[] { keyFile };
                var authMethod = new PrivateKeyAuthenticationMethod(username, keyFiles);

                // Cria a conexão SFTP
                var connectionInfo = new ConnectionInfo(host, 22, username, authMethod);

                using (var sftp = new SftpClient(connectionInfo))
                {
                    sftp.Connect();
                    Console.WriteLine("Conectado ao SFTP!");

                    // Lista arquivos na pasta raiz do SFTP
                    var pastasFundo = sftp.ListDirectory("/IN/Relatorios_Estoque").Where(x => x.IsDirectory).Skip(2).ToList();

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
                                Console.WriteLine($"Processando data: {arquivoEstoque.Name}");

                                if (context.tb_aux_relatorios_processados.FirstOrDefault(x => x.NomeArquivo == arquivoEstoque.FullName) is null)
                                {
                                    using MemoryStream ms = new MemoryStream();
                                    sftp.DownloadFile(arquivoEstoque.FullName, ms);
                                    ms.Position = 0;

                                    using var reader = new StreamReader(ms);
                                    using var csv = new CsvReader(reader, new CsvConfiguration
                                    {
                                        Delimiter = ";",
                                        HasHeaderRecord = true
                                    });

                                    // File.WriteAllBytes(Environment.CurrentDirectory + "/src/arquivo.csv", ms.ToArray());
                                    var estoque = csv.GetRecords<EstoqueCsv>().ToList();

                                    var novosRegistros = new List<EstoqueModel>();

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
                                        };

                                        Console.WriteLine($"Linha adicionada: {linhaEstoque.nome_cedente} - {linhaEstoque.nu_documento}");
                                        novosRegistros.Add(linhaEstoque);
                                    }

                                    if (novosRegistros.Count != 0)
                                    {
                                        context.tb_stg_estoque_singulare_full.AddRange(novosRegistros);

                                        context.tb_aux_relatorios_processados.Add(new RelatoriosProcessados
                                        {
                                            NomeArquivo = arquivoEstoque.FullName,
                                            DataSolicitada = pastaData.Name,
                                            Fundo = pastaFundo.Name
                                        });

                                        context.SaveChanges();
                                    }
                                }
                            }
                        }

                        sftp.Disconnect();
                        Console.WriteLine("Desconectado!");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro: {ex.Message}");
            await Task.FromException(ex);
        }
    }

}