﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Unic.SitecoreCMS.Modules.UrlMapper.Security.Filter
{
    abstract public class BaseFilter
    {
        virtual public string Filter(string input)
        {
            if (input == null)
            {
                input = "";
            }
            string output = input.Trim();
            return output;
        }
    }
}
