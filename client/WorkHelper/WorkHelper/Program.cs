using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorkProcess;
using System.Threading;

namespace WorkHelper
{
    class Program
    {
        static void Main(string[] args)
        {
            Processor process = new Processor("dw3_loubenjian", "dw3_loubenjian");


            //use WTA
            Dictionary<string, string> pairs = process.Login();
            ////////////////////////////////
            ///

            WorkTaskAcceptExecutor workTaskAccept = new WorkTaskAcceptExecutor(pairs);
            Task<bool> b = workTaskAccept.Login();
            
            if (b.Result)
            {
                
                var results = workTaskAccept.FetchTaskList().Result;
               //Console.WriteLine(workTaskAccept.getAcceptFormContent(results[0]).Result.ReadAsStringAsync().Result);
               //Console.WriteLine(workTaskAccept.AcceptOneTask(results[0]).Result);
                foreach (var rse in results)
                {
                    Console.WriteLine(rse);
                }
                var findResult = results.FindAll((e)=>{
                    return e.Status == "待受理";
                });
                if(findResult.Count > 0)
                {
                    Console.WriteLine("处理？");
                    Console.ReadLine();
                    Console.WriteLine(workTaskAccept.AcceptOneTask(findResult[0]).Result);


                }
            }



            Console.ReadKey();
        }
    }

  
}
