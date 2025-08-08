import SwiftUI
import StoreKit

struct SubscriptionView: View {
    @StateObject private var manager = PurchaseManager()
    @State private var expiryText = ""

    var body: some View {
        VStack(spacing: 20) {
            Text("Subscription expires: \(expiryText)")
            Button("Restore Purchases") {
                Task { await manager.restore(); updateExpiry() }
            }
        }
        .onAppear { updateExpiry() }
    }

    private func updateExpiry() {
        if let transaction = manager.lastTransaction {
            let date = transaction.expirationDate ?? Date()
            let formatter = DateFormatter()
            formatter.dateStyle = .medium
            expiryText = formatter.string(from: date)
        } else {
            expiryText = "Unknown"
        }
    }
}
