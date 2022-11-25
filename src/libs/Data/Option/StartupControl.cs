﻿// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) FanControl and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Principal;
using System.Windows.Forms;
using Microsoft.Win32;
using Microsoft.Win32.TaskScheduler;
using Action = Microsoft.Win32.TaskScheduler.Action;

namespace FanCtrl
{
    public class StartupControl
    {
        private const string RegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private bool _startup;
        private const string RegistryName = "FanControl";

        public int DelayTime { get; set; }

        public StartupControl()
        {
            if (Environment.OSVersion.Platform >= PlatformID.Unix)
            {
                IsAvailable = false;
                return;
            }

            if (IsAdministrator() && TaskService.Instance.Connected)
            {
                IsAvailable = true;

                Task task = GetTask();
                if (task != null)
                {
                    foreach (Action action in task.Definition.Actions)
                    {
                        if (action.ActionType == TaskActionType.Execute && action is ExecAction execAction)
                        {
                            if (execAction.Path.Equals(Application.ExecutablePath, StringComparison.OrdinalIgnoreCase))
                                _startup = true;
                        }
                    }
                }
            }
            else
            {
                try
                {
                    using (RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(RegistryPath))
                    {
                        string value = (string)registryKey?.GetValue(RegistryName);

                        if (value != null)
                            _startup = value == Application.ExecutablePath;
                    }

                    IsAvailable = true;
                }
                catch (SecurityException)
                {
                    IsAvailable = false;
                }
            }
        }

        public bool IsAvailable { get; }

        public bool Startup
        {
            get { return _startup; }
            set
            {
                if (_startup != value)
                {
                    if (IsAvailable)
                    {
                        if (TaskService.Instance.Connected)
                        {
                            if (value)
                                CreateTask();
                            else
                                DeleteTask();

                            _startup = value;
                        }
                        else
                        {
                            try
                            {
                                if (value)
                                    CreateRegistryKey();
                                else
                                    DeleteRegistryKey();

                                _startup = value;
                            }
                            catch (UnauthorizedAccessException)
                            {
                                throw new InvalidOperationException();
                            }
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                }
            }
        }

        private static bool IsAdministrator()
        {
            try
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(identity);

                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        private static Task GetTask()
        {
            try
            {
                return TaskService.Instance.AllTasks.FirstOrDefault(x => x.Name.Equals(RegistryName, StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return null;
            }
        }

        private void CreateTask()
        {
            TaskDefinition taskDefinition = TaskService.Instance.NewTask();
            taskDefinition.RegistrationInfo.Description = "Starts FanCtrl on Windows startup.";

            var trigger = new LogonTrigger();
            trigger.Delay = new TimeSpan(0, 0, DelayTime);
            taskDefinition.Triggers.Add(trigger);

            taskDefinition.Settings.StartWhenAvailable = true;
            taskDefinition.Settings.DisallowStartIfOnBatteries = false;
            taskDefinition.Settings.StopIfGoingOnBatteries = false;
            taskDefinition.Settings.ExecutionTimeLimit = TimeSpan.Zero;

            taskDefinition.Principal.RunLevel = TaskRunLevel.Highest;
            taskDefinition.Principal.LogonType = TaskLogonType.InteractiveToken;

            taskDefinition.Actions.Add(new ExecAction(Application.ExecutablePath, "", Path.GetDirectoryName(Application.ExecutablePath)));

            TaskService.Instance.RootFolder.RegisterTaskDefinition(RegistryName, taskDefinition);
        }

        private static void DeleteTask()
        {
            Task task = GetTask();
            task?.Folder.DeleteTask(task.Name, false);
        }

        private static void CreateRegistryKey()
        {
            RegistryKey registryKey = Registry.CurrentUser.CreateSubKey(RegistryPath);
            registryKey?.SetValue(RegistryName, Application.ExecutablePath);
        }

        private static void DeleteRegistryKey()
        {
            RegistryKey registryKey = Registry.CurrentUser.CreateSubKey(RegistryPath);
            registryKey?.DeleteValue(RegistryName);
        }
    }
}
