using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Pangolin
{
    static class WebLayer
    {
        private const int _zeroIndex = 0;

        private const string _contentType = "application/json; charset=utf-8";

        private const string _methodDelete = "DELETE";
        private const string _methodGet = "GET";
        private const string _methodHead = "HEAD";
        private const string _methodOptions = "OPTIONS";
        private const string _methodPatch = "PATCH";
        private const string _methodPost = "POST";
        private const string _methodPut = "PUT";
        private const string _methodTrace = "TRACE";

        private static readonly DecompressionMethods _defaultDecompression = DecompressionMethods.GZip;

        private static string GetBasicAuthenticationHeader(string username, string password)
        {
            if (username == null) { throw new ArgumentNullException(nameof(username)); }
            if (password == null) { throw new ArgumentNullException(nameof(password)); }

            try
            {
                string authorizationString = $"{username}:{password}";
                byte[] authorizationBytes = Encoding.ASCII.GetBytes(authorizationString);
                string authorizationBytesBase64 = Convert.ToBase64String(authorizationBytes);

                return $"Basic {authorizationBytesBase64}";
            }
            catch (EncoderFallbackException exc)
            {
                ExceptionLayer.Handle(exc);
                throw;
            }
        }

        #region FtpWebRequest
        public static async Task<bool> CreateDirectoryAsync(string externalFolderLocation, string userName, string password)
        {
            NetworkCredential networkCredential = new NetworkCredential(userName, password);
            return await CreateDirectoryAsync(externalFolderLocation, networkCredential);
        }

        public static async Task<bool> CreateDirectoryAsync(string externalFolderLocation, NetworkCredential networkCredential = null)
        {
            if (ConfigurationLayer.IsLiveFTP && Uri.TryCreate(externalFolderLocation, UriKind.Absolute, out Uri ftpUri) && ftpUri.Scheme == Uri.UriSchemeFtp)
            {
                try
                {
                    FtpWebRequest ftpWebRequest = WebRequest.Create(ftpUri) as FtpWebRequest;
                    ftpWebRequest.Method = WebRequestMethods.Ftp.MakeDirectory;

                    if (networkCredential != null)
                    {
                        ftpWebRequest.Credentials = networkCredential;
                    }

                    ftpWebRequest.EnableSsl = true;
                    ftpWebRequest.KeepAlive = false;
                    ftpWebRequest.UseBinary = true;
                    ftpWebRequest.UsePassive = true;

                    using (FtpWebResponse ftpWebResponse = await ftpWebRequest.GetResponseAsync() as FtpWebResponse)
                    {
                        return true;
                    }
                }
                catch (ArgumentNullException exc) { await ExceptionLayer.HandleAsync(exc); }
                catch (ArgumentException exc) { await ExceptionLayer.HandleAsync(exc); }
                catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); }
                catch (NotSupportedException exc) { await ExceptionLayer.HandleAsync(exc); }
                catch (SecurityException exc) { await ExceptionLayer.HandleAsync(exc); }
            }

            return false;
        }

        public static async Task<bool> DeleteAsync(string externalFileLocation, string userName = null, string password = null)
        {
            if (ConfigurationLayer.IsLiveFTP && Uri.TryCreate(externalFileLocation, UriKind.Absolute, out Uri ftpUri) && ftpUri.Scheme == Uri.UriSchemeFtp)
            {
                try
                {
                    FtpWebRequest ftpWebRequest = WebRequest.Create(ftpUri) as FtpWebRequest;
                    ftpWebRequest.Method = WebRequestMethods.Ftp.DeleteFile;

                    if (!string.IsNullOrWhiteSpace(userName) && !string.IsNullOrWhiteSpace(password))
                    {
                        ftpWebRequest.Credentials = new NetworkCredential(userName, password);
                    }

                    ftpWebRequest.EnableSsl = true;
                    ftpWebRequest.KeepAlive = false;
                    ftpWebRequest.UseBinary = true;
                    ftpWebRequest.UsePassive = true;

                    using (FtpWebResponse ftpWebResponse = await ftpWebRequest.GetResponseAsync() as FtpWebResponse)
                    {
                        return true;
                    }
                }
                catch (ArgumentNullException exc) { await ExceptionLayer.HandleAsync(exc); }
                catch (ArgumentException exc) { await ExceptionLayer.HandleAsync(exc); }
                catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); }
                catch (NotSupportedException exc) { await ExceptionLayer.HandleAsync(exc); }
                catch (SecurityException exc) { await ExceptionLayer.HandleAsync(exc); }
            }

            return false;
        }

        public static async Task<bool> DownloadAsync(string externalFileLocation, string localFileLocation, string userName = null, string password = null)
        {
            if (ConfigurationLayer.IsLiveFTP && Uri.TryCreate(externalFileLocation, UriKind.Absolute, out Uri ftpUri) && ftpUri.Scheme == Uri.UriSchemeFtp)
            {
                try
                {
                    FtpWebRequest ftpWebRequest = WebRequest.Create(ftpUri) as FtpWebRequest;
                    ftpWebRequest.Method = WebRequestMethods.Ftp.DownloadFile;

                    if (!string.IsNullOrWhiteSpace(userName) && !string.IsNullOrWhiteSpace(password))
                    {
                        ftpWebRequest.Credentials = new NetworkCredential(userName, password);
                    }

                    ftpWebRequest.EnableSsl = true;
                    ftpWebRequest.KeepAlive = false;
                    ftpWebRequest.UseBinary = true;
                    ftpWebRequest.UsePassive = true;

                    using (FtpWebResponse ftpWebResponse = await ftpWebRequest.GetResponseAsync() as FtpWebResponse)
                    using (Stream stream = ftpWebResponse.GetResponseStream())
                    using (StreamReader streamReader = new StreamReader(stream))
                    {
                        string content = await streamReader.ReadToEndAsync();
                        return await FileLayer.CreateFileAsync(localFileLocation, content);
                    }
                }
                catch (ArgumentNullException exc) { await ExceptionLayer.HandleAsync(exc); }
                catch (ArgumentException exc) { await ExceptionLayer.HandleAsync(exc); }
                catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); }
                catch (NotSupportedException exc) { await ExceptionLayer.HandleAsync(exc); }
                catch (SecurityException exc) { await ExceptionLayer.HandleAsync(exc); }
            }

            return false;
        }

        public static async Task<string[]> ListDirectoriesAsync(string externalFolderLocation, string userName = null, string password = null)
        {
            if (ConfigurationLayer.IsLiveFTP && Uri.TryCreate(externalFolderLocation, UriKind.Absolute, out Uri ftpUri) && ftpUri.Scheme == Uri.UriSchemeFtp)
            {
                try
                {
                    FtpWebRequest ftpWebRequest = WebRequest.Create(ftpUri) as FtpWebRequest;
                    ftpWebRequest.Method = WebRequestMethods.Ftp.ListDirectory;

                    if (!string.IsNullOrWhiteSpace(userName) && !string.IsNullOrWhiteSpace(password))
                    {
                        ftpWebRequest.Credentials = new NetworkCredential(userName, password);
                    }

                    ftpWebRequest.EnableSsl = true;
                    ftpWebRequest.KeepAlive = false;
                    ftpWebRequest.UseBinary = true;
                    ftpWebRequest.UsePassive = true;

                    List<string> fileList = new List<string>();

                    using (FtpWebResponse ftpWebResponse = await ftpWebRequest.GetResponseAsync() as FtpWebResponse)
                    using (Stream stream = ftpWebResponse.GetResponseStream())
                    using (StreamReader streamReader = new StreamReader(stream))
                    {
                        string line = string.Empty;
                        while ((line = await streamReader.ReadLineAsync()) != null)
                        {
                            fileList.Add(line);
                        }
                    }

                    return fileList.ToArray();
                }
                catch (ArgumentNullException exc) { await ExceptionLayer.HandleAsync(exc); }
                catch (ArgumentOutOfRangeException exc) { await ExceptionLayer.HandleAsync(exc); }
                catch (ArgumentException exc) { await ExceptionLayer.HandleAsync(exc); }
                catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); }
                catch (NotSupportedException exc) { await ExceptionLayer.HandleAsync(exc); }
                catch (SecurityException exc) { await ExceptionLayer.HandleAsync(exc); }
            }

            return new string[] { };
        }

        public static async Task<bool> RenameAsync(string externalFileLocation, string newFileName, string userName = null, string password = null)
        {
            if (ConfigurationLayer.IsLiveFTP && Uri.TryCreate(externalFileLocation, UriKind.Absolute, out Uri ftpUri) && ftpUri.Scheme == Uri.UriSchemeFtp)
            {
                try
                {
                    FtpWebRequest ftpWebRequest = WebRequest.Create(ftpUri) as FtpWebRequest;
                    ftpWebRequest.Method = WebRequestMethods.Ftp.Rename;

                    if (!string.IsNullOrWhiteSpace(userName) && !string.IsNullOrWhiteSpace(password))
                    {
                        ftpWebRequest.Credentials = new NetworkCredential(userName, password);
                    }

                    ftpWebRequest.EnableSsl = true;
                    ftpWebRequest.KeepAlive = false;
                    ftpWebRequest.UseBinary = true;
                    ftpWebRequest.UsePassive = true;

                    ftpWebRequest.RenameTo = newFileName;

                    using (FtpWebResponse ftpWebResponse = await ftpWebRequest.GetResponseAsync() as FtpWebResponse)
                    {
                        return true;
                    }
                }
                catch (ArgumentNullException exc) { await ExceptionLayer.HandleAsync(exc); }
                catch (ArgumentException exc) { await ExceptionLayer.HandleAsync(exc); }
                catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); }
                catch (NotSupportedException exc) { await ExceptionLayer.HandleAsync(exc); }
                catch (SecurityException exc) { await ExceptionLayer.HandleAsync(exc); }
            }

            return false;
        }
        
        public static async Task<bool> UploadAsync(string localFileLocation, string externalFileLocation, string userName = null, string password = null)
        {
            if (ConfigurationLayer.IsLiveFTP && Uri.TryCreate(externalFileLocation, UriKind.Absolute, out Uri ftpUri) && ftpUri.Scheme == Uri.UriSchemeFtp)
            {
                FtpWebRequest ftpWebRequest;
                try
                {
                    ftpWebRequest = WebRequest.Create(ftpUri) as FtpWebRequest;

                    ftpWebRequest.Method = WebRequestMethods.Ftp.UploadFile;

                    if (!string.IsNullOrWhiteSpace(userName) && !string.IsNullOrWhiteSpace(password))
                    {
                        ftpWebRequest.Credentials = new NetworkCredential(userName, password);
                    }

                    ftpWebRequest.EnableSsl = true;
                    ftpWebRequest.KeepAlive = false;
                    ftpWebRequest.UseBinary = true;
                    ftpWebRequest.UsePassive = true;

                    string content = await FileLayer.ReadFileAsync(localFileLocation, CancellationToken.None);

                    byte[] contentBytes = Encoding.UTF8.GetBytes(content);

                    if (contentBytes?.Length > 0)
                    {
                        using (Stream s = await ftpWebRequest.GetRequestStreamAsync())
                        {
                            await s.WriteAsync(contentBytes, 0, contentBytes.Length);
                        }

                        ftpWebRequest.ContentLength = contentBytes.Length;

                        using (FtpWebResponse ftpWebResponse = (await ftpWebRequest.GetResponseAsync()) as FtpWebResponse)
                        {
                            return ftpWebResponse.StatusCode == FtpStatusCode.FileActionOK;
                        }
                    }
                }
                catch (ArgumentNullException exc) { await ExceptionLayer.HandleAsync(exc); }
                catch (EncoderFallbackException exc) { await ExceptionLayer.HandleAsync(exc); }
                catch (ArgumentException exc) { await ExceptionLayer.HandleAsync(exc); }
                catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); }
                catch (NotSupportedException exc) { await ExceptionLayer.HandleAsync(exc); }
                catch (SecurityException exc) { await ExceptionLayer.HandleAsync(exc); }
            }

            return false;
        }
        #endregion

        #region HttpWebRequest
        public static CredentialCache GetCredentialCache(Uri address, string username, string password)
        {
            NetworkCredential networkCredential = new NetworkCredential(username, password);

            CredentialCache credentialCache = new CredentialCache()
            {
                { address, "Basic", networkCredential },
                { address, "Digest", networkCredential },
                { address, "NTLM", networkCredential }
            };

            return credentialCache;
        }

        public static async Task<bool> DeleteAsync(Uri address)
        {
            try
            {
                if (address.Scheme == Uri.UriSchemeHttps || address.Scheme == Uri.UriSchemeHttp)
                {
                    HttpWebRequest httpWebRequest = WebRequest.CreateHttp(address);
                    httpWebRequest.AutomaticDecompression = _defaultDecompression;
                    httpWebRequest.Method = _methodDelete;

                    using (HttpWebResponse httpWebResponse = await httpWebRequest.GetResponseAsync() as HttpWebResponse)
                    {
                        return httpWebResponse.StatusCode == HttpStatusCode.OK || httpWebResponse.StatusCode == HttpStatusCode.Accepted || httpWebResponse.StatusCode == HttpStatusCode.NoContent;
                    }
                }
            }
            catch (NotSupportedException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (SecurityException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (ProtocolViolationException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (ArgumentNullException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (ArgumentException exc) { await ExceptionLayer.HandleAsync(exc); }

            return false;
        }
        
        public static async Task<bool> DeleteAsync(Uri address, string username, string password)
        {
            try
            {
                if (address.Scheme == Uri.UriSchemeHttps || address.Scheme == Uri.UriSchemeHttp)
                {
                    HttpWebRequest httpWebRequest = WebRequest.CreateHttp(address);
                    httpWebRequest.AutomaticDecompression = _defaultDecompression;
                    httpWebRequest.Method = _methodDelete;
                    
                    httpWebRequest.Credentials = GetCredentialCache(address, username, password);

                    using (HttpWebResponse httpWebResponse = await httpWebRequest.GetResponseAsync() as HttpWebResponse)
                    {
                        return httpWebResponse.StatusCode == HttpStatusCode.OK || httpWebResponse.StatusCode == HttpStatusCode.Accepted || httpWebResponse.StatusCode == HttpStatusCode.NoContent;
                    }
                }
            }
            catch (NotSupportedException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (SecurityException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (ProtocolViolationException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (ArgumentNullException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (ArgumentException exc) { await ExceptionLayer.HandleAsync(exc); }

            return false;
        }

        public static async Task<string> GetAsync(Uri address)
        {
            try
            {
                if (address.Scheme == Uri.UriSchemeHttps || address.Scheme == Uri.UriSchemeHttp)
                {
                    HttpWebRequest httpWebRequest = WebRequest.CreateHttp(address);
                    httpWebRequest.AutomaticDecompression = _defaultDecompression;
                    httpWebRequest.Method = _methodGet;

                    using (HttpWebResponse httpWebResponse = await httpWebRequest.GetResponseAsync() as HttpWebResponse)
                    using (Stream stream = httpWebResponse.GetResponseStream())
                    using (StreamReader streamReader = new StreamReader(stream))
                    {
                        return await streamReader.ReadToEndAsync();
                    }
                }
            }
            catch (ProtocolViolationException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (WebException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (NotSupportedException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (SecurityException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (NotImplementedException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (ArgumentNullException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (ArgumentException exc) { await ExceptionLayer.HandleAsync(exc); }

            return HttpStatusCode.BadRequest.ToString();
        }

        public static async Task<string> GetAsync(Uri address, string username, string password)
        {
            try
            {
                if (address.Scheme == Uri.UriSchemeHttps || address.Scheme == Uri.UriSchemeHttp)
                {
                    HttpWebRequest httpWebRequest = WebRequest.CreateHttp(address);
                    httpWebRequest.AutomaticDecompression = _defaultDecompression;
                    httpWebRequest.Method = _methodGet;

                    httpWebRequest.Credentials = GetCredentialCache(address, username, password);

                    using (HttpWebResponse httpWebResponse = await httpWebRequest.GetResponseAsync() as HttpWebResponse)
                    using (Stream stream = httpWebResponse.GetResponseStream())
                    using (StreamReader streamReader = new StreamReader(stream))
                    {
                        return await streamReader.ReadToEndAsync();
                    }
                }
            }
            catch (ProtocolViolationException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (WebException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (NotSupportedException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (SecurityException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (NotImplementedException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (ArgumentNullException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (ArgumentException exc) { await ExceptionLayer.HandleAsync(exc); }

            return HttpStatusCode.BadRequest.ToString();
        }

        public static async Task<bool> HeadAsync(Uri address)
        {
            try
            {
                if (address.Scheme == Uri.UriSchemeHttps || address.Scheme == Uri.UriSchemeHttp)
                {
                    HttpWebRequest httpWebRequest = WebRequest.CreateHttp(address);
                    httpWebRequest.AutomaticDecompression = _defaultDecompression;
                    httpWebRequest.Method = _methodHead;

                    using (HttpWebResponse httpWebResponse = await httpWebRequest.GetResponseAsync() as HttpWebResponse)
                    {
                        return httpWebResponse.StatusCode == HttpStatusCode.OK;
                    }
                }
                
            }
            catch (ProtocolViolationException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (NotSupportedException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (SecurityException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (NotImplementedException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (ArgumentNullException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (ArgumentException exc) { await ExceptionLayer.HandleAsync(exc); }

            return false;
        }

        public static async Task<bool> HeadAsync(Uri address, string username, string password)
        {
            try
            {
                if (address.Scheme == Uri.UriSchemeHttps || address.Scheme == Uri.UriSchemeHttp)
                {
                    HttpWebRequest httpWebRequest = WebRequest.CreateHttp(address);
                    httpWebRequest.AutomaticDecompression = _defaultDecompression;
                    httpWebRequest.Method = _methodHead;

                    httpWebRequest.Credentials = GetCredentialCache(address, username, password);

                    using (HttpWebResponse httpWebResponse = await httpWebRequest.GetResponseAsync() as HttpWebResponse)
                    {
                        return httpWebResponse.StatusCode == HttpStatusCode.OK;
                    }
                }

            }
            catch (ProtocolViolationException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (NotSupportedException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (SecurityException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (NotImplementedException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (ArgumentNullException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (ArgumentException exc) { await ExceptionLayer.HandleAsync(exc); }

            return false;
        }

        public static async Task<string[]> OptionsAsync(Uri address)
        {
            try
            {
                if (address.Scheme == Uri.UriSchemeHttps || address.Scheme == Uri.UriSchemeHttp)
                {
                    HttpWebRequest httpWebRequest = WebRequest.CreateHttp(address);
                    httpWebRequest.AutomaticDecompression = _defaultDecompression;
                    httpWebRequest.Method = _methodOptions;

                    using (HttpWebResponse httpWebResponse = await httpWebRequest.GetResponseAsync() as HttpWebResponse)
                    {
                        string allow = httpWebResponse.Headers[HttpResponseHeader.Allow];
                        if (!string.IsNullOrWhiteSpace(allow))
                        {
                            return allow.Split(ConfigurationLayer.FieldSplit, StringSplitOptions.RemoveEmptyEntries);
                        }
                    }
                }
            }
            catch (ProtocolViolationException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (NotSupportedException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (SecurityException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (NotImplementedException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (ArgumentNullException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (ArgumentException exc) { await ExceptionLayer.HandleAsync(exc); }

            return new string[] { };
        }

        public static async Task<string[]> OptionsAsync(Uri address, string username, string password)
        {
            try
            {
                if (address.Scheme == Uri.UriSchemeHttps || address.Scheme == Uri.UriSchemeHttp)
                {
                    HttpWebRequest httpWebRequest = WebRequest.CreateHttp(address);
                    httpWebRequest.AutomaticDecompression = _defaultDecompression;
                    httpWebRequest.Method = _methodOptions;

                    httpWebRequest.Credentials = GetCredentialCache(address, username, password);

                    using (HttpWebResponse httpWebResponse = await httpWebRequest.GetResponseAsync() as HttpWebResponse)
                    {
                        string allow = httpWebResponse.Headers[HttpResponseHeader.Allow];
                        if (!string.IsNullOrWhiteSpace(allow))
                        {
                            return allow.Split(ConfigurationLayer.FieldSplit, StringSplitOptions.RemoveEmptyEntries);
                        }
                    }
                }
            }
            catch (ProtocolViolationException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (NotSupportedException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (SecurityException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (NotImplementedException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (ArgumentNullException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (ArgumentException exc) { await ExceptionLayer.HandleAsync(exc); }

            return new string[] { };
        }

        public static async Task<string[]> OptionsCorsAsync(Uri address)
        {
            try
            {
                if (address.Scheme == Uri.UriSchemeHttps || address.Scheme == Uri.UriSchemeHttp)
                {
                    HttpWebRequest httpWebRequest = WebRequest.CreateHttp(address);
                    httpWebRequest.AutomaticDecompression = _defaultDecompression;
                    httpWebRequest.Headers["Access-Control-Request-Method"] = "POST";
                    httpWebRequest.Headers["Access-Control-Request-Headers"] = "X-PINGOTHER, Content-Type";
                    httpWebRequest.Method = _methodOptions;

                    using (HttpWebResponse httpWebResponse = await httpWebRequest.GetResponseAsync() as HttpWebResponse)
                    {
                        if (httpWebResponse.StatusCode == HttpStatusCode.OK)
                        {
                            string accessControlAllowMethods = httpWebResponse.Headers["Access-Control-Allow-Methods"];
                            if (!string.IsNullOrWhiteSpace(accessControlAllowMethods))
                            {
                                return accessControlAllowMethods.Split(ConfigurationLayer.FieldSplit, StringSplitOptions.RemoveEmptyEntries);
                            }
                        }
                    }
                }
            }
            catch (ProtocolViolationException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (NotSupportedException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (SecurityException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (NotImplementedException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (ArgumentNullException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (ArgumentException exc) { await ExceptionLayer.HandleAsync(exc); }

            return new string[] { };
        }

        public static async Task<string[]> OptionsCorsAsync(Uri address, string username, string password)
        {
            try
            {
                if (address.Scheme == Uri.UriSchemeHttps || address.Scheme == Uri.UriSchemeHttp)
                {
                    HttpWebRequest httpWebRequest = WebRequest.CreateHttp(address);
                    httpWebRequest.AutomaticDecompression = _defaultDecompression;
                    httpWebRequest.Headers["Access-Control-Request-Method"] = "POST";
                    httpWebRequest.Headers["Access-Control-Request-Headers"] = "X-PINGOTHER, Content-Type";
                    httpWebRequest.Method = _methodOptions;

                    httpWebRequest.Credentials = GetCredentialCache(address, username, password);

                    using (HttpWebResponse httpWebResponse = await httpWebRequest.GetResponseAsync() as HttpWebResponse)
                    {
                        if (httpWebResponse.StatusCode == HttpStatusCode.OK)
                        {
                            string accessControlAllowMethods = httpWebResponse.Headers["Access-Control-Allow-Methods"];
                            if (!string.IsNullOrWhiteSpace(accessControlAllowMethods))
                            {
                                return accessControlAllowMethods.Split(ConfigurationLayer.FieldSplit, StringSplitOptions.RemoveEmptyEntries);
                            }
                        }
                    }
                }
            }
            catch (ProtocolViolationException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (NotSupportedException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (SecurityException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (NotImplementedException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (ArgumentNullException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (ArgumentException exc) { await ExceptionLayer.HandleAsync(exc); }

            return new string[] { };
        }

        public static async Task<bool> PatchAsync(Uri address, string textData)
        {
            try
            {
                if (address.Scheme == Uri.UriSchemeHttps || address.Scheme == Uri.UriSchemeHttp)
                {
                    byte[] data = Encoding.UTF8.GetBytes(textData);

                    HttpWebRequest httpWebRequest = WebRequest.CreateHttp(address);
                    httpWebRequest.AutomaticDecompression = _defaultDecompression;
                    httpWebRequest.ContentType = _contentType;
                    httpWebRequest.ContentLength = data.LongLength;
                    httpWebRequest.Method = _methodPatch;

                    using (Stream stream = await httpWebRequest.GetRequestStreamAsync())
                    {
                        await stream.WriteAsync(data, _zeroIndex, data.Length);
                    }
                    using (HttpWebResponse httpWebResponse = await httpWebRequest.GetResponseAsync() as HttpWebResponse)
                    {
                        return httpWebResponse.StatusCode == HttpStatusCode.OK || httpWebResponse.StatusCode == HttpStatusCode.Created || httpWebResponse.StatusCode == HttpStatusCode.NoContent;
                    }
                }
            }
            catch (EncoderFallbackException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (NotSupportedException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (SecurityException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (IOException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (ArgumentOutOfRangeException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (ArgumentNullException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (ArgumentException exc) { await ExceptionLayer.HandleAsync(exc); }

            return false;
        }

        public static async Task<bool> PatchAsync(Uri address, string textData, string username, string password)
        {
            try
            {
                if (address.Scheme == Uri.UriSchemeHttps || address.Scheme == Uri.UriSchemeHttp)
                {
                    byte[] data = Encoding.UTF8.GetBytes(textData);

                    HttpWebRequest httpWebRequest = WebRequest.CreateHttp(address);
                    httpWebRequest.AutomaticDecompression = _defaultDecompression;
                    httpWebRequest.ContentType = _contentType;
                    httpWebRequest.ContentLength = data.LongLength;
                    httpWebRequest.Method = _methodPatch;

                    httpWebRequest.Credentials = GetCredentialCache(address, username, password);

                    using (Stream stream = await httpWebRequest.GetRequestStreamAsync())
                    {
                        await stream.WriteAsync(data, _zeroIndex, data.Length);
                    }
                    using (HttpWebResponse httpWebResponse = await httpWebRequest.GetResponseAsync() as HttpWebResponse)
                    {
                        return httpWebResponse.StatusCode == HttpStatusCode.OK || httpWebResponse.StatusCode == HttpStatusCode.Created || httpWebResponse.StatusCode == HttpStatusCode.NoContent;
                    }
                }
            }
            catch (EncoderFallbackException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (NotSupportedException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (SecurityException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (IOException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (ArgumentOutOfRangeException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (ArgumentNullException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (ArgumentException exc) { await ExceptionLayer.HandleAsync(exc); }

            return false;
        }

        public static async Task<Uri> PostAsync(Uri address, string textData)
        {
            try
            {
                if (address.Scheme == Uri.UriSchemeHttps || address.Scheme == Uri.UriSchemeHttp)
                {
                    byte[] data = Encoding.UTF8.GetBytes(textData);

                    HttpWebRequest httpWebRequest = WebRequest.CreateHttp(address);
                    httpWebRequest.AutomaticDecompression = _defaultDecompression;
                    httpWebRequest.ContentType = _contentType;
                    httpWebRequest.ContentLength = data.LongLength;
                    httpWebRequest.Method = _methodPost;

                    using (Stream stream = await httpWebRequest.GetRequestStreamAsync())
                    {
                        await stream.WriteAsync(data, _zeroIndex, data.Length);
                    }
                    using (HttpWebResponse httpWebResponse = await httpWebRequest.GetResponseAsync() as HttpWebResponse)
                    {
                        string location = httpWebResponse.Headers[HttpResponseHeader.Location];
                        Uri locationUri = Uri.TryCreate(location, UriKind.RelativeOrAbsolute, out locationUri) ? locationUri : address;

                        return locationUri;
                    }
                }
            }
            catch (EncoderFallbackException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (NotSupportedException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (SecurityException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (IOException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (ArgumentOutOfRangeException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (ArgumentNullException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (ArgumentException exc) { await ExceptionLayer.HandleAsync(exc); }

            return address;
        }

        public static async Task<Uri> PostAsync(Uri address, string textData, string username, string password)
        {
            try
            {
                if (address.Scheme == Uri.UriSchemeHttps || address.Scheme == Uri.UriSchemeHttp)
                {
                    byte[] data = Encoding.UTF8.GetBytes(textData);

                    HttpWebRequest httpWebRequest = WebRequest.CreateHttp(address);
                    httpWebRequest.AutomaticDecompression = _defaultDecompression;
                    httpWebRequest.ContentType = _contentType;
                    httpWebRequest.ContentLength = data.LongLength;
                    httpWebRequest.Method = _methodPost;

                    httpWebRequest.Credentials = GetCredentialCache(address, username, password);

                    using (Stream stream = await httpWebRequest.GetRequestStreamAsync())
                    {
                        await stream.WriteAsync(data, _zeroIndex, data.Length);
                    }
                    using (HttpWebResponse httpWebResponse = await httpWebRequest.GetResponseAsync() as HttpWebResponse)
                    {
                        string location = httpWebResponse.Headers[HttpResponseHeader.Location];
                        Uri locationUri = Uri.TryCreate(location, UriKind.RelativeOrAbsolute, out locationUri) ? locationUri : address;

                        return locationUri;
                    }
                }
            }
            catch (EncoderFallbackException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (NotSupportedException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (SecurityException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (IOException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (ArgumentOutOfRangeException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (ArgumentNullException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (ArgumentException exc) { await ExceptionLayer.HandleAsync(exc); }

            return address;
        }

        public static async Task<bool> PutAsync(Uri address, string textData)
        {
            try
            {
                if (address.Scheme == Uri.UriSchemeHttps || address.Scheme == Uri.UriSchemeHttp)
                {
                    byte[] data = Encoding.UTF8.GetBytes(textData);

                    HttpWebRequest httpWebRequest = WebRequest.CreateHttp(address);
                    httpWebRequest.AutomaticDecompression = _defaultDecompression;
                    httpWebRequest.ContentType = _contentType;
                    httpWebRequest.ContentLength = data.LongLength;
                    httpWebRequest.Method = _methodPut;

                    using (Stream stream = await httpWebRequest.GetRequestStreamAsync())
                    {
                        await stream.WriteAsync(data, _zeroIndex, data.Length);
                    }
                    using (HttpWebResponse httpWebResponse = await httpWebRequest.GetResponseAsync() as HttpWebResponse)
                    {
                        return httpWebResponse.StatusCode == HttpStatusCode.OK || httpWebResponse.StatusCode == HttpStatusCode.Created || httpWebResponse.StatusCode == HttpStatusCode.NoContent;
                    }
                }
            }
            catch (EncoderFallbackException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (NotSupportedException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (SecurityException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (IOException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (ArgumentOutOfRangeException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (ArgumentNullException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (ArgumentException exc) { await ExceptionLayer.HandleAsync(exc); }

            return false;
        }

        public static async Task<bool> PutAsync(Uri address, string textData, string username, string password)
        {
            try
            {
                if (address.Scheme == Uri.UriSchemeHttps || address.Scheme == Uri.UriSchemeHttp)
                {
                    byte[] data = Encoding.UTF8.GetBytes(textData);

                    HttpWebRequest httpWebRequest = WebRequest.CreateHttp(address);
                    httpWebRequest.AutomaticDecompression = _defaultDecompression;
                    httpWebRequest.ContentType = _contentType;
                    httpWebRequest.ContentLength = data.LongLength;
                    httpWebRequest.Method = _methodPut;

                    httpWebRequest.Credentials = GetCredentialCache(address, username, password);

                    using (Stream stream = await httpWebRequest.GetRequestStreamAsync())
                    {
                        await stream.WriteAsync(data, _zeroIndex, data.Length);
                    }
                    using (HttpWebResponse httpWebResponse = await httpWebRequest.GetResponseAsync() as HttpWebResponse)
                    {
                        return httpWebResponse.StatusCode == HttpStatusCode.OK || httpWebResponse.StatusCode == HttpStatusCode.Created || httpWebResponse.StatusCode == HttpStatusCode.NoContent;
                    }
                }
            }
            catch (EncoderFallbackException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (NotSupportedException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (SecurityException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (IOException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (ArgumentOutOfRangeException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (ArgumentNullException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (ArgumentException exc) { await ExceptionLayer.HandleAsync(exc); }

            return false;
        }

        public static async Task<string> TraceAsync(Uri address)
        {
            try
            {
                if (address.Scheme == Uri.UriSchemeHttps || address.Scheme == Uri.UriSchemeHttp)
                {
                    HttpWebRequest httpWebRequest = WebRequest.CreateHttp(address);
                    httpWebRequest.AutomaticDecompression = _defaultDecompression;
                    httpWebRequest.Method = _methodTrace;

                    using (HttpWebResponse httpWebResponse = await httpWebRequest.GetResponseAsync() as HttpWebResponse)
                    using (Stream stream = httpWebResponse.GetResponseStream())
                    using (StreamReader streamReader = new StreamReader(stream))
                    {
                        return await streamReader.ReadToEndAsync();
                    }
                }
            }
            catch (ProtocolViolationException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (WebException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (NotSupportedException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (SecurityException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (NotImplementedException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (ArgumentNullException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (ArgumentException exc) { await ExceptionLayer.HandleAsync(exc); }

            return HttpStatusCode.BadRequest.ToString();
        }

        public static async Task<string> TraceAsync(Uri address, string username, string password)
        {
            try
            {
                if (address.Scheme == Uri.UriSchemeHttps || address.Scheme == Uri.UriSchemeHttp)
                {
                    HttpWebRequest httpWebRequest = WebRequest.CreateHttp(address);
                    httpWebRequest.AutomaticDecompression = _defaultDecompression;
                    httpWebRequest.Method = _methodTrace;

                    httpWebRequest.Credentials = GetCredentialCache(address, username, password);

                    using (HttpWebResponse httpWebResponse = await httpWebRequest.GetResponseAsync() as HttpWebResponse)
                    using (Stream stream = httpWebResponse.GetResponseStream())
                    using (StreamReader streamReader = new StreamReader(stream))
                    {
                        return await streamReader.ReadToEndAsync();
                    }
                }
            }
            catch (ProtocolViolationException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (WebException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (NotSupportedException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (SecurityException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (NotImplementedException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (ArgumentNullException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (ArgumentException exc) { await ExceptionLayer.HandleAsync(exc); }

            return HttpStatusCode.BadRequest.ToString();
        }
        #endregion

        #region WebClient
        public static async Task<string> DownloadAsync(Uri address)
        {
            try
            {
                using (WebClient webClient = new WebClient())
                {
                    webClient.Encoding = ConfigurationLayer.DefaultEncoding;
                    return await webClient.DownloadStringTaskAsync(address);
                }
            }
            catch (ArgumentNullException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (WebException exc) { await ExceptionLayer.HandleAsync(exc); }

            return string.Empty;
        }

        public static async Task<string> DownloadAsync(Uri address, string username, string password)
        {
            try
            {
                using (WebClient webClient = new WebClient())
                {
                    webClient.Encoding = ConfigurationLayer.DefaultEncoding;
                    webClient.Headers.Add(HttpRequestHeader.Authorization, GetBasicAuthenticationHeader(username, password));
                    return await webClient.DownloadStringTaskAsync(address);
                }
            }
            catch (ArgumentNullException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (WebException exc) { await ExceptionLayer.HandleAsync(exc); }

            return string.Empty;
        }

        public static async Task<string> DownloadAsync(Uri baseAddress, Uri relativeAddress)
        {
            try
            {
                using (WebClient webClient = new WebClient())
                {
                    webClient.BaseAddress = baseAddress.AbsolutePath;
                    webClient.Encoding = ConfigurationLayer.DefaultEncoding;
                    return await webClient.DownloadStringTaskAsync(relativeAddress);
                }
            }
            catch (ArgumentNullException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (ArgumentException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (WebException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); }

            return string.Empty;
        }

        public static async Task<string> DownloadAsync(Uri baseAddress, Uri relativeAddress, string username, string password)
        {
            try
            {
                using (WebClient webClient = new WebClient())
                {
                    webClient.BaseAddress = baseAddress.AbsolutePath;
                    webClient.Encoding = ConfigurationLayer.DefaultEncoding;
                    webClient.Headers.Add(HttpRequestHeader.Authorization, GetBasicAuthenticationHeader(username, password));
                    return await webClient.DownloadStringTaskAsync(relativeAddress);
                }
            }
            catch (ArgumentNullException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (ArgumentException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (WebException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); }

            return string.Empty;
        }

        public static async Task<string> UploadAsync(Uri address, string data)
        {
            try
            {
                using (WebClient webClient = new WebClient())
                {
                    webClient.Encoding = ConfigurationLayer.DefaultEncoding;
                    return await webClient.UploadStringTaskAsync(address, data);
                }
            }
            catch (ArgumentNullException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (WebException exc) { await ExceptionLayer.HandleAsync(exc); }

            return string.Empty;
        }

        public static async Task<string> UploadAsync(Uri address, string data, string username, string password)
        {
            try
            {
                using (WebClient webClient = new WebClient())
                {
                    webClient.Encoding = ConfigurationLayer.DefaultEncoding;
                    webClient.Headers.Add(HttpRequestHeader.Authorization, GetBasicAuthenticationHeader(username, password));
                    return await webClient.UploadStringTaskAsync(address, data);
                }
            }
            catch (ArgumentNullException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (WebException exc) { await ExceptionLayer.HandleAsync(exc); }

            return string.Empty;
        }

        public static async Task<string> UploadAsync(Uri baseAddress, Uri relativeAddress, string data)
        {
            try
            {
                using (WebClient webClient = new WebClient())
                {
                    webClient.BaseAddress = baseAddress.AbsolutePath;
                    webClient.Encoding = ConfigurationLayer.DefaultEncoding;
                    return await webClient.UploadStringTaskAsync(relativeAddress, data);
                }
            }
            catch (ArgumentNullException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (ArgumentException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (WebException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); }

            return string.Empty;
        }

        public static async Task<string> UploadAsync(Uri baseAddress, Uri relativeAddress, string data, string username, string password)
        {
            try
            {
                using (WebClient webClient = new WebClient())
                {
                    webClient.BaseAddress = baseAddress.AbsolutePath;
                    webClient.Encoding = ConfigurationLayer.DefaultEncoding;
                    webClient.Headers.Add(HttpRequestHeader.Authorization, GetBasicAuthenticationHeader(username, password));
                    return await webClient.UploadStringTaskAsync(relativeAddress, data);
                }
            }
            catch (ArgumentNullException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (ArgumentException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (WebException exc) { await ExceptionLayer.HandleAsync(exc); }
            catch (InvalidOperationException exc) { await ExceptionLayer.HandleAsync(exc); }

            return string.Empty;
        }
        #endregion
    }
}