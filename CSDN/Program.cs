using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSDN
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            CsdnComment csdn = new CsdnComment();

            Console.Write("请输入用户名:");
            string username = Console.ReadLine();
            Console.Write("请输入密码:");
            string password = Console.ReadLine();

            csdn.Login(username, password);
            if (csdn.LoginState)
            {
                csdn.GetMyDownloads();
                if (csdn.Downloads.Detail.Count>0)
                {
                    csdn.Comment();
                }
            }

            Console.ReadKey();
        }
    }
}