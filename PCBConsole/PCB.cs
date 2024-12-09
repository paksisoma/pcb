using Emgu.CV.Structure;
using Emgu.CV;
using Emgu.CV.CvEnum;
using System.Xml;
using Emgu.CV.UI;
using Emgu.CV.Util;
using static Constants;

public struct Rectangle
{
    public int XMin { get; set; }
    public int XMax { get; set; }
    public int YMin { get; set; }
    public int YMax { get; set; }

    public Rectangle(int xMin, int xMax, int yMin, int yMax)
    {
        XMin = xMin;
        XMax = xMax;
        YMin = yMin;
        YMax = yMax;
    }
}

namespace PCBConsole
{
    public enum Mode
    {
        Show,
        Save,
        Analyse
    }

    class PCB
    {
        Image<Bgr, byte> image;
        XmlDocument doc = new XmlDocument();
        Mode mode;
        string filename;
        string imagename;

        public PCB(string path, string annotation, Mode mode, string filename, string imagename)
        {
            image = new Image<Bgr, byte>(path);
            doc.Load(annotation);
            this.mode = mode;
            this.filename = filename;
            this.imagename = imagename;
        }

        public void RunMissingHole(int threshold, int? areaMin = null, int? areaMax = null, double roundnessLimit = 0.7)
        {
            List<Point> found = new List<Point>();

            Image<Gray, byte> gray = new(image.Size);

            CvInvoke.CvtColor(image, gray, ColorConversion.Bgr2Gray);

            CvInvoke.GaussianBlur(gray, gray, new Size(7, 7), 10);

            CvInvoke.Threshold(gray, gray, threshold, 255, ThresholdType.Binary);

            var morf = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(6, 6), new Point(-1, -1));

            CvInvoke.Dilate(gray, gray, morf, new Point(-1, -1), 1, BorderType.Default, new MCvScalar(0));

            CvInvoke.Threshold(gray, gray, 0, 255, ThresholdType.BinaryInv);

            VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
            Mat hier = new Mat();

            //CvInvoke.Threshold(gray, gray, 240, 255, ThresholdType.Binary);
            CvInvoke.FindContours(gray, contours, hier, RetrType.Ccomp, ChainApproxMethod.ChainApproxSimple);

            for (int i = 0; i < contours.Size; i++)
            {
                // Contour center
                var moments = CvInvoke.Moments(contours[i]);
                Point center = new Point((int)(moments.M10 / moments.M00), (int)(moments.M01 / moments.M00));

                if (center.Y > gray.Height || center.Y < 0 || center.X > gray.Width || center.X < 0)
                    continue;

                // White area
                Gray pixelValue = gray[center.Y, center.X];

                if (!pixelValue.Equals(new Gray(255)))
                    continue;

                // Area size
                var area = CvInvoke.ContourArea(contours[i]);

                if ((areaMin != null && area < areaMin) || (areaMax != null && area > areaMax))
                    continue;

                // Roundness
                var perimeter = CvInvoke.ArcLength(contours[i], true);
                double roundness = (4 * Math.PI * area) / (perimeter * perimeter);

                if (roundness < roundnessLimit)
                    continue;

                // Ignore duplication
                bool ignore = false;

                foreach (var b in found)
                    if (center.X <= b.X + 10 && center.X >= b.X - 10 && center.Y <= b.Y + 10 && center.Y >= b.Y - 10)
                    {
                        ignore = true;
                        break;
                    }

                if (ignore)
                    continue;

                // Draw text
                string circleText = "Circle: " + (roundness * 100).ToString("F1") + "%";
                CvInvoke.PutText(image, circleText, new Point(center.X + 18, center.Y + 3), FontFace.HersheySimplex, 1, new MCvScalar(0, 0, 0), 2); // Shadow
                CvInvoke.PutText(image, circleText, new Point(center.X + 15, center.Y), FontFace.HersheySimplex, 1, new MCvScalar(255, 255, 255), 2);

                string areaText = "Area: " + area.ToString("F1");
                CvInvoke.PutText(image, areaText, new Point(center.X + 18, center.Y + 38), FontFace.HersheySimplex, 1, new MCvScalar(0, 0, 0), 2); // Shadow
                CvInvoke.PutText(image, areaText, new Point(center.X + 15, center.Y + 35), FontFace.HersheySimplex, 1, new MCvScalar(255, 255, 255), 2);

                // Draw contour
                CvInvoke.DrawContours(image, contours, i, new MCvScalar(0, 0, 255), 3);

                //CvInvoke.Circle(image, center, 15, new MCvScalar(0, 0, 255), 2);
                found.Add(center);
            }

            if (mode == Mode.Show)
            {
                ImageViewer.Show(image);
            }
            else if (mode == Mode.Save && filename != null)
            {
                CreateFolderIfNotExists(Path.Combine(SAVE_PATH, "Missing_hole"));
                image.Save(Path.Combine(SAVE_PATH, "Missing_hole", filename + ".png"));
            }
            else if (mode == Mode.Analyse) {
                List<Rectangle> annotations = GetAnnotationData();
                int pass = 0;
                int wrong = 0;

                bool IsWithin(Rectangle a, Point b) =>
                    b.X >= a.XMin && b.X <= a.XMax && b.Y >= a.YMin && b.Y <= a.YMax;

                foreach (var a in annotations)
                {
                    if (found.Any(b => IsWithin(a, b)))
                        pass++;
                }

                wrong = found.Count(b => !annotations.Any(a => IsWithin(a, b)));

                //Console.WriteLine($"{annotations.Count}/{pass} helyes és {wrong} helyen rossz helyre jelölt.");
                Console.WriteLine($"{imagename};{annotations.Count};{pass};{wrong}");
            }
        }

        public void RunMouseBite(int snakeLength, int checkLength, int minHeight, int? maxHeight = null)
        {
            List<Point> points = new List<Point>();
            List<Point> found = new List<Point>();

            Image<Gray, byte> gray = new(image.Size);

            CvInvoke.CvtColor(image, gray, ColorConversion.Bgr2Gray);

            CvInvoke.GaussianBlur(gray, gray, new Size(7, 7), 1);

            CvInvoke.Threshold(gray, gray, 50, 255, ThresholdType.Binary);

            var morf = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(6, 6), new Point(-1, -1));

            CvInvoke.Dilate(gray, gray, morf, new Point(-1, -1), 1, BorderType.Default, new MCvScalar(0));

            using (var contours = new VectorOfVectorOfPoint())
            {
                CvInvoke.FindContours(gray, contours, null, RetrType.Tree, ChainApproxMethod.ChainApproxNone);

                for (int i = 0; i < contours.Size; i++)
                {
                    VectorOfPoint contour = contours[i];
                    HashSet<int> indexes = new HashSet<int>();

                    for (int j = 0; j < contour.Size - snakeLength; j++)
                    {
                        Point first = contour[j];
                        Point mid = contour[j + (snakeLength / 2)];
                        Point last = contour[j + snakeLength];

                        double firstAvg = 0;
                        double lastAvg = 0;

                        double midHeight = DistanceFromLine(first, mid, last);

                        // Average how straight is the lines
                        for (int k = 0; k < checkLength; k++)
                        {
                            Point pointFirst = contour[j + k];
                            double differenceFirst = DistanceFromLine(first, pointFirst, last);
                            firstAvg += differenceFirst;

                            Point pointLast = contour[j + snakeLength - k];
                            double differenceLast = DistanceFromLine(first, pointLast, last);
                            lastAvg += differenceLast;
                        }

                        firstAvg /= checkLength;
                        lastAvg /= checkLength;

                        if (firstAvg < 1 && lastAvg < 1 && midHeight > minHeight && (maxHeight == null || midHeight < maxHeight))
                        {
                            for (int k = j; k < j + snakeLength; k++)
                            {
                                // If this line already checked
                                if (indexes.Contains(k))
                                    goto Error;

                                Point point = contour[k];
                                double a = DistanceFromLine(first, point, last);

                                // Compare two line similarity
                                double b = DistanceFromLine(first, point, contour[j + checkLength]);

                                if (Math.Abs(a - b) > 1)
                                    goto Error;

                                // Mid must be the highest
                                if (a > midHeight)
                                    goto Error;
                            }

                            // Reserve all indexes to prevent multiple lines
                            for (int k = j; k < j + snakeLength; k++)
                                indexes.Add(k);

                            // Draw circle
                            CvInvoke.Circle(image, mid, 15, new MCvScalar(0, 0, 255), 2);

                            // Draw text
                            string midText = "Mid height: " + midHeight.ToString("F1");
                            CvInvoke.PutText(image, midText, new Point(mid.X + 18, mid.Y + 3), FontFace.HersheySimplex, 1, new MCvScalar(0, 0, 0), 2); // Shadow
                            CvInvoke.PutText(image, midText, new Point(mid.X + 15, mid.Y), FontFace.HersheySimplex, 1, new MCvScalar(255, 255, 255), 2);

                            found.Add(mid);
                        }

                    Error:
                        continue;
                    }
                }
            }

            if (mode == Mode.Show)
            {
                ImageViewer.Show(image);
            }
            else if (mode == Mode.Save && filename != null)
            {
                CreateFolderIfNotExists(Path.Combine(SAVE_PATH, "Mouse_bite"));
                image.Save(Path.Combine(SAVE_PATH, "Mouse_bite", filename + ".png"));
            }
            else if (mode == Mode.Analyse)
            {
                List<Rectangle> annotations = GetAnnotationData();
                int pass = 0;
                int wrong = 0;

                bool IsWithin(Rectangle a, Point b) =>
                    b.X >= a.XMin && b.X <= a.XMax && b.Y >= a.YMin && b.Y <= a.YMax;

                foreach (var a in annotations)
                {
                    if (found.Any(b => IsWithin(a, b)))
                        pass++;
                }

                wrong = found.Count(b => !annotations.Any(a => IsWithin(a, b)));

                //Console.WriteLine($"{annotations.Count}/{pass} helyes és {wrong} helyen rossz helyre jelölt.");
                Console.WriteLine($"{imagename};{annotations.Count};{pass};{wrong}");
            }
        }

        /*public void ShowShort(string? filename = null)
        {
            List<Point> points = new List<Point>();

            Image<Gray, byte> gray = new(image.Size);

            CvInvoke.CvtColor(image, gray, ColorConversion.Bgr2Gray);

            CvInvoke.GaussianBlur(gray, gray, new Size(7, 7), 2);

            CvInvoke.Threshold(gray, gray, 50, 255, ThresholdType.Binary);

            var morf = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(7, 7), new Point(-1, -1));

            CvInvoke.Dilate(gray, gray, morf, new Point(-1, -1), 1, BorderType.Default, new MCvScalar(0));

            using (var contours = new VectorOfVectorOfPoint())
            {
                CvInvoke.FindContours(gray, contours, null, RetrType.Tree, ChainApproxMethod.ChainApproxNone);

                for (int i = 0; i < contours.Size; i++)
                {
                    VectorOfPoint contour = contours[i];
                    HashSet<int> indexes = new HashSet<int>();

                    int snakeLength = 7;

                    double? lastAngle = null;

                    for (int j = 0; j < contour.Size - snakeLength; j++)
                    {
                        Point first = contour[j];
                        Point last = contour[j + snakeLength];

                        double angle = CalculateAngle(first, last);
                        if (lastAngle == null)
                        {
                            lastAngle = angle;

                            for (int k = j; k < j + snakeLength; k++)
                            {
                                Point point = contour[k];

                                image[point.Y, point.X] = new Bgr(255, 0, 0);
                            }
                        }
                        else
                        {
                            if (Math.Abs((decimal)(lastAngle - angle)) > 80 && Math.Abs((decimal)(lastAngle - angle)) < 100)
                            {
                                //lastAngle = angle;

                                for (int k = j; k < j + snakeLength; k++)
                                {
                                    Point point = contour[k];

                                    image[point.Y, point.X] = new Bgr(0, 0, 255);
                                }

                                lastAngle = null;
                            }
                        }
                    }
                }
            }

            if (mode == Mode.Show)
            {
                ImageViewer.Show(gray);
            }
            else if (mode == Mode.Save && filename != null)
            {
                CreateFolderIfNotExists(Path.Combine(SAVE_PATH, "Short"));
                image.Save(Path.Combine(SAVE_PATH, "Short", filename + ".png"));
            }
        }*/

        private double DistanceFromLine(Point first, Point mid, Point last)
        {
            int A = last.Y - first.Y;
            int B = first.X - last.X;
            int C = last.X * first.Y - first.X * last.Y;

            return Math.Abs(A * mid.X + B * mid.Y + C) / Math.Sqrt(A * A + B * B);
        }

        public static double CalculateAngle(Point point1, Point point2)
        {
            double deltaX = point2.X - point1.X;
            double deltaY = point2.Y - point1.Y;

            double angleInRadians = Math.Atan2(deltaY, deltaX);
            double angleInDegrees = angleInRadians * (180.0 / Math.PI);

            if (angleInDegrees < 0)
                angleInDegrees += 360;

            return angleInDegrees;
        }

        private void CreateFolderIfNotExists(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        public List<Rectangle> GetAnnotationData()
        {
            List<Rectangle> result = new List<Rectangle>();

            XmlNodeList objects = doc.GetElementsByTagName("object");

            foreach (XmlNode obj in objects)
            {
                XmlNode? bndbox = obj["bndbox"];

                if (bndbox == null)
                    continue;

                string? xmin = bndbox["xmin"]?.InnerText;
                string? xmax = bndbox["xmax"]?.InnerText;
                string? ymin = bndbox["ymin"]?.InnerText;
                string? ymax = bndbox["ymax"]?.InnerText;

                if (xmin != null && xmax != null && ymin != null && ymax != null)
                    result.Add(new Rectangle(int.Parse(xmin), int.Parse(xmax), int.Parse(ymin), int.Parse(ymax)));
            }

            return result;
        }
    }
}
