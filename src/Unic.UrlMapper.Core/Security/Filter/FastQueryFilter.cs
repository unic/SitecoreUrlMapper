using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Unic.UrlMapper.Core.Security.Filter
{
    public class FastQueryFilter : BaseFilter
    {
        public override string Filter(string input)
        {
            input = base.Filter(input);
            return input.Replace("'", "");
        }
    }
}
