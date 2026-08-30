using System;
using System.Collections.ObjectModel;
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

        // --- Forces the TV screen to rebuild its layout on command when edits occur ---
        public void ForceRefresh()
        {
            _lastDatabaseCount = -1;
            SyncDisplayLists(DateTime.Now);
        }

        private void ClockTimer_Tick(object sender, EventArgs e)
        {
            DateTime now = DateTime.Now;
            txtClock.Text = now.ToString("HH:mm:ss");
            txtDate.Text = now.ToString("dddd, MMM d, yyyy");

            SyncDisplayLists(now);
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
