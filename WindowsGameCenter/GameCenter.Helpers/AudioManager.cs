using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Media.Devices;

namespace GameCenter.Helpers
{
    public class AudioDeviceManager
    {
        public ObservableCollection<AudioDevice> AudioDevices { get; private set; }
        public AudioDevice DefaultDevice { get; private set; }

        public AudioDeviceManager()
        {
            AudioDevices = new ObservableCollection<AudioDevice>();
        }

        public async Task LoadAudioDevicesAsync()
        {
            var deviceSelector = MediaDevice.GetAudioRenderSelector();
            var devices = await DeviceInformation.FindAllAsync(deviceSelector);

            AudioDevices.Clear();
            foreach (var device in devices)
            {
                AudioDevices.Add(new AudioDevice { Id = device.Id, Name = device.Name });
            }

            string defaultDeviceId = MediaDevice.GetDefaultAudioRenderId(AudioDeviceRole.Default);
            DefaultDevice = AudioDevices.FirstOrDefault(d => d.Id == defaultDeviceId);
        }

        public async Task SetDefaultAudioDeviceAsync(AudioDevice device)
        {
            
        }
    }
}
