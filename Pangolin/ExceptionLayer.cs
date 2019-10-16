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
        [Flags]
        private enum ExceptionFormat
        {
            Html,
            Text,
            Xml
        }

        private const char _dash = '-';
        private const char _space = ' ';

        private const int _lineBreakSpace = 80;
        private const int _padRightSpace = 32;

        private static readonly string _indentation = new string(_space, _padRightSpace);
        private static readonly string _lineBreak = new string(_dash, _lineBreakSpace);

        public static void Handle(Exception exception)
        {
            string exceptionText = WriteExceptionText(exception);
            DiagnosticsLayer.EventLogWriteError(exceptionText);

            try
            {
                Console.WriteLine(exceptionText);
            }
            finally { }
        }
        
        public static async Task HandleAsync(Exception exception)
        {
            string exceptionText = WriteExceptionText(exception);
            await FileLayer.LogExceptionAsync(exceptionText);
            DiagnosticsLayer.EventLogWriteError(exceptionText);

            string body = WriteExceptionHtml(exception);
            await MailLayer.SendDeveloperMailAsync($"ExceptionLayer Exception - {exception.GetType().Name}", body);
        }

        public static async Task HandleAsync(Exception exception, string query, DbParameter[] dbParameters)
        {
            exception = AddDeveloperCustomData(exception, query);
            exception = AddDeveloperCustomData(exception, dbParameters);

            string exceptionText = WriteExceptionText(exception);
            await FileLayer.LogExceptionAsync(exceptionText);
            DiagnosticsLayer.EventLogWriteError(exceptionText);

            string body = WriteExceptionHtml(exception);
            await MailLayer.SendDeveloperMailAsync($"ExceptionLayer Exception - {exception.GetType().Name}", body);
        }
        
        private static string RenderException(Exception exception, ExceptionFormat exceptionFormat)
        {
            string exceptionText = string.Empty;

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

            switch (exceptionFormat)
            {
                case ExceptionFormat.Html:
                    exceptionText = WriteExceptionHtml(type, timestamp, message, hresult, source, targetSite, stackTrace, data, specificData, helpLink, innerExceptions);
                    break;
                case ExceptionFormat.Text:
                    exceptionText = WriteExceptionText(type, timestamp, message, hresult, source, targetSite, stackTrace, data, specificData, helpLink, innerExceptions);
                    break;
                case ExceptionFormat.Xml:
                    exceptionText = WriteExceptionXml(type, timestamp, message, hresult, source, targetSite, stackTrace, data, specificData, helpLink, innerExceptions);
                    break;
                default:
                    break;
            }

            return exceptionText;
        }

        #region Format Exception - Text
        private static string WriteExceptionText(Exception exception)
        {
            return RenderException(exception, ExceptionFormat.Text);
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

                exceptionText.AppendLine("HRESULT:".PadRight(_padRightSpace) + hresult.ToString());

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
            return RenderException(exception, ExceptionFormat.Html);
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
                    htmlTextWriter.WriteFullBeginTag("b");
                    htmlTextWriter.Write("Message");
                    htmlTextWriter.WriteEndTag("b");
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
                        htmlTextWriter.WriteFullBeginTag("b");
                        htmlTextWriter.Write("Data");
                        htmlTextWriter.WriteEndTag("b");
                        htmlTextWriter.WriteEndTag("summary");

                        foreach (DictionaryEntry entry in data)
                        {
                            if (entry.Key != null && entry.Value != null)
                            {
                                if (string.Equals(entry.Key.ToString(), "Parameters") && entry.Value is DbParameter[] dbParameters)
                                {
                                    htmlTextWriter.WriteFullBeginTag("b");
                                    htmlTextWriter.Write("Parameters");
                                    htmlTextWriter.WriteEndTag("b");

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
                                    htmlTextWriter.WriteFullBeginTag("b");
                                    htmlTextWriter.Write("Query");
                                    htmlTextWriter.WriteEndTag("b");

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
                        htmlTextWriter.WriteFullBeginTag("b");
                        htmlTextWriter.Write("Specific Data");
                        htmlTextWriter.WriteEndTag("b");
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
                    htmlTextWriter.WriteFullBeginTag("b");
                    htmlTextWriter.Write("HRESULT");
                    htmlTextWriter.WriteEndTag("b");
                    htmlTextWriter.WriteEndTag("summary");
                    htmlTextWriter.WriteFullBeginTag("code");
                    htmlTextWriter.Write(hresult.ToString());
                    htmlTextWriter.WriteEndTag("code");
                    htmlTextWriter.WriteEndTag("details");

                    if (source?.Length > 0)
                    {
                        htmlTextWriter.WriteBeginTag("details");
                        htmlTextWriter.WriteAttribute("open", null);
                        htmlTextWriter.Write(HtmlTextWriter.TagRightChar);
                        htmlTextWriter.WriteFullBeginTag("summary");
                        htmlTextWriter.WriteFullBeginTag("b");
                        htmlTextWriter.Write("Source");
                        htmlTextWriter.WriteEndTag("b");
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
                        htmlTextWriter.WriteFullBeginTag("b");
                        htmlTextWriter.Write("Target Site");
                        htmlTextWriter.WriteEndTag("b");
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
                        htmlTextWriter.WriteFullBeginTag("b");
                        htmlTextWriter.Write("Stack Trace");
                        htmlTextWriter.WriteEndTag("b");
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
                        htmlTextWriter.WriteFullBeginTag("b");
                        htmlTextWriter.Write("Stack Trace");
                        htmlTextWriter.WriteEndTag("b");
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
                        htmlTextWriter.WriteFullBeginTag("b");
                        htmlTextWriter.Write("Inner Exceptions");
                        htmlTextWriter.WriteEndTag("b");
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
            return RenderException(exception, ExceptionFormat.Xml);
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
                xmlWriter.WriteAttributeString("HRESULT", hresult.ToString());

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
        private static Exception AddDeveloperCustomData(Exception exception, string query)
        {
            if (exception == null)
            {
                throw new ArgumentNullException("exception");
            }
            if (string.IsNullOrWhiteSpace(query))
            {
                throw new ArgumentNullException(query);
            }
            if (exception.Data.IsReadOnly)
            {
                throw new InvalidOperationException("The Exception.Data property is read-only.");
            }
            if (exception.Data.Contains("Query"))
            {
                throw new InvalidOperationException("The 'Query' parameter has already been added.");
            }

            exception.Data.Add("Query", query);

            return exception;
        }

        private static Exception AddDeveloperCustomData(Exception exception, DbParameter[] dbParameters)
        {
            if (exception == null)
            {
                throw new ArgumentNullException("exception");
            }
            if (dbParameters == null)
            {
                throw new ArgumentNullException("dbParameters");
            }
            if (dbParameters.Length < 1)
            {
                throw new ArgumentException("dbParameters has no elements.");
            }
            if (exception.Data.IsReadOnly)
            {
                throw new InvalidOperationException("The Exception.Data property is read-only.");
            }
            if (exception.Data.Contains("Parameters"))
            {
                throw new InvalidOperationException("The 'Parameters' parameter has already been added.");
            }

            exception.Data.Add("Parameters", dbParameters);

            return exception;
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
