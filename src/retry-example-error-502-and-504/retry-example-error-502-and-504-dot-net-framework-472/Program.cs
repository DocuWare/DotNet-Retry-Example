using DocuWare.Platform.ServerClient;
using retry_example_error_502_and_504_retry_policy_dot_net_standard_2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace retry_example_error_502_and_504_dot_net_framework_472
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Variables
            string serverUrl = "http://localhost";
            string userName = "admin";
            string userPassword = "admin";

            RetryPolicy retryPolicy = new RetryPolicy();
            ServiceConnection connection = ServiceConnection.Create(new Uri(serverUrl), 
                                                                    userName, 
                                                                    userPassword, 
                                                                    httpClientHandler: retryPolicy.CreateHttpMessageRetryHandlerWithPollyAndExponentialBackoff(5, TimeSpan.FromSeconds(1)));

            var org = connection.Organizations.FirstOrDefault();

            if (org != null)
            {
                Console.WriteLine($"Organization: {org.Name}");
            }
            else
            {
                Console.WriteLine("Organization not found");
            }
        }
    }
}
