using ComputerUtils.Logging;
using System.Text.Json;

namespace Cards_against_humanity
{
    public class Config
    {
        public string publicAddress { get; set; } = "";
        public string crashPingId { get; set; } = "631189193825058826";
        public int port { get; set; } = 506;
        public string masterToken { get; set; } = "";
        public string mongoDBName { get; set; } = "CAH";
        public string mongoDBUrl { get; set; } = "";
        public string masterWebhookUrl { get; set; } = "";

        public static Config LoadConfig()
        {
            string configLocation = CAHEnvironment.workingDir + "data" + Path.DirectorySeparatorChar + "config.json";
            if (!File.Exists(configLocation)) File.WriteAllText(configLocation, JsonSerializer.Serialize(new Config()));
            return JsonSerializer.Deserialize<Config>(File.ReadAllText(configLocation));
        }

        public void Save()
        {
            try
            {
                File.WriteAllText(CAHEnvironment.workingDir + "data" + Path.DirectorySeparatorChar + "config.json", JsonSerializer.Serialize(this));
            }
            catch (Exception e)
            {
                Logger.Log("couldn't save config: " + e.ToString(), LoggingType.Warning);
            }
        }
    }
}