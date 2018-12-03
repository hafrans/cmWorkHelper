
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Windows.Forms;
using WorkHelperGUI.Models;
using WorkHelperGUI.Services;
using WorkProcess;



namespace WorkHelperGUI
{
    public partial class Form1 : Form
    {
        private List<User> users = new List<User>();
        private bool start = false;
        private IWorkComponent workComponent = null;
        public static int userCount = 0;

        public Form1()
        {
            string limits = "2018/11/28 12:00";
#if TESTUSE
            if (DateTime.Now < DateTime.Parse(limits))
            {
                if( MessageBox.Show("当前是测试版本，该版本在"+limits+"后过期，是否继续试用？","提示",MessageBoxButtons.YesNo,MessageBoxIcon.Warning) != DialogResult.Yes)
                {
                    System.Environment.Exit(-1);
                }
            }
            else
            {
                MessageBox.Show("该版本已经过期，请使用正版软件！");
                System.Environment.Exit(-1);
            }

#endif

#if CHECKSERVER
            string myLimit = "";
            bool notify = false;
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(BaseConfig.checkServer);
            try
            {
                JsonTextReader reader = new JsonTextReader(new StringReader(client.GetStringAsync("/status").Result));
                while (reader.Read())
                {
                    if (reader.TokenType == JsonToken.PropertyName)
                    {
                        switch (reader.Value)
                        {
                            case "ProductId":
                                reader.Read();
                                break;
                            case "Enable":
                                reader.ReadAsBoolean();
                                if (!((bool)reader.Value))
                                {
                                    MessageBox.Show("您没有该软件的使用权限，请联系软件管理员！");
                                    System.Environment.Exit(-1);
                                }
                                break;
                            case "Limit":
                                reader.Read();
                                myLimit = reader.Value as String;
                                break;
                            case "Notify":
                                reader.ReadAsBoolean();
                                notify = (bool)reader.Value;
                                break;




                        }
                    }
                }

                if (myLimit.Length > 5) {
                    if (DateTime.Now > DateTime.Parse(myLimit))
                    {
                        MessageBox.Show("该版本已经过期("+myLimit+")，请使用正版软件！");
                        System.Environment.Exit(-1);
                    }
                    else
                    {
                        if (notify)
                        {
                            if (MessageBox.Show("该软件于" + myLimit + "后过期，是否继续使用？", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                            {
                                System.Environment.Exit(-1);
                            }
                        }
                    }
                }

            }catch(AggregateException e)
            {
                e.Handle((err) =>
               {
                   MessageBox.Show("服务器获取信息失败，软件无法正常使用！");
                   System.Environment.Exit(-1);
                   return true;
               });
            }

           

                
#endif
       



            this.StartPosition = FormStartPosition.CenterScreen;
            try
            {
                DataBaseService.InitDefaultBase();
            }
            catch(Exception es)
            {
                MessageBox.Show(es.Message);
                System.Environment.Exit(-1);
            }




           
            this.InitWorkComponent(new SimultaneousWork());
            InitializeComponent();
            this.initDataGrid();
            this.InitDatabase();


            this.comboBox1.SelectedIndex = 0;
            this.eventLog1.Source = "Application";
            //this.eventLog1.WriteEntry("Form Load? Hafrans", System.Diagnostics.EventLogEntryType.FailureAudit);

           
            
            
        }

        public void InitDatabase()
        {
            
            this.dataGridView1.Rows.Clear();
            this.users.Clear();
            using (DataBaseService s = new DataBaseService())
            {
                users = s.findAllUsers();
                userCount = users.Count;
                //fillDataGrid
                int count = 0;
                foreach (User user in users)
                {
                    user.RowNum = count++;
                    
                    this.dataGridView1.Rows.Add(new object[]
                    {
                        user.Id,
                        user.Username,
                        user.Operation,
                        user.Duration,
                        user.Dotimes,
                        "未执行",
                    });
                }

            }
            this.dataGridView1.ClearSelection();
        }

        private void Worker_MessageEvent(int rowNumber, DataGridViewRow obj, string eventMessage, WorkerMessageType type)
        {
            MessageBox.Show(this.users[rowNumber].Username + "出现了问题：" + eventMessage, "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void Change_DGV(int rowNumber, DataGridViewRow obj, int changeColumn, Color? color)
        {
            Debug.WriteLine("hafrans！"+obj.Cells[4].Value);
            using (DataBaseService s = new DataBaseService())
            {
                //find One User
                User user = this.users[rowNumber];
                switch (changeColumn)
                {
                    case 4:
                        user.Dotimes = (int)obj.Cells[changeColumn].Value;
                        break;
                }
                s.SaveOneUser(user);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

           
            this.dataGridView1.ClearSelection();

            //设置初始参数

            
        }

        private void InitWorkComponent(IWorkComponent component)
        {
            this.workComponent = component;//不同模式
            workComponent.InitEvents(this.Change_DGV, this.Worker_MessageEvent);
        }

        private void initDataGrid()
        {
            this.dataGridView1.Columns.Add("id", "序号");
            this.dataGridView1.Columns[0].Width = 60;
            this.dataGridView1.Columns.Add("username", "用户名称");
            this.dataGridView1.Columns.Add("operation", "操作模板");
            this.dataGridView1.Columns.Add("duration", "时间间隔");
            this.dataGridView1.Columns.Add("dotimes", "执行次数");
            this.dataGridView1.Columns.Add("timeleft", "执行计时");
            this.dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            this.dataGridView1.MultiSelect = false;
        }

        private void 退出ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Dispose();
            System.Environment.Exit(0);
        }

        private void AboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //MessageBox.Show(this,"Copy right(c) 2009-2018 TensorFlower All Rights Reserved \n Version:1.0 ",
            // "关于", MessageBoxButtons.OK, MessageBoxIcon.Information);
            new AboutBox1().ShowDialog(this);
        }

        private void stopAll_Click(object sender, EventArgs e)
        {
            
            if(this.start == false)
            {
                MessageBox.Show("未开始，无需停止");
            }
            else
            {
                workComponent.UnDeploy();
                this.toolStripStatusLabel1.Text = "就绪";
                MessageBox.Show("全部停止成功！");
                this.comboBox1.Enabled = true;

            }
            this.start = false;
        }

        private void startAll_Click(object sender, EventArgs e)
        {
            if (this.start)
            {
                MessageBox.Show("已经开始，不需要再次开始！");
            }
            else
            {
                foreach (var item in this.users)
                {
                    workComponent.Deploy(item.RowNum, this.dataGridView1.Rows[item.RowNum], DataBaseService.UserToObject(item));
                    
                }
                
                if (this.users.Count > 0)
                {
                    workComponent.Run();
                    this.start = true;
                    this.toolStripStatusLabel1.Text = "执行中";
                    this.comboBox1.Enabled = false;
                }
            }
           // MessageBox.Show("全部执行开始！");
        }

        private void deleteUser_Click(object sender, EventArgs e)
        {
            if (this.start)
            {
                MessageBox.Show("脚本运行中，不可删除！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if(this.dataGridView1.SelectedRows.Count>0)
            {
                DataGridViewRow row = this.dataGridView1.SelectedRows[0];
                DialogResult dialogResult = MessageBox.Show("您确定要删除 " + row.Cells[1].Value + " 吗？","删除提示",MessageBoxButtons.YesNo,MessageBoxIcon.Question);
                if (dialogResult == DialogResult.Yes)
                {
                    using(DataBaseService d = new DataBaseService())
                    {
                        if (d.DelOneUserById((int)row.Cells[0].Value))
                        {
                            InitDatabase();
                            MessageBox.Show("删除成功!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("请先选择要删除的行！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void toolStripStatusLabel1_Click(object sender, EventArgs e)
        {

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            this.toolStripStatusLabel2.Text = DateTime.Now.ToString();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (this.comboBox1.SelectedIndex)
            {
                case 0:
                    this.InitWorkComponent(new SimultaneousWork());
                    break;
                case 1:
                    this.InitWorkComponent(new WheeledWork());
                    break;
            }            
        }

        private void addUser_Click(object sender, EventArgs e)
        {

            if (start)
            {
                MessageBox.Show("运行中，无法添加用户！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return ;
            }

#if USERLIMIT
                  if (Form1.userCount >= BaseConfig.userLimit)
                  {
                        MessageBox.Show("用户数因为系统授权限制无法增加！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                  }
#endif

            UserAdd ua = new UserAdd();
            DialogResult result =  ua.ShowDialog(this);
            if(result == DialogResult.OK)
            {
                this.DataRefresh();
            }
            

        }


        public void DataRefresh()
        {
            this.InitDatabase();
        }
    }
}
