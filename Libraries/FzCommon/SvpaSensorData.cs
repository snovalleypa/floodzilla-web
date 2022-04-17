using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace FzCommon
{
    public class SvpaSensorData
    {
        public ushort Version { get; set; }
        public uint UniqueId { get; set; }
        public uint Size { get; set; }
        public uint TypeId { get; set; }

        public SvpaSensorData(uint uniqueId)
        {
            this.UniqueId = uniqueId;
        }
    }

    public class SvpaSensorHeaders : SvpaSensorData
    {
        public int HeaderId { get; set; }

        public int Count { get; set; }
        public int BatteryVolt { get; set; }
        public int HeaterVolt { get; set; }
        public int HeaterOnBatteryVolt { get; set; }
        public int TimeBetweenADC { get; set; }
        public int HeaterOnTime { get; set; }
        public int StartTempTop { get; set; }
        public int StartTempBottom { get; set; }
        public int IterationNumber { get; set; }
        public DateTime? DeviceTimeStamp { get; set; }
        public int ICCID4 { get; set; }
        public int ICCIDLAST4 { get; set; }

        internal static readonly TimeSpan MinValidTimeStamp = new TimeSpan(30, 0, 0, 0);
        internal static readonly TimeSpan MaxValidTimeStamp = new TimeSpan(0, 3, 0, 0);

        public SvpaSensorHeaders(uint uniqueId) : base(uniqueId)
        {
        }
    }

    public class SvpaSensorLevel : SvpaSensorData
    {
        private uint id;
        private int HeaderId;
        private DateTime? timeStamp;

        public SvpaSensorLevel(uint uniqueId, int headerId, DateTime? timeStamp) : base(uniqueId)
        {
            this.id = uniqueId;
            this.HeaderId = headerId;
            this.timeStamp = timeStamp;
        }

        //$ TODO: figure this out
    }
}
