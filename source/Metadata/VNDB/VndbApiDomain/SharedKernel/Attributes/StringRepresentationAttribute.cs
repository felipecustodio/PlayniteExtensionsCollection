﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VndbApiDomain.SharedKernel
{
    public class StringRepresentationAttribute : Attribute
    {
        public string Value { get; }

        public StringRepresentationAttribute(string value)
        {
            Value = value;
        }

        public override string ToString()
        {
            return Value;
        }
    }
}
