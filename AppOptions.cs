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
    }
}
