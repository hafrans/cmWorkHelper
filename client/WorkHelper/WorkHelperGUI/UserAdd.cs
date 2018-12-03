using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WorkHelperGUI.Services;
using WorkProcess;



namespace WorkHelperGUI
{

    public partial class UserAdd : Form
    {

        public UserAdd()
        {
            this.StartPosition = FormStartPosition.CenterParent;
            InitializeComponent();
        }

        private void UserAdd_Load(object sender, EventArgs e)
        {

        }

        private void textBox3_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar != 8 && !Char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }




        private void button1_Click(object sender, EventArgs e)
        {

            #if USERLIMIT
                  if (Form1.userCount >= BaseConfig.userLimit)
                  {
                        MessageBox.Show("用户数因为系统授权限制无法增加！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                  }
            #endif



            //验证所有信息是否填写全了

            if (this.username.Text.Length == 0)
            {
                MessageBox.Show("请输入用户名","提示",MessageBoxButtons.OK,MessageBoxIcon.Warning);
                return;
            }

            if(this.password.Text.Length == 0)
            {
                MessageBox.Show("请输入密码", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if(this.duration.Text.Length == 0)
            {
                MessageBox.Show("请输入运行间隔", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int duration = int.Parse(this.duration.Text);

            if(duration < 10 || duration > 1800)
            {
                MessageBox.Show("请输入正确的运行间隔（10~1800）", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ///执行
            ///
            this.status.Text = "正在验证账户可用性";

            this.progressBar1.Style = ProgressBarStyle.Marquee;
            Processor process = new Processor(this.username.Text, this.password.Text);
            ThreadPool.QueueUserWorkItem(delegate (object state)
            {
                try
                {
                    Dictionary<string, string> pairs = process.Login();
                    if(pairs != null)
                    {
                        DialogResult result = MessageBox.Show("验证成功！确认添加用户"+this.username.Text+"吗", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if(result == DialogResult.Yes)
                        {
                            using (DataBaseService s = new DataBaseService())
                            {
                                s.AddOneUser(this.username.Text, this.password.Text, "受理", duration);
                            }
                            this.Invoke(new MethodInvoker(delegate() {
                                this.DialogResult = DialogResult.OK;
                                this.Dispose();
                            }));
                        }
                        else
                        {
                            this.Invoke(new MethodInvoker(delegate () {
                                this.progressBar1.Style = ProgressBarStyle.Blocks;
                                this.status.Text = "";
                            }));
                        }
                    }
                }
                catch (AggregateException ee)
                {
                    ee.Handle((err) =>
                    {
                        if (err is Exception)
                        {
                            MessageBox.Show(err.Message, "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            this.Invoke(new MethodInvoker(delegate ()
                            {
                                this.progressBar1.Style = ProgressBarStyle.Blocks;
                                this.status.Text = "";
                            }));
                        }

                        return true;
                    });
                }
            });



        }

        
    }
}
