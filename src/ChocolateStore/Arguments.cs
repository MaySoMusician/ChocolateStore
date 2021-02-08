using System;
using System.Collections;
using System.Collections.Generic;

namespace ChocolateStore
{
    public class Arguments
    {
        public string Directory { get; set; }
        public string PackageName { get; set; }
        public IEnumerable<Tuple<string, IEnumerable<string>>> Variables { get; set; }
    }
}
