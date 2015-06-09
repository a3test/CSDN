using System;

namespace CSDN
{
    public static class DateTimeExtensions
    {
        public static double getTime(this DateTime dt)
        {
            DateTime st = new DateTime(1970, 1, 1);
            TimeSpan ts = DateTime.Now.Subtract(st);
            return Math.Floor(ts.TotalMilliseconds);
        }
    }
}
