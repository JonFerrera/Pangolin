using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Pangolin
{
    public static class CryptographyLayer
    {
        private const char _delimiter = '|';

        private const int _blockBitSize = 128;
        private const int _hashBitSize = 512;
        private const int _keyBitSize = 256;
        private const int _saltBitSize = 512;
        private const int _iterations = 10000;

        private const int _rsaKeySize = 2048;

        private const int _bitsPerByte = 8;

        private static readonly int _blockByteSize = _blockBitSize / _bitsPerByte;
        private static readonly int _hashByteSize = _hashBitSize / _bitsPerByte;
        private static readonly int _keyByteSize = _keyBitSize / _bitsPerByte;
        private static readonly int _saltByteSize = _saltBitSize / _bitsPerByte;

        #region File Checksums
        public static bool IsMD5ChecksumValid(string fileName, string checksum)
        {
            if (fileName == null) { throw new ArgumentNullException(nameof(fileName)); }
            if (checksum == null) { throw new ArgumentNullException(nameof(checksum)); }

            return string.Equals(checksum, MD5Checksum(fileName));
        }

        public static bool IsSHA1ChecksumValid(string fileName, string checksum)
        {
            if (fileName == null) { throw new ArgumentNullException(nameof(fileName)); }
            if (checksum == null) { throw new ArgumentNullException(nameof(checksum)); }

            return string.Equals(checksum, SHA1Checksum(fileName));
        }

        public static bool IsSHA256ChecksumValid(string fileName, string checksum)
        {
            if (fileName == null) { throw new ArgumentNullException(nameof(fileName)); }
            if (checksum == null) { throw new ArgumentNullException(nameof(checksum)); }

            return string.Equals(checksum, SHA256Checksum(fileName));
        }

        public static string MD5Checksum(string fileName)
        {
            if (fileName == null) { throw new ArgumentNullException(nameof(fileName)); }
            if (!File.Exists(fileName)) { throw new InvalidOperationException(fileName + " does not exist."); }

            string checkSum = string.Empty;

            try
            {
                using (FileStream fileStream = File.OpenRead(fileName))
                {
                    try
                    {
                        using (MD5 md5 = MD5.Create())
                        {
                            checkSum = Encoding.Default.GetString(md5.ComputeHash(fileStream));
                        }
                    }
                    catch (DecoderFallbackException exc) { ExceptionLayer.Handle(exc); throw; }
                    catch (TargetInvocationException exc) { ExceptionLayer.Handle(exc); throw; }
                }
            }
            catch (PathTooLongException exc) { ExceptionLayer.Handle(exc); throw; }
            catch (DirectoryNotFoundException exc) { ExceptionLayer.Handle(exc); throw; }
            catch (UnauthorizedAccessException exc) { ExceptionLayer.Handle(exc); throw; }
            catch (NotSupportedException exc) { ExceptionLayer.Handle(exc); throw; }
            catch (IOException exc) { ExceptionLayer.Handle(exc); throw; }

            return checkSum;
        }

        public static string SHA1Checksum(string fileName)
        {
            if (fileName == null) { throw new ArgumentNullException(nameof(fileName)); }
            if (!File.Exists(fileName)) { throw new InvalidOperationException(fileName + " does not exist."); }

            string checkSum = string.Empty;

            try
            {
                using (FileStream fileStream = File.OpenRead(fileName))
                {
                    try
                    {
                        using (SHA1 sha1 = SHA1.Create())
                        {
                            checkSum = Encoding.Default.GetString(sha1.ComputeHash(fileStream));
                        }
                    }
                    catch (DecoderFallbackException exc) { ExceptionLayer.Handle(exc); throw; }
                    catch (TargetInvocationException exc) { ExceptionLayer.Handle(exc); throw; }
                }
            }
            catch (PathTooLongException exc) { ExceptionLayer.Handle(exc); throw; }
            catch (DirectoryNotFoundException exc) { ExceptionLayer.Handle(exc); throw; }
            catch (UnauthorizedAccessException exc) { ExceptionLayer.Handle(exc); throw; }
            catch (NotSupportedException exc) { ExceptionLayer.Handle(exc); throw; }
            catch (IOException exc) { ExceptionLayer.Handle(exc); throw; }

            return checkSum;
        }

        public static string SHA256Checksum(string fileName)
        {
            if (fileName == null) { throw new ArgumentNullException(nameof(fileName)); }
            if (!File.Exists(fileName)) { throw new InvalidOperationException(fileName + " does not exist."); }

            string checkSum = string.Empty;

            try
            {
                using (FileStream fileStream = File.OpenRead(fileName))
                {
                    try
                    {
                        using (SHA256 sha256 = SHA256.Create())
                        {
                            checkSum = Encoding.Default.GetString(sha256.ComputeHash(fileStream));
                        }
                    }
                    catch (DecoderFallbackException exc) { ExceptionLayer.Handle(exc); throw; }
                    catch (TargetInvocationException exc) { ExceptionLayer.Handle(exc); throw; }
                }
            }
            catch (PathTooLongException exc) { ExceptionLayer.Handle(exc); throw; }
            catch (DirectoryNotFoundException exc) { ExceptionLayer.Handle(exc); throw; }
            catch (UnauthorizedAccessException exc) { ExceptionLayer.Handle(exc); throw; }
            catch (NotSupportedException exc) { ExceptionLayer.Handle(exc); throw; }
            catch (IOException exc) { ExceptionLayer.Handle(exc); throw; }

            return checkSum;
        }
        #endregion

        #region HTTP Authentication Hashing
        public static async Task<string> MD5Hash(string plainText)
        {
            if (plainText == null) { throw new ArgumentNullException(nameof(plainText)); }

            string hash = string.Empty;

            byte[] md5Bytes = null;
            try
            {
                byte[] asciiBytes = Encoding.ASCII.GetBytes(plainText);

                using (MD5CryptoServiceProvider md5CryptoServiceProvider = new MD5CryptoServiceProvider())
                {
                    md5Bytes = md5CryptoServiceProvider.ComputeHash(asciiBytes);
                }
            }
            catch (EncoderFallbackException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }

            try
            {
                StringBuilder stringBuilder = new StringBuilder();
                for (int i = 0; i < md5Bytes?.Length; i++)
                {
                    stringBuilder.Append(md5Bytes[i].ToString("X2"));
                }
                hash = stringBuilder.ToString();
            }
            catch (OverflowException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (FormatException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (ArgumentOutOfRangeException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }

            return hash;
        }

        public static async Task<string> SHA256Hash(string plainText)
        {
            if (plainText == null) { throw new ArgumentNullException(nameof(plainText)); }

            string hash = string.Empty;

            try
            {
                byte[] plainBytes = Encoding.ASCII.GetBytes(plainText);
                using (SHA256CryptoServiceProvider sha256CryptoServiceProvider = new SHA256CryptoServiceProvider())
                {
                    byte[] sha256Bytes = sha256CryptoServiceProvider.ComputeHash(plainBytes);
                    StringBuilder stringBuilder = new StringBuilder();
                    for (int i = 0; i < sha256Bytes?.Length; i++)
                    {
                        stringBuilder.Append(sha256Bytes[i].ToString("X2"));
                    }
                    hash = stringBuilder.ToString();
                }
            }
            catch (EncoderFallbackException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (FormatException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (ArgumentOutOfRangeException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }

            return hash;
        }

        public static async Task<string> SHA512Hash(string plainText)
        {
            if (plainText == null) { throw new ArgumentNullException(nameof(plainText)); }

            string hash = string.Empty;

            try
            {
                byte[] plainBytes = Encoding.ASCII.GetBytes(plainText);
                using (SHA512CryptoServiceProvider sha512CryptoServiceProvider = new SHA512CryptoServiceProvider())
                {
                    byte[] sha512Bytes = sha512CryptoServiceProvider.ComputeHash(plainBytes);
                    StringBuilder stringBuilder = new StringBuilder();
                    for (int i = 0; i < sha512Bytes?.Length; i++)
                    {
                        stringBuilder.Append(sha512Bytes[i].ToString("X2"));
                    }
                    hash = stringBuilder.ToString();
                }
            }
            catch (EncoderFallbackException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (FormatException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (ArgumentOutOfRangeException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }

            return hash;
        }
        #endregion

        #region Password Hashing
        public static async Task<string> HashPasswordAsync(string password, CancellationToken cancellationToken)
        {
            if (password == null) { throw new ArgumentNullException(nameof(password)); }

            string hash = string.Empty;

            byte[] saltBytes = new byte[_saltByteSize];
            FillBytes(saltBytes);

            byte[] hashBytes = await GetPbkdf2Bytes(password, saltBytes, _iterations, _hashByteSize);
            byte[] saltHashBytes = new byte[_saltByteSize + _hashByteSize];

            try
            {
                using (MemoryStream memoryStream = new MemoryStream(_saltByteSize + _hashByteSize))
                {
                    await memoryStream.WriteAsync(saltBytes, 0, _saltByteSize, cancellationToken);
                    await memoryStream.WriteAsync(hashBytes, 0, _hashByteSize, cancellationToken);
                    await memoryStream.FlushAsync(cancellationToken);

                    hash = Convert.ToBase64String(memoryStream.ToArray());
                }
            }
            catch (ArgumentNullException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (ArgumentOutOfRangeException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (ArgumentException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (NotSupportedException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (InvalidOperationException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }

            return hash;
        }

        public static async Task<string> HashAuthenticatePasswordAsync(string password, byte[] macKey, CancellationToken cancellationToken)
        {
            if (password == null) { throw new ArgumentNullException(nameof(password)); }
            if (macKey == null) { throw new ArgumentNullException(nameof(macKey)); }

            string hash = string.Empty;

            byte[] saltBytes = new byte[_saltByteSize];
            FillBytes(saltBytes);

            byte[] hashBytes = await GetPbkdf2Bytes(password, saltBytes, _iterations, _hashByteSize);

            try
            {
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    await memoryStream.WriteAsync(saltBytes, 0, _saltByteSize, cancellationToken);
                    await memoryStream.WriteAsync(hashBytes, 0, _hashByteSize, cancellationToken);
                    await memoryStream.FlushAsync(cancellationToken);

                    byte[] sentTag = null;
                    using (HMACSHA512 hmacSha512 = new HMACSHA512(macKey))
                    {
                        sentTag = hmacSha512.ComputeHash(memoryStream.ToArray());
                    }

                    await memoryStream.WriteAsync(sentTag, 0, sentTag.Length, cancellationToken);
                    await memoryStream.FlushAsync(cancellationToken);

                    hash = Convert.ToBase64String(memoryStream.ToArray());
                }
            }
            catch (ArgumentNullException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (ArgumentOutOfRangeException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (ArgumentException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (NotSupportedException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (InvalidOperationException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }

            return hash;
        }

        private static async Task<byte[]> GetPbkdf2Bytes(string password, byte[] salt, int iterations, int outputBytes)
        {
            if (password == null) { throw new ArgumentNullException(nameof(password)); }
            if (salt == null) { throw new ArgumentNullException(nameof(salt)); }
            else if (salt.Length < 8) { throw new ArgumentException($"{nameof(salt)} cannot be smaller than eight bytes.", nameof(salt)); }
            if (iterations < 1) { throw new ArgumentOutOfRangeException(nameof(iterations), iterations, $"{nameof(iterations)} cannot be less than one."); }

            byte[] bytes = new byte[] { };

            try
            {
                using (Rfc2898DeriveBytes rfc2898 = new Rfc2898DeriveBytes(password, salt) { IterationCount = iterations })
                {
                    bytes = rfc2898.GetBytes(outputBytes);
                }
            }
            catch (ArgumentOutOfRangeException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (ArgumentException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }

            return bytes;
        }

        public static async Task<bool> ValidatePasswordHashAsync(string password, string storedHashSalt)
        {
            if (password == null) { throw new ArgumentNullException(nameof(password)); }
            if (storedHashSalt == null) { throw new ArgumentNullException(nameof(storedHashSalt)); }

            bool isValid = false;

            try
            {
                byte[] storedHashSaltBytes = Convert.FromBase64String(storedHashSalt);

                byte[] storedHashBytes = new byte[_saltByteSize];
                Array.Copy(storedHashSaltBytes, _saltByteSize, storedHashBytes, 0, _hashByteSize);

                byte[] storedSaltBytes = new byte[_hashByteSize];
                Array.Copy(storedHashSaltBytes, 0, storedSaltBytes, 0, _saltByteSize);

                byte[] hashBytes = await GetPbkdf2Bytes(password, storedSaltBytes, _iterations, _hashByteSize);

                isValid = SlowEquals(hashBytes, storedHashBytes);
            }
            catch (FormatException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (ArgumentNullException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (ArgumentOutOfRangeException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (ArgumentException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (RankException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (ArrayTypeMismatchException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (InvalidCastException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }

            return isValid;
        }

        public static async Task<bool> ValidateAuthenticatePasswordHashAsync(string password, string storedHashSalt, byte[] macKey)
        {
            if (password == null) { throw new ArgumentNullException(nameof(password)); }
            if (storedHashSalt == null) { throw new ArgumentNullException(nameof(storedHashSalt)); }
            if (macKey == null) { throw new ArgumentNullException(nameof(macKey)); }

            bool isValid = false;

            try
            {
                byte[] encryptedBytes = Convert.FromBase64String(storedHashSalt);
                byte[] sentTag = null, calcTag = null;
                using (HMACSHA512 hmacSha512 = new HMACSHA512(macKey))
                {
                    sentTag = new byte[hmacSha512.HashSize / _bitsPerByte];
                    calcTag = hmacSha512.ComputeHash(encryptedBytes, 0, encryptedBytes.Length - sentTag.Length);
                }

                Array.Copy(encryptedBytes, encryptedBytes.Length - sentTag.Length, sentTag, 0, sentTag.Length);

                if (!SlowEquals(sentTag, calcTag))
                {
                    isValid = false;
                }

                isValid = await ValidatePasswordHashAsync(password, storedHashSalt);
            }
            catch (FormatException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (ArgumentNullException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (ArgumentOutOfRangeException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (ArgumentException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (RankException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (ArrayTypeMismatchException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (InvalidCastException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }

            return isValid;
        }

        private static bool SlowEquals(byte[] a, byte[] b)
        {
            if (a == null) { throw new ArgumentNullException(nameof(a)); }
            if (b == null) { throw new ArgumentNullException(nameof(b)); }

            var diff = (uint)a.Length ^ (uint)b.Length;
            for (int i = 0; i < a.Length && i < b.Length; i++)
            {
                diff |= (uint)(a[i] ^ b[i]);
            }
            return diff == 0;
        }
        #endregion

        #region Encryption - Asymmetric
        private static RSAParameters GenerateKey(bool includePrivateParameters)
        {
            try
            {
                using (RSACryptoServiceProvider rsaCsp = new RSACryptoServiceProvider(_rsaKeySize))
                {
                    return rsaCsp.ExportParameters(includePrivateParameters);
                }
            }
            catch (CryptographicException exc) { ExceptionLayer.Handle(exc); throw; }
        }

        private static string GenerateKeyXml()
        {
            try
            {
                using (RSACryptoServiceProvider rsaCsp = new RSACryptoServiceProvider(_rsaKeySize))
                {
                    rsaCsp.ExportParameters(false);
                    return rsaCsp.ToXmlString(true);
                }
            }
            catch (CryptographicException exc) { ExceptionLayer.Handle(exc); throw; }
        }

        public static string Decrypt(string rsaParametersXml, byte[] encryptedBytes)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Encryption - Symmetric
        public static async Task<string> DecryptAsync(byte[] encryptedBytes, byte[] key, byte[] iv)
        {
            if (encryptedBytes == null) { throw new ArgumentNullException(nameof(encryptedBytes)); }
            if (key == null) { throw new ArgumentNullException(nameof(key)); }
            if (iv == null) { throw new ArgumentNullException(nameof(iv)); }

            string plainText = string.Empty;

            try
            {
                using (AesCryptoServiceProvider aesCryptoServiceProvider = new AesCryptoServiceProvider() { KeySize = _keyBitSize, BlockSize = _blockBitSize, Mode = CipherMode.CBC, Padding = PaddingMode.PKCS7 })
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    using (ICryptoTransform cryptoTransform = aesCryptoServiceProvider.CreateDecryptor(key, iv))
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, cryptoTransform, CryptoStreamMode.Write))
                    using (StreamReader streamReader = new StreamReader(cryptoStream))
                    {
                        plainText = await streamReader.ReadToEndAsync();
                    }
                }

            }
            catch (PlatformNotSupportedException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (InvalidOperationException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (ArgumentOutOfRangeException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (ArgumentNullException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (ArgumentException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }

            return plainText;
        }

        public static async Task<byte[]> EncryptAsync(string plainText, byte[] key, byte[] iv)
        {
            if (plainText == null) { throw new ArgumentNullException(nameof(plainText)); }
            if (key == null) { throw new ArgumentNullException(nameof(key)); }
            if (iv == null) { throw new ArgumentNullException(nameof(iv)); }

            byte[] encryptedBytes = null;

            try
            {
                using (AesCryptoServiceProvider aesCryptoServiceProvider = new AesCryptoServiceProvider() { KeySize = _keyBitSize, BlockSize = _blockBitSize, Mode = CipherMode.CBC, Padding = PaddingMode.PKCS7 })
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    using (ICryptoTransform cryptoTransform = aesCryptoServiceProvider.CreateEncryptor(key, iv))
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, cryptoTransform, CryptoStreamMode.Write))
                    using (StreamWriter streamWriter = new StreamWriter(cryptoStream))
                    {
                        await streamWriter.WriteAsync(plainText);
                        await streamWriter.FlushAsync();
                    }
                    encryptedBytes = memoryStream.ToArray();
                }
                
            }
            catch (PlatformNotSupportedException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (InvalidOperationException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (ArgumentNullException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (ArgumentException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }

            return encryptedBytes;
        }
        #endregion

        #region Encryption With Authentication - Symmetric
        public static async Task<string> AuthenticatedEncryptAsync(string plainText, byte[] cryptKey, byte[] macKey, CancellationToken cancellationToken)
        {
            CreateIV(out byte[] cryptIv);

            return await AuthenticatedEncryptAsync(plainText, cryptKey, cryptIv, macKey, cancellationToken);
        }

        public static async Task<string> AuthenticatedEncryptAsync(string plainText, TimeSpan expiration, byte[] cryptKey, byte[] macKey, CancellationToken cancellationToken)
        {
            DateTime expirationDate = DateTime.UtcNow + expiration;

            string message = plainText + _delimiter + expirationDate.ToString();

            return await AuthenticatedEncryptAsync(message, cryptKey, macKey, cancellationToken);
        }

        private static async Task<string> AuthenticatedEncryptAsync(string plainText, byte[] cryptKey, byte[] cryptIv, byte[] macKey, CancellationToken cancellationToken)
        {
            if (plainText == null) { throw new ArgumentNullException(nameof(plainText)); }
            if (cryptKey == null) { throw new ArgumentNullException(nameof(cryptKey)); }
            if (cryptIv == null) { throw new ArgumentNullException(nameof(cryptIv)); }
            if (macKey == null) { throw new ArgumentNullException(nameof(macKey)); }

            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);

            byte[] encryptedBytes;
            using (AesCryptoServiceProvider aesCryptoServiceProvider = new AesCryptoServiceProvider() { KeySize = _keyBitSize, BlockSize = _blockBitSize, Mode = CipherMode.CBC, Padding = PaddingMode.PKCS7 })
            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (ICryptoTransform iCryptoTransform = aesCryptoServiceProvider.CreateEncryptor(cryptKey, cryptIv))
                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, iCryptoTransform, CryptoStreamMode.Write))
                {
                    await cryptoStream.WriteAsync(plainBytes, 0, plainBytes.Length, cancellationToken);
                }
                encryptedBytes = memoryStream.ToArray();
            }

            using (MemoryStream memoryStream = new MemoryStream())
            {
                await memoryStream.WriteAsync(cryptIv, 0, cryptIv.Length, cancellationToken);
                await memoryStream.WriteAsync(encryptedBytes, 0, encryptedBytes.Length, cancellationToken);
                await memoryStream.FlushAsync(cancellationToken);

                byte[] sentTag = null;
                using (HMACSHA512 hmacSha512 = new HMACSHA512(macKey))
                {
                    sentTag = hmacSha512.ComputeHash(memoryStream.ToArray());
                }

                await memoryStream.WriteAsync(sentTag, 0, sentTag.Length, cancellationToken);
                await memoryStream.FlushAsync(cancellationToken);

                return Convert.ToBase64String(memoryStream.ToArray());
            }
        }

        public static async Task<string> AuthenticatedDecryptAsync(string encryptedText, byte[] cryptKey, byte[] macKey, CancellationToken cancellationToken)
        {
            if (encryptedText == null) { throw new ArgumentNullException(nameof(encryptedText)); }
            if (cryptKey == null) { throw new ArgumentNullException(nameof(cryptKey)); }
            if (macKey == null) { throw new ArgumentNullException(nameof(macKey)); }

            byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
            byte[] sentTag, calcTag;
            using (HMACSHA512 hmacSha512 = new HMACSHA512(macKey))
            {
                sentTag = new byte[hmacSha512.HashSize / _bitsPerByte];
                calcTag = hmacSha512.ComputeHash(encryptedBytes, 0, encryptedBytes.Length - sentTag.Length);
            }

            Array.Copy(encryptedBytes, encryptedBytes.Length - sentTag.Length, sentTag, 0, sentTag.Length);

            if (!SlowEquals(sentTag, calcTag))
            {
                return null;
            }

            byte[] cryptIv = new byte[_blockByteSize];
            Array.Copy(encryptedBytes, 0, cryptIv, 0, cryptIv.Length);

            using (AesCryptoServiceProvider aesCryptoServiceProvider = new AesCryptoServiceProvider() { KeySize = _keyBitSize, BlockSize = _blockBitSize, Mode = CipherMode.CBC, Padding = PaddingMode.PKCS7 })
            using (ICryptoTransform iCryptoTransform = aesCryptoServiceProvider.CreateDecryptor(cryptKey, cryptIv))
            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, iCryptoTransform, CryptoStreamMode.Write))
                {
                    await cryptoStream.WriteAsync(encryptedBytes, cryptIv.Length, encryptedBytes.Length - cryptIv.Length - sentTag.Length, cancellationToken);
                }

                return Encoding.UTF8.GetString(memoryStream.ToArray());
            }
        }

        public static async Task<string> AuthenticatedDecryptExpirationAsync(string encryptedText, byte[] cryptKey, byte[] macKey, CancellationToken cancellationToken)
        {
            if (encryptedText == null) { throw new ArgumentNullException(nameof(encryptedText)); }
            if (cryptKey == null) { throw new ArgumentNullException(nameof(cryptKey)); }
            if (macKey == null) { throw new ArgumentNullException(nameof(macKey)); }

            string message = await AuthenticatedDecryptAsync(encryptedText, cryptKey, macKey, cancellationToken);
            string[] messageParts = message.Split(_delimiter);
            string expirationValue = string.Empty;

            if (messageParts?.Length == 2)
            {
                expirationValue = messageParts[1];
                DateTime expiration = !string.IsNullOrWhiteSpace(expirationValue) && DateTime.TryParse(expirationValue, out expiration) ? expiration : default;
                if (DateTime.Compare(DateTime.UtcNow, expiration) <= 0)
                {
                    return messageParts[0];
                }
            }
            
            return string.Empty;
        }

        public static void CreateIV(out byte[] cryptIv)
        {
            byte[] passwordBytes = new byte[_keyByteSize];
            FillBytes(passwordBytes);

            byte[] ivSalt = new byte[_saltByteSize];
            FillBytes(ivSalt);
            using (Rfc2898DeriveBytes rfc = new Rfc2898DeriveBytes(passwordBytes, ivSalt, _iterations))
            {
                cryptIv = rfc.GetBytes(_blockByteSize);
            }
        }

        public static void CreateKeys(out byte[] cryptKey, out byte[] macKey)
        {
            byte[] passwordBytes = new byte[_keyByteSize];
            FillBytes(passwordBytes);

            byte[] keySalt = new byte[_saltByteSize];
            FillBytes(keySalt);
            using (Rfc2898DeriveBytes rfc = new Rfc2898DeriveBytes(passwordBytes, keySalt, _iterations))
            {
                cryptKey = rfc.GetBytes(_keyByteSize);
            }

            byte[] macSalt = new byte[_saltByteSize];
            FillBytes(macSalt);
            using (Rfc2898DeriveBytes rfc = new Rfc2898DeriveBytes(passwordBytes, macSalt, _iterations))
            {
                macKey = rfc.GetBytes(_keyByteSize);
            }
        }
        #endregion

        #region Message Authentication
        public static string Authenticate(string plainText, byte[] macKey)
        {
            if (plainText == null) { throw new ArgumentNullException(nameof(plainText)); }
            if (macKey == null) { throw new ArgumentNullException(nameof(macKey)); }

            string hash = string.Empty;

            try
            {
                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                using (HMACSHA512 hmac = new HMACSHA512(macKey))
                {
                    byte[] authenticatedBytes = hmac.ComputeHash(plainBytes);
                    hash = Convert.ToBase64String(authenticatedBytes);
                }
            }
            catch (EncoderFallbackException exc)
            {
                ExceptionLayer.Handle(exc);
                throw;
            }

            return hash;
        }

        public static string Authenticate(string plainText, string macKey)
        {
            if (plainText == null) { throw new ArgumentNullException(nameof(plainText)); }
            if (macKey == null) { throw new ArgumentNullException(nameof(macKey)); }

            string hash = string.Empty;

            try
            {
                byte[] macKeyBytes = Encoding.UTF8.GetBytes(macKey);
                hash = Authenticate(plainText, macKeyBytes);

            }
            catch (EncoderFallbackException exc)
            {
                ExceptionLayer.Handle(exc);
                throw;
            }

            return hash;
        }
        #endregion

        #region Secure Random Number Generator
        public static void FillBytes(byte[] bytes)
        {
            using (RandomNumberGenerator randomNumberGenerator = RandomNumberGenerator.Create())
            {
                randomNumberGenerator.GetBytes(bytes);
            }
        }

        public static void FillNonZeroBytes(byte[] bytes)
        {
            using (RandomNumberGenerator randomNumberGenerator = RandomNumberGenerator.Create())
            {
                randomNumberGenerator.GetNonZeroBytes(bytes);
            }
        }
        #endregion
    }
}
