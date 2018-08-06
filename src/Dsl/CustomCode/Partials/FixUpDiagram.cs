using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Modeling;

namespace Sawczyn.EFDesigner.EFModel
{
   partial class FixUpDiagram
   {
      protected override bool SkipFixup(ModelElement childElement)
      {
         return base.SkipFixup(childElement);
      }
   }
}
