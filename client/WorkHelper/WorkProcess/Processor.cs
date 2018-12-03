using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Diagnostics;
using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace WorkProcess
{
    public class Processor
    {
        string username;
        string password;

        //HttpRequestHeaders defaultRequestHeaders;
        Dictionary<String, HttpClient> clients = new Dictionary<string, HttpClient>();

        public Uri BaseUrlA { get; set; } = new Uri("http://211.137.182.252:8082/NMMP/");

        public Dictionary<String, String> requestA { get; private set; } = new Dictionary<string, string>
        {
            {"base","login.ilf"},
            {"login","CtrlUser.action?action=login&surl=jsp/home/main.jsp&furl=jsp/login.jsp" }
        };

        // add WorkTemplate

        public Processor(string username, string password)
        {
            this.username = username;
            this.password = password;

            //defaultRequestHeaders.Add("Accept", @"text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8");
            //defaultRequestHeaders.Add("Accept-Encoding","gzip,deflate");
            //defaultRequestHeaders.Add("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8,zh-TW;q=0.7");
            //defaultRequestHeaders.Add("User-Agent","Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/70.0.3538.67 Safari/537.36");
            //defaultRequestHeaders.Add("DNT", "1");
            //defaultRequestHeaders.Add("Referer", "http://211.137.182.252:8082/NMMP/login.ilf");
            //defaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");


        }

        public Dictionary<String, String> Login()
        {
            Debug.WriteLine("开始登陆");


            Task<Dictionary<String, String>> result = this.loginForBridgeLoginToken();
            return result.Result;//拿到虚拟字符串

        }

        /// <summary>
        /// 获取桥梁信息，到另一个网站
        /// 虽然是登陆一个网站，但是分析http数据包后发现
        /// 正确信息是在另一个网站获取的，所以要获取这个桥梁信息
        /// </summary>
        /// <param name="client">HttpClient 具有状态的http客户端 </param>
        /// <returns>Dictionary<String, String> , 桥梁用户名(u)，密码(p) </returns>
        private async Task<Dictionary<String, String>> getBridgeDictionary(HttpClient client)
        {
            /*
            * 虽然是登陆一个网站，但是正确信息是在另一个网站获取的，所以要获取这个桥梁信息
            */

            String resp = await client.GetStringAsync("jsp/eomssso.jsp");
            //Console.WriteLine(resp);
            //太坑人了，dom写在js的变量里面，艹

            string pattern = "SCROLLING=auto src=\".*u=(\\w+)&p=(\\w+)&.*\"></IFRAME>";
            Match match = Regex.Match(resp, pattern);

            if (!match.Success)//匹配上了用户名
            {
                return null;
            }
            return new Dictionary<String, String>
            {
                { "u",match.Groups[1].Value },
                { "p",match.Groups[2].Value }
            };
        }


        private async Task<Dictionary<String, String>> loginForBridgeLoginToken()
        {

            CookieContainer cookieContainer = new CookieContainer();
            HttpClientHandler httpClientHandler = new HttpClientHandler
            {
                CookieContainer = cookieContainer
            };
            HttpClient httpClient = new HttpClient(httpClientHandler);
            httpClient.BaseAddress = this.BaseUrlA;
            fillHeader(httpClient.DefaultRequestHeaders);

            //获取session
            String resp = await httpClient.GetStringAsync("login.ilf");
            if (resp.IndexOf("密码重置") <= 0) //不是首页
            {
                throw new Exception("首次首页访问失败！");
            }

            if (await loginByUserpwd(httpClient, username, password))
            {
                //返回访问login.ilf如果是有登陆后台的字符串则真成功登陆了，否则是虚登陆

                resp = await httpClient.GetStringAsync("login.ilf");
                if (resp.IndexOf("你有一个合同今天到期，请留意") <= 0) //不是首页
                {
                    throw new Exception("登陆检测失败！");
                }
                else
                {
                    Debug.WriteLine("登陆检测状态验证成功");
                }

            }
            else
            {
                throw new Exception("登陆失败!");
            }

            return await getBridgeDictionary(httpClient);

        }

        private async Task<bool> loginByUserpwd(HttpClient client, string username, string password)
        {
            Random random = new Random(DateTime.Now.Millisecond);

            FormUrlEncodedContent content = new FormUrlEncodedContent(new[]{
                    new KeyValuePair<String, String>("useraccount",username),
                    new KeyValuePair<string, string>("password",password),
                    new KeyValuePair<string, string>("pwdParam",password),
                    new KeyValuePair<string, string>("x", (random.NextDouble() * 20 + 3).ToString()),
                    new KeyValuePair<string, string>("y", (random.NextDouble() * 20 + 3).ToString()),
            });

            try
            {   
                
                HttpResponseMessage resp = await client.PostAsync(@"CtrlUser.action?action=login&surl=jsp/home/main.jsp&furl=jsp/login.jsp", content);
                if (resp.StatusCode == HttpStatusCode.OK)
                {
                    string respContentResult = await resp.Content.ReadAsStringAsync();
                    //Console.WriteLine(respContentResult);
                    if (respContentResult.IndexOf("window.location.href=\"/NMMP/login.ilf\"") > 0)
                    {
                        //登陆成功！
                        Debug.WriteLine("登陆成功");
                        //change referer first.
                        client.DefaultRequestHeaders.Referrer = new Uri(@"http://211.137.182.252:8082/NMMP/CtrlUser.action?action=login&surl=jsp/home/main.jsp&furl=jsp/login.jsp");
                        return true;
                    }
                }
                else
                {
                    throw new Exception("服务器端访问异常" + resp.StatusCode.ToString());
                }
            }
            catch (Exception e)
            {
                throw e;
            }

            Debug.WriteLine("登陆失败");
            return false;
        }

        internal static void fillHeader(HttpRequestHeaders defaultRequestHeaders)
        {
            defaultRequestHeaders.Add("Accept", @"text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8");
            defaultRequestHeaders.Add("Accept-Encoding", "gzip,deflate");
            defaultRequestHeaders.Add("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8,zh-TW;q=0.7");
            defaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/70.0.3538.67 Safari/537.36");
            defaultRequestHeaders.Add("DNT", "1");
            defaultRequestHeaders.Add("Referer", "http://211.137.182.252:8082/NMMP/login.ilf");
            defaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
        }


    }
}
