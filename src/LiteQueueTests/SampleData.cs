using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteQueueTests
{
    public static class SampleData
    {
        public static List<CustomRecord> GetCustomRecords()
        {
            var record1 = new CustomRecord() {
                Device = new DeviceLocation() {
                    LatitudeDegrees = 120,
                    LongitudeDegrees = 30
                },
                LogValue = "test",
                SensorReading = 2.2,
                Timestamp = DateTime.UtcNow.AddHours(-2) // Intentionally out of order
            };
            var record2 = new CustomRecord() {
                Device = new DeviceLocation() {
                    LatitudeDegrees = 121,
                    LongitudeDegrees = 31
                },
                LogValue = "test2",
                SensorReading = 2.3,
                Timestamp = DateTime.UtcNow.AddHours(-3) // Intentionally out of order
            };
            var record3 = new CustomRecord() {
                Device = new DeviceLocation() {
                    LatitudeDegrees = 122,
                    LongitudeDegrees = 32
                },
                LogValue = "test3",
                SensorReading = 2.4,
                Timestamp = DateTime.UtcNow.AddHours(-1) // Intentionally out of order
            };

            var batch = new List<CustomRecord>() { record1, record2, record3 };
            return batch;
        }
    }
}
