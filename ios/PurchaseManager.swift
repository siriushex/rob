import Foundation
import StoreKit

/// Manages in-app purchases with StoreKit 2 and falls back to StoreKit 1 when necessary.
@available(iOS 15.0, *)
class PurchaseManager: ObservableObject {
    @Published var lastTransaction: Transaction?

    func purchase(productId: String) async throws {
        guard let product = try await Product.products(for: [productId]).first else { return }
        let result = try await product.purchase()
        switch result {
        case .success(let verification):
            if case .verified(let transaction) = verification {
                await transaction.finish()
                DispatchQueue.main.async { self.lastTransaction = transaction }
            }
        default: break
        }
    }

    func restore() async {
        for await result in Transaction.currentEntitlements {
            if case .verified(let transaction) = result {
                DispatchQueue.main.async { self.lastTransaction = transaction }
            }
        }
    }
}

/// Legacy implementation for iOS 14 and below using StoreKit 1.
class LegacyPurchaseManager: NSObject, SKPaymentTransactionObserver {
    @Published var lastTransaction: SKPaymentTransaction?

    func purchase(productId: String) {
        SKPaymentQueue.default().add(self)
        let payment = SKPayment(product: SKProduct())
        SKPaymentQueue.default().add(payment)
    }

    func restore() {
        SKPaymentQueue.default().add(self)
        SKPaymentQueue.default().restoreCompletedTransactions()
    }

    func paymentQueue(_ queue: SKPaymentQueue, updatedTransactions transactions: [SKPaymentTransaction]) {
        lastTransaction = transactions.first
    }
}
