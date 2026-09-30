using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Management;

namespace ChatGPT
{
    public partial class Form1 : Form
    {
        private WebView2 webView = new WebView2();
        private Form? overlayForm = null;
        private bool isFullScreen = false;
        private bool _overlayInitialized = false;
        private bool isDnsEnabled = false;
        private bool _webViewMouseScriptAttached = false;
        private bool isMouseOverToolbar = false;
        
        private Panel? topToolbar = null;
        private Button? btnOverlayToggle = null;
        private Button? btnFullscreenToggle = null;
        private Button? btnBrowserToggle = null;
        private Button? btnAddressBarToggle = null;
        
        private Panel addressPanel = new Panel();
        private TextBox addressBar = new TextBox();
        private Button btnBack = new Button();
        private Button btnRefresh = new Button();
        private Button btnForward = new Button();
        
        private Button? btnToggleDns = null;
        private Button? btnOverlayClose = null;
        
        private Dictionary<string, Bitmap> _iconCache = new Dictionary<string, Bitmap>();
        private Bitmap? _cachedBackground = null;
        private ToolTip dnsToolTip = new ToolTip();
        
        private System.Windows.Forms.Timer? _toolbarTimer;
        private int _toolbarTargetHeight = 40;
        private int _toolbarStep = 8;
        private DateTime _lastMouseMove = DateTime.Now;

        private CustomFlowLayoutPanel? buttonPanel = null;

        public Form1()
        {
            InitializeComponent();
            this.Opacity = 0.0;
            LoadResources();
            InitializeForm();
            InitializeButtonPanel();
            InitializeOverlayForm();
            
			this.Shown += async (sender, e) => 
            {
                ShowOverlay();
                this.Opacity = 1.0;

                bool webViewReady = await InitializeWebViewWithTimeoutAsync();
                if (!webViewReady)
                {
                    overlayForm?.Hide();
                    MessageBox.Show(
                        this,
                        "Не удалось инициализировать WebView2. Проверьте установку Microsoft Edge WebView2 Runtime и повторите запуск.",
                        "ChatGPT",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            };
            SetupTopToolbar();
            InitializeToolbarSlideAutoHide();
            InitializeAddressBar();
        }

        private void LoadResources()
        {
            try
            {
                using Stream? backgroundStream = Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream("ChatGPT.Resources.Player.jpg");
                if (backgroundStream != null)
                {
                    _cachedBackground?.Dispose();
                    _cachedBackground = new Bitmap(backgroundStream);
                }
            }
            catch { }
            
            try
            {
                using Stream? iconStream = Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream("ChatGPT.Resources.ChatGPT.ico");
                if (iconStream != null)
                {
                    this.Icon = new Icon(iconStream);
                }
            }
            catch { }
        }

        private void InitializeForm()
        {
            this.Text = "ChatGPT";
            this.KeyPreview = true;
            this.KeyDown += Form1_KeyDown;
            this.Resize += (s, e) => UpdateOverlayPosition();
            this.Layout += (s, e) => UpdateOverlayPosition();
            this.Move += (s, e) => UpdateOverlayPosition();
            SetInitialWindowSize();
            this.DoubleBuffered = true;
            this.FormClosing += Form1_FormClosing;
        }

        private void InitializeButtonPanel()
        {
            buttonPanel = new CustomFlowLayoutPanel()
            {
                Name = "MainButtonPanel",
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(20),
                BackColor = Color.Transparent
            };
            
            string[,] links = {
                { "https://chat.openai.com", "ChatGPT.Resources.ChatGPT.ico" },
                { "https://claude.ai", "ChatGPT.Resources.Claude.ico" },
                { "https://gemini.google.com/app", "ChatGPT.Resources.Gemini.ico" },
                { "https://chat.deepseek.com", "ChatGPT.Resources.DeepSeek.ico" },
                { "https://grok.com/", "ChatGPT.Resources.Grok.ico" },
                { "https://copilot.microsoft.com", "ChatGPT.Resources.Copilot.ico" }
            };
            
            for (int i = 0; i < links.GetLength(0); i++)
            {
                Button btn = CreateIconButton(links[i, 0], links[i, 1], 128);
                
                if (i == 2)
                    buttonPanel.SetFlowBreak(btn, true);
                
                buttonPanel.Controls.Add(btn);
            }
        }
        
        private void InitializeOverlayForm()
        {
            overlayForm = new CustomForm()
            {
                FormBorderStyle = FormBorderStyle.None,
                Opacity = 1,
                StartPosition = FormStartPosition.Manual,
                TopMost = false,
                ShowInTaskbar = false,
                KeyPreview = true,
                ShowIcon = false,
                BackgroundImage = _cachedBackground,
                BackgroundImageLayout = ImageLayout.Stretch,
                AcceptButton = null,
                CancelButton = null,
            };

            overlayForm.KeyDown += (s, e) => Form1_KeyDown(this, e);
            
            if (buttonPanel != null)
            {
                overlayForm.Controls.Add(buttonPanel);
            }
            
            InitializeOverlayButtons();
            overlayForm.TabStop = false;
            overlayForm.Owner = this;
            _overlayInitialized = true;
        }

        private void InitializeOverlayButtons()
        {
            btnToggleDns = CreateOverlayButton(
                "btnToggleDns", 
                48,
                Program.IsAdmin ? (isDnsEnabled ? "ChatGPT.Resources.on.ico" : "ChatGPT.Resources.off.ico") : "ChatGPT.Resources.no.ico",
                Program.IsAdmin ? (isDnsEnabled ? "Отключить обход блокировок" : "Включить обход блокировок") : "Требуются права администратора",
                (s, e) => 
                {
                    if (!Program.IsAdmin) return;
                    isDnsEnabled = !isDnsEnabled;
                    string stateText = isDnsEnabled ? "Отключить" : "Включить";
                    string resource = isDnsEnabled ? "ChatGPT.Resources.on.ico" : "ChatGPT.Resources.off.ico";
                    if (btnToggleDns != null)
					{
					    btnToggleDns.Image = LoadIcon(resource, 48, crop: true);
                        dnsToolTip.SetToolTip(btnToggleDns, $"{stateText} обход блокировок");
					}
                    overlayForm?.Focus();
                },
                Program.IsAdmin
            );

            btnOverlayClose = CreateOverlayButton(
                "btnOverlayClose",
                48,
                "ChatGPT.Resources.Close.ico",
                "Назад к просмотру",
				(s, e) => TryReturnFromOverlay(),
                true
            );

			if (overlayForm != null && btnToggleDns != null)
			{
			    overlayForm.Controls.Add(btnToggleDns);
			}
			if (overlayForm != null && btnOverlayClose != null)
			{
			    overlayForm.Controls.Add(btnOverlayClose);
			}

            UpdateOverlayBackButtonVisibility();
        }

        private Button CreateOverlayButton(string name, int size, string iconResource, string tooltip, EventHandler clickHandler, bool enabled)
        {
            var btn = new Button
            {
                Name = name,
                Width = size,
                Height = size,
                FlatStyle = FlatStyle.Flat,
                TabStop = false,
                BackColor = Color.Transparent,
                ImageAlign = ContentAlignment.MiddleCenter,
                Image = LoadIcon(iconResource, size, crop: true),
                Enabled = enabled
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.Transparent;
            btn.FlatAppearance.MouseDownBackColor = Color.Transparent;
            dnsToolTip.SetToolTip(btn, tooltip);
            btn.Click += clickHandler;
            return btn;
        }

        private void SetInitialWindowSize()
        {
            this.WindowState = FormWindowState.Normal;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.TopMost = false;
            this.Width = 1035;
            this.Height = 728;
            this.StartPosition = FormStartPosition.CenterScreen;
        }

        private async Task<bool> InitializeWebViewAsync()
        {
            try
            {
                webView.Dock = DockStyle.Fill;
                webView.Source = new Uri("about:blank");
                webView.KeyDown += (s, e) => Form1_KeyDown(this, e);
                webView.SourceChanged += WebView_SourceChanged;
                this.Controls.Add(webView);
                await webView.EnsureCoreWebView2Async();
                topToolbar?.BringToFront();

                if (webView.CoreWebView2 != null)
                {
                    // Настройка безопасности
                    webView.CoreWebView2.Settings.IsZoomControlEnabled = true;
                    webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
                    webView.CoreWebView2.Settings.IsStatusBarEnabled = true;
                    
                    // Блокировка опасных схем
                    webView.CoreWebView2.NavigationStarting += (s, e) =>
                    {
                        if (e.Uri.StartsWith("file://") || 
                            e.Uri.StartsWith("ftp://") ||
                            e.Uri.StartsWith("javascript:"))
                        {
                            e.Cancel = true;
                        }
                    };
                    
                    AttachWebViewMouseMove();
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WebView2 init error: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> InitializeWebViewWithTimeoutAsync()
        {
            try
            {
                Task<bool> initTask = InitializeWebViewAsync();
                Task completedTask = await Task.WhenAny(initTask, Task.Delay(AppOptions.WebViewInitTimeoutMs));
                if (completedTask != initTask)
                {
                    System.Diagnostics.Debug.WriteLine("WebView2 init timeout.");
                    return false;
                }

                return await initTask;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WebView2 init wrapper error: {ex.Message}");
                return false;
            }
        }

        private void WebView_SourceChanged(object? sender, EventArgs e)
        {
            if (webView.Source != null)
            {
                addressBar.Text = webView.Source.ToString();
            }

            UpdateOverlayBackButtonVisibility();
        }

        private void Form1_KeyDown(object? sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.F11 when !e.Alt && !e.Control && !e.Shift:
                    ToggleFullScreen();
                    e.Handled = true;
                    break;
                case Keys.F10 when !e.Alt && !e.Control && !e.Shift:
                    ToggleOverlayVisibility();
                    e.Handled = true;
                    break;
                case Keys.Escape when !e.Alt && !e.Control && !e.Shift:
                    HandleEscapeKey();
                    e.Handled = true;
                    break;
            }
        }

        private void HandleEscapeKey()
        {
            if (overlayForm?.Visible == true)
            {
                TryReturnFromOverlay();
                return;
            }

            ShowOverlay();
        }

        private bool HasPlayableSourceLoaded()
        {
            if (webView?.Source == null) return false;
            string uri = webView.Source.ToString();
            return !string.Equals(uri, "about:blank", StringComparison.OrdinalIgnoreCase);
        }

        private void UpdateOverlayBackButtonVisibility()
        {
            if (btnOverlayClose == null) return;
            btnOverlayClose.Visible = HasPlayableSourceLoaded();
        }

        private void TryReturnFromOverlay()
        {
            if (!HasPlayableSourceLoaded())
            {
                overlayForm?.BringToFront();
                overlayForm?.Focus();
                return;
            }

            overlayForm?.Hide();
            this.Focus();
            webView?.Focus();
        }

        private void SetupTopToolbar()
        {
            topToolbar = new Panel
            {
                Dock = DockStyle.None,
                Height = 35,
                Padding = new Padding(4),
                BackColor = Color.LightGray,
                Left = 0,
                Top = 0,
                Width = this.ClientSize.Width
            };
            
            btnOverlayToggle = MakeToolbarButton("ChatGPT.Resources.Menu.ico", 24, DockStyle.Left, (s, e) => ToggleOverlayVisibility());
            btnFullscreenToggle = MakeToolbarButton("ChatGPT.Resources.Fullscreen.ico", 24, DockStyle.Right, (s, e) => ToggleFullScreen());
            btnBrowserToggle = MakeToolbarButton("ChatGPT.Resources.Browser.ico", 24, DockStyle.Right, (s, e) =>
            {
                overlayForm?.Hide();
                try { webView.Source = new Uri("https://www.google.com/?hl=ru"); } catch { }
                webView.Focus();
            });
            btnAddressBarToggle = MakeToolbarButton("ChatGPT.Resources.Adressbar.ico", 24, DockStyle.Right, (s, e) =>
            {
                addressPanel.Visible = !addressPanel.Visible;
            });
            
            topToolbar.Controls.Add(btnOverlayToggle);
            topToolbar.Controls.Add(btnAddressBarToggle);
            topToolbar.Controls.Add(btnBrowserToggle);
            topToolbar.Controls.Add(btnFullscreenToggle);
            this.Controls.Add(topToolbar);
            topToolbar.BringToFront();
            this.Resize += (s, e) => topToolbar.Width = this.ClientSize.Width;
        }

        private Button MakeToolbarButton(string iconResource, int size, DockStyle dock, EventHandler onClick)
        {
            var btn = new Button
            {
                Width = 40,
                Height = 32,
                FlatStyle = FlatStyle.Flat,
                Dock = dock,
                TabStop = false,
                Image = LoadIcon(iconResource, size),
                ImageAlign = ContentAlignment.MiddleCenter
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += (s, e) =>
            {
                onClick(s, e);
                this.ActiveControl = null;
                overlayForm?.Focus();
            };
            return btn;
        }

        private void InitializeToolbarSlideAutoHide()
        {
            if (topToolbar == null) return;
            _toolbarTargetHeight = topToolbar.Height > 0 ? topToolbar.Height : 35;
            topToolbar.Dock = DockStyle.None;
            topToolbar.Left = 0;
            topToolbar.Top = 0;
            topToolbar.Width = this.ClientSize.Width;
            _toolbarTimer = new System.Windows.Forms.Timer();
            _toolbarTimer.Interval = 30;
            _toolbarTimer.Tick += ToolbarTimer_Tick;
            _toolbarTimer.Start();
            this.MouseMove += Form_MouseMove;
            topToolbar.MouseEnter += (s, e) => isMouseOverToolbar = true;
            topToolbar.MouseLeave += (s, e) => isMouseOverToolbar = false;
            foreach (Control ctrl in topToolbar.Controls)
            {
                ctrl.MouseEnter += (s, e) => isMouseOverToolbar = true;
                ctrl.MouseLeave += (s, e) => isMouseOverToolbar = false;
            }
            this.Resize += (s, e) => topToolbar.Width = this.ClientSize.Width;
			webView.CoreWebView2InitializationCompleted += (s, e) =>
			{
			    if (e?.IsSuccess == true && webView?.CoreWebView2 != null)
			        AttachWebViewMouseMove();
			};
        }

		private void Form_MouseMove(object? sender, MouseEventArgs e)
		{
		    if (e != null && e.Y <= 10)
		        _lastMouseMove = DateTime.Now;
		}

        private void ToolbarTimer_Tick(object? sender, EventArgs e)
        {
            if (topToolbar == null) return;
            int hiddenTop = -_toolbarTargetHeight;
            int visibleTop = 0;
            if (isMouseOverToolbar)
            {
                if (topToolbar.Top < visibleTop)
                    topToolbar.Top = Math.Min(visibleTop, topToolbar.Top + _toolbarStep);
                return;
            }
            bool idleTooLong = (DateTime.Now - _lastMouseMove).TotalSeconds >= 1;
            if (idleTooLong)
            {
                if (topToolbar.Top > hiddenTop)
                    topToolbar.Top = Math.Max(hiddenTop, topToolbar.Top - _toolbarStep);
            }
            else
            {
                if (topToolbar.Top < visibleTop)
                {
                    topToolbar.Top = Math.Min(visibleTop, topToolbar.Top + _toolbarStep);
                    topToolbar.BringToFront();
                }
            }
        }

        private void InitializeAddressBar()
        {
            addressPanel.Dock = DockStyle.Top;
            addressPanel.Height = 30;
            addressPanel.Visible = false;
            addressPanel.Padding = new Padding(3);
            addressPanel.BackColor = Color.LightGray;
            addressBar.Dock = DockStyle.Fill;
            addressBar.Font = new Font("Segoe UI", 10);
            addressBar.KeyDown += AddressBar_KeyDown;
            btnBack = MakeAddressBarButton("←", DockStyle.Right, (s, e) =>
            {
                if (webView.CanGoBack) webView.GoBack();
            });
            btnRefresh = MakeAddressBarButton("⟳", DockStyle.Right, (s, e) =>
            {
                webView.Reload();
            });
            btnForward = MakeAddressBarButton("→", DockStyle.Right, (s, e) =>
            {
                if (webView.CanGoForward) webView.GoForward();
            });
            addressPanel.Controls.Add(addressBar);
            addressPanel.Controls.Add(btnBack);
            addressPanel.Controls.Add(btnRefresh);
            addressPanel.Controls.Add(btnForward);
            this.Controls.Add(addressPanel);
        }

        private Button MakeAddressBarButton(string text, DockStyle dock, EventHandler onClick)
        {
            var btn = new Button
            {
                Text = text,
                Width = 35,
                Dock = dock,
                TabStop = false
            };
            btn.Click += (s, e) =>
            {
                onClick(s, e);
                this.ActiveControl = null;
                this.Focus();
            };
            return btn;
        }

        private void AddressBar_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e?.KeyCode != Keys.Enter) return;
            
            string url = addressBar?.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(url)) return;

            try
            {
                if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                    !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    url = "https://" + url;
                }
                
				if (webView != null)
				{
				    webView.Source = new Uri(url);
				}
            }
            catch (UriFormatException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Invalid URL: {url}, Error: {ex.Message}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"URL navigation error: {ex.Message}");
            }
            
            e.Handled = true;
            e.SuppressKeyPress = true;
        }

        private void ToggleFullScreen()
        {
            if (isFullScreen)
            {
                this.FormBorderStyle = FormBorderStyle.Sizable;
                this.WindowState = FormWindowState.Normal;
                SetInitialWindowSize();
                CenterToScreen();
            }
            else
            {
                this.FormBorderStyle = FormBorderStyle.None;
                this.WindowState = FormWindowState.Normal;
                Screen screen = Screen.FromControl(this);
                this.Bounds = screen.Bounds;
            }
            UpdateOverlayPosition();
            isFullScreen = !isFullScreen;
        }

        private void ToggleOverlayVisibility()
        {
            if (overlayForm == null) return;
            
            if (overlayForm.Visible)
            {
                overlayForm.Hide();
                webView?.Focus();
            }
            else
            {
                UpdateOverlayPosition();
                UpdateOverlayBackButtonVisibility();
                overlayForm.Show();
                overlayForm.BringToFront();
                overlayForm.Focus();
            }
        }
        
        private void ShowOverlay()
        {
            if (_overlayInitialized && overlayForm != null)
            {
                UpdateOverlayPosition();
                UpdateOverlayBackButtonVisibility();
                overlayForm.Visible = true;
                overlayForm.Focus();
                return;
            }
        }

        private void UpdateOverlayPosition()
        {
            if (overlayForm == null || overlayForm.IsDisposed || this.IsDisposed) 
                return;

            try
            {
                overlayForm.Location = this.PointToScreen(Point.Empty);
                overlayForm.Size = this.ClientSize;
                
                btnToggleDns?.SetBounds(10, 10, btnToggleDns.Width, btnToggleDns.Height);
                
                if (btnOverlayClose != null)
                {
                    btnOverlayClose.Left = overlayForm.ClientSize.Width - btnOverlayClose.Width - 10;
                    btnOverlayClose.Top = 10;
                }
                
                var buttonPanel = overlayForm.Controls.OfType<CustomFlowLayoutPanel>().FirstOrDefault();
                if (buttonPanel != null)
                {
                    buttonPanel.Left = Math.Max(0, (overlayForm.ClientSize.Width - buttonPanel.Width) / 2);
                    buttonPanel.Top = Math.Max(0, (overlayForm.ClientSize.Height - buttonPanel.Height) / 2);
                }
            }
            catch (ObjectDisposedException)
            {
                // Игнорировать если формы уже disposed
            }
        }

        private Button CreateIconButton(string url, string iconResource, int size)
        {
            if (!_iconCache.TryGetValue(iconResource, out var icon))
            {
                icon = LoadIcon(iconResource, size, crop: true);
                _iconCache[iconResource] = icon;
            }
            var btn = new Button
            {
                Size = new Size(size, size),
                Margin = new Padding(30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ImageAlign = ContentAlignment.MiddleCenter,
                Image = icon,
                TabStop = false
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.Transparent;
            btn.FlatAppearance.MouseDownBackColor = Color.Transparent;
            btn.Click += (s, e) =>
            {
                if (Program.IsAdmin && isDnsEnabled)
                {
                    try
                    {
                        SetDns("111.88.96.50", "111.88.96.51");
                    }
                    catch { }
                }
                overlayForm?.Hide();
                try
                {
                    webView.Source = new Uri(url);
                    webView.Focus();
                }
                catch { }
                this.ActiveControl = null;
                this.Focus();
            };
            return btn;
        }

        private Bitmap LoadIcon(string resourceName, int targetSize, bool crop = false, int feather = 10)
        {
            // Кэширование по ключу, включающему размер и параметры crop
            string cacheKey = $"{resourceName}_{targetSize}_{crop}_{feather}";
            
            if (_iconCache.TryGetValue(cacheKey, out var cachedIcon))
                return cachedIcon;

            try
            {
                using Stream? stream = Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream(resourceName);
                if (stream == null)
                    return new Bitmap(targetSize, targetSize);
                    
                using var icon = new Icon(stream, new Size(targetSize, targetSize));
                using Bitmap bmp = icon.ToBitmap();
                Bitmap result = crop ? CropToCircle(bmp, feather) : new Bitmap(bmp, new Size(targetSize, targetSize));
                
                _iconCache[cacheKey] = result;
                return result;
            }
            catch
            {
                return new Bitmap(targetSize, targetSize);
            }
        }

        private Bitmap CropToCircle(Bitmap bmp, int featherRadius = 10)
        {
            int size = Math.Min(bmp.Width, bmp.Height);
            Bitmap cropped = new Bitmap(size, size);
            using (Graphics g = Graphics.FromImage(cropped))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.Clear(Color.Transparent);
                using (GraphicsPath path = new GraphicsPath())
                {
                    path.AddEllipse(featherRadius, featherRadius, size - 2 * featherRadius, size - 2 * featherRadius);
                    g.SetClip(path);
                    g.DrawImage(bmp, new Rectangle(featherRadius, featherRadius, size - 2 * featherRadius, size - 2 * featherRadius),
                              new Rectangle(0, 0, bmp.Width, bmp.Height), GraphicsUnit.Pixel);
                }
            }
            return cropped;
        }

		private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
		{
		    try
		    {
		        this.Opacity = 0;
		        this.Hide();
		        Application.DoEvents();
		        
		        _toolbarTimer?.Stop();
		        _toolbarTimer?.Dispose();
		        _toolbarTimer = null;
		
		        if (webView != null)
		        {
		            webView.Visible = false;
		            
		            try
		            {
		                webView.CoreWebView2?.Stop();
		                Application.DoEvents();
		            }
		            catch { }
		        }
		
		        if (overlayForm != null && !overlayForm.IsDisposed)
		        {
		            overlayForm.Opacity = 0;
		            overlayForm.Hide();
		            overlayForm.Close();
		            overlayForm.Dispose();
		            overlayForm = null;
		        }
		
		        ResetDns();
		
		        webView?.Dispose();
		        //webView = null;
		    }
		    catch { }
		    finally
		    {
		        dnsToolTip?.Dispose();
		        // dnsToolTip = null; // Убрать - ToolTip не нужно обнулять
		        
		        foreach (var kvp in _iconCache)
		        {
		            kvp.Value?.Dispose();
		        }
		        _iconCache.Clear();
		        
		        _cachedBackground?.Dispose();
		        _cachedBackground = null;
		
		        // Убираем обнуление для компонентов, которые не объявлены как nullable
		        addressBar?.Dispose();
		        // addressBar = null;
		        
		        btnBack?.Dispose();
		        // btnBack = null;
		        
		        btnRefresh?.Dispose();
		        // btnRefresh = null;
		        
		        btnForward?.Dispose();
		        // btnForward = null;
		        
		        topToolbar?.Dispose();
		        // topToolbar = null;
		        
		        btnOverlayToggle?.Dispose();
		        // btnOverlayToggle = null;
		        
		        btnFullscreenToggle?.Dispose();
		        // btnFullscreenToggle = null;
		        
		        btnBrowserToggle?.Dispose();
		        // btnBrowserToggle = null;
		        
		        btnAddressBarToggle?.Dispose();
		        // btnAddressBarToggle = null;
		        
		        btnToggleDns?.Dispose();
		        // btnToggleDns = null;
		        
		        btnOverlayClose?.Dispose();
		        // btnOverlayClose = null;
		        
		        buttonPanel?.Dispose();
		        // buttonPanel = null;
		        
		        addressPanel?.Dispose();
		        // addressPanel = null;
		
		        GC.Collect();
		        GC.WaitForPendingFinalizers();
		    }
		}

        public class CustomForm : Form
        {
            public CustomForm()
            {
                this.SetStyle(
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.UserPaint,
                    true);
            }
        }

        public class CustomFlowLayoutPanel : FlowLayoutPanel
        {
            public CustomFlowLayoutPanel()
            {
                this.SetStyle(
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.UserPaint,
                    true);
            }
        }

        private void SetDns(string primaryDns, string? secondaryDns = null)
        {
            if (!Program.IsAdmin || !isDnsEnabled) return;
            
            try
            {
                string[] dnsServers = string.IsNullOrEmpty(secondaryDns)
                    ? new[] { primaryDns }
                    : new[] { primaryDns, secondaryDns };

                using var searcher = new ManagementObjectSearcher(
                    "root\\cimv2",
                    "SELECT * FROM Win32_NetworkAdapterConfiguration WHERE IPEnabled=True");

                foreach (ManagementObject obj in searcher.Get().Cast<ManagementObject>())
                {
                    try
                    {
                        using var inParams = obj.GetMethodParameters("SetDNSServerSearchOrder");
                        if (inParams == null) continue;
                        inParams["DNSServerSearchOrder"] = dnsServers;
                        using var result = obj.InvokeMethod("SetDNSServerSearchOrder", inParams, null);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"DNS setting error: {ex.Message}");
                    }
                    finally
                    {
		                if (obj != null)
		                {
		                    obj.Dispose();
		                }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DNS error: {ex.Message}");
            }
        }

        private void ResetDns()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "root\\cimv2",
                    "SELECT * FROM Win32_NetworkAdapterConfiguration WHERE IPEnabled=True");

                foreach (ManagementObject obj in searcher.Get())
                {
                    try
                    {
                        using var inParams = obj.GetMethodParameters("SetDNSServerSearchOrder");
                        if (inParams == null) continue;
                        inParams["DNSServerSearchOrder"] = null;
                        obj.InvokeMethod("SetDNSServerSearchOrder", inParams, null);
                    }
                    catch { }
                    finally
                    {
		                if (obj != null)
		                {
		                    obj.Dispose();
		                }
                    }
                }
            }
            catch { }
        }
    }
}
