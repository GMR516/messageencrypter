using System;
using System.Collections;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            DragEnter += Form1_DragEnter;
            DragDrop += Form1_DragDrop;
        }

        void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        void Form1_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (string file in files)
            {
                Console.WriteLine(file);
                Input(file);
            }
        }

        static void Input(string filePath)
        {
            string code = "";
            FileStream fileStream = new FileStream(filePath, FileMode.Open);
            fileStream.Seek(-16, SeekOrigin.End);
            while (fileStream.Position != fileStream.Length)
            {
                code += fileStream.ReadByte();
            }
            Console.WriteLine();
            if (code == "1011109911412111211610110070105108101717782")
            {
                Console.WriteLine("Encrypted.");
                Console.WriteLine("Decrypting...");
                fileStream.Dispose();

                byte[] fileBytes = File.ReadAllBytes(filePath);
                byte[] originalFileBytes = new byte[fileBytes.Length - 16];
                Array.Copy(fileBytes, originalFileBytes, originalFileBytes.Length);
                File.Delete(filePath);
                File.WriteAllBytes(filePath, originalFileBytes);
                DecryptFile(filePath, GetPassword());
            }
            else
            {
                Console.WriteLine(code);
                Console.WriteLine("Not encrypted.");
                Console.WriteLine("Encrypting...");
                fileStream.Dispose();

                EncryptFile(filePath, GetPassword());
                byte[] codeBytes = { 101, 110, 99, 114, 121, 112, 116, 101, 100, 70, 105, 108, 101, 71, 77, 82 };
                using (var stream = new FileStream(filePath, FileMode.Append))
                {
                    stream.Write(codeBytes, 0, codeBytes.Length);
                }
            }
        }


        #region Encryption
        public static string GetPassword(int length = 0)
        {
            /*const string valid = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890*!=&?&/";
            StringBuilder res = new StringBuilder();
            Random rnd = new Random();
            while (0 < length--)
            {
                res.Append(valid[rnd.Next(valid.Length)]);
            }
            return res.ToString();*/
            return "!Msg0QuiCKEnCr!";
        }
        public static void EncryptFile(string file, string password)
        {

            byte[] bytesToBeEncrypted = File.ReadAllBytes(file);
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

            passwordBytes = SHA256.Create().ComputeHash(passwordBytes);

            byte[] bytesEncrypted = AES_Encrypt(bytesToBeEncrypted, passwordBytes);

            File.WriteAllBytes(file, bytesEncrypted);
            //File.Move(file, file + ".test");
        }
        public static byte[] AES_Encrypt(byte[] bytesToBeEncrypted, byte[] passwordBytes)
        {
            byte[] encryptedBytes;
            byte[] saltBytes = { 2, 7, 0, 5, 8, 1, 2, 2 };
            using (MemoryStream ms = new MemoryStream())
            {
                using (RijndaelManaged AES = new RijndaelManaged())
                {
                    AES.KeySize = 256;
                    AES.BlockSize = 128;

                    var key = new Rfc2898DeriveBytes(passwordBytes, saltBytes, 1000);
                    AES.Key = key.GetBytes(AES.KeySize / 8);
                    AES.IV = key.GetBytes(AES.BlockSize / 8);

                    AES.Mode = CipherMode.CBC;

                    using (var cs = new CryptoStream(ms, AES.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(bytesToBeEncrypted, 0, bytesToBeEncrypted.Length);
                        cs.Close();
                    }
                    encryptedBytes = ms.ToArray();
                }
            }
            return encryptedBytes;
        }
        #endregion

        #region Decryption
        public static void DecryptFile(string file, string password)
        {

            byte[] bytesToBeDecrypted = File.ReadAllBytes(file);
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            passwordBytes = SHA256.Create().ComputeHash(passwordBytes);

            byte[] bytesDecrypted = AES_Decrypt(bytesToBeDecrypted, passwordBytes);

            File.WriteAllBytes(file, bytesDecrypted);
            /*string extension = Path.GetExtension(file);
            string result = file.Substring(0, file.Length - extension.Length);
            File.Move(file, result);*/
        }
        public static byte[] AES_Decrypt(byte[] bytesToBeDecrypted, byte[] passwordBytes)
        {
            byte[] decryptedBytes = null;
            byte[] saltBytes = new byte[] { 2, 7, 0, 5, 8, 1, 2, 2 };

            using (MemoryStream ms = new MemoryStream())
            {
                using (RijndaelManaged AES = new RijndaelManaged())
                {

                    AES.KeySize = 256;
                    AES.BlockSize = 128;

                    var key = new Rfc2898DeriveBytes(passwordBytes, saltBytes, 1000);
                    AES.Key = key.GetBytes(AES.KeySize / 8);
                    AES.IV = key.GetBytes(AES.BlockSize / 8);

                    AES.Mode = CipherMode.CBC;

                    using (var cs = new CryptoStream(ms, AES.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(bytesToBeDecrypted, 0, bytesToBeDecrypted.Length);
                        cs.Close();
                    }
                    decryptedBytes = ms.ToArray();


                }
            }
            return decryptedBytes;
        }
        #endregion
    }
}
