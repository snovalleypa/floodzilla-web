using Microsoft.Data.SqlClient;
using System.Data;

namespace FzCommon
{
    public enum PushNotificationAttemptStatus
    {
        New,
        Sent,
        Complete,
        Failed,
        Timeout,
    }

    public class PushNotificationAttempt
    {
        //$ TODO: also store user / device IDs here?  token is unique so it's not strictly necessary
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Token { get; set; }

        //$ TODO: Add any other parameters that we might need in order to retry the send attempt
        public string? Title { get; set; }
        public string? Subtitle { get; set; }
        public string? Body { get; set; }
        public string? Data { get; set; }

        public string? TicketId { get; set; }
        public DateTime LastCheckTime { get; set; }

        public PushNotificationAttemptStatus Status { get; private set; }
        public DateTime SendTime { get; private set; }
        public int? LastRetrySeconds { get; private set; }
        public DateTime? NextActiveTime { get; private set; }

        // NOTE: The current plan is to have these checked by a process that runs once per minute,
        // so setting them to less than 60 seconds will not have the desired effect...
        const int FirstCheckDelaySeconds = 60;
        const int MaxDelaySeconds = 60 * 8;

        public async Task Succeed(SqlConnection sqlcn)
        {
            this.Status = PushNotificationAttemptStatus.Complete;
            this.NextActiveTime = null;
            await this.SaveAsync(sqlcn);
        }

        public async Task Fail(SqlConnection sqlcn)
        {
            this.Status = PushNotificationAttemptStatus.Failed;
            this.NextActiveTime = null;
            await this.SaveAsync(sqlcn);
        }

        public async Task Retry(SqlConnection sqlcn)
        {
            this.SetNextActiveTime();
            await this.SaveAsync(sqlcn);
        }

        public async Task AttemptWasSent(SqlConnection sqlcn)
        {
            this.Status = PushNotificationAttemptStatus.Sent;
            this.SetNextActiveTime();
            await this.SaveAsync(sqlcn);
        }

        // Based on the last time we checked, and the last amount of time
        // that we waited, compute the next time that we should check this
        // attempt for something to do.
        private void SetNextActiveTime()
        {
            if (!this.LastRetrySeconds.HasValue || this.LastRetrySeconds == 0)
            {
                this.LastRetrySeconds = FirstCheckDelaySeconds;
            }
            else
            {
                this.LastRetrySeconds *= 2;
            }

            if (this.LastRetrySeconds > MaxDelaySeconds)
            {
                this.NextActiveTime = null;
                this.Status = PushNotificationAttemptStatus.Timeout;
            }
            else
            {
                this.NextActiveTime = this.LastCheckTime.AddSeconds((double)this.LastRetrySeconds);
            }
        }

        public async static Task<PushNotificationAttempt> CreateAsync(SqlConnection sqlcn,
                                                                      string token,
                                                                      string? title,
                                                                      string? subtitle,
                                                                      string? body,
                                                                      string? data,
                                                                      DateTime sendTime,
                                                                      DateTime lastCheckTime,
                                                                      PushNotificationAttemptStatus status)
        {
            using SqlCommand cmd = new SqlCommand("CreatePushNotificationAttempt", sqlcn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Token", token);
            cmd.Parameters.AddWithValue("@Title", title);
            cmd.Parameters.AddWithValue("@Subtitle", subtitle);
            cmd.Parameters.AddWithValue("@Body", body);
            cmd.Parameters.AddWithValue("@Data", data);
            cmd.Parameters.AddWithValue("@SendTime", sendTime);
            cmd.Parameters.AddWithValue("@LastCheckTime", lastCheckTime);
            cmd.Parameters.AddWithValue("@Status", status);
            using SqlDataReader dr = await cmd.ExecuteReaderAsync();
            if (!await dr.ReadAsync())
            {
                throw new ApplicationException("Error saving push receipt attempt");
            }
            return InstantiateFromReader(dr);
        }

        public async Task SaveAsync(SqlConnection sqlcn)
        {
            using SqlCommand cmd = new SqlCommand("UpdatePushNotificationAttempt", sqlcn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Id", this.Id);
            cmd.Parameters.AddWithValue("@Status", this.Status);
            cmd.Parameters.AddWithValue("@LastCheckTime", this.LastCheckTime);
            cmd.Parameters.AddWithValue("@TicketId", this.TicketId);
            cmd.Parameters.AddWithValue("@LastRetrySeconds", this.LastRetrySeconds);
            cmd.Parameters.AddWithValue("@NextActiveTime", this.NextActiveTime);
            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task<List<PushNotificationAttempt>> GetActiveAttemptsAsync(SqlConnection sqlcn, DateTime activeTime)
        {
            List<PushNotificationAttempt> attempts = new();
            SqlCommand cmd = new SqlCommand("GetActivePushNotificationAttempts", sqlcn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@ActiveTime", activeTime);
            try
            {
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        attempts.Add(InstantiateFromReader(reader));
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorManager.ReportException(ErrorSeverity.Major, "PushNotificationAttempt.GetActiveAttempts", ex);
            }
            return attempts;
        }

        private static PushNotificationAttempt InstantiateFromReader(SqlDataReader dr)
        {
            return new PushNotificationAttempt()
            {
                Id = SqlHelper.Read<int>(dr, "Id"),
                Token = SqlHelper.Read<string>(dr, "Token"),
                Title = SqlHelper.Read<string?>(dr, "Title"),
                Subtitle = SqlHelper.Read<string?>(dr, "Subtitle"),
                Body = SqlHelper.Read<string?>(dr, "Body"),
                Data = SqlHelper.Read<string?>(dr, "Data"),
                Status = SqlHelper.Read<PushNotificationAttemptStatus>(dr, "Status"),
                SendTime = SqlHelper.Read<DateTime>(dr, "SendTime"),
                TicketId = SqlHelper.Read<string?>(dr, "TicketId"),
                LastCheckTime = SqlHelper.Read<DateTime>(dr, "LastCheckTime"),
                LastRetrySeconds = SqlHelper.Read<int?>(dr, "LastRetrySeconds"),
                NextActiveTime = SqlHelper.Read<DateTime?>(dr, "NextActiveTime"),
            };
        }
    }
}

