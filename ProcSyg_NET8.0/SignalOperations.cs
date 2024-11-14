using NWaves.Effects;
using NWaves.Operations.Tsm;
using NWaves.Signals;
using ScottPlot.DataSources;
using ScottPlot.Plottables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcSyg_NET8._0
{
    internal class SignalOperations
    {

        // Method for adding white noise to the signals
        public static void AddWhiteNoise(SignalHolder sh, double noiseLevel = 0.01) {

            // create white noise and get its samples
            DiscreteSignal noise = new NWaves.Signals.Builders.WhiteNoiseBuilder().SetParameter("min", -noiseLevel)
                                                                                  .SetParameter("max", noiseLevel)
                                                                                  .SampledAt(sh.SamplingRate)
                                                                                  .OfLength(sh.SamplesList[0].Length) // if stereo => both channels have the same length
                                                                                  .Build();
            double[] noiseSamples = noise.Samples.Select(x => (double)x).ToArray();

            // apply to the signals
            for (int i = 0; i < sh.ChannelsCount; ++i) {
                for (int j = 0; j < sh.SamplesList[i].Length; ++j) {
                    sh.SamplesList[i][j] += noiseSamples[j];
                }
                // update signals list
                DiscreteSignal newSignal = new DiscreteSignal(sh.SamplingRate, sh.SamplesList[i].Select(x => (float)x).ToArray());
                sh.SignalsList[i] = newSignal;
            }

        }

        // Method for adding echo effect to the signals | feedback = 1 -> same power as signal
        public static void Echo(SignalHolder sh, int ms, float feedback=0.5f) {

            // convert ms to s
            float sec = ms / 1000f;
            var echoEffect = new EchoEffect(sh.SamplingRate, sec, feedback);

            // apply to the signals and update lists
            for (int i = 0; i < sh.ChannelsCount; ++i) {

                var echoSignal = echoEffect.ApplyTo(sh.SignalsList[i]);
                double[] samplesNew = echoSignal.Samples.Select(x => (double)x).ToArray();

                sh.SignalsList[i] = echoSignal;
                sh.SamplesList[i] = samplesNew;
            }

        }

        // Method for adding delay effect to the signals | feedback = 1 -> same power as signal
        public static void Delay(SignalHolder sh, int ms, float feedback=0.5f) {

            // convert ms to s
            float sec = ms / 1000f;
            var delayEffect = new DelayEffect(sh.SamplingRate, sec, feedback);

            // apply to the signals and update lists
            for (int i = 0; i < sh.ChannelsCount; ++i) {

                var delayedSignal = delayEffect.ApplyTo(sh.SignalsList[i]);
                double[] samplesNew = delayedSignal.Samples.Select(x => (double)x).ToArray();

                sh.SignalsList[i] = delayedSignal;
                sh.SamplesList[i] = samplesNew;
            }
            
        }

        /* Method for time stretching/shrinking operations
         * stretchingPar > 1 -> stretching, stretchigPar < 1 -> shrinking
         * ideas: adding ifs etc. for different signals parameters for optimalisations and speed
         */
        public static void TimeStretching(SignalHolder sh, double stretchingPar) {

            // one of the classes for time-stretching
            var pfipl = new PhaseLockingVocoder(stretchingPar, 128, 1024); // 128 -> hop par, 1024 -> fft size

            // apply to the signals and iptade lists
            for (int i = 0; i < sh.ChannelsCount; ++i) {

                var stretchedSingal = pfipl.ApplyTo(sh.SignalsList[i]);
                double[] samplesNew = stretchedSingal.Samples.Select(x => (double)x).ToArray();

                sh.SignalsList[i] = stretchedSingal;
                sh.SamplesList[i] = samplesNew;
            }
            
        }

        // Method for low-pass filtering
        public static void LowPassFilter(SignalHolder sh, int maxFreq) {

            for (int i = 0; i < sh.ChannelsCount; ++i) {

                // zero-pad samples to fit power of 2, at least 2 samples
                double[] samplesNew = FftSharp.Pad.ZeroPad(sh.SamplesList[i]);
                if (samplesNew.Length == 1) samplesNew = samplesNew.Append(0.0).ToArray();
                
                // get samples after filtering
                samplesNew = FftSharp.Filter.LowPass(samplesNew, sh.SamplingRate, maxFreq);
                // get rid of additional zeros from padding (if any)
                TruncateZeros(sh.SamplesList[i], ref samplesNew);

                // update lists
                sh.SamplesList[i] = samplesNew;
                DiscreteSignal newSignal = new DiscreteSignal(sh.SamplingRate, sh.SamplesList[i].Select(x => (float)x).ToArray());
                sh.SignalsList[i] = newSignal;
            }

        }

        // Method for high-pass filtering
        public static void HighPassFilter(SignalHolder sh, int minFreq) {

            for (int i = 0; i < sh.ChannelsCount; ++i) {

                // zero-pad samples to fit power of 2, at least 2 samples
                double[] samplesNew = FftSharp.Pad.ZeroPad(sh.SamplesList[i]);
                if (samplesNew.Length == 1) samplesNew = samplesNew.Append(0.0).ToArray();

                // get samples after filtering
                samplesNew = FftSharp.Filter.HighPass(samplesNew, sh.SamplingRate, minFreq);
                // get rid of additional zeros from padding (if any)
                TruncateZeros(sh.SamplesList[i], ref samplesNew);

                // update lists
                sh.SamplesList[i] = samplesNew;
                DiscreteSignal newSignal = new DiscreteSignal(sh.SamplingRate, sh.SamplesList[i].Select(x => (float)x).ToArray());
                sh.SignalsList[i] = newSignal;
            }

        }

        // Method for changing power level of signals (in dB)
        public static void ChangeDBLevel(SignalHolder sh, int dB) {

            int fftSize = 2058; // has to be power of 2
            int index = 0;

            for (int i = 0; i < sh.ChannelsCount; ++i) {

                // if data is smaller or equal than fft sample number
                if (sh.SamplesList[i].Length <= fftSize) {

                    double[] changedSamples = ChangeDBLevelChunk(sh.SamplesList[i], dB);
                    Array.Copy(changedSamples, sh.SamplesList[i], changedSamples.Length);

                } else {

                    double[] dataChunk = new double[fftSize];
                    double[] changedChunk = new double[fftSize];

                    // if data is bigger than fft sample number
                    for (index = 0; index < sh.SamplesList[i].Length; index += fftSize) {

                        // if not multiple of fft sample number
                        if (index + fftSize > sh.SamplesList[i].Length) break;

                        // change the block of data and applay to samples
                        Array.Copy(sh.SamplesList[i], index, dataChunk, 0, fftSize);
                        changedChunk = ChangeDBLevelChunk(dataChunk, dB);
                        Array.Copy(changedChunk, 0, sh.SamplesList[i], index, fftSize);

                    }
                    // how many samples left in input
                    int numberLeft = sh.SamplesList[i].Length - index;

                    // if any left
                    if (numberLeft > 0) {

                        // get rest of the samples
                        dataChunk = new double[numberLeft];
                        Array.Copy(sh.SamplesList[i], index, dataChunk, 0, numberLeft);

                        // applay changes
                        changedChunk = ChangeDBLevelChunk(dataChunk, dB);
                        Array.Copy(changedChunk, 0, sh.SamplesList[i], index, numberLeft);
                    }
                }

                // update signals list
                DiscreteSignal newSignal = new DiscreteSignal(sh.SamplingRate, sh.SamplesList[i].Select(x => (float)x).ToArray());
                sh.SignalsList[i] = newSignal;

            }

        }

        // Help method only used in ChangeDBLevel(...)
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
