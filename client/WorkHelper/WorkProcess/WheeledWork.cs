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
    public class WheeledWork : IWorkComponent
    {
        public event WorkEvent FormEvent;
        public event WorkMessageEvent MessageEvent;

        public bool start = false;

        public static Dictionary<int, bool> userRun = new Dictionary<int, bool>();
        public static Dictionary<int, object[]> userEntity = new Dictionary<int, object[]>();
        public static Dictionary<int, DataGridViewRow> userRow = new Dictionary<int, DataGridViewRow>();



        private void DeployThread(object state)
        {

            while (start)
            {
                foreach (var item in userEntity)
                {
                    int rowNumber = item.Key;

                    object[] entity = userEntity[item.Key];
                    DataGridViewRow row = userRow[item.Key];

                    int dotimes = (int)row.Cells[4].Value;
                    int left = (int)row.Cells[3].Value;
                    int duration = (int)row.Cells[3].Value;


                    string usrname = entity[1] as string;
                    string password = entity[2] as string;

                    if (!userRun.ContainsKey(item.Key) || ! userRun[item.Key])
                    {
                        //标橙色
                        row.Cells[5].Style.BackColor = Color.Orange;
                        row.Cells[5].Value = "不再运行";
                        continue;
                    }

                    //标绿色
                    row.Cells[5].Style.BackColor = Color.LawnGreen;
                    row.Selected = false;


                    try
                    {

                        //登陆
                        row.Cells[5].Value = "登陆中";
                        Processor processor = new Processor(usrname, password);
                        Dictionary<string, string> keyValuePairs = processor.Login();


                        while (userRun.ContainsKey(item.Key) && userRun[item.Key])
                        {

                            if (left == 0)
                            {
                                row.Cells[5].Value = "寻找受理中";
                                WorkTaskAcceptExecutor workTaskAccept = new WorkTaskAcceptExecutor(keyValuePairs);
                                Task<bool> b = workTaskAccept.Login();

                                if (b.Result)//二次登陆成功
                                {
                                    List<WorkTask> workTasks = workTaskAccept.FetchTaskList().Result;
                                    if (workTasks.Count > 0)
                                    {
                                        List<WorkTask> needAccept = workTaskAccept.FetchPendingTaskList(workTasks);
                                        if (needAccept.Count > 0)
                                        {//
                                            Debug.WriteLine("受理" + needAccept.Count + "个");
                                            foreach (var task in needAccept)
                                            {
                                                if (workTaskAccept.AcceptOneTask(task).Result)
                                                {
                                                    Debug.WriteLine("受理成功");
                                                    row.Cells[5].Style.BackColor = Color.Green;
                                                    row.Cells[5].Value = "受理成功";
                                                    dotimes++;
                                                    Thread.Sleep(3000);
                                                    row.Cells[5].Style.BackColor = Color.LawnGreen;
                                                    /**
                                                     * 带着break只说明这个账户只受理一个
                                                     */
                                                    break;
                                                }
                                                else
                                                {
                                                    Debug.WriteLine("受理失败");
                                                }
                                            }

                                            row.Cells[4].Value = dotimes;
                                            //通知入库
                                            FormEvent?.Invoke(rowNumber, row, 4, null);


                                            //下一个账户
                                            
                                            break;
                                        }
                                        else
                                        {
                                            Debug.WriteLine("没有待受理任务");
                                            row.Cells[5].Value = "无待受理任务";
                                            Thread.Sleep(1000);
                                        }
                                    }
                                    else
                                    {
                                        Debug.WriteLine("没有任何任务");
                                        row.Cells[5].Value = "无任务";
                                        Thread.Sleep(1000);
                                    }


                                }

                                left = duration+1;
                               
                            }






                            left--;

                            row.Cells[5].Value = left;//通知更改
                            Debug.WriteLine("DO:" + DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToLongTimeString());
                            Thread.Sleep(1000);
                        }
                        //运行结束
                        row.Cells[5].Style.BackColor = Color.Yellow;
                        row.Cells[5].Value = "等待运行";




                    }
                    catch (AggregateException e)
                    {
                        e.Handle((err) =>
                        {
                            switch (err.GetType().ToString())
                            {
                                case "Exception":
                                    MessageEvent?.Invoke(rowNumber, row, err.Message, WorkerMessageType.ERROR);
                                    row.ErrorText = err.Message;
                                    userRun[rowNumber] = false;

                                    row.Cells[5].Value = "错误";
                                    row.Cells[5].Style.BackColor = Color.Red;

                                    break;
                                default:
                                    Debug.WriteLine(err.GetType().ToString());
                                    MessageEvent?.Invoke(rowNumber, row, err.Message, WorkerMessageType.ERROR);
                                    row.ErrorText = err.Message;
                                    userRun[rowNumber] = false;
                                    row.Cells[5].Value = "错误";
                                    row.Cells[5].Style.BackColor = Color.Red;
                                    break;
                            }

                            return true;
                        });
                    }



                }
                //主循环
                //Thread.Sleep(3000);
            }
            if (!start)
            {
                foreach(var i in userRow)
                {
                    i.Value.Cells[5].Value = "停止执行";
                    i.Value.Cells[5].Style.BackColor = Color.Red;
                }
            }


        }



        private void DeployThread2(object state)
        {
            object[] obj = (object[])state;
            int rowNumber = (int)obj[0];
            DataGridViewRow entity = obj[1] as DataGridViewRow;
            string username = entity.Cells[1].Value as String;
            string password = obj[2] as String;


            ////
            //
            int dotimes = (int)entity.Cells[4].Value;
            int left = (int)entity.Cells[3].Value;
            int duration = (int)entity.Cells[3].Value;

            //标绿色
            entity.Cells[5].Style.BackColor = Color.LawnGreen;
            entity.Selected = false;

            try
            {

                Processor processor = new Processor(username, password);
                Dictionary<string, string> keyValuePairs = processor.Login();
                while (userRun.TryGetValue(rowNumber, out bool res) && res)
                {


                    left--;
                    if (left <= 0)
                    {
                        left = duration;
                        //执行操作
                        ///////////////////////////
                        ///需要重构
                        ///
                        entity.Cells[5].Value = "处理中...";
                        WorkTaskAcceptExecutor workTaskAccept = new WorkTaskAcceptExecutor(keyValuePairs);
                        Task<bool> b = workTaskAccept.Login();

                        if (b.Result)//二次登陆成功
                        {
                            List<WorkTask> workTasks = workTaskAccept.FetchTaskList().Result;
                            if (workTasks.Count > 0)
                            {
                                List<WorkTask> needAccept = workTaskAccept.FetchPendingTaskList(workTasks);
                                if (needAccept.Count > 0)
                                {//
                                    Debug.WriteLine("受理" + needAccept.Count + "个");
                                    foreach (var task in needAccept)
                                    {
                                        if (workTaskAccept.AcceptOneTask(task).Result)
                                        {
                                            Debug.WriteLine("受理成功");
                                            dotimes++;
                                            Thread.Sleep(3000);
                                        }
                                        else
                                        {
                                            Debug.WriteLine("受理失败");
                                        }
                                    }

                                    entity.Cells[4].Value = dotimes;
                                    //通知入库
                                    if (this.FormEvent != null)
                                    {
                                        FormEvent(rowNumber, entity, 4, null);
                                    }
                                }
                                else
                                {
                                    Debug.WriteLine("没有待受理任务");
                                }
                            }
                            else
                            {
                                Debug.WriteLine("没有任何任务");
                            }


                        }





                        //userRun[rowNumber] = false;
                    }

                    entity.Cells[5].Value = left;
                    Debug.WriteLine("DO:" + DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToLongTimeString());
                    Thread.Sleep(1000);
                }

                //处理结束
                entity.Cells[5].Value = "停止执行";
                entity.Cells[5].Style.BackColor = Color.OrangeRed;//标红色

            }
            catch (AggregateException e)
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
                            if (MessageEvent != null)
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

        public void Deploy(int rowNumber, DataGridViewRow row, object[] obj)
        {
            if (userRun.ContainsKey(rowNumber))
            {
                userRun.Remove(rowNumber);
                
               
            }
            if (userEntity.ContainsKey(rowNumber))
            {
                userEntity.Remove(rowNumber);
                userRow.Remove(rowNumber);
            }
            row.ErrorText = "";
            userRun.Add(rowNumber, true);
            userEntity.Add(rowNumber, obj);
            userRow.Add(rowNumber, row);

        }

        public void InitEvents(WorkEvent workEvent, WorkMessageEvent messageEvent)
        {
            this.FormEvent -= workEvent;
            this.FormEvent = workEvent;
            this.MessageEvent -= messageEvent;
            this.MessageEvent = messageEvent;
            userEntity.Clear();
            userRow.Clear();
            userRun.Clear();
        }

        public void UnDeploy()
        {
            start = false;
            userRun.Clear();
        }

        public void Run()
        {
            start = true;
            if (start)
            {
                foreach (var i in userRow)
                {
                    i.Value.Cells[5].Value = "等待执行";
                    i.Value.Cells[5].Style.BackColor = Color.Yellow;
                }
            }
            ThreadPool.QueueUserWorkItem(this.DeployThread);
        }
    }
}
