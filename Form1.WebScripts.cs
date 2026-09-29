using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Web.WebView2.WinForms;

namespace ChatGPT
{
    public partial class Form1
    {
        private async void AttachWebViewMouseMove()
        {
            if (!AppOptions.EnableToolbarMouseBridge) return;
            if (webView?.CoreWebView2 == null || _webViewMouseScriptAttached) return;

            webView.CoreWebView2.WebMessageReceived += (sender, args) =>
            {
                if (args.TryGetWebMessageAsString() == "toolbar_mousemove")
                    _lastMouseMove = DateTime.Now;
            };

            string script = LoadEmbeddedScript("toolbar-mouse.js");
            if (string.IsNullOrWhiteSpace(script)) return;

            await webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(script);
            _webViewMouseScriptAttached = true;
        }

        private static string LoadEmbeddedScript(string fileName)
        {
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                string? resourceName = assembly.GetManifestResourceNames()
                    .FirstOrDefault(name => name.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));

                if (string.IsNullOrEmpty(resourceName)) return string.Empty;

                using Stream? stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null) return string.Empty;

                using StreamReader reader = new StreamReader(stream);
                return reader.ReadToEnd();
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
