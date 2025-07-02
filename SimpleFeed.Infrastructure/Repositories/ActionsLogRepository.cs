using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Npgsql;
using SimpleFeed.Application.DTOs;
using SimpleFeed.Application.Interfaces;
using SimpleFeed.Domain.Enums;

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
            SELECT at.id, cal.timestamp, at.display_name, at.description, cal.details, at.code
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

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var actionEnum = (ClientActionType)reader.GetInt32(reader.GetOrdinal("id"));
                        var timestamp = reader.GetDateTime(reader.GetOrdinal("timestamp"));
                        var action = reader.GetString(reader.GetOrdinal("display_name"));
                        var description = reader.GetString(reader.GetOrdinal("description"));
                        var detailsRaw = reader.IsDBNull(reader.GetOrdinal("details")) ? null : reader.GetString(reader.GetOrdinal("details"));
                        var actionCode = reader.GetString(reader.GetOrdinal("code"));

                        var obs = GerarObservacao(detailsRaw, actionEnum);

                        result.Add(new ActionLogResultDto
                        {
                            Timestamp = timestamp,
                            Action = action,
                            Description = description,
                            Observations = obs
                        });
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao buscar logs de ações.", ex);
            }
        }

        private string GerarObservacao(string? detailsJson, ClientActionType actionEnum)
        {
            if (string.IsNullOrWhiteSpace(detailsJson))
                return "Ação registrada";

            JsonDocument doc;
            try
            {
                doc = JsonDocument.Parse(detailsJson);
            }
            catch
            {
                return "Ação registrada";
            }

            var root = doc.RootElement;

            switch (actionEnum)
            {
                case ClientActionType.CreateForm:
                    return root.TryGetProperty("form_name", out var formName)
                    ? $"Formulário criado com o nome \"{formName.GetString()}\""
                    : "Formulário criado";

                case ClientActionType.DuplicateForm:
                    return root.TryGetProperty("original_name_form", out var original) && root.TryGetProperty("new_form_name", out var novo)
                    ? $"Formulário \"{original.GetString()}\" duplicado como \"{novo.GetString()}\""
                    : "Formulário duplicado";

                case ClientActionType.ExcludeFeedback:
                    return root.TryGetProperty("deleted_count", out var count) && root.TryGetProperty("form_name", out var formName2)
                    ? $"{count.GetInt32()} feedbacks do formulário \"{formName2.GetString()}\" excluídos manualmente"
                    : "Feedbacks excluídos manualmente";

                case ClientActionType.DeleteForm:
                    return root.TryGetProperty("reason", out var reasonDelete) && root.TryGetProperty("form_name", out var formNameDelete)
                    ? $"Formulário \"{formNameDelete.GetString()}\" excluído com motivo: {reasonDelete.GetString()}"
                    : "Formulário excluído";

                case ClientActionType.ActivateForm:
                    return root.TryGetProperty("activation_method", out var method) && root.TryGetProperty("form_name", out var formNameActive)
                    ? $"Formulário \"{formNameActive.GetString()}\" ativado via {method.GetString()}"
                    : "Formulário ativado";

                case ClientActionType.InactivateForm:
                    return root.TryGetProperty("reason", out var reasonInactive) && root.TryGetProperty("form_name", out var formNameInact)
                    ? $"Formulário \"{formNameInact.GetString()}\" inativado. Motivo: {reasonInactive.GetString()}"
                    : "Formulário inativado";

                case ClientActionType.ScheduledFormInativation:
                    return root.TryGetProperty("form_name", out var formNameScheduled)
                    ? $"Formulário \"{formNameScheduled.GetString()}\" inativado por agendamento"
                    : "Formulário inativado por agendamento";

                case ClientActionType.EditForm:
                    return root.TryGetProperty("form_name", out var formNameEdit)
                    ? $"Formulário \"{formNameEdit.GetString()}\" editado"
                    : "Formulário editado";

                case ClientActionType.EditStyleForm:
                    return root.TryGetProperty("form_name", out var formNameStyle)
                    ? $"Estilo do formulário \"{formNameStyle.GetString()}\" editado"
                    : "Estilo do formulário editado";

                case ClientActionType.MigratePlan:
                    return root.TryGetProperty("previous_plan", out var previousPlan) && root.TryGetProperty("new_plan", out var newPlan)
                    ? $"Plano migrado de \"{previousPlan.GetString()}\" para \"{newPlan.GetString()}\""
                    : "Plano migrado";

                case ClientActionType.EditQrCode:
                    return root.TryGetProperty("form_name", out var formNameQrCode)
                    ? $"QR Code do formulário \"{formNameQrCode.GetString()}\" editado"
                    : "QR Code do formulário editado";

                case ClientActionType.AiAnalysis:
                    return root.TryGetProperty("form_name", out var formNameAi)
                    ? $"Análise IA do formulário \"{formNameAi.GetString()}\" realizada"
                    : "Análise IA realizada";

                default:
                    return "Ação registrada";
            }
        }

    }

}