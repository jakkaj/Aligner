using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
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

namespace Align.Services
{
    public class AlignerService
    {
        public enum AlignerType
        {
            Harris,
            FREAK,
            Surf
        }

        private const int take = 10;

        public async Task Align(Bitmap img1, string fileName, double threshold, AlignerType type = AlignerType.FREAK)
        {
            var timer = new Stopwatch();
            timer.Start();
            var img2 = Resize(LoadImage(fileName));
           
            IntPoint[][] matches = null;
           
            if (type == AlignerType.FREAK)
            {
                var matcher = new KNearestNeighborMatching<byte[]>(5, new Hamming());
                var freak = new FastRetinaKeypointDetector();
               var keyPoints1 = freak.Transform(img1);
               var  keyPoints2 = freak.Transform(img2);
               matches = matcher.Match(keyPoints1, keyPoints2);
            }
            else if (type == AlignerType.Harris)
            {
                var harris = new HarrisCornersDetector(
                    HarrisCornerMeasure.Harris, 20000f, 1.4f, 5);
                var keyPoints1 = harris.ProcessImage(img1).ToArray();
                var keyPoints2 = harris.ProcessImage(img2).ToArray();
                var matcher = new CorrelationMatching(9, img1, img2); 
                matches = matcher.Match(keyPoints1, keyPoints2);
            }
            else
            {
                var matcher = new KNearestNeighborMatching(5);
                var surf = new SpeededUpRobustFeaturesDetector();
                var keyPoints1 = surf.Transform(img1);
                var keyPoints2 = surf.Transform(img2);
                matches = matcher.Match(keyPoints1, keyPoints2);

            }

            // Get the two sets of points
            var correlationPoints1 = matches[0];
            var correlationPoints2 = matches[1];

            var cpFixed1 = new List<IntPoint>();
            var cpFixed2 = new List<IntPoint>();
            var cpDistance = new List<(int, IntPoint, IntPoint)>();
            Console.WriteLine($"Correlation: {correlationPoints1.Length}");
            for (var i = 0; i < correlationPoints1.Length; i++)
            {
                var p1 = correlationPoints1[i];
                var p2 = correlationPoints2[i];
                var x = p1.X - p2.X;
                var y = p1.Y - p2.Y;

                if ((x > threshold || x < -threshold) || (y > threshold || y < -threshold))
                {

                }
                else
                {
                    
                    
                    
                    cpDistance.Add((Math.Abs(x) + Math.Abs(y), correlationPoints1[i], correlationPoints2[i]));
                }
            }

            cpDistance = cpDistance.OrderBy(_ => _.Item1).ToList();
            cpFixed1.AddRange(cpDistance.Take(take).ToList().Select(_=>_.Item2));
            cpFixed2.AddRange(cpDistance.Take(take).ToList().Select(_ => _.Item3));
            //cpFixed1.Add(correlationPoints1[i]);
            //cpFixed2.Add(correlationPoints2[i]);

            correlationPoints1 = cpFixed1.ToArray();
            correlationPoints2 = cpFixed2.ToArray();

            if (correlationPoints1.Length < 5)
            {
                Console.WriteLine("Not enough points");
                return;
            }

            RansacHomographyEstimator ransac = new RansacHomographyEstimator(.001, 0.99);
            var homography = ransac.Estimate(correlationPoints1, correlationPoints2);

            // Plot RANSAC results against correlation results
            //IntPoint[] inliers1 = correlationPoints1.Get(ransac.Inliers);
            //IntPoint[] inliers2 = correlationPoints2.Get(ransac.Inliers);

            // Step 4: Project and blend the second image using the homography
            //Blend blend = new Blend(homography, img1);
            var rect = new Rectification(homography);
            var o = rect.Apply(img2);

            Save(o, fileName, threshold, type);
            Console.WriteLine($"Time: {timer.Elapsed.Seconds}:{timer.Elapsed.Milliseconds}");

        }


        public void Save(Bitmap img, string path, double threshold, AlignerType type)
        {
            var file = new FileInfo(path);
            var subSir = Path.Combine(file.Directory.FullName, type.ToString());
            subSir = Path.Combine(subSir, $"{type.ToString()}-{threshold}-output");
            if (!Directory.Exists(subSir))
            {
                Directory.CreateDirectory(subSir);
            }

            var fileOutput = Path.Combine(subSir, file.Name);

            using (var f = File.OpenWrite(fileOutput))
            {
                img.Save(f, ImageFormat.Jpeg);
            }
        }

        public Bitmap Resize(Bitmap img)
        {
            var resize = new ResizeBicubic(1024, 768);

            img = resize.Apply(img);

            return img;

        }

        public Bitmap LoadImage(string path)
        {
            var img = Image.FromFile(path);

            return img;
        }
    }
}
