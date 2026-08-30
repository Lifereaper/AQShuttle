using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
        private Booking _editingBooking = null;

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
            if (_editingBooking != null) return;

            try
            {
                using (MySqlConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string query = "SELECT * FROM Bookings ORDER BY Id DESC";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        var tempList = new System.Collections.Generic.List<Booking>();
                        while (reader.Read())
                        {
                            tempList.Add(new Booking
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

                        BookingDatabase.Clear();
                        foreach (var b in tempList)
                        {
                            BookingDatabase.Add(b);
                        }
                    }
                }
            }
            catch (Exception) { }
        }

        // --- ADD A NEW BOOKING ---
        private async void BtnAddBooking_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string dateStr = dpDate.SelectedDate.HasValue ? dpDate.SelectedDate.Value.ToString("M/d/yyyy") : "No Date";
                string timeStr = txtTime.Text;
                string customerStr = txtCustomerName.Text;
                string pickupStr = txtPickup.Text;
                string dropoffStr = txtDropoff.Text;
                string paxStr = txtPax.Text;
                string bagsStr = txtBags.Text;
                string driverStr = txtDriver.Text;
                string priceStr = txtPrice.Text;

                using (MySqlConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string insertQuery = @"INSERT INTO Bookings 
                                          (BookingDate, BookingTime, CustomerName, Pickup, Dropoff, Pax, Bags, Driver, Price, Status) 
                                          VALUES (@date, @time, @customer, @pickup, @dropoff, @pax, @bags, @driver, @price, 'Pending')";

                    using (MySqlCommand cmd = new MySqlCommand(insertQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@date", dateStr);
                        cmd.Parameters.AddWithValue("@time", timeStr);
                        cmd.Parameters.AddWithValue("@customer", customerStr);
                        cmd.Parameters.AddWithValue("@pickup", pickupStr);
                        cmd.Parameters.AddWithValue("@dropoff", dropoffStr);
                        cmd.Parameters.AddWithValue("@pax", paxStr);
                        cmd.Parameters.AddWithValue("@bags", bagsStr);
                        cmd.Parameters.AddWithValue("@driver", driverStr);
                        cmd.Parameters.AddWithValue("@price", priceStr);

                        cmd.ExecuteNonQuery();
                    }
                }

                // Push to Google Sheets in background so UI stays smooth
                await Task.Run(() => GoogleHelper.PushToGoogleSheet(dateStr, timeStr, customerStr, pickupStr, dropoffStr, paxStr, bagsStr, priceStr));

                ClearForm();
                LoadBookings();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Cannot save booking.\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // --- EDIT A BOOKING ---
        private void BtnEditBooking_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Booking bookingToEdit)
            {
                _editingBooking = bookingToEdit;

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

                btnAddBooking.Visibility = Visibility.Collapsed;
                btnUpdateBooking.Visibility = Visibility.Visible;
                btnCancelEdit.Visibility = Visibility.Visible;
            }
        }

        // --- UPDATE AN EDITED BOOKING ---
        private async void BtnUpdateBooking_Click(object sender, RoutedEventArgs e)
        {
            if (_editingBooking == null) return;

            try
            {
                string dateStr = dpDate.SelectedDate.HasValue ? dpDate.SelectedDate.Value.ToString("M/d/yyyy") : "No Date";
                string timeStr = txtTime.Text;
                string customerStr = txtCustomerName.Text;
                string pickupStr = txtPickup.Text;
                string dropoffStr = txtDropoff.Text;
                string paxStr = txtPax.Text;
                string bagsStr = txtBags.Text;
                string driverStr = txtDriver.Text;
                string priceStr = txtPrice.Text;

                // 1. Capture original data BEFORE MySQL update
                string originalDate = _editingBooking.BookingDate;
                string originalTime = _editingBooking.BookingTime;
                string originalCustomer = _editingBooking.CustomerName;
                string originalStatus = _editingBooking.Status;

                // 2. Update MySQL Database
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
                        cmd.Parameters.AddWithValue("@time", timeStr);
                        cmd.Parameters.AddWithValue("@customer", customerStr);
                        cmd.Parameters.AddWithValue("@pickup", pickupStr);
                        cmd.Parameters.AddWithValue("@dropoff", dropoffStr);
                        cmd.Parameters.AddWithValue("@pax", paxStr);
                        cmd.Parameters.AddWithValue("@bags", bagsStr);
                        cmd.Parameters.AddWithValue("@driver", driverStr);
                        cmd.Parameters.AddWithValue("@price", priceStr);
                        cmd.Parameters.AddWithValue("@id", _editingBooking.Id);

                        cmd.ExecuteNonQuery();
                    }
                }

                // 3. Push edits to Google Sheets on background thread
                await Task.Run(() => GoogleHelper.UpdateFullGoogleSheetRow(
                    originalDate, originalTime, originalCustomer,
                    dateStr, timeStr, customerStr,
                    pickupStr, dropoffStr, paxStr,
                    bagsStr, priceStr, originalStatus
                ));

                // 4. Reset form & unlock refresh
                ClearForm();
                LoadBookings();

                // 5. Notify open TV windows to pull fresh MySQL data
                foreach (Window window in Application.Current.Windows)
                {
                    if (window is TVDisplayView tvWindow)
                    {
                        tvWindow.ForceRefresh();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Cannot update booking.\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // --- CANCEL EDIT ---
        private void BtnCancelEdit_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

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

            _editingBooking = null;

            btnAddBooking.Visibility = Visibility.Visible;
            btnUpdateBooking.Visibility = Visibility.Collapsed;
            btnCancelEdit.Visibility = Visibility.Collapsed;
        }

        // --- DELETE BOOKING ---
        private void BtnDeleteBooking_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Booking bookingToDelete)
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
        private async void CmbStatus_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;

            if (comboBox != null && comboBox.IsLoaded && comboBox.IsDropDownOpen && comboBox.DataContext is Booking updatedBooking)
            {
                string newStatus = "";

                if (comboBox.SelectedItem is ComboBoxItem item)
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

                    string bDate = updatedBooking.BookingDate;
                    string bTime = updatedBooking.BookingTime;
                    string bCustomer = updatedBooking.CustomerName;

                    await Task.Run(() => GoogleHelper.UpdateGoogleSheetStatus(bDate, bTime, bCustomer, newStatus));
                    updatedBooking.Status = newStatus;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Cannot reach the database to update status.\n\n" + ex.Message, "Network Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // --- NAVIGATION WINDOWS ---
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
