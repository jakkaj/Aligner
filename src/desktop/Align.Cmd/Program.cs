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
using Image = Accord.Imaging.Image;


namespace Align.Cmd
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            await _process(@"C:\Users\jakka\temp\pt");
        }

        static async Task _process(string path)
        {
            var info = new DirectoryInfo(path);

            Bitmap img1 = null;

            foreach (var f in info.GetFiles("*.jpg"))
            {
                Console.WriteLine($"Processing {f.Name}");
                if (img1 == null)
                {
                    img1 = _resize(_loadImage(f.FullName));
                    _save(img1, f.FullName);
                    continue;
                }

                var img2 = _resize(_loadImage(f.FullName));

                FastRetinaKeypointDetector freak = new FastRetinaKeypointDetector();

                var keyPoints1 = freak.Transform(img1);
                var keyPoints2 = freak.Transform(img2);

                // Show the marked points in the original images
                // TODO: The following construct can be simplified
                Bitmap img1mark = new PointsMarker(keyPoints1.Select(x => (IFeaturePoint)x).ToList()).Apply(img1);
                Bitmap img2mark = new PointsMarker(keyPoints2.Select(x => (IFeaturePoint)x).ToList()).Apply(img2);

                // Step 2: Match feature points using a k-NN
                var matcher = new KNearestNeighborMatching<byte[]>(5, new Hamming());
                IntPoint[][] matches = matcher.Match(keyPoints1, keyPoints2);

                // Get the two sets of points
                var correlationPoints1 = matches[0];
                var correlationPoints2 = matches[1];

                var cpFixed1 = new List<IntPoint>();
                var cpFixed2 = new List<IntPoint>();

                for (var i = 0; i < correlationPoints1.Length; i++)
                {
                    var p1 = correlationPoints1[i];
                    var p2 = correlationPoints2[i];
                    var x = p1.X - p2.X;
                    var y = p1.Y - p2.Y;

                    var threshold = 30;

                    if ((x > threshold || x < -threshold) || (y > threshold || y < -threshold))
                    {

                    }
                    else
                    {
                        cpFixed1.Add(correlationPoints1[i]);
                        cpFixed2.Add(correlationPoints2[i]);
                    }
                }

                correlationPoints1 = cpFixed1.ToArray();
                correlationPoints2 = cpFixed2.ToArray();
                RansacHomographyEstimator ransac = new RansacHomographyEstimator(.001, 0.99);
                var homography = ransac.Estimate(correlationPoints1, correlationPoints2);

                // Plot RANSAC results against correlation results
                IntPoint[] inliers1 = correlationPoints1.Get(ransac.Inliers);
                IntPoint[] inliers2 = correlationPoints2.Get(ransac.Inliers);

                // Step 4: Project and blend the second image using the homography
                //Blend blend = new Blend(homography, img1);
                var rect = new Rectification(homography);
                var o = rect.Apply(img2);

                _save(o, f.FullName);

            }
        }

        static void _save(Bitmap img, string path)
        {
            var file = new FileInfo(path);
            var subSir = Path.Join(file.Directory.FullName, "output");
            if (!Directory.Exists(subSir))
            {
                Directory.CreateDirectory(subSir);
            }

            var fileOutput = Path.Join(subSir, file.Name);

            using (var f = File.OpenWrite(fileOutput))
            {
                img.Save(f, ImageFormat.Jpeg);
            }
        }

        static Bitmap _resize(Bitmap img)
        {
            var resize = new ResizeBicubic(1024, 768);

            img = resize.Apply(img);

            return img;

        }

        static Bitmap _loadImage(string path)
        {
            var img = Image.FromFile(path);

            return img;
        }
    }
}
