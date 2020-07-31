using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Contract;
using Dapper;

namespace API.Repository
{
    public class ValidationJobRepository : JobRepository, IValidationJobRepository
    {
        private readonly IHelper _helper;

        public ValidationJobRepository(IHelper helper) : base(helper)
        {
            _helper = helper ?? throw new ArgumentNullException(nameof(helper));
        }

        public Guid Add(ValidationJobRequest request, ICollection<ValidationJob> jobs)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (jobs == null) throw new ArgumentNullException(nameof(jobs));
            if (jobs.Count == 0) throw new ArgumentException("Jobs parameter must contain at least 1 job", nameof(jobs));

            Guid jobCorrelationId = Guid.NewGuid();

            using (var scope = TransactionUtils.CreateTransactionScope())
            {
                using (var connection = _helper.GetConnection())
                {
                    connection.Execute(
                        "INSERT INTO [FfmpegValidationRequest] (JobCorrelationId, Filenames, Needed, Created) VALUES(@JobCorrelationId, @Filenames, @Needed, @Created);",
                        new
                        {
                            JobCorrelationId = jobCorrelationId,
                            Filenames = string.Join(";", request.Filenames),
                            request.Needed,
                            Created = DateTime.UtcNow
                        });

                    var jobId = connection.ExecuteScalar<int>(
                        "INSERT INTO FfmpegJobs (JobCorrelationId, Created, Needed, JobState, JobType) VALUES(@JobCorrelationId, @Created, @Needed, @JobState, @JobType);SELECT @@IDENTITY;",
                        new
                        {
                            JobCorrelationId = jobCorrelationId,
                            Created = DateTimeOffset.UtcNow,
                            request.Needed,
                            JobState = TranscodingJobState.Queued,
                            JobType = JobType.Validation
                        });

                    foreach (var scrubJob in jobs)
                    {
                        connection.Execute(
                            "INSERT INTO FfmpegTasks (FfmpegJobs_id,FfmpegExePath,  Arguments, TaskState, DestinationFilename, DestinationDurationSeconds, VerifyOutput) VALUES(@FfmpegJobsId,@FfmpegExePath, @Arguments, @TaskState, @DestinationFilename, @DestinationDurationSeconds, @VerifyOutput);",
                            new
                            {
                                FfmpegJobsId = jobId,
                                scrubJob.FfmpegExePath,
                                scrubJob.Arguments,
                                TaskState = TranscodingJobState.Queued,
                                scrubJob.DestinationFilename,
                                scrubJob.DestinationDurationSeconds,
                                VerifyOutput = false
                            });
                    }
                }

                scope.Complete();

                return jobCorrelationId;
            }
        }
    }
}
