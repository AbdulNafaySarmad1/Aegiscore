package pk.edu.nocturne.mobile

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.compose.material3.Button
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.material3.Surface
import androidx.compose.runtime.*

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContent {
            MaterialTheme {
                Surface {
                    var role by remember { mutableStateOf("Student") }
                    NocturneHome(role = role, onRole = { role = it })
                }
            }
        }
    }
}

@Composable
fun NocturneHome(role: String, onRole: (String) -> Unit) {
    androidx.compose.foundation.layout.Column {
        Text("Nocturne $role")
        listOf("Login", "Dashboard", "Attendance", "Grades", "Schedule", "Fees", "Notifications", "Profile", "Support").forEach { Text(it) }
        Button(onClick = { onRole(if (role == "Student") "Faculty" else "Student") }) { Text("Switch role") }
    }
}
