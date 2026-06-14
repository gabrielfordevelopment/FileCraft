namespace FileCraft.Shared.Helpers
{
    public static class ResourceHelper
    {
        public static string GetString(string key)
        {
            var application = Application.Current;
            if (application == null)
            {
                return key;
            }

            if (application.Dispatcher.CheckAccess())
            {
                return application.TryFindResource(key) as string ?? key;
            }

            return application.Dispatcher.Invoke(() => application.TryFindResource(key) as string ?? key);
        }
    }
}
