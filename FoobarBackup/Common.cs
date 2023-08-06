using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace FoobarBackup
{
    public class Common
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
        public static IConfiguration GetConfig()
        {
            try
            {
                IConfigurationRoot config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
                return config;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }
    }
}
