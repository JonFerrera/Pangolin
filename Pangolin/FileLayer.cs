using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Pangolin
{
    public static class FileLayer
    {
        private const short _maxPathLength = short.MaxValue;

        private const int _nullIndex = -1;

        private const string 
            _gZipFileExtension = ".gz",
            _zipFileExtension = ".zip";

        private static readonly char[]
            _invalidFileNameChars = Path.GetInvalidFileNameChars(),
            _invalidPathChars = Path.GetInvalidPathChars();
        
        private const string
            _exceptionTextFile = "Exception_Log_{0}.txt",
            _exceptionInternalTextFile = "Pangolin_Exception_Log_{0}.txt",
            _statusTextFile = "Status_Log_{0}.txt",
            _statusInternalTextFile = "Pangolin_Status_Log_{0}.txt";

        private static readonly XmlReaderSettings _xmlReaderSettings = new XmlReaderSettings()
        {
            Async = true,
            IgnoreComments = true,
            IgnoreWhitespace = true
        };
        private static readonly XmlWriterSettings _xmlWriterSettings = new XmlWriterSettings()
        {
            Async = true,
            Encoding = ConfigurationLayer.DefaultEncoding,
            Indent = true,
            IndentChars = ConfigurationLayer.Tab,
            NewLineChars = ConfigurationLayer.NewLine
        };

        #region Data URI
        public static async Task<string> GetDataURI(string path, CancellationToken cancellationToken)
        {
            if (path == null) { throw new ArgumentNullException(nameof(path)); }

            string dataUri = string.Empty;

            try
            {
                string extension = GetExtension(path);
                if (!string.IsNullOrWhiteSpace(extension))
                {
                    byte[] contentBytes = await ReadFileBytesAsync(path, cancellationToken);
                    string base64String = Convert.ToBase64String(contentBytes);
                    extension = extension.Replace(".", string.Empty);
                    dataUri = $"data:image/{extension};base64,{base64String}";
                }
            }
            catch (ArgumentNullException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (ArgumentException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }

            return dataUri;
        }
        #endregion
        
        #region File Compression
        #region GZip
        public static async Task<bool> GZipAsync(string fileName, CancellationToken cancellationToken)
        {
            if (fileName == null) { throw new ArgumentNullException(nameof(fileName)); }

            byte[] contentBytes = await ReadFileBytesAsync(fileName, cancellationToken);

            byte[] outputBytes = await TextLayer.GZipCompressAsync(contentBytes, cancellationToken);

            fileName += _gZipFileExtension;

            return await CreateFileBytesAsync(fileName, outputBytes, cancellationToken);
        }

        public static async Task<bool> UnGZipAsync(string fileName, CancellationToken cancellationToken)
        {
            if (fileName == null) { throw new ArgumentNullException(nameof(fileName)); }

            byte[] contentBytes = await ReadFileBytesAsync(fileName, cancellationToken);

            byte[] outputBytes = await TextLayer.GZipDecompressAsync(contentBytes, cancellationToken);

            return await CreateFileBytesAsync(fileName, outputBytes, cancellationToken);
        }
        #endregion

        #region Zip
        public static async Task ZipAsync(string sourcePath, string zipFile)
        {
            if (sourcePath == null) { throw new ArgumentNullException(nameof(sourcePath)); }
            if (zipFile == null) { throw new ArgumentNullException(nameof(zipFile)); }

            string[] sourceFiles = GetFiles(sourcePath);

            try
            {
                using (FileStream fileStreamZipArchive = new FileStream(zipFile, FileMode.Create))
                using (ZipArchive zipArchive = new ZipArchive(fileStreamZipArchive, ZipArchiveMode.Create))
                {
                    try
                    {
                        Uri baseUri = new Uri(sourcePath);

                        foreach (string sourceFile in sourceFiles)
                        {
                            Uri
                                relativeUri = new Uri(sourceFile),
                                zipArchiveEntryUri = baseUri.MakeRelativeUri(relativeUri);

                            string relativePath = TextLayer.UrlDecode(zipArchiveEntryUri.ToString());

                            ZipArchiveEntry zipArchiveEntry = zipArchive.CreateEntry(relativePath, CompressionLevel.Optimal);

                            using (Stream stream = zipArchiveEntry.Open())
                            using (FileStream fileStreamZipArchiveEntry = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                            {
                                await fileStreamZipArchiveEntry.CopyToAsync(stream);
                            }
                        }
                    }
                    catch (UriFormatException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
                    catch (InvalidOperationException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
                }
            }
            catch (NotSupportedException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (SecurityException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (FileNotFoundException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (DirectoryNotFoundException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (PathTooLongException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (IOException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (InvalidDataException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
        }

        public static async Task UnZipAsync(string zipFile, string extractPath)
        {
            if (zipFile == null) { throw new ArgumentNullException(nameof(zipFile)); }
            if (extractPath == null) { throw new ArgumentNullException(nameof(extractPath)); }

            try
            {
                extractPath = GetFullPath(extractPath);
            }
            catch (ArgumentException exc){ ExceptionLayer.CoreHandle(exc); throw; }
            catch (SecurityException exc){ ExceptionLayer.CoreHandle(exc); throw; }
            catch (NotSupportedException exc){ ExceptionLayer.CoreHandle(exc); throw; }
            catch (PathTooLongException exc){ ExceptionLayer.CoreHandle(exc); throw; }

            if (!extractPath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            {
                extractPath += Path.DirectorySeparatorChar;
            }
            
            try
            {
                Directory.CreateDirectory(extractPath);

                using (FileStream fileStreamZipArchive = new FileStream(zipFile, FileMode.Open))
                using (ZipArchive zipArchive = new ZipArchive(fileStreamZipArchive, ZipArchiveMode.Read))
                {
                    if (zipArchive.Entries?.Count > 0)
                    {
                        foreach (ZipArchiveEntry zipArchiveEntry in zipArchive.Entries)
                        {
                            string 
                                destinationPath = GetFullPath(Path.Combine(extractPath, zipArchiveEntry.FullName)),
                                directoryName = Path.GetDirectoryName(destinationPath);

                            if (destinationPath.StartsWith(extractPath, StringComparison.Ordinal))
                            {
                                Directory.CreateDirectory(directoryName);

                                using (Stream stream = zipArchiveEntry.Open())
                                using (FileStream fileStreamZipArchiveEntry = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None))
                                {
                                    await stream.CopyToAsync(fileStreamZipArchiveEntry);
                                }
                            }
                        }
                    }
                }
            }
            catch (UnauthorizedAccessException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (NotSupportedException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (SecurityException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (FileNotFoundException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (DirectoryNotFoundException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (PathTooLongException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (IOException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (InvalidDataException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
        }
        #endregion
        #endregion

        #region File Management
        public static string ChangeExtension(string path, string extension)
        {
            if (path == null) { throw new ArgumentNullException(nameof(path)); }
            if (extension == null) { throw new ArgumentNullException(nameof(extension)); }

            string modifiedPath = string.Empty;

            if (IsValidPath(path))
            {
                modifiedPath = Path.ChangeExtension(path, extension);
            }
            else
            {
                throw new InvalidOperationException($"{nameof(path)}: {path} is invalid.");
            }

            return modifiedPath;
        }

        public static string GetDirectoryName(string path)
        {
            if (path == null) { throw new ArgumentNullException(nameof(path)); }

            string directoryName = string.Empty;

            if (IsValidPath(path))
            {
                directoryName = Path.GetDirectoryName(path);
            }
            else
            {
                throw new InvalidOperationException($"{nameof(path)}: {path} is invalid.");
            }

            return directoryName;
        }

        public static string GetExtension(string path)
        {
            if (path == null) { throw new ArgumentNullException(nameof(path)); }

            string extension = string.Empty;

            if (IsValidPath(path))
            {
                extension = Path.GetExtension(path);
            }
            else
            {
                throw new InvalidOperationException($"{nameof(path)}: {path} is invalid.");
            }

            return extension;
        }

        public static string GetFileName(string path)
        {
            if (path == null) { throw new ArgumentNullException(nameof(path)); }

            string fileName = string.Empty;

            if (IsValidPath(path))
            {
                fileName = Path.GetFileName(path);
            }
            else
            {
                throw new InvalidOperationException($"{nameof(path)}: {path} is invalid.");
            }

            return fileName;
        }

        public static string GetFileNameWithoutExtension(string path)
        {
            if (path == null) { throw new ArgumentNullException(nameof(path)); }

            string fileNameWithoutExtension = string.Empty;

            if (IsValidPath(path))
            {
                fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
            }
            else
            {
                throw new InvalidOperationException($"{nameof(path)}: {path} is invalid.");
            }

            return fileNameWithoutExtension;
        }

        public static string[] GetFiles(string path)
        {
            string[] files = null;

            if (IsDirectory(path))
            {
                try
                {
                    files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
                }
                catch (PathTooLongException exc){ ExceptionLayer.CoreHandle(exc); throw; }
                catch (DirectoryNotFoundException exc){ ExceptionLayer.CoreHandle(exc); throw; }
                catch (IOException exc){ ExceptionLayer.CoreHandle(exc); throw; }
                catch (UnauthorizedAccessException exc){ ExceptionLayer.CoreHandle(exc); throw; }
            }
            else
            {
                files = new string[] { path };
            }

            return files;
        }

        public static string GetFullPath(string path)
        {
            if (path == null) { throw new ArgumentNullException(nameof(path)); }

            string fullPath = string.Empty;
            
            if (IsValidPath(path))
            {
                try
                {
                    fullPath = Path.GetFullPath(path);
                }
                catch (ArgumentException exc){ ExceptionLayer.CoreHandle(exc); throw; }
                catch (NotSupportedException exc){ ExceptionLayer.CoreHandle(exc); throw; }
                catch (SecurityException exc){ ExceptionLayer.CoreHandle(exc); throw; }
            }
            else
            {
                throw new InvalidOperationException($"{nameof(path)}: {path} is invalid.");
            }

            return fullPath;
        }

        public static bool IsDirectory(string path)
        {
            if (path == null) { throw new ArgumentNullException(nameof(path)); }

            try
            {
                FileAttributes fileAttributes = File.GetAttributes(path);

                return fileAttributes.HasFlag(FileAttributes.Directory);
            }
            catch (PathTooLongException exc){ ExceptionLayer.CoreHandle(exc); throw; }
            catch (NotSupportedException exc){ ExceptionLayer.CoreHandle(exc); throw; }
            catch (FileNotFoundException exc){ ExceptionLayer.CoreHandle(exc); throw; }
            catch (DirectoryNotFoundException exc){ ExceptionLayer.CoreHandle(exc); throw; }
            catch (IOException exc){ ExceptionLayer.CoreHandle(exc); throw; }
            catch (UnauthorizedAccessException exc){ ExceptionLayer.CoreHandle(exc); throw; }
        }

        public static bool IsValidPath(string path)
        {
            bool
                isValidDirectoryPath = false,
                isValidFileName = false;

            string
                directoryPath = string.Empty,
                fileName = string.Empty;

            bool isValidLength = path?.Length < _maxPathLength;
            
            if (isValidLength)
            {
                try
                {
                    directoryPath = Path.GetDirectoryName(path);
                    fileName = Path.GetFileName(path);
                }
                catch (ArgumentException exc){ ExceptionLayer.CoreHandle(exc); throw; }

                isValidDirectoryPath = directoryPath.IndexOfAny(_invalidPathChars) == _nullIndex;
                isValidFileName = fileName.IndexOfAny(_invalidFileNameChars) == _nullIndex;
            }

            return isValidLength && isValidDirectoryPath && isValidFileName;
        }
        #endregion

        #region File - XML
        public static bool CreateXMLFile(string fileName, XmlDocument xmlDocument)
        {
            if (fileName == null) { throw new ArgumentNullException(nameof(fileName)); }

            bool isCreated = false;

            string fullFileName = GetFullPath(fileName);

            if (!string.IsNullOrWhiteSpace(fullFileName))
            {
                using (XmlWriter xmlWriter = XmlWriter.Create(fullFileName, _xmlWriterSettings))
                {
                    xmlDocument.WriteContentTo(xmlWriter);
                    isCreated = true;
                }
            }
            return isCreated;
        }

        public static async Task<bool> CreateXMLFileAsync(string fileName, string content)
        {
            if (fileName == null) { throw new ArgumentNullException(nameof(fileName)); }

            bool isCreated = false;

            string fullFileName = GetFullPath(fileName);

            if (!string.IsNullOrWhiteSpace(fullFileName))
            {
                using (XmlWriter xmlWriter = XmlWriter.Create(fullFileName, _xmlWriterSettings))
                {
                    try
                    {
                        await xmlWriter.WriteRawAsync(content);
                        isCreated = true;
                    }
                    catch (InvalidOperationException exc) { await ExceptionLayer.CoreHandleAsync(exc); }
                }
            }

            return isCreated;
        }
        
        public static XmlDocument ReadXMLFile(string fileName)
        {
            if (fileName == null) { throw new ArgumentNullException(nameof(fileName)); }

            string fullFileName = GetFullPath(fileName);
            XmlDocument xmlDocument = new XmlDocument();

            if (!string.IsNullOrWhiteSpace(fullFileName))
            {
                try
                {
                    using (XmlReader xmlReader = XmlReader.Create(fullFileName, _xmlReaderSettings))
                    {
                        xmlDocument.Load(xmlReader);
                    }
                }
                catch (XmlException exc){ ExceptionLayer.CoreHandle(exc); }
                catch (UriFormatException exc){ ExceptionLayer.CoreHandle(exc); }
                catch (FileNotFoundException exc){ ExceptionLayer.CoreHandle(exc); }
            }

            return xmlDocument;
        }
        #endregion

        #region I/O
        #region File
        public static async Task<bool> AppendFileAsync(string fileName, string content)
        {
            if (fileName == null) { throw new ArgumentNullException(nameof(fileName)); }
            if (content == null) { throw new ArgumentNullException(nameof(content)); }

            return await WriteFileAsync(fileName, content, FileMode.Append, FileAccess.Write, FileShare.Read);
        }

        public static async Task<bool> AppendFileAsync(string fileName, byte[] contentBytes, CancellationToken cancellationToken)
        {
            if (fileName == null) { throw new ArgumentNullException(nameof(fileName)); }
            if (contentBytes == null) { throw new ArgumentNullException(nameof(contentBytes)); }

            return await WriteFileAsync(fileName, contentBytes, FileMode.Append, FileAccess.Write, FileShare.Read, cancellationToken);
        }

        public static bool CopyFile(string sourceFileName, string destinationFileName, bool overwrite = true)
        {
            if (sourceFileName == null) { throw new ArgumentNullException(nameof(sourceFileName)); }
            if (destinationFileName == null) { throw new ArgumentNullException(nameof(destinationFileName)); }

            bool isCopied = false;

            string fullSourceFileName = GetFullPath(sourceFileName);
            string fullDestinationFileName = GetFullPath(destinationFileName);

            if (string.IsNullOrWhiteSpace(fullSourceFileName))
            {
                throw new InvalidOperationException($"{nameof(sourceFileName)} resulted in an invalid path.");
            }
            if (string.IsNullOrWhiteSpace(fullDestinationFileName))
            {
                throw new InvalidOperationException($"{nameof(destinationFileName)} resulted in an invalid path.");
            }

            try
            {
                File.Copy(fullSourceFileName, fullDestinationFileName, overwrite);
                isCopied = true;
            }
            catch (DirectoryNotFoundException exc){ ExceptionLayer.CoreHandle(exc); throw; }
            catch (IOException exc){ ExceptionLayer.CoreHandle(exc); throw; }
            catch (UnauthorizedAccessException exc){ ExceptionLayer.CoreHandle(exc); throw; }
            catch (NotSupportedException exc){ ExceptionLayer.CoreHandle(exc); throw; }

            return isCopied;
        }

        public static async Task<bool> CreateFileAsync(string fileName, string content)
        {
            if (fileName == null) { throw new ArgumentNullException(nameof(fileName)); }
            if (content == null) { throw new ArgumentNullException(nameof(content)); }

            return await WriteFileAsync(fileName, content, FileMode.Create, FileAccess.Write, FileShare.Read);
        }

        public static async Task<bool> CreateFileBytesAsync(string fileName, byte[] contentBytes, CancellationToken cancellationToken)
        {
            if (fileName == null) { throw new ArgumentNullException(nameof(fileName)); }
            if (contentBytes == null) { throw new ArgumentNullException(nameof(contentBytes)); }

            return await WriteFileAsync(fileName, contentBytes, FileMode.Create, FileAccess.Write, FileShare.Read, cancellationToken);
        }

        public static bool DeleteFile(string fileName)
        {
            if (fileName == null) { throw new ArgumentNullException(nameof(fileName)); }

            bool isDeleted = false;

            string fullFileName = GetFullPath(fileName);

            if (string.IsNullOrWhiteSpace(fullFileName))
            {
                throw new InvalidOperationException("fileName resulted in an invalid path.");
            }
            else if (!File.Exists(fullFileName))
            {
                throw new InvalidOperationException("The file does not exist, so it cannot be deleted.");
            }

            try
            {
                File.Delete(fullFileName);
                isDeleted = true;
            }
            catch (IOException exc){ ExceptionLayer.CoreHandle(exc); throw; }
            catch (NotSupportedException exc){ ExceptionLayer.CoreHandle(exc); throw; }
            catch (UnauthorizedAccessException exc){ ExceptionLayer.CoreHandle(exc); throw; }

            return isDeleted;
        }

        public static bool FileExists(string fileName)
        {
            if (fileName == null) { throw new ArgumentNullException(nameof(fileName)); }

            string fullFileName = GetFullPath(fileName);

            if (string.IsNullOrWhiteSpace(fullFileName))
            {
                throw new InvalidOperationException("fileName resulted in an invalid path.");
            }

            return File.Exists(fullFileName);
        }

        public static bool MoveFile(string sourceFileName, string destinationFileName)
        {
            if (sourceFileName == null) { throw new ArgumentNullException(nameof(sourceFileName)); }
            if (destinationFileName == null) { throw new ArgumentNullException(nameof(destinationFileName)); }

            bool isMoved = false;

            string fullSourceFileName = GetFullPath(sourceFileName);
            string fullDestinationFileName = GetFullPath(destinationFileName);

            if (string.IsNullOrWhiteSpace(fullSourceFileName))
            {
                throw new InvalidOperationException($"{nameof(sourceFileName)} resulted in an invalid path.");
            }
            if (string.IsNullOrWhiteSpace(fullDestinationFileName))
            {
                throw new InvalidOperationException($"{nameof(destinationFileName)} resulted in an invalid path.");
            }

            try
            {
                File.Move(fullSourceFileName, fullDestinationFileName);
                isMoved = true;
            }
            catch (DirectoryNotFoundException exc){ ExceptionLayer.CoreHandle(exc); throw; }
            catch (IOException exc){ ExceptionLayer.CoreHandle(exc); throw; }
            catch (UnauthorizedAccessException exc){ ExceptionLayer.CoreHandle(exc); throw; }
            catch (NotSupportedException exc){ ExceptionLayer.CoreHandle(exc); throw; }

            return isMoved;
        }
        
        public static async Task<string> ReadFileAsync(string fileName, CancellationToken cancellationToken)
        {
            if (fileName == null) { throw new ArgumentNullException(nameof(fileName)); }

            return await ReadFileAsync(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, cancellationToken);
        }

        private static async Task<string> ReadFileAsync(string fileName, FileMode fileMode, FileAccess fileAccess, FileShare fileShare, CancellationToken cancellationToken)
        {
            if (fileName == null) { throw new ArgumentNullException(nameof(fileName)); }

            string fullFileName = GetFullPath(fileName);

            string contents = null;

            if (!string.IsNullOrWhiteSpace(fullFileName) && File.Exists(fullFileName))
            {
                try
                {
                    using (FileStream fileStream = new FileStream(fullFileName, fileMode, fileAccess, fileShare))
                    using (StreamReader streamReader = new StreamReader(fileStream))
                    {
                        contents = await streamReader.ReadToEndAsync();
                    }
                }
                catch (PathTooLongException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
                catch (NotSupportedException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
                catch (SecurityException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
                catch (UnauthorizedAccessException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            }

            return contents;
        }

        public static async Task<byte[]> ReadFileBytesAsync(string fileName, CancellationToken cancellationToken)
        {
            if (fileName == null) { throw new ArgumentNullException(nameof(fileName)); }

            return await ReadFileBytesAsync(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, cancellationToken);
        }

        private static async Task<byte[]> ReadFileBytesAsync(string fileName, FileMode fileMode, FileAccess fileAccess, FileShare fileShare, CancellationToken cancellationToken)
        {
            if (fileName == null) { throw new ArgumentNullException(nameof(fileName)); }

            string fullFileName = GetFullPath(fileName);

            byte[] contentBytes = null;

            if (!string.IsNullOrWhiteSpace(fullFileName) && File.Exists(fullFileName))
            {
                try
                {
                    using (FileStream fileStream = new FileStream(fullFileName, fileMode, fileAccess, fileShare))
                    {
                        contentBytes = new byte[fileStream.Length];
                        await fileStream.ReadAsync(contentBytes, 0, contentBytes.Length, cancellationToken);
                    }
                }
                catch (PathTooLongException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
                catch (NotSupportedException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
                catch (SecurityException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
                catch (UnauthorizedAccessException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            }

            return contentBytes;
        }
        
        public static async Task<string[]> ReadFileLinesAsync(string fileName)
        {
            if (fileName == null) { throw new ArgumentNullException(nameof(fileName)); }

            string fullFileName = GetFullPath(fileName);

            string[] fileLines = null;

            if (!string.IsNullOrWhiteSpace(fullFileName) && File.Exists(fullFileName))
            {
                List<string> lines = new List<string>();
                string line = string.Empty;

                try
                {
                    using (StreamReader streamReader = File.OpenText(fullFileName))
                    {
                        while ((line = await streamReader.ReadLineAsync()) != null)
                        {
                            lines.Add(line);
                        }
                    }
                    
                }
                catch (PathTooLongException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
                catch (NotSupportedException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
                catch (SecurityException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
                catch (UnauthorizedAccessException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }

                fileLines = lines.ToArray();
            }

            return fileLines;
        }

        private static async Task<bool> WriteFileAsync(string fileName, string content, FileMode fileMode, FileAccess fileAccess, FileShare fileShare)
        {
            if (fileName == null) { throw new ArgumentNullException(nameof(fileName)); }
            if (content == null) { throw new ArgumentNullException(nameof(content)); }

            string fullFileName = GetFullPath(fileName);

            if (string.IsNullOrWhiteSpace(fullFileName))
            {
                throw new InvalidOperationException($"{nameof(fileName)} resulted in an invalid file path.");
            }

            bool isWritten = false;

            try
            {
                using (FileStream fileStream = new FileStream(fullFileName, fileMode, fileAccess, fileShare))
                using (StreamWriter streamWriter = new StreamWriter(fileStream))
                {
                    await streamWriter.WriteLineAsync(content);
                }

                isWritten = true;
            }
            catch (PathTooLongException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (NotSupportedException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (SecurityException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (UnauthorizedAccessException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (FileNotFoundException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (IOException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }

            return isWritten;
        }

        private static async Task<bool> WriteFileAsync(string fileName, byte[] contentBytes, FileMode fileMode, FileAccess fileAccess, FileShare fileShare, CancellationToken cancellationToken)
        {
            if (fileName == null) { throw new ArgumentNullException(nameof(fileName)); }
            if (contentBytes == null) { throw new ArgumentNullException(nameof(contentBytes)); }

            string fullFileName = GetFullPath(fileName);

            if (string.IsNullOrWhiteSpace(fullFileName))
            {
                throw new InvalidOperationException($"{nameof(fileName)} resulted in an invalid file path.");
            }

            bool isWritten = false;

            try
            {
                using (FileStream fileStream = new FileStream(fullFileName, fileMode, fileAccess, fileShare))
                {
                    await fileStream.WriteAsync(contentBytes, 0, contentBytes.Length, cancellationToken);
                }

                isWritten = true;
            }
            catch (PathTooLongException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (NotSupportedException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (SecurityException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (UnauthorizedAccessException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (FileNotFoundException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (IOException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }

            return isWritten;
        }
        #endregion

        #region Directory
        public static bool CreateDirectory(string path)
        {
            if (path == null) { throw new ArgumentNullException(nameof(path)); }

            bool isCreated = false;

            if (IsValidPath(path) && !Directory.Exists(path))
            {
                try
                {
                    Directory.CreateDirectory(path);
                    isCreated = true;
                }
                catch (IOException exc){ ExceptionLayer.CoreHandle(exc); throw; } /* The directory specified by path is a file or the network name is not known. */
                catch (UnauthorizedAccessException exc){ ExceptionLayer.CoreHandle(exc); throw; } /* The caller does not have the required permission. */
                catch (NotSupportedException exc){ ExceptionLayer.CoreHandle(exc); throw; } /* Path contains a colon character (:) that is not part of a drive label. */
            }

            return isCreated;
        }
        #endregion
        #endregion

        #region Log
        internal static async Task LogExceptionCoreAsync(string exceptionText)
        {
            try
            {
                string exceptionFile = string.Format(_exceptionInternalTextFile, DiagnosticsLayer.TimestampLongFormatFile);
                await LogExceptionAsync(exceptionFile, exceptionText);
            }
            catch (FormatException) { }
            catch (ArgumentNullException) { }
            catch (ArgumentOutOfRangeException) { }
        }

        internal static async Task LogStatusCoreAsync(string message)
        {
            try
            {
                string statusFile = string.Format(_statusInternalTextFile, DiagnosticsLayer.TimestampLongFormatFile);
                await LogStatusAsync(statusFile, message);
            }
            catch (FormatException) { }
            catch (ArgumentNullException) { }
            catch (ArgumentOutOfRangeException) { }
        }

        public static async Task LogExceptionAsync(string exceptionText)
        {
            try
            {
                string exceptionFile = string.Format(_exceptionTextFile, DiagnosticsLayer.TimestampLongFormatFile);
                await LogExceptionAsync(exceptionFile, exceptionText);
            }
            catch (FormatException) { }
            catch (ArgumentNullException) { }
            catch (ArgumentOutOfRangeException) { }
        }

        public static async Task LogStatusAsync(string message)
        {
            try
            {
                string statusFile = string.Format(_statusTextFile, DiagnosticsLayer.TimestampLongFormatFile);
                await LogStatusAsync(statusFile, message);
            }
            catch (FormatException) { }
            catch (ArgumentNullException) { }
            catch (ArgumentOutOfRangeException) { }
        }

        private static async Task LogExceptionAsync(string exceptionFile, string exceptionText)
        {
            try
            {
                string logFile = Path.Combine(Directory.GetCurrentDirectory(), exceptionFile);
                await AppendFileAsync(logFile, exceptionText);
            }
            catch (NotSupportedException) { }
            catch (UnauthorizedAccessException) { }
            catch (ArgumentNullException) { }
            catch (ArgumentException) { }
        }

        private static async Task LogStatusAsync(string statusFile, string message)
        {
            try
            {
                string logFile = Path.Combine(Directory.GetCurrentDirectory(), statusFile);

                message += $" [{DiagnosticsLayer.TimestampLongFormat}]";

                await AppendFileAsync(logFile, message);
            }
            catch (NotSupportedException) { }
            catch (UnauthorizedAccessException) { }
            catch (ArgumentNullException) { }
            catch (ArgumentException) { }
        }
        #endregion
    }
}