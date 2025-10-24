namespace YasGMP.Models.Enums
{
    /// <summary>
    /// <b>SensorType</b> – All sensor types supported in IoT, calibration, and environmental monitoring.
    /// <para>
    /// Use for mapping sensor data logs, calibration, asset tracking, and anomaly detection.
    /// </para>
    /// </summary>
    public enum SensorType
    {
        /// <summary>Temperature (°C, °F, K).</summary>
        Temperature = 0,
        /// <summary>Pressure (bar, psi, Pa).</summary>
        Pressure = 1,
        /// <summary>Humidity (%RH).</summary>
        Humidity = 2,
        /// <summary>Flow (l/min, m3/h, etc).</summary>
        Flow = 3,
        /// <summary>Vibration (mm/s, g, etc).</summary>
        Vibration = 4,
        /// <summary>Voltage (V, mV).</summary>
        Voltage = 5,
        /// <summary>Current (A, mA).</summary>
        Current = 6,
        /// <summary>pH (acidity).</summary>
        PH = 7,
        /// <summary>Conductivity (µS/cm, mS/cm).</summary>
        Conductivity = 8,
        /// <summary>CO2 or gas concentration (ppm, %).</summary>
        CO2 = 9,
        /// <summary>Weight/Load (kg, N, lb).</summary>
        Weight = 10,
        /// <summary>Rotation (rpm).</summary>
        RPM = 11,
        /// <summary>Custom/other sensor types (future proof).</summary>
        Other = 1000
    }
}

