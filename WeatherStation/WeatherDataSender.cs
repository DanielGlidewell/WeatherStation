using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Newtonsoft.Json;

namespace WeatherStation
{
    // A simple class for taking our measurements and sending them to the Event Hub
    public class WeatherDataSender
    {        
        private EventHubClient _eventHubClient; // Used to connect to our Event Hub in Azure

        public WeatherDataSender(string _eventHubConnectionString)
        {
            _eventHubClient = EventHubClient.CreateFromConnectionString(_eventHubConnectionString);
        }

        // Method to send weather data to the Event Hub in Azure
        public async Task SendDataAsync(WeatherData data)
        {
            var dataAsJson = JsonConvert.SerializeObject(data);
            var encodedData = new EventData(Encoding.UTF8.GetBytes(dataAsJson));
            await _eventHubClient.SendAsync(encodedData);
        }
    }
}
