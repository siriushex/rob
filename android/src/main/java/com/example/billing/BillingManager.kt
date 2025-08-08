package com.example.billing

import android.app.Activity
import android.content.Context
import com.android.billingclient.api.*

/**
 * Simple wrapper around [BillingClient] to handle subscription purchase and restore.
 */
class BillingManager(private val context: Context) : PurchasesUpdatedListener {
    private val billingClient: BillingClient = BillingClient.newBuilder(context)
        .setListener(this)
        .enablePendingPurchases()
        .build()

    fun startConnection(onReady: () -> Unit) {
        billingClient.startConnection(object : BillingClientStateListener {
            override fun onBillingSetupFinished(result: BillingResult) {
                if (result.responseCode == BillingClient.BillingResponseCode.OK) {
                    onReady()
                }
            }

            override fun onBillingServiceDisconnected() {
                // Retry or notify user
            }
        })
    }

    fun purchase(activity: Activity, productId: String) {
        val params = BillingFlowParams.newBuilder()
            .setProductDetailsParamsList(
                listOf(
                    BillingFlowParams.ProductDetailsParams.newBuilder()
                        .setProductDetails(
                            queryProduct(productId) ?: return
                        )
                        .build()
                )
            )
            .build()
        billingClient.launchBillingFlow(activity, params)
    }

    private fun queryProduct(productId: String): ProductDetails? {
        val params = QueryProductDetailsParams.newBuilder()
            .setProductList(
                listOf(
                    QueryProductDetailsParams.Product.newBuilder()
                        .setProductId(productId)
                        .setProductType(BillingClient.ProductType.SUBS)
                        .build()
                )
            ).build()
        val result = billingClient.queryProductDetails(params)
        return result.productDetailsList?.firstOrNull()
    }

    fun restorePurchases(onResult: (List<Purchase>) -> Unit) {
        billingClient.queryPurchasesAsync(
            QueryPurchasesParams.newBuilder().setProductType(BillingClient.ProductType.SUBS).build()
        ) { _, purchases ->
            onResult(purchases)
        }
    }

    override fun onPurchasesUpdated(result: BillingResult, purchases: MutableList<Purchase>?) {
        if (result.responseCode == BillingClient.BillingResponseCode.OK && purchases != null) {
            // Handle purchased items
        }
    }
}
