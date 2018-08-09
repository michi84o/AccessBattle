namespace AccessBattle.Plugins
{
    /// <summary>Interface for plugins.</summary>
    public interface IPlugin
    {
        /// <summary>Plugin metadata.</summary>
        IPluginMetadata Metadata { get; set; }
    }

    /// <summary>Interface for plugin metadata.</summary>
    public interface IPluginMetadata
    {
        /// <summary>Name of the plugin.</summary>
        string Name { get;  }
        /// <summary>Description of the plugin.</summary>
        string Description { get;  }
        /// <summary>Version of the plugin.</summary>
        string Version { get;  }
    }

    /// <summary>Implementation of IPluginMetadata.</summary>
    public class PluginMetadata : IPluginMetadata
    {
        /// <summary>Name of the plugin.</summary>
        public string Name { get; set; }
        /// <summary>Description of the plugin.</summary>
        public string Description { get; set; }
        /// <summary>Version of the plugin.</summary>
        public string Version { get; set; }
    }


}
