import SwiftUI
import NetworkExtension

@main
struct VPNApp: App {
    @StateObject private var viewModel = VPNViewModel()

    var body: some Scene {
        WindowGroup {
            ContentView()
                .environmentObject(viewModel)
        }
    }
}
