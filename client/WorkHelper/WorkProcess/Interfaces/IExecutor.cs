using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkProcess
{
    interface IExecutor
    {

        void Execute();
        Task<bool> Login();
        
    }
}
