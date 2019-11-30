using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Common;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI;
using System.Xml;

namespace Pangolin
{
    public static class ExceptionLayer
    {
        private const char
            _dash = '-',
            _space = ' ';

        private const int
            _lineBreakSpace = 80,
            _padRightSpace = 32;

        private static readonly string
            _indentation = new string(_space, _padRightSpace),
            _lineBreak = new string(_dash, _lineBreakSpace);

        public static string Handle(Exception exception)
        {
            string exceptionText = WriteExceptionText(exception);
            DiagnosticsLayer.EventLogWriteError(exceptionText);

            try
            {
                Console.WriteLine(exceptionText);
            }
            catch (IOException) { }

            return exceptionText;
        }

        internal static async Task CoreHandleAsync(Exception exception)
        {
            string
                exceptionText = Handle(exception),
                body = WriteExceptionHtml(exception);

            Task
                logExceptionTask = FileLayer.LogExceptionCoreAsync(exceptionText),
                coreMailTask = MailLayer.SendCoreMailAsync($"{nameof(ExceptionLayer)} Exception - {exception.GetType().Name}", body);

            await Task.WhenAll(logExceptionTask, coreMailTask);
        }

        internal static async Task CoreHandleAsync(DbException exception, string query)
        {
            exception = AddCustomData(exception, query);

            await CoreHandleAsync(exception);
        }

        internal static async Task CoreHandleAsync(DbException exception, string query, DbParameter[] dbParameters)
        {
            exception = AddCustomData(exception, query);
            exception = AddCustomData(exception, dbParameters);

            await CoreHandleAsync(exception);
        }

        public static async Task HandleAsync(Exception exception)
        {
            string
                exceptionText = Handle(exception),
                body = WriteExceptionHtml(exception);

            Task
                logExceptionTask = FileLayer.LogExceptionAsync(exceptionText),
                coreMailTask = MailLayer.SendCoreMailAsync($"{nameof(ExceptionLayer)} Exception - {exception.GetType().Name}", body);

            await Task.WhenAll(logExceptionTask, coreMailTask);
        }

        public static async Task HandleAsync(DbException exception, string query)
        {
            exception = AddCustomData(exception, query);

            await HandleAsync(exception);
        }

        public static async Task HandleAsync(DbException exception, string query, DbParameter[] dbParameters)
        {
            exception = AddCustomData(exception, query);
            exception = AddCustomData(exception, dbParameters);

            await HandleAsync(exception);
        }
        
        private static (string type, DateTime timestamp, string message, int hresult, string[] source, string[] targetSite, string[] stackTrace, IDictionary data, Dictionary<string, object> specificData, string[] helpLink, Exception[] innerExceptions) RenderException(Exception exception)
        {
            string type = exception.GetType().Name;
            DateTime timestamp = DateTime.Now;
            string message = exception.Message;
            int hresult = exception.HResult;

            string[] source = null;
            if (!string.IsNullOrWhiteSpace(exception.Source))
            {
                source = exception.Source.Split(ConfigurationLayer.LineSplit, StringSplitOptions.RemoveEmptyEntries).Select(i => i.Trim()).ToArray();
            }

            string[] targetSite = null;
            if (exception.TargetSite != null)
            {
                targetSite = exception.TargetSite.ToString().Split(ConfigurationLayer.LineSplit, StringSplitOptions.RemoveEmptyEntries).Select(i => i.Trim()).ToArray();
            }

            string[] stackTrace = null;
            if (!string.IsNullOrWhiteSpace(exception.StackTrace))
            {
                stackTrace = exception.StackTrace.Split(ConfigurationLayer.LineSplit, StringSplitOptions.RemoveEmptyEntries).Select(i => i.Trim()).ToArray();
            }

            IDictionary data = null;
            if (exception.Data?.Count > 0)
            {
                data = exception.Data;
            }

            Dictionary<string, object> specificData = ExceptionSpecificData(exception);

            string[] helpLink = null;
            if (!string.IsNullOrWhiteSpace(exception.HelpLink))
            {
                helpLink = exception.HelpLink.Split(ConfigurationLayer.LineSplit, StringSplitOptions.RemoveEmptyEntries).Select(i => i.Trim()).ToArray();
            }

            List<Exception> innerExceptionList = new List<Exception>();
            while (exception.InnerException != null)
            {
                innerExceptionList.Add(exception.InnerException);
                exception = exception.InnerException;
            }
            Exception[] innerExceptions = innerExceptionList.ToArray();

            return (type, timestamp, message, hresult, source, targetSite, stackTrace, data, specificData, helpLink, innerExceptions);
        }

        #region HRESULT
        public static (bool severity, bool reserved, bool customer, bool n, bool x, int facility, int code) ParseHresult(int hResult)
        {
            byte[] bytes = BitConverter.GetBytes(hResult);
            BitArray bitArray = new BitArray(bytes);

            bool severity = bitArray.Get(0);
            bool reserved = bitArray.Get(1);
            bool customer = bitArray.Get(2);
            bool n = bitArray.Get(3);
            bool x = bitArray.Get(4);

            bool[] facilityBytes = new bool[]
            {
                bitArray.Get(5),
                bitArray.Get(6),
                bitArray.Get(7),
                bitArray.Get(8),
                bitArray.Get(9),
                bitArray.Get(10),
                bitArray.Get(11),
                bitArray.Get(12),
                bitArray.Get(13),
                bitArray.Get(14),
                bitArray.Get(15)
            };

            StringBuilder facilityBinary = new StringBuilder(11);
            for (int i = 0; i < facilityBytes.Length; i++)
            {
                if (facilityBytes[i])
                {
                    facilityBinary.Append('1');
                }
                else
                {
                    facilityBinary.Append('0');
                }
            }
            int facility = Convert.ToInt32(facilityBinary.ToString(), 2);

            bool[] codeBytes = new bool[]
            {
                bitArray.Get(16),
                bitArray.Get(17),
                bitArray.Get(18),
                bitArray.Get(19),
                bitArray.Get(20),
                bitArray.Get(21),
                bitArray.Get(22),
                bitArray.Get(23),
                bitArray.Get(24),
                bitArray.Get(25),
                bitArray.Get(26),
                bitArray.Get(27),
                bitArray.Get(28),
                bitArray.Get(29),
                bitArray.Get(30),
                bitArray.Get(31)
            };

            StringBuilder codeBinary = new StringBuilder(16);
            for (int i = 0; i < codeBytes.Length; i++)
            {
                if (codeBytes[i])
                {
                    codeBinary.Append('1');
                }
                else
                {
                    codeBinary.Append('0');
                }
            }
            int code = Convert.ToInt32(codeBinary.ToString(), 2);

            return (severity, reserved, customer, n, x, facility, code);
        }
        #endregion

        #region Format Exception - Text
        private static string WriteExceptionText(Exception exception)
        {
            (string type, DateTime timestamp, string message, int hresult, string[] source, string[] targetSite, string[] stackTrace, IDictionary data, Dictionary<string, object> specificData, string[] helpLink, Exception[] innerExceptions) = RenderException(exception);

            return WriteExceptionText(type, timestamp, message, hresult, source, targetSite, stackTrace, data, specificData, helpLink, innerExceptions); ;
        }

        private static string WriteExceptionText(string type, DateTime timestamp, string message, int hresult, string[] source, string[] targetSite, string[] stackTrace, IDictionary data = null, Dictionary<string, object> specificData = null, string[] helpLink = null, Exception[] innerExceptions = null)
        {
            StringBuilder exceptionText = new StringBuilder(1024);

            try
            {
                exceptionText.AppendLine("Exception Type:".PadRight(_padRightSpace) + type);
                exceptionText.AppendLine("Exception Timestamp:".PadRight(_padRightSpace) + timestamp.ToString(ConfigurationLayer.DateTimeLongFormat));

                exceptionText.AppendLine("Message:".PadRight(_padRightSpace) + message);

                if (data?.Count > 0)
                {
                    exceptionText.AppendLine("Data:");
                    foreach (DictionaryEntry entry in data)
                    {
                        if (string.Equals(entry.Key.ToString(), "Parameters") && entry.Value is DbParameter[] dbParameters)
                        {
                            exceptionText.AppendLine(ConfigurationLayer.Tab + "Parameters");
                            if (dbParameters?.Length > 0)
                            {
                                foreach (DbParameter dbParameter in dbParameters)
                                {
                                    exceptionText.AppendLine(_indentation + dbParameter.ParameterName);
                                    exceptionText.AppendLine(_indentation + ConfigurationLayer.Tab + dbParameter.Value);
                                }
                            }
                        }
                        else
                        {
                            exceptionText.AppendLine((ConfigurationLayer.Tab + entry.Key.ToString()).PadRight(_padRightSpace) + entry.Value.ToString());
                        }
                    }
                }

                if (specificData?.Count > 0)
                {
                    exceptionText.AppendLine("Specific Data:");
                    foreach (KeyValuePair<string, object> specificDatum in specificData)
                    {
                        exceptionText.AppendLine(_indentation + specificDatum.Key);
                        string datumValue = specificDatum.Value != null ? specificDatum.Value.ToString() : string.Empty;
                        exceptionText.AppendLine(_indentation + ConfigurationLayer.Tab + datumValue);
                    }
                }

                (bool severity, bool reserved, bool customer, bool n, bool x, int facility, int code) = ParseHresult(hresult);
                exceptionText.AppendLine("HRESULT:".PadRight(_padRightSpace) + "0x" + hresult.ToString("X8"));
                exceptionText.AppendLine((ConfigurationLayer.Tab + "Severity:").PadRight(_padRightSpace) + severity);
                exceptionText.AppendLine((ConfigurationLayer.Tab + "Reserved:").PadRight(_padRightSpace) + reserved);
                exceptionText.AppendLine((ConfigurationLayer.Tab + "Customer:").PadRight(_padRightSpace) + customer);
                exceptionText.AppendLine((ConfigurationLayer.Tab + "n:").PadRight(_padRightSpace) + n);
                exceptionText.AppendLine((ConfigurationLayer.Tab + "x:").PadRight(_padRightSpace) + x);
                exceptionText.AppendLine((ConfigurationLayer.Tab + "Facility:").PadRight(_padRightSpace) + facility);
                exceptionText.AppendLine((ConfigurationLayer.Tab + "Code:").PadRight(_padRightSpace) + code);

                if (source?.Length > 0)
                {
                    exceptionText.AppendLine("Source:");
                    for (int i = 0; i < source.Length; i++)
                    {
                        exceptionText.AppendLine(_indentation + source[i]);
                    }
                }

                if (targetSite?.Length > 0)
                {
                    exceptionText.AppendLine("Target Site:");
                    for (int i = 0; i < targetSite.Length; i++)
                    {
                        exceptionText.AppendLine(_indentation + targetSite[i]);
                    }
                }

                if (stackTrace?.Length > 0)
                {
                    exceptionText.AppendLine("Stack Trace:");
                    for (int i = 0; i < stackTrace.Length; i++)
                    {
                        exceptionText.AppendLine(_indentation + stackTrace[i]);
                    }
                }

                if (helpLink?.Length > 0)
                {
                    exceptionText.AppendLine("Help Link:");
                    for (int i = 0; i < helpLink.Length; i++)
                    {
                        exceptionText.AppendLine(_indentation + helpLink[i]);
                    }
                }

                if (innerExceptions?.Length > 0)
                {
                    exceptionText.AppendLine("Inner Exceptions:");
                    int level = 1;
                    foreach (Exception innerException in innerExceptions)
                    {
                        exceptionText.AppendLine($"{_indentation}#{level.ToString()}: {innerException.GetType().Name} - {innerException.Message}");
                        level++;
                    }
                }
            }
            catch (ArgumentOutOfRangeException) { }

            return exceptionText.ToString();
        }
        #endregion

        #region Format Exception - Html
        private static string WriteExceptionHtml(Exception exception)
        {
            (string type, DateTime timestamp, string message, int hresult, string[] source, string[] targetSite, string[] stackTrace, IDictionary data, Dictionary<string, object> specificData, string[] helpLink, Exception[] innerExceptions) = RenderException(exception);

            return WriteExceptionHtml(type, timestamp, message, hresult, source, targetSite, stackTrace, data, specificData, helpLink, innerExceptions);
        }

        private static string WriteExceptionHtml(string type, DateTime timestamp, string message, int hresult, string[] source, string[] targetSite, string[] stackTrace, IDictionary data = null, Dictionary<string, object> specificData = null, string[] helpLink = null, Exception[] innerExceptions = null)
        {
            string title = type;
            string body = string.Empty;

            using (StringWriter stringWriter = new StringWriter())
            {
                using (HtmlTextWriter htmlTextWriter = new HtmlTextWriter(stringWriter, ConfigurationLayer.Tab))
                {
                    htmlTextWriter.WriteFullBeginTag("h1");
                    htmlTextWriter.Write(type);
                    htmlTextWriter.WriteEndTag("h1");

                    htmlTextWriter.WriteFullBeginTag("p");
                    htmlTextWriter.WriteBeginTag("time");
                    htmlTextWriter.WriteAttribute("datetime", timestamp.ToString(ConfigurationLayer.DateTimeLongFormat));
                    htmlTextWriter.Write(HtmlTextWriter.TagRightChar);
                    htmlTextWriter.Write(timestamp.ToString(ConfigurationLayer.DateTimeHumanFormat));
                    htmlTextWriter.WriteEndTag("time");
                    htmlTextWriter.WriteEndTag("p");

                    htmlTextWriter.WriteBeginTag("details");
                    htmlTextWriter.WriteAttribute("open", null);
                    htmlTextWriter.Write(HtmlTextWriter.TagRightChar);
                    htmlTextWriter.WriteFullBeginTag("summary");
                    htmlTextWriter.WriteFullBeginTag("strong");
                    htmlTextWriter.Write("Message");
                    htmlTextWriter.WriteEndTag("strong");
                    htmlTextWriter.WriteEndTag("summary");
                    htmlTextWriter.WriteFullBeginTag("p");
                    htmlTextWriter.Write(message);
                    htmlTextWriter.WriteEndTag("p");
                    htmlTextWriter.WriteEndTag("details");

                    if (data?.Count > 0)
                    {
                        htmlTextWriter.WriteBeginTag("details");
                        htmlTextWriter.WriteAttribute("open", null);
                        htmlTextWriter.Write(HtmlTextWriter.TagRightChar);
                        htmlTextWriter.WriteFullBeginTag("summary");
                        htmlTextWriter.WriteFullBeginTag("strong");
                        htmlTextWriter.Write("Data");
                        htmlTextWriter.WriteEndTag("strong");
                        htmlTextWriter.WriteEndTag("summary");

                        foreach (DictionaryEntry entry in data)
                        {
                            if (entry.Key != null && entry.Value != null)
                            {
                                if (string.Equals(entry.Key.ToString(), "Parameters") && entry.Value is DbParameter[] dbParameters)
                                {
                                    htmlTextWriter.WriteFullBeginTag("strong");
                                    htmlTextWriter.Write("Parameters");
                                    htmlTextWriter.WriteEndTag("strong");

                                    if (dbParameters?.Length > 0)
                                    {
                                        htmlTextWriter.WriteFullBeginTag("ul");
                                        foreach (DbParameter dbParameter in dbParameters)
                                        {
                                            htmlTextWriter.WriteFullBeginTag("li");
                                            htmlTextWriter.WriteFullBeginTag("code");

                                            string parameterValue = dbParameter.Value != null ? dbParameter.Value.ToString() : string.Empty;
                                            htmlTextWriter.Write($"{dbParameter.ParameterName} - {parameterValue}");

                                            htmlTextWriter.WriteEndTag("code");
                                            htmlTextWriter.WriteEndTag("li");
                                        }
                                        htmlTextWriter.WriteEndTag("ul");
                                    }
                                }
                                else
                                {
                                    htmlTextWriter.WriteFullBeginTag("strong");
                                    htmlTextWriter.Write("Query");
                                    htmlTextWriter.WriteEndTag("strong");

                                    htmlTextWriter.WriteFullBeginTag("ul");
                                    htmlTextWriter.WriteFullBeginTag("li");
                                    htmlTextWriter.WriteFullBeginTag("code");
                                    htmlTextWriter.Write(entry.Value.ToString());
                                    htmlTextWriter.WriteEndTag("code");
                                    htmlTextWriter.WriteEndTag("li");
                                    htmlTextWriter.WriteEndTag("ul");
                                }
                            }
                        }

                        htmlTextWriter.WriteEndTag("details");
                    }

                    if (specificData?.Count > 0)
                    {
                        htmlTextWriter.WriteBeginTag("details");
                        htmlTextWriter.WriteAttribute("open", null);
                        htmlTextWriter.Write(HtmlTextWriter.TagRightChar);
                        htmlTextWriter.WriteFullBeginTag("summary");
                        htmlTextWriter.WriteFullBeginTag("strong");
                        htmlTextWriter.Write("Specific Data");
                        htmlTextWriter.WriteEndTag("strong");
                        htmlTextWriter.WriteEndTag("summary");

                        htmlTextWriter.WriteFullBeginTag("ul");
                        foreach (KeyValuePair<string, object> specificDatum in specificData)
                        {
                            htmlTextWriter.WriteFullBeginTag("li");
                            htmlTextWriter.WriteFullBeginTag("code");

                            string datumValue = specificDatum.Value != null ? specificDatum.Value.ToString() : string.Empty;
                            htmlTextWriter.Write($"{specificDatum.Key} - {datumValue}");

                            htmlTextWriter.WriteEndTag("code");
                            htmlTextWriter.WriteEndTag("li");
                        }
                        htmlTextWriter.WriteEndTag("ul");

                        htmlTextWriter.WriteEndTag("details");
                    }

                    htmlTextWriter.WriteFullBeginTag("details");
                    htmlTextWriter.WriteFullBeginTag("summary");
                    htmlTextWriter.WriteFullBeginTag("strong");
                    htmlTextWriter.Write("HRESULT");
                    htmlTextWriter.WriteEndTag("strong");
                    htmlTextWriter.WriteEndTag("summary");
                    htmlTextWriter.WriteFullBeginTag("code");
                    htmlTextWriter.Write("0x" + hresult.ToString("X8"));
                    htmlTextWriter.WriteEndTag("code");
                    htmlTextWriter.WriteEndTag("details");

                    if (source?.Length > 0)
                    {
                        htmlTextWriter.WriteBeginTag("details");
                        htmlTextWriter.WriteAttribute("open", null);
                        htmlTextWriter.Write(HtmlTextWriter.TagRightChar);
                        htmlTextWriter.WriteFullBeginTag("summary");
                        htmlTextWriter.WriteFullBeginTag("strong");
                        htmlTextWriter.Write("Source");
                        htmlTextWriter.WriteEndTag("strong");
                        htmlTextWriter.WriteEndTag("summary");

                        for (int i = 0; i < source.Length; i++)
                        {
                            htmlTextWriter.WriteFullBeginTag("code");
                            htmlTextWriter.Write(source[i]);
                            htmlTextWriter.WriteEndTag("code");
                        }

                        htmlTextWriter.WriteEndTag("details");
                    }

                    if (targetSite?.Length > 0)
                    {
                        htmlTextWriter.WriteBeginTag("details");
                        htmlTextWriter.WriteAttribute("open", null);
                        htmlTextWriter.Write(HtmlTextWriter.TagRightChar);
                        htmlTextWriter.WriteFullBeginTag("summary");
                        htmlTextWriter.WriteFullBeginTag("strong");
                        htmlTextWriter.Write("Target Site");
                        htmlTextWriter.WriteEndTag("strong");
                        htmlTextWriter.WriteEndTag("summary");

                        for (int i = 0; i < targetSite.Length; i++)
                        {
                            htmlTextWriter.WriteFullBeginTag("code");
                            htmlTextWriter.Write(targetSite[i]);
                            htmlTextWriter.WriteEndTag("code");
                        }

                        htmlTextWriter.WriteEndTag("details");
                    }

                    if (stackTrace?.Length > 0)
                    {
                        htmlTextWriter.WriteBeginTag("details");
                        htmlTextWriter.WriteAttribute("open", null);
                        htmlTextWriter.Write(HtmlTextWriter.TagRightChar);
                        htmlTextWriter.WriteFullBeginTag("summary");
                        htmlTextWriter.WriteFullBeginTag("strong");
                        htmlTextWriter.Write("Stack Trace");
                        htmlTextWriter.WriteEndTag("strong");
                        htmlTextWriter.WriteEndTag("summary");

                        for (int i = 0; i < stackTrace.Length; i++)
                        {
                            htmlTextWriter.WriteFullBeginTag("code");
                            htmlTextWriter.Write(stackTrace[i]);
                            htmlTextWriter.WriteEndTag("code");
                        }

                        htmlTextWriter.WriteEndTag("details");
                    }

                    if (helpLink?.Length > 0)
                    {
                        htmlTextWriter.WriteBeginTag("details");
                        htmlTextWriter.WriteAttribute("open", null);
                        htmlTextWriter.Write(HtmlTextWriter.TagRightChar);
                        htmlTextWriter.WriteFullBeginTag("summary");
                        htmlTextWriter.WriteFullBeginTag("strong");
                        htmlTextWriter.Write("Stack Trace");
                        htmlTextWriter.WriteEndTag("strong");
                        htmlTextWriter.WriteEndTag("summary");

                        for (int i = 0; i < helpLink.Length; i++)
                        {
                            htmlTextWriter.WriteFullBeginTag("code");
                            htmlTextWriter.Write(helpLink[i]);
                            htmlTextWriter.WriteEndTag("code");
                        }

                        htmlTextWriter.WriteEndTag("details");
                    }

                    if (innerExceptions?.Length > 0)
                    {
                        htmlTextWriter.WriteBeginTag("details");
                        htmlTextWriter.WriteAttribute("open", null);
                        htmlTextWriter.Write(HtmlTextWriter.TagRightChar);
                        htmlTextWriter.WriteFullBeginTag("summary");
                        htmlTextWriter.WriteFullBeginTag("strong");
                        htmlTextWriter.Write("Inner Exceptions");
                        htmlTextWriter.WriteEndTag("strong");
                        htmlTextWriter.WriteEndTag("summary");

                        htmlTextWriter.WriteFullBeginTag("ol");
                        foreach (Exception innerException in innerExceptions)
                        {
                            htmlTextWriter.WriteFullBeginTag("li");
                            htmlTextWriter.Write($"{innerException.GetType().Name} - {innerException.Message}");
                            htmlTextWriter.WriteEndTag("li");
                        }
                        htmlTextWriter.WriteEndTag("ol");

                        htmlTextWriter.WriteEndTag("details");
                    }
                }

                body = stringWriter.ToString();
            }

            return TextLayer.HtmlWritePage(title, body);
        }
        #endregion

        #region Format Exception - XML
        private static string WriteExceptionXml(Exception exception)
        {
            (string type, DateTime timestamp, string message, int hresult, string[] source, string[] targetSite, string[] stackTrace, IDictionary data, Dictionary<string, object> specificData, string[] helpLink, Exception[] innerExceptions) = RenderException(exception);

            return WriteExceptionXml(type, timestamp, message, hresult, source, targetSite, stackTrace, data, specificData, helpLink, innerExceptions);
        }

        private static string WriteExceptionXml(string type, DateTime timestamp, string message, int hresult, string[] source, string[] targetSite, string[] stackTrace, IDictionary data = null, Dictionary<string, object> specificData = null, string[] helpLink = null, Exception[] innerExceptions = null)
        {
            StringBuilder exceptionText = new StringBuilder(1024);

            using (XmlWriter xmlWriter = XmlWriter.Create(exceptionText))
            {
                xmlWriter.WriteStartDocument();
                xmlWriter.WriteStartElement("exception");
                xmlWriter.WriteAttributeString("type", type);
                xmlWriter.WriteAttributeString("timestamp", timestamp.ToString(ConfigurationLayer.DateTimeLongFormat));
                xmlWriter.WriteAttributeString("HRESULT", "0x" + hresult.ToString("X8"));

                xmlWriter.WriteStartElement("message");
                xmlWriter.WriteString(message);
                xmlWriter.WriteEndElement();

                if (data?.Count > 0)
                {
                    xmlWriter.WriteStartElement("data");

                    foreach (DictionaryEntry entry in data)
                    {
                        xmlWriter.WriteStartElement("datum");
                        if (string.Equals(entry.Key.ToString(), "Parameters") && entry.Value is DbParameter[] dbParameters)
                        {
                            if (dbParameters?.Length > 0)
                            {
                                xmlWriter.WriteStartElement("parameters");
                                foreach (DbParameter dbParameter in dbParameters)
                                {
                                    xmlWriter.WriteStartElement("parameter");
                                    xmlWriter.WriteAttributeString("name", dbParameter.ParameterName);
                                    xmlWriter.WriteAttributeString("value", dbParameter.Value.ToString());
                                    xmlWriter.WriteEndElement();
                                }
                                xmlWriter.WriteEndElement();
                            }
                        }
                        else
                        {
                            xmlWriter.WriteStartElement(entry.Key.ToString());
                            xmlWriter.WriteAttributeString("value", entry.Value.ToString());
                            xmlWriter.WriteEndElement();
                        }
                        xmlWriter.WriteEndElement();
                    }

                    xmlWriter.WriteEndElement();
                }

                if (specificData?.Count > 0)
                {
                    xmlWriter.WriteStartElement("specificData");

                    foreach (KeyValuePair<string, object> specificDatum in specificData)
                    {
                        xmlWriter.WriteStartElement("specificDatum");
                        xmlWriter.WriteAttributeString("name", specificDatum.Key);
                        string datumValue = specificDatum.Value != null ? specificDatum.Value.ToString() : string.Empty;
                        xmlWriter.WriteAttributeString("value", datumValue);
                        xmlWriter.WriteEndElement();
                    }

                    xmlWriter.WriteEndElement();
                }

                if (source?.Length > 0)
                {
                    xmlWriter.WriteStartElement("sources");
                    for (int i = 0; i < source.Length; i++)
                    {
                        xmlWriter.WriteStartElement("source");
                        xmlWriter.WriteAttributeString("number", i.ToString());
                        xmlWriter.WriteString(source[i]);
                        xmlWriter.WriteEndElement();
                    }
                    xmlWriter.WriteEndElement();
                }

                if (targetSite?.Length > 0)
                {
                    xmlWriter.WriteStartElement("targetSites");
                    for (int i = 0; i < targetSite.Length; i++)
                    {
                        xmlWriter.WriteStartElement("targetSite");
                        xmlWriter.WriteAttributeString("number", i.ToString());
                        xmlWriter.WriteString(targetSite[i]);
                        xmlWriter.WriteEndElement();
                    }
                    xmlWriter.WriteEndElement();
                }

                if (stackTrace?.Length > 0)
                {
                    xmlWriter.WriteStartElement("stackTrace");
                    for (int i = 0; i < stackTrace.Length; i++)
                    {
                        xmlWriter.WriteStartElement("stackFrame");
                        xmlWriter.WriteAttributeString("number", i.ToString());
                        xmlWriter.WriteString(stackTrace[i]);
                        xmlWriter.WriteEndElement();
                    }
                    xmlWriter.WriteEndElement();
                }

                if (helpLink?.Length > 0)
                {
                    xmlWriter.WriteStartElement("helpLinks");
                    for (int i = 0; i < helpLink.Length; i++)
                    {
                        xmlWriter.WriteStartElement("helpLink");
                        xmlWriter.WriteAttributeString("number", i.ToString());
                        xmlWriter.WriteString(helpLink[i]);
                        xmlWriter.WriteEndElement();
                    }
                    xmlWriter.WriteEndElement();
                }

                if (innerExceptions?.Length > 0)
                {
                    xmlWriter.WriteStartElement("innerExceptions");
                    int level = 1;
                    foreach (Exception innerException in innerExceptions)
                    {
                        xmlWriter.WriteStartElement("innerException");
                        xmlWriter.WriteAttributeString("level", level.ToString());
                        xmlWriter.WriteAttributeString("name", innerException.GetType().Name);
                        xmlWriter.WriteAttributeString("message", innerException.Message);
                        xmlWriter.WriteEndElement();
                        level++;
                    }
                    xmlWriter.WriteEndElement();
                }

                xmlWriter.WriteEndElement();
                xmlWriter.WriteEndDocument();
            }

            return exceptionText.ToString();
        }
        #endregion

        #region Custom Exception Data
        private static DbException AddCustomData(DbException dbException, string query)
        {
            if (dbException == null)
            {
                throw new ArgumentNullException(nameof(dbException));
            }
            if (string.IsNullOrWhiteSpace(query))
            {
                throw new ArgumentNullException(nameof(query));
            }
            if (dbException.Data.IsReadOnly)
            {
                throw new InvalidOperationException($"The {nameof(dbException)}.{nameof(dbException.Data)} property is read-only.");
            }
            if (dbException.Data.Contains("Query"))
            {
                throw new InvalidOperationException("The 'Query' parameter has already been added.");
            }

            dbException.Data.Add("Query", query);

            return dbException;
        }

        private static DbException AddCustomData(DbException dbException, DbParameter[] dbParameters)
        {
            if (dbException == null)
            {
                throw new ArgumentNullException(nameof(dbException));
            }
            if (dbParameters == null)
            {
                throw new ArgumentNullException(nameof(dbParameters));
            }
            if (dbParameters.Length < 1)
            {
                throw new ArgumentException($"{nameof(dbParameters)} has no elements.");
            }
            if (dbException.Data.IsReadOnly)
            {
                throw new InvalidOperationException($"The {nameof(dbException)}.{nameof(dbException.Data)} property is read-only.");
            }
            if (dbException.Data.Contains("Parameters"))
            {
                throw new InvalidOperationException("The 'Parameters' parameter has already been added.");
            }

            dbException.Data.Add("Parameters", dbParameters);

            return dbException;
        }
        
        private static Dictionary<string, object> ExceptionSpecificData(Exception exception)
        {
            Dictionary<string, object> exceptionSpecificData = null;
            try
            {
                switch (exception)
                {
                    case ArgumentNullException argumentNullException:
                        exceptionSpecificData = ArgumentNullExceptionData(argumentNullException);
                        break;
                    case ArgumentOutOfRangeException argumentOutOfRangeException:
                        exceptionSpecificData = ArgumentOutOfRangeExceptionData(argumentOutOfRangeException);
                        break;
                    case DecoderFallbackException decoderFallbackException:
                        exceptionSpecificData = DecoderFallbackExceptionData(decoderFallbackException);
                        break;
                    case EncoderFallbackException encoderFallbackException:
                        exceptionSpecificData = EncoderFallbackExceptionData(encoderFallbackException);
                        break;
                    case ArgumentException argumentException:
                        exceptionSpecificData = ArgumentExceptionData(argumentException);
                        break;
                    case ConfigurationErrorsException configurationErrorsException:
                        exceptionSpecificData = ConfigurationErrorsExceptionData(configurationErrorsException);
                        break;
                    case FileNotFoundException fileNotFoundException:
                        exceptionSpecificData = FileNotFoundExceptionData(fileNotFoundException);
                        break;
                    case ObjectDisposedException objectDisposedException:
                        exceptionSpecificData = ObjectDisposedExceptionData(objectDisposedException);
                        break;
                    case SecurityException securityException:
                        exceptionSpecificData = SecurityExceptionData(securityException);
                        break;
                    case SqlException sqlException:
                        exceptionSpecificData = SqlExceptionData(sqlException);
                        break;
                    case WebException webException:
                        exceptionSpecificData = WebExceptionData(webException);
                        break;
                }
            }
            catch (Exception) { }

            return exceptionSpecificData;
        }

        private static Dictionary<string, object> ArgumentExceptionData(ArgumentException exception)
        {
            return new Dictionary<string, object>()
            {
                {"Parameter Name", exception.ParamName}
            };
        }

        private static Dictionary<string, object> ArgumentNullExceptionData(ArgumentNullException exception)
        {
            return new Dictionary<string, object>()
            {
                {"Parameter Name", exception.ParamName}
            };
        }

        private static Dictionary<string, object> ArgumentOutOfRangeExceptionData(ArgumentOutOfRangeException exception)
        {
            return new Dictionary<string, object>()
            {
                {"Actual Value", exception.ActualValue},
                {"Parameter Name", exception.ParamName}
            };
        }

        private static Dictionary<string, object> ConfigurationErrorsExceptionData(ConfigurationErrorsException exception)
        {
            return new Dictionary<string, object>()
            {
                {"Bare Message", exception.BareMessage},
                {"Filename", exception.Filename},
                {"Line", exception.Line}
            };
        }

        private static Dictionary<string, object> DecoderFallbackExceptionData(DecoderFallbackException exception)
        {
            return new Dictionary<string, object>()
            {
                {"Bytes Unknown", exception.BytesUnknown},
                {"Index", exception.Index},
                {"Parameter Name", exception.ParamName}
            };
        }

        private static Dictionary<string, object> EncoderFallbackExceptionData(EncoderFallbackException exception)
        {
            return new Dictionary<string, object>()
            {
                {"Character Unknown", exception.CharUnknown},
                {"Character Unknown High", exception.CharUnknownHigh},
                {"Character Unknown Low", exception.CharUnknownLow},
                {"Is Unknown Surrogate", exception.IsUnknownSurrogate()},
                {"Parameter Name", exception.ParamName}
            };
        }

        private static Dictionary<string, object> FileNotFoundExceptionData(FileNotFoundException exception)
        {
            return new Dictionary<string, object>()
            {
                {"File Name", exception.FileName}
            };
        }

        private static Dictionary<string, object> ObjectDisposedExceptionData(ObjectDisposedException exception)
        {
            return new Dictionary<string, object>()
            {
                {"Object Name", exception.ObjectName}
            };
        }

        private static Dictionary<string, object> SecurityExceptionData(SecurityException exception)
        {
            return new Dictionary<string, object>()
            {
                {"Action", exception.Action},
                {"Demanded", exception.Demanded},
                {"Deny Set Instance", exception.DenySetInstance},
                {"Failed Assembly Info", exception.FailedAssemblyInfo},
                {"First Permission That Failed", exception.FirstPermissionThatFailed},
                {"Granted Set", exception.GrantedSet},
                {"Method", exception.Method},
                {"Permission State", exception.PermissionState},
                {"Permission Type", exception.PermissionType},
                {"Permit Only Set Instance", exception.PermitOnlySetInstance},
                {"Refused Set", exception.RefusedSet},
                {"Url", exception.Url},
                {"Zone", exception.Zone}
            };
        }

        private static Dictionary<string, object> SqlExceptionData(SqlException exception)
        {
            return new Dictionary<string, object>()
            {
                {"Server", exception.Server},
                {"Class", exception.Class},
                {"Number", exception.Number},
                {"Procedure", exception.Procedure},
                {"Line Number", exception.LineNumber}
            };
        }

        private static Dictionary<string, object> WebExceptionData(WebException exception)
        {
            return new Dictionary<string, object>()
            {
                {"Response", exception.Response},
                {"Status", exception.Status}
            };
        }
        #endregion
    }
}
