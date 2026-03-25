using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Quartz;
using VSuiteLab.Models;
using VSuiteLab.Services.NotificationController;

public class AlarmJobController : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var map = context.MergedJobDataMap;

        var alarmId = Guid.Parse(map.GetString("AlarmId"));
        var summary = map.GetString("Summary") ?? "Reminder";
        var description = map.GetString("Description") ?? "";
        
        await using var db = new DatabaseContext();

        var alarm = await db.Set<CalDavAlarm>()
            .FirstOrDefaultAsync(a => a.Id == alarmId);

        if (alarm != null && !alarm.HasRan)
        {
            alarm.HasRan = true;
            await db.SaveChangesAsync();
        }
        
        var notifications = new NotificationService();
        await notifications.ShowNotificationAsync(summary, description);
        
        await context.Scheduler.DeleteJob(context.JobDetail.Key);
    }
}