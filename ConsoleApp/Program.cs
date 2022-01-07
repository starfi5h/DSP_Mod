using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp
{
    class Program
    {
        private static readonly DateTime UNIX_EPOCH = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        static void Main(string[] args)
        {
            Console.WriteLine(DateTime.UtcNow);
            Console.WriteLine(DateTimeOffset.UtcNow);

            Console.WriteLine(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            Console.WriteLine((DateTime.UtcNow - UNIX_EPOCH).TotalMilliseconds);
            Console.ReadLine();
        }
    }
}
