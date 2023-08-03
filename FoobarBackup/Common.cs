using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace FoobarBackup
{
    internal class Common
    {
        public bool CheckService()
        {
            try
            {
                var services = ServiceController.GetServices();
                if (services.FirstOrDefault(s => s.ServiceName == "FoobarBackup") != null)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
        }
    }
}
