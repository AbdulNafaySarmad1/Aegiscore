import Foundation

struct AttendanceRecord: Codable, Identifiable {
    let id: String
    let studentId: String
    let status: String
    let wasOfflineAt: Date
}

struct SyncResponse: Codable {
    let synced: Int
}

final class ApiClient {
    private let baseUrl: URL
    private let session = URLSession.shared

    init(baseUrl: URL = ApiClient.defaultBaseUrl()) {
        self.baseUrl = baseUrl
    }

    func syncAttendance(_ records: [AttendanceRecord]) async throws -> SyncResponse {
        var request = URLRequest(url: baseUrl.appending(path: "/api/curriculum/attendance/offline-sync"))
        request.httpMethod = "POST"
        request.setValue("application/json", forHTTPHeaderField: "Content-Type")
        request.httpBody = try JSONEncoder().encode(records)
        let (data, response) = try await session.data(for: request)
        guard (response as? HTTPURLResponse)?.statusCode == 200 else { throw URLError(.badServerResponse) }
        return try JSONDecoder().decode(SyncResponse.self, from: data)
    }

    private static func defaultBaseUrl() -> URL {
        if let configured = Bundle.main.object(forInfoDictionaryKey: "NOCTURNE_API_BASE_URL") as? String,
           let url = URL(string: configured) {
            return url
        }

        return URL(string: "https://api.nocturne.local")!
    }
}
