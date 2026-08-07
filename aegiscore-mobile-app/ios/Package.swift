// swift-tools-version: 6.0
import PackageDescription

let package = Package(
    name: "AegisCoreIOS",
    platforms: [.iOS(.v18)],
    products: [
        .library(
            name: "AegisCoreIOS",
            targets: ["AegisCoreIOS"]
        )
    ],
    targets: [
        .target(
            name: "AegisCoreIOS",
            path: "AegisCore",
            resources: [
                .process("PrivacyInfo.xcprivacy")
            ]
        )
    ]
)
