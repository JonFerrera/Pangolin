using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Pangolin
{
    public static class DiagnosticsLayer
    {
        public static string TimestampHumanFormat => DateTime.Now.ToString(ConfigurationLayer.DateTimeHumanFormat);
        public static string TimestampLongFormat => DateTime.Now.ToString(ConfigurationLayer.DateTimeLongFormat);
        public static string TimestampLongFormatFile => DateTime.Now.ToString(ConfigurationLayer.DateTimeLongFormatFile);
        public static string TimestampShortFormat => DateTime.Now.ToString(ConfigurationLayer.DateTimeShortFormat);
        public static string TimestampShortFormatFile => DateTime.Now.ToString(ConfigurationLayer.DateTimeShortFormatFile);

        #region Event Log
        public static bool IsEventLogCleared()
        {
            bool isSuccess = false;

            if (EventLog.SourceExists(ConfigurationLayer.EventLogSource))
            {
                try
                {
                    using (EventLog eventLog = new EventLog() { Source = ConfigurationLayer.EventLogSource, Log = ConfigurationLayer.EventLogName, MaximumKilobytes = ConfigurationLayer.EventLogMaxSize })
                    {
                        try
                        {
                            eventLog.Clear();
                            isSuccess = true;
                        }
                        catch (Win32Exception) { }
                    }
                }
                catch (ArgumentOutOfRangeException) { }
                catch (ArgumentException) { }
                catch (InvalidOperationException) { }
            }

            return isSuccess;
        }

        public static string EventLogRead()
        {
            string eventLogEntries = string.Empty;

            if (IsEventLogCreated())
            {
                try
                {
                    using (EventLog eventLog = new EventLog() { Source = ConfigurationLayer.EventLogSource, Log = ConfigurationLayer.EventLogName, MaximumKilobytes = ConfigurationLayer.EventLogMaxSize })
                    {
                        if (eventLog?.Entries?.Count >= 1)
                        {
                            string[] eventLogMessages = eventLog.Entries.Cast<EventLogEntry>().Select(x => x.Message).ToArray();

                            eventLogEntries = string.Join(Environment.NewLine, eventLogMessages);
                        }
                    }
                }
                catch (ArgumentException) { }
                catch (InvalidOperationException) { }
            }

            return eventLogEntries;
        }

        public static void EventLogWriteAudit(string auditMessage, bool isSuccess = true)
        {
            if (isSuccess)
            {
                EventLogWrite(auditMessage, EventLogEntryType.SuccessAudit);
            }
            else
            {
                EventLogWrite(auditMessage, EventLogEntryType.FailureAudit);
            }
        }

        public static void EventLogWriteError(string errorMessage)
        {
            EventLogWrite(errorMessage, EventLogEntryType.Error);
        }

        public static void EventLogWriteInformation(string informationMessage)
        {
            EventLogWrite(informationMessage, EventLogEntryType.Information);
        }

        public static void EventLogWriteWarning(string warningMessage)
        {
            EventLogWrite(warningMessage, EventLogEntryType.Warning);
        }
        
        private static bool IsEventLogCreated()
        {
            bool isSuccess = false;

            try
            {
                if (EventLog.SourceExists(ConfigurationLayer.EventLogSource))
                {
                    isSuccess = true;
                }
                else
                {
                    try
                    {
                        EventLog.CreateEventSource(new EventSourceCreationData(ConfigurationLayer.EventLogSource, ConfigurationLayer.EventLogName));
                        isSuccess = true;
                    }
                    catch (ArgumentNullException) { }
                    catch (InvalidOperationException) { }
                }
            }
            catch (SecurityException) { }

            return isSuccess;
        }

        private static void EventLogWrite(string message, EventLogEntryType eventLogEntryType)
        {
            if (!string.IsNullOrWhiteSpace(message) && IsEventLogCreated())
            {
                try
                {
                    using (EventLog eventLog = new EventLog() { Source = ConfigurationLayer.EventLogSource, Log = ConfigurationLayer.EventLogName, MaximumKilobytes = ConfigurationLayer.EventLogMaxSize })
                    {
                        eventLog.ModifyOverflowPolicy(OverflowAction.OverwriteOlder, ConfigurationLayer.EventLogMaxAge);

                        try
                        {
                            eventLog.WriteEntry(message, eventLogEntryType);
                        }
                        catch (Win32Exception) { }
                    }
                }
                catch (ArgumentOutOfRangeException) { }
                catch (ArgumentException) { }
                catch (InvalidOperationException) { }
            }
        }
        #endregion

        #region Executeable
        #region Calling Assembly
        public static bool IsCallingAssemblyFullyTrusted()
        {
            bool isFullyTrusted = false;

            Assembly callingAssembly = Assembly.GetCallingAssembly();
            isFullyTrusted = callingAssembly.IsFullyTrusted;

            return isFullyTrusted;
        }

        public static string GetCallingAssemblyLocation()
        {
            string location = string.Empty;

            Assembly callingAssembly = Assembly.GetCallingAssembly();
            try
            {
                location = callingAssembly.Location;
            }
            catch (NotSupportedException) { /* The current assembly is a dynamic assembly. */ }

            return location;
        }
        #endregion

        #region Entry Assembly
        public static bool IsEntryAssemblyFullyTrusted()
        {
            bool isFullyTrusted = false;

            Assembly entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly != null) /* Can return null when a managed assembly has been loaded from an unmanaged application. */
            {
                isFullyTrusted = entryAssembly.IsFullyTrusted;
            }

            return isFullyTrusted;
        }

        public static string GetEntryAssemblyLocation()
        {
            string location = string.Empty;

            Assembly entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly != null) /* Can return null when a managed assembly has been loaded from an unmanaged application. */
            {
                try
                {
                    location = entryAssembly.Location;
                }
                catch (NotSupportedException) { /* The current assembly is a dynamic assembly. */ }
            }

            return location;
        }
        #endregion

        #region Executing Assembly
        public static bool IsExecutingAssemblyFullyTrusted()
        {
            bool isFullyTrusted = false;

            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            isFullyTrusted = executingAssembly.IsFullyTrusted;

            return isFullyTrusted;
        }

        public static string GetExecutinggAssemblyLocation()
        {
            string location = string.Empty;

            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            try
            {
                location = executingAssembly.Location;
            }
            catch (NotSupportedException) { /* The current assembly is a dynamic assembly. */ }

            return location;
        }
        #endregion

        public static (string codeBase, string fullName) GetApplicationIdentity()
        {
            ApplicationIdentity applicationIdentity = AppDomain.CurrentDomain.ApplicationIdentity;

            return (applicationIdentity.CodeBase, applicationIdentity.FullName);
        }

        public static string GetBaseDirectory()
        {
            string baseDirectory = string.Empty;

            try
            {
                baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            }
            catch (AppDomainUnloadedException) { /* The operation is attempted on an unloaded application domain. */ }

            return baseDirectory;
        }
        #endregion

        #region Network
        public async static Task<bool> IsEthernetConnection()
        {
            bool isSuccess = false;

            if (IsNetworkAvailable())
            {
                try
                {
                    isSuccess = NetworkInterface.GetAllNetworkInterfaces().Any(x => (x.NetworkInterfaceType == NetworkInterfaceType.Ethernet || x.NetworkInterfaceType == NetworkInterfaceType.Ethernet3Megabit || x.NetworkInterfaceType == NetworkInterfaceType.FastEthernetFx || x.NetworkInterfaceType == NetworkInterfaceType.FastEthernetT) && x.OperationalStatus == OperationalStatus.Up);
                }
                catch (NetworkInformationException exc) { await ExceptionLayer.CoreHandleAsync(exc); }
            }

            return isSuccess;
        }
        
        public async static Task<bool> IsMobileBroadbandConnection()
        {
            bool isSuccess = false;

            if (IsNetworkAvailable())
            {
                try
                {
                    isSuccess = NetworkInterface.GetAllNetworkInterfaces().Any(x => (x.NetworkInterfaceType == NetworkInterfaceType.Wman || x.NetworkInterfaceType == NetworkInterfaceType.Wwanpp || x.NetworkInterfaceType == NetworkInterfaceType.Wwanpp2) && x.OperationalStatus == OperationalStatus.Up);
                }
                catch (NetworkInformationException exc) { await ExceptionLayer.CoreHandleAsync(exc); }
            }

            return isSuccess;
        }

        public static bool IsNetworkAvailable()
        {
            return NetworkInterface.GetIsNetworkAvailable();
        }

        public async static Task<bool> IsPingable(string hostNameOrAddress)
        {
            bool isPingable = false;

            if (!string.IsNullOrWhiteSpace(hostNameOrAddress))
            {
                using (Ping ping = new Ping())
                {
                    
                    try
                    {
                        PingReply pingReply = await ping.SendPingAsync(hostNameOrAddress);
                        isPingable = pingReply.Status == IPStatus.Success;
                    }
                    catch (PingException exc) { await ExceptionLayer.CoreHandleAsync(exc); }
                    catch (InvalidOperationException exc) { await ExceptionLayer.CoreHandleAsync(exc); }
                    catch (NotSupportedException exc) { await ExceptionLayer.CoreHandleAsync(exc); }
                }
            }

            return isPingable;
        }

        public async static Task<bool> IsPingable(string hostNameOrAddress, int timeout)
        {
            bool isPingable = false;

            if (!string.IsNullOrWhiteSpace(hostNameOrAddress) && timeout > 0)
            {
                using (Ping ping = new Ping())
                {
                    try
                    {
                        PingReply pingReply = await ping.SendPingAsync(hostNameOrAddress, timeout);
                        isPingable = pingReply.Status == IPStatus.Success;
                    }
                    catch (PingException exc) { await ExceptionLayer.CoreHandleAsync(exc); }
                    catch (InvalidOperationException exc) { await ExceptionLayer.CoreHandleAsync(exc); }
                    catch (NotSupportedException exc) { await ExceptionLayer.CoreHandleAsync(exc); }
                }
            }

            return isPingable;
        }

        public async static Task<bool> IsVPNConnection()
        {
            // Doesn't always work?
            bool isSuccess = false;

            if (IsNetworkAvailable())
            {
                try
                {
                    isSuccess = NetworkInterface.GetAllNetworkInterfaces().Any(x => x.NetworkInterfaceType == NetworkInterfaceType.Ppp && x.NetworkInterfaceType != NetworkInterfaceType.Loopback && x.OperationalStatus == OperationalStatus.Up);
                }
                catch (NetworkInformationException exc) { await ExceptionLayer.CoreHandleAsync(exc); }
            }

            return isSuccess;
        }

        public async static Task<bool> IsWirelessConnection()
        {
            bool isSuccess = false;

            if (IsNetworkAvailable())
            {
                try
                {
                    isSuccess = NetworkInterface.GetAllNetworkInterfaces().Any(x => x.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 && x.OperationalStatus == OperationalStatus.Up);
                }
                catch (NetworkInformationException exc) { await ExceptionLayer.CoreHandleAsync(exc); }
            }

            return isSuccess;
        }
        #endregion

        #region Peripherals
        public static string GetSystemDriveInformation()
        {
            StringBuilder systemDriveInformation = new StringBuilder(512);

            try
            {
                DriveInfo[] drives = DriveInfo.GetDrives();

                try
                {
                    foreach (DriveInfo drive in drives)
                    {
                        systemDriveInformation.AppendLine($"Name: {drive.Name}");
                        systemDriveInformation.AppendLine($"Type: {drive.DriveType.ToString()}");

                        if (drive.IsReady)
                        {
                            systemDriveInformation.AppendLine($"Format: {drive.DriveFormat}");
                            systemDriveInformation.AppendLine($"Root Directory: {drive.RootDirectory.Name}");
                            systemDriveInformation.AppendLine($"Volume Label: {drive.VolumeLabel}");
                            systemDriveInformation.AppendLine($"Available Free Space: {drive.AvailableFreeSpace.ToString()} bytes");
                            systemDriveInformation.AppendLine($"Total Free Space: {drive.TotalFreeSpace.ToString()} bytes");
                            systemDriveInformation.AppendLine($"Total Size: {drive.TotalSize.ToString()} bytes");
                        }

                        systemDriveInformation.AppendLine();
                    }
                }
                catch (SecurityException) { /* The caller does not have the required permission. */ }
                catch (DriveNotFoundException) { /* The drive is not mapped or does not exist. */ }
            }
            catch (IOException) { /* An I/O error occurred (for example, a disk error or a drive was not ready). */ }
            catch (UnauthorizedAccessException) { /* The caller does not have the required permission. */ }

            return systemDriveInformation.ToString();
        }

        public static string GetPrinterInformation()
        {
            var printerQuery = new ManagementObjectSearcher("SELECT * from Win32_Printer");
            foreach (var printer in printerQuery.Get())
            {
                var name = printer.GetPropertyValue("Name");
                var status = printer.GetPropertyValue("Status");
                var isDefault = printer.GetPropertyValue("Default");
                var isNetworkPrinter = printer.GetPropertyValue("Network");
            }

            return string.Empty;
        }
        #endregion

        #region Process
        public static int GetBasePriority(Process process)
        {
            int basePriority = default;
            process.Refresh();

            try
            {
                basePriority = process.BasePriority;
            }
            catch (PlatformNotSupportedException) { /* The platform is Windows 98 or Windows Millennium Edition (Windows Me). */ }
            catch (InvalidOperationException) { /* The process has exited or the process has not started, so there is no process ID.*/ }

            return basePriority;
        }

        public static long GetPhysicalMemoryUsage(Process process)
        {
            long physicalMemoryUsage = default;
            process.Refresh();

            try
            {
                physicalMemoryUsage = process.WorkingSet64;
            }
            catch (PlatformNotSupportedException) { /* The platform is Windows 98 or Windows Millennium Edition (Windows Me). */ }

            return physicalMemoryUsage;
        }

        public static string GetMachineName(Process process)
        {
            string machineName = string.Empty;
            process.Refresh();

            try
            {
                machineName = process.MachineName;
            }
            catch (InvalidOperationException) { /* There is no process associated with this Process object. */ }

            return machineName;
        }

        public static string GetName(Process process)
        {
            process.Refresh();
            return process.ProcessName;
        }

        public static long GetNonpagedSystemMemory(Process process)
        {
            long nonpagedSystemMemory = default;
            process.Refresh();

            try
            {
                nonpagedSystemMemory = process.NonpagedSystemMemorySize64;
            }
            catch (PlatformNotSupportedException) { /* The platform is Windows 98 or Windows Millennium Edition (Windows Me). */ }

            return nonpagedSystemMemory;
        }

        public static long GetPagedMemory(Process process)
        {
            long pagedMemorySize = default;
            process.Refresh();

            try
            {
                pagedMemorySize = process.PagedMemorySize64;
            }
            catch (PlatformNotSupportedException) { /* The platform is Windows 98 or Windows Millennium Edition (Windows Me). */ }

            return pagedMemorySize;
        }

        public static long GetPagedSystemMemory(Process process)
        {
            long pagedSystemMemory = default;
            process.Refresh();

            try
            {
                pagedSystemMemory = process.PagedSystemMemorySize64;
            }
            catch (PlatformNotSupportedException) { /* The platform is Windows 98 or Windows Millennium Edition (Windows Me). */ }

            return pagedSystemMemory;
        }

        public static long GetPeakPagedMemory(Process process)
        {
            long peakPagedMemory = default;
            process.Refresh();

            try
            {
                peakPagedMemory = process.PeakPagedMemorySize64;
            }
            catch (PlatformNotSupportedException) { /* The platform is Windows 98 or Windows Millennium Edition (Windows Me). */ }

            return peakPagedMemory;
        }
        
        public static long GetPeakVirtualMemory(Process process)
        {
            long peakVirtualMemory = default;
            process.Refresh();

            try
            {
                peakVirtualMemory = process.PeakVirtualMemorySize64;
            }
            catch (PlatformNotSupportedException) { /* The platform is Windows 98 or Windows Millennium Edition (Windows Me). */ }

            return peakVirtualMemory;
        }

        public static long GetPeakWorkingSet(Process process)
        {
            long peakWorkingSet = default;
            process.Refresh();

            try
            {
                peakWorkingSet = process.PeakWorkingSet64;
            }
            catch (PlatformNotSupportedException) { /* The platform is Windows 98 or Windows Millennium Edition (Windows Me). */ }

            return peakWorkingSet;
        }

        public static long GetPrivateMemory(Process process)
        {
            long privateMemory = default;
            process.Refresh();

            try
            {
                privateMemory = process.PrivateMemorySize64;
            }
            catch (PlatformNotSupportedException) { /* The platform is Windows 98 or Windows Millennium Edition (Windows Me). */ }

            return privateMemory;
        }

        public static TimeSpan GetPrivilegedProcessorTime(Process process)
        {
            TimeSpan privilegedProcessorTime = new TimeSpan();
            process.Refresh();

            try
            {
                privilegedProcessorTime = process.PrivilegedProcessorTime;
            }
            catch (PlatformNotSupportedException) { /* The platform is Windows 98 or Windows Millennium Edition (Windows Me). */ }
            catch (NotSupportedException) { /* You are attempting to access the PrivilegedProcessorTime property for a process that is running on a remote computer. */ }

            return privilegedProcessorTime;
        }

        public static int GetSessionId(Process process)
        {
            int sessionId = default;

            try
            {
                sessionId = process.SessionId;
            }
            catch (NullReferenceException) { /* There is no session associated with this process. */ }
            catch (InvalidOperationException) { /* There is no process associated with this session identifier or the associated process is not on this machine. */ }
            catch (PlatformNotSupportedException) { /* The SessionId property is not supported on Windows 98. */ }

            return sessionId;
        }

        public static TimeSpan GetTotalProcessorTime(Process process)
        {
            TimeSpan totalProcessorTime = new TimeSpan();
            process.Refresh();

            try
            {
                totalProcessorTime = process.TotalProcessorTime;
            }
            catch (PlatformNotSupportedException) { /* The platform is Windows 98 or Windows Millennium Edition (Windows Me). */ }
            catch (NotSupportedException) { /* You are attempting to access the TotalProcessorTime property for a process that is running on a remote computer. */ }

            return totalProcessorTime;
        }

        public static TimeSpan GetUserProcessorTime(Process process)
        {
            TimeSpan userProcessorTime = new TimeSpan();
            process.Refresh();

            try
            {
                userProcessorTime = process.UserProcessorTime;
            }
            catch (PlatformNotSupportedException) { /* The platform is Windows 98 or Windows Millennium Edition (Windows Me). */ }
            catch (NotSupportedException) { /* You are attempting to access the UserProcessorTime property for a process that is running on a remote computer. */ }

            return userProcessorTime;
        }

        public static bool HasExited(Process process)
        {
            process.Refresh();
            return process.HasExited;
        }

        public static bool IsResponding(Process process)
        {
            process.Refresh();
            return process.Responding;
        }

        public static async Task<bool> IsCurrentlyRunning()
        {
            try
            {
                return Process.GetProcessesByName(Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location)).Count() > 1;
            }
            catch (NotSupportedException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (InvalidOperationException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (OverflowException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
        }
        #endregion

        #region Threading
        public static bool BelongsToManagedThreadPool(Thread thread)
        {
            return thread.IsThreadPoolThread;
        }

        public static int GetId(Thread thread)
        {
            return thread.ManagedThreadId;
        }

        public static string GetName(Thread thread)
        {
            return thread.Name;
        }

        public static string GetPriority(Thread thread)
        {
            string priority = string.Empty;
            try
            {
                priority = thread.Priority.ToString();
            }
            catch (ThreadStateException) { /* The thread has reached a final state. */ }

            return priority;
        }

        public static string GetState(Thread thread)
        {
            return thread.ThreadState.ToString();
        }

        public static bool IsAlive(Thread thread)
        {
            return thread.IsAlive;
        }

        public static bool IsBackground(Thread thread)
        {
            return thread.IsBackground;
        }

        public static int GetCurrentThreadId()
        {
            return GetId(Thread.CurrentThread);
        }

        public static string GetCurrentThreadName()
        {
            return GetName(Thread.CurrentThread);
        }

        public static string GetCurrentThreadPriority()
        {
            return GetPriority(Thread.CurrentThread);
        }

        public static string GetCurrentThreadState()
        {
            return GetState(Thread.CurrentThread);
        }

        public static bool IsBackgroundCurrentThread()
        {
            return IsBackground(Thread.CurrentThread);
        }

        public static bool BelongsToManagedThreadPoolCurrentThread()
        {
            return BelongsToManagedThreadPool(Thread.CurrentThread);
        }
        #endregion

        #region Web Server
        private static async Task<string> GetCurrentDirectory()
        {
            try
            {
                return Environment.CurrentDirectory;
            }
            catch (IOException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (SecurityException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
        }

        private static async Task<string> GetDomainName()
        {
            try
            {
                return Environment.UserDomainName;
            }
            catch (PlatformNotSupportedException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (InvalidOperationException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
        }

        private static async Task<string> GetMachineName()
        {
            try
            {
                return Environment.MachineName;
            }
            catch (InvalidOperationException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
        }

        private static async Task<string> GetOSVersion()
        {
            try
            {
                return Environment.OSVersion.VersionString;
            }
            catch (InvalidOperationException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
        }

        public static TimeSpan GetUpTime()
        {
            return new TimeSpan(GetTickCount());
        }

        private static int GetTickCount()
        {
            return Environment.TickCount;
        }

        private static async Task<string> GetUserDomainName()
        {
            try
            {
                return Environment.UserDomainName;
            }
            catch (PlatformNotSupportedException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (InvalidOperationException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
        }

        private static async Task<string> GetUserName()
        {
            try
            {
                return Environment.UserName;
            }
            catch (PlatformNotSupportedException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (InvalidOperationException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
        }

        private static long GetWorkingSet()
        {
            return Environment.WorkingSet;
        }

        private static bool Is64BitOperatingSystem()
        {
            return Environment.Is64BitOperatingSystem;
        }
        private static bool Is64BitProcess()
        {
            return Environment.Is64BitProcess;
        }
        private static bool IsUserInteractive()
        {
            return Environment.UserInteractive;
        }
        #endregion
    }
}