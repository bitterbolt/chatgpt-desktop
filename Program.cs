using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Web.WebView2.Core;
using System.IO;
using System.Threading;

namespace ChatGPT
{
    internal static class Program
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LoadLibrary(string dllToLoad);

        [DllImport("shell32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsUserAnAdmin();

        private static bool _isAdmin = false;
        public static bool IsAdmin => _isAdmin;

        [STAThread]
        static void Main()
        {
            _isAdmin = IsUserAnAdmin();

            string dllName = "WebView2Loader.dll";
            string dllPath = Path.Combine(Path.GetTempPath(), dllName);

            if (!ExtractEmbeddedDll(dllName, dllPath))
            {
                MessageBox.Show("Не удалось извлечь библиотеку.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!string.IsNullOrEmpty(dllPath))
            {
                LoadLibrary(dllPath);
            }

            string userDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ChatGPT"
            );

            if (!Directory.Exists(userDataFolder))
            {
                Directory.CreateDirectory(userDataFolder);
            }

            Environment.SetEnvironmentVariable("WEBVIEW2_USER_DATA_FOLDER", userDataFolder);


            InitializeWebView2();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
			Form1 form = new Form1();

			form.FormClosing += (s, e) =>
			{
                ClearEBWebViewDefaultFolder(userDataFolder);
			};
			
			Application.Run(form);
        }

        private static bool ExtractEmbeddedDll(string dllName, string dllPath)
        {
            try
            {
                if (File.Exists(dllPath))
                {
                    return true;
                }

                string? resourceName = Assembly.GetExecutingAssembly()
                    .GetManifestResourceNames()
                    .FirstOrDefault(name => name.EndsWith(dllName));

                if (string.IsNullOrEmpty(resourceName))
                {
                    MessageBox.Show($"Не найден встроенный ресурс {dllName}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

			using Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
			if (stream == null)
			{
			    MessageBox.Show($"Ошибка загрузки ресурса {dllName}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
			    return false;
			}
			using FileStream fileStream = new FileStream(dllPath, FileMode.Create, FileAccess.Write);
			stream!.CopyTo(fileStream);

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при извлечении библиотеки: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private static void InitializeWebView2()
        {
            try
            {
                var webViewEnvironment = CoreWebView2Environment.CreateAsync().Result;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при инициализации WebView2: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

		private static void ClearEBWebViewDefaultFolder(string userDataFolder)
		{
		    try
		    {
		        string defaultFolderPath = Path.Combine(userDataFolder, "EBWebView", "Default");
		
		        if (Directory.Exists(defaultFolderPath))
		        {
		            string codeCachePath = Path.Combine(defaultFolderPath, "Code Cache");
		            if (Directory.Exists(codeCachePath))
		            {
		                int attempts = 0;
		                while (Directory.Exists(codeCachePath) && attempts < 5)
		                {
		                    try
		                    {
		                        Directory.Delete(codeCachePath, true);
		                    }
		                    catch (IOException)
		                    {
		                        Thread.Sleep(50);
		                    }
		                    attempts++;
		                }
		            }
		
		            string cachePath = Path.Combine(defaultFolderPath, "Cache");
		            if (Directory.Exists(cachePath))
		            {
		                int attempts = 0;
		                while (Directory.Exists(cachePath) && attempts < 5)
		                {
		                    try
		                    {
		                        Directory.Delete(cachePath, true);
		                    }
		                    catch (IOException)
		                    {
		                        Thread.Sleep(50);
		                    }
		                    attempts++;
		                }
		            }
		        }
		    }
		    catch
		    {

		    }
		}
    }
}
