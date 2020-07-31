using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contract
{
    public class ValidationJobRequest : JobRequest
    {
        public List<string> Filenames { get; set; }

        public override bool Equals(object other)
        {
            var b = other as ValidationJobRequest;
            if (b == null)
                return false;

            return (Needed == b.Needed &&
                    Filenames == b.Filenames);
        }

    }
}
