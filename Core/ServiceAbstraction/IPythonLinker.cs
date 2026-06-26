using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared.DataTransferObject;

namespace ServiceAbstraction
{
    public interface IPythonLinker
    {
        Task<PythonOutputDto?> GetOptimizedPlanAsync(PythonRequestDto request);
    }
}
