using System;
using System.IO;
using System.Security;
using System.Security.Permissions;
using System.Security.Principal;

namespace Pangolin
{
    static class SecurityLayer
    {
        public static bool IsRunAsAdministrator()
        {
            try
            {
                using (WindowsIdentity windowsIdentity = WindowsIdentity.GetCurrent())
                {
                    WindowsPrincipal windowsPrincipal = new WindowsPrincipal(windowsIdentity);
                    return windowsPrincipal.IsInRole(WindowsBuiltInRole.Administrator);
                }
            }
            catch (SecurityException exc)
            {
                ExceptionLayer.Handle(exc);
            }

            return false;
        }

        #region Directory/File Security
        private static bool HasPermissions(FileIOPermissionAccess fileIOPermissionAccess, string path)
        {
            bool hasPermissions = default;

            if (string.IsNullOrWhiteSpace(path)) { throw new ArgumentNullException("path"); }

            try
            {
                FileIOPermission fileIOPermission = new FileIOPermission(fileIOPermissionAccess, path);

                try
                {
                    fileIOPermission.Demand();
                    hasPermissions = true;
                }
                catch (SecurityException) { /* The caller does not have the appropriate permissions. */}
            }
            catch (ArgumentException) { /* The path parameter does not specify the absolute path to the file or directory. */ }

            return hasPermissions;
        }

        public static bool HasAllAccessPermissions(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) { throw new ArgumentNullException("path"); }

            return HasPermissions(FileIOPermissionAccess.AllAccess, path);
        }

        public static bool HasAppendPermissions(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) { throw new ArgumentNullException("path"); }

            return HasPermissions(FileIOPermissionAccess.Append, path);
        }

        public static bool HasNoAccessPermissions(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) { throw new ArgumentNullException("path"); }

            return HasPermissions(FileIOPermissionAccess.NoAccess, path);
        }

        public static bool HasPathDiscoveryAccess(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) { throw new ArgumentNullException("path"); }

            return HasPermissions(FileIOPermissionAccess.PathDiscovery, path);
        }

        public static bool HasReadPermissions(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) { throw new ArgumentNullException("path"); }

            return HasPermissions(FileIOPermissionAccess.Read, path);
        }

        public static bool HasWritePermissions(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) { throw new ArgumentNullException("path"); }

            return HasPermissions(FileIOPermissionAccess.Append, path);
        }
        #endregion

        #region Validation
        #region DateTime
        public static bool IsValidFutureDate(DateTime givenDateTime, bool includeToday = false)
        {
            long theTicks = givenDateTime.Ticks;

            return includeToday ? theTicks >= DateTime.Now.Ticks : theTicks >= DateTime.Today.AddDays(1).Ticks;
        }

        public static bool IsValidFutureDate(string givenDateTime, bool includeToday = false)
        {
            DateTime theDateTime = DateTime.TryParse(givenDateTime, out theDateTime) ? theDateTime : default;
            
            return IsValidFutureDate(theDateTime, includeToday);
        }

        public static bool IsValidPastDate(DateTime givenDateTime, bool includeToday = false)
        {
            long theTicks = givenDateTime.Ticks;

            return includeToday ? theTicks <= DateTime.Now.Ticks : theTicks < DateTime.Today.Ticks;
        }

        public static bool IsValidPastDate(string givenDateTime, bool includeToday = false)
        {
            DateTime theDateTime = DateTime.TryParse(givenDateTime, out theDateTime) ? theDateTime : default;
            
            return IsValidPastDate(theDateTime, includeToday);
        }
        #endregion
        #endregion
    }
}
