# UI Design Guidelines for VPN App

## Main Screen
- A large, centered **"Подключить VPN"** button occupies the primary focus.
- Below the button, display connection status (`Не подключено`/`Подключено`).
- Keep navigation minimal: top app bar with the logo on the left and a settings icon on the right.

## Themes

### Android (MaterialTheme)
- Define light and dark color palettes in `values/colors.xml` and `values-night/colors.xml`.
- Use `Theme.VPNApp` inheriting from `Material3`.
- Sample `themes.xml` snippet:
  ```xml
  <!-- values/themes.xml -->
  <style name="Theme.VPNApp" parent="Theme.Material3.Light.NoActionBar">
      <item name="colorPrimary">@color/teal_200</item>
      <item name="colorSecondary">@color/amber_200</item>
  </style>
  <!-- values-night/themes.xml -->
  <style name="Theme.VPNApp" parent="Theme.Material3.Dark.NoActionBar">
      <item name="colorPrimary">@color/teal_200</item>
      <item name="colorSecondary">@color/amber_200</item>
  </style>
  ```

### iOS (UIColor + UITraitCollection)
- Provide light and dark `UIColor` assets.
- Switch colors when `traitCollection.userInterfaceStyle` changes:
  ```swift
  override func traitCollectionDidChange(_ previousTraitCollection: UITraitCollection?) {
      super.traitCollectionDidChange(previousTraitCollection)
      view.backgroundColor = UIColor(named: "Background")
  }
  ```

## Branding
- Logo and icons follow a friendly outline style inspired by Outline/TunnelBear.
- Suggested palette: teal (#17a2b8), soft yellow (#f5c156), warm gray (#f2f2f2).
- Logo concept: simple bear or shield outline with smooth curves.
- Store assets under `assets/logo.png`, `assets/icons/` (placeholders for now).

## Resources
- Mockups can be produced in Figma using these guidelines.
- Ensure controls remain accessible and legible in both themes.
