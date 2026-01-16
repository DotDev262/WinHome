using Microsoft.Extensions.Logging;
using Microsoft.Win32.TaskScheduler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinHome.Interfaces;
using WinHome.Models;

namespace WinHome.Services.System
{
    public class ScheduledTaskService : IScheduledTaskService
    {
        private readonly ILogger<ScheduledTaskService> _logger;

        public ScheduledTaskService(ILogger<ScheduledTaskService> logger)
        {
            _logger = logger;
        }

        public void Apply(ScheduledTaskConfig task, bool dryRun)
        {
            _logger.LogInformation($"Applying scheduled task '{task.Name}'...");

            if (dryRun)
            {
                _logger.LogInformation($"[DryRun] Would create or update scheduled task '{task.Name}'.");
                return;
            }

            using (var ts = new TaskService())
            {
                var taskDefinition = ts.NewTask();

                taskDefinition.RegistrationInfo.Description = task.Description;
                taskDefinition.RegistrationInfo.Author = task.Author;

                foreach (var trigger in task.Triggers)
                {
                    var taskTrigger = CreateTrigger(trigger);
                    taskDefinition.Triggers.Add(taskTrigger);
                }

                foreach (var action in task.Actions)
                {
                    var taskAction = CreateAction(action);
                    taskDefinition.Actions.Add(taskAction);
                }

                ts.RootFolder.RegisterTaskDefinition(task.Path, taskDefinition);
            }

            _logger.LogInformation($"Scheduled task '{task.Name}' applied successfully.");
        }

        private Trigger CreateTrigger(TriggerConfig triggerConfig)
        {
            Trigger trigger;

            switch (triggerConfig.Type.ToLower())
            {
                case "daily":
                    trigger = new DailyTrigger();
                    break;
                case "weekly":
                    trigger = new WeeklyTrigger();
                    break;
                case "monthly":
                    trigger = new MonthlyTrigger();
                    break;
                case "logon":
                    trigger = new LogonTrigger();
                    break;
                default:
                    throw new NotSupportedException($"Trigger type '{triggerConfig.Type}' is not supported.");
            }

            trigger.Enabled = triggerConfig.Enabled;
            if(triggerConfig.StartBoundary.HasValue)
                trigger.StartBoundary = triggerConfig.StartBoundary.Value;
            if (triggerConfig.EndBoundary.HasValue)
                trigger.EndBoundary = triggerConfig.EndBoundary.Value;
            if (triggerConfig.ExecutionTimeLimit.HasValue)
                trigger.ExecutionTimeLimit = triggerConfig.ExecutionTimeLimit.Value;
            trigger.Id = triggerConfig.Id;
            if(triggerConfig.Repetition != null)
            {
                trigger.Repetition.Interval = triggerConfig.Repetition.Interval;
                trigger.Repetition.Duration = triggerConfig.Repetition.Duration;
                trigger.Repetition.StopAtDurationEnd = triggerConfig.Repetition.StopAtDurationEnd;
            }


            return trigger;
        }

        private Microsoft.Win32.TaskScheduler.Action CreateAction(ActionConfig actionConfig)
        {
            Microsoft.Win32.TaskScheduler.Action action;

            switch (actionConfig.Type.ToLower())
            {
                case "exec":
                    action = new ExecAction(actionConfig.Path, actionConfig.Arguments, actionConfig.WorkingDirectory);
                    break;
                default:
                    throw new NotSupportedException($"Action type '{actionConfig.Type}' is not supported.");
            }

            return action;
        }
    }
}
