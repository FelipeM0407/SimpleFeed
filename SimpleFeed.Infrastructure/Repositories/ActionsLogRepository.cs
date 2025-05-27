using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Npgsql;
using SimpleFeed.Application.DTOs;
using SimpleFeed.Application.Interfaces;

namespace SimpleFeed.Infrastructure.Repositories
{
    public class ActionsLogRepository : IActionsLogRepository
    {
        private readonly string _connectionString;

        public ActionsLogRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<List<ActionLogResultDto>> GetLogsAsync(ActionLogFilterDto filter)
        {
            var result = new List<ActionLogResultDto>();
            try
            {
                var query = @"
            SELECT cal.timestamp, at.display_name, at.description, cal.details
            FROM client_action_logs cal
            INNER JOIN action_types at ON at.id = cal.action_id
            WHERE cal.client_id = @ClientId";

                if (filter.ActionTypes != null && filter.ActionTypes.Any())
                    query += $" AND cal.action_id = ANY(@Actions)";

                if (filter.StartDate.HasValue)
                    query += " AND cal.timestamp >= @StartDate";

                if (filter.EndDate.HasValue)
                    query += " AND cal.timestamp <= @EndDate";

                query += " ORDER BY cal.timestamp DESC";

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                using var command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@ClientId", filter.ClientId);

                if (filter.ActionTypes != null && filter.ActionTypes.Any())
                    command.Parameters.AddWithValue("@Actions", filter.ActionTypes.Select(x => (int)x).ToArray());

                if (filter.StartDate.HasValue)
                    command.Parameters.AddWithValue("@StartDate", filter.StartDate.Value);

                if (filter.EndDate.HasValue)
                    command.Parameters.AddWithValue("@EndDate", filter.EndDate.Value);

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var action = reader.GetString(1);
                    var description = reader.GetString(2);
                    var detailsRaw = reader.IsDBNull(3) ? null : reader.GetString(3);

                    var obs = GerarObservacao(detailsRaw, action);

                    result.Add(new ActionLogResultDto
                    {
                        Timestamp = reader.GetDateTime(0),
                        Action = action,
                        Description = description,
                        Observations = obs
                    });
                }
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao buscar logs de ações.", ex);
            }

        }

        private string GerarObservacao(string? detailsJson, string action)
        {
            if (string.IsNullOrWhiteSpace(detailsJson))
            {
                if (action.Contains("Estilo"))
                    return "Estilo do formulário editado manualmente";
                return "Configurações do formulário editadas manualmente";
            }

            var doc = JsonDocument.Parse(detailsJson);
            var root = doc.RootElement;

            return action switch
            {
                "Criação de Formulário" =>
                    root.TryGetProperty("form_name", out var formName)
                        ? $"Formulário criado com o nome \"{formName.GetString()}\""
                        : "Formulário criado",

                "Duplicação de Formulário" =>
                    root.TryGetProperty("original_name_form", out var original) && root.TryGetProperty("new_form_name", out var novo)
                        ? $"Formulário \"{original.GetString()}\" duplicado como \"{novo.GetString()}\""
                        : "Formulário duplicado",

                "Exclusão de Feedback" =>
                    root.TryGetProperty("deleted_count", out var count) && root.TryGetProperty("form_name", out var formName)
                        ? $"{count.GetInt32()} feedbacks do formulário \"{formName.GetString()}\" excluídos manualmente"
                        : "Feedbacks excluídos manualmente",

                "Exclusão de Formulário" =>
                    root.TryGetProperty("reason", out var reasonDelete) && root.TryGetProperty("form_name", out var formNameDelete)
                        ? $"Formulário \"{formNameDelete.GetString()}\" excluído com motivo: {reasonDelete.GetString()}"
                        : "Formulário excluído",

                "Ativação de Formulário" =>
                    root.TryGetProperty("activation_method", out var method) && root.TryGetProperty("form_name", out var formNameActive)
                        ? $"Formulário \"{formNameActive.GetString()}\" ativado via {method.GetString()}"
                        : "Formulário ativado",

                "Inativação de Formulário" =>
                    root.TryGetProperty("reason", out var reasonInactive) && root.TryGetProperty("form_name", out var formNameInact)
                        ? $"Formulário \"{formNameInact.GetString()}\" inativado. Motivo: {reasonInactive.GetString()}"
                        : "Formulário inativado",
                "Inativação de Formulário Agendada" =>
                    root.TryGetProperty("form_name", out var formNameScheduled)
                        ? $"Formulário \"{formNameScheduled.GetString()}\" inativado por agendamento"
                        : "Formulário inativado por agendamento",

                "Edição de Formulário" =>
                    root.TryGetProperty("form_name", out var formNameEdit)
                        ? $"Formulário \"{formNameEdit.GetString()}\" editado"
                        : "Formulário editado",

                "Edição do Estilo do Formulário" =>
                    root.TryGetProperty("form_name", out var formNameStyle)
                        ? $"Estilo do formulário \"{formNameStyle.GetString()}\" editado"
                        : "Estilo do formulário editado",

                _ => "Ação registrada"
            };
        }

    }

}