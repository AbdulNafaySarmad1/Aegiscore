package pk.edu.nocturne.mobile

import androidx.room.*

@Entity(tableName = "offline_attendance")
data class OfflineAttendance(
    @PrimaryKey val id: String,
    val studentId: String,
    val status: String,
    val wasOfflineAt: Long,
    val synced: Boolean = false
)

@Dao
interface OfflineAttendanceDao {
    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun save(record: OfflineAttendance)

    @Query("SELECT * FROM offline_attendance WHERE synced = 0 AND wasOfflineAt >= :cutoff")
    suspend fun pending(cutoff: Long): List<OfflineAttendance>

    @Query("DELETE FROM offline_attendance WHERE synced = 1 OR wasOfflineAt < :cutoff")
    suspend fun prune(cutoff: Long)
}

@Database(entities = [OfflineAttendance::class], version = 1)
abstract class NocturneDatabase : RoomDatabase() {
    abstract fun attendance(): OfflineAttendanceDao
}
