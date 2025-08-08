import NetworkExtension

class PacketTunnelProvider: NEPacketTunnelProvider {
    private var ssClient: ShadowsocksClient?

    override func startTunnel(options: [String : NSObject]?, completionHandler: @escaping (Error?) -> Void) {
        do {
            let config = ShadowsocksConfig(server: "example.com", port: 8388, password: "password")
            ssClient = ShadowsocksClient(config: config)
            try ssClient?.start()

            let settings = NEPacketTunnelNetworkSettings(tunnelRemoteAddress: config.server)
            settings.ipv4Settings = NEIPv4Settings(addresses: ["10.0.0.2"], subnetMasks: ["255.255.255.0"])
            settings.mtu = 1500
            setTunnelNetworkSettings(settings) { error in
                completionHandler(error)
            }
        } catch {
            completionHandler(error)
        }
    }

    override func stopTunnel(with reason: NEProviderStopReason, completionHandler: @escaping () -> Void) {
        ssClient?.stop()
        completionHandler()
    }
}
