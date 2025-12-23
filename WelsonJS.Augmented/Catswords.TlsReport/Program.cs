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
            
            try
            {
                var report = Tls12OfflineInspector.Evaluate();
                Console.WriteLine(report.ToText());
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Inspector failed: " + ex.GetType().Name + ": " + ex.Message);
            }

            Console.WriteLine();
        }
    }
}
