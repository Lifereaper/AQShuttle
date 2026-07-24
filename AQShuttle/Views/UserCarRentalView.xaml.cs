using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using MySql.Data.MySqlClient;

namespace AQShuttle.Views
{
    public partial class UserCarRentalView : Window
    {
        public ObservableCollection<Car> CarDatabase { get; set; }

        public UserCarRentalView()
        {
            InitializeComponent();

            CarDatabase = new ObservableCollection<Car>();
            dgCars.ItemsSource = CarDatabase;
            cmbCarSearch.ItemsSource = CarDatabase;

            // Load the fleet from the database as soon as the screen opens
            LoadCars();
        }

        // --- FETCH CARS FROM MYSQL ---
        private void LoadCars()
        {
            try
            {
                CarDatabase.Clear();

                using (MySqlConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string query = "SELECT * FROM Cars ORDER BY Id DESC";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            CarDatabase.Add(new Car
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Make = reader["Make"].ToString(),
                                Model = reader["Model"].ToString(),
                                Year = reader["Year"].ToString(),
                                Plate = reader["Plate"].ToString(),
                                TankSize = reader["TankSize"].ToString(),
                                EngineSize = reader["EngineSize"].ToString()
                            });
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Silently fails if not connected yet
            }
        }

        // --- ADD A CAR TO MYSQL ---
        private void BtnAddCar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtMake.Text) || string.IsNullOrWhiteSpace(txtPlate.Text))
            {
                MessageBox.Show("Please enter at least a Make and Plate Number.", "Missing Info", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (MySqlConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();

                    string insertQuery = @"INSERT INTO Cars (Make, Model, Year, Plate, TankSize, EngineSize) 
                                           VALUES (@make, @model, @year, @plate, @tank, @engine)";

                    using (MySqlCommand cmd = new MySqlCommand(insertQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@make", txtMake.Text);
                        cmd.Parameters.AddWithValue("@model", txtModel.Text);
                        cmd.Parameters.AddWithValue("@year", txtYear.Text);
                        cmd.Parameters.AddWithValue("@plate", txtPlate.Text);
                        cmd.Parameters.AddWithValue("@tank", txtTankSize.Text);
                        cmd.Parameters.AddWithValue("@engine", txtEngine.Text);

                        cmd.ExecuteNonQuery();
                    }
                }

                // Clear the form
                txtMake.Clear();
                txtModel.Clear();
                txtYear.Clear();
                txtPlate.Clear();
                txtTankSize.Clear();
                txtEngine.Clear();

                // Instantly refresh the table
                LoadCars();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Cannot reach the database to save this vehicle.\n\n" + ex.Message, "Network Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // --- FUEL CALCULATOR MATH ---
        private void CmbCarSearch_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbCarSearch.SelectedItem is Car selectedCar)
            {
                txtSelectedTankSize.Text = selectedCar.TankSize;
                txtCalcError.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnCalculateFuel_Click(object sender, RoutedEventArgs e)
        {
            txtCalcError.Visibility = Visibility.Collapsed;

            if (!(cmbCarSearch.SelectedItem is Car selectedCar))
            {
                txtCalcError.Text = "Please select a car first.";
                txtCalcError.Visibility = Visibility.Visible;
                return;
            }

            // Ensure fractions are selected
            if (cmbFuelOut.SelectedIndex < 0 || cmbFuelIn.SelectedIndex < 0)
            {
                txtCalcError.Text = "Please select both Fuel Out and Fuel In fractions.";
                txtCalcError.Visibility = Visibility.Visible;
                return;
            }

            bool validTank = double.TryParse(selectedCar.TankSize, out double tankSizeGallons);
            bool validPrice = double.TryParse(txtFuelPrice.Text, out double fuelPrice);

            if (!validTank || !validPrice)
            {
                txtCalcError.Text = "Please enter valid numbers for Tank Size and Price.";
                txtCalcError.Visibility = Visibility.Visible;
                return;
            }

            // Index values (0 to 16) match sixteenths mathematically 
            double fuelOutFraction = cmbFuelOut.SelectedIndex / 16.0;
            double fuelInFraction = cmbFuelIn.SelectedIndex / 16.0;
            double fractionMissing = fuelOutFraction - fuelInFraction;

            if (fractionMissing <= 0)
            {
                txtResult.Text = "$0.00";
                return;
            }

            double gallonsMissing = tankSizeGallons * fractionMissing;
            double totalOwed = gallonsMissing * fuelPrice;

            txtResult.Text = $"${totalOwed:0.00}";
        }

        // --- VIRTUAL TANK ANIMATIONS ---
        private void FuelInput_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (rectFuelOut == null || rectFuelIn == null || cmbFuelOut == null || cmbFuelIn == null)
                return;

            double maxTankHeight = 96.0;

            // Handle Fuel Out Dropdown Change
            if (cmbFuelOut.SelectedIndex >= 0)
            {
                double fraction = cmbFuelOut.SelectedIndex / 16.0;
                rectFuelOut.Height = maxTankHeight * fraction;

                if (cmbFuelOut.SelectedItem is ComboBoxItem selectedItem)
                {
                    // Trims out notes like "(Full)" or "(Empty)" to show just the raw fraction string
                    lblFuelOut.Text = selectedItem.Content.ToString().Split(' ')[0];
                }
                SetTankColor(rectFuelOut, fraction * 100);
            }

            // Handle Fuel In Dropdown Change
            if (cmbFuelIn.SelectedIndex >= 0)
            {
                double fraction = cmbFuelIn.SelectedIndex / 16.0;
                rectFuelIn.Height = maxTankHeight * fraction;

                if (cmbFuelIn.SelectedItem is ComboBoxItem selectedItem)
                {
                    lblFuelIn.Text = selectedItem.Content.ToString().Split(' ')[0];
                }
                SetTankColor(rectFuelIn, fraction * 100);
            }
        }

        private void SetTankColor(Rectangle rect, double levelPercentage)
        {
            if (levelPercentage > 50) rect.Fill = new SolidColorBrush(Colors.LimeGreen);
            else if (levelPercentage > 20) rect.Fill = new SolidColorBrush(Colors.Orange);
            else rect.Fill = new SolidColorBrush(Colors.Red);
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}