﻿using System;
using System.Collections.Generic;
using System.Linq;
using FreePIE.Core.Contracts;
using FreePIE.Core.Plugins.Globals;
using FreePIE.Core.Plugins.SensorFusion;
using FreePIE.Core.Plugins.Yei3Space;

namespace FreePIE.Core.Plugins
{
    [GlobalType(Type = typeof(Yei3SpaceGlobal), IsIndexed = true)]
    public class Yei3SpacePlugin : Plugin
    {
        private IList<TssComPort> ports;
        private List<Yei3SpaceGlobalHolder> globals;

        public override object CreateGlobal()
        {
            return new GlobalIndexer<Yei3SpaceGlobal>(CreateDevice);
        }

        public override Action Start()
        {
            globals = new List<Yei3SpaceGlobalHolder>();
            ports = Api.GetComPorts();
            if(!ports.Any())
                throw new Exception("No YEI3 Space devices connected!");
            
            return null;
        }

        private Yei3SpaceGlobal CreateDevice(int index)
        {
            if (index >= ports.Count)
                throw new Exception(string.Format("Only {0} connected devices, {1} is out of bounds", ports.Count, index));

            var port = ports[index];
            var deviceId = Api.CreateDevice(port);
            if ((TssDeviceIdMask)deviceId == TssDeviceIdMask.TSS_NO_DEVICE_ID)
                throw new Exception(string.Format("Could not create device: {0} on port {1}", port.FriendlyName, port.Port));

            var holder = new  Yei3SpaceGlobalHolder(deviceId);
            globals.Add(holder);
            return holder.Global;
        }

        public override void DoBeforeNextExecute()
        {
            globals.ForEach(g => g.Update());
        }

        public override string FriendlyName
        {
            get { return "YEI 3 Space"; }
        }
    }

    public class Yei3SpaceGlobalHolder : IUpdatable
    {
        private readonly int deviceId;

        public Yei3SpaceGlobalHolder(int deviceId)
        {
            this.deviceId = deviceId;

            Global = new Yei3SpaceGlobal(this);
            Quaternion = new Quaternion();
        }

        public void Update()
        {
            var error = Api.UpdateQuaternion(deviceId, Quaternion);
            if (error != TssError.TSS_NO_ERROR)
                throw new Exception(string.Format("Error while reading device: {0}", error));

            OnUpdate();
        }

        public Quaternion Quaternion { get; private set; }
        public Yei3SpaceGlobal Global { get; private set; }
        public Action OnUpdate { get; set; }
        public bool GlobalHasUpdateListener { get; set; }
    }

    [Global(Name = "yei")]
    public class Yei3SpaceGlobal : UpdateblePluginGlobal<Yei3SpaceGlobalHolder>
    {
        public Yei3SpaceGlobal(Yei3SpaceGlobalHolder plugin)
            : base(plugin)
        {
        }
        public double Yaw { get { return plugin.Quaternion.Yaw; } }
        public double Pitch { get { return plugin.Quaternion.Pitch; } }
        public double Roll { get { return plugin.Quaternion.Roll; } }

    }
}