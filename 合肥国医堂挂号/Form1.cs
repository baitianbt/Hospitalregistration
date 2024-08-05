using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace 合肥国医堂挂号
{
    public partial class Form1 : Form
    {
        private const string TimeServerUrl = "http://worldtimeapi.org/api/ip";
        private HttpClient httpClient;
        private List<dynamic> storedCookies; // Changed to dynamic for simplicity
        private System.Threading.Timer timer;
        private string targetUrl;
        private HttpClient client;

        public Form1()
        {
            InitializeComponent();
            httpClient = new HttpClient();
            storedCookies = new List<dynamic>();
            comboBox1.SelectedIndex = 0;
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            var environment = await CoreWebView2Environment.CreateAsync();
            await webView.EnsureCoreWebView2Async(environment);
            webView.Source = new Uri("http://gyt.lpxxkj.com/index.php/Public/wx_login");

            webView.CoreWebView2InitializationCompleted += (s, et) =>
            {
                if (et.IsSuccess)
                {
                    Console.WriteLine("WebView2 初始化成功");
                }
                else
                {
                    Console.WriteLine($"WebView2 初始化失败: {et.InitializationException}");
                }
            };

            webView.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
            timer1.Start();
        }

        private async void CoreWebView2_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (e.IsSuccess)
            {
                await webView.CoreWebView2.ExecuteScriptAsync("document.body.style.zoom = '50%';");
                await Task.Delay(5000);
                await FetchCookies();
            }
            else
            {
                MessageBox.Show("Navigation failed.");
            }
        }

        private async Task FetchCookies()
        {
            var cookies = await webView.CoreWebView2.CookieManager.GetCookiesAsync("http://gyt.lpxxkj.com");
            storedCookies.Clear();
            foreach (var cookie in cookies)
            {
                storedCookies.Add(new
                {
                    Name = cookie.Name,
                    Value = cookie.Value,
                    Domain = cookie.Domain,
                    Path = cookie.Path,
                    Expires = cookie.Expires
                });
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            label2.Text = DateTime.Now.ToString();
        }

        private async Task UpdateTimeFromNetwork()
        {
            try
            {
                var response = await httpClient.GetStringAsync(TimeServerUrl);
                dynamic json = Newtonsoft.Json.JsonConvert.DeserializeObject(response);
                string networkTime = json.datetime;
                label2.Text = $"Current Time: {networkTime}";
            }
            catch (Exception ex)
            {
                label2.Text = "Failed to retrieve time";
                Console.WriteLine(ex.Message);
            }
        }

        private async void BtnQuery_Click(object sender, EventArgs e)
        {
            await SendRequestWithCookies("http://gyt.lpxxkj.com/index.php/wxIndex/my_order");
        }

        private async Task SendRequestWithCookies(string url)
        {
            try
            {
                var handler = new HttpClientHandler();
                var cookieContainer = new CookieContainer();

                foreach (var cookie in storedCookies)
                {
                    try
                    {
                        var netCookie = new Cookie(cookie.Name, cookie.Value)
                        {
                            Domain = cookie.Domain,
                            Path = cookie.Path,
                            Expires = cookie.Expires
                        };
                        cookieContainer.Add(new Uri(url), netCookie);
                    }
                    catch (CookieException ex)
                    {
                        Console.WriteLine($"Invalid cookie: {cookie.Name}, Value: {cookie.Value}, Exception: {ex.Message}");
                    }
                }

                handler.CookieContainer = cookieContainer;
                using (var client = new HttpClient(handler))
                {
                    var htmlContent = await client.GetStringAsync(url);
                    webView.CoreWebView2.NavigateToString(htmlContent);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to send request or display content: {ex.Message}");
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (linkLabel1.Text == "未登录")
            {
                webView.CoreWebView2.Navigate("http://gyt.lpxxkj.com/index.php/Public/wx_login");
            }
            else if (MessageBox.Show("请确定是否退出，将清空", "是否退出", MessageBoxButtons.OKCancel) == DialogResult.OK)
            {
                storedCookies.Clear();
                linkLabel1.Text = "未登录";
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string doc_id = "";
            string morOr = "";
            string dateweek = "";
            string date = "";

            if (comboBox1.Text == "李鑫")
            {
                doc_id = "32056";
            }
            morOr = radioButton1.Checked ? "1" : "2";
            date = dateTimePicker1.Value.ToString("yyyy-MM-dd");
            dateweek = ((int)dateTimePicker1.Value.DayOfWeek).ToString();

            targetUrl = $"http://gyt.lpxxkj.com/index.php?m=&c=wx_index&a=doc_reg&week={dateweek}&day={morOr}&date={date}&doc_id={doc_id}";

            DateTime targetTime = DateTime.Today.AddHours(8).AddMinutes(29).AddSeconds(58);
            if (DateTime.Now > targetTime)
            {
                targetTime = targetTime.AddDays(1);
            }

            TimeSpan initialDelay = targetTime - DateTime.Now;
            timer = new System.Threading.Timer(TimerCallback, null, initialDelay, Timeout.InfiniteTimeSpan);
            button2.Enabled = false;
        }

        private void TimerCallback(object state)
        {
            Task.Run(() => SendRequestsWithCookies(targetUrl));
        }

        private async Task SendRequestsWithCookies(string pageUrl)
        {
            var handler = new HttpClientHandler();
            var cookieContainer = new CookieContainer();
            foreach (var cookie in storedCookies)
            {
                try
                {
                    var netCookie = new Cookie(cookie.Name, cookie.Value)
                    {
                        Domain = cookie.Domain,
                        Path = cookie.Path,
                        Expires = cookie.Expires
                    };
                    cookieContainer.Add(new Uri(pageUrl), netCookie);
                }
                catch (CookieException ex)
                {
                    Console.WriteLine($"Invalid cookie: {cookie.Name}, Value: {cookie.Value}, Exception: {ex.Message}");
                }
            }
            handler.CookieContainer = cookieContainer;
            client = new HttpClient(handler);

            var tasks = new List<Task>();
            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationToken token = cts.Token;

            for (int i = 0; i < Environment.ProcessorCount; i++)
            {
                tasks.Add(Task.Run(() => SendRequestsContinuously(token), token));
            }

            Task.Delay(10000).ContinueWith(_ => cts.Cancel());

            try
            {
                await Task.WhenAll(tasks);
                MessageBox.Show("Stopped sending requests after 10 seconds.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error occurred: {ex.Message}");
            }
        }

        private async Task SendRequestsContinuously(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var response = await client.GetStringAsync(targetUrl);
                    Console.WriteLine("Request sent successfully.");
                }
                catch (HttpRequestException httpEx)
                {
                    Console.WriteLine($"HTTP request failed: {httpEx.Message}");
                }
                catch (TaskCanceledException cancelEx)
                {
                    Console.WriteLine($"Request was canceled: {cancelEx.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to send request: {ex.Message}");
                }

                await Task.Delay(100); // Adding a delay to avoid overwhelming the server
            }
        }
    }
}
