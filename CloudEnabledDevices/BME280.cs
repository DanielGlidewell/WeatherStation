using BoschDevices;

namespace CloudEnabledDevices
{
    public class BME280 : BME280Sensor
    {
        public BME280(float localSeaLevelPressureHectopascal) : base(localSeaLevelPressureHectopascal){}


    }
}
