using AltStreamLib.Api.v2.Models;
using AltStreamLib.EventSub.Chat.Models;
using Newtonsoft.Json;
using NodaTime;
using Pastel;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using static FoxedItUp._Settings;

namespace FoxedItUp {
    internal static class Functions {
        internal static System.Version version = Assembly.GetExecutingAssembly().GetName().Version;
        private static readonly string WindowTitle = $"FoxedItUp {version}"; 
        private static AltStreamLib.Logging.Logger logger = new AltStreamLib.Logging.Logger();
        internal static AutoResetEvent QuitSignal = new AutoResetEvent(false);

        internal static void AltStreamLibLogInit() {
            logger.OnLog += Logger_OnLog;
        }

        private static void Logger_OnLog(LogMessage obj) {
            if (Settings.LogLevel < 6 && obj.Message == "PING") { return; }
            Log ($"AltStreamLib: {obj.Message}",69);
        }

        internal static void Output (string Message) {
            Console.WriteLine(Message);
            Log(Message, 1);
        }

        internal static void Output(string Message, Color FGCol, Color BGCol) {
            Log(Message, 1);
            Console.WriteLine(Message.Pastel(FGCol).PastelBg(BGCol));
        }

        internal static void Output(string Prefix, Author User, string Message, Color FGCol, Color BGCol) {
            Log($"{Prefix} {User.DisplayName} ({User.ChannelName}) {Message}", 1);
            Prefix = Prefix.Pastel(FGCol);
            
            Message = Message.Pastel(FGCol);

            Console.WriteLine($"{Prefix} {ColorUserName(User)} {Message}".PastelBg(BGCol));
        }

        internal static string ColorUserName(Author User) {
            string UserName;
            if (!String.IsNullOrEmpty(User.Color)) {
                UserName = User.DisplayName.Pastel(System.Drawing.ColorTranslator.FromHtml(User.Color));
            } else {
                UserName = User.DisplayName;
            }
            if (Settings.ShowChannelName && (User.DisplayName.ToLower() != User.ChannelName.ToLower())) {
                UserName += $" [{User.ChannelName}]";
            }
            if (Settings.ShowPronouns && !String.IsNullOrEmpty(User.Pronouns)) {
                UserName += $" ({User.Pronouns})".Pastel(Theme.Pronouns);
            }
            return UserName;
        }

        internal static void Output(string Prefix, Author User, string Message, Color FGCol) {
            Output(Prefix, User, Message, FGCol, Theme.Background);
        }

        internal static void Log(string Message, int LogLevel) {
            if (LogLevel <= Settings.LogLevel) {
                System.IO.File.AppendAllText(Settings.LogFile, Message+'\n');
            }
        }

        internal static void ErrorHandler(Exception Ex) {
            Log($"ERROR: {Ex.ToString()}",1);
        }

        internal static string HandleRelativePaths(string URL) {
            if (!String.IsNullOrEmpty(URL)) {
                if (URL.StartsWith("https://") || URL.StartsWith("http://")) {
                    return URL;
                } else {
                    return $"https://altstream.online{URL}";
                }
            } else {
                return "https://altstream.online/logo_ashe_big.gif";
            }
        }
        internal static void SetWindowTitle(string msg) {
            Console.Title = $"{WindowTitle} - {msg}";
        }

        internal static LocalDateTime FromDateTimeUtc(DateTime datetime) {
            return new LocalDateTime(
                datetime.Year,
                datetime.Month,
                datetime.Day,
                datetime.Hour,
                datetime.Minute,
                datetime.Second,
                datetime.Millisecond
            );
        }

        internal static string DateTimeToAge(DateTime datetime) {
            Period Between = Period.Between(FromDateTimeUtc(datetime), FromDateTimeUtc(DateTime.UtcNow));
            string Result = Between.Years.ToString();
            if (Between.Years != 1) { Result += " Years "; } else { Result += " Year "; }
            Result += Between.Months.ToString();
            if (Between.Months != 1) { Result += " Months "; } else { Result += " Month "; }
            Result += Between.Days.ToString();
            if (Between.Days != 1) { Result += " Days"; } else { Result += " Day"; }
            return Result;
        }
        internal static int DateTimeToDays(DateTime datetime) {
            return (int)Math.Floor((DateTime.UtcNow - datetime).TotalDays);
        }
        internal static string GetBotToken() {
            return Decrypt(Settings.EncryptedBotToken);
        }
        internal static void Quit() {
            //if (Settings.EnableUnofficialEvents) {
            //    Output("Stopping unofficial event polling");
            //    AltStream_Unofficial.Disconnect();
            //}
            Output("Stopping HTTP server");
            HTTPServer.Stop();
            Output("Disconnecting AltEventClient");
            AltStream.Disconnect();

            QuitSignal.Set();
        }

        internal static void FillIncompleteAuthor(ref Author Author) {
            if (Author.Pronouns == null || Author.Image == null || Author.Color == null) {
                Log($"Looking up full info for {Author.ChannelName}", 1);
                AltStreamLib.Api.v2.Models.User FullUser = AltStream.AltAPI.GetUserInfoAsync(Author.ChannelName).GetAwaiter().GetResult();
                Author.DisplayName = FullUser.DisplayName;
                Author.Pronouns = FullUser.Pronouns;
                Author.Image = FullUser.Image;
                Author.Color = FullUser.Color;
                Author.Roles = FullUser.Roles;
            }
        }

        private const int Keysize = 256;

        // This constant determines the number of iterations for the password bytes generation function.
        private const int DerivationIterations = 1000;

       
        public static string Encrypt(string plainText) {
            // Salt and IV is randomly generated each time, but is preprended to encrypted cipher text
            // so that the same Salt and IV values can be used when decrypting.  
            var ivStringBytes = Generate256BitsOfRandomEntropy();
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            using (var password = new Rfc2898DeriveBytes(ShitSettings.PassPhrase, ShitSettings.Salt, DerivationIterations)) {
                var keyBytes = password.GetBytes(Keysize / 8);
                using (var symmetricKey = new RijndaelManaged()) {
                    symmetricKey.BlockSize = 256;
                    symmetricKey.Mode = CipherMode.CBC;
                    symmetricKey.Padding = PaddingMode.PKCS7;
                    using (var encryptor = symmetricKey.CreateEncryptor(keyBytes, ivStringBytes)) {
                        using (var memoryStream = new MemoryStream()) {
                            using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write)) {
                                cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                                cryptoStream.FlushFinalBlock();
                                // Create the final bytes as a concatenation of the random salt bytes, the random iv bytes and the cipher bytes.
                                var cipherTextBytes = ShitSettings.Salt;
                                cipherTextBytes = cipherTextBytes.Concat(ivStringBytes).ToArray();
                                cipherTextBytes = cipherTextBytes.Concat(memoryStream.ToArray()).ToArray();
                                memoryStream.Close();
                                cryptoStream.Close();
                                return Convert.ToBase64String(cipherTextBytes);
                            }
                        }
                    }
                }
            }
        }

        public static string Decrypt(string cipherText) {
            // Get the complete stream of bytes that represent:
            // [32 bytes of Salt] + [32 bytes of IV] + [n bytes of CipherText]
            var cipherTextBytesWithSaltAndIv = Convert.FromBase64String(cipherText);
            // Get the saltbytes by extracting the first 32 bytes from the supplied cipherText bytes.
            var saltStringBytes = cipherTextBytesWithSaltAndIv.Take(Keysize / 8).ToArray();
            // Get the IV bytes by extracting the next 32 bytes from the supplied cipherText bytes.
            var ivStringBytes = cipherTextBytesWithSaltAndIv.Skip(Keysize / 8).Take(Keysize / 8).ToArray();
            // Get the actual cipher text bytes by removing the first 64 bytes from the cipherText string.
            var cipherTextBytes = cipherTextBytesWithSaltAndIv.Skip((Keysize / 8) * 2).Take(cipherTextBytesWithSaltAndIv.Length - ((Keysize / 8) * 2)).ToArray();

            using (var password = new Rfc2898DeriveBytes(ShitSettings.PassPhrase, saltStringBytes, DerivationIterations)) {
                var keyBytes = password.GetBytes(Keysize / 8);
                using (var symmetricKey = new RijndaelManaged()) {
                    symmetricKey.BlockSize = 256;
                    symmetricKey.Mode = CipherMode.CBC;
                    symmetricKey.Padding = PaddingMode.PKCS7;
                    using (var decryptor = symmetricKey.CreateDecryptor(keyBytes, ivStringBytes)) {
                        using (var memoryStream = new MemoryStream(cipherTextBytes)) {
                            using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                            using (var streamReader = new StreamReader(cryptoStream, Encoding.UTF8)) {
                                return streamReader.ReadToEnd();
                            }
                        }
                    }
                }
            }
        }

        private static byte[] Generate256BitsOfRandomEntropy() {
            var randomBytes = new byte[32]; // 32 Bytes will give us 256 bits.
            using (var rngCsp = new RNGCryptoServiceProvider()) {
                // Fill the array with cryptographically secure random bytes.
                rngCsp.GetBytes(randomBytes);
            }
            return randomBytes;
        }

    }
}
