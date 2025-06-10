using System;
using System.Threading.Tasks;
using SimpleFeed.Application.DTOs;
using SimpleFeed.Application.Interfaces;

public class BillingSummaryService
{
    private readonly IBillingSummaryRepository _repository;

    public BillingSummaryService(IBillingSummaryRepository repository)
    {
        _repository = repository;
    }

    public async Task<BillingSummaryDto?> GetSummaryAsync(int clientId, DateTime referenceMonth)
    {
        var now = DateTime.UtcNow;
        var inicioDoMesAtual = new DateTime(now.Year, now.Month, 1);
        var inicioDoMesSolicitado = new DateTime(referenceMonth.Year, referenceMonth.Month, 1);

        if (inicioDoMesSolicitado == inicioDoMesAtual)
        {
            // fatura do mês atual (em aberto, query dinâmica)
            return await _repository.GetBillingSummaryAsync(clientId, referenceMonth);
        }
        else
        {
            // fatura fechada (consultar tabela billing_summary)
            return await _repository.GetBillingSummaryFromStoredAsync(clientId, referenceMonth);
        }
    }

    public async Task<bool> MigratePlanAsync(int clientId, int newPlanId)
    {
        return await _repository.MigratePlanAsync(clientId, newPlanId);
    }

}
