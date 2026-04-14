using VSuiteLab.Controllers;

namespace VSuiteLab.Services;
using Quartz.Impl;
using Quartz;
using System.Collections.Generic;
using System.Threading.Tasks;
using Models;

public class AlarmService
{
    private IScheduler? _scheduler;

    public async Task InitializeAsync()
    {
        var factory = new StdSchedulerFactory();
        _scheduler = await factory.GetScheduler();

        await _scheduler.Start();
    }

    
    /// <summary>
    /// Syncs the alarm list with the scheduler
    /// </summary>
    /// <param name="alarms">The list of alarms to sync</param>
    public async Task SyncAlarmsAsync(IEnumerable<CalDavAlarm> alarms)
    {
        if (_scheduler == null)
            return;
        
        // ❌ Remove stale
        await _scheduler.Clear();

        // ➕ Add/update
        foreach (var alarm in alarms)
        {
            var jobKey = new JobKey(alarm.Id.ToString());

            var job = JobBuilder.Create<AlarmJobController>()
                .WithIdentity(jobKey)
                .UsingJobData("AlarmId", alarm.Id.ToString())
                .UsingJobData("Summary", alarm.Summary ?? "")
                .UsingJobData("Description", alarm.Description ?? "")
                .Build();

            var trigger = TriggerBuilder.Create()
                .WithIdentity($"trigger-{alarm.Id}")
                .StartAt(alarm.SelectedDate!.Value.UtcDateTime)
                .WithSimpleSchedule(x =>
                    x.WithMisfireHandlingInstructionIgnoreMisfires())
                .Build();

            await _scheduler.ScheduleJob(job, trigger);
        }
    }
}