using System.Threading;
using static System.Console;

namespace P2P
{
    class Program
    {
        static void Main(string[] args)
        {
            var user = new P2P.Core.User(AppSettings.Name, AppSettings.Port);

            while (true)
            {
                foreach (var userName in user.GetOtherUsers())
                {
                    WriteLine(userName);
                }

                Thread.Sleep(5);
            }
        }
    }
}
