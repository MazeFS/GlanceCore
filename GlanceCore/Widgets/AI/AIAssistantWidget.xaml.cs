
namespace GlanceCore.Widgets.AI;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

public partial class AIAssistantWidget : GlanceCore.Widgets.BaseWidgetWindow
{
    private static readonly HttpClient _http = new();
    private readonly List<object> _messagesContext = new();
    private string _chatHistory = "Готов к работе.\n\n";
    public string ChatHistory { get => _chatHistory; set { _chatHistory = value; OnPropertyChanged(); } }

    public AIAssistantWidget()
    {
        InitializeComponent();
    }

    private async void BtnSend_Click(object sender, RoutedEventArgs e) => await SendMessage();
    private async void TxtInput_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) await SendMessage(); }

    private async Task SendMessage()
    {
        string input = TxtInput.Text.Trim();
        if (string.IsNullOrEmpty(input)) return;

        ChatHistory += $"Вы: {input}\n";
        TxtInput.Text = "";
        ChatScroll.ScrollToEnd();

        var cfg = Core.WidgetHost.CurrentConfig;

        if (_messagesContext.Count == 0) _messagesContext.Add(new { role = "system", content = cfg.AiSystemPrompt });
        _messagesContext.Add(new { role = "user", content = input });
        if (_messagesContext.Count > 11) _messagesContext.RemoveRange(1, 2);

        string url = cfg.AiEndpoint.EndsWith("/") ? cfg.AiEndpoint + "chat/completions" : cfg.AiEndpoint + "/chat/completions";

        var payload = new { model = cfg.AiModel, temperature = cfg.AiTemperature, messages = _messagesContext };
        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json") };
        if (!string.IsNullOrEmpty(cfg.AiApiKey)) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", cfg.AiApiKey);

        try
        {
            var response = await _http.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            string reply = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
            _messagesContext.Add(new { role = "assistant", content = reply });
            ChatHistory += $"AI: {reply.Trim()}\n\n";
        }
        catch { ChatHistory += "AI: Ошибка соединения с API.\n\n"; _messagesContext.RemoveAt(_messagesContext.Count - 1); }
        ChatScroll.ScrollToEnd();
    }

    private void CloseWidget_Click(object sender, RoutedEventArgs e) => Core.WidgetHost.CloseWidgetExplicitly("AI_01");
}