using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using API.WindowsService.Models;
using Contract;
using System.IO;

namespace API.WindowsService.Controllers
{
    public class ValidationJobController : ApiController
    {
        private readonly IValidationJobRepository _repository;
        private readonly IHelper _helper;
        private readonly ILogging _logging;

        public ValidationJobController(IValidationJobRepository repository, IHelper helper, ILogging logging)
        {
            _repository = repository;
            _logging = logging;
            _helper = helper;
        }

        /// <summary>
        /// Create a new job
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost]
        public Guid CreateNew(ValidationJobRequestModel input)
        {
            if (!ModelState.IsValid)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState));
            }

            var res = HandleNewValidationJob(input);
            _logging.Info($"Created new validation job : {res}");
            return res;
        }

        private Guid HandleNewValidationJob(ValidationJobRequestModel request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var jobRequest = new ValidationJobRequest
            {
                Filenames = request.Filenames,
                Needed = request.Needed.LocalDateTime
            };
            Guid jobCorrelationId = Guid.NewGuid();
            var jobs = new List<ValidationJob>();

            foreach (var sourceFilename in jobRequest.Filenames)
            {
                var info = _helper.GetMediainfo(sourceFilename);
                if (info.Duration <= 0)
                    throw new InvalidDataException($"Validation request failed. Input file is invalid. Duration: {info.Duration} sec.");

                // TODO different arguemnt for video/audio?
                var arguments = $"-v warning -xerror -i \"{sourceFilename}\" -map 0:1? -f null -";
                var validationJob = new ValidationJob
                {
                    JobCorrelationId = jobCorrelationId,
                    SourceFilename = sourceFilename,
                    Needed = request.Needed.DateTime,
                    State = TranscodingJobState.Queued,
                    Arguments = arguments,
                };
                jobs.Add(validationJob);
            }

            return _repository.Add(jobRequest, jobs);
        }

    }
}
