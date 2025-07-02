using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using SimpleFeed.Application.DTOs;
using SimpleFeed.Application.Interfaces;
using SimpleFeed.Domain.Entities;
using SimpleFeed.Web.Models;

namespace SimpleFeed.Application.Services
{
    public class ReportsService
    {
        private readonly HttpClient _httpClient;
        private readonly IReportsRepository _reportsRepository;
        private readonly string? _apiKey;
        public ReportsService(HttpClient httpClient, IConfiguration configuration, IReportsRepository reportsRepository, IOptions<OpenAiSettings> settings)
        {
            _httpClient = httpClient;
            _apiKey = settings.Value.ApiKey;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            _reportsRepository = reportsRepository;
        }

        public async Task<DetailReportIA> GerarRelatorioAsync(OpenAiRequestDTO dto, int client_Id)
        {
            try
            {
                // 1. Buscar dados
                var (clientId, formName, feedbacks, fields) = await _reportsRepository.GetReportDataAsync(
                    dto.FormId, dto.DataInicio, dto.DataFim, client_Id);

                var fieldMap = fields.ToDictionary(f => f.Id, f => new { f.Name, f.Type });
                var respostas = new List<string>();

                foreach (var fb in feedbacks)
                {
                    var answersArray = JsonSerializer.Deserialize<JsonArray>(fb.Answers);
                    if (answersArray == null) continue;

                    foreach (var entry in answersArray)
                    {
                        int idCampo = entry?["id_form_field"]?.GetValue<int>() ?? 0;
                        string valor = entry?["value"]?.ToString() ?? "";

                        if (fieldMap.TryGetValue(idCampo, out var campo))
                            respostas.Add($"Campo: {campo.Name} (tipo: {campo.Type}) - Valor: {valor}");
                    }
                }

                // 2. Construir prompt
                var periodo = string.Empty;

                if (dto.DataInicio > DateTime.MinValue && dto.DataFim > DateTime.MinValue)
                {
                    periodo = $"\n O período de análise é de {dto.DataInicio:dd/MM/yyyy} a {dto.DataFim:dd/MM/yyyy}.";
                }

                var prompt = $@"
                Você é um especialista em análise de feedbacks de clientes.

                O estabelecimento é: {dto.ContextoNegocio}.{periodo}

                Abaixo estão os feedbacks coletados. Cada linha contém o título do campo (pergunta), seu tipo, e a resposta dada por um cliente. Use essas informações para entender corretamente o contexto de cada resposta.

                Exemplo:
                'Como avalia o atendimento?': (tipo: rating) → 5
                'Comentário sobre a experiência': (tipo: text) → ""Foi ótimo""

                Com base nos dados abaixo, escreva um relatório dividido claramente em três seções com os seguintes marcadores:

                [PONTOS_POSITIVOS]  
                Liste os principais pontos positivos observados com base nos feedbacks.  
                [PONTOS_MELHORIA]  
                Liste as oportunidades de melhoria identificadas.  
                [RESUMO_GERAL]  
                Escreva uma conclusão final de até 3 parágrafos, consolidando os principais achados.

                Apenas envie os três blocos com os marcadores indicados, sem reexibir os dados brutos.
                Ignore dados como CPF, e-mail ou dados irrelevantes.

                Feedbacks:
                {string.Join("\n", respostas.Distinct())}
                ";


                // 3. Chamar a OpenAI
                var requestBody = new
                {
                    model = "gpt-3.5-turbo",
                    messages = new[]
                    {
                new { role = "system", content = "Você é um gerador de relatórios profissionais com base em avaliações coletadas de clientes." },
                new { role = "user", content = prompt }
                },
                    temperature = 0.7
                };

                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
                var result = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new Exception("Erro ao chamar OpenAI: " + result);

                var doc = JsonDocument.Parse(result);
                var texto = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString()
                    ?.Trim() ?? "Sem conteúdo gerado.";

                // Inserir log de auditoria usando método de log
                var logDetails = new
                {
                    form_id = dto.FormId,
                    form_name = formName
                };

                var report = new RelatorioDTO
                {
                    PontosPositivos = GetBloco("[PONTOS_POSITIVOS]", texto),
                    PontosMelhoria = GetBloco("[PONTOS_MELHORIA]", texto),
                    ResumoGeral = GetBloco("[RESUMO_GERAL]", texto)
                };

                string periodoSolicitado;
                if (dto.DataInicio > DateTime.MinValue && dto.DataFim > DateTime.MinValue)
                    periodoSolicitado = $"Período analisado:  {dto.DataInicio:dd/MM/yyyy} à {dto.DataFim:dd/MM/yyyy}";
                else
                    periodoSolicitado = "Todo o período";


                int idReport = await _reportsRepository.LogClientActionAsync(
                    clientId,
                    dto.FormId,
                    Domain.Enums.ClientActionType.AiAnalysis,
                    logDetails,
                    report,
                    periodoSolicitado);

                return await _reportsRepository.GetReportByIdAsync(idReport);
            }
            catch (Exception ex)
            {
                // Aqui você pode logar o erro, lançar novamente ou retornar um DTO com mensagem de erro
                // Exemplo: throw; ou logar e lançar novamente
                throw new Exception("Erro ao gerar relatório: " + ex.Message, ex);
            }
        }


        string GetBloco(string marcador, string textoTotal)
        {
            var inicio = textoTotal.IndexOf(marcador);
            if (inicio == -1) return "";

            var fim = textoTotal.IndexOf("[", inicio + marcador.Length);
            if (fim == -1) fim = textoTotal.Length;

            return textoTotal.Substring(inicio + marcador.Length, fim - inicio - marcador.Length).Trim();
        }

        public async Task<List<ReportsIAs>> GetReportsIaAsync(int formId, DateTime? startDate, DateTime? endDate)
        {
            return await _reportsRepository.GetReportsIaAsync(formId, startDate, endDate);
        }

        public async Task<DetailReportIA> GetReportByIdAsync(int reportId)
        {
            return await _reportsRepository.GetReportByIdAsync(reportId);
        }
    }
}