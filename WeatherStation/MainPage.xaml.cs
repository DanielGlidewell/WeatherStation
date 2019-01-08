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
        private BME280Sensor BME280;
        private DispatcherTimer timer;
        private const float LocalSeaLevelPressure = 1022.00f;
        private const int timerInterval = 2000;

        public MainPage()
        {
            InitializeComponent();
        }

        // This method will be called by the application framework when the page is first loaded
        protected override async void OnNavigatedTo(NavigationEventArgs navArgs)
        {
            try
            {                   
                BME280 = new BME280Sensor(LocalSeaLevelPressure);   // Create a new object for our sensor class
                await BME280.InitializeDevice();    // Initialize the sensor

                if (BME280.init)
                {
                    timer = new DispatcherTimer
                    {
                        Interval = TimeSpan.FromMilliseconds(timerInterval)
                    };
                    timer.Tick += TakeReading;
                    timer.Start();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private async void TakeReading(object sender, object e)
        {
            // Create variables to store the sensor data: temperature, pressure, humidity and altitude. 
            var temp = new List<float>();
            var pressure = new List<float>();
            var altitude = new List<float>();
            var humidity = new List<float>();
            
            // Read 10 samples of the data
            for (int i = 0; i < 10; i++)
            {
                temp.Add(await BME280.ReadTemperature());
                pressure.Add(await BME280.ReadPressure());
                altitude.Add(await BME280.ReadAltitude());
                humidity.Add(await BME280.ReadHumidity());
            }
            
            // Write the average of the sampled values to your console
            Debug.WriteLine($"Temperature: {temp.Average()} deg C");
            Debug.WriteLine($"Humidity: {humidity.Average()} %");
            Debug.WriteLine($"Pressure: {pressure.Average()} Pa");
            Debug.WriteLine($"Altitude: {altitude.Average()} m");
            Debug.WriteLine("");                    
        }
    }
}
