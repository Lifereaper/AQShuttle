using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using MySql.Data.MySqlClient;

namespace AQShuttle.Views
{
    // The Blueprint for a User
    public class UserAccount
    {
        public string Username { get; set; }
        public string Role { get; set; }
    }

    public partial class CreateUserView : Window
    {
        // We keep the list just to connect the data to your XAML table visually
        public ObservableCollection<UserAccount> UserDatabase { get; set; }

        public CreateUserView()
        {
            InitializeComponent();

            UserDatabase = new ObservableCollection<UserAccount>();
            dgUsers.ItemsSource = UserDatabase;

            // Go get the real users from the database the second this window opens!
            LoadUsers();
        }

        // --- FETCH USERS FROM MYSQL ---
        private void LoadUsers()
        {
            try
            {
                UserDatabase.Clear(); // Wipe the table clean first

                using (MySqlConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string query = "SELECT Username, Role FROM Users"; // We don't download passwords for security!

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // Add each user we find in the database to the visual table
                            UserDatabase.Add(new UserAccount
                            {
                                Username = reader["Username"].ToString(),
                                Role = reader["Role"].ToString()
                            });
                        }
                    }
                }
            }
            catch (Exception)
            {
                // If it fails to connect, it just stays empty silently so it doesn't crash
            }
        }

        // --- CREATE A USER ---
        private void BtnCreate_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNewUsername.Text) ||
                string.IsNullOrWhiteSpace(txtNewPassword.Password))
            {
                txtError.Text = "Please fill in all fields.";
                txtError.Visibility = Visibility.Visible;
                return;
            }

            if (txtNewPassword.Password != txtConfirmPassword.Password)
            {
                txtError.Text = "Passwords do not match!";
                txtError.Visibility = Visibility.Visible;
                return;
            }

            string selectedRole = "Standard User";
            if (cmbRole.SelectedItem is ComboBoxItem selectedItem)
            {
                selectedRole = selectedItem.Content.ToString();
            }

            try
            {
                using (MySqlConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();

                    // Safety Check: Does this username already exist?
                    string checkQuery = "SELECT COUNT(*) FROM Users WHERE Username = @user";
                    using (MySqlCommand checkCmd = new MySqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@user", txtNewUsername.Text);
                        int userCount = Convert.ToInt32(checkCmd.ExecuteScalar());

                        if (userCount > 0)
                        {
                            txtError.Text = "This username already exists!";
                            txtError.Visibility = Visibility.Visible;
                            return;
                        }
                    }

                    // Save the new user to the database
                    string insertQuery = "INSERT INTO Users (Username, Password, Role) VALUES (@user, @pass, @role)";
                    using (MySqlCommand cmd = new MySqlCommand(insertQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@user", txtNewUsername.Text);
                        cmd.Parameters.AddWithValue("@pass", txtNewPassword.Password);
                        cmd.Parameters.AddWithValue("@role", selectedRole);
                        cmd.ExecuteNonQuery();
                    }
                }

                // Clear the form boxes for the next entry
                txtNewUsername.Clear();
                txtNewPassword.Clear();
                txtConfirmPassword.Clear();
                txtError.Visibility = Visibility.Collapsed;
                cmbRole.SelectedIndex = 1;

                MessageBox.Show($"Account '{txtNewUsername.Text}' successfully created!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                // Instantly refresh the visual table so the new user appears
                LoadUsers();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Cannot reach the database to save this user.\n\n" + ex.Message, "Network Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // --- DELETE A USER ---
        private void BtnDeleteUser_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn != null && btn.DataContext is UserAccount userToDelete)
            {
                // Safety catch: Don't let them delete the main admin!
                if (userToDelete.Username == "admin")
                {
                    MessageBox.Show("You cannot delete the primary administrator account!", "Action Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Make sure they didn't misclick!
                MessageBoxResult confirm = MessageBox.Show($"Are you sure you want to permanently delete {userToDelete.Username}?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (confirm == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (MySqlConnection conn = DatabaseHelper.GetConnection())
                        {
                            conn.Open();

                            // Tell the database to erase them
                            string deleteQuery = "DELETE FROM Users WHERE Username = @user";
                            using (MySqlCommand cmd = new MySqlCommand(deleteQuery, conn))
                            {
                                cmd.Parameters.AddWithValue("@user", userToDelete.Username);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        // Instantly refresh the visual table so they disappear
                        LoadUsers();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Cannot reach the database to delete this user.\n\n" + ex.Message, "Network Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}