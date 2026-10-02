namespace ChatGPT
{
    internal static class AppOptions
    {
        public static readonly bool EnableToolbarMouseBridge = true;
        public const int WebViewInitTimeoutMs = 15000;

        /// <summary>
        /// Первичный и вторичный DNS-серверы для функции «обхода блокировок».
        /// </summary>
        public const string PrimaryDns = "111.88.96.50";
        public const string SecondaryDns = "111.88.96.51";

        /// <summary>
        /// Сервисы нейросетей для стартового оверлея (Название, URL, Иконка).
        /// </summary>
        public static readonly (string Name, string Url, string IconResource)[] Services = new[]
        {
            ("ChatGPT", "https://chat.openai.com", "ChatGPT.Resources.ChatGPT.ico"),
            ("Claude", "https://claude.ai", "ChatGPT.Resources.Claude.ico"),
            ("Gemini", "https://gemini.google.com/app", "ChatGPT.Resources.Gemini.ico"),
            ("DeepSeek", "https://chat.deepseek.com", "ChatGPT.Resources.DeepSeek.ico"),
            ("Grok", "https://grok.com/", "ChatGPT.Resources.Grok.ico"),
            ("Copilot", "https://copilot.microsoft.com", "ChatGPT.Resources.Copilot.ico")
        };
    }
}
