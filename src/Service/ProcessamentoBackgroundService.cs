public class ProcessamentoBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly Queue<Func<IServiceProvider, Task>> _filaProcessamento = new();

    public ProcessamentoBackgroundService(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    public void AdicionarProcesso(Func<IServiceProvider, Task> processo)
    {
        lock (_filaProcessamento)
        {
            _filaProcessamento.Enqueue(processo);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            Func<IServiceProvider, Task> processo = null;

            lock (_filaProcessamento)
            {
                if (_filaProcessamento.Count > 0)
                    processo = _filaProcessamento.Dequeue();
            }

            if (processo != null)
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var serviceProvider = scope.ServiceProvider;

                try
                {
                    await processo(serviceProvider);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro no processamento: {ex.Message}");
                }
            }

            await Task.Delay(1000, stoppingToken); // Evita loop infinito consumindo CPU
        }
    }
}