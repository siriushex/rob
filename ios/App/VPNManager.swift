import Foundation
import NetworkExtension
import Combine

final class VPNViewModel: ObservableObject {
    @Published var status: NEVPNStatus = .invalid
    private var manager: NETunnelProviderManager?
    private var statusObserver: AnyCancellable?

    init() {
        loadManager()
    }

    func loadManager() {
        NETunnelProviderManager.loadAllFromPreferences { managers, error in
            if let existing = managers?.first {
                self.manager = existing
            } else {
                self.manager = NETunnelProviderManager()
                self.configureManager()
            }
            self.status = self.manager?.connection.status ?? .invalid
            self.observeStatus()
        }
    }

    private func observeStatus() {
        guard let connection = manager?.connection else { return }
        statusObserver = NotificationCenter.default.publisher(for: .NEVPNStatusDidChange, object: connection)
            .sink { [weak self] _ in
                self?.status = connection.status
            }
    }

    func toggle() {
        guard let manager = manager else { return }
        switch manager.connection.status {
        case .connected, .connecting:
            manager.connection.stopVPNTunnel()
        default:
            do {
                try manager.connection.startVPNTunnel()
            } catch {
                print("Start error: \(error)")
            }
        }
    }

    private func configureManager() {
        guard let manager = manager else { return }
        let protocolConfig = NETunnelProviderProtocol()
        protocolConfig.providerBundleIdentifier = "com.example.PacketTunnel"
        protocolConfig.serverAddress = "127.0.0.1"
        protocolConfig.includeAllNetworks = true
        protocolConfig.excludeLocalNetworks = true
        manager.protocolConfiguration = protocolConfig
        manager.localizedDescription = "Shadowsocks VPN"
        manager.isEnabled = true

        // Kill Switch rule: block traffic when tunnel is down
        let rule = NEOnDemandRuleDisconnect()
        rule.interfaceTypeMatch = .any
        manager.isOnDemandEnabled = true
        manager.onDemandRules = [rule]

        manager.saveToPreferences { error in
            if let error = error {
                print("Preferences error: \(error)")
            }
        }
    }
}

extension NEVPNStatus: CustomStringConvertible {
    public var description: String {
        switch self {
        case .connected: return "Connected"
        case .connecting: return "Connecting"
        case .disconnected: return "Disconnected"
        case .disconnecting: return "Disconnecting"
        case .invalid: return "Invalid"
        case .reasserting: return "Reasserting"
        @unknown default: return "Unknown"
        }
    }
}
