// swift-tools-version: 6.0
import PackageDescription

let package = Package(
    name: "NocturneIOS",
    platforms: [.iOS(.v17)],
    products: [.library(name: "NocturneIOS", targets: ["NocturneIOS"])],
    targets: [.target(name: "NocturneIOS", path: "Nocturne")]
)
