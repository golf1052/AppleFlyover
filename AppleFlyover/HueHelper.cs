﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Q42.HueApi;
using Q42.HueApi.Interfaces;

namespace AppleFlyover
{
    public class HueHelper : INotifyPropertyChanged
    {
        private ILocalHueClient hueClient;
        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<string> Lights { get; set; }
        private Dictionary<string, string> lightToId;
        private Dictionary<string, string> groupToId;
        private string selectedLight;
        private bool lightOrGroup;

        private byte lightBrightness;
        public byte LightBrightness
        {
            get { return lightBrightness; }
            private set { lightBrightness = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LightBrightness))); }
        }

        private bool lightOn;
        public bool LightOn
        {
            get { return lightOn; }
            private set { lightOn = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LightOn))); }
        }

        public HueHelper()
        {
            Lights = new ObservableCollection<string>();
            lightToId = new Dictionary<string, string>();
            groupToId = new Dictionary<string, string>();
        }

        public async Task Setup()
        {
            IBridgeLocator locator = new HttpBridgeLocator();
            var ips = (await locator.LocateBridgesAsync(TimeSpan.FromSeconds(5))).ToList();
            if (ips.Count > 0)
            {
                hueClient = new LocalHueClient(ips[0].IpAddress, Secrets.HueUsername);
            }

            var lights = await hueClient.GetLightsAsync();
            foreach (var light in lights)
            {
                lightToId.Add(light.Name, light.Id);
                Lights.Add(light.Name);
            }

            var groups = await hueClient.GetGroupsAsync();
            foreach (var group in groups)
            {
                var name = group.Name;
                if (Lights.Contains(name))
                {
                    name = $"{name} (group)";
                }
                groupToId.Add(name, group.Id);
                Lights.Add(name);
            }
        }

        public async Task SelectLight(string name)
        {
            selectedLight = name;
            if (lightToId.ContainsKey(selectedLight))
            {
                lightOrGroup = true;
                var light = await hueClient.GetLightAsync(lightToId[selectedLight]);
                LightBrightness = light.State.Brightness;
                LightOn = light.State.On;
            }
            else if (groupToId.ContainsKey(selectedLight))
            {
                lightOrGroup = false;
                var group = await hueClient.GetGroupAsync(groupToId[selectedLight]);
                LightBrightness = group.Action.Brightness;
                LightOn = group.State.AnyOn.Value;
            }
        }

        public async Task ChangeBrightness(byte value)
        {
            LightCommand command = new LightCommand();
            command.Brightness = value;
            await SendCommand(command);
        }

        public async Task ToggleLight()
        {
            LightCommand command = new LightCommand();
            command.On = !LightOn;
            await SendCommand(command);
            LightOn = !LightOn;
        }

        private async Task SendCommand(LightCommand command)
        {
            string id = string.Empty;
            if (lightOrGroup)
            {
                id = lightToId[selectedLight];
                hueClient.SendCommandAsync(command, new string[] { id });
            }
            else
            {
                id = groupToId[selectedLight];
                hueClient.SendGroupCommandAsync(command, id);
            }
        }
    }
}