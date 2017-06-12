using SoloWordGame.Properties;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SoloWordGame
{
    public partial class frmMain : Form
    {
        public static IDictionary<int, IList<string>> dicc = new Dictionary<int, IList<string>>();
        public const string pattern = "abcdefghijklnmñopqrstuvxyz";
        public string[] abcd = pattern.ToCharArray().Select(x => Convert.ToString(x)).ToArray();

        private static IEnumerable<string> allWords;
        private static Stopwatch sw;
        private static Random rng;
        private static long ii;
        private static int pp;
        private static CancellationTokenSource tokenSource2;

        private delegate void SetTextCallback(string text);

        public frmMain()
        {
            InitializeComponent();
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            sw = new Stopwatch();

            sw.Start();

            SetEstado("Esperando al usuario...");

            allWords = Resources.diccionario.Split(Environment.NewLine.ToCharArray());
            foreach (string line in allWords)
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
            progressBar1.Maximum = (int)nudNumWords.Value * txtPalabra.Text.Length * abcd.Length;
            txtResueltos.Text = "";
            progressBar1.Value = 0;
            pp = 0;

            //Si no cancelamos el thread anterior vamos a tener problemas con la CPU
            if (tokenSource2 != null)
                tokenSource2.Cancel();

            tokenSource2 = new CancellationTokenSource();
            CancellationToken ct = tokenSource2.Token;

            Task.Run(() => {
                Resolver(ct);
            }, tokenSource2.Token);
        }

        private void Resolver(CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(txtPalabra.Text))
                return;

            sw = new Stopwatch();
            sw.Start();

            string word = txtPalabra.Text.ToLower(), 
                   curWord = word,
                   newWord = "",
                   cumWords = "",
                   stuckWord = "";
            bool found = false,
                 stucked = false;
            IList<string> lista = dicc[word.Length],
                          resolved = new List<string>();
            int numWords = (int)nudNumWords.Value,
                max = numWords * curWord.Length * abcd.Length;

            if(!allWords.Contains(curWord))
            {
                SetEstado(string.Format("La palabra {0} no existe.", curWord.ToUpper()));
                sw.Stop();
                return;
            }

            resolved.Add(curWord);
            abcd.Shuffle(rng);

            for (int i = 0; i < nudNumWords.Value; ++i)
            {
                for(int j = 0; j < curWord.Length; ++j)
                {
                    for(int k = 0; k < abcd.Length; ++k)
                    {
                        newWord = curWord.Remove(j, 1).Insert(j, abcd[k]);

                        foreach (string s in lista)
                        {
                            if (!resolved.Contains(s) && newWord == s)
                            {
                                resolved.Add(s);
                                cumWords += newWord.ToUpper() + Environment.NewLine;
                                SetText(cumWords);

                                curWord = newWord;
                                found = true;
                                break;
                            }

                            ++ii;
                        }

                        ++ii;

                        if (found || ct.IsCancellationRequested)
                        {
                            pp += (abcd.Length - k) * (curWord.Length - j);
                            break;
                        }
                        else
                            pp += (curWord.Length - j - 1);
                    }

                    ++ii;

                    if (found || ct.IsCancellationRequested)
                        break;
                }

                ++ii;

                if (curWord != stuckWord)
                {
                    ActualizarEstado(i, numWords, curWord, max);

                    //Paso de rallarme
                    if (pp >= max)
                        pp = max - 1;

                    UpdateBar(pp);

                    stuckWord = curWord;
                }
                else
                {
                    stucked = true;
                    UpdateBar(max);
                    UpdateBarState(2); //Error color
                    sw.Stop();
                    SetEstado(string.Format("ABORTADO. Tiempo: {0} ms | Últ. palabra {1} | Faltan {2} palabras por resolver | Iteraciones: {3} | CPU Rate: {4} it/ms", sw.ElapsedMilliseconds, curWord.ToUpper(), (numWords - i), ii.ToString("N0",
                                            CultureInfo.GetCultureInfo("es")), ii / sw.ElapsedMilliseconds));
                    break;
                }

                abcd.Shuffle(rng);
                found = false;
            }

            if (!ct.IsCancellationRequested && !stucked)
            {
                sw.Stop();
                UpdateBar(max);

                SetEstado(string.Format("Proceso finalizado en {0} ms! Con un total de {1} iteraciones. CPU Rate: {2} it/ms", sw.ElapsedMilliseconds, ii.ToString("N0",
                                        CultureInfo.GetCultureInfo("es")), ii / sw.ElapsedMilliseconds));
                Console.WriteLine("Resolved in {0} ms! Total iterations: {1}", sw.ElapsedMilliseconds, ii);
            }
        }

        private void ActualizarEstado(int i, int numWords, string curWord, int max)
        {
            SetEstado(string.Format("[{0} / {1}] Buscando palabra asociada para: '{2}' ({3:F2}%)", i, numWords, curWord.ToUpper(), ((double)pp / max) * 100));
        }

        private void UpdateBar(int i)
        {
            if (progressBar1.InvokeRequired)
                progressBar1.BeginInvoke(new Action<int>(UpdateBar), new object[] { i });
            else
                progressBar1.Value = i;
        }

        private void UpdateBarState(int state)
        {
            if (progressBar1.InvokeRequired)
                progressBar1.BeginInvoke(new Action<int>(UpdateBarState), new object[] { state });
            else
                ModifyProgressBarColor.SetState(progressBar1, state);
        }

        private void SetText(string text)
        {
            if (txtResueltos.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetText);
                Invoke(d, new object[] { text });
            }
            else
                txtResueltos.Text = text;
        }

        private void SetEstado(string text)
        {
            if (lblEstado.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetEstado);
                Invoke(d, new object[] { text });
            }
            else
                lblEstado.Text = text;
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

    public static class ModifyProgressBarColor
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr w, IntPtr l);
        public static void SetState(this ProgressBar pBar, int state)
        {
            SendMessage(pBar.Handle, 1040, (IntPtr)state, IntPtr.Zero);
        }
    }
}