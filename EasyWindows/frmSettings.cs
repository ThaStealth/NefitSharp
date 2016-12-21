using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using DigitalThermostat.Properties;

namespace DigitalThermostat
{
    public partial class FrmSettings : Form
    {
       public const string cPassPhrase = "EasyForWindows";


        public FrmSettings()
        {            
            InitializeComponent();
            tbxSerial.Text = Settings.Default.serial;
            tbxAccessCode.Text = Settings.Default.accessKey;
            tbxPassword.Text = StringCipher.Decrypt(Settings.Default.password, cPassPhrase);
            nudRefreshInterval.Value = Convert.ToDecimal(Settings.Default.refreshInterval/1000.0);
            cbxScale.SelectedIndex = (int) (4 - Settings.Default.scale/0.25F);
            cbxDebugMode.Checked = Settings.Default.debugMode;
            cbxDebugMode.CheckedChanged += cbxDebug_CheckedChanged;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (tbxSerial.Text.Length == 9 && tbxAccessCode.Text.Length == 16 && tbxPassword.Text.Length >= 4)
            {
                Settings.Default.serial = tbxSerial.Text;
                Settings.Default.accessKey = tbxAccessCode.Text;
                Settings.Default.password = StringCipher.Encrypt(tbxPassword.Text,cPassPhrase);
                Settings.Default.refreshInterval = Convert.ToInt32(nudRefreshInterval.Value*1000);
                Settings.Default.scale = (4 - cbxScale.SelectedIndex)*0.25F;
                Settings.Default.firstStart = false;
                Settings.Default.debugMode = cbxDebugMode.Checked;
                Settings.Default.Save();
                DialogResult = DialogResult.OK;                
            }
            else
            {
                MessageBox.Show(@"Please check the credentials you provided.", @"Invalid credentials", MessageBoxButtons.OK, MessageBoxIcon.Error);
                
            }
        }

        private void cbxDebug_CheckedChanged(object sender, EventArgs e)
        {
            if (cbxDebugMode.Enabled)
            {
               if(MessageBox.Show("Are you sure you want to enable debug mode?\r\nThis will only log the communication between Bosch server and this application" +
                                "\r\nUse only if you are instructed or want to debug the protocol for yourself","Are you sure?",MessageBoxButtons.YesNo,MessageBoxIcon.Question) == DialogResult.No)
               {
                   cbxDebugMode.Enabled = false;
               }
            }
        }
    }

    public static class StringCipher
    {
        // This constant is used to determine the keysize of the encryption algorithm in bits.
        // We divide this by 8 within the code below to get the equivalent number of bytes.
        private const int Keysize = 256;

        // This constant determines the number of iterations for the password bytes generation function.
        private const int DerivationIterations = 1000;

        public static string Encrypt(string plainText, string passPhrase)
        {
            // Salt and IV is randomly generated each time, but is preprended to encrypted cipher text
            // so that the same Salt and IV values can be used when decrypting.  
            var saltStringBytes = Generate256BitsOfRandomEntropy();
            var ivStringBytes = Generate256BitsOfRandomEntropy();
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            using (var password = new Rfc2898DeriveBytes(passPhrase, saltStringBytes, DerivationIterations))
            {
                var keyBytes = password.GetBytes(Keysize / 8);
                using (var symmetricKey = new RijndaelManaged())
                {
                    symmetricKey.BlockSize = 256;
                    symmetricKey.Mode = CipherMode.CBC;
                    symmetricKey.Padding = PaddingMode.PKCS7;
                    using (var encryptor = symmetricKey.CreateEncryptor(keyBytes, ivStringBytes))
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                            {
                                cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                                cryptoStream.FlushFinalBlock();
                                // Create the final bytes as a concatenation of the random salt bytes, the random iv bytes and the cipher bytes.
                                var cipherTextBytes = saltStringBytes;
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

        public static string Decrypt(string cipherText, string passPhrase)
        {
            try
            {
                // Get the complete stream of bytes that represent:
                // [32 bytes of Salt] + [32 bytes of IV] + [n bytes of CipherText]
                var cipherTextBytesWithSaltAndIv = Convert.FromBase64String(cipherText);
                // Get the saltbytes by extracting the first 32 bytes from the supplied cipherText bytes.
                var saltStringBytes = cipherTextBytesWithSaltAndIv.Take(Keysize / 8).ToArray();
                // Get the IV bytes by extracting the next 32 bytes from the supplied cipherText bytes.
                var ivStringBytes = cipherTextBytesWithSaltAndIv.Skip(Keysize / 8).Take(Keysize / 8).ToArray();
                // Get the actual cipher text bytes by removing the first 64 bytes from the cipherText string.
                var cipherTextBytes = cipherTextBytesWithSaltAndIv.Skip((Keysize / 8) * 2).Take(cipherTextBytesWithSaltAndIv.Length - ((Keysize / 8) * 2)).ToArray();

                using (var password = new Rfc2898DeriveBytes(passPhrase, saltStringBytes, DerivationIterations))
                {
                    var keyBytes = password.GetBytes(Keysize / 8);
                    using (var symmetricKey = new RijndaelManaged())
                    {
                        symmetricKey.BlockSize = 256;
                        symmetricKey.Mode = CipherMode.CBC;
                        symmetricKey.Padding = PaddingMode.PKCS7;
                        using (var decryptor = symmetricKey.CreateDecryptor(keyBytes, ivStringBytes))
                        {
                            using (var memoryStream = new MemoryStream(cipherTextBytes))
                            {
                                using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                                {
                                    var plainTextBytes = new byte[cipherTextBytes.Length];
                                    var decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
                                    memoryStream.Close();
                                    cryptoStream.Close();
                                    return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                
            }
            return string.Empty;
        }

        private static byte[] Generate256BitsOfRandomEntropy()
        {
            var randomBytes = new byte[32]; // 32 Bytes will give us 256 bits.
            using (var rngCsp = new RNGCryptoServiceProvider())
            {
                // Fill the array with cryptographically secure random bytes.
                rngCsp.GetBytes(randomBytes);
            }
            return randomBytes;
        }
    }
}
