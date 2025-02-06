using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Azure;
using Azure.AI.TextAnalytics;
using Microsoft.Extensions.Configuration;

class Program
{
    private static string endpoint;
    private static string apiKey;

    static async Task Main(string[] args)
    {
        LoadConfiguration();

        Console.WriteLine("Skriv in en fråga:");
        string userInput = Console.ReadLine();

        // Anropa NLP (språkdetektion)
        string detectedLanguage = DetectLanguage(userInput);
        Console.WriteLine($"Identifierat språk: {detectedLanguage}");

        // Anropa QnA Maker
        string response = await GetQnAResponse(userInput);
        Console.WriteLine($"AI-svar: {response}");
    }

    static void LoadConfiguration()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        endpoint = config["AzureAI:Endpoint"];
        apiKey = config["AzureAI:ApiKey"];
    }

    static string DetectLanguage(string text)
    {
        var client = new TextAnalyticsClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
        var response = client.DetectLanguage(text);
        return response.PrimaryLanguage.Name;
    }

    static async Task<string> GetQnAResponse(string question)
    {
        string qnaEndpoint = "DIN_QNA_ENDPOINT";
        string qnaKey = "DIN_QNA_APIKEY";
        string knowledgeBaseId = "DIN_KNOWLEDGEBASE_ID";

        var requestBody = new
        {
            question = question,
            top = 1
        };

        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("Authorization", $"EndpointKey {qnaKey}");
            string requestJson = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync($"{qnaEndpoint}/knowledgebases/{knowledgeBaseId}/generateAnswer", content);
            string result = await response.Content.ReadAsStringAsync();
            JsonDocument jsonDoc = JsonDocument.Parse(result);
            return jsonDoc.RootElement.GetProperty("answers")[0].GetProperty("answer").GetString();
        }
    }
}
