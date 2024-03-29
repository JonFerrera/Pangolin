﻿using Pangolin.Properties;
using System;
using System.ComponentModel;
using System.Configuration;
using System.Security;
using System.Text;

namespace Pangolin
{
    public static class ConfigurationLayer
    {
        #region Private Variables
        private const char _comma = ',';
        private const char _newLine = '\n';
        private const char _returnCarriage = '\r';
        private const char _semicolon = ';';
        private const char _space = ' ';
        private const char _tab = '\t';
        private const int _tabLength = 4;
        #endregion

        #region Variables
        public const string DateTimeHumanFormat = "dddd, MMMM dd yyyy HH:mm";
        public const string DateTimeLongFormat = "yyyy-MM-dd HH:mm:ss.fff";
        public const string DateTimeLongFormatFile = "yyyyMMddHHmmssfff";
        public const string DateTimeShortFormat = "yyyy-MM-dd";
        public const string DateTimeShortFormatFile = "yyyyMMdd";
        public const string EmptyJson = "{}";
        public const string NullTerminator = "\0";
        
        public static char[] EmailSplit
        {
            get
            {
                return new char[] { _comma, _semicolon };
            }
        }
        public static char[] FieldSplit
        {
            get
            {
                return new char[] { _tab, _comma };
            }
        }
        public static char[] LineSplit
        {
            get
            {
                return new char[] { _newLine, _returnCarriage };
            }
        }
        public static char[] WhitespaceSplit
        {
            get
            {
                return new char[0];
            }
        }

        public static DateTime UnixEpoch
        {
            get
            {
                return new DateTime(1970, 1, 1, 0, 0, 0, 0);
            }
        }

        public static string NewLine
        {
            get
            {
                return Environment.NewLine;
            }
        }
        public static string Tab
        {
            get
            {
                return new string(_space, _tabLength);
            }
        }

        public static Encoding DefaultEncoding
        {
            get
            {
                return Encoding.UTF8;
            }
        }
        #endregion

        #region Expression-bodied Members

        #endregion

        /// <summary>
        /// Gets the connection string for the key name provided.
        /// </summary>
        /// <param name="name">The key for the connection string value in the config file.</param>
        /// <returns>Returns the connection string.</returns>
        /// <exception cref="ArgumentNullException">The name is null.</exception>
        /// <exception cref="ArgumentException">The name is empty or consists of only white-space characters.</exception>
        /// <exception cref="ConfigurationErrorsException">Could not retrieve a ConnectionStringSettingsCollection object.</exception>
        public static string GetConnectionString(string name)
        {
            if (name == null) { throw new ArgumentNullException(nameof(name)); }
            else if (string.Equals(name, string.Empty)) { throw new ArgumentException($"{nameof(name)} cannot be an empty string.", nameof(name)); }
            else if (string.Equals(name.Trim(), string.Empty)) { throw new ArgumentException($"{nameof(name)} cannot be a white-space string.", nameof(name)); }

            string _value = null;
            try
            {
                _value = !string.IsNullOrWhiteSpace(name) ? ConfigurationManager.ConnectionStrings[name].ConnectionString : null;
            }
            catch (ConfigurationErrorsException exc) { ExceptionLayer.CoreHandle(exc); throw; }

            return _value ?? string.Empty;
        }

        /// <summary>
        /// Gets the environment variable for the key provided.
        /// </summary>
        /// <param name="key">The key for the environment variable.</param>
        /// <returns>Returns the environment variable.</returns>
        /// <exception cref="ArgumentNullException">The key is null.</exception>
        /// <exception cref="ArgumentException">The key is empty or consists of only white-space characters.</exception>
        /// <exception cref="SecurityException">The caller does not have permission to get the environment variable.</exception>
        public static string GetEnvironmentVariable(string key)
        {
            if (key == null) { throw new ArgumentNullException(nameof(key)); }
            else if (string.Equals(key, string.Empty)) { throw new ArgumentException($"{nameof(key)} cannot be an empty string.", nameof(key)); }
            else if (string.Equals(key.Trim(), string.Empty)) { throw new ArgumentException($"{nameof(key)} cannot be a white-space string.", nameof(key)); }

            string _value = null;
            try
            {
                _value = !string.IsNullOrWhiteSpace(key) ? Environment.GetEnvironmentVariable(key) : null;
            }
            catch (SecurityException exc) { ExceptionLayer.CoreHandle(exc); throw; }

            return _value ?? string.Empty;
        }

        /// <summary>
        /// Gets the value for the key provided.
        /// </summary>
        /// <param name="key">The key for the configuration variable.</param>
        /// <returns>Returns the value.</returns>
        /// <exception cref="ArgumentNullException">The key is null.</exception>
        /// <exception cref="ArgumentException">The key is empty or consists of only white-space characters.</exception>
        /// <exception cref="ConfigurationErrorsException">Could not retrieve a NameValueCollection object with the application settings data.</exception>
        /// <exception cref="InvalidOperationException">The configuration has no non-null keys.</exception>
        private static string GetValue(string key)
        {
            if (key == null) { throw new ArgumentNullException(nameof(key)); }
            else if (string.Equals(key, string.Empty)) { throw new ArgumentException($"{nameof(key)} cannot be an empty string.", nameof(key)); }
            else if (string.Equals(key.Trim(), string.Empty)) { throw new ArgumentException($"{nameof(key)} cannot be a white-space string.", nameof(key)); }
            if (!ConfigurationManager.AppSettings.HasKeys()) { throw new InvalidOperationException($"{ConfigurationManager.AppSettings} has no non-null keys."); }

            string _value = null;
            try
            {
                _value = ConfigurationManager.AppSettings[key];
            }
            catch (ConfigurationErrorsException exc) { ExceptionLayer.CoreHandle(exc); throw; }

            return _value ?? string.Empty;
        }

        #region Environment Folder
        public static string GetProgramFilesFolder()
        {
            return GetFolder(Environment.SpecialFolder.ProgramFiles);
        }

        public static string GetProgramFilesX86Folder()
        {
            return GetFolder(Environment.SpecialFolder.ProgramFilesX86);
        }

        public static string GetWindowsFolder()
        {
            return GetFolder(Environment.SpecialFolder.Windows);
        }

        private static string GetFolder(Environment.SpecialFolder specialFolder)
        {
            try
            {
                return Environment.GetFolderPath(specialFolder);
            }
            catch (PlatformNotSupportedException exc) { ExceptionLayer.CoreHandle(exc); throw; }
        }
        #endregion

        #region App/Web Config
        public static T GetConfigValue<T>(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) { throw new ArgumentNullException(nameof(key)); }

            string _value = GetValue(key);
            T _valueT = default;
            
            try
            {
                switch (_valueT)
                {
                    case int _valueInt:
                        _valueT = (T)(object)int.Parse(_value);
                        break;
                    case uint _valueUint:
                        _valueT = (T)(object)uint.Parse(_value);
                        break;
                    case sbyte _valueSbyte:
                        _valueT = (T)(object)sbyte.Parse(_value);
                        break;
                    case short _valueShort:
                        _valueT = (T)(object)short.Parse(_value);
                        break;
                    case ushort _valueUshort:
                        _valueT = (T)(object)ushort.Parse(_value);
                        break;
                    case long _valueLong:
                        _valueT = (T)(object)long.Parse(_value);
                        break;
                    case ulong _valueUlong:
                        _valueT = (T)(object)ulong.Parse(_value);
                        break;
                    case double _valueDouble:
                        _valueT = (T)(object)double.Parse(_value);
                        break;
                    case float _valueFloat:
                        _valueT = (T)(object)float.Parse(_value);
                        break;
                    case decimal _valueDecimal:
                        _valueT = (T)(object)decimal.Parse(_value);
                        break;
                    case char _valueChar:
                        _valueT = (T)(object)char.Parse(_value);
                        break;
                    case bool _valueBool:
                        _valueT = (T)(object)bool.Parse(_value);
                        break;
                    case byte _valueByte:
                        _valueT = (T)(object)byte.Parse(_value);
                        break;
                    case DateTime _valueDateTime:
                        _valueT = (T)(object)DateTime.Parse(_value);
                        break;
                    case TimeSpan _valueTimeSpan:
                        _valueT = (T)(object)TimeSpan.Parse(_value);
                        break;
                    case Guid _valueGuid:
                        _valueT = (T)(object)Guid.Parse(_value);
                        break;
                    default:
                        TypeConverter typeConverter = TypeDescriptor.GetConverter(typeof(T));
                        try
                        {
                            _valueT = (T)typeConverter?.ConvertFromString(_value);
                        }
                        catch (NotSupportedException exc){ ExceptionLayer.CoreHandle(exc); throw; }
                        catch (Exception exc){ ExceptionLayer.CoreHandle(exc); throw; }
                        break;
                }
            }
            catch (ArgumentNullException exc){ ExceptionLayer.CoreHandle(exc); throw; }
            catch (FormatException exc){ ExceptionLayer.CoreHandle(exc); throw; }
            catch (OverflowException exc){ ExceptionLayer.CoreHandle(exc); throw; }

            return _valueT;
        }
        #endregion

        #region Properties
        public static int AsyncTimeOut
        {
            get
            {
                 return Settings.Default.AsyncTimeOut;
            }
        }

        public static int CommandTimeOut
        {
            get
            {
                return Settings.Default.CommandTimeOut;
            }
        }

        public static string DeveloperMailFrom
        {
            get
            {
                return Settings.Default.DeveloperMailFrom;
            }
        }
        public static string[] DeveloperMailTo
        {
            get
            {
                return Settings.Default.DeveloperMailTo.Split(EmailSplit, StringSplitOptions.RemoveEmptyEntries);
            }
        }
        public static string[] DeveloperMailCC
        {
            get
            {
                return Settings.Default.DeveloperMailCC.Split(EmailSplit, StringSplitOptions.RemoveEmptyEntries);
            }
        }
        public static string[] DeveloperMailBCC
        {
            get
            {
                return Settings.Default.DeveloperMailBCC.Split(EmailSplit, StringSplitOptions.RemoveEmptyEntries);
            }
        }

        public static string EventLogSource
        {
            get
            {
                return Settings.Default.EventLogSource;
            }
        }
        public static string EventLogName
        {
            get
            {
                return Settings.Default.EventLogName;
            }
        }
        public static int EventLogMaxSize
        {
            get
            {
                return Settings.Default.EventLogMaxSize;
            }
        }
        public static int EventLogMaxAge
        {
            get
            {
                return Settings.Default.EventLogMaxAge;
            }
        }

        public static string UserAgentImpersonate
        {
            get
            {
                return Settings.Default.UserAgentImpersonate;
            }
        }
        #endregion
    }
}