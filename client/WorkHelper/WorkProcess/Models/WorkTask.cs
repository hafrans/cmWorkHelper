using System;

namespace WorkProcess.Models
{
    public class WorkTask
    {
        //PI指令
        public string PI { get; set; }

        //TKI指令
        public string TKI { get; set; }

        //目标URL地址，访问任务详情
        public string TargetURL { get; set; }

        //任务名称
        public string Title { get; set; }

        //工单编号
        public string TaskCode { get; set; }

        //流程模板
        public string Template { get; set; }

        //当前节点
        public string CurrentNode { get; set; }

        /// <summary>
        /// 当前待办人
        /// </summary>
        public string Person { get; set; }

        //工单状态
        public string Status { get; set; }

        //工单到达时间
        public DateTime Arrival { get; set; }

        //完成时限
        public DateTime Limitation { get; set; }

        //发起人
        public string From { get; set; }

        //所属地市
        public string  OriginCity { get; set; }

        //Unknown 1
        public string HtName { get; set; }

        //Unknown 2
        public string FlowId { get; set; }


        public override string ToString()
        {
            return String.Format("[{0}-{1}-{2}-{3}-{4}-{5}-{6}-{7}-{8}-{9}-{10}]",
                new[] { Person,CurrentNode,Template,Status,Arrival.ToString(),TaskCode,OriginCity,PI,TKI,FlowId,HtName });
        }


    }
}
