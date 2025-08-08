import SwiftUI
import NetworkExtension

struct ContentView: View {
    @EnvironmentObject var viewModel: VPNViewModel

    var body: some View {
        VStack(spacing: 24) {
            Text("VPN Status: \(viewModel.status.description)")
            Button(action: {
                viewModel.toggle()
            }) {
                Text(viewModel.status == .connected ? "Disconnect" : "Connect")
                    .padding()
                    .frame(maxWidth: .infinity)
                    .background(Color.blue)
                    .foregroundColor(.white)
                    .cornerRadius(8)
            }
        }
        .padding()
        .onAppear {
            viewModel.loadManager()
        }
    }
}

#Preview {
    ContentView()
        .environmentObject(VPNViewModel())
}
