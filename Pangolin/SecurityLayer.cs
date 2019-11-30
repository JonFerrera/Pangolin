using System;
using System.IO;
using System.Security;
using System.Security.Permissions;
using System.Security.Principal;

namespace Pangolin
{
    static class SecurityLayer
    {
        /// <summary>
        /// Checks if the current application is run as an administrator.
        /// </summary>
        /// <returns>Returns true if the current application is run as an administrator.</returns>
        /// <exception cref="SecurityException">The caller does not have the correct permissions.</exception>
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
            catch (SecurityException exc) { ExceptionLayer.Handle(exc); }

            return false;
        }

        #region Directory/File Security
        /// <summary>
        /// Checks if the caller of the function has certain permissions on a path.
        /// </summary>
        /// <param name="fileIOPermissionAccess">The type of file access.</param>
        /// <param name="path">The path to the directory or file to check permissions.</param>
        /// <returns>Returns a bool whether the caller has file access on the path.</returns>
        /// <exception cref="ArgumentNullException">The path is null.</exception>
        /// <exception cref="ArgumentException">The path parameter does not specify the absolute path to the file or directory.</exception>
        private static bool HasPermissions(FileIOPermissionAccess fileIOPermissionAccess, string path)
        {
            bool hasPermissions = default;

            if (string.IsNullOrWhiteSpace(path)) { throw new ArgumentNullException(nameof(path)); }

            try
            {
                FileIOPermission fileIOPermission = new FileIOPermission(fileIOPermissionAccess, path);

                try
                {
                    fileIOPermission.Demand();
                    hasPermissions = true;
                }
                catch (SecurityException) { /* The caller does not have the appropriate permissions. */ }
            }
            catch (ArgumentException exc) /* The path parameter does not specify the absolute path to the file or directory. */
            {
                ExceptionLayer.Handle(exc);
                throw;
            }

            return hasPermissions;
        }

        /// <summary>
        /// <para>Checks is the caller of the function has all-permissions on a path.</para>
        /// <para>All-access includes append, read, write, and path discovery.</para>
        /// </summary>
        /// <param name="path">The path to the directory or file to check permissions.</param>
        /// <returns>Returns a bool whether the caller has all-permissions permissions on the path.</returns>
        /// <exception cref="ArgumentNullException">The path is null.</exception>
        /// <exception cref="ArgumentException">The path parameter does not specify the absolute path to the file or directory.</exception>
        public static bool HasAllAccessPermissions(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) { throw new ArgumentNullException(nameof(path)); }

            return HasPermissions(FileIOPermissionAccess.AllAccess, path);
        }

        /// <summary>
        /// Checks if the caller of the function has append permissions on a path.
        /// </summary>
        /// <param name="fileIOPermissionAccess">The type of file access.</param>
        /// <param name="path">The path to the directory or file to check permissions.</param>
        /// <returns>Returns a bool whether the caller has append permissions on the path.</returns>
        /// <exception cref="ArgumentNullException">The path is null.</exception>
        /// <exception cref="ArgumentException">The path parameter does not specify the absolute path to the file or directory.</exception>
        public static bool HasAppendPermissions(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) { throw new ArgumentNullException(nameof(path)); }

            return HasPermissions(FileIOPermissionAccess.Append, path);
        }

        /// <summary>
        /// Checks if the caller of the function has no access permissions on a path.
        /// </summary>
        /// <param name="fileIOPermissionAccess">The type of file access.</param>
        /// <param name="path">The path to the directory or file to check permissions.</param>
        /// <returns>Returns a bool whether the caller has no access permissions on the path.</returns>
        /// <exception cref="ArgumentNullException">The path is null.</exception>
        /// <exception cref="ArgumentException">The path parameter does not specify the absolute path to the file or directory.</exception>
        public static bool HasNoAccessPermissions(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) { throw new ArgumentNullException(nameof(path)); }

            return HasPermissions(FileIOPermissionAccess.NoAccess, path);
        }

        /// <summary>
        /// Checks if the caller of the function has path discovery permissions on a path.
        /// </summary>
        /// <param name="fileIOPermissionAccess">The type of file access.</param>
        /// <param name="path">The path to the directory or file to check permissions.</param>
        /// <returns>Returns a bool whether the caller has path discovery permissions on the path.</returns>
        /// <exception cref="ArgumentNullException">The path is null.</exception>
        /// <exception cref="ArgumentException">The path parameter does not specify the absolute path to the file or directory.</exception>
        public static bool HasPathDiscoveryAccess(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) { throw new ArgumentNullException(nameof(path)); }

            return HasPermissions(FileIOPermissionAccess.PathDiscovery, path);
        }

        /// <summary>
        /// Checks if the caller of the function has read permissions on a path.
        /// </summary>
        /// <param name="fileIOPermissionAccess">The type of file access.</param>
        /// <param name="path">The path to the directory or file to check permissions.</param>
        /// <returns>Returns a bool whether the caller has read permissions on the path.</returns>
        /// <exception cref="ArgumentNullException">The path is null.</exception>
        /// <exception cref="ArgumentException">The path parameter does not specify the absolute path to the file or directory.</exception>
        public static bool HasReadPermissions(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) { throw new ArgumentNullException(nameof(path)); }

            return HasPermissions(FileIOPermissionAccess.Read, path);
        }

        /// <summary>
        /// Checks if the caller of the function has write permissions on a path.
        /// </summary>
        /// <param name="fileIOPermissionAccess">The type of file access.</param>
        /// <param name="path">The path to the directory or file to check permissions.</param>
        /// <returns>Returns a bool whether the caller has write permissions on the path.</returns>
        /// <exception cref="ArgumentNullException">The path is null.</exception>
        /// <exception cref="ArgumentException">The path parameter does not specify the absolute path to the file or directory.</exception>
        public static bool HasWritePermissions(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) { throw new ArgumentNullException(nameof(path)); }

            return HasPermissions(FileIOPermissionAccess.Append, path);
        }
        #endregion

        #region Validation
        #region DateTime
        /// <summary>
        /// Checks if the passed-in DateTime is a valid date and time in the future.
        /// </summary>
        /// <param name="givenDateTime">The DateTime to check against the current time.</param>
        /// <param name="includeToday">The Boolean for if tomorrow is the earliest valid DateTime.</param>
        /// <returns>Returns true if the passed-in DateTime is in the future.</returns>
        public static bool IsValidFutureDate(DateTime givenDateTime, bool includeToday = false)
        {
            long theTicks = givenDateTime.Ticks;

            return includeToday ? theTicks >= DateTime.Now.Ticks : theTicks >= DateTime.Today.AddDays(1).Ticks;
        }

        /// <summary>
        /// Checks if the passed-in DateTime is a valid date and time in the future.
        /// </summary>
        /// <param name="givenDateTime">The string representation of a DateTime to check against the current time.</param>
        /// <param name="includeToday">The Boolean for if tomorrow is the earliest valid DateTime.</param>
        /// <returns>Returns true if the passed-in DateTime is in the future.</returns>
        public static bool IsValidFutureDate(string givenDateTime, bool includeToday = false)
        {
            DateTime theDateTime = DateTime.TryParse(givenDateTime, out theDateTime) ? theDateTime : default;
            
            return IsValidFutureDate(theDateTime, includeToday);
        }

        /// <summary>
        /// Checks if the passed-in DateTime is a valid date and time in the past.
        /// </summary>
        /// <param name="givenDateTime">The DateTime to check against the current time.</param>
        /// <param name="includeToday">The Boolean for if the current date and time is the latest valid DateTime.</param>
        /// <returns>Returns true if the passed-in DateTime is in the past.</returns>
        public static bool IsValidPastDate(DateTime givenDateTime, bool includeToday = false)
        {
            long theTicks = givenDateTime.Ticks;

            return includeToday ? theTicks <= DateTime.Now.Ticks : theTicks < DateTime.Today.Ticks;
        }

        /// <summary>
        /// Checks if the passed-in DateTime is a valid date and time in the past.
        /// </summary>
        /// <param name="givenDateTime">The string representation of a DateTime to check against the current time.</param>
        /// <param name="includeToday">The Boolean for if the current date and time is the latest valid DateTime.</param>
        /// <returns>Returns true if the passed-in DateTime is in the past.</returns>
        public static bool IsValidPastDate(string givenDateTime, bool includeToday = false)
        {
            DateTime theDateTime = DateTime.TryParse(givenDateTime, out theDateTime) ? theDateTime : default;
            
            return IsValidPastDate(theDateTime, includeToday);
        }
        #endregion
        #endregion
    }
}
