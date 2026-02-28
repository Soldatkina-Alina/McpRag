using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace McpRag.TestClient;

public class Program
{
    private static readonly HttpClient _httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:11434") };

    public static async Task Main(string[] args)
    {
        try
        {
            Console.WriteLine("=== Запрос к Ollama API /api/tags ===");
            
            var response = await _httpClient.GetAsync("/api/tags");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Ответ:");
                Console.WriteLine(content);

                var tagsResponse = JsonSerializer.Deserialize<OllamaTagsResponse>(content);
                
                if (tagsResponse?.Models != null && tagsResponse.Models.Count > 0)
                {
                    Console.WriteLine($"\n=== Найденные модели ({tagsResponse.Models.Count}) ===");
                    foreach (var model in tagsResponse.Models)
                    {
                        Console.WriteLine($"- {model.Name}");
                    }
                }
                else
                {
                    Console.WriteLine("\n=== Нет доступных моделей ===");
                }

                // Проверка конкретных моделей
                var targetModels = new[] { "phi3:mini", "nomic-embed-text" };
                Console.WriteLine("\n=== Проверка модели ===");
                
                foreach (var targetModel in targetModels)
                {
                    var isAvailable = tagsResponse?.Models.Any(m => 
                        m.Name.StartsWith(targetModel, StringComparison.OrdinalIgnoreCase)) ?? false;
                    
                    Console.WriteLine($"Модель '{targetModel}': {(isAvailable ? "Доступна" : "Не доступна")}");
                }
            }
            else
            {
                Console.WriteLine($"Ошибка: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Исключение: {ex.Message}");
            if (ex.InnerException != null)
                Console.WriteLine($"Внутреннее исключение: {ex.InnerException.Message}");
        }

        _httpClient.Dispose();
        Console.WriteLine("\nНажмите Enter для выхода...");
        Console.ReadLine();
    }
}

public class OllamaTagsResponse
{
    [JsonPropertyName("models")]
    public List<OllamaModel> Models { get; set; } = new();
}

public class OllamaModel
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    [JsonPropertyName("digest")]
    public string Digest { get; set; } = string.Empty;
    [JsonPropertyName("size")]
    public long Size { get; set; } = 0;
    [JsonPropertyName("modified_at")]
    public string ModifiedAt { get; set; } = string.Empty;
}
