using System;
using System.Windows;
using System.Windows.Input;
using MySql.Data.MySqlClient;

namespace AQShuttle.Views
{
    public partial class LoginView : Window
    {
        public LoginView()
        {
            InitializeComponent();
        }

        // Draggable window logic
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void BtnShowPassword_Click(object sender, RoutedEventArgs e)
        {
            if (txtPasswordHidden.Visibility == Visibility.Visible)
            {
                txtPasswordVisible.Text = txtPasswordHidden.Password;
                txtPasswordHidden.Visibility = Visibility.Collapsed;
                txtPasswordVisible.Visibility = Visibility.Visible;
                btnShowPassword.Content = "🙈";
            }
            else
            {
                txtPasswordHidden.Password = txtPasswordVisible.Text;
                txtPasswordVisible.Visibility = Visibility.Collapsed;
                txtPasswordHidden.Visibility = Visibility.Visible;
                btnShowPassword.Content = "👁️";
            }
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string currentPassword = txtPasswordVisible.Visibility == Visibility.Visible
                                     ? txtPasswordVisible.Text
                                     : txtPasswordHidden.Password;

            if (string.IsNullOrWhiteSpace(txtUsername.Text) || string.IsNullOrWhiteSpace(currentPassword))
            {
                txtError.Text = "Please enter username and password.";
                txtError.Visibility = Visibility.Visible;
                return;
            }

            // --- REAL MYSQL LOGIC WITH CRASH PROTECTION ---
            try
            {
                // 1. Grab the connection from our helper
                using (MySqlConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Open(); // Attempt to connect to the server

                    // 2. Ask the database if this user exists, and grab their Role
                    string query = "SELECT Role FROM Users WHERE Username = @user AND Password = @pass";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        // Using parameters (@) stops hackers from injecting bad code!
                        cmd.Parameters.AddWithValue("@user", txtUsername.Text);
                        cmd.Parameters.AddWithValue("@pass", currentPassword);

                        // ExecuteScalar brings back the single piece of data we asked for (the Role)
                        object result = cmd.ExecuteScalar();

                        if (result != null) // They typed the right password!
                        {
                            string role = result.ToString();
                            txtError.Visibility = Visibility.Collapsed;

                            // --- PHASE 1 CAPTURE: Save the username to the global session! ---
                            Session.CurrentUser = txtUsername.Text;

                            // Route them based on what the database says their role is
                            if (role == "Administrator")
                            {
                                AdminView adminWindow = new AdminView();
                                adminWindow.Show();
                            }
                            else
                            {
                                UserView userWindow = new UserView();
                                userWindow.Show();
                            }
                            this.Close();
                        }
                        else
                        {
                            // Wrong username or password
                            txtError.Text = "Invalid username or password.";
                            txtError.Visibility = Visibility.Visible;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // If the Wi-Fi drops or the host computer is off, it comes here instead of crashing!
                MessageBox.Show("Cannot reach the dispatch server. Please check your connection.\n\nDetails: " + ex.Message, "Network Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}