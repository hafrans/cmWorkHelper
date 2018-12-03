using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.IO;
using System.Diagnostics;
using WorkHelperGUI.Models;




namespace WorkHelperGUI.Services
{
    class DataBaseService:IDisposable
    {
        public const string DEFAULT_BASE = "./wh.db";
        public const string DEFAULT_DS = "data source=./wh.db";
        public static void InitDefaultBase()
        {
            if (!File.Exists(DEFAULT_BASE))
            {
                //create databases;
                SQLiteConnection.CreateFile(DEFAULT_BASE);

                //创建表

                using (SQLiteConnection conn = new SQLiteConnection(DataBaseService.DEFAULT_DS))
                {
                    conn.Open();
                    if (conn.IsReadOnly("main"))
                    {
                        throw new Exception("Sqlite 只读！");
                    }

                    string initSql = @"
                      create table users(
                            id integer primary key autoincrement,
                            username varchar(255) not null,
                            password varchar(255) not null,
                            operation TEXT not null default '受理',
                            dotimes integer default 0,
                            duration integer default 60
                      )
                    ";
                    SQLiteCommand command = new SQLiteCommand(initSql, conn);
                    try
                    {
                        int s = command.ExecuteNonQuery();
                        Debug.WriteLine(s);
                           Debug.WriteLine("数据库创建成功！");
                    }
                    catch
                    {
                        conn.Close();
                        File.Delete(DEFAULT_BASE);
                        throw;
                    }
                    finally
                    {
                        command.Dispose();
                    }


                }


            }
        }


        SQLiteConnection conn;

        public void Dispose()
        {
           if(conn!= null && conn.State == System.Data.ConnectionState.Open)
            {
                conn.Dispose();
            }
        }

        public DataBaseService()
        {
            this.conn = new SQLiteConnection(DEFAULT_DS);
        }

        void Open()
        {
            if(this.conn.State != System.Data.ConnectionState.Open)
            {
                this.conn.Open();
            }
        }

        public List<User> findAllUsers()
        {
            List<User> users = new List<User>();
            this.Open();
            string sql = @"select * from users";
            using (SQLiteCommand command = new SQLiteCommand(sql,this.conn))
            {
                SQLiteDataReader reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        /**
                         * 
                         *  id integer primary key autoincrement,
                            username varchar(255) not null,
                            password varchar(255) not null,
                            operation TEXT not null default '受理',
                            dotimes integer default 0,
                            duration integer default 60
                         */
                        Debug.Write("OK----");

                        User tmpUser = new User();
                        tmpUser.Id = reader.GetInt32(0);
                        tmpUser.Username = reader.GetString(1);
                        tmpUser.Password = reader.GetString(2);
                        tmpUser.Operation = reader.GetString(3);
                        tmpUser.Dotimes = reader.GetInt32(4);
                        tmpUser.Duration = reader.GetInt32(5);

                        users.Add(tmpUser);
                    }
                }
                reader.Close();
            }
            return users;

        }

        public bool SaveAllUsers(List<User> users)
        {
            this.Open();
            int count = 0;
            if(users.Count > 0)
            {
                string sql = @"update users set username = @u , password = @p , operation=@o, dotimes=@do, duration=@du where id = @id";
                foreach (User u in users)
                {
                    
                    using (SQLiteCommand command = new SQLiteCommand(sql,this.conn))
                    {
                        command.Prepare();
                        command.Parameters.AddWithValue("u",u.Username);
                        command.Parameters.AddWithValue("p", u.Password);
                        command.Parameters.AddWithValue("do", u.Dotimes);
                        command.Parameters.AddWithValue("o", u.Operation);
                        command.Parameters.AddWithValue("du", u.Duration);
                        command.Parameters.AddWithValue("id", u.Id);
                        command.ExecuteNonQuery();
                        ++count;
                    }
                }

            }

            return count == users.Count;
        }

        public bool SaveOneUser(User u)
        {

            this.Open();
            string sql = @"update users set username = @u , password = @p , operation=@o, dotimes=@do, duration=@du where id = @id";
            using (SQLiteCommand command = new SQLiteCommand(sql, this.conn))
            {
                command.Prepare();
                command.Parameters.AddWithValue("u", u.Username);
                command.Parameters.AddWithValue("p", u.Password);
                command.Parameters.AddWithValue("do", u.Dotimes);
                command.Parameters.AddWithValue("o", u.Operation);
                command.Parameters.AddWithValue("du", u.Duration);
                command.Parameters.AddWithValue("id", u.Id);
                return 1 == command.ExecuteNonQuery();
            }
        }

        public bool AddOneUser(string user,string pwd,string operation,int duration)
        {
            this.Open();
            string sql = @"insert into users (username,password,operation,duration) values (@u,@p,@o,@d)";
            using (SQLiteCommand command = new SQLiteCommand(sql, this.conn))
            {
                command.Prepare();
                command.Parameters.AddWithValue("u", user);
                command.Parameters.AddWithValue("p", pwd);
                command.Parameters.AddWithValue("o", operation);
                command.Parameters.AddWithValue("d", duration);
                return 1 == command.ExecuteNonQuery();
            }
        }

        public bool DelOneUserById(int id)
        {
            this.Open();
            string sql = "delete from users where id = " + id;
            using (SQLiteCommand command = new SQLiteCommand(sql, this.conn))
            {
                return command.ExecuteNonQuery() == 1;
            }
        }

        public static object[] UserToObject(User user)
        {
            return new object[]
            {
                user.Id,
                user.Username,
                user.Password,
                user.Operation,
                user.Dotimes,
                user.Duration,
                user.RowNum
            };
        }


    }
}
