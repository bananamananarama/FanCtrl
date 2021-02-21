﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibreHardwareMonitor.Hardware;

namespace FanCtrl
{
    public class DimmTemp : BaseSensor
    {
        public delegate bool LockSMBusHandler(int ms);
        public delegate void UnlockSMBusHandler();
        public event LockSMBusHandler LockBus;
        public event UnlockSMBusHandler UnlockBus;

        private byte mAddress = 0;

        public DimmTemp(string id, string name, byte address) : base(LIBRARY_TYPE.DIMM)
        {
            ID = id;
            Name = name;
            mAddress = address;
        }

        public override string getString()
        {
            if (OptionManager.getInstance().IsFahrenheit == true)
                return Util.getFahrenheit(Value) + " °F";
            else
                return Value + " °C";
        }
        public override void update()
        {
            if (LockBus(10) == false)
                return;

            var wordArray = SMBusController.i2cWordData(0, mAddress, 10);
            if (wordArray == null)
            {
                UnlockBus();
                return;
            }
            UnlockBus();

            if (wordArray != null && wordArray.Length == 10)
            {
                var temp = BitConverter.GetBytes(wordArray[5]);
                temp[1] = (byte)(temp[1] & 0x0F);

                ushort count = BitConverter.ToUInt16(temp, 0);
                double value = Math.Round(count * 0.0625f);
                if (value > 0)
                {
                    Value = (int)value;
                }
            }
            Util.sleep(10);
        }

    }
}
