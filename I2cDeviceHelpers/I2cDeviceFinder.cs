using System;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;

namespace I2cDeviceHelpers
{
    public class I2cDeviceFinder
    {
        private const string _raspberryPiI2cBusFriendlyName = "I2C1";

        public static async Task<I2cDevice> FindI2cDeviceForRaspberryPi(byte slaveAddress)
        {
            // Create an advanced query syntax string (AQS) which we will use 
            // to get a DeviceInformation object for our Raspberry Pi.
            string advancedQuerySyntaxString = I2cDevice.GetDeviceSelector(_raspberryPiI2cBusFriendlyName);
            
            // Create a collection of devices whose criteria match the predicates in the 
            // AQS. I believe this always returns one object - our Raspberry Pi.
            DeviceInformationCollection devicesFound = await DeviceInformation.FindAllAsync(advancedQuerySyntaxString);


            // Instantiate the I2CConnectionSettings using the slaveAddress provided
            I2cConnectionSettings connectionSettings = new I2cConnectionSettings(slaveAddress)
            {
                BusSpeed = I2cBusSpeed.FastMode // Set the I2C bus speed of connection to fast mode
            };
            
            // Use the device we found earlier and the connectionSettings together 
            // to return an I2cDevice at the slaveAddress provided
            return await I2cDevice.FromIdAsync(devicesFound[0].Id, connectionSettings);
        }
    }
}
