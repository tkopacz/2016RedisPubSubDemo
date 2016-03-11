using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SubscriberRedis {
    class Program {
        static void Main(string[] args) {
            //Debugger.Break();
            //Kompilować dla x64
            var opt = ConfigurationOptions.Parse("pltkdxcache1.redis.cache.windows.net:6380");
            opt.Password = "<haslo>";
            opt.Ssl = true;

            var cnn = ConnectionMultiplexer.Connect(opt);
            //var cnn = ConnectionMultiplexer.Connect("localhost");
            cnn.PreserveAsyncOrder = true; //Szybko
            //Nie działa subkrypcja - nie wiem dlaczego!
            IDatabase db = cnn.GetDatabase();
            Console.WriteLine(db.IsConnected("TK"));
            ISubscriber sub = cnn.GetSubscriber();
            sub.Subscribe("test", handler);
            Console.WriteLine(sub.IsConnected("test"));

            Console.WriteLine("Koniect");
            Console.ReadLine();
            sub.UnsubscribeAll();
            cnn.Close();

        }

        private static void handler(RedisChannel channel, RedisValue value) {
            long vr = DateTime.Now.Ticks;
            long v = long.Parse(value);
            Console.WriteLine(channel.ToString() + " - " + v.ToString() + " - " + vr.ToString() + ", " + (v - vr).ToString());
        }
    }
}
