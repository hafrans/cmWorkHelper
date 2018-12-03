using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using WorkProcess.Models;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading;

namespace WorkProcess
{
    public class WorkTaskAcceptExecutor : IDisposable
    {

        Dictionary<string, string> pairs;
        HttpClient client = null;


        public Uri BaseUrl { get; set; } = new Uri("http://211.137.182.250:8083/");

        public WorkTaskAcceptExecutor(Dictionary<String, String> valuePairs, HttpClient client = null)
        {
            this.pairs = valuePairs;
            if (client == null)
            {
                
                IWebProxy proxy = new WebProxy(new Uri("http://127.0.0.1:8888"));
                HttpClientHandler handler = new HttpClientHandler
                {
                    Proxy = proxy,
                    UseProxy = false
                };
                this.client = new HttpClient(handler);
                Processor.fillHeader(this.client.DefaultRequestHeaders);

            }
            else
            {
                this.client = client;
            }
            //重置信息
            this.client.BaseAddress = this.BaseUrl;
            this.client.DefaultRequestHeaders.Referrer = new Uri(@"http://211.137.182.252:8082/NMMP/login.ilf"); //登陆地址
            Debug.WriteLine(this.BaseUrl.AbsoluteUri);

        }

        /// <summary>
        /// 获取这个账户的第一页任务
        /// </summary>
        /// <param name="page">页号，暂时不可用</param>
        /// <returns>Task< List<WorkTask> > 工单实体类 </returns>
        public async Task< List<WorkTask> > FetchTaskList(int page=0)
        {
            //修改referer先
            this.client.DefaultRequestHeaders.Referrer = new Uri(@"http://211.137.182.250:8083/eoms3/jsp/home/main.jsp");

            //做请求硬刚
            HttpResponseMessage taskResp = await this.client.GetAsync("HumanTaskWeb/share/shareAction.do?parm=myTask&template_display_name=%20&pageStyle=2&first=0");
            if (taskResp.StatusCode != HttpStatusCode.OK)
            {
                //有问题
                throw new Exception("获取工单列表失败！");
            }

            //开始解析html获取一些东西

            HtmlDocument htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(await taskResp.Content.ReadAsStringAsync());
            List<WorkTask> workTasks = new List<WorkTask>();
            var nodes = htmlDocument.DocumentNode.SelectNodes(@"//td[@title]");
            if (nodes != null)
            {
                //Debug.WriteLine(nodes.Count);
                //原先的网站bug太严重，只能通过兄弟节点进行访问
                foreach (var personNode in nodes)
                {

                    WorkTask tmpWorkTask = new WorkTask();

                    //解析工单的关键数据

                    HtmlNode firstNode = personNode.PreviousSibling.PreviousSibling.PreviousSibling.PreviousSibling.PreviousSibling.PreviousSibling
                                                   .PreviousSibling.PreviousSibling.PreviousSibling.PreviousSibling.PreviousSibling.PreviousSibling;

                  
                    string firstNodeValue = firstNode.ChildNodes[0].GetAttributeValue("value","");
                    if(firstNodeValue != "")
                    {
                        string[] vals = firstNodeValue.Split(',');
                        foreach(string s in vals)
                        {
                            if(s.IndexOf(':')> 0)
                            {
                                string[] subvals = s.Split(':');
                                switch (subvals[0])
                                {
                                    case "_PI":
                                        tmpWorkTask.PI = subvals[1];
                                        break;
                                    case "_TKI":
                                        tmpWorkTask.TKI = subvals[1];
                                        break;
                                }
                            }
                        }
                    }
                    else
                    {
                        Debug.WriteLine("获取失败！");
                        continue;
                    }


                   string secondNodeValue =  personNode.PreviousSibling.PreviousSibling
                               .PreviousSibling.PreviousSibling
                               .PreviousSibling.PreviousSibling
                               .PreviousSibling.PreviousSibling
                               .ChildNodes[0].ChildNodes[0].GetAttributeValue("onclick", "");
                    if(secondNodeValue != "")
                    {
                        string pattern = @".*?htName=(.*)?&roleName.*?&flowId=(.*)?&state.*";
                        string pattern2 = @"f_doAction\(""(.*)""\)";
                        Match m = Regex.Match(secondNodeValue, pattern);
                        if (!m.Success)
                        {
                            Debug.WriteLine("匹配失败："+secondNodeValue.Substring(0,10));
                        }
                        else
                        {
                            tmpWorkTask.HtName = m.Groups[1].Value;
                            tmpWorkTask.FlowId = m.Groups[2].Value;
                        }
                        Match m2 = Regex.Match(secondNodeValue, pattern2);
                        tmpWorkTask.TargetURL = m2.Groups[1].Value.Replace(" ", "%20").Substring(1);

                    }
                    else
                    {
                        Debug.WriteLine("获取SecondNodeValue失败");
                        continue;
                    }


                    //工单标题
                    tmpWorkTask.Title = personNode.PreviousSibling.PreviousSibling.PreviousSibling.PreviousSibling.PreviousSibling.PreviousSibling.PreviousSibling.PreviousSibling.InnerText.Trim();

                    //工单编号
                    tmpWorkTask.TaskCode = personNode.PreviousSibling.PreviousSibling.PreviousSibling.PreviousSibling.PreviousSibling.PreviousSibling.InnerText.Trim();

                    //填充流程模板
                    tmpWorkTask.Template = personNode.PreviousSibling.PreviousSibling.PreviousSibling.PreviousSibling.InnerText.Trim();
                    
                    //填充当前节点
                    tmpWorkTask.CurrentNode = personNode.PreviousSibling.PreviousSibling.InnerText.Trim();
                   
                    //填充待办人
                    tmpWorkTask.Person = personNode.InnerText.Trim();

                    //填充状态
                    tmpWorkTask.Status = personNode.NextSibling.NextSibling.InnerText.Trim();

                    //开始时间
                    tmpWorkTask.Arrival = DateTime.Parse(personNode.NextSibling.NextSibling.NextSibling.NextSibling.InnerText.Trim());

                    //省市
                    tmpWorkTask.OriginCity = personNode.NextSibling.NextSibling.NextSibling.NextSibling.NextSibling.NextSibling.InnerText.Trim();

                    //发起人
                    tmpWorkTask.From = personNode.NextSibling.NextSibling.NextSibling.NextSibling.NextSibling.NextSibling.NextSibling.NextSibling.InnerText.Trim();

                    //截止时间
                    tmpWorkTask.Limitation = DateTime.Parse(personNode.NextSibling.NextSibling.NextSibling.NextSibling.NextSibling.NextSibling.NextSibling.NextSibling.NextSibling.NextSibling.InnerText.Trim());


                    workTasks.Add(tmpWorkTask);
                    

                }
            }
           
            return workTasks;
        }

        public async Task<bool> Login()
        {

            if (!pairs.TryGetValue("u", out string usr))
            {
                throw new Exception("桥梁用户名不存在！");
            }

            if (!pairs.TryGetValue("p", out string pwd))
            {
                throw new Exception("桥梁密码不存在！");
            }
            //process
            Debug.WriteLine("Begin Login");
            //先获取一个session再搞，模拟正常访问。
            String refererForLogin = String.Format(@"/eoms3/LogonServlet?u={0}&p={1}&url=/HumanTaskWeb/share/shareAction.do?parm=myTask&template_display_name=%20&pageStyle=2&first=0", new object[] { pairs["u"], pairs["p"] });
            String resp = await this.client.GetStringAsync(refererForLogin);



            //进行第二个网站的登陆

            //j_security_check

            FormUrlEncodedContent formUrlEncodedContent = new FormUrlEncodedContent(new[] {

                new KeyValuePair<string,string>("j_username",pairs["u"]),
                new KeyValuePair<string,string>("j_password",pairs["p"]),
                new KeyValuePair<string,string>("url",@"/HumanTaskWeb/share/shareAction.do?parm=myTask&template_display_name=%20&pageStyle=2&first=0")

            });
            this.client.DefaultRequestHeaders.Referrer = new Uri(this.BaseUrl.AbsoluteUri + refererForLogin);
            HttpResponseMessage httpResponseMessage = await this.client.PostAsync("eoms3/j_security_check", formUrlEncodedContent);
            //Console.WriteLine(await httpResponseMessage.Content.ReadAsStringAsync());
            if (httpResponseMessage.StatusCode == HttpStatusCode.OK)
            {
                //lastcheck login status
                //不想check，以后这个地方出了问题再说

                //直接获取任务列表
                Debug.WriteLine("二次登陆成功");
                //await this.ProcessTaskList();
                return true;

            }
            else
            {
                throw new Exception("二次登陆失败！");

            }

        }

        /// <summary>
        /// 筛选所有待处理的工单
        /// </summary>
        /// <param name="workTasks"></param>
        /// <returns></returns>
        public List<WorkTask> FetchPendingTaskList(List<WorkTask> workTasks)
        {
            return workTasks.FindAll((e) => {
                Debug.WriteLine(e.Status);
                return e.Status == "待受理";
            });
        }

        /// <summary>
        /// 能够解决莫名奇妙的问题
        /// </summary>
        /// <returns></returns>
        private async Task<bool> AssholeRequest(WorkTask task)
        {
            FormUrlEncodedContent formUrlEncodedContentCheck = new FormUrlEncodedContent(new[] {
                new KeyValuePair<string, string>("piid","_PI:"+task.PI)
            });
            HttpResponseMessage resp = await this.client.PostAsync("/eoms3/checkhdyzflagAction!checkflag.ilf", formUrlEncodedContentCheck);
            return resp.StatusCode == HttpStatusCode.OK;
        }

        /// <summary>
        /// 创造受理订单
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        public async Task<FormUrlEncodedContent> getAcceptFormContent(WorkTask task)
        {

            //获取表单用来

            if (!await this.AssholeRequest(task))
            {
                throw new Exception("未知错误(ASREQ)");
            }

            string content = await this.client.GetStringAsync(this.BaseUrl.AbsoluteUri+task.TargetURL);

            HtmlDocument htmlDocument = new HtmlDocument();

            htmlDocument.LoadHtml(content);
           // Debug.Write(content);

            List<KeyValuePair<string, string>> list = new List<KeyValuePair<string, string>>();

            string[] strings = new string[]
            {
                "dealMode","dealtimeLmt","faultEquimanu","falseAlarm","isDealEffe","isDealEffeReason","owerName","ownerId","roleName",
                "companyName","companyId","deptName","deptId","cellPhone","email","accepteTime","dealTime","flowId","id","piid","state","title","tkiid","htName",
                "htDescription","prtFormNo","transferGroupname","sendMode","commonCol3"
            };//所有提交字段
            
            foreach(string s in strings){
                HtmlNode node = htmlDocument.DocumentNode.SelectSingleNode("//form[@name='form1']//input[@name='" + s + "']|//form[@name='form1']//textarea[@name='commonCol3']");
                string targetString = "";
                if (node == null)//选不到node？
                {
                    Debug.WriteLine(">>>"+s);
                }
                else
                {
                    targetString = htmlDocument.DocumentNode.SelectSingleNode("//form[@name='form1']//input[@name='" + s + "']|//form[@name='form1']//textarea[@name='commonCol3']").GetAttributeValue("value", "");
                }
                list.Add(new KeyValuePair<string, string>(s,targetString));
            }

            return new FormUrlEncodedContent(list);
        }

        public async Task<bool> AcceptOneTask(WorkTask task)
        {
            if (task == null)
            {
                throw new ArgumentNullException("WorkTask 是空！");
            }

            if (this.client == null)
            {
                throw new NullReferenceException("HttpClient is Null！");
            }



            //验证flag1 /eoms3/checkhdyzflagAction!checkflag.ilf 沙雕请求

            
            if(! await this.AssholeRequest(task))
            {
                throw new Exception("未知错误(ASREQ)");
            }


            FormUrlEncodedContent formUrlEncodedContentCheck = new FormUrlEncodedContent(new[] {
                new KeyValuePair<string, string>("piid","_PI:"+task.PI)
            });

            HttpResponseMessage resp = await this.client.PostAsync("/eoms3/checkhdyzflagAction!checkflag.ilf", formUrlEncodedContentCheck);
            
            
            
            if ("false" != await resp.Content.ReadAsStringAsync())
            {
                throw new Exception("未知错误");
            }


            //拼接请求

            FormUrlEncodedContent formUrlEncodedContent = new FormUrlEncodedContent(new[]{
                  new KeyValuePair<string,string>("flowid",task.FlowId),
                  new KeyValuePair<string, string>("htname",task.HtName),
                  new KeyValuePair<string, string>("tkiid","_TKI:"+task.TKI),
                  new KeyValuePair<string, string>("piid","_PI:"+task.PI)
            });
            string getQuery = (await formUrlEncodedContent.ReadAsStringAsync()).Replace("%3A", ":");
            //Debug.WriteLine(getQuery);


            //提交请求
            this.client.DefaultRequestHeaders.Add("X-Requested-With","XMLHttpRequest");

            this.client.DefaultRequestHeaders.Referrer = new Uri(this.BaseUrl.AbsoluteUri + task.TargetURL);
            //Console.WriteLine(this.BaseUrl.AbsoluteUri + task.TargetURL);


            resp = await this.client.PostAsync("/eoms3/tBnsFaultTaskAction!ifAnyOneAccept.ilf?" + getQuery,null);
            
            this.client.DefaultRequestHeaders.Remove("X-Requested-With");
          
            if (resp.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception("待受理状态修改异常！");
            }
            else
            {
                string respContent = await resp.Content.ReadAsStringAsync();
                //Console.WriteLine(respContent);
                
                if(respContent.IndexOf("yes") >= 0)
                {

                    //提交受理请求

                    HttpResponseMessage resps = await this.client.PostAsync("/eoms3/tBnsFaultTaskAction!accept.ilf",await this.getAcceptFormContent(task));
                    string contentString = await resp.Content.ReadAsStringAsync();
                   
                    if (resp.StatusCode == HttpStatusCode.OK)
                    {
                        Debug.WriteLine("受理成功");
                        return true;
                    }
                    else
                    {
                        Debug.WriteLine("受理失败");
                        return false;
                    } 
                }
                else
                {
                    Debug.WriteLine("修改失败！");
                    return false;
                }
            }
        }

        public void Dispose()
        {
           if(this.client != null)
            {
                this.client.Dispose();
            }
        }
    }
}
