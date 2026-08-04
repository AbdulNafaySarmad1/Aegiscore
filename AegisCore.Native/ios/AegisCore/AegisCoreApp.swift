import SwiftUI

@main
struct AegisCoreApp: App {
    var body: some Scene {
        WindowGroup { HomeView() }
    }
}

struct HomeView: View {
    @State private var role = "Student"
    private let screens = ["Login", "Dashboard", "Attendance", "Grades", "Schedule", "Fees", "Notifications", "Profile", "Support"]

    var body: some View {
        NavigationStack {
            List {
                Picker("Role", selection: $role) {
                    Text("Student").tag("Student")
                    Text("Faculty").tag("Faculty")
                }.pickerStyle(.segmented)
                ForEach(screens, id: \.self) { Text($0) }
            }
            .navigationTitle("AegisCore \(role)")
        }
    }
}
