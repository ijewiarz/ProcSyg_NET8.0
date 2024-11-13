using NWaves.Effects;
using NWaves.Operations.Tsm;
using NWaves.Signals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcSyg_NET8._0
{
    internal class SignalOperations
    {

        public static void Echo(DiscreteSignal signal, ref double[] samples, int ms, float feedback=0.5f) {

            float sec = ms / 1000f;
            var echoEffect = new EchoEffect(signal.SamplingRate, sec, feedback);
            var echoSignal = echoEffect.ApplyTo(signal);

            double[] samplesNew = echoSignal.Samples.Select(x => (double)x).ToArray();

            samples = samplesNew;
        }


        // take ms variable and make it true ms since constructor takes sec, feedback I assume 1 => same power as original
        public static void Delay(DiscreteSignal signal, ref double[] samples, int ms, float feedback=0.5f) {

            float sec = ms / 1000f;
            var delayEffect = new DelayEffect(signal.SamplingRate, sec, feedback);
            var delayedSignal = delayEffect.ApplyTo(signal);

            double[] samplesNew = delayedSignal.Samples.Select(x => (double)x).ToArray();

            samples = samplesNew;
        }

        // can do some ifs and what-nots for different signal lengths / stretch parameters 
        public static void TimeStreaching(DiscreteSignal signal, ref double[] samples, int stretchingPar) {

            // class for time-stretching
            var pfipl = new PhaseLockingVocoder(stretchingPar, 128, 1024); // par stretch > 1 => elongated signal ( e.g. 2 gives twice as long), < 1 shortened signal

            // apply to signal
            var stretchedSingal = pfipl.ApplyTo(signal);

            // get new samples of changed signal
            double[] samplesNew = stretchedSingal.Samples.Select(x => (double)x).ToArray();

            //Console.WriteLine($"samples L: {samples.Length} | samplesNew L: {samplesNew.Length}");

            samples = samplesNew;
            
        }


        // sampleRate dawać taki jaki jest sygnału wczytanego do końcowego programu
        // praca w chunkach nie działa idk czemu jeszcze
        public static void LowPassFilter(double[] samples, int sampleRate, int maxFreq) {

            // zero-pad samples to fit power of 2, at least 2 samples
            double[] samplesNew = FftSharp.Pad.ZeroPad(samples);
            if (samplesNew.Length == 1) samplesNew = samplesNew.Append(0.0).ToArray();
            samplesNew = FftSharp.Filter.LowPass(samplesNew, sampleRate, maxFreq);
            // get rid of additional zeros from padding (if any)
            TruncateZeros(samples, ref samplesNew);

            Array.Copy(samplesNew, samples, samples.Length);

        }

        public static void HighPassFilter(double[] samples, int sampleRate, int minFreq) {

            // zero-pad samples to fit power of 2, at least 2 samples
            double[] samplesNew = FftSharp.Pad.ZeroPad(samples);
            if (samplesNew.Length == 1) samplesNew = samplesNew.Append(0.0).ToArray();

            samplesNew = FftSharp.Filter.HighPass(samplesNew, sampleRate, minFreq);
            // get rid of additional zeros from padding (if any)
            TruncateZeros(samples, ref samplesNew);

            Array.Copy(samplesNew, samples, samples.Length);


        }


        /*
         * IN: samples of signal, value of decibels
         * 
         * Method to change the level of signal power. It's IN PLACE computing
         * using chunks of data for much faster results (e.g. for audio processing)
         * 
         * OUT: -
         */
        public static void ChangeDBLevel(double[] samples, int dB) {

            int fftSize = 2058; // has to be power of 2
            int index = 0;

            // if data is smaller or equal than fft sample number
            if (samples.Length <= fftSize) {

                double[] changedSamples = ChangeDBLevelChunk(samples, dB);
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
                    changedChunk = ChangeDBLevelChunk(dataChunk, dB);
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
                    changedChunk = ChangeDBLevelChunk(dataChunk, dB);
                    Array.Copy(changedChunk, 0, samples, index, numberLeft);
                }
            }
        }

        /*
         * IN: samples of signal, value of decibels
         * 
         * Method to change the level of signal power
         * 
         * OUT: array with modified samples of signal
         */
        private static double[] ChangeDBLevelChunk(double[] samples, int dB) {

            // zero-pad samples to fit power of 2, at least 2 samples
            double[] samplesNew = FftSharp.Pad.ZeroPad(samples);
            if (samplesNew.Length == 1) samplesNew = samplesNew.Append(0.0).ToArray();

            // get fft output
            System.Numerics.Complex[] spectrum = FftSharp.FFT.ForwardReal(samplesNew); // tutaj działa to real bo pracuje tylko na rzeczywistym sygnale tj. przed częstotliwością nyquista
            int fftSize = spectrum.Length;                                             // do magnitue/power trzeba wsadzać wartości wszystkie z .Forward(), bo on to potem tnie na pół

            // prepare array for spectrum changes
            System.Numerics.Complex[] newSpectrum = Array.Empty<System.Numerics.Complex>();

            // dB -> linear scale
            double scale = Math.Sqrt(Math.Pow(10, dB / 10.0));
            // applay changes
            for (int i = 0; i < fftSize; ++i) {
                newSpectrum = newSpectrum.Append(spectrum[i] * scale).ToArray();
            }

            // inverse fft
            samplesNew = FftSharp.FFT.InverseReal(newSpectrum);
            // get rid of additional zeros from padding (if any)
            TruncateZeros(samples, ref samplesNew);

            return samplesNew;
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



        //end of class
    }
}
