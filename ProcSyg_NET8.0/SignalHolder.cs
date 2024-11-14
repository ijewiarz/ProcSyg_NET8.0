using NWaves.Audio;
using NWaves.Signals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace ProcSyg_NET8._0
{
    internal class SignalHolder {

        private WaveFormat header;

        private short channelsCount; // mono/stereo 
        private int samplingRate;
        private List<DiscreteSignal>? originalSignal; // original loaded signals
        private List<DiscreteSignal> signalsList = new(); // signals after modifications
        private List<double[]> samplesList = new(); // samples of signals after modifications

        // Accessors
        public short ChannelsCount {  get { return channelsCount; } }
        public int SamplingRate { get { return samplingRate; } }
        public List<double[]> SamplesList { get { return samplesList; } }
        public List<DiscreteSignal> SignalsList { get { return signalsList; } }
 
        // Method for loading .wav file
        public void LoadSignal(string path) {

            // read file
            using (var stream = new FileStream(path, FileMode.Open)) {
                var waveSignal = new WaveFile(stream);
                // get header and original signals
                header = waveSignal.WaveFmt;
                originalSignal = waveSignal.Signals;
            }
            // extract informations from header
            channelsCount = header.ChannelCount;
            samplingRate = header.SamplingRate;

            // create list of signals and their samples
            signalsList = new();
            for (int i = 0; i < originalSignal.Count; ++i) {
                signalsList.Add(originalSignal[i]);
            }
            samplesList = new();
            for (int i = 0; i < signalsList.Count; ++i) {
                samplesList.Add(signalsList[i].Samples.Select(x => (double)x).ToArray());
            }

        }

        // Method for saving .wav file
        public void SaveSignal(string path) {

            WaveFile output = new WaveFile(signalsList, header.BitsPerSample);
            // create and save .wav file
            using (var stream = new FileStream(path, FileMode.Create)) {
                output.SaveTo(stream);
            }
        }

        // Undo all signals operations
        public void CleanSignal() {
            
            // if file loaded
            if (originalSignal != null) {

                // create new signals thier samples lists (clean signals and samples)
                signalsList = new();
                for (int i = 0; i < originalSignal.Count; ++i) {
                    signalsList.Add(originalSignal[i]);
                }
                samplesList = new();
                for (int i = 0; i < signalsList.Count; ++i) {
                    samplesList.Add(signalsList[i].Samples.Select(x => (double)x).ToArray());
                }

            }

        }


    }
}
