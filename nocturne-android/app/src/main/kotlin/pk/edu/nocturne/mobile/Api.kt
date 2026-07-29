package pk.edu.nocturne.mobile

import retrofit2.http.Body
import retrofit2.http.GET
import retrofit2.http.POST
import retrofit2.http.Path

data class AttendanceRecord(val id: String, val studentId: String, val status: String, val wasOfflineAt: Long)
data class SyncResponse(val synced: Int)

interface NocturneApi {
    @GET("api/attendance/student/{studentId}")
    suspend fun attendance(@Path("studentId") studentId: String): List<AttendanceRecord>

    @POST("api/curriculum/attendance/offline-sync")
    suspend fun syncAttendance(@Body records: List<AttendanceRecord>): SyncResponse
}
