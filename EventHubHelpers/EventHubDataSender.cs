using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Newtonsoft.Json;

namespace EventHubHelpers
{
    // A simple class for taking data and sending them to an Event Hub
    public class EventHubDataSender
    {
        private EventHubClient _eventHubClient; // Used to connect to our Event Hub in Azure

        public EventHubDataSender(string _eventHubConnectionString)
        {
            _eventHubClient = EventHubClient.CreateFromConnectionString(_eventHubConnectionString);
        }

        // Method to send data as JSON
        public async Task SendDataJsonUtf8Async(object data)
        {
            var dataAsJson = JsonConvert.SerializeObject(data);
            var encodedData = new EventData(Encoding.UTF8.GetBytes(dataAsJson));
            await _eventHubClient.SendAsync(encodedData);
        }        
    }
}

