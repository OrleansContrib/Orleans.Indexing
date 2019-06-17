using System;
using System.IO;
using System.Threading;
using Xunit.Abstractions;

namespace Orleans.Indexing.Tests
{
    /// <summary>
    /// Workaround for Console.Write(Line) in Xunit tests
    /// </summary>
    // Requires app.config with the following entry for passing tests to output the strings (failing tests will always do so):
    //   <configuration>
    //     <appSettings>
    //       <add key = "xunit.diagnosticMessages" value="true"/>
    //     </appSettings> 
    //   </configuration>
    //
    // This class is intended for debugging only one test at a time
    internal class TestConsoleOutputWriter : IDisposable
    {
        ITestOutputHelper output;
        private TextWriter originalConOut;
        private StringWriter stringWriter;

        internal TestConsoleOutputWriter(ITestOutputHelper output, string message)
        {
            this.output = output;
            if (!string.IsNullOrEmpty(message))
            {
                output.WriteLine(message);
            }
            this.stringWriter = new StringWriter();
            this.originalConOut = Console.Out;
            Console.SetOut(stringWriter);
        }

        // using (var tw = new TestConsoleOutputWriter(...)) {
        //   All Console.Write* calls are collected between ctor and Dispose(), including in called assemblies.
        //   If "xunit.diagnosticMessages" is configured "true" as above, the are written on Dispose().
        // }

        public void Dispose()
        {
            var conOut = Interlocked.Exchange(ref this.originalConOut, null);
            if (conOut != null)
            {
                this.output.WriteLine(this.stringWriter.ToString());
                Console.SetOut(conOut);
            }
        }
    }

}
