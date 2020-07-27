﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using API.WindowsService.Filters;
using API.WindowsService.Models;
using Contract;

namespace API.WindowsService.Controllers
{
    [ApiAuthorization]
    public class AudioDemuxJobController : ApiController
    {
        private readonly IAudioDemuxJobRepository _repository;
        private readonly ILogging _logging;

        public AudioDemuxJobController(IAudioDemuxJobRepository repository, ILogging logging)
        {
            if (repository == null) throw new ArgumentNullException(nameof(repository));
            if (logging == null) throw new ArgumentNullException(nameof(logging));

            _repository = repository;
            _logging = logging;
        }

        [HttpPost]
        public Guid CreateNew(AudioDemuxJobRequestModel input)
        {
            if (!ModelState.IsValid)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState));
            }

            var res = HandleNewAudioDemuxJob(input);
            _logging.Info($"Created new mux job : {res}");
            return res;
        }

        private Guid HandleNewAudioDemuxJob(AudioDemuxJobRequestModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            var outputFilename = $"{model.OutputFolder}{Path.DirectorySeparatorChar}{model.DestinationFilename}";

            string arguments = string.Empty;

            if (model.LeftStream == model.RightStream)
                arguments += $"-i {model.VideoSourceFilename} -map 0:{model.LeftStream} {outputFilename} -y"; //We assume the stream is stereo already
            else
                arguments += $"-i {model.VideoSourceFilename} -filter_complex \"[0:{model.LeftStream}][0:{model.RightStream}]amerge = inputs = 2[aout]\" -map \"[aout]\" {outputFilename} -y";


            var jobs = new List<FFmpegJob>
            {
                new AudioDemuxJob
                {
                    Needed = model.Needed.LocalDateTime,
                    FfmpegExePath = ConfigurationWrapper.FFmpeg32,
                    Arguments = arguments,
                    State = TranscodingJobState.Queued,
                    DestinationFilename = outputFilename
                }
            };
            var request = new AudioDemuxJobRequest
            {
                VideoSourceFilename = model.VideoSourceFilename,
                DestinationFilename = model.DestinationFilename,
                OutputFolder = model.OutputFolder
            };

            return _repository.Add(request, jobs);
        }
    }
}