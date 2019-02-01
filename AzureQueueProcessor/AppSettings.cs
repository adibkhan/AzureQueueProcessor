using System;
using System.ComponentModel;
using System.Configuration;


namespace AzureQueueProcessor
{
    public static class AppSettings
    {
        public static T Get<T>(string key)
        {
            var appSetting = ConfigurationManager.AppSettings[key];
            if (string.IsNullOrWhiteSpace(appSetting)) throw new ConfigurationErrorsException(key);

            var converter = TypeDescriptor.GetConverter(typeof(T));
            return (T)(converter.ConvertFromInvariantString(appSetting));
        }


        public static string AppName()
        {
            return "AzureQueueProcessorService";
        }
    }
}
