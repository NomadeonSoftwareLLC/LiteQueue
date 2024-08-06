/* Copyright 2024 by Nomadeon LLC. Licensed uinder MIT: https://opensource.org/licenses/MIT */
using System;
using System.Collections.Generic;
using System.Text;

namespace LiteQueueTests
{
    class DuplicateException : Exception
    {
        object _dupe;

        public DuplicateException(object dupe)
        {
            _dupe = dupe;
        }

        public override string ToString()
        {
            return base.ToString() + " duplicate: " + _dupe;
        }
    }
}
