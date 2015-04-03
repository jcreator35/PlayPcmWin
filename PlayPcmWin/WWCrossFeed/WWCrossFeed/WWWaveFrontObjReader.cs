using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace WWCrossFeed {
    class WWWaveFrontObjReader {
        public static WW3DModel ReadFromFile(string path) {
            string [] lines = null;

            try {
                lines = File.ReadAllLines(path);
            } catch (IOException ex) {
                Console.WriteLine("WW3DModel.Read(" + path + ") " + ex.Message);
                return null;
            }

            return Generate(lines);
        }

        public static WW3DModel ReadFromStream(Stream s) {
            var list = new List<string>();
            using (var sr = new StreamReader(s)) {
                string line;
                while ((line = sr.ReadLine()) != null) {
                    list.Add(line);
                }
            }

            string[] lines = list.ToArray();
            return Generate(lines);
        }

        public static WW3DModel Generate(string [] lines) {
            var vertices = new List<Point3D>();
            var indices = new List<int>();

            foreach (var l in lines) {
                if (l.Length < 7) {
                    continue;
                }

                var s = l.Split(' ');
                if ("v".Equals(s[0]) && 4 <= s.Length) {
                    // triangle vertex
                    var p = new Point3D(
                            Convert.ToDouble(s[1]), Convert.ToDouble(s[2]), Convert.ToDouble(s[3]));
                    vertices.Add(p);
                }

                if ("f".Equals(s[0]) && 4 <= s.Length) {
                    // triangle index. index starts from 1
                    indices.Add(Convert.ToInt32(s[1]) - 1);
                    indices.Add(Convert.ToInt32(s[2]) - 1);
                    indices.Add(Convert.ToInt32(s[3]) - 1);
                }
            }

            if (vertices.Count == 0 || indices.Count == 0) {
                return null;
            }
            return new WW3DModel(vertices.ToArray(), indices.ToArray());
        }
    }
}
