using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Redis;
using System.Diagnostics;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Threading;

namespace ConsoleDemoRedis {

    public static class SampleStackExchangeRedisExtensions {
        public static T Get<T>(this IDatabase cache, string key) {
            return Deserialize<T>(cache.StringGet(key));
        }

        public static object Get(this IDatabase cache, string key) {
            return Deserialize<object>(cache.StringGet(key));
        }

        public static void Set(this IDatabase cache, string key, object value) {
            cache.StringSet(key, Serialize(value));
        }

        static byte[] Serialize(object o) {
            if (o == null) {
                return null;
            }

            BinaryFormatter binaryFormatter = new BinaryFormatter();
            using (MemoryStream memoryStream = new MemoryStream()) {
                binaryFormatter.Serialize(memoryStream, o);
                byte[] objectDataAsStream = memoryStream.ToArray();
                return objectDataAsStream;
            }
        }

        static T Deserialize<T>(byte[] stream) {
            if (stream == null) {
                return default(T);
            }

            BinaryFormatter binaryFormatter = new BinaryFormatter();
            using (MemoryStream memoryStream = new MemoryStream(stream)) {
                T result = (T)binaryFormatter.Deserialize(memoryStream);
                return result;
            }
        }
    }

    [Serializable]
    class Employee {
        public int Id { get; set; }
        public string Name { get; set; }

        public Employee(int EmployeeId, string Name) {
            this.Id = EmployeeId;
            this.Name = Name;
        }
    }

    

    class Program {

        static async Task Task1(int id) {
            RedisValue token = Environment.MachineName+"_TASK" + id.ToString();
            RedisKey key = "SYNC";
            while (true)
	        {
                var l = await db.LockTakeAsync(key, token, TimeSpan.FromSeconds(10));
                if (l) {
                    for (int i = 0; i < 3; i++) {
                        Console.WriteLine("TASK(" + id + "): " + i);
                        await Task.Delay(1000);
                    }
                    await db.LockReleaseAsync(key, token);
                    Thread.Sleep(1000);//Dajmy szansę innym :)
                } else {
                    await Task.Delay(100);
                }
            }
        }

        static ConnectionMultiplexer cnn;
        static IDatabase db;
        static void Main(string[] args) {
            //Debugger.Break();
            //Kompilować dla x64
            var opt = ConfigurationOptions.Parse("pltkdxcache1.redis.cache.windows.net:6380");
            //Windows Azure MSDN - Visual Studio Ultimate, ae9529f7-0df8-42d0-8e94-85779d924421
            opt.Password = "haslo";
            opt.Ssl = true;

            cnn = ConnectionMultiplexer.Connect(opt);
            //var cnn = ConnectionMultiplexer.Connect("localhost");
            db = cnn.GetDatabase();
            //goto send;

            //goto sync;

            Stopwatch sw;
            for (int j = 0; j < 2; j++) {

                sw = Stopwatch.StartNew();
                for (int i = 0; i < 10; i++) {
                    db.StringSet("TK", "0");
                    string str = db.StringGet("TK");
                    if (str.Length != 1) Console.WriteLine("Error");

                }
                sw.Stop();
                Console.WriteLine("Set/Get (string 1): " + sw.ElapsedMilliseconds.ToString());

                sw = Stopwatch.StartNew();
                for (int i = 0; i < 10; i++) {
                    db.StringSet("TK", "0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789");
                    string str = db.StringGet("TK");
                    if (str.Length != 100) Console.WriteLine("Error");

                }
                sw.Stop();
                Console.WriteLine("Set/Get (string 100): " + sw.ElapsedMilliseconds.ToString());
                sw = Stopwatch.StartNew();
                for (int i = 0; i < 10; i++) {
                    db.Set("Employee25", new Employee(25, "Clayton Gragg"));
                    Employee e2 = db.Get<Employee>("Employee25");
                }
                sw.Stop();
                Console.WriteLine("Set/Get (Employee, generic): " + sw.ElapsedMilliseconds.ToString());

                sw = Stopwatch.StartNew();
                for (int i = 0; i < 10; i++) {
                    db.Set("Employee25", new Employee(25, "Clayton Gragg"));
                    Employee e2 = (Employee)db.Get("Employee25");
                }
                sw.Stop();
                Console.WriteLine("Set/Get (Employee, object): " + sw.ElapsedMilliseconds.ToString());
            }
send:
            //URUCHOMIC SUBSCRIBERA!!!!
            Console.WriteLine("Enter - zaczynam wysyłać");
            Console.ReadLine();
            cnn.PreserveAsyncOrder = true; //Szybko
            var sub = cnn.GetSubscriber();
            for (int i = 0; i < 10; i++) {
                string v = DateTime.Now.Ticks.ToString();
                var r = sub.Publish("test", v);
                Thread.Sleep(500);
            }
            Console.WriteLine("Koniec wysyłania");
            Console.ReadLine();
            sub.UnsubscribeAll();

sync:
            //Test synchronizacji

            CancellationTokenSource cts = new CancellationTokenSource();

            db.KeyDelete("SYNC");
            Task.Run(() => Task1(1), cts.Token);
            Task.Run(() => Task1(2), cts.Token);
            Task.Run(() => Task1(3), cts.Token);
            Task.Run(() => Task1(4), cts.Token);


            Console.WriteLine("Koniec synchronizacji; enter - przerwać");
            Console.ReadLine();
            cts.Cancel();

            cnn.Close();
        }
    }
}
