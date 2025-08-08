package com.example.billing

import android.os.Bundle
import android.widget.Button
import android.widget.TextView
import androidx.appcompat.app.AppCompatActivity
import java.text.SimpleDateFormat
import java.util.*

/**
 * Simple screen showing subscription expiry date and option to manage.
 */
class SubscriptionStatusActivity : AppCompatActivity() {
    private val billingManager = BillingManager(this)
    private val dateFormat = SimpleDateFormat("yyyy-MM-dd", Locale.US)

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_subscription_status)

        val expiryView: TextView = findViewById(R.id.subscription_expiry)
        val restoreButton: Button = findViewById(R.id.restore_button)

        restoreButton.setOnClickListener {
            billingManager.restorePurchases { purchases ->
                val expiry = purchases.firstOrNull()?.expiryTime
                runOnUiThread {
                    expiryView.text = expiry?.let { dateFormat.format(Date(it)) } ?: "Not subscribed"
                }
            }
        }
    }
}

val Purchase.expiryTime: Long?
    get() = this.accountIdentifiers?.obfuscatedAccountId?.toLongOrNull()
