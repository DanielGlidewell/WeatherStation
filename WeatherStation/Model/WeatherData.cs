using System;

namespace WeatherStation
{
    // This class encapsulates all of the elements which make up a weather data reading.
    public class WeatherData
    {
        public DateTime RecordingTime { get; set; }
        public float Temperature { get; set; }
        public float Altitude { get; set; }
        public float Pressure { get; set; }
        public float Humidity { get; set; }

        public override string ToString()
        {
            return $"RecordingTime: {RecordingTime:yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'} | "
                 + $"Temperature: {Temperature} | "
                 + $"Altitude: {Altitude} | "
                 + $"Pressure: {Pressure} | "
                 + $"Humidity: {Humidity}";
        }   
    }
}