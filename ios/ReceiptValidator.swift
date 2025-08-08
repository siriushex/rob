import Foundation

struct ValidationResponse: Decodable { let success: Bool }

enum ReceiptValidator {
    static func validate(receiptData: Data) async throws -> Bool {
        var request = URLRequest(url: URL(string: "https://example.com/api/validate/ios")!)
        request.httpMethod = "POST"
        request.setValue("application/json", forHTTPHeaderField: "Content-Type")
        request.httpBody = try JSONSerialization.data(withJSONObject: ["receipt": receiptData.base64EncodedString()])
        let (data, _) = try await URLSession.shared.data(for: request)
        return try JSONDecoder().decode(ValidationResponse.self, from: data).success
    }
}
