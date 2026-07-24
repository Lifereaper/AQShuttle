using System;
using System.Collections.ObjectModel;
using System.Windows;
using MySql.Data.MySqlClient;

namespace AQShuttle.Views
{
    public partial class UserView : Window
    {
        // It uses the same Booking class we defined in the AdminView!
        public ObservableCollection<Booking> BookingDatabase { get; set; }

        public UserView()
        {
            InitializeComponent();

            BookingDatabase = new ObservableCollection<Booking>();
            dgBookings.ItemsSource = BookingDatabase;

            // Load all real bookings the second the dashboard opens
            LoadBookings();
        }

        // --- FETCH BOOKINGS FROM MYSQL ---
        private void LoadBookings()
        {
            try
            {
                BookingDatabase.Clear();

                using (MySqlConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    // Grab everything from the Bookings table
                    string query = "SELECT * FROM Bookings ORDER BY Id DESC";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            BookingDatabase.Add(new Booking
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                BookingDate = reader["BookingDate"].ToString(),
                                BookingTime = reader["BookingTime"].ToString(),
                                CustomerName = reader["CustomerName"].ToString(),
                                Pickup = reader["Pickup"].ToString(),
                                Dropoff = reader["Dropoff"].ToString(),
                                Pax = reader["Pax"].ToString(),
                                Bags = reader["Bags"].ToString(),
                                Driver = reader["Driver"].ToString(),
                                Price = reader["Price"].ToString(),
                                Status = reader["Status"].ToString()
                            });
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Silently fails if not connected so the app doesn't crash on startup
            }
        }

        // --- ADD A NEW BOOKING TO MYSQL & GOOGLE SHEETS ---
        private void BtnAddBooking_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Grab the date string so we can send it to both MySQL and Google
                string dateStr = dpDate.SelectedDate.HasValue ? dpDate.SelectedDate.Value.ToShortDateString() : "No Date";

                using (MySqlConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();

                    string insertQuery = @"INSERT INTO Bookings 
                                          (BookingDate, BookingTime, CustomerName, Pickup, Dropoff, Pax, Bags, Driver, Price, Status) 
                                          VALUES (@date, @time, @customer, @pickup, @dropoff, @pax, @bags, @driver, @price, 'Pending')";

                    using (MySqlCommand cmd = new MySqlCommand(insertQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@date", dateStr);
                        cmd.Parameters.AddWithValue("@time", txtTime.Text);
                        cmd.Parameters.AddWithValue("@customer", txtCustomerName.Text);
                        cmd.Parameters.AddWithValue("@pickup", txtPickup.Text);
                        cmd.Parameters.AddWithValue("@dropoff", txtDropoff.Text);
                        cmd.Parameters.AddWithValue("@pax", txtPax.Text);
                        cmd.Parameters.AddWithValue("@bags", txtBags.Text);
                        cmd.Parameters.AddWithValue("@driver", txtDriver.Text);
                        cmd.Parameters.AddWithValue("@price", txtPrice.Text);

                        cmd.ExecuteNonQuery();
                    }
                }

                // --- PUSH TO GOOGLE SHEETS ---
                // We fire this off immediately after MySQL successfully saves!
                GoogleHelper.PushToGoogleSheet(dateStr, txtTime.Text, txtCustomerName.Text, txtPickup.Text, txtDropoff.Text, txtPax.Text, txtBags.Text, txtPrice.Text);

                // Clear the form
                dpDate.SelectedDate = null;
                txtTime.Clear();
                txtCustomerName.Clear();
                txtPickup.Clear();
                txtDropoff.Clear();
                txtPax.Clear();
                txtBags.Clear();
                txtDriver.Clear();
                txtPrice.Clear();

                // Instantly refresh the table
                LoadBookings();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Cannot reach the database to save this booking.\n\n" + ex.Message, "Network Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // --- CHANGE STATUS FROM DROPDOWN (Updates MySQL & Google Sheets) ---
        private void CmbStatus_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var comboBox = sender as System.Windows.Controls.ComboBox;

            // CRITICAL WPF FIX: Only run if a human being actually opened the dropdown to click it!
            if (comboBox != null && comboBox.IsLoaded && comboBox.IsDropDownOpen && comboBox.DataContext is Booking updatedBooking)
            {
                string newStatus = "";

                if (comboBox.SelectedItem is System.Windows.Controls.ComboBoxItem item)
                {
                    newStatus = item.Content.ToString();
                }
                else if (comboBox.SelectedItem != null)
                {
                    newStatus = comboBox.SelectedItem.ToString();
                }

                if (string.IsNullOrWhiteSpace(newStatus))
                    return;

                try
                {
                    // 1. Update your local MySQL Database
                    using (MySqlConnection conn = DatabaseHelper.GetConnection())
                    {
                        conn.Open();
                        string updateQuery = "UPDATE Bookings SET Status = @status WHERE Id = @id";
                        using (MySqlCommand cmd = new MySqlCommand(updateQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@status", newStatus);
                            cmd.Parameters.AddWithValue("@id", updatedBooking.Id);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    // 2. Clear out the cloud update instantly!
                    GoogleHelper.UpdateGoogleSheetStatus(updatedBooking.BookingDate, updatedBooking.BookingTime, updatedBooking.CustomerName, newStatus);

                    // Update local UI
                    updatedBooking.Status = newStatus;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Cannot reach the database to update status.\n\n" + ex.Message, "Network Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // --- OPEN OTHER WINDOWS ---
        private void BtnUserCarRental_Click(object sender, RoutedEventArgs e)
        {
            UserCarRentalView carRentalWindow = new UserCarRentalView();
            carRentalWindow.ShowDialog();
        }

        private void BtnTVDisplay_Click(object sender, RoutedEventArgs e)
        {
            TVDisplayView tvWindow = new TVDisplayView(BookingDatabase);
            tvWindow.Show();
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            LoginView loginWindow = new LoginView();
            loginWindow.Show();
            this.Close();
        }
    }
}