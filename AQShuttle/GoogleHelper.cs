using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;

namespace AQShuttle
{
    public static class GoogleHelper
    {
        private const string SpreadsheetId = "1Uze53X0J1aZbC--lP-XQCX6z2I1j-5VpVhqkkz3Eppc";

        private static string GetJsonKeyPath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "aq-shuttle-a027889d83f1.json");
        }

        private static SheetsService GetSheetsService()
        {
            string jsonKeyPath = GetJsonKeyPath();
            if (!File.Exists(jsonKeyPath))
            {
                throw new FileNotFoundException($"Google key file not found at:\n{jsonKeyPath}");
            }

            GoogleCredential credential;
            using (var stream = new FileStream(jsonKeyPath, FileMode.Open, FileAccess.Read))
            {
                credential = GoogleCredential.FromStream(stream).CreateScoped(SheetsService.Scope.Spreadsheets);
            }

            return new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "AQ Shuttle App",
            });
        }

        private static string NormalizeTime(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";
            if (DateTime.TryParse(input, out DateTime parsed))
            {
                return parsed.ToString("hh:mm tt");
            }
            return input.Trim();
        }

        // --- 1. PUSH A BRAND NEW BOOKING ---
        public static void PushToGoogleSheet(string date, string time, string customer, string pickup, string dropoff, string pax, string bags, string price)
        {
            try
            {
                var service = GetSheetsService();

                // Order matches columns A through M exactly:
                // A: Row(), B: Date, C: Time, D: Customer, E: Pickup, F: Dropoff, G: Pax, H: Bags, I: Status, J: Price, K: Timestamp, L: Empty, M: User
                var oblist = new List<object>()
                {
                    "=ROW()-1",
                    date,
                    time,
                    customer,
                    pickup,
                    dropoff,
                    pax,
                    bags,
                    "Pending",
                    price,
                    DateTime.Now.ToString("g"),
                    "",
                    Session.CurrentUser
                };

                var valueRange = new ValueRange { Values = new List<IList<object>> { oblist } };
                var appendRequest = service.Spreadsheets.Values.Append(valueRange, SpreadsheetId, "Sheet1!A1");
                appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
                appendRequest.Execute();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Saved to database, but failed to push to Google Sheets.\n\nError: " + ex.Message, "Google Sheets Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // --- 2. UPDATE AN EXISTING BOOKING'S STATUS ---
        public static void UpdateGoogleSheetStatus(string date, string time, string customer, string newStatus)
        {
            try
            {
                var service = GetSheetsService();
                var getRequest = service.Spreadsheets.Values.Get(SpreadsheetId, "Sheet1!B:D");
                var response = getRequest.Execute();
                IList<IList<object>> values = response.Values;

                if (values != null && values.Count > 0)
                {
                    int rowIndex = 1; // 1-based row index for Google Sheets API
                    bool found = false;

                    string targetTime = NormalizeTime(time);
                    DateTime.TryParse(date, out DateTime targetDate);

                    foreach (var row in values)
                    {
                        if (row.Count >= 3)
                        {
                            string sheetDateStr = row[0]?.ToString()?.Trim() ?? "";
                            string sheetTime = NormalizeTime(row[1]?.ToString());
                            string sheetCustomer = row[2]?.ToString()?.Trim() ?? "";

                            DateTime.TryParse(sheetDateStr, out DateTime sheetDate);
                            bool dateMatches = (sheetDate != DateTime.MinValue && targetDate != DateTime.MinValue)
                                ? sheetDate.Date == targetDate.Date
                                : sheetDateStr.Equals(date?.Trim(), StringComparison.OrdinalIgnoreCase);

                            if (dateMatches &&
                                sheetTime.Equals(targetTime, StringComparison.OrdinalIgnoreCase) &&
                                sheetCustomer.Equals(customer?.Trim(), StringComparison.OrdinalIgnoreCase))
                            {
                                found = true;
                                break;
                            }
                        }
                        rowIndex++;
                    }

                    if (found)
                    {
                        // Column I is Status
                        string rangeToUpdate = $"Sheet1!I{rowIndex}";
                        var valueRange = new ValueRange { Values = new List<IList<object>> { new List<object> { newStatus } } };

                        var updateRequest = service.Spreadsheets.Values.Update(valueRange, SpreadsheetId, rangeToUpdate);
                        updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                        updateRequest.Execute();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Google Sheets failed to update status.\n\nError: " + ex.Message, "Google Sync Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // --- 3. UPDATE ALL DETAILS OF AN EXISTING BOOKING ---
        public static void UpdateFullGoogleSheetRow(string originalDate, string originalTime, string originalCustomer,
            string newDate, string newTime, string newCustomer, string newPickup, string newDropoff, string newPax, string newBags, string newPrice, string newStatus)
        {
            try
            {
                var service = GetSheetsService();
                var getRequest = service.Spreadsheets.Values.Get(SpreadsheetId, "Sheet1!B:D");
                var response = getRequest.Execute();
                IList<IList<object>> values = response.Values;

                if (values != null && values.Count > 0)
                {
                    int rowIndex = 1; // 1-based index matching Google Sheets rows
                    bool found = false;

                    string targetTime = NormalizeTime(originalTime);
                    DateTime.TryParse(originalDate, out DateTime targetDate);

                    foreach (var row in values)
                    {
                        if (row.Count >= 3)
                        {
                            string sheetDateStr = row[0]?.ToString()?.Trim() ?? "";
                            string sheetTime = NormalizeTime(row[1]?.ToString());
                            string sheetCustomer = row[2]?.ToString()?.Trim() ?? "";

                            DateTime.TryParse(sheetDateStr, out DateTime sheetDate);
                            bool dateMatches = (sheetDate != DateTime.MinValue && targetDate != DateTime.MinValue)
                                ? sheetDate.Date == targetDate.Date
                                : sheetDateStr.Equals(originalDate?.Trim(), StringComparison.OrdinalIgnoreCase);

                            if (dateMatches &&
                                sheetTime.Equals(targetTime, StringComparison.OrdinalIgnoreCase) &&
                                sheetCustomer.Equals(originalCustomer?.Trim(), StringComparison.OrdinalIgnoreCase))
                            {
                                found = true;
                                break;
                            }
                        }
                        rowIndex++;
                    }

                    if (found)
                    {
                        // Columns B to J:
                        // B: Date, C: Time, D: Customer, E: Pickup, F: Dropoff, G: Pax, H: Bags, I: Status, J: Price
                        string rangeToUpdate = $"Sheet1!B{rowIndex}:J{rowIndex}";
                        var oblist = new List<object>() 
                        { 
                            newDate, 
                            newTime, 
                            newCustomer, 
                            newPickup, 
                            newDropoff, 
                            newPax, 
                            newBags, 
                            newStatus, 
                            newPrice 
                        };

                        var valueRange = new ValueRange { Values = new List<IList<object>> { oblist } };
                        var updateRequest = service.Spreadsheets.Values.Update(valueRange, SpreadsheetId, rangeToUpdate);
                        updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                        updateRequest.Execute();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to push edit to Google Sheets.\n\nError: " + ex.Message, "Google Sync Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
