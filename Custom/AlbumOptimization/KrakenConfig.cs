using System;
using System.Configuration;
using System.Linq;
using Telerik.Sitefinity.Configuration;
using Telerik.Sitefinity.Localization;

namespace SitefinityWebApp.Custom.AlbumOptimization
{
    [ObjectInfo(Title = "KrakenAPI", Description = "Configuration for Kraken.io")]
    public class KrakenConfig : ConfigSection
    {
        [ObjectInfo(Title = "API Key", Description = "API Key for Kraken.io")]
        [ConfigurationProperty("APIKey", DefaultValue = "")]
        public string APIKey
        {
            get
            {
                return (string)this["APIKey"];
            }
            set
            {
                this["APIKey"] = value;
            }
        }

        [ObjectInfo(Title = "API Secret", Description = "API Secret for Kraken.io")]
        [ConfigurationProperty("APISecret", DefaultValue = "")]
        public string APISecret
        {
            get
            {
                return (string)this["APISecret"];
            }
            set
            {
                this["APISecret"] = value;
            }
        }

        [ObjectInfo(Title = "Lossy optimization", Description = "Use lossy optimization to save up to 90% of the initial file weight")]
        [ConfigurationProperty("LossyOptimization", DefaultValue = false)]
        public bool LossyOptimization
        {
            get
            {
                return (bool)this["LossyOptimization"];
            }
            set
            {
                this["LossyOptimization"] = value;
            }
        }
    }
}