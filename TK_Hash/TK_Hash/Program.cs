using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TK_Hash
{
    // Single Instance for app. - Reuse ConnectionMultiplexer
    // Timeout options
    // Key Expiration options
    // How should the multiplexer be configured.
    public static class ConnectionOptions
    {
        public static void Run()
        {

            IDatabase cache = Connection.GetDatabase();

            // Demo Setup
            DemoSetup(cache);
            //String
            cache.StringSet("i", 1);
            Console.WriteLine("Current Value=" + cache.StringGet("i"));

        }

        private static void DemoSetup(IDatabase cache)
        {
            cache.KeyDelete("i");
        }

        private static Lazy<ConnectionMultiplexer> lazyConnection = new Lazy<ConnectionMultiplexer>(() =>
        {
            ConfigurationOptions config = new ConfigurationOptions();
            config.EndPoints.Add(ConfigurationManager.AppSettings["RedisCacheName"]);
            config.Password = ConfigurationManager.AppSettings["RedisCachePassword"];
            config.Ssl = true;
            config.AbortOnConnectFail = false;
            config.ConnectRetry = 5;
            config.ConnectTimeout = 1000;
            return ConnectionMultiplexer.Connect(config);
        });

        public static ConnectionMultiplexer Connection
        {
            get
            {
                return lazyConnection.Value;
            }
        }
    }
    
    public class MyRefObj
    {
        public int A1 { get; set; }
        public int A2 { get; set; }


    }

    public class MyObject
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public double Val { get; set; }
        public MyRefObj RefObj { get; } = new MyRefObj() { A1 = 1, A2 = 2 };
    }

    class Program
    {
        static void Main(string[] args)
        {
            try {
                IDatabase cache = Connection.GetDatabase();
                MyObject o1 = new MyObject();
                MyObject o2 = new MyObject();
                o1.Id = "ABC";
                o1.Name = "O1";
                o1.Val = 123;
                o2.Id = "DEF";
                o2.Name = "O2";
                o2.Val = 456;
                var l = ConvertToHashEntryList(o1);
                cache.HashSet("MyObject:Id#" + o1.Id, l);
                ConvertToHashEntryList(o2);
                l = ConvertToHashEntryList(o2);
                cache.HashSet("MyObject:Id#" + o2.Id, l);

                l = cache.HashGetAll("MyObject:Id#" + o1.Id);
                l = cache.HashGetAll("MyObject:Id#" + o2.Id);

                //To Look for
                var endpoints = Connection.GetEndPoints();
                var endp = endpoints.First();
                var srv = Connection.GetServer(endp);
                var counters = srv.GetCounters();
                var info = srv.Info();
                //Timeout on scan!
                var k1 = srv.Keys(pattern: "MyObject:Id#*",pageSize:1000); //Will use SCAN or KEYS - depend os server version - scan here!
                Console.WriteLine("Wait (long)");
                foreach (var key in k1)
                {
                    Console.WriteLine(key);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            Console.WriteLine("END");
            Console.ReadLine();
        }


        private static HashEntry[] ConvertToHashEntryList(object instance, string prefix = "")
        {
            var propertiesInHashEntryList = new List<HashEntry>();
            var p = prefix == "" ? "" : (prefix + ":");
            foreach (var property in instance.GetType().GetProperties())
            {
                if (!(property.Name == "RefObj") ) //Ugly, I know!
                {
                    // This is just for an example
                    propertiesInHashEntryList.Add(new HashEntry(p + property.Name, instance.GetType().GetProperty(property.Name).GetValue(instance).ToString()));
                }
                else
                {
                    var subPropertyList = ConvertToHashEntryList(instance.GetType().GetProperty(property.Name).GetValue(instance), p + property.Name);
                    propertiesInHashEntryList.AddRange(subPropertyList);
                }
            }
            return propertiesInHashEntryList.ToArray();
        }

        private static Lazy<ConnectionMultiplexer> lazyConnection = new Lazy<ConnectionMultiplexer>(() =>
        {
            ConfigurationOptions config = new ConfigurationOptions();
            config.EndPoints.Add(ConfigurationManager.AppSettings["RedisCacheName"]);
            config.Password = ConfigurationManager.AppSettings["RedisCachePassword"];
            config.Ssl = true;
            config.AbortOnConnectFail = false;
            config.ConnectRetry = 5;
            config.ConnectTimeout = 1000;
            config.SyncTimeout = 60000;
            config.ResponseTimeout = 60000; //To Scan!
            config.AllowAdmin = true; //Required for Info
            return ConnectionMultiplexer.Connect(config);
        });

        public static ConnectionMultiplexer Connection
        {
            get
            {
                return lazyConnection.Value;
            }
        }

    }
}
