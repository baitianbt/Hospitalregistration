using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;
using System.Net;
using System.Web;

namespace 合肥国医堂挂号
{
    public partial class Form1 : Form
    {
        // 网络校准时间API
        private const string TimeServerUrl = "http://worldtimeapi.org/api/ip";
        private HttpClient httpClient;
        private List<CoreWebView2Cookie> storedCookies;
        private System.Threading.Timer timer;
        private string targetUrl;
        private  HttpClient client;

        public Form1()
        {
            InitializeComponent();
            httpClient = new HttpClient();
            storedCookies = new List<CoreWebView2Cookie>();
            comboBox1.SelectedIndex = 0;
        }



        private async void Form1_Load(object sender, EventArgs e)
        {
            #region 初始化Web
            await webView.EnsureCoreWebView2Async(null);
            webView.CoreWebView2.Navigate("http://gyt.lpxxkj.com/index.php/Public/wx_login");
            webView.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
            #endregion

            timer1.Start();
        }

        private async void CoreWebView2_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (e.IsSuccess)
            {
                // Execute JavaScript to set the zoom level to 50%
                await webView.CoreWebView2.ExecuteScriptAsync("document.body.style.zoom = '50%';");

                // Wait for some time to ensure login is completed
                await Task.Delay(5000); // Adjust delay as needed

                // Fetch cookies after login
                await FetchCookies();
            }
            else
            {
                MessageBox.Show("Navigation failed.");
            }
        }


        private async Task FetchCookies()
        {
            storedCookies = await webView.CoreWebView2.CookieManager.GetCookiesAsync("http://gyt.lpxxkj.com/index.php?m=&c=WxIndex&a=hos_list"); // Replace with your domain
            
        }

        private  void timer1_Tick(object sender, EventArgs e)
        {
            // await UpdateTimeFromNetwork();
            label2.Text = DateTime.Now.ToString();
        }


        private async Task UpdateTimeFromNetwork()
        {
            try
            {
                // Fetch time data from the network time server
                var response = await httpClient.GetStringAsync(TimeServerUrl);
                // Example response handling
                dynamic json = Newtonsoft.Json.JsonConvert.DeserializeObject(response);
                string networkTime = json.datetime;

                // Update the label with the network time
                label2.Text = $"Current Time: {networkTime}";
            }
            catch (Exception ex)
            {
                // Handle exceptions (e.g., network issues)
                label2.Text = "Failed to retrieve time";
                Console.WriteLine(ex.Message);
            }
        }

        private async void BtnQuery_Click(object sender, EventArgs e)
        {
           await SendRequestWithCookies("http://gyt.lpxxkj.com/index.php/wxIndex/my_order");
        }



        /// <summary>
        /// 发送请求
        /// </summary>
        /// <returns></returns>
        private async Task SendRequestWithCookies(string url)
        {
            try
            {
                // Set cookies for HttpClient request
                var handler = new HttpClientHandler();
                var cookieContainer = new System.Net.CookieContainer();

                foreach (var cookie in storedCookies)
                {
                    try
                    {
                        // URL encode the cookie value
                        var cookieName = HttpUtility.UrlEncode(cookie.Name);
                        var cookieValue = HttpUtility.UrlEncode(cookie.Value);

                        // Print cookie details for debugging
                        Console.WriteLine($"Adding cookie: Name={cookieName}, Value={cookieValue}, Domain={cookie.Domain}, Path={cookie.Path}");

                        // Create a new System.Net.Cookie with the encoded name and value
                        var netCookie = new Cookie(cookieName, cookieValue)
                        {
                            Domain = cookie.Domain,
                            Path = cookie.Path
                        };

                        // Optional: Handle other properties if needed
                        if (cookie.Expires != DateTime.MinValue)
                        {
                            netCookie.Expires = cookie.Expires;
                        }

                        cookieContainer.Add(new Uri(url), netCookie);
                    }
                    catch (CookieException ex)
                    {
                        // Handle invalid cookie value
                        Console.WriteLine($"Invalid cookie: {cookie.Name}, Value: {cookie.Value}, Exception: {ex.Message}");
                    }
                }

                handler.CookieContainer = cookieContainer;
                var client = new HttpClient(handler);

                // Send a request and get HTML content
                var htmlContent = await client.GetStringAsync(url);

                // Display the HTML content in WebView2
                webView.CoreWebView2.NavigateToString(htmlContent);
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
            else if(MessageBox.Show("请确定是否退出，将清空","是否退出",MessageBoxButtons.OKCancel) == DialogResult.OK)
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

            // http://gyt.lpxxkj.com/index.php?m=&c=wx_index&a=doc_reg&week=4&day=2&date=2024-07-19&doc_id=32056
            if (comboBox1.Text == "李鑫")
            {
                doc_id = "32056";
            }
            if (radioButton1.Checked)
            {
                morOr = "1";
            }
            else
            {
                morOr = "2";
            }
            date = dateTimePicker1.Value.ToString("yyyy-MM-dd");
            dateweek = ((int)dateTimePicker1.Value.DayOfWeek).ToString();


             targetUrl = $"http://gyt.lpxxkj.com/index.php?m=&c=wx_index&a=doc_reg&week={dateweek}&day={morOr}&date={date}&doc_id={doc_id}";

           DateTime targetTime = DateTime.Today.AddHours(8).AddMinutes(29).AddSeconds(58);
            if (DateTime.Now > targetTime)
            {
                targetTime = targetTime.AddDays(1);
            }

            // Calculate the initial delay until the target time
            TimeSpan initialDelay = targetTime - DateTime.Now;

            // Set up the timer to trigger at the target time
            timer = new System.Threading.Timer(TimerCallback, null, initialDelay, TimeSpan.FromHours(24));

            button2.Enabled = false;
        }

        private void TimerCallback(object state)
        {
            Task.Run(() => SendRequestsWithCookies(targetUrl));
        }


        private async Task SendRequestsWithCookies(string PageUrl)
        {
            var handler = new HttpClientHandler();
            var cookieContainer = new System.Net.CookieContainer();
            foreach (var cookie in storedCookies)
            {
                try
                {
                    // URL encode the cookie value
                    var cookieName = HttpUtility.UrlEncode(cookie.Name);
                    var cookieValue = HttpUtility.UrlEncode(cookie.Value);

                    // Print cookie details for debugging
                    Console.WriteLine($"Adding cookie: Name={cookieName}, Value={cookieValue}, Domain={cookie.Domain}, Path={cookie.Path}");

                    // Create a new System.Net.Cookie with the encoded name and value
                    var netCookie = new Cookie(cookieName, cookieValue)
                    {
                        Domain = cookie.Domain,
                        Path = cookie.Path
                    };

                    // Optional: Handle other properties if needed
                    if (cookie.Expires != DateTime.MinValue)
                    {
                        netCookie.Expires = cookie.Expires;
                    }

                    cookieContainer.Add(new Uri(PageUrl), netCookie);
                }
                catch (CookieException ex)
                {
                    // Handle invalid cookie value
                    Console.WriteLine($"Invalid cookie: {cookie.Name}, Value: {cookie.Value}, Exception: {ex.Message}");
                }
            }
            handler.CookieContainer = cookieContainer;

             client = new HttpClient(handler);

            var tasks = new List<Task>();

            // 创建一个取消令牌源
            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationToken token = cts.Token;

            // 启动多个任务，每个任务在不同的线程上运行
            for (int i = 0; i < Environment.ProcessorCount; i++)
            {
                tasks.Add(Task.Run(() => SendRequestsContinuously(token), token));
            }

            // 设置一个定时器，10秒后取消所有任务
            Task.Delay(10000).ContinueWith(_ => cts.Cancel());

            try
            {
                // 等待所有任务完成
                await Task.WhenAll(tasks);
                MessageBox.Show("Stopped sending requests after 10 seconds.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error occurred: {ex.Message}");
            }
        }


        private  async Task SendRequestsContinuously(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var response = await client.GetStringAsync(targetUrl);
                    Console.WriteLine("Request sent successfully.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to send request: {ex.Message}");
                }
            }
        }
    }
}
