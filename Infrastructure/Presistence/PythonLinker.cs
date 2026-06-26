using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ServiceAbstraction;
using Shared.DataTransferObject;

namespace Presistence
{
    public class PythonLinker : IPythonLinker
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<PythonLinker> _logger;

        public PythonLinker(HttpClient httpClient, ILogger<PythonLinker> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<PythonOutputDto?> GetOptimizedPlanAsync(PythonRequestDto request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("optimize", request);
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning(
                        "Python optimizer returned non-success status {StatusCode}. Response: {ResponseBody}",
                        (int)response.StatusCode,
                        body);
                    return null;
                }

                var result = await response.Content.ReadFromJsonAsync<PythonOutputDto>();
                if (result is null)
                {
                    _logger.LogWarning("Python optimizer returned an empty body.");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to call Python optimizer.");
                return null;
            }
        }
    }
}
