using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using MySql.Data.MySqlClient;

namespace AQShuttle.Views
{
    public class Booking
    {
        public int Id { get; set; }
        public string BookingDate { get; set; }
        public string BookingTime { get; set; }
        public string CustomerName { get; set; }
        public string Pickup { get; set; }
        public string Dropoff { get; set; }
        public string Pax { get; set; }
        public string Bags { get; set; }
        public string Driver { get; set; }
        public string Price { get; set; }
        public string Status { get; set; }
    }

    public partial class AdminView : Window
    {
        public ObservableCollection<Booking> BookingDatabase { get; set; }
        private DispatcherTimer _autoRefreshTimer;
        private Booking _editingBooking = null; // Tracks which booking we are currently editing

        public AdminView()
        {
            InitializeComponent();

            BookingDatabase = new ObservableCollection<Booking>();
            dgBookings.ItemsSource = BookingDatabase;

            LoadBookings();

            _autoRefreshTimer = new DispatcherTimer();
            _autoRefreshTimer.Interval = TimeSpan.FromSeconds(5);
            _autoRefreshTimer.Tick += (s, args) =>
            {
                LoadBookings();
            };
            _autoRefreshTimer.Start();
        }

        private void LoadBookings()
        {
            // Pause auto-refreshing IF we are in the middle of editing a booking so the screen doesn't jump
            if (_editingBooking != null) return;

            try
            {
                BookingDatabase.Clear();

                using (MySqlConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
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
            catch (Exception) { }
        }

        // --- ADD A NEW BOOKING ---
        private void BtnAddBooking_Click(object sender, RoutedEventArgs e)
        {
            try
            {
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

                GoogleHelper.PushToGoogleSheet(dateStr, txtTime.Text, txtCustomerName.Text, txtPickup.Text, txtDropoff.Text, txtPax.Text, txtBags.Text, txtPrice.Text);

                ClearForm();
                LoadBookings();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Cannot save booking.\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // --- EDIT A BOOKING (Pull data into form) ---
        private void BtnEditBooking_Click(object sender, RoutedEventArgs e)
        {
            if (sender as System.Windows.Controls.Button is System.Windows.Controls.Button btn && btn.DataContext is Booking bookingToEdit)
            {
                _editingBooking = bookingToEdit; // Lock the screen so it stops auto-refreshing while we type

                // Populate the text boxes
                if (DateTime.TryParse(bookingToEdit.BookingDate, out DateTime parsedDate))
                    dpDate.SelectedDate = parsedDate;
                else
                    dpDate.SelectedDate = null;

                txtTime.Text = bookingToEdit.BookingTime;
                txtCustomerName.Text = bookingToEdit.CustomerName;
                txtPickup.Text = bookingToEdit.Pickup;
                txtDropoff.Text = bookingToEdit.Dropoff;
                txtPax.Text = bookingToEdit.Pax;
                txtBags.Text = bookingToEdit.Bags;
                txtDriver.Text = bookingToEdit.Driver;
                txtPrice.Text = bookingToEdit.Price;

                // Swap the buttons out visually
                btnAddBooking.Visibility = Visibility.Collapsed;
                btnUpdateBooking.Visibility = Visibility.Visible;
                btnCancelEdit.Visibility = Visibility.Visible;
            }
        }

        // --- UPDATE AN EDITED BOOKING ---
        private void BtnUpdateBooking_Click(object sender, RoutedEventArgs e)
        {
            if (_editingBooking == null) return;

            try
            {
                string dateStr = dpDate.SelectedDate.HasValue ? dpDate.SelectedDate.Value.ToShortDateString() : "No Date";

                using (MySqlConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();

                    string updateQuery = @"UPDATE Bookings 
                                           SET BookingDate=@date, BookingTime=@time, CustomerName=@customer, 
                                               Pickup=@pickup, Dropoff=@dropoff, Pax=@pax, Bags=@bags, 
                                               Driver=@driver, Price=@price 
                                           WHERE Id=@id";

                    using (MySqlCommand cmd = new MySqlCommand(updateQuery, conn))
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
                        cmd.Parameters.AddWithValue("@id", _editingBooking.Id);

                        cmd.ExecuteNonQuery();
                    }
                }

                // Reset the form and release the auto-refresh pause
                ClearForm();
                LoadBookings();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Cannot update booking.\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // --- CANCEL AN EDIT ---
        private void BtnCancelEdit_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        // --- HELPER TO CLEAR THE FORM ---
        private void ClearForm()
        {
            dpDate.SelectedDate = null;
            txtTime.Clear();
            txtCustomerName.Clear();
            txtPickup.Clear();
            txtDropoff.Clear();
            txtPax.Clear();
            txtBags.Clear();
            txtDriver.Clear();
            txtPrice.Clear();

            _editingBooking = null; // Unlocks the auto-refresh timer!

            btnAddBooking.Visibility = Visibility.Visible;
            btnUpdateBooking.Visibility = Visibility.Collapsed;
            btnCancelEdit.Visibility = Visibility.Collapsed;
        }

        // --- DELETE A BOOKING ---
        private void BtnDeleteBooking_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Button btn = sender as System.Windows.Controls.Button;
            if (btn != null && btn.DataContext is Booking bookingToDelete)
            {
                MessageBoxResult confirm = MessageBox.Show($"Are you sure you want to permanently delete the booking for {bookingToDelete.CustomerName}?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (confirm == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (MySqlConnection conn = DatabaseHelper.GetConnection())
                        {
                            conn.Open();
                            string deleteQuery = "DELETE FROM Bookings WHERE Id = @id";
                            using (MySqlCommand cmd = new MySqlCommand(deleteQuery, conn))
                            {
                                cmd.Parameters.AddWithValue("@id", bookingToDelete.Id);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        LoadBookings();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Cannot reach the database to delete this booking.\n\n" + ex.Message, "Network Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        // --- CHANGE STATUS FROM DROPDOWN ---
        private void CmbStatus_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var comboBox = sender as System.Windows.Controls.ComboBox;

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

                if (string.IsNullOrWhiteSpace(newStatus)) return;

                try
                {
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

                    GoogleHelper.UpdateGoogleSheetStatus(updatedBooking.BookingDate, updatedBooking.BookingTime, updatedBooking.CustomerName, newStatus);
                    updatedBooking.Status = newStatus;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Cannot reach the database to update status.\n\n" + ex.Message, "Network Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // --- OPEN OTHER WINDOWS ---
        private void BtnCreateUser_Click(object sender, RoutedEventArgs e)
        {
            CreateUserView createUserWindow = new CreateUserView();
            createUserWindow.ShowDialog();
        }

        private void BtnGasCalculator_Click(object sender, RoutedEventArgs e)
        {
            CarRentalView calcWindow = new CarRentalView();
            calcWindow.Show();
        }

        private void BtnAdminCarRental_Click(object sender, RoutedEventArgs e)
        {
            CarRentalView carRentalWindow = new CarRentalView();
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