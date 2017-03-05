using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using WebhostUniversalWindows.Attendance;

namespace WebhostUniversalWindows
{
    public partial class SectionInfo
    {
        /// <summary>
        /// Get today's attendances.
        /// </summary>
        public async Task<List<AttendanceInfo>> AttendancesToday()
        {
            return (List<AttendanceInfo>)await WebhostAPICall.GetObjectAsync(String.Format("api/self/sections/{0}/attendance", this.Id), typeof(List<AttendanceInfo>));
        }

        /// <summary>
        /// Get attendances for a particular date.
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public async Task<List<AttendanceInfo>> GetAttendances(DateTime date)
        {
            return (List<AttendanceInfo>)await WebhostAPICall.GetObjectAsync(String.Format("api/self/sections/{0}/attendance?datebinary={1}", this.Id, date.ToBinary()), typeof(List<AttendanceInfo>));
        }

        /// <summary>
        /// Submit a list of attendances for this section.
        /// </summary>
        /// <param name="attendances"></param>
        /// <returns></returns>
        public async Task<bool> SubmitAttendance(List<AttendanceInfo> attendances)
        {
            List<AttendanceInfo> submitted =
                (List<AttendanceInfo>)await WebhostAPICall.GetObjectAsync(String.Format("api/self/section/{0}/attendance", this.Id), typeof(List<AttendanceInfo>), "PUT",
                                                                    typeof(List<AttendanceInfo>), attendances);

            return submitted.Count == attendances.Count;
        }
    }
}
