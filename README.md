# Subscription Billing Demo

This repository demonstrates subscription handling for Android and iOS.

## Android
- Uses Google Play Billing library for purchasing and restoring subscriptions.
- `BillingManager` wraps `BillingClient` to initiate purchases and query existing subscriptions.
- `SubscriptionStatusActivity` shows subscription expiry and allows restoring purchases.

## iOS
- Implements StoreKit 2 with a fallback legacy manager using StoreKit 1.
- `SubscriptionView` provides a simple UI with a *Restore Purchases* button.

## Validation
- `ValidationService` sends purchase tokens or receipts to a backend endpoint for validation.

These modules are stubs and intended for demonstration purposes.
