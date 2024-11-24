namespace ConsultaCnpjReceita.Service;

using Data.AppDbContext;
using WebConsultaCnpjReceita.Models;

public class WebhookService
{
    private readonly AppDbContext _context;

    public WebhookService(AppDbContext context)
    {
        _context = context;
    }

    public Task CallbackEstoqueSingulare(WebhookPayload payload)
    {
        if (payload is not null)
        {
            var payloadModel = new WebhookModel
            {
                EventType = payload.EventType,
                FileLink = payload.Data.FileLink,
                JobId = payload.JobId,
                WebhookId = payload.WebhookId,
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

}