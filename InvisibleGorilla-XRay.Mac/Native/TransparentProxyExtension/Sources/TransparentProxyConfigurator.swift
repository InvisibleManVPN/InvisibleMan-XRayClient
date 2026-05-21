import Foundation
import NetworkExtension

struct TransparentProxyBridgeConfig: Decodable {
    let mode: String
    let socksUri: String?
    let socksPort: Int
    let socksUsername: String?
    let socksPassword: String?
    let tunnelAddress: String
    let dns: String
    let bundleIdentifiers: [String]
    let generatedAtUtc: Date
}

enum TransparentProxyConfigurator {
    static func installOrUpdate(from configURL: URL) throws {
        let data = try Data(contentsOf: configURL)
        let decoder = JSONDecoder()
        decoder.dateDecodingStrategy = .iso8601
        let config = try decoder.decode(TransparentProxyBridgeConfig.self, from: data)

        // TODO:
        // TODO:
        // 1. Discover installed applications on macOS.
        // 2. Map config.mode to the right NEAppRule behavior:
        //    - ALL_APPS => no per-app filtering.
        //    - BYPASS_SELECTED_APPS => include-list = all known bundle ids minus bundleIdentifiers.
        //    - ONLY_SELECTED_APPS => include-list = bundleIdentifiers.
        // 3. Save/update a NETransparentProxyManager profile bound to the signed provider target.
        print("Prepared transparent proxy scaffold for mode \(config.mode) with \(config.bundleIdentifiers.count) bundle ids")
    }
}
