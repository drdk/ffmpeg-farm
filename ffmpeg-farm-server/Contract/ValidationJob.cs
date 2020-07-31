using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contract
{
    public class ValidationJob : FFmpegJob
    {
        public override JobType Type => JobType.Validation;
    }
}
