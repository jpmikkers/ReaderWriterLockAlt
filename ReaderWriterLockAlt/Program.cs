using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace CodePlex.JPMikkers
{
    class Program
    {
        const int TestRepeats = 10;
        const int TestIterations = 1000000;

        static void Main(string[] args)
        {
            object sync = new object();

            Stopwatch sw = new System.Diagnostics.Stopwatch();

            for (int u = 0; u < TestRepeats; u++)
            {

                sw.Start();
                for (int t = 0; t < TestIterations; t++)
                {
                    Monitor.Enter(sync);
                    Monitor.Exit(sync);
                }
                sw.Stop();
                Console.WriteLine("{0} iterations of monitor enter/exit took {1} ms", TestIterations, sw.ElapsedMilliseconds);
                sw.Reset();

                ReaderWriterLock readerWriterLock = new ReaderWriterLock();

                sw.Start();
                for (int t = 0; t < TestIterations; t++)
                {
                    readerWriterLock.AcquireWriterLock(Timeout.Infinite);
                    readerWriterLock.ReleaseWriterLock();
                }

                sw.Stop();
                Console.WriteLine("{0} iterations of ReaderWriterLock Acquire/Release WriterLock took {1} ms", TestIterations, sw.ElapsedMilliseconds);
                sw.Reset();

                ReaderWriterLockAlt freeReaderWriterLock = new ReaderWriterLockAlt();

                sw.Start();
                for (int t = 0; t < TestIterations; t++)
                {
                    using (freeReaderWriterLock.WriteLock())
                    {
                    }
                }

                sw.Stop();
                Console.WriteLine("{0} iterations of FreeReaderWriterLock WriteLock()/Dispose() took {1} ms", TestIterations, sw.ElapsedMilliseconds);
                sw.Reset();
            }

            Console.ReadLine();
        }
    }
}
