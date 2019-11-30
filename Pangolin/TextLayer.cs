using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI;

namespace Pangolin
{
    public static class TextLayer
    {
        #region Alphabetical Order
        private static string RemoveInitialArticle(string text)
        {
            if (text == null) { throw new ArgumentNullException(nameof(text)); }
            else if (string.Equals(text, string.Empty)) { throw new ArgumentException($"{nameof(text)} cannot be an empty string.", nameof(text)); }
            else if (string.Equals(text.Trim(), string.Empty)) { throw new ArgumentException($"{nameof(text)} cannot be a white-space string.", nameof(text)); }

            string modifiedText = string.Empty;

            if (text.StartsWith("A ", StringComparison.InvariantCultureIgnoreCase))
            {
                modifiedText = text.Remove(0, 2);
            }
            else if (text.StartsWith("An ", StringComparison.InvariantCultureIgnoreCase))
            {
                modifiedText = text.Remove(0, 3);
            }
            else if (text.StartsWith("The ", StringComparison.InvariantCultureIgnoreCase))
            {
                modifiedText = text.Remove(0, 4);
            }

            if (!string.IsNullOrWhiteSpace(modifiedText))
            {
                modifiedText = modifiedText.TrimStart();
            }

            return modifiedText;
        }
        #endregion

        #region Compression
        public static async Task<byte[]> GZipCompressAsync(byte[] contentBytes, CancellationToken cancellationToken)
        {
            if (contentBytes == null) { throw new ArgumentNullException(nameof(contentBytes)); }
            else if (contentBytes.Length < 1) { throw new ArgumentException($"{nameof(contentBytes)} contains no elements.", nameof(contentBytes)); }

            try
            {
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    using (GZipStream gZipStream = new GZipStream(memoryStream, CompressionMode.Compress, false))
                    {
                        await gZipStream.WriteAsync(contentBytes, 0, contentBytes.Length, cancellationToken);
                    }
                    return memoryStream.ToArray();
                }
            }
            catch (NotSupportedException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (InvalidOperationException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
        }

        public static async Task<byte[]> GZipCompressAsync(string content, CancellationToken cancellationToken)
        {
            if (content == null) { throw new ArgumentNullException(nameof(content)); }
            else if (string.Equals(content, string.Empty)) { throw new ArgumentException($"{nameof(content)} cannot be an empty string.", nameof(content)); }

            try
            {
                byte[] contentBytes = ConfigurationLayer.DefaultEncoding.GetBytes(content);
                return await GZipCompressAsync(contentBytes, cancellationToken);
            }
            catch (EncoderFallbackException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
        }

        public static async Task<string> GZipCompressToBase64StringAsync(byte[] contentBytes, CancellationToken cancellationToken)
        {
            if (contentBytes == null) { throw new ArgumentNullException(nameof(contentBytes)); }
            else if (contentBytes.Length < 1) { throw new ArgumentException($"{nameof(contentBytes)} contains no elements.", nameof(contentBytes)); }

            byte[] gzippedBytes = await GZipCompressAsync(contentBytes, cancellationToken);
            return Convert.ToBase64String(gzippedBytes);
        }

        public static async Task<string> GZipCompressToBase64StringAsync(string content, CancellationToken cancellationToken)
        {
            if (content == null) { throw new ArgumentNullException(nameof(content)); }
            else if (string.Equals(content, string.Empty)) { throw new ArgumentException($"{nameof(content)} cannot be an empty string.", nameof(content)); }

            byte[] gzippedBytes = await GZipCompressAsync(content, cancellationToken);
            return Convert.ToBase64String(gzippedBytes);
        }

        public static async Task<byte[]> GZipDecompressAsync(byte[] contentBytes, CancellationToken cancellationToken)
        {
            if (contentBytes == null) { throw new ArgumentNullException(nameof(contentBytes)); }
            else if (contentBytes.Length < 1) { throw new ArgumentException($"{nameof(contentBytes)} contains no elements.", nameof(contentBytes)); }

            try
            {
                using (MemoryStream memoryStreamRead = new MemoryStream(contentBytes))
                using (GZipStream gZipStream = new GZipStream(memoryStreamRead, CompressionMode.Decompress))
                {
                    const int size = 4096;
                    byte[] buffer = new byte[size];
                    using (MemoryStream memoryStreamWrite = new MemoryStream())
                    {
                        int count = 0;
                        do
                        {
                            count = await gZipStream.ReadAsync(buffer, 0, size, cancellationToken);
                            if (count > 0)
                            {
                                await memoryStreamWrite.WriteAsync(buffer, 0, count, cancellationToken);
                            }
                        } while (count > 0);
                        return memoryStreamWrite.ToArray();
                    }
                }
            }
            catch (InvalidDataException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (IOException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (NotSupportedException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
        }

        public static async Task<byte[]> GZipDecompressAsync(string content, CancellationToken cancellationToken)
        {
            if (content == null) { throw new ArgumentNullException(nameof(content)); }
            else if (string.Equals(content, string.Empty)) { throw new ArgumentException($"{nameof(content)} cannot be an empty string.", nameof(content)); }

            try
            {
                byte[] contentBytes = Convert.FromBase64String(content);
                return await GZipDecompressAsync(contentBytes, cancellationToken);
            }
            catch (FormatException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
        }

        public static async Task<string> GZipDecompressToBase64StringAsync(byte[] contentBytes, CancellationToken cancellationToken)
        {
            if (contentBytes == null) { throw new ArgumentNullException(nameof(contentBytes)); }
            else if (contentBytes.Length < 1) { throw new ArgumentException($"{nameof(contentBytes)} contains no elements.", nameof(contentBytes)); }

            byte[] unGzippedBytes = await GZipDecompressAsync(contentBytes, cancellationToken);
            return Convert.ToBase64String(unGzippedBytes);
        }

        public static async Task<string> GZipDecompressToBase64StringAsync(string content, CancellationToken cancellationToken)
        {
            if (content == null) { throw new ArgumentNullException(nameof(content)); }
            else if (string.Equals(content, string.Empty)) { throw new ArgumentException($"{nameof(content)} cannot be an empty string.", nameof(content)); }

            byte[] unGzippedBytes = await GZipDecompressAsync(content, cancellationToken);
            return Convert.ToBase64String(unGzippedBytes);
        }
        #endregion

        #region CSV
        public static string FixedField(char text, int totalWidth, char paddingChar = ' ')
        {
            string charText = text.ToString();

            if (charText.Length > totalWidth)
            {
                throw new ArgumentOutOfRangeException("totalWidth", totalWidth, "The totalWidth cannot be less than the length of the text.");
            }
            else if (totalWidth < 0)
            {
                throw new ArgumentOutOfRangeException("totalWidth", totalWidth, "The totalWidth cannot be less than zero.");
            }

            return charText.PadRight(totalWidth, paddingChar);
        }

        public static string FixedField(double number, int totalWidth, char paddingChar = ' ')
        {
            string numberText = number.ToString();

            if (numberText.Length > totalWidth)
            {
                throw new ArgumentOutOfRangeException("totalWidth", totalWidth, "The totalWidth cannot be less than the length of the text.");
            }
            else if (totalWidth < 0)
            {
                throw new ArgumentOutOfRangeException("totalWidth", totalWidth, "The totalWidth cannot be less than zero.");
            }

            return numberText.PadLeft(totalWidth, paddingChar);
        }

        public static string FixedField(double number, string format, int totalWidth, char paddingChar = ' ')
        {
            if (string.IsNullOrWhiteSpace(format))
            {
                throw new ArgumentNullException("format");
            }

            string numberText = string.Empty;

            try
            {
                numberText = number.ToString(format);
            }
            catch (FormatException exc) { ExceptionLayer.Handle(exc); throw; }

            if (numberText.Length > totalWidth)
            {
                throw new ArgumentOutOfRangeException("totalWidth", totalWidth, "The totalWidth cannot be less than the length of the text.");
            }
            else if (totalWidth < 0)
            {
                throw new ArgumentOutOfRangeException("totalWidth", totalWidth, "The totalWidth cannot be less than zero.");
            }

            return numberText.PadLeft(totalWidth, paddingChar);
        }

        public static string FixedField(float number, int totalWidth, char paddingChar = ' ')
        {
            string numberText = number.ToString();

            if (numberText.Length > totalWidth)
            {
                throw new ArgumentOutOfRangeException("totalWidth", totalWidth, "The totalWidth cannot be less than the length of the text.");
            }
            else if (totalWidth < 0)
            {
                throw new ArgumentOutOfRangeException("totalWidth", totalWidth, "The totalWidth cannot be less than zero.");
            }

            return numberText.PadLeft(totalWidth, paddingChar);
        }

        public static string FixedField(float number, string format, int totalWidth, char paddingChar = ' ')
        {
            if (string.IsNullOrWhiteSpace(format))
            {
                throw new ArgumentNullException("format");
            }

            string numberText = string.Empty;

            try
            {
                numberText = number.ToString(format);
            }
            catch (FormatException exc) { ExceptionLayer.Handle(exc); throw; }

            if (numberText.Length > totalWidth)
            {
                throw new ArgumentOutOfRangeException("totalWidth", totalWidth, "The totalWidth cannot be less than the length of the text.");
            }
            else if (totalWidth < 0)
            {
                throw new ArgumentOutOfRangeException("totalWidth", totalWidth, "The totalWidth cannot be less than zero.");
            }

            return numberText.PadLeft(totalWidth, paddingChar);
        }

        public static string FixedField(int number, int totalWidth, char paddingChar = ' ')
        {
            string numberText = number.ToString();

            if (numberText.Length > totalWidth)
            {
                throw new ArgumentOutOfRangeException("totalWidth", totalWidth, "The totalWidth cannot be less than the length of the text.");
            }
            else if (totalWidth < 0)
            {
                throw new ArgumentOutOfRangeException("totalWidth", totalWidth, "The totalWidth cannot be less than zero.");
            }

            return numberText.PadLeft(totalWidth, paddingChar);
        }

        public static string FixedField(int number, string format, int totalWidth, char paddingChar = ' ')
        {
            if (string.IsNullOrWhiteSpace(format))
            {
                throw new ArgumentNullException("format");
            }

            string numberText = string.Empty;

            try
            {
                numberText = number.ToString(format);
            }
            catch (FormatException exc) { ExceptionLayer.Handle(exc); throw; }

            if (numberText.Length > totalWidth)
            {
                throw new ArgumentOutOfRangeException("totalWidth", totalWidth, "The totalWidth cannot be less than the length of the text.");
            }
            else if (totalWidth < 0)
            {
                throw new ArgumentOutOfRangeException("totalWidth", totalWidth, "The totalWidth cannot be less than zero.");
            }

            return numberText.PadLeft(totalWidth, paddingChar);
        }

        public static string FixedField(long number, int totalWidth, char paddingChar = ' ')
        {
            string numberText = number.ToString();

            if (numberText.Length > totalWidth)
            {
                throw new ArgumentOutOfRangeException("totalWidth", totalWidth, "The totalWidth cannot be less than the length of the text.");
            }
            else if (totalWidth < 0)
            {
                throw new ArgumentOutOfRangeException("totalWidth", totalWidth, "The totalWidth cannot be less than zero.");
            }

            return numberText.PadLeft(totalWidth, paddingChar);
        }

        public static string FixedField(long number, string format, int totalWidth, char paddingChar = ' ')
        {
            if (string.IsNullOrWhiteSpace(format))
            {
                throw new ArgumentNullException("format");
            }

            string numberText = string.Empty;

            try
            {
                numberText = number.ToString(format);
            }
            catch (FormatException exc) { ExceptionLayer.Handle(exc); throw; }

            if (numberText.Length > totalWidth)
            {
                throw new ArgumentOutOfRangeException("totalWidth", totalWidth, "The totalWidth cannot be less than the length of the text.");
            }
            else if (totalWidth < 0)
            {
                throw new ArgumentOutOfRangeException("totalWidth", totalWidth, "The totalWidth cannot be less than zero.");
            }

            return numberText.PadLeft(totalWidth, paddingChar);
        }

        public static string FixedField(string text, int totalWidth, char paddingChar = ' ')
        {
            if (text == null) { throw new ArgumentNullException(nameof(text)); }
            else if (text.Length > totalWidth)
            {
                throw new ArgumentOutOfRangeException(nameof(totalWidth), totalWidth, $"{nameof(totalWidth)} cannot be less than the length of {text}.");
            }
            else if (totalWidth < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(totalWidth), totalWidth, $"{nameof(totalWidth)} cannot be less than zero.");
            }

            return text.PadRight(totalWidth, paddingChar);
        }
        #endregion

        #region Html
        public static string HtmlDecode(string html)
        {
            return HttpUtility.HtmlDecode(html);
        }

        public static string HtmlEncode(string html)
        {
            return HttpUtility.HtmlEncode(html);
        }

        public static string HtmlWritePage(string title, string body)
        {
            StringBuilder stringBuilder = new StringBuilder("<!DOCTYPE HTML>", 1024);

            using (StringWriter stringWriter = new StringWriter(stringBuilder))
            {
                using (HtmlTextWriter htmlTextWriter = new HtmlTextWriter(stringWriter, ConfigurationLayer.Tab))
                {
                    htmlTextWriter.WriteBeginTag("html");
                    htmlTextWriter.WriteAttribute("lang", "en-us");
                    htmlTextWriter.Write(HtmlTextWriter.TagRightChar);

                    htmlTextWriter.WriteFullBeginTag("head");

                    htmlTextWriter.WriteFullBeginTag("title");
                    htmlTextWriter.Write(title);
                    htmlTextWriter.WriteEndTag("title");

                    htmlTextWriter.WriteBeginTag("base");
                    htmlTextWriter.WriteAttribute("href", "./");
                    htmlTextWriter.Write(HtmlTextWriter.TagRightChar);

                    htmlTextWriter.WriteBeginTag("meta");
                    htmlTextWriter.WriteAttribute("charset", "UTF-8");
                    htmlTextWriter.Write(HtmlTextWriter.TagRightChar);

                    htmlTextWriter.WriteBeginTag("meta");
                    htmlTextWriter.WriteAttribute("http-equiv", "Content-Security-Policy");
                    htmlTextWriter.WriteAttribute("content", "default-src 'none';");
                    htmlTextWriter.Write(HtmlTextWriter.TagRightChar);

                    htmlTextWriter.WriteBeginTag("meta");
                    htmlTextWriter.WriteAttribute("http-equiv", "Strict-Transport-Security");
                    htmlTextWriter.WriteAttribute("content", "max-age=20070400; includeSubDomains");
                    htmlTextWriter.Write(HtmlTextWriter.TagRightChar);

                    htmlTextWriter.WriteBeginTag("meta");
                    htmlTextWriter.WriteAttribute("http-equiv", "X-Content-Type-Options");
                    htmlTextWriter.WriteAttribute("content", "nosniff");
                    htmlTextWriter.Write(HtmlTextWriter.TagRightChar);

                    htmlTextWriter.WriteBeginTag("meta");
                    htmlTextWriter.WriteAttribute("http-equiv", "X-XSS-Protection");
                    htmlTextWriter.WriteAttribute("content", "1; mode=block");
                    htmlTextWriter.Write(HtmlTextWriter.TagRightChar);

                    htmlTextWriter.WriteBeginTag("meta");
                    htmlTextWriter.WriteAttribute("name", "viewport");
                    htmlTextWriter.WriteAttribute("content", "height = device-height, width = device-width, initial-scale = 1.0, user-scalable = yes");
                    htmlTextWriter.Write(HtmlTextWriter.TagRightChar);

                    htmlTextWriter.WriteEndTag("head");

                    htmlTextWriter.WriteFullBeginTag("body");

                    htmlTextWriter.WriteFullBeginTag("noscript");
                    htmlTextWriter.WriteFullBeginTag("code");
                    htmlTextWriter.Write("Scripts are currently disabled on your browser!");
                    htmlTextWriter.WriteEndTag("code");
                    htmlTextWriter.WriteEndTag("noscript");

                    htmlTextWriter.Write(body);

                    htmlTextWriter.WriteEndTag("body");

                    htmlTextWriter.WriteEndTag("html");
                }
                return stringWriter.ToString();
            }
        }

        public static string HtmlWriteTable(DataTable dataTable)
        {
            if (dataTable == null) { throw new ArgumentNullException(nameof(dataTable)); }
            else if (dataTable.Columns.Count < 1) { throw new ArgumentException($"{nameof(dataTable)} contains no columns.", nameof(dataTable)); }
            else if (dataTable.Rows.Count < 1) { throw new ArgumentException($"{nameof(dataTable)} contains no rows.", nameof(dataTable)); }

            StringBuilder stringBuilder = new StringBuilder();
            using (StringWriter stringWriter = new StringWriter(stringBuilder))
            {
                using (HtmlTextWriter htmlTextWriter = new HtmlTextWriter(stringWriter, ConfigurationLayer.Tab))
                {
                    htmlTextWriter.WriteBeginTag("table");

                    htmlTextWriter.WriteBeginTag("tr");
                    for (int c = 0; c < dataTable.Columns.Count; c++)
                    {
                        htmlTextWriter.WriteBeginTag("th");

                        htmlTextWriter.Write(dataTable.Columns[c].ColumnName);

                        htmlTextWriter.WriteEndTag("th");
                    }
                    htmlTextWriter.WriteEndTag("tr");

                    for (int r = 0; r < dataTable.Rows.Count; r++)
                    {
                        htmlTextWriter.WriteBeginTag("tr");
                        for (int c = 0; c < dataTable.Columns.Count; c++)
                        {
                            htmlTextWriter.WriteBeginTag("td");

                            if (dataTable.Rows[r][c] != null && dataTable.Rows[r][c] != DBNull.Value)
                            {
                                htmlTextWriter.Write(dataTable.Rows[r][c].ToString());
                            }

                            htmlTextWriter.WriteEndTag("td");
                        }
                        htmlTextWriter.WriteEndTag("tr");
                    }

                    htmlTextWriter.WriteEndTag("table");
                }
            }

            return stringBuilder.ToString();
        }

        public static string HtmlWriteTable(DataSet dataSet)
        {
            if (dataSet == null) { throw new ArgumentNullException(nameof(dataSet)); }
            else if (dataSet.Tables.Count < 1) { throw new ArgumentException($"{nameof(dataSet)} contains no tables.", nameof(dataSet)); }

            StringBuilder stringBuilder = new StringBuilder();

            try
            {
                for (int t = 0; t < dataSet.Tables.Count; t++)
                {
                    stringBuilder.Append(HtmlWriteTable(dataSet.Tables[t]));
                }
            }
            catch (ArgumentOutOfRangeException exc) { ExceptionLayer.Handle(exc); throw; }
            
            return stringBuilder.ToString();

        }
        #endregion
        
        #region Uri

        #region Url Encoding
        public static string UrlDecode(string url)
        {
            return HttpUtility.UrlDecode(url, ConfigurationLayer.DefaultEncoding);
        }

        public static string UrlEncode(string url)
        {
            return HttpUtility.UrlEncode(url, ConfigurationLayer.DefaultEncoding);
        }
        #endregion


        #region Query String
        public static string CreateQueryString(NameValueCollection queryStringKeyValues)
        {
            if (queryStringKeyValues == null) { throw new ArgumentNullException(nameof(queryStringKeyValues)); }
            else if (queryStringKeyValues.Count < 1) { throw new ArgumentException($"{nameof(queryStringKeyValues)} contains no values.", nameof(queryStringKeyValues)); }

            string queryString = string.Empty;

            string[] queryStringParts = new string[queryStringKeyValues.Count];
            int index = default;
            foreach (KeyValuePair<string, string> queryStringPair in queryStringKeyValues)
            {
                string encodedKey = UrlEncode(queryStringPair.Key.Trim());
                string encodedValue = UrlEncode(queryStringPair.Value.Trim());
                queryStringParts[index] = $"{encodedKey}={encodedValue}";
                index++;
            }

            queryString = $"?{string.Join("&", queryStringParts)}";

            return queryString;
        }
        #endregion
        #endregion

        #region Xml
        #endregion
    }
}
