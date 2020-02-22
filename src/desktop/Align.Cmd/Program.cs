using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Accord;
using Accord.Imaging;
using Accord.Imaging.Filters;
using Accord.Math;
using Accord.Math.Distances;
using Align.Services;
using Image = Accord.Imaging.Image;


namespace Align.Cmd
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            for (var i = 35; i < 70; i+=5)
            {
                await _process(@"C:\Users\jakka\temp\pt\fc8fac57-97d0-4e1a-b040-f56883511227", i, AlignerService.AlignerType.Surf);
            }
        }

        static async Task _process(string path, double threshold, AlignerService.AlignerType type)
        {
            var aligner = new AlignerService();
            var info = new DirectoryInfo(path);
            Console.WriteLine($"Threshold: {threshold}");
            Bitmap img1 = null;
            var dFiles = info.GetFiles("*.jpg").OrderBy(_ => _.Name).ToList();
            Console.WriteLine($"Count: {dFiles.Count}");
            var tasks = new List<Task>();
            foreach (var f in dFiles)
            {
                
                Console.WriteLine($"Processing {f.Name}");
                if (img1 == null)
                {
                    img1 = aligner.Resize(aligner.LoadImage(f.FullName));
                    aligner.Save(img1, f.FullName, threshold, type);
                    continue;
                }

                //await aligner.Align(img1, f.FullName, threshold);

                var fName = f.FullName;
                Bitmap bm = img1.Clone(new Rectangle(new System.Drawing.Point(0, 0),img1.Size), img1.PixelFormat);
                
                var t = Task.Run(()=>aligner.Align(bm, fName, threshold, type));
                //await Task.Delay(1000);
                //if (tasks.Count > 1)
                //{
                //    await Task.WhenAll(tasks);
                //    tasks.Clear();
                //}

                tasks.Add(t);

                

            }

            await Task.WhenAll(tasks);
            await Task.Delay(500);
        }
    }
}
