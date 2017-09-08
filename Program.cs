using pdf2xml.Properties;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks.Dataflow;
using System.Xml.Serialization;

namespace pdf2xml
{
    class Program
    {
        static ConcurrentDictionary<string, object> Queue = new ConcurrentDictionary<string, object>();
        static ActionBlock<string> Block = new ActionBlock<string>(file => ProcessFile(file));

        static void Main(string[] args)
        {
            while (true)
            {
                AddFiles();
                Thread.Sleep(1000);
                break;
            }
            Block.Complete();
            Block.Completion.Wait();
        }

        static void AddFiles()
        {
            foreach (var file in Directory.EnumerateFiles(Settings.Default.IncomingPath, "*.pdf"))
            {
                if (Queue.TryAdd(file, null))
                {
                    Block.Post(file);
                }
            }
        }

        static void ProcessFile(string file)
        {
            try
            {
                ReportData data;
                using (var stream = GetFileStream(file))
                {
                    data = PdfParser.Parse(stream);
                }

                var xs = new XmlSerializer(typeof(ReportData));
                var output = $@"{Settings.Default.OutgoingPath}\{Path.GetFileNameWithoutExtension(file)}.xml";
                using (var fs = File.Create(output))
                {
                    xs.Serialize(fs, data);
                }

                Queue.TryRemove(file, out object value);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"following error occured processing file {Path.GetFileName(file)}: {ex}");
            }
        }

        static FileStream GetFileStream(string filePath)
        {
            for (int i = 0; i < 100; i++)
            {
                try
                {
                    return File.OpenRead(filePath);
                }
                catch
                {
                    if (i == 99)
                    {
                        throw;
                    }
                    Thread.Sleep(100);
                }
            }
            throw null;
        }
    }
}
