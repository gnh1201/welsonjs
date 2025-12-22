using System;

namespace Catswords.TlsReport
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Catswords TLS 1.2 Offline Inspector");
            Console.WriteLine("https://catswords.com");
            Console.WriteLine();

            var report = Tls12OfflineInspector.Evaluate();
            Console.WriteLine(report.ToText());
            Console.WriteLine();
        }
    }
}
