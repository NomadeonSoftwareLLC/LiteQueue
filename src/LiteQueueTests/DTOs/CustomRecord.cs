/* Copyright 2024 by Nomadeon LLC. Licensed uinder MIT: https://opensource.org/licenses/MIT */
using System;
using System.Collections.Generic;
using System.Text;

namespace LiteQueueTests
{
    /// <summary>
    /// Contrived complex object for testing the T in LiteQueue<T>
    /// </summary>
    public class CustomRecord
    {
        public DeviceLocation Device { get; set; }

        public double SensorReading { get; set; }
        public string LogValue { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
