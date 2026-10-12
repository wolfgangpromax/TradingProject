using System;
using System.Collections.Generic;
using System.Text;

namespace TradingAnalyst.Business.Domain.Enums
{
    public enum Timeframe : short
    {
        OneMinute = 1,
        ThreeMinutes = 3,
        FiveMinutes = 5,
        FifteenMinutes = 15,
        ThirtyMinutes = 30,
        OneHour = 60,
        FourHours = 240,
        OneDay = 1440
    }
}
