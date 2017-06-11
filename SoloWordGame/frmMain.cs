using SoloWordGame.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SoloWordGame
{
    public partial class frmMain : Form
    {
        public static Dictionary<int, List<string>> dicc = new Dictionary<int, List<string>>();
        public const string pattern = "abcdefghijklnmñopqrstuvxyz";
        public string[] abcd = pattern.ToCharArray().Select(x => Convert.ToString(x)).ToArray();
        private static Stopwatch sw;
        private static Random rng;

        public frmMain()
        {
            InitializeComponent();
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            sw = new Stopwatch();

            sw.Start();
            var lines = Resources.diccionario.Split(Environment.NewLine.ToCharArray());
            foreach (string line in lines)
            {
                int len = line.Length;
                bool exist = dicc.ContainsKey(len);

                var lone = exist ? dicc[len] : new List<string>();

                lone.Add(line);

                if (!exist)
                    dicc.Add(len, lone);
                else
                    dicc[len] = lone;
            }

            rng = new Random(string.IsNullOrWhiteSpace(txtSeed.Text) ? new Random().Next(int.MaxValue) : Convert.ToInt32(Encoding.Unicode.GetBytes(txtSeed.Text)));
            sw.Stop();

            Console.WriteLine("Everything loaded in {0} ms!", sw.ElapsedMilliseconds);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            sw = new Stopwatch();

            sw.Start();
            Resolver();
            sw.Stop();

            Console.WriteLine("Resolved in {0} ms!", sw.ElapsedMilliseconds);
        }

        private void Resolver()
        {
            if (string.IsNullOrWhiteSpace(txtPalabra.Text))
                return;

            txtResueltos.Text = "";

            string word = txtPalabra.Text.ToLower(), curWord = word, newWord = "";
            bool found = false;
            List<string> lista = dicc[word.Length],
                         resolved = new List<string>();

            resolved.Add(curWord);
            abcd.Shuffle(rng);

            for (int i = 0; i < nudNumWords.Value; ++i)
            {
                for(int j = 0; j < curWord.Length; ++j)
                {
                    for(int k = 0; k < abcd.Length; ++k)
                    {
                        newWord = curWord.Remove(j, 1).Insert(j, abcd[k]);

                        foreach(string s in lista)
                            if(!resolved.Contains(s) && newWord == s)
                            {
                                resolved.Add(s);
                                txtResueltos.Text += newWord.ToUpper() + Environment.NewLine;

                                curWord = newWord;
                                found = true;
                                break;
                            }

                        if (found)
                            break;
                    }

                    if (found)
                        break;
                }

                found = false;
            }
        }
    }

    public static class ListHelpers
    { //Pa la API
        public static void Shuffle<T>(this IList<T> list, Random rng)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}
