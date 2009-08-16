using System;
using System.Collections.Generic;
using System.Text;
using OSGridLauncher;

namespace Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Performing tests...");
            OpenSimConfigurator osc = new OpenSimConfigurator();

            Console.WriteLine("Testing Network...: " + osc.TestNetwork());
            Console.WriteLine("Done.");
            Console.ReadLine();
        }
    }
}
