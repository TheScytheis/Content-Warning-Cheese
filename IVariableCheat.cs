using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestUnityPlugin
{
    internal interface IVariableCheat<T>
    {
        T Value { get; set; }
    }
}

