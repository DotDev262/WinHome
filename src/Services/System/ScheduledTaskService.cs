using System;
using Microsoft.Win32.TaskScheduler;
using System.Runtime.Versioning;
using WinHome.Interfaces;
using WinHome.Models;

namespace WinHome.Services.System
{
    /// <summary>
    /// Provides administrative orchestration for the Windows Task Scheduler, enabling 
    /// programmatic creation, registration, and management of automated background tasks 
    /// via abstracted trigger and execution action configurations.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class ScheduledTaskService : IScheduledTaskService
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduledTaskService"/> class with telemetry support.
        /// </summary>
        /// <param name="logger">The diagnostic log tracker capture utility routing system statuses or errors.</param>
        public ScheduledTaskService(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Registers or updates a persistent scheduled task definition in the root system task folder 
        /// based on the provided configuration schema.
        /// </summary>
        /// <param name="task">The parsed task metadata container containing triggers, actions, and security context info.</param>
        /// <param name="dryRun">A flag which, when <c>true</c>, simulates the operation via logs without modifying the Task Scheduler registry.</param>
        public void Apply(ScheduledTaskConfig task, bool dryRun)
        {
            _logger.LogInfo($"[TaskScheduler] Initializing configuration for '{task.Name}'...");

            if (dryRun)
            {
                _logger.LogInfo($"[DryRun] Would register scheduled task '{task.Path}' defined as '{task.Name}'.");
                return;
            }

            using (var ts = new TaskService())
            {
                var taskDefinition = ts.NewTask();

                taskDefinition.RegistrationInfo.Description = task.Description;
                taskDefinition.RegistrationInfo.Author = task.Author;

                foreach (var trigger in task.Triggers)
                {
                    taskDefinition.Triggers.Add(CreateTrigger(trigger));
                }

                foreach (var action in task.Actions)
                {
                    taskDefinition.Actions.Add(CreateAction(action));
                }

                ts.RootFolder.RegisterTaskDefinition(task.Path, taskDefinition);
            }

            _logger.LogSuccess($"[TaskScheduler] Scheduled task '{task.Name}' registered successfully.");
        }

        /// <summary>
        /// Factory method that generates specific <see cref="Trigger"/> types based on requested temporal conditions 
        /// and applies standard repetition and boundary constraints.
        /// </summary>
        /// <param name="triggerConfig">The configuration model specifying trigger types, timings, and recurrence rules.</param>
        /// <returns>A concrete <see cref="Trigger"/> instance for the Windows Task Scheduler engine.</returns>
        private Trigger CreateTrigger(TriggerConfig triggerConfig)
        {
            Trigger trigger = triggerConfig.Type.ToLower() switch
            {
                "daily" => new DailyTrigger(),
                "weekly" => new WeeklyTrigger(),
                "monthly" => new MonthlyTrigger(),
                "logon" => new LogonTrigger(),
                _ => throw new NotSupportedException($"Trigger type '{triggerConfig.Type}' is not supported.")
            };

            trigger.Enabled = triggerConfig.Enabled;
            if (triggerConfig.StartBoundary.HasValue) trigger.StartBoundary = triggerConfig.StartBoundary.Value;
            if (triggerConfig.EndBoundary.HasValue) trigger.EndBoundary = triggerConfig.EndBoundary.Value;
            if (triggerConfig.ExecutionTimeLimit.HasValue) trigger.ExecutionTimeLimit = triggerConfig.ExecutionTimeLimit.Value;
            trigger.Id = triggerConfig.Id;

            if (triggerConfig.Repetition != null)
            {
                trigger.Repetition.Interval = triggerConfig.Repetition.Interval;
                trigger.Repetition.Duration = triggerConfig.Repetition.Duration;
                trigger.Repetition.StopAtDurationEnd = triggerConfig.Repetition.StopAtDurationEnd;
            }

            return trigger;
        }

        /// <summary>
        /// Factory method mapping logical action configurations to native OS execution primitives.
        /// </summary>
        /// <param name="actionConfig">The configuration model defining the binary path, arguments, and context for the task action.</param>
        /// <returns>A concrete <see cref="Microsoft.Win32.TaskScheduler.Action"/> instance.</returns>
        private Microsoft.Win32.TaskScheduler.Action CreateAction(ActionConfig actionConfig)
        {
            return actionConfig.Type.ToLower() switch
            {
                "exec" => new ExecAction(actionConfig.Path, actionConfig.Arguments, actionConfig.WorkingDirectory),
                _ => throw new NotSupportedException($"Action type '{actionConfig.Type}' is not supported.")
            };
        }
    }
}