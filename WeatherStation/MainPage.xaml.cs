using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;
using BoschDevices;

namespace WeatherStation
{
    public sealed partial class MainPage : Page
    {
        // Configurables - ensure these are set appropriately!
        private const string _eventHubConnectionString = "Endpoint=sb://turbospudweather.servicebus.windows.net/;SharedAccessKeyName=SendAndListenPolicy;SharedAccessKey=e8NWK4eZDatOqDvHkKMyGKmoXqZqYpNhryN97CPB8Jg=;EntityPath=weatherstation";    // Set this to the connection string for your Event Hub in Azure (Shared Access Policy - Read & Listen)
        private const float _localSeaLevelPressure = 1022.00f;  // Used by the BME280 when taking measurements
        private const int _timerInterval = 2000;                // Determines how often a measurement will be taken

        private BME280Sensor _BME280;
        private DispatcherTimer _timer;
        private WeatherDataSender _weatherDataSender;

        public MainPage()
        {
            InitializeComponent();
        }

        // This method will be called by the application framework when the page is first loaded
        protected override async void OnNavigatedTo(NavigationEventArgs navArgs)
        {
            try
            {                
                _BME280 = new BME280Sensor(_localSeaLevelPressure); // Create a new object for our sensor class
                await _BME280.InitializeDevice();                   // Initialize the sensor

                if (_BME280.init)
                {
                    // If all goes well, our BME280 is initialized and we can set up
                    // a timer which will take a reading and send data to the cloud
                    _weatherDataSender = new WeatherDataSender(_eventHubConnectionString);
                    _timer = new DispatcherTimer
                    {
                        Interval = TimeSpan.FromMilliseconds(_timerInterval)
                    };
                    _timer.Tick += TakeReadingAsync;
                    _timer.Start();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private async void TakeReadingAsync(object sender, object e)
        {
            // Create variables to store the sensor data: temperature, pressure, humidity and altitude. 
            var temperatureReadings = new List<float>();
            var pressureReadings = new List<float>();
            var altitudeReadings = new List<float>();
            var humidityReadings = new List<float>();
            
            // Read 10 samples of the data
            for (int i = 0; i < 10; i++)
            {
                temperatureReadings.Add(await _BME280.ReadTemperature());
                pressureReadings.Add(await _BME280.ReadPressure());
                altitudeReadings.Add(await _BME280.ReadAltitude());
                humidityReadings.Add(await _BME280.ReadHumidity());
            }

            // Create a WeatherData object which will hold the 
            // average of the 10 samples for each attribute.
            WeatherData weatherReading = new WeatherData
            {
                RecordingTime = DateTime.Now.ToUniversalTime(),
                Temperature = temperatureReadings.Average(),
                Humidity = humidityReadings.Average(),
                Pressure = pressureReadings.Average(),
                Altitude = altitudeReadings.Average()
            };

            // Send to console for debugging purposes
            Debug.WriteLine(weatherReading.ToString());

            // Send the reading to the event hub
            await _weatherDataSender.SendDataAsync(weatherReading);
        }
    }
}
