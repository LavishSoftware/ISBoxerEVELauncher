using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows;

namespace ISBoxerEVELauncher.Windows
{
    /// <summary>
    /// Interaction logic for SetMasterKeyWindow.xaml
    /// </summary>
    public partial class SetMasterKeyWindow : Window
    {
        public SetMasterKeyWindow()
        {
            InitializeComponent();
        }

        static bool SecureStringEqual(SecureString s1, SecureString s2)
        {
            if (s1 == null)
            {
                throw new ArgumentNullException("s1");
            }
            if (s2 == null)
            {
                throw new ArgumentNullException("s2");
            }

            if (s1.Length != s2.Length)
            {
                return false;
            }

            IntPtr bstr1 = IntPtr.Zero;
            IntPtr bstr2 = IntPtr.Zero;

            RuntimeHelpers.PrepareConstrainedRegions();

            try
            {
                bstr1 = Marshal.SecureStringToBSTR(s1);
                bstr2 = Marshal.SecureStringToBSTR(s2);

                unsafe
                {
                    for (Char* ptr1 = (Char*)bstr1.ToPointer(), ptr2 = (Char*)bstr2.ToPointer();
                        *ptr1 != 0 && *ptr2 != 0;
                         ++ptr1, ++ptr2)
                    {
                        if (*ptr1 != *ptr2)
                        {
                            return false;
                        }
                    }
                }

                return true;
            }
            finally
            {
                if (bstr1 != IntPtr.Zero)
                {
                    Marshal.ZeroFreeBSTR(bstr1);
                }

                if (bstr2 != IntPtr.Zero)
                {
                    Marshal.ZeroFreeBSTR(bstr2);
                }
            }
        }

        private void buttonGo_Click(object sender, RoutedEventArgs e)
        {
            SecureString s1 = textPassword.SecurePassword;
            SecureString s2 = textPasswordVerify.SecurePassword;
            e.Handled = true;
            if (s1 == null || s1.Length < 1)
            {
                MessageBox.Show("Password is required. Please try again!");
                return;
            }

            if (s2 == null || s2.Length < 1)
            {
                MessageBox.Show("Re-enter Password is required. Please try again!");
                return;
            }

            if (!SecureStringEqual(s1, s2))
            {
                MessageBox.Show("Passwords do not match. Please try again!");
                return;
            }

            App.Settings.SetPasswordMasterKey(s1);
            DialogResult = true;
            this.Close();
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            DialogResult = false;
            this.Close();
        }


    }
}
