﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Contract;
using Dapper;

namespace API.Repository
{
    /// <summary>
    /// Receives hard subtitles preprocess jobs orders.
    /// </summary>
    public class HardSubtitlesJobRepository : JobRepository, IHardSubtilesJobRepository
    {
        private readonly string _connectionString;

        public HardSubtitlesJobRepository(IHelper helper, string connectionString) : base(helper)
        {
            if (string.IsNullOrWhiteSpace(connectionString)) throw new ArgumentNullException(nameof(connectionString));

            _connectionString = connectionString;
        }

        public Guid Add(HardSubtilesJobRequest request, ICollection<FFmpegJob> jobs)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (jobs == null) throw new ArgumentNullException(nameof(jobs));

            Guid jobCorrelationId = Guid.NewGuid();

            using (var scope = TransactionUtils.CreateTransactionScope())
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Execute(
                        "INSERT INTO FfmpegHardSubtitlesRequest (JobCorrelationId, VideoSourceFilename, SubtilesFilename, DestinationFilename, OutputFolder, Needed, Created) VALUES(@JobCorrelationId, @VideoSourceFilename, @AudioSourceFilename, @DestinationFilename, @OutputFolder, @Needed, @Created);",
                        new
                        {
                            JobCorrelationId = jobCorrelationId,
                            request.VideoSourceFilename,
                            request.SubtilesFilename,
                            request.Needed,
                            request.DestinationFilename,
                            request.OutputFolder,
                            Created = DateTime.UtcNow
                        });

                    foreach (MuxJob job in jobs.Select(x => x as MuxJob))
                    {
                        var jobId = connection.ExecuteScalar<int>(
                            "INSERT INTO FfmpegJobs (JobCorrelationId, Created, Needed, JobState, JobType) VALUES(@JobCorrelationId, @Created, @Needed, @State, @JobType);SELECT @@IDENTITY;",
                            new
                            {
                                JobCorrelationId = jobCorrelationId,
                                Created = DateTimeOffset.UtcNow,
                                job.Needed,
                                State = job.State,
                                JobType = JobType.Mux
                            });

                        connection.Execute(
                            "INSERT INTO FfmpegTasks (FfmpegJobs_id, Arguments, TaskState, DestinationFilename, DestinationDurationSeconds, VerifyOutput) VALUES(@FfmpegJobsId, @Arguments, @QueuedState, @DestinationFilename, @DestinationDurationSeconds, @VerifyOutput);",
                            new
                            {
                                FfmpegJobsId = jobId,
                                job.Arguments,
                                QueuedState = TranscodingJobState.Queued,
                                job.DestinationFilename,
                                job.DestinationDurationSeconds,
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