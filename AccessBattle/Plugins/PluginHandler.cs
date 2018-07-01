using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Reflection;
using System.IO;

namespace AccessBattle.Plugins
{
    public class PluginHandler
    {
        //static bool _constructing;
        static readonly object InstanceLock = new object();
        static PluginHandler _instance;
        public static PluginHandler Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        // For debugging
                        //if (_constructing) throw new Exception("Implementation Error: PluginHandler is requested while it is beeing constructed.");
                        //_constructing = true;

                        var inst = new PluginHandler();
                        try
                        {
                            inst.Compose();
                        }
                        catch (Exception e)
                        {
                            Log.WriteLine(LogPriority.Critical, "Error reading Plugins: " + e.Message);
                        }
                        _instance = inst;
                        // _constructing = false;
                    }
                }
                return _instance;
            }
        }

#pragma warning disable 649
        [ImportMany(typeof(IPlugin), RequiredCreationPolicy = CreationPolicy.NonShared)]
        IEnumerable<Lazy<IPlugin, IPluginMetadata>> _pluginsLazy;
#pragma warning restore 649
        List<IPlugin> _plugins;


        void Compose()
        {
            var catalog = new AggregateCatalog();
            try
            {
                // Throws exception if one dll is bad
                //catalog.Catalogs.Add(new DirectoryCatalog(
                //    System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)));

                // Workaround:
                foreach (string dll in Directory.GetFiles(
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "*.dll"))
                {
                    try
                    {
                        var cat = new AssemblyCatalog(Assembly.LoadFile(dll));
                        cat.Parts.ToArray(); // Boom
                        catalog.Catalogs.Add(cat);
                    }
                    catch { }
                }

                var container = new CompositionContainer(catalog);
                container.ComposeParts(this);

                _plugins = new List<IPlugin>();

                foreach (Lazy<IPlugin, IPluginMetadata> plugin in _pluginsLazy)
                {
                    plugin.Value.Metadata = plugin.Metadata;
                    _plugins.Add(plugin.Value);
                }
            }
            catch (Exception e)
            {
                Log.WriteLine(LogPriority.Critical, "Error reading Plugins: " + e.Message);
            }
        }

        public List<T> GetPlugins<T>() where T : IPlugin
        {
            if (_plugins == null)
                return new List<T>();
            return _plugins.OfType<T>().Cast<T>().ToList();
        }
    }
}
