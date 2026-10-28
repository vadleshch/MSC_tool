using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MSC_tool
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            try
            {
                var data = new List<double>();
                foreach (var raw in File.ReadLines(openFileDialog1.FileName))
                {
                    var line = raw.Trim();
                    if (line.Length == 0) continue;
                    line = line.Replace(',', '.');
                    if (double.TryParse(line, NumberStyles.Float, CultureInfo.InvariantCulture, out double x))
                        data.Add(x);
                }

                int n = data.Count;
                if (n < 2)
                {
                    MessageBox.Show("Потрібно щонайменше 2 числа у файлі.", "Помилка",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                double mean = data.Average();
                double ss = data.Sum(v => (v - mean) * (v - mean));
                double varSample = ss / (n - 1);
                double sd = Math.Sqrt(varSample);

                int df = n - 1;
                double t = Stats.StudentTQuantile(0.975, df);
                double margin = t * sd / Math.Sqrt(n);

                string result =
                    $"Файл: {Path.GetFileName(openFileDialog1.FileName)}\n" +
                    $"Обсяг вибірки n = {n}" + "\n\n" +
                    $"Матсподівання = {mean:G10}\n" +
                    $"Дисперсія = {varSample:G10}\n" +
                    "\n" +
                    $"t(0.975; {df}) = {t:G6}\n" +
                    $"95% довірчий інтервал:\n[{mean - margin:G10} ; {mean + margin:G10}]";

                MessageBox.Show(result, "Результати", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка читання файлу:\n" + ex.Message, "Помилка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "Текстові файли (*.txt)|*.txt|Усі файли (*.*)|*.*";
            openFileDialog1.Title = "Виберіть файл з вибіркою";
            openFileDialog1.ShowDialog();
        }
    }

    // ---------- t-розподіл Стьюдента ---------- Згенеровано ШІ (Cloude)
    static class Stats
    {
        public static double StudentTQuantile(double p, int df)
        {
            double lo = 0, hi = 1;
            while (StudentTCdf(hi, df) < p) hi *= 2;
            for (int i = 0; i < 200; i++)
            {
                double mid = (lo + hi) / 2;
                if (StudentTCdf(mid, df) < p) lo = mid; else hi = mid;
            }
            return (lo + hi) / 2;
        }

        static double StudentTCdf(double t, int df)
        {
            double x = df / (df + t * t);
            double ib = RegIncompleteBeta(df / 2.0, 0.5, x);
            return t >= 0 ? 1 - 0.5 * ib : 0.5 * ib;
        }

        static double RegIncompleteBeta(double a, double b, double x)
        {
            if (x <= 0) return 0;
            if (x >= 1) return 1;
            double lnFront = LogGamma(a + b) - LogGamma(a) - LogGamma(b)
                             + a * Math.Log(x) + b * Math.Log(1 - x);
            if (x < (a + 1) / (a + b + 2))
                return Math.Exp(lnFront) * BetaCF(a, b, x) / a;
            return 1 - Math.Exp(lnFront) * BetaCF(b, a, 1 - x) / b;
        }

        static double BetaCF(double a, double b, double x)
        {
            const double eps = 1e-15, tiny = 1e-300;
            double qab = a + b, qap = a + 1, qam = a - 1;
            double c = 1, d = 1 - qab * x / qap;
            if (Math.Abs(d) < tiny) d = tiny;
            d = 1 / d;
            double h = d;
            for (int m = 1; m <= 500; m++)
            {
                int m2 = 2 * m;
                double aa = m * (b - m) * x / ((qam + m2) * (a + m2));
                d = 1 + aa * d; if (Math.Abs(d) < tiny) d = tiny;
                c = 1 + aa / c; if (Math.Abs(c) < tiny) c = tiny;
                d = 1 / d; h *= d * c;
                aa = -(a + m) * (qab + m) * x / ((a + m2) * (qap + m2));
                d = 1 + aa * d; if (Math.Abs(d) < tiny) d = tiny;
                c = 1 + aa / c; if (Math.Abs(c) < tiny) c = tiny;
                d = 1 / d;
                double del = d * c;
                h *= del;
                if (Math.Abs(del - 1) < eps) break;
            }
            return h;
        }

        static double LogGamma(double x)
        {
            double[] g = { 676.5203681218851, -1259.1392167224028, 771.32342877765313,
                           -176.61502916214059, 12.507343278686905, -0.13857109526572012,
                           9.9843695780195716e-6, 1.5056327351493116e-7 };
            if (x < 0.5)
                return Math.Log(Math.PI / Math.Abs(Math.Sin(Math.PI * x))) - LogGamma(1 - x);
            x -= 1;
            double s = 0.99999999999980993;
            for (int i = 0; i < g.Length; i++) s += g[i] / (x + i + 1);
            double tt = x + g.Length - 0.5;
            return 0.5 * Math.Log(2 * Math.PI) + (x + 0.5) * Math.Log(tt) - tt + Math.Log(s);
        }
    }
}