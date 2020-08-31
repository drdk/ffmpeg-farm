using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contract
{
    public interface IValidationJobRepository : IJobRepository
    {
        Guid Add(ValidationJobRequest request, ICollection<ValidationJob> jobs);
    }
}
