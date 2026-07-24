namespace AQShuttle // Make sure this matches your actual project name!
{
    public static class Session
    {
        // This is our global bucket that holds the logged-in username
        public static string CurrentUser { get; set; } = "Unknown";
    }
}