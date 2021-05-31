using System;

public class TimeUtils
{
    // 格林威治时间
    readonly static DateTime GMT = new DateTime(1970, 1, 1, 8, 0, 0);

    //自格林威治时间以来经过的毫秒数
    public static double DeltaMillisecondsSinceGMT()
    {
        DateTime nowtime = DateTime.Now.ToLocalTime();
        return nowtime.Subtract(GMT).TotalMilliseconds;
    }
    //自格林威治时间以来经过的秒数
    public static double DeltaSecondsSinceGMT()
    {
        DateTime nowtime = DateTime.Now.ToLocalTime();
        return nowtime.Subtract(GMT).TotalSeconds;
    }
}