using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Devices.I2c;
using I2cDeviceHelpers;

namespace BoschDevices
{
    public class BME280Sensor
    {
        #region Attributes

        public bool init = false;                           // Variable to check if device is initialized
        private I2cDevice bme280 = null;                    // Create an I2C device        
        private const byte BME280_Address = 0x77;           // Slave address for the BME280 - used to find the device on the I2C bus
        private const byte BME280_Signature = 0x60;         // This value is used to verify that the device at the address is the BME280
        private Int32 t_fine = Int32.MinValue;              // t_fine carries fine temperature as global value
        private readonly float seaLevelPressure;            // Pressure at sea level. Based on your local sea level pressure (Unit: Hectopascal)

        #region BME280 Device Registers

        // The BME280 register addresses according to the datasheet: https://cdn-shop.adafruit.com/datasheets/BST-BME280_DS001-10.pdf
        // These register addresses are crucial for being able to read and write data to and from the device.
        private enum Registers : byte
        {
            BME280_REGISTER_CALIBRATION_DATA_T1 = 0x88,
            BME280_REGISTER_CALIBRATION_DATA_T2 = 0x8A,
            BME280_REGISTER_CALIBRATION_DATA_T3 = 0x8C,

            BME280_REGISTER_CALIBRATION_DATA_P1 = 0x8E,
            BME280_REGISTER_CALIBRATION_DATA_P2 = 0x90,
            BME280_REGISTER_CALIBRATION_DATA_P3 = 0x92,
            BME280_REGISTER_CALIBRATION_DATA_P4 = 0x94,
            BME280_REGISTER_CALIBRATION_DATA_P5 = 0x96,
            BME280_REGISTER_CALIBRATION_DATA_P6 = 0x98,
            BME280_REGISTER_CALIBRATION_DATA_P7 = 0x9A,
            BME280_REGISTER_CALIBRATION_DATA_P8 = 0x9C,
            BME280_REGISTER_CALIBRATION_DATA_P9 = 0x9E,

            BME280_REGISTER_CALIBRATION_DATA_H1 = 0xA1,
            BME280_REGISTER_CALIBRATION_DATA_H2 = 0xE1,
            BME280_REGISTER_CALIBRATION_DATA_H3 = 0xE3,
            BME280_REGISTER_CALIBRATION_DATA_H4 = 0xE4,
            BME280_REGISTER_CALIBRATION_DATA_H5 = 0xE5,
            BME280_REGISTER_CALIBRATION_DATA_H6 = 0xE7,

            BME280_REGISTER_CHIPID = 0xD0,
            BME280_REGISTER_VERSION = 0xD1,
            BME280_REGISTER_SOFTRESET = 0xE0,

            BME280_REGISTER_CAL26 = 0xE1,  // R calibration stored in 0xE1-0xF0

            BME280_REGISTER_CONTROLHUMID = 0xF2,
            BME280_REGISTER_CONTROL = 0xF4,
            BME280_REGISTER_CONFIG = 0xF5,

            BME280_REGISTER_PRESSUREDATA_MSB = 0xF7,
            BME280_REGISTER_PRESSUREDATA_LSB = 0xF8,
            BME280_REGISTER_PRESSUREDATA_XLSB = 0xF9, // bits <7:4>

            BME280_REGISTER_TEMPURATUREDATA_MSB = 0xFA,
            BME280_REGISTER_TEMPURATUREDATA_LSB = 0xFB,
            BME280_REGISTER_TEMPURATUREDATA_XLSB = 0xFC, // bits <7:4>

            BME280_REGISTER_HUMIDITYDATA_MSB = 0xFD,
            BME280_REGISTER_HUMIDITYDATA_LSB = 0xFE,
        };

        #endregion

        #region BME280 Calibration Data

        // The calibration coefficients set at the factory for the device. 
        // Populated after device initialization in the InitializeDevice() method.
        private UInt16 Calibration_Data_T1;
        private Int16 Calibration_Data_T2;
        private Int16 Calibration_Data_T3;

        private UInt16 Calibration_Data_P1;
        private Int16 Calibration_Data_P2;
        private Int16 Calibration_Data_P3;
        private Int16 Calibration_Data_P4;
        private Int16 Calibration_Data_P5;
        private Int16 Calibration_Data_P6;
        private Int16 Calibration_Data_P7;
        private Int16 Calibration_Data_P8;
        private Int16 Calibration_Data_P9;

        private byte Calibration_Data_H1;
        private Int16 Calibration_Data_H2;
        private byte Calibration_Data_H3;
        private Int16 Calibration_Data_H4;
        private Int16 Calibration_Data_H5;
        private SByte Calibration_Data_H6;

        #endregion

        #endregion

        #region Methods        
        public BME280Sensor(float localSeaLevelPressureHectopascal)
        {
            seaLevelPressure = localSeaLevelPressureHectopascal;
        }

        // Method to initialize the BME280 sensor using the I2C interface
        public async Task InitializeDevice()
        {
            Debug.WriteLine("BME280::Initializing device with I2C interface");

            try
            {
                bme280 = await I2cDeviceFinder.FindI2cDeviceForRaspberryPi(BME280_Address);

                if (bme280 == null) // Check if a device was found at the slave address provided by BME280_Address
                {
                    Debug.WriteLine("Device not found");
                    return;
                }
                else // Since we found a device, let's verify that it is a BME280 by looking at the signature
                {
                    Debug.WriteLine("BME280::Verifying device signature");
                    var WriteBuffer = new byte[] { (byte)Registers.BME280_REGISTER_CHIPID }; // This register stores the signature value
                    var ReadBuffer = new byte[] { 0xFF }; // Initialized to an arbitrary number

                    bme280.WriteRead(WriteBuffer, ReadBuffer);  // Read the device signature and store in ReadBuffer[0]

                    if (ReadBuffer[0] != BME280_Signature) // Verify the device signature
                    {
                        Debug.WriteLine("BME280::Signature mismatch");
                        return;
                    }

                    Debug.WriteLine("BME280::Getting device ready");

                    // As per the data sheet's instructions, we need some stuff before we can 
                    // start taking measurements using the BME280's registers.
                    await ReadCalibrationCoefficients();  // Read the compensation coefficients from the factory
                    await WriteControlRegister();         // Write to the control register 
                    await WriteControlRegisterHumidity(); // Write to the humidity control register 

                    Debug.WriteLine("BME280::Device is ready");
                }

                init = true;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception: " + e.Message + "\n" + e.StackTrace);
                throw;
            }
        }

        public async Task<float> ReadTemperature()
        {
            // Make sure the I2C device is ready for sampling
            if (!init)
            {
                await InitializeDevice();
            }

            // Read the MSB, LSB and XLSB (bits 7:4) data from the BME280 temperature registers
            byte tmsb = ReadByte((byte)Registers.BME280_REGISTER_TEMPURATUREDATA_MSB);
            byte tlsb = ReadByte((byte)Registers.BME280_REGISTER_TEMPURATUREDATA_LSB);
            byte txlsb = ReadByte((byte)Registers.BME280_REGISTER_TEMPURATUREDATA_XLSB); // bits 7:4


            Int32 uncompensatedTemp = (tmsb << 12) + (tlsb << 4) + (txlsb >> 4);    // Combine the values into a 32-bit integer            
            double temp = BME280_Compensate_Temperature_Double(uncompensatedTemp);  // Convert the raw value to the temperature in degC
            return (float)temp;                                                     // Return the temperature as a float value
        }

        public async Task<float> ReadPressure()
        {
            // Make sure the I2C device is ready for sampling
            if (!init)
            {
                await InitializeDevice();
            }

            // Read the temperature first to load the t_fine value for compensation
            if (t_fine == Int32.MinValue)
            {
                await ReadTemperature();
            }

            // Read the MSB, LSB and XLSB (bits 7:4) data from the BME280 pressure registers
            byte pmsb = ReadByte((byte)Registers.BME280_REGISTER_PRESSUREDATA_MSB);
            byte plsb = ReadByte((byte)Registers.BME280_REGISTER_PRESSUREDATA_LSB);
            byte pxlsb = ReadByte((byte)Registers.BME280_REGISTER_PRESSUREDATA_XLSB); // bits 7:4

            Int32 uncompensatedPressure = (pmsb << 12) + (plsb << 4) + (pxlsb >> 4);    // Combine the values into a 32-bit integer
            Int64 pressure = BME280_Compensate_Pressure_Int64(uncompensatedPressure);   // Convert the raw value to the pressure in Pa
            return ((float)pressure) / 256;                                             // Return the temperature as a float value
        }

        public async Task<float> ReadHumidity()
        {
            if (!init)
            {
                await InitializeDevice();
            }

            // Read the MSB and LSB data from the BME280 humidity registers
            byte hmsb = ReadByte((byte)Registers.BME280_REGISTER_HUMIDITYDATA_MSB);
            byte hlsb = ReadByte((byte)Registers.BME280_REGISTER_HUMIDITYDATA_LSB);

            Int32 uncompensatedHumidity = (hmsb << 8) + hlsb;                           // Combine the values into a 32-bit integer
            UInt32 humidity = BME280_Compensate_Humidity_Int32(uncompensatedHumidity);  // Convert the raw value to the humidity 
            return ((float)humidity) / 1000;                                            // Return the humidity as a float value
        }

        // Method to take the sea level pressure in Hectopascals(hPa) as a parameter and calculate the altitude using current pressure.
        public async Task<float> ReadAltitude()
        {
            // Make sure the I2C device is ready for sampling
            if (!init)
            {
                await InitializeDevice();
            }

            float pressure = await ReadPressure();  // Read the pressure first
            pressure /= 100;                        // Convert the pressure to Hectopascals(hPa) 

            // Calculate and return the altitude using the international barometric formula
            return 44330.0f * (1.0f - (float)Math.Pow((pressure / seaLevelPressure), 0.1903f));
        }

        // Method to write the value 0x03 to the humidity control register
        private async Task WriteControlRegisterHumidity()
        {
            // 0x03 == 0000 0011
            var WriteBuffer = new byte[] { (byte)Registers.BME280_REGISTER_CONTROLHUMID, 0x03 };
            bme280.Write(WriteBuffer);
            await Task.Delay(1);
            return;
        }

        // Method to write the value 0x3F to the control register
        private async Task WriteControlRegister()
        {
            // 0x3F == 0011 1111
            var WriteBuffer = new byte[] { (byte)Registers.BME280_REGISTER_CONTROL, 0x3F };
            bme280.Write(WriteBuffer);
            await Task.Delay(1);
            return;
        }

        // Method to read the calibration data from the registers
        private async Task ReadCalibrationCoefficients()
        {
            // Read temperature calibration data
            Calibration_Data_T1 = ReadUInt16_LittleEndian((byte)Registers.BME280_REGISTER_CALIBRATION_DATA_T1);
            Calibration_Data_T2 = (Int16)ReadUInt16_LittleEndian((byte)Registers.BME280_REGISTER_CALIBRATION_DATA_T2);
            Calibration_Data_T3 = (Int16)ReadUInt16_LittleEndian((byte)Registers.BME280_REGISTER_CALIBRATION_DATA_T3);

            // Read pressure calibration data
            Calibration_Data_P1 = ReadUInt16_LittleEndian((byte)Registers.BME280_REGISTER_CALIBRATION_DATA_P1);
            Calibration_Data_P2 = (Int16)ReadUInt16_LittleEndian((byte)Registers.BME280_REGISTER_CALIBRATION_DATA_P2);
            Calibration_Data_P3 = (Int16)ReadUInt16_LittleEndian((byte)Registers.BME280_REGISTER_CALIBRATION_DATA_P3);
            Calibration_Data_P4 = (Int16)ReadUInt16_LittleEndian((byte)Registers.BME280_REGISTER_CALIBRATION_DATA_P4);
            Calibration_Data_P5 = (Int16)ReadUInt16_LittleEndian((byte)Registers.BME280_REGISTER_CALIBRATION_DATA_P5);
            Calibration_Data_P6 = (Int16)ReadUInt16_LittleEndian((byte)Registers.BME280_REGISTER_CALIBRATION_DATA_P6);
            Calibration_Data_P7 = (Int16)ReadUInt16_LittleEndian((byte)Registers.BME280_REGISTER_CALIBRATION_DATA_P7);
            Calibration_Data_P8 = (Int16)ReadUInt16_LittleEndian((byte)Registers.BME280_REGISTER_CALIBRATION_DATA_P8);
            Calibration_Data_P9 = (Int16)ReadUInt16_LittleEndian((byte)Registers.BME280_REGISTER_CALIBRATION_DATA_P9);

            // Read humidity calibration data
            Calibration_Data_H1 = ReadByte((byte)Registers.BME280_REGISTER_CALIBRATION_DATA_H1);
            Calibration_Data_H2 = (Int16)ReadUInt16_LittleEndian((byte)Registers.BME280_REGISTER_CALIBRATION_DATA_H2);
            Calibration_Data_H3 = ReadByte((byte)Registers.BME280_REGISTER_CALIBRATION_DATA_H3);
            Calibration_Data_H4 = (Int16)((ReadByte((byte)Registers.BME280_REGISTER_CALIBRATION_DATA_H4) << 4) | (ReadByte((byte)Registers.BME280_REGISTER_CALIBRATION_DATA_H4 + 1) & 0xF));
            Calibration_Data_H5 = (Int16)((ReadByte((byte)Registers.BME280_REGISTER_CALIBRATION_DATA_H5 + 1) << 4) | (ReadByte((byte)Registers.BME280_REGISTER_CALIBRATION_DATA_H5) >> 4));
            Calibration_Data_H6 = (sbyte)ReadByte((byte)Registers.BME280_REGISTER_CALIBRATION_DATA_H6);

            await Task.Delay(1);
            return;
        }

        // Method to read a 16-bit value from a register and return it in little endian format
        private UInt16 ReadUInt16_LittleEndian(byte register)
        {
            byte[] writeBuffer = new byte[] { register };
            byte[] readBuffer = new byte[] { 0x00, 0x00 };

            bme280.WriteRead(writeBuffer, readBuffer);

            int h = readBuffer[1] << 8;
            int l = readBuffer[0];
            UInt16 value = (UInt16)(h + l);

            return value;
        }

        // Method to read an 8-bit value from a register
        private byte ReadByte(byte register)
        {
            byte[] writeBuffer = new byte[] { register };
            byte[] readBuffer = new byte[] { 0x00 };

            bme280.WriteRead(writeBuffer, readBuffer);

            byte value = readBuffer[0];

            return value;
        }

        #region Methods provided by Bosch in the BME280 datasheet

        // Method to return the temperature in DegC. Resolution is 0.01 DegC. Output value of “5123” equals 51.23 DegC.
        private double BME280_Compensate_Temperature_Double(Int32 adc_T)
        {
            double var1, var2, T;

            // The temperature is calculated using the compensation formula in the BME280 datasheet
            var1 = ((adc_T / 16384.0) - (Calibration_Data_T1 / 1024.0)) * Calibration_Data_T2;
            var2 = ((adc_T / 131072.0) - (Calibration_Data_T1 / 8192.0)) * Calibration_Data_T3;

            t_fine = (Int32)(var1 + var2);

            T = (var1 + var2) / 5120.0;
            return T;
        }

        // Method to returns the pressure in Pa, in Q24.8 format (24 integer bits and 8 fractional bits).
        // Output value of “24674867” represents 24674867/256 = 96386.2 Pa = 963.862 hPa
        private Int64 BME280_Compensate_Pressure_Int64(Int32 adc_P)
        {
            Int64 var1, var2, p;

            // The pressure is calculated using the compensation formula in the BME280 datasheet
            var1 = t_fine - 128000;
            var2 = var1 * var1 * (Int64)Calibration_Data_P6;
            var2 = var2 + ((var1 * (Int64)Calibration_Data_P5) << 17);
            var2 = var2 + ((Int64)Calibration_Data_P4 << 35);
            var1 = ((var1 * var1 * (Int64)Calibration_Data_P3) >> 8) + ((var1 * (Int64)Calibration_Data_P2) << 12);
            var1 = (((((Int64)1 << 47) + var1)) * (Int64)Calibration_Data_P1) >> 33;
            if (var1 == 0)
            {
                Debug.WriteLine("BME280_compensate_P_Int64 Jump out to avoid / 0");
                return 0; // Avoid exception caused by division by zero
            }
            // Perform calibration operations as per datasheet: https:// cdn-shop.adafruit.com/datasheets/BST-BME280_DS001-10.pdf
            p = 1048576 - adc_P;
            p = (((p << 31) - var2) * 3125) / var1;
            var1 = ((Int64)Calibration_Data_P9 * (p >> 13) * (p >> 13)) >> 25;
            var2 = ((Int64)Calibration_Data_P8 * p) >> 19;
            p = ((p + var1 + var2) >> 8) + ((Int64)Calibration_Data_P7 << 4);
            return p;
        }

        // Returns humidity in %RH as unsigned 32 bit integer in Q22.10 format (22 integer and 10 fractional bits).
        // Output value of “47445” represents 47445/1024 = 46.333 %RH
        private UInt32 BME280_Compensate_Humidity_Int32(Int32 adc_H)
        {
            Int32 v_x1_u32r;
            v_x1_u32r = (t_fine - ((Int32)76800));
            v_x1_u32r = (((((adc_H << 14) - (((Int32)Calibration_Data_H4) << 20) - (((Int32)Calibration_Data_H5) * v_x1_u32r)) +
            ((Int32)16384)) >> 15) * (((((((v_x1_u32r * ((Int32)Calibration_Data_H6)) >> 10) * (((v_x1_u32r *
                ((Int32)Calibration_Data_H3)) >> 11) + ((Int32)32768))) >> 10) + ((Int32)2097152)) *
            ((Int32)Calibration_Data_H2) + 8192) >> 14));
            v_x1_u32r = (v_x1_u32r - (((((v_x1_u32r >> 15) * (v_x1_u32r >> 15)) >> 7) * ((Int32)Calibration_Data_H1)) >> 4));
            v_x1_u32r = (v_x1_u32r < 0 ? 0 : v_x1_u32r);
            v_x1_u32r = (v_x1_u32r > 419430400 ? 419430400 : v_x1_u32r);
            return (UInt32)(v_x1_u32r >> 12);
        }

        #endregion

        #endregion
    }
}
