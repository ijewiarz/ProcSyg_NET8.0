using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NWaves.Audio;
using NWaves.Filters.ChebyshevI;
using NWaves.Signals;
using NWaves.Signals.Builders;
using NWaves.Transforms;
using NWaves.Utils;
using ScottPlot;
using FftSharp;
using ProcSyg_NET8._0;

namespace ProcSyg
{
    internal class Program
    {
        static void Main(string[] args) {

            /* used for varius plots - test purposes
            var pltBefore = new ScottPlot.Plot();
            var pltAfter = new ScottPlot.Plot();
            */

            SignalHolder sH = new SignalHolder();
            sH.LoadSignal("test.wav");
            Console.WriteLine("Channels: " + sH.ChannelsCount.ToString());

            SignalOperations.ChangeDBLevel(sH, 3);
            SignalOperations.Delay(sH, 75);
            SignalOperations.AddWhiteNoise(sH, 0.01);
            SignalOperations.Echo(sH, 80);
            SignalOperations.TimeStretching(sH, 1.5);
            SignalOperations.LowPassFilter(sH, 500);
            SignalOperations.HighPassFilter(sH, 250);

            sH.CleanSignal();

            sH.SaveSignal("saved_test.wav");
            // end of program
            Console.WriteLine("Koniec.");

        }



        

        /* IN: array of double values
         * 
         * For test purposes.
         * 
         * OUT: -
         */
        private static void ShowArray(double[] arr) {
            foreach (var i in arr) {
                Console.WriteLine(i);
            }
            Console.WriteLine($"Length: {arr.Length}");
            Console.WriteLine("--------");
        }
    }

}





/* SINUS FOR TESTS
              
            
            int N = 216;
            int fp = 100;
            var pltSin = new ScottPlot.Plot();
            var sin = new SineBuilder().SetParameter("frequency", 1).OfLength(N).SampledAt(fp).Build();
            double[] sinTime = Generate.Consecutive(N, 1.0f / fp, 0);
            float[] sinSamples = sin.Samples;
            for (int i = 0; i < sinSamples.Length; ++i) sinSamples[i] += 0.2f;
            pltSin.Add.Scatter(sinTime, sinSamples);
            pltSin.SavePng(AppContext.BaseDirectory + "wykresSinus.png", 1500, 1000);


            var dSinSamples = sinSamples.Select(s => (double)s).ToArray();
            Console.WriteLine($"sin samples L before: {dSinSamples.Length}");
            ChangeDBLevelChunks(dSinSamples, 3);
            Console.WriteLine($"sin samples L after: {dSinSamples.Length}");

            double[] sinTimeAfter = Generate.Consecutive(dSinSamples.Length, 1.0f / fp, 0);
            Console.WriteLine($"X axis length: {sinTimeAfter.Length}");

            pltAfter.Add.Scatter(sinTimeAfter, dSinSamples);
            pltAfter.SavePng(AppContext.BaseDirectory + "wykresSinusAfter.png", 1500, 1000);
            
*/

/*
            int N = 512;
            int fp = 3000;
            var pltSin = new ScottPlot.Plot();
            var pltAfter = new ScottPlot.Plot();
            var pltFourier = new ScottPlot.Plot();

            var sin1 = new SineBuilder().SetParameter("frequency", 500).OfLength(N).SampledAt(fp).Build();
            var sin2 = new SineBuilder().SetParameter("frequency", 1000).OfLength(N).SampledAt(fp).Build();
            double[] sinTime = Generate.Consecutive(N, 1.0f / fp, 0);
            var sin = sin1 + sin2;
            float[] sinSamples = sin.Samples;
            //for (int i = 0; i < sinSamples.Length; ++i) sinSamples[i] += 0.2f;
            pltSin.Add.Scatter(sinTime, sinSamples);
            pltSin.SavePng(AppContext.BaseDirectory + "wykresSinus.png", 1500, 1000);

            var dSinSamples = sinSamples.Select(s => (double)s).ToArray();
            System.Numerics.Complex[] spectrum = FftSharp.FFT.Forward(dSinSamples);
            var magnitudeSpectrum = FftSharp.FFT.Magnitude(spectrum);
            double[] freqAxis = FftSharp.FFT.FrequencyScale(magnitudeSpectrum.Length, fp);
            pltFourier.Add.Scatter(freqAxis, magnitudeSpectrum);
            pltFourier.SavePng(AppContext.BaseDirectory + "wykresMagnitude.png", 1500, 1000);

            SignalOperations.HighPassFilter(dSinSamples, fp, 700);

            spectrum = FftSharp.FFT.Forward(dSinSamples);
            magnitudeSpectrum = FftSharp.FFT.Magnitude(spectrum);
            pltAfter.Add.Scatter(freqAxis, magnitudeSpectrum);
            pltAfter.SavePng(AppContext.BaseDirectory + "wykresMagnitudePoFiltrze.png", 1500, 1000);

            */