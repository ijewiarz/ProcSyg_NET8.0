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

namespace ProcSyg
{
    internal class Program
    {
        static void Main(string[] args) {

            WaveFile waveFile;
            // used for varius plots - not used yet
            var pltBefore = new ScottPlot.Plot();
            var pltAfter = new ScottPlot.Plot();

            // Load .wav file
            using (var stream = new FileStream("test3s.wav", FileMode.Open)) {
                waveFile = new WaveFile(stream);
            }
            // Extract header
            WaveFormat Header = waveFile.WaveFmt;

            Console.WriteLine($"Channels: {Header.ChannelCount}");

            // Extract audio channel and its samples
            DiscreteSignal chLeft = waveFile[Channels.Left];
            double[] samplesLeft = chLeft.Samples.Select(s => (double)s).ToArray();

            // Change dB level of audio
            int dB = 10;
            samplesLeft = ChangeDBLevel(samplesLeft, dB);
            // cast to float for DiscreteSignal class
            float[] fSamplesLeft = samplesLeft.Select(s => (float)s).ToArray();
            
            // Constructing output .wav file
            var outL = new DiscreteSignal(Header.SamplingRate, fSamplesLeft);
            WaveFile output = new WaveFile(new[] { outL });

            // Save .wav file
            using (var stream = new FileStream("saved_test.wav", FileMode.Create)) {
                output.SaveTo(stream);
            }
            

            // end of program
            Console.WriteLine("Koniec.");

        }



        /*
         * Ideas: move fft to main, normalize/denormalize signal
         * To do: do fft in block of e.g. 1024 and ZeroPad only the last block -> should be omega faster
         *        check the difference of ForwardReal and Forward (ForwardReal still gives imag values ???)
         *        find a way to truncate signal from zero-padding
         * 
         */
        private static double[] ChangeDBLevel(double[] samples, int dB) {

            // zero-pad samples to fit power of 2
            samples = FftSharp.Pad.ZeroPad(samples);
            // get fft output
            System.Numerics.Complex[] spectrum = FftSharp.FFT.ForwardReal(samples);
            int fftSize = spectrum.Length;
            // prepare array for spectrum changes
            System.Numerics.Complex[] newSpectrum = Array.Empty<System.Numerics.Complex>();

            // test purposes
            Console.WriteLine(spectrum[10]);

            // dB -> linear scale
            double scale = Math.Sqrt(Math.Pow(10, dB / 10.0));
            // applay changes
            for (int i = 0; i < fftSize; ++i) {
                newSpectrum = newSpectrum.Append(spectrum[i] * scale).ToArray();
                //Console.WriteLine($"{newSpectrum[i]} {spectrum[i]}");
            }
            //Console.WriteLine($"{fftSize}, {newSpectrum.Length}");

            // iverse fft
            return FftSharp.FFT.InverseReal(newSpectrum);
        }






    }

}





/* SINUS
              
            var plt = new ScottPlot.Plot();
            int N = 512;
            int fp = 200;
            var pltSin = new ScottPlot.Plot();
            var sin = new SineBuilder().SetParameter("frequency", 10).OfLength(N).SampledAt(fp).Build();
            double[] sinTime = Generate.Consecutive(N, 1.0f / fp, 0);
            pltSin.Add.Scatter(sinTime, sin.Samples);
            pltSin.SavePng(AppContext.BaseDirectory + "wykresSinus.png", 1500, 1000);

            var sinSamples = sin.Samples;
            var dSinSamples= sinSamples.Select(s => (double)s).ToArray();
            double[] changedSin = ChangeMagnitudeLevel(dSinSamples, 2);
            Console.WriteLine($"Pre: {dSinSamples[5]} Post: {changedSin[5]}");
            pltAfter.Add.Scatter(sinTime, changedSin);
            pltAfter.SavePng(AppContext.BaseDirectory + "wykresSinusAfter.png", 1500, 1000);
            */