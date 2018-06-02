using System.Configuration;

namespace P2P
{
    static internal class AppSettings
    {
        public static string Name { get; }
        public static int Port { get; }

        static AppSettings()
        {
            Name = ConfigurationManager.AppSettings["Name"];
            Port = int.Parse(ConfigurationManager.AppSettings["Port"]);
        }
    }
}
