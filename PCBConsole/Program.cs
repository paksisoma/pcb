using System.Text.RegularExpressions;
using static Constants;

namespace PCBConsole
{
    internal class Program
    {
        static void Main(string[] args)
        {
            RunMissingHole(Mode.Show);
            RunMouseBite(Mode.Show);
        }

        private static void RunMissingHole(Mode mode)
        {
            string[] images = Directory.GetFiles(Path.Combine(FOLDER_PATH, "images", "Missing_hole"));

            int i = 0;

            foreach (string image in images)
            {
                string number = GetNumber(image);
                string xml = Path.Combine(FOLDER_PATH, "Annotations", "Missing_hole", GetFilename(image) + ".xml");
                PCB pcb = new PCB(image, xml, mode, i.ToString(), GetFilename(image));

                switch (number)
                {
                    case "01":
                        pcb.RunMissingHole(40);
                        break;
                    case "04":
                        pcb.RunMissingHole(45, 10, 150);
                        break;
                    case "06":
                        pcb.RunMissingHole(40);
                        break;
                    case "07":
                        pcb.RunMissingHole(40, 5, 200);
                        break;
                    case "08":
                        pcb.RunMissingHole(40, 10, 100, 0.8);
                        break;
                    case "09":
                        pcb.RunMissingHole(40, 20, 100, 0.8);
                        break;
                    case "10":
                        pcb.RunMissingHole(40, 100, 400); //?
                        break;
                    case "11":
                        pcb.RunMissingHole(40, 10, 400);
                        break;
                    case "12":
                        pcb.RunMissingHole(45, 10, 150);
                        break;
                    default:
                        break;
                }

                i++;
            }
        }

        private static void RunMouseBite(Mode mode)
        {
            string[] images = Directory.GetFiles(Path.Combine(FOLDER_PATH, "images", "Mouse_bite"));

            int i = 0;

            foreach (string image in images)
            {
                string number = GetNumber(image);

                string xml = Path.Combine(FOLDER_PATH, "Annotations", "Mouse_bite", GetFilename(image) + ".xml");
                PCB pcb = new PCB(image, xml, mode, i.ToString(), GetFilename(image));

                switch (number)
                {
                    case "01":
                        pcb.RunMouseBite(50, 12, 4, null);
                        break;
                    case "04":
                        pcb.RunMouseBite(150, 25, 4, 25);
                        break;
                    case "06":
                        pcb.RunMouseBite(150, 25, 10, 25);
                        break;
                    case "07":
                        pcb.RunMouseBite(100, 20, 10, null);
                        break;
                    case "08":
                        //pcb.RunMouseBite(100, 20, 9, null);
                        break;
                    case "09":
                        //pcb.RunMouseBite(100, 25, 10, null);
                        break;
                    case "10":
                        pcb.RunMouseBite(200, 25, 17, null);
                        break;
                    case "11":
                        pcb.RunMouseBite(150, 25, 10, 20);
                        break;
                    case "12":
                        pcb.RunMouseBite(150, 25, 11, 20);
                        break;
                    default:
                        break;
                }

                i++;
            }
        }

        private static string GetFilename(string path)
        {
            Regex rg = new Regex(@"[^\\)]+(?=\.[^.]+$)");

            MatchCollection matchedAuthors = rg.Matches(path);

            if (matchedAuthors.Count > 0)
                return matchedAuthors[0].Value;
            else
                return "";
        }

        private static string GetNumber(string path)
        {
            Regex rg = new Regex(@"\\([\d][\d])_");

            MatchCollection matchedAuthors = rg.Matches(path);

            if (matchedAuthors.Count > 0)
                return matchedAuthors[0].Groups[1].Value;
            else
                return "";
        }
    }
}
