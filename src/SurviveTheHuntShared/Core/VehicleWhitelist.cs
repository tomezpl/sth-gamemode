using Newtonsoft.Json;
using System.Text;

namespace SurviveTheHuntShared.Core
{
    public class VehicleWhitelist
    {
        [JsonProperty("vehicles")]
        public string[] Vehicles { get; set; }

        public VehicleWhitelist()
        {
            Vehicles = new string[0];
        }

        public string Serialize()
        {
            StringBuilder sb = new StringBuilder("");
            bool hasEntries = false;

            foreach(string vehicle in Vehicles)
            {
                hasEntries = true;
                sb.Append($"{vehicle};");
            }

            if(hasEntries)
            {
                sb.Length--;
            }

            return sb.ToString();
        }
    }
}
