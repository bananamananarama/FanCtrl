﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanCtrl
{
    public class CLCPumpControl : BaseControl
    {
        private CLC mCLC = null;

        public CLCPumpControl(string id, CLC clc, uint num) : base(LIBRARY_TYPE.EVGA_CLC)
        {
            ID = id;
            mCLC = clc;
            Name = "EVGA CLC Pump";
            if (num > 1)
            {
                Name = Name + " #" + num;
            }
        }

        public override void update()
        {

        }

        public override int getMinSpeed()
        {
            return mCLC.getMinPumpSpeed();
        }

        public override int getMaxSpeed()
        {
            return mCLC.getMaxPumpSpeed();
        }

        public override void setSpeed(int value)
        {
            int min = this.getMinSpeed();
            int max = this.getMaxSpeed();

            if (value > max)
            {
                Value = max;
            }
            else if (value < min)
            {
                Value = min;
            }
            else
            {
                Value = value;
            }
            mCLC.setPumpSpeed(Value);
        }
    }
}
