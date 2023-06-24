using System;

namespace InvisibleManXRay.Core
{
    using Models;
    using Handlers.Proxies;
    using Handlers.Tunnels;
    using Values;
    using Utilities;

    public class InvisibleManXRayCore
    {
        private Func<Config> getConfig;
        private Func<Mode> getMode;
        private Func<Protocol> getProtocol;
        private Func<LogLevel> getLogLevel;
        private Func<string> getLogPath;
        private Func<int> getProxyPort;
        private Func<int> getTunPort;
        private Func<int> getTestPort;
        private Func<bool> getUdpEnabled;
        private Func<string> getTunIp;
        private Func<string> getDns;
        private Func<IProxy> getProxy;
        private Func<ITunnel> getTunnel;
        private Action<string> onFailLoadingConfig;

        public void Setup(
            Func<Config> getConfig, 
            Func<Mode> getMode,
            Func<Protocol> getProtocol,
            Func<LogLevel> getLogLevel,
            Func<string> getLogPath,
            Func<int> getProxyPort,
            Func<int> getTunPort,
            Func<int> getTestPort,
            Func<bool> getUdpEnabled,
            Func<string> getTunIp,
            Func<string> getDns,
            Func<IProxy> getProxy, 
            Func<ITunnel> getTunnel,
            Action<string> onFailLoadingConfig)
        {
            this.getConfig = getConfig;
            this.getMode = getMode;
            this.getProtocol = getProtocol;
            this.getLogLevel = getLogLevel;
            this.getLogPath = getLogPath;
            this.getProxyPort = getProxyPort;
            this.getTunPort = getTunPort;
            this.getTestPort = getTestPort;
            this.getUdpEnabled = getUdpEnabled;
            this.getTunIp = getTunIp;
            this.getDns = getDns;
            this.getProxy = getProxy;
            this.getTunnel = getTunnel;
            this.onFailLoadingConfig = onFailLoadingConfig;
        }
        
        public Status LoadConfig()
        {
            Config config = getConfig.Invoke();

            if (config == null)
                return new Status(Code.ERROR, SubCode.NO_CONFIG, Message.NO_CONFIGS_FOUND);

            return LoadConfig(config.Path);
        }

        public Status LoadConfig(string path)
        {
            if (!XRayCoreWrapper.IsFileExists(path))
            {
                onFailLoadingConfig.Invoke(path);
                return new Status(Code.ERROR, SubCode.NO_CONFIG, Message.NO_CONFIGS_FOUND);
            }

            string format = XRayCoreWrapper.GetConfigFormat(path);
            string file = XRayCoreWrapper.LoadConfig(format, path);

            if (!JsonUtility.IsJsonValid(file))
                return new Status(Code.ERROR, SubCode.INVALID_CONFIG, Message.INVALID_CONFIG);

            return new Status(Code.SUCCESS, SubCode.SUCCESS, file);
        }

        public Status EnableMode()
        {
            Mode mode = getMode.Invoke();
            
            if (mode == Mode.PROXY)
                return EnableProxy();
            else
                return EnableTunnel();
        }

        public void DisableMode()
        {
            DisableProxy();
            DisableTunnel();
        }

        public void Run(string config)
        {
            Mode mode = getMode.Invoke();
            int port = mode == Mode.PROXY ? getProxyPort.Invoke() : getTunPort.Invoke();
            LogLevel logLevel = getLogLevel.Invoke();
            string logPath = System.IO.Path.GetFullPath($"{getLogPath.Invoke()}/{getConfig.Invoke().Name}");
            bool isSocks = getProtocol.Invoke() == Protocol.SOCKS || mode == Mode.TUN;
            bool isUdpEnabled = getUdpEnabled.Invoke();

            XRayCoreWrapper.StartServer(config, port, logLevel, logPath, isSocks, isUdpEnabled);
        }

        public void Stop()
        {
            XRayCoreWrapper.StopServer();
        }

        public void Cancel()
        {
            CancelProxy();
            CancelTunnel();
        }

        public bool Test(string config)
        {
            return XRayCoreWrapper.TestConnection(config, getTestPort.Invoke());
        }

        public string GetVersion()
        {
            return XRayCoreWrapper.GetVersion();
        }

        private Status EnableProxy()
        {
            IProxy proxy = getProxy.Invoke();

            return proxy.Enable(
                ip: Global.LOCAL_HOST,
                port: getProxyPort.Invoke()
            );
        }

        private void DisableProxy()
        {
            IProxy proxy = getProxy.Invoke();
            proxy.Disable();
        }

        private Status EnableTunnel()
        {
            Status configStatus = LoadConfigFile();
            if (configStatus.Code == Code.ERROR)
                return configStatus;

            string server = JsonUtility.Find(
                key: "address",
                parent: "outbounds",
                jsonString: configStatus.Content.ToString()
            );
            int port = getTunPort.Invoke();
            string address = getTunIp.Invoke();
            string dns = getDns.Invoke();
            
            ITunnel tunnel = getTunnel.Invoke();

            return tunnel.Enable(
                ip: Global.LOCAL_HOST,
                port: port,
                address: address,
                server: server,
                dns: dns
            );

            Status LoadConfigFile()
            {
                Config config = getConfig.Invoke();

                if (config == null)
                    return new Status(Code.ERROR, SubCode.NO_CONFIG, Message.NO_CONFIGS_FOUND);
                
                return new Status(Code.SUCCESS, SubCode.SUCCESS, System.IO.File.ReadAllText(config.Path).ToLower());
            }
        }

        private void DisableTunnel()
        {
            ITunnel tunnel = getTunnel.Invoke();
            tunnel.Disable();
        }

        private void CancelProxy()
        {
            IProxy proxy = getProxy.Invoke();
            proxy.Cancel();
        }

        private void CancelTunnel()
        {
            ITunnel tunnel = getTunnel.Invoke();
            tunnel.Cancel();
        }
    }
}