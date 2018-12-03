using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WorkProcess
{
    /// <summary>
    ///  Delegate AsyncWorker.cs
    ///  
    /// </summary>
    public class SimultaneousWork : IWorkComponent
    {
        //占位
        public event WorkEvent FormEvent;
        public event WorkMessageEvent MessageEvent;

       
        /// <summary>
        /// 
        /// </summary>
        /// <param name="rowNumber"></param>
        /// <param name="row"></param>
        /// <param name="obj">User 类 id,</param>
        public void Deploy(int rowNumber, DataGridViewRow row, object[] obj)
        {
            row.ErrorText = "";
            AsyncWorker.Deploy(rowNumber, row, obj[2] as string);
        }

        public void InitEvents(WorkEvent workEvent, WorkMessageEvent messageEvent)
        {
            this.FormEvent += workEvent;
            this.MessageEvent += messageEvent;

            AsyncWorker.FormEvent -= workEvent;
            AsyncWorker.FormEvent += workEvent;
            AsyncWorker.MessageEvent -= messageEvent;
            AsyncWorker.MessageEvent += messageEvent;

        }

        public void Run()
        {
            
        }

        public void UnDeploy()
        {
            AsyncWorker.UnDeployAll();
        }
    }
}
