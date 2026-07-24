using System;
using System.Windows;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Services;
using System.Collections.Generic;
using System.IO;

namespace AQShuttle
{
    public static class GoogleHelper
    {
        // --- 1. PUSH A BRAND NEW BOOKING ---
        public static void PushToGoogleSheet(string date, string time, string customer, string pickup, string dropoff, string pax, string bags, string price)
        {
            try
            {
                string jsonKeyPath = "aq-shuttle-a027889d83f1.json";
                string spreadsheetId = "1Uze53X0J1aZbC--lP-XQCX6z2I1j-5VpVhqkkz3Eppc";

                GoogleCredential credential;
                using (var stream = new FileStream(jsonKeyPath, FileMode.Open, FileAccess.Read))
                {
                    credential = GoogleCredential.FromStream(stream).CreateScoped(SheetsService.Scope.Spreadsheets);
                }

                var service = new SheetsService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "AQ Shuttle App",
                });

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

                var valueRange = new ValueRange();
                valueRange.Values = new List<IList<object>> { oblist };

                var appendRequest = service.Spreadsheets.Values.Append(valueRange, spreadsheetId, "Sheet1!A1");
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
                string jsonKeyPath = "aq-shuttle-a027889d83f1.json";
                string spreadsheetId = "1Uze53X0J1aZbC--lP-XQCX6z2I1j-5VpVhqkkz3Eppc";

                GoogleCredential credential;
                using (var stream = new FileStream(jsonKeyPath, FileMode.Open, FileAccess.Read))
                {
                    credential = GoogleCredential.FromStream(stream).CreateScoped(SheetsService.Scope.Spreadsheets);
                }

                var service = new SheetsService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "AQ Shuttle App",
                });

                // Grab columns B, C, and D (Date, Time, Customer)
                var getRequest = service.Spreadsheets.Values.Get(spreadsheetId, "Sheet1!B:D");
                var response = getRequest.Execute();
                IList<IList<object>> values = response.Values;

                if (values != null && values.Count > 0)
                {
                    int rowIndex = 1;
                    bool found = false;

                    // Parse our target date cleanly
                    DateTime.TryParse(date, out DateTime targetDate);

                    foreach (var row in values)
                    {
                        if (row.Count >= 3)
                        {
                            string sheetDateStr = row[0]?.ToString()?.Trim() ?? "";
                            string sheetTime = row[1]?.ToString()?.Trim() ?? "";
                            string sheetCustomer = row[2]?.ToString()?.Trim() ?? "";

                            // Smart Date Match: Converts spreadsheet text to a true date to avoid formatting errors
                            DateTime.TryParse(sheetDateStr, out DateTime sheetDate);
                            bool dateMatches = (sheetDate != DateTime.MinValue && targetDate != DateTime.MinValue)
                                ? sheetDate.Date == targetDate.Date
                                : sheetDateStr.Equals(date?.Trim(), StringComparison.OrdinalIgnoreCase);

                            // Match the fields
                            if (dateMatches &&
                                sheetTime.Equals(time?.Trim(), StringComparison.OrdinalIgnoreCase) &&
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
                        // Overwrite Column I (Status) on that row index
                        string rangeToUpdate = $"Sheet1!I{rowIndex}";

                        var oblist = new List<object>() { newStatus };
                        var valueRange = new ValueRange();
                        valueRange.Values = new List<IList<object>> { oblist };

                        var updateRequest = service.Spreadsheets.Values.Update(valueRange, spreadsheetId, rangeToUpdate);
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
    }
}