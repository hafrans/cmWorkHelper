using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WorkProcess
{
    public enum WorkerMessageType
    {
        INFO, WARNING, ERROR
    }

    public delegate void WorkEvent(int rowNumber, DataGridViewRow obj, int changeColumn, Color? color);

    public delegate void WorkMessageEvent(int rowNumber, DataGridViewRow obj, string eventMessage, WorkerMessageType type);

   
    public interface IWorkComponent
    {
        event WorkEvent FormEvent;

        event WorkMessageEvent MessageEvent;

        void InitEvents(WorkEvent workEvent, WorkMessageEvent messageEvent);

        void UnDeploy();

        void Deploy(int rowNumber, DataGridViewRow row, object[] obj);

        void Run();
    }
}
