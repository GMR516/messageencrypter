using System;
using System.Collections;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        public static Form1 mainApp;
        public static Form2 passwordInput;
        public static string password = "";

        public static Form3 errorForm;
        public Form1()
        {
            InitializeComponent();
            DragEnter += Form1_DragEnter;
            DragDrop += Form1_DragDrop;

            mainApp = this;
        }

        void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        void Form1_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            passwordInput = new Form2();
            passwordInput.Location = mainApp.Location;
            passwordInput.Location = new Point(passwordInput.Location.X + 89, passwordInput.Location.Y + 211);
            passwordInput.ShowDialog();

            //foreach (string file in files)
            //{
            Console.WriteLine(files[0]);
            Input(files[0]);
            //}
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
            if (password.Length < 1)
                return "!Msg0QuiCKEnCr!";
            else
                return password;
        }
        public static void EncryptFile(string file, string password)
        {

            byte[] bytesToBeEncrypted = File.ReadAllBytes(file);
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

            passwordBytes = SHA256.Create().ComputeHash(passwordBytes);

            byte[] bytesEncrypted = AES_Encrypt(bytesToBeEncrypted, passwordBytes);

            File.WriteAllBytes(file, bytesEncrypted);
            //TODO: THIS CAUSES DECRYPTION TO NOT WORK
            //File.WriteAllBytes(Path.GetDirectoryName(file) + "\\" + Path.GetFileNameWithoutExtension(file) + "_encrypted" + Path.GetExtension(file), bytesEncrypted);
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
            try
            {
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
            }
            catch
            {

            }
            return decryptedBytes;
        }
        #endregion

        private void pictureBox2_MouseDown(object sender, EventArgs e)
        {
            ReleaseCapture();
            SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);

        }

        private void label1_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void label1_MouseEnter(object sender, EventArgs e)
        {
            label1.BackColor = Color.FromArgb(224, 2, 2);
        }

        private void label1_MouseLeave(object sender, EventArgs e)
        {
            label1.BackColor = Color.FromArgb(0, 0, 0);
        }
    }
}
