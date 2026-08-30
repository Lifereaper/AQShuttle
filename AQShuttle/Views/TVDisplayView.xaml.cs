using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace AQShuttle.Views
{
    public partial class TVDisplayView : Window
    {
        private ObservableCollection<Booking> _liveDatabase;

        private ObservableCollection<Booking> _stickyList = new ObservableCollection<Booking>();
        private ObservableCollection<Booking> _scrollList = new ObservableCollection<Booking>();

        private Booking _currentNextBooking = null;
        private int _lastDatabaseCount = -1;

        private DispatcherTimer _clockTimer;
        private DispatcherTimer _scrollTimer;
        private ScrollViewer _dgScrollViewer;

        public TVDisplayView(ObservableCollection<Booking> liveDatabase)
        {
            InitializeComponent();
            _liveDatabase = liveDatabase;

            dgStickyBooking.ItemsSource = _stickyList;
            dgScrollingBookings.ItemsSource = _scrollList;

            _clockTimer = new DispatcherTimer();
            _clockTimer.Interval = TimeSpan.FromSeconds(1);
            _clockTimer.Tick += ClockTimer_Tick;
            _clockTimer.Start();

            _scrollTimer = new DispatcherTimer();
            _scrollTimer.Interval = TimeSpan.FromMilliseconds(50);
            _scrollTimer.Tick += ScrollTimer_Tick;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _dgScrollViewer = GetVisualChild<ScrollViewer>(dgScrollingBookings);
            if (_dgScrollViewer != null)
            {
                _scrollTimer.Start();
            }
            ClockTimer_Tick(null, null);
        }

        // Forces the TV screen to rebuild its layout on command
        public void ForceRefresh()
        {
            _lastDatabaseCount = -1;
        }

        private async void ClockTimer_Tick(object sender, EventArgs e)
        {
            DateTime now = DateTime.Now;
            txtClock.Text = now.ToString("HH:mm:ss");
            txtDate.Text = now.ToString("dddd, MMM d, yyyy");

            // Poll MySQL for updates every 5 seconds
            if (now.Second % 5 == 0)
            {
                await RefreshFromDatabaseAsync();
            }

            SyncDisplayLists(now);
        }

        private async Task RefreshFromDatabaseAsync()
        {
            try
            {
                // Fetch the latest active bookings from MySQL
                var freshBookings = await DatabaseHelper.GetActiveBookingsAsync();

                if (freshBookings != null)
                {
                    _liveDatabase.Clear();
                    foreach (var b in freshBookings)
                    {
                        _liveDatabase.Add(b);
                    }

                    // Force layout rebuild so edits reflect immediately
                    ForceRefresh();
                }
            }
            catch
            {
                // Ignore temporary database/network glitches to keep TV display stable
            }
        }

        private void SyncDisplayLists(DateTime now)
        {
            bool needsScrollRebuild = false;

            if (_liveDatabase.Count != _lastDatabaseCount)
            {
                needsScrollRebuild = true;
                _lastDatabaseCount = _liveDatabase.Count;
            }

            Booking nextBooking = null;
            TimeSpan minDiff = TimeSpan.MaxValue;

            foreach (var booking in _liveDatabase)
            {
                if (DateTime.TryParse($"{booking.BookingDate} {booking.BookingTime}", out DateTime bookingDateTime))
                {
                    TimeSpan diff = bookingDateTime - now;
                    if (diff.TotalMinutes >= -1 && diff < minDiff)
                    {
                        minDiff = diff;
                        nextBooking = booking;
                    }
                }
            }

            // Ensures a "going" and "return" trip for the same person don't overwrite each other
            string nextUniqueId = nextBooking != null ? $"{nextBooking.CustomerName}-{nextBooking.BookingDate}-{nextBooking.BookingTime}" : "None";
            string currentUniqueId = _currentNextBooking != null ? $"{_currentNextBooking.CustomerName}-{_currentNextBooking.BookingDate}-{_currentNextBooking.BookingTime}" : "None";

            if (nextUniqueId != currentUniqueId)
            {
                needsScrollRebuild = true;
                _currentNextBooking = nextBooking;

                _stickyList.Clear();
                if (nextBooking != null)
                {
                    _stickyList.Add(nextBooking);
                }
            }

            if (needsScrollRebuild)
            {
                _scrollList.Clear();
                foreach (var b in _liveDatabase)
                {
                    // Only hide the trip if Name, Date, AND Time match the sticky banner
                    bool isSticky = _currentNextBooking != null &&
                                    b.CustomerName == _currentNextBooking.CustomerName &&
                                    b.BookingDate == _currentNextBooking.BookingDate &&
                                    b.BookingTime == _currentNextBooking.BookingTime;

                    if (!isSticky)
                    {
                        if (DateTime.TryParse($"{b.BookingDate} {b.BookingTime}", out DateTime dt))
                        {
                            if ((dt - now).TotalMinutes >= -1)
                            {
                                _scrollList.Add(b);
                            }
                        }
                        else
                        {
                            _scrollList.Add(b);
                        }
                    }
                }
            }
        }

        private void ScrollTimer_Tick(object sender, EventArgs e)
        {
            if (_dgScrollViewer == null)
            {
                _dgScrollViewer = GetVisualChild<ScrollViewer>(dgScrollingBookings);
                if (_dgScrollViewer == null) return;
            }

            if (_dgScrollViewer.ScrollableHeight > 0)
            {
                if (_dgScrollViewer.VerticalOffset >= _dgScrollViewer.ScrollableHeight)
                {
                    _dgScrollViewer.ScrollToTop();
                }
                else
                {
                    _dgScrollViewer.ScrollToVerticalOffset(_dgScrollViewer.VerticalOffset + 1.0);
                }
            }
        }

        private static T GetVisualChild<T>(DependencyObject parent) where T : Visual
        {
            T child = default(T);
            int numVisuals = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < numVisuals; i++)
            {
                Visual v = (Visual)VisualTreeHelper.GetChild(parent, i);
                child = v as T;
                if (child == null) child = GetVisualChild<T>(v);
                if (child != null) break;
            }
            return child;
        }

        private void dgStickyBooking_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
