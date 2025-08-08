// swift-tools-version: 6.1
// The swift-tools-version declares the minimum version of Swift required to build this package.

import PackageDescription

let package = Package(
    name: "VPNApp",
    platforms: [
        .iOS(.v15)
    ],
    products: [
        .executable(name: "VPNApp", targets: ["App"])
    ],
    targets: [
        .executableTarget(
            name: "App",
            path: "App"
        ),
        .target(
            name: "PacketTunnel",
            path: "PacketTunnel"
        )
    ]
)
