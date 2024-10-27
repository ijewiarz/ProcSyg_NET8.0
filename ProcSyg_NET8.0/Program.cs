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

            var pltBefore = new ScottPlot.Plot();
            var pltAfter = new ScottPlot.Plot();

            
            WaveFile waveFile;
            // used for varius plots - not used yet
            

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
            int dB = -20;
            ChangeDBLevelChunks(samplesLeft, dB);
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
         * IN: samples of signal, value of decibels
         * 
         * Method to change the level of signal power
         * 
         * OUT: array with modified samples of signal
         */
        private static double[] ChangeDBLevel(double[] samples, int dB) {

            // zero-pad samples to fit power of 2, at least 2 samples
            double[] samplesNew = FftSharp.Pad.ZeroPad(samples);
            if (samplesNew.Length == 1) samplesNew = samplesNew.Append(0.0).ToArray();

            // get fft output
            System.Numerics.Complex[] spectrum = FftSharp.FFT.ForwardReal(samplesNew);
            int fftSize = spectrum.Length;

            // prepare array for spectrum changes
            System.Numerics.Complex[] newSpectrum = Array.Empty<System.Numerics.Complex>();

            // dB -> linear scale
            double scale = Math.Sqrt(Math.Pow(10, dB / 10.0));
            // applay changes
            for (int i = 0; i < fftSize; ++i) {
                newSpectrum = newSpectrum.Append(spectrum[i] * scale).ToArray();
            }

            // iverse fft
            samplesNew = FftSharp.FFT.InverseReal(newSpectrum);
            // get rid of additional zeros from padding (if any)
            TruncateZeros(samples, ref samplesNew);

            return samplesNew;

        }

        /*
         * IN: samples of signal, value of decibels
         * 
         * Method to change the level of signal power. It computes values in place
         * using chunks of data for much faster results (e.g. for audio processing)
         * 
         * OUT: -
         */
        private static void ChangeDBLevelChunks(double[] samples, int dB) {

            int fftSize = 2058; // has to be power of 2
            int index = 0;

            // if data is smaller or equal than fft sample number
            if (samples.Length <= fftSize) {
                
                double[] changedSamples = ChangeDBLevel(samples, dB);
                Array.Copy(changedSamples, samples, changedSamples.Length);

            } else {

                double[] dataChunk = new double[fftSize];
                double[] changedChunk = new double[fftSize];

                // if data is bigger than fft sample number
                for (index = 0; index < samples.Length; index += fftSize) {

                    // if not multiple of fft sample number
                    if (index + fftSize > samples.Length) break;

                    // change the block of data and applay to samples
                    Array.Copy(samples, index, dataChunk, 0, fftSize);
                    changedChunk = ChangeDBLevel(dataChunk, dB);
                    Array.Copy(changedChunk, 0, samples, index, fftSize);

                }
                // how many samples left in input
                int numberLeft = samples.Length - index;

                // if any left
                if (numberLeft > 0) {

                    // get rest of the samples
                    dataChunk = new double[numberLeft];
                    Array.Copy(samples, index, dataChunk, 0, numberLeft);

                    // applay changes
                    changedChunk = ChangeDBLevel(dataChunk, dB);
                    Array.Copy(changedChunk, 0, samples, index, numberLeft);
                }

            }

            
        }

        /*
         * IN: array of samples before padding, array of samples after padding
         * 
         * Method for cutting samples added from zero-padding (front and back). It works on array of samples
         * aftef IFFT. Original array of samples needed for its length (change to get arg only samples.Length?).
         * 
         * OUT: -
         */
        private static void TruncateZeros(double[] oldS, ref double[] paddedS) {

            // difference in length
            int diff = paddedS.Length - oldS.Length;
            // do nothing if the same length
            if (diff == 0) return;

            // if difference is even -> same amount of zeros in front and back
            if (diff % 2 == 0) { 
                paddedS = paddedS.Skip(diff / 2).Take(paddedS.Length - diff).ToArray();
            } else {
            // else -> one less in front than in the back
                int tmp = diff / 2;
                paddedS = paddedS.Skip(tmp).Take(paddedS.Length - tmp - 1).ToArray();
            }

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