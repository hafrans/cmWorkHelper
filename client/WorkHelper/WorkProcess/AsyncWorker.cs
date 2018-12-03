using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WorkProcess.Models;

namespace WorkProcess
{

    



    public class AsyncWorker
    {
        public static event WorkEvent FormEvent;
        public static event WorkMessageEvent MessageEvent;
        public static Dictionary<int, bool> userRun = new Dictionary<int, bool>();

        public AsyncWorker()
        {
           
        }

        private static void DeployThread(object state)
        {
            object[] obj = (object[]) state;
            int rowNumber = (int) obj[0];
            DataGridViewRow entity = obj[1] as DataGridViewRow;
            string username = entity.Cells[1].Value as String;
            string password = obj[2] as String;


            ////
            //
            int dotimes = (int)entity.Cells[4].Value;
            int left = (int)entity.Cells[3].Value;
            int duration = (int)entity.Cells[3].Value;

            entity.Cells[5].Style.BackColor = Color.LawnGreen;
            entity.Selected = false;

            try
            {
                entity.Cells[5].Value = "登陆中...";
                Processor processor = new Processor(username,password);
                Dictionary<string,string> keyValuePairs =  processor.Login();
                while (userRun.TryGetValue(rowNumber,out bool res) && res)
                {


                    left--;
                    if (left <= 0)
                    {
                        left = duration;
                        //执行操作
                        ///////////////////////////
                        ///需要重构
                        ///
                        entity.Cells[5].Value = "寻找受理中";
                        WorkTaskAcceptExecutor workTaskAccept = new WorkTaskAcceptExecutor(keyValuePairs);
                        Task<bool> b = workTaskAccept.Login();

                        if (b.Result)//二次登陆成功
                        {
                            List<WorkTask> workTasks = workTaskAccept.FetchTaskList().Result;
                            if(workTasks.Count > 0)
                            {
                                List<WorkTask> needAccept = workTaskAccept.FetchPendingTaskList(workTasks);
                                if(needAccept.Count > 0)
                                {//
                                    Debug.WriteLine("受理"+ needAccept.Count+"个");
                                    foreach (var task in needAccept)
                                    {
                                        if (workTaskAccept.AcceptOneTask(task).Result)
                                        {
                                            Debug.WriteLine("受理成功");
                                            entity.Cells[5].Style.BackColor = Color.Green;
                                            entity.Cells[5].Value = "受理成功";
                                            dotimes++;
                                            Thread.Sleep(3000);
                                            entity.Cells[5].Style.BackColor = Color.LawnGreen;
                                            /**
                                                     * 带着break只说明这个账户只受理一个
                                                     */
                                           /// break;
                                        }
                                        else
                                        {
                                            Debug.WriteLine("受理失败");
                                        }
                                    }

                                    entity.Cells[4].Value = dotimes;
                                    //通知入库
                                    if(FormEvent != null)
                                    {
                                        FormEvent(rowNumber, entity, 4, null);
                                    }
                                }
                                else
                                {
                                    Debug.WriteLine("没有待受理任务");
                                    entity.Cells[5].Value = "无待受理任务";
                                    Thread.Sleep(1000);
                                }
                            }
                            else
                            {
                                Debug.WriteLine("没有任何任务");
                                entity.Cells[5].Value = "无任务";
                                Thread.Sleep(1000);
                            }


                        }




                        
                        //userRun[rowNumber] = false;
                    }
                    
                    entity.Cells[5].Value = left;
                    Debug.WriteLine("DO:" + DateTime.Now.ToLongDateString()+" "+DateTime.Now.ToLongTimeString());
                    Thread.Sleep(1000);
                }

                //处理结束
                entity.Cells[5].Value = "停止执行";
                entity.Cells[5].Style.BackColor = Color.OrangeRed;

            }
            catch(AggregateException e)
            {
                e.Handle((err) =>
                {
                    switch (err.GetType().ToString())
                    {
                        case "Exception":
                            if (MessageEvent != null)
                            {
                                MessageEvent(rowNumber, entity, err.Message, WorkerMessageType.ERROR);

                            }
                            entity.ErrorText = e.Message;
                            userRun[rowNumber] = false;
                            entity.Cells[5].Value = "错误";
                            entity.Cells[5].Style.BackColor = Color.Red;
                            break;
                        default:
                            Debug.WriteLine(err.GetType().ToString());
                            if(MessageEvent != null)
                            {
                                MessageEvent(rowNumber, entity, err.Message, WorkerMessageType.ERROR);
                               
                            }
                            entity.ErrorText = e.Message;
                            userRun[rowNumber] = false;
                            entity.Cells[5].Value = "错误";
                            entity.Cells[5].Style.BackColor = Color.Red;
                            break;
                    }

                    return true;
                });
            }


        }

        public static void Deploy(int rowNumber, DataGridViewRow row,string passwd)
        {
            if (userRun.ContainsKey(rowNumber))
            {
                userRun.Remove(rowNumber);
            }
            userRun.Add(rowNumber, true);
            ThreadPool.QueueUserWorkItem(DeployThread, new object[] { rowNumber, row ,passwd});
        }

        public static void UnDeployAll()
        {
            userRun.Clear();
        }


      
    }
}
