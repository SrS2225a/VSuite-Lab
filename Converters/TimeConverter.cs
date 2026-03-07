using System;

namespace VSuiteLab.Converters;

public class TimeConverter
{
    public static double ConvertToDisplay(float seconds, TimeUnit unit) => unit switch
    {
        TimeUnit.Minutes => seconds / 60.0,
        TimeUnit.Hours => seconds / 3600.0,
        _ => seconds
    };

    public static int ConvertToSeconds(double value, TimeUnit unit) => unit switch
    {
        TimeUnit.Minutes => (int)(value * 60),
        TimeUnit.Hours => (int)(value * 3600),
        _ => (int)value
    };

    public static DateTimeOffset? GetDateOnly(DateTimeOffset? source)
    {
        return source?.Date;
    }

    public static TimeSpan? GetTimeOnly(DateTimeOffset? source)
    {
        return source?.TimeOfDay;
    }

    public static DateTimeOffset? SetDateOnly(DateTimeOffset? current, DateTimeOffset? newDate)
    {
        if (newDate == null)
            return null;
        
        var time = current?.TimeOfDay ?? TimeSpan.Zero;
        return new DateTimeOffset(newDate.Value.Date + time, newDate.Value.Offset);
    }

    public static DateTimeOffset? SetTimeOnly(DateTimeOffset? current, TimeSpan? newTime)
    {
        if(current == null) 
            return null;
        
        var date = current.Value.Date;
        var offset = current.Value.Offset;
        
        return new DateTimeOffset(date + (newTime ?? TimeSpan.Zero), offset);
    }
    
    public enum TimeUnit
    {
        Minutes,
        Hours
    }
}